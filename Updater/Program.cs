using System.CommandLine;
using System.IO.Compression;
using System.Security.Cryptography;
using System.Text.Json;
using System.Text;
using Serilog;
using Serilog.Events;
using Serilog.Enrichers;
using Polly;
using NuGet.Versioning;
using System.Net.Http;
using System.Net.Security;
using System.Security.Cryptography.X509Certificates;
using System.Diagnostics;
using System.Threading.Channels;

record UpdaterConfig(
    string LocalSolutionPath,
    string VersionFileName,
    string LatestAliasPath,
    string[] PreservePaths,
    string ApplyMode,
    int BackupRetention,
    bool ValidateServerCertificate,
    int ConnectTimeoutSeconds,
    int ReadWriteTimeoutSeconds,
    int RetryCount,
    int RetryBaseDelayMs,
    int ParallelCopyDegree,
    bool ValidateBuild,
    bool JsonLogs,
    int LogRetentionDays,
    string? HttpBaseUrl = null,
    string[]? CertificatePinsSha256 = null
);

record VersionManifest(
    string version,
    string buildTimestamp,
    string artifact,
    string sha256,
    string? minUpdaterVersion = null,
    Dictionary<string,string>? files = null
);

record ProgressInfo(double Percent, long TransferredBytes, long TotalBytes);

class ThrottledProgress : IProgress<ProgressInfo>
{
    private readonly string _label;
    private double _lastPerc = -1;
    private DateTime _lastLog = DateTime.MinValue;
    private readonly object _lock = new();
    private readonly Stopwatch _sw = Stopwatch.StartNew();
    public ThrottledProgress(string label) => _label = label;
    public void Report(ProgressInfo value)
    {
        lock (_lock)
        {
            var perc = value.TotalBytes > 0 ? value.Percent : (value.TransferredBytes > 0 ? Math.Min(99.9, value.TransferredBytes / 1.0) : 0);
            var now = DateTime.UtcNow;
            if (perc >= 100 || perc - _lastPerc >= 1 || (now - _lastLog).TotalSeconds >= 0.5)
            {
                _lastPerc = perc;
                _lastLog = now;
                var speed = value.TransferredBytes / Math.Max(1, _sw.Elapsed.TotalSeconds);
                var remainBytes = Math.Max(0, value.TotalBytes - value.TransferredBytes);
                var eta = TimeSpan.FromSeconds(speed > 0 ? remainBytes / speed : 0);

                // Build progress bar
                int barWidth = 50;
                int filled = (int)Math.Round((perc / 100.0) * barWidth);
                if (filled > barWidth) filled = barWidth;
                // Use ASCII characters for compatibility instead of Unicode blocks
                string bar = new string('#', filled) + new string('-', barWidth - filled);
                // Format percent correctly (perc already 0..100). Avoid "P" format which multiplies.
                string pctText = perc >= 100 ? "100.0%" : perc.ToString("0.0") + "%";
                string line = $"{_label} [{bar}] {pctText} {FormatBytes(value.TransferredBytes)}/{FormatBytes(Math.Max(1,value.TotalBytes))} @ {FormatBytes((long)speed)}/s ETA {eta:hh\\:mm\\:ss}";

                try
                {
                    int consoleWidth;
                    try { consoleWidth = Console.WindowWidth; } catch { consoleWidth = 120; }
                    if (line.Length > consoleWidth - 1)
                        line = line.Substring(0, Math.Max(0, consoleWidth - 4)) + "...";
                    Console.Write("\r" + line.PadRight(Math.Max(1, consoleWidth - 1)));
                }
                catch { }

                Log.Information("progress:{Label} {Percent:0.0} {Transferred} {Total} {Speed}/s ETA {ETA}", _label, perc, value.TransferredBytes, value.TotalBytes, (long)speed, eta);
                if (perc >= 100)
                {
                    try { Console.WriteLine(); } catch { }
                    Log.Information("{Label} download complete", _label);
                }
            }
        }
    }
    static string FormatBytes(long b)
    {
        string[] units = { "B","KB","MB","GB","TB" }; double v = b; int i=0; while(v>=1024 && i<units.Length-1){ v/=1024; i++; } return $"{v:0.##}{units[i]}"; }
}

class Program
{
    static readonly JsonSerializerOptions ManifestJsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        ReadCommentHandling = JsonCommentHandling.Skip,
        AllowTrailingCommas = true
    };

    static async Task<int> Main(string[] args)
    {
        var optConfig = new Option<FileInfo?>("--config", description: "Path to appsettings.json");
        var optForce = new Option<bool>("--force", () => false, "Force update ignoring version");
        var optDryRun = new Option<bool>("--dry-run", () => false, "Show actions without applying");
        var optNoBackup = new Option<bool>("--no-backup", () => false, "Skip backup");
        var optApplyMode = new Option<string?>("--apply-mode", description: "Override apply mode: swap|overlay");
        var optJsonLogs = new Option<bool>("--json-logs", () => false, "Emit JSON logs");

        var rootCommand = new RootCommand("Solution Updater");
        rootCommand.AddOption(optConfig);
        rootCommand.AddOption(optForce);
        rootCommand.AddOption(optDryRun);
        rootCommand.AddOption(optNoBackup);
        rootCommand.AddOption(optApplyMode);
        rootCommand.AddOption(optJsonLogs);

        rootCommand.SetHandler(async (FileInfo? configFile, bool force, bool dryRun, bool noBackup, string? applyModeOverride, bool jsonLogs) =>
        {
            using var cts = new CancellationTokenSource();
            Console.CancelKeyPress += (s, e) => { e.Cancel = true; cts.Cancel(); };

            LoadDotEnv();

            var baseDir = AppContext.BaseDirectory;
            var cfg = LoadConfig(configFile?.FullName ?? Path.Combine(baseDir, "appsettings.json"));
            if (applyModeOverride is { Length: > 0 }) cfg = cfg with { ApplyMode = applyModeOverride };
            if (jsonLogs) cfg = cfg with { JsonLogs = true };

            ConfigureLogging(cfg);

            try
            {
                var result = await RunAsync(cfg, force, dryRun, noBackup, cts.Token);
                Log.CloseAndFlush();
                Environment.ExitCode = result;
            }
            catch (OperationCanceledException)
            {
                Log.Warning("Operation canceled by user");
                Log.CloseAndFlush();
                Environment.ExitCode = 130;
            }
            catch (Exception ex)
            {
                Log.Fatal(ex, "Fatal error");
                Log.CloseAndFlush();
                Environment.ExitCode = 1;
            }
        }, optConfig, optForce, optDryRun, optNoBackup, optApplyMode, optJsonLogs);

        return await rootCommand.InvokeAsync(args);
    }

    static void LoadDotEnv()
    {
        foreach (var path in new[] { Path.Combine(AppContext.BaseDirectory, ".env"), Path.Combine(Directory.GetCurrentDirectory(), ".env") })
        {
            if (!File.Exists(path)) continue;
            foreach (var line in File.ReadAllLines(path))
            {
                var t = line.Trim();
                if (string.IsNullOrWhiteSpace(t) || t.StartsWith("#")) continue;
                var idx = t.IndexOf('=');
                if (idx <= 0) continue;
                var key = t[..idx].Trim();
                var val = t[(idx + 1)..].Trim().Trim('"');
                if (string.IsNullOrEmpty(Environment.GetEnvironmentVariable(key)))
                {
                    Environment.SetEnvironmentVariable(key, val);
                }
            }
        }
    }

    static UpdaterConfig LoadConfig(string path)
    {
        using var fs = File.OpenRead(path);
        using var doc = JsonDocument.Parse(fs);
        var root = doc.RootElement.GetProperty("Updater");
        static string EnvOr(string key, string? fallback)
        {
            var env = Environment.GetEnvironmentVariable(key);
            return !string.IsNullOrEmpty(env) ? env : (fallback ?? string.Empty);
        }
        static string Expand(string? val)
        {
            if (string.IsNullOrEmpty(val)) return string.Empty;
            if (val.StartsWith("${") && val.EndsWith("}"))
            {
                var key = val.Trim('$', '{', '}');
                return Environment.GetEnvironmentVariable(key) ?? string.Empty;
            }
            return val;
        }
        return new UpdaterConfig(
            EnvOr("LOCAL_SOLUTION_PATH", Expand(root.GetProperty("LocalSolutionPath").GetString() ?? "")),
            root.GetProperty("VersionFileName").GetString()!,
            root.GetProperty("LatestAliasPath").GetString()!,
            root.GetProperty("PreservePaths").EnumerateArray().Select(e => e.GetString()!).ToArray(),
            root.GetProperty("ApplyMode").GetString()!,
            root.GetProperty("BackupRetention").GetInt32(),
            root.GetProperty("ValidateServerCertificate").GetBoolean(),
            root.GetProperty("ConnectTimeoutSeconds").GetInt32(),
            root.GetProperty("ReadWriteTimeoutSeconds").GetInt32(),
            root.GetProperty("RetryCount").GetInt32(),
            root.GetProperty("RetryBaseDelayMs").GetInt32(),
            root.GetProperty("ParallelCopyDegree").GetInt32(),
            root.GetProperty("ValidateBuild").GetBoolean(),
            root.GetProperty("JsonLogs").GetBoolean(),
            root.GetProperty("LogRetentionDays").GetInt32(),
            EnvOr("HTTP_BASE_URL", root.TryGetProperty("HttpBaseUrl", out var http) ? http.GetString() : null),
            root.TryGetProperty("CertificatePinsSha256", out var pins) ? pins.EnumerateArray().Select(p => p.GetString()!).Where(p=>!string.IsNullOrEmpty(p)).ToArray() : null
        );
    }

    static void ConfigureLogging(UpdaterConfig cfg)
    {
        Directory.CreateDirectory(Path.Combine(AppContext.BaseDirectory, "logs"));
        var conf = new LoggerConfiguration()
            .MinimumLevel.Debug()
            .Enrich.WithProperty("App", "Updater")
            .Enrich.WithEnvironmentUserName()
            .Enrich.WithMachineName()
            .Enrich.WithThreadId();

        if (cfg.JsonLogs)
            conf = conf.WriteTo.File(new Serilog.Formatting.Json.JsonFormatter(), Path.Combine("logs", "updater.json"), rollingInterval: RollingInterval.Day, retainedFileCountLimit: 10, restrictedToMinimumLevel: LogEventLevel.Information);
        else
            conf = conf.WriteTo.File(Path.Combine("logs", "updater-.log"), rollingInterval: RollingInterval.Day, retainedFileCountLimit: 10);

        conf = conf.WriteTo.Console();
        Log.Logger = conf.CreateLogger();
    }

    static async Task<int> RunAsync(UpdaterConfig cfg, bool force, bool dryRun, bool noBackup, CancellationToken ct)
    {
        Log.Information("Updater start. Force={Force} DryRun={DryRun}", force, dryRun);
        var manifest = await LoadRemoteManifest(cfg, ct);

        var localManifestPath = Path.Combine(cfg.LocalSolutionPath, cfg.VersionFileName);
        VersionManifest? local = null;
        if (File.Exists(localManifestPath))
        {
            try { local = JsonSerializer.Deserialize<VersionManifest>(await File.ReadAllTextAsync(localManifestPath, ct), ManifestJsonOptions); }
            catch (Exception ex) { Log.Warning(ex, "Failed reading local manifest"); }
        }

        if (manifest.minUpdaterVersion is { Length: > 0 })
        {
            var myVersion = typeof(Program).Assembly.GetName().Version?.ToString() ?? "0.0.0";
            if (NuGetVersion.TryParse(manifest.minUpdaterVersion, out var min) && NuGetVersion.TryParse(myVersion, out var cur))
            {
                if (cur < min)
                {
                    Log.Error("Updater version {Cur} is below required {Min}", cur, min);
                    return 5;
                }
            }
        }

        bool needsUpdate = force || local == null || IsRemoteNewer(local.version, manifest.version);
        if (!needsUpdate)
        {
            Log.Information("Already latest version {Version}", manifest.version);
            return 0;
        }
        Log.Information("Update required: local={Local} remote={Remote}", local?.version ?? "<none>", manifest.version);

        if (dryRun)
        {
            Log.Information("Dry-run: would download and apply version {Version}", manifest.version);
            return 0;
        }

        var (zipPath, checksum) = await DownloadArtifactWithRetry(cfg, manifest, ct);
        ValidateChecksum(zipPath, checksum);

        var stagingDir = Path.Combine(AppContext.BaseDirectory, "_staging", manifest.version);
        Directory.CreateDirectory(stagingDir);
        var extractDir = Path.Combine(stagingDir, "extract");
        ExtractZip(zipPath, extractDir);

        if (cfg.ValidateBuild)
        {
            var sln = Directory.GetFiles(extractDir, "*.sln", SearchOption.TopDirectoryOnly).FirstOrDefault();
            if (sln != null)
            {
                Log.Information("Validating build of extracted solution {Sln}", sln);
            }
        }

        string targetDir = Path.GetFullPath(cfg.LocalSolutionPath);
        string backupRoot = Path.Combine(AppContext.BaseDirectory, "_backup");
        Directory.CreateDirectory(backupRoot);
        string? backupDir = null;

        if (!noBackup && Directory.Exists(targetDir))
        {
            backupDir = Path.Combine(backupRoot, DateTime.UtcNow.ToString("yyyyMMdd_HHmmss"));
            Log.Information("Creating backup at {BackupDir}", backupDir);
            await CopyDirectoryAsync(targetDir, backupDir, cfg.PreservePaths, cfg.ParallelCopyDegree, ct);
        }

        try
        {
            await ApplyUpdateAsync(cfg, extractDir, targetDir, ct);
            await File.WriteAllTextAsync(localManifestPath, JsonSerializer.Serialize(manifest, new JsonSerializerOptions { WriteIndented = true }), ct);
            CleanupBackups(backupRoot, cfg.BackupRetention);
            CleanupStaging();
            Log.Information("Update applied successfully to {Version}", manifest.version);
            return 0;
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Failed applying update");
            if (backupDir != null)
            {
                Log.Warning("Attempting rollback from {BackupDir}", backupDir);
                try
                {
                    CleanDirectory(targetDir, cfg.PreservePaths);
                    await CopyDirectoryAsync(backupDir, targetDir, cfg.PreservePaths, cfg.ParallelCopyDegree, ct);
                    Log.Information("Rollback succeeded");
                    return 3;
                }
                catch (Exception rbEx)
                {
                    Log.Fatal(rbEx, "Rollback failed");
                    return 4;
                }
            }
            return 2;
        }
    }

    static bool IsRemoteNewer(string local, string remote)
    {
        if (NuGetVersion.TryParse(local, out var l) && NuGetVersion.TryParse(remote, out var r))
            return r > l;
        return !string.Equals(local, remote, StringComparison.OrdinalIgnoreCase);
    }

    static async Task<VersionManifest> LoadRemoteManifest(UpdaterConfig cfg, CancellationToken ct)
    {
        if (string.IsNullOrEmpty(cfg.HttpBaseUrl)) throw new InvalidOperationException("HttpBaseUrl is required in config");
        var url = CombineRemote(cfg.HttpBaseUrl!, cfg.LatestAliasPath, cfg.VersionFileName);
        using var http = CreateHttpClient(cfg);
        Log.Information("Downloading manifest from {Url}", url);
        using var resp = await http.GetAsync(url, ct);
        resp.EnsureSuccessStatusCode();
        var json = await resp.Content.ReadAsStringAsync(ct);
        if (string.IsNullOrWhiteSpace(json) || !json.TrimStart().StartsWith("{"))
        {
            Log.Error("Manifest content is not JSON. First 200 chars: {Snippet}", json?.Substring(0, Math.Min(200, json.Length)));
            throw new InvalidOperationException("Remote manifest is empty or not JSON");
        }
        try
        {
            using var doc = JsonDocument.Parse(json);
            var root = doc.RootElement;
            string GetString(string name)
            {
                if (!root.TryGetProperty(name, out var el)) return string.Empty;
                if (el.ValueKind == JsonValueKind.String) return el.GetString() ?? string.Empty;
                if (el.ValueKind == JsonValueKind.Object && el.TryGetProperty("value", out var v) && v.ValueKind == JsonValueKind.String) return v.GetString() ?? string.Empty; // PowerShell object style
                return string.Empty;
            }
            var version = GetString("version");
            var buildTs = GetString("buildTimestamp");
            var artifact = GetString("artifact");
            var sha256 = GetString("sha256");
            if (string.IsNullOrWhiteSpace(sha256) && root.TryGetProperty("sha256", out var shaEl) && shaEl.ValueKind == JsonValueKind.Object)
            {
                // fallback: maybe property is directly the hash under different key
                foreach (var prop in shaEl.EnumerateObject())
                {
                    if (prop.NameEquals("Hash") || prop.NameEquals("hash"))
                    {
                        if (prop.Value.ValueKind == JsonValueKind.String) sha256 = prop.Value.GetString() ?? sha256;
                    }
                }
            }
            if (string.IsNullOrWhiteSpace(version) || string.IsNullOrWhiteSpace(artifact) || string.IsNullOrWhiteSpace(sha256))
            {
                Log.Error("Manifest missing required fields after normalization: {Json}", json);
                throw new InvalidOperationException("Manifest missing required fields");
            }
            var minUpd = GetString("minUpdaterVersion");
            Dictionary<string,string>? files = null;
            if (root.TryGetProperty("files", out var filesEl) && filesEl.ValueKind == JsonValueKind.Object)
            {
                files = new Dictionary<string,string>();
                foreach (var f in filesEl.EnumerateObject())
                {
                    if (f.Value.ValueKind == JsonValueKind.String)
                        files[f.Name] = f.Value.GetString() ?? string.Empty;
                }
            }
            return new VersionManifest(version, buildTs, artifact, sha256, string.IsNullOrWhiteSpace(minUpd)? null : minUpd, files);
        }
        catch (JsonException jex)
        {
            Log.Error(jex, "Failed to parse manifest JSON. Content: {Json}", json);
            throw;
        }
    }

    static async Task<(string zipPath, string checksum)> DownloadArtifactWithRetry(UpdaterConfig cfg, VersionManifest manifest, CancellationToken ct)
    {
        // Retry only the ZIP download
        var policy = Polly.Policy
            .Handle<Exception>()
            .WaitAndRetryAsync(cfg.RetryCount, i => TimeSpan.FromMilliseconds(cfg.RetryBaseDelayMs * Math.Pow(2, i - 1)), (ex, ts, attempt, _) =>
            {
                Log.Warning(ex, "Retry {Attempt} after {Delay} downloading ZIP", attempt, ts);
            });

        string stagingRoot = Path.Combine(AppContext.BaseDirectory, "_downloads");
        Directory.CreateDirectory(stagingRoot);
        string zipPath = Path.Combine(stagingRoot, manifest.version + ".zip");
        string zipUrl = CombineRemote(cfg.HttpBaseUrl!, manifest.version, manifest.artifact);

        var progress = new ThrottledProgress("ZIP");

        await policy.ExecuteAsync(async () =>
        {
            using var http = CreateHttpClient(cfg);
            await DownloadHttpWithProgressAsync(http, zipUrl, zipPath, progress, ct);
        });

        var checksum = (manifest.sha256 ?? string.Empty).Trim();
        if (string.IsNullOrWhiteSpace(checksum))
        {
            throw new InvalidOperationException("Manifest does not contain a sha256 value");
        }
        return (zipPath, checksum);
    }

    static void ValidateChecksum(string zipPath, string expected)
    {
        using var fs = File.OpenRead(zipPath);
        using var sha256 = SHA256.Create();
        var sha = sha256.ComputeHash(fs);
        var actual = BitConverter.ToString(sha).Replace("-", string.Empty).ToLowerInvariant();
        if (!string.Equals(actual, expected, StringComparison.OrdinalIgnoreCase))
            throw new InvalidOperationException($"Checksum mismatch expected {expected} actual {actual}");
        Log.Information("Checksum validated");
    }

    static void ExtractZip(string zipPath, string extractDir)
    {
        if (Directory.Exists(extractDir)) Directory.Delete(extractDir, true);
        ZipFile.ExtractToDirectory(zipPath, extractDir);
        Log.Information("Extracted zip to {Dir}", extractDir);
    }

    static async Task ApplyUpdateAsync(UpdaterConfig cfg, string sourceDir, string targetDir, CancellationToken ct)
    {
        if (string.Equals(cfg.ApplyMode, "swap", StringComparison.OrdinalIgnoreCase))
        {
            Log.Information("Applying update with swap strategy");
            CleanDirectory(targetDir, cfg.PreservePaths);
            await CopyDirectoryAsync(sourceDir, targetDir, cfg.PreservePaths, cfg.ParallelCopyDegree, ct);
        }
        else
        {
            Log.Information("Applying update with overlay strategy");
            await CopyDirectoryAsync(sourceDir, targetDir, cfg.PreservePaths, cfg.ParallelCopyDegree, ct);
        }
    }

    static HttpClient CreateHttpClient(UpdaterConfig cfg)
    {
        var handler = new HttpClientHandler();
        if (cfg.ValidateServerCertificate && cfg.CertificatePinsSha256 is { Length: > 0 })
        {
            handler.ServerCertificateCustomValidationCallback = (req, cert, chain, errors) =>
            {
                if (errors != SslPolicyErrors.None) return false;
                if (cert is null) return false;
                using var sha256 = SHA256.Create();
                var hash = sha256.ComputeHash(cert.GetRawCertData());
                var hex = BitConverter.ToString(hash).Replace("-", string.Empty).ToLowerInvariant();
                return cfg.CertificatePinsSha256!.Any(pin => string.Equals(pin, hex, StringComparison.OrdinalIgnoreCase));
            };
        }
        return new HttpClient(handler) { Timeout = TimeSpan.FromSeconds(cfg.ReadWriteTimeoutSeconds) };
    }

    static async Task DownloadHttpWithProgressAsync(HttpClient http, string url, string destPath, ThrottledProgress progress, CancellationToken ct)
    {
        using var resp = await http.GetAsync(url, HttpCompletionOption.ResponseHeadersRead, ct);
        resp.EnsureSuccessStatusCode();
        var total = resp.Content.Headers.ContentLength ?? -1L;
        await using var src = await resp.Content.ReadAsStreamAsync(ct);
        await using var dst = File.Create(destPath);
        var buffer = new byte[1024 * 256];
        long readTotal = 0;
        int read;
        var lastReport = DateTime.UtcNow;
        while ((read = await src.ReadAsync(buffer.AsMemory(0, buffer.Length), ct)) > 0)
        {
            await dst.WriteAsync(buffer.AsMemory(0, read), ct);
            readTotal += read;
            var now = DateTime.UtcNow;
            if (total > 0)
            {
                var percent = Math.Min(100.0, (readTotal * 100.0) / Math.Max(1, total));
                if ((now - lastReport).TotalMilliseconds >= 250)
                {
                    lastReport = now;
                    progress.Report(new ProgressInfo(percent, readTotal, total));
                }
            }
        }
        progress.Report(new ProgressInfo(100, total, total));
    }

    static string CombineRemote(params string[] parts)
        => string.Join('/', parts.Where(p => !string.IsNullOrWhiteSpace(p)).Select(p => p.Trim('/')));

    static void CleanupBackups(string backupRoot, int keep)
    {
        if (!Directory.Exists(backupRoot)) return;
        var dirs = new DirectoryInfo(backupRoot).GetDirectories()
            .OrderByDescending(d => d.CreationTimeUtc)
            .ToList();
        foreach (var d in dirs.Skip(keep))
        {
            try { d.Delete(true); Log.Information("Removed old backup {Dir}", d.FullName); } catch { }
        }
    }

    static void CleanupStaging()
    {
        string[] toClean = { "_staging", "_downloads" };
        foreach (var d in toClean)
        {
            var path = Path.Combine(AppContext.BaseDirectory, d);
            if (Directory.Exists(path))
            {
                try { Directory.Delete(path, true); Log.Information("Cleaned {Path}", path); } catch { }
            }
        }
    }

    static async Task CopyDirectoryAsync(string sourceDir, string targetDir, string[] preservePaths, int parallelDegree, CancellationToken ct)
    {
        Directory.CreateDirectory(targetDir);
        var files = Directory.GetFiles(sourceDir, "*", SearchOption.AllDirectories);
        var dirs = Directory.GetDirectories(sourceDir, "*", SearchOption.AllDirectories);

        foreach (var dir in dirs)
        {
            var rel = Path.GetRelativePath(sourceDir, dir);
            if (preservePaths.Any(p => IsPreserved(rel, p))) continue;
            if (IsLogDirectory(rel)) continue; // skip logs folder
            Directory.CreateDirectory(Path.Combine(targetDir, rel));
        }

        var channel = Channel.CreateBounded<string>(new BoundedChannelOptions(parallelDegree) { FullMode = BoundedChannelFullMode.Wait });
        var writer = channel.Writer;
        var reader = channel.Reader;

        var producers = Task.Run(async () =>
        {
            foreach (var file in files)
            {
                var rel = Path.GetRelativePath(sourceDir, file);
                if (preservePaths.Any(p => IsPreserved(rel, p))) continue;
                if (IsLogFile(rel)) continue; // skip active log files
                await writer.WriteAsync(file, ct);
            }
            writer.Complete();
        }, ct);

        var workers = Enumerable.Range(0, parallelDegree).Select(_ => Task.Run(async () =>
        {
            while (await reader.WaitToReadAsync(ct))
            {
                while (reader.TryRead(out var file))
                {
                    var rel = Path.GetRelativePath(sourceDir, file);
                    var dest = Path.Combine(targetDir, rel);
                    Directory.CreateDirectory(Path.GetDirectoryName(dest)!);
                    await CopyFileAsync(file, dest, ct);
                }
            }
        }, ct)).ToArray();

        await Task.WhenAll(workers.Prepend(producers));
    }

    static bool IsLogDirectory(string relativePath)
        => relativePath.Replace('\\','/').Split('/').Any(seg => string.Equals(seg, "logs", StringComparison.OrdinalIgnoreCase));
    static bool IsLogFile(string relativePath)
    {
        var rp = relativePath.Replace('\\','/');
        if (rp.Contains("/logs/") || rp.StartsWith("logs/", StringComparison.OrdinalIgnoreCase))
        {
            var ext = Path.GetExtension(relativePath);
            if (string.Equals(ext, ".log", StringComparison.OrdinalIgnoreCase) || rp.EndsWith("updater.json", StringComparison.OrdinalIgnoreCase)) return true;
        }
        return false;
    }

    static async Task CopyFileAsync(string src, string dst, CancellationToken ct)
    {
        const int bufSize = 1024 * 256;
        const int maxAttempts = 3;
        for (int attempt = 1; attempt <= maxAttempts; attempt++)
        {
            try
            {
                // Try opening with FileShare.ReadWrite in case source is being written
                await using var input = new FileStream(src, FileMode.Open, FileAccess.Read, FileShare.ReadWrite, bufSize, useAsync: true);
                await using var output = new FileStream(dst, FileMode.Create, FileAccess.Write, FileShare.None, bufSize, useAsync: true);
                await input.CopyToAsync(output, bufSize, ct);
                return;
            }
            catch (IOException ioex) when (attempt < maxAttempts)
            {
                Log.Warning(ioex, "Retry {Attempt}/{Max} copying locked file {File}", attempt, maxAttempts, src);
                await Task.Delay(200 * attempt, ct);
                continue;
            }
            catch (Exception ex)
            {
                Log.Warning(ex, "Skipping file {File} due to error", src);
                return;
            }
        }
    }

    static void CleanDirectory(string dir, string[] preservePaths)
    {
        if (!Directory.Exists(dir)) return;
        foreach (var file in Directory.GetFiles(dir, "*", SearchOption.AllDirectories))
        {
            var rel = Path.GetRelativePath(dir, file);
            if (preservePaths.Any(p => IsPreserved(rel, p))) continue;
            try { File.Delete(file); } catch { }
        }
        foreach (var sub in Directory.GetDirectories(dir, "*", SearchOption.AllDirectories).OrderByDescending(p => p.Length))
        {
            var rel = Path.GetRelativePath(dir, sub);
            if (preservePaths.Any(p => IsPreserved(rel, p))) continue;
            try { if (!Directory.EnumerateFileSystemEntries(sub).Any()) Directory.Delete(sub, true); } catch { }
        }
    }

    static bool IsPreserved(string relativePath, string pattern)
    {
        var normRel = relativePath.Replace('\\', '/');
        var normPat = pattern.Replace('\\', '/');
        return normRel.StartsWith(normPat, StringComparison.OrdinalIgnoreCase);
    }
}
