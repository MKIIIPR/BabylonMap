using System.Diagnostics;
using System.Text.Json;
using System.Runtime.InteropServices;

namespace BabylonMap.Updater;

public static class PreStartUpdater
{
    // Launches updater check first; only if update available ask and then run updater detached. Otherwise continue startup.
    public static void RunIfAvailable()
    {
        try
        {
            var baseDirUpdater = Path.Combine(AppContext.BaseDirectory, "Updater");
#if WINDOWS
            var exeName = "Updater.exe";
            var appExe = Path.Combine(AppContext.BaseDirectory, "BabylonMap.exe");
#else
            var exeName = "Updater"; // for mac/linux publish
            var appExe = Path.Combine(AppContext.BaseDirectory, "BabylonMap");
#endif
            var updaterPath = Path.Combine(baseDirUpdater, exeName);
            if (!File.Exists(updaterPath)) return; // no updater present

            // 1) Run quick check to see if update is available
            var cfgPath = Path.Combine(baseDirUpdater, "appsettings.json");
            var checkArgs = new List<string> { "--json-logs", "--check-only" };
            if (File.Exists(cfgPath)) { checkArgs.Add("--config"); checkArgs.Add($"\"{cfgPath}\""); }

            var checkExit = StartUpdaterAndWait(updaterPath, string.Join(' ', checkArgs));
            if (checkExit != 11) return; // no update

            // 2) Ask user for consent
            if (!AskUserToRunUpdater()) return;

            // 3) Start updater to perform update (detached) and exit app immediately
            var runArgs = new List<string> { "--json-logs" };
            if (File.Exists(cfgPath)) { runArgs.Add("--config"); runArgs.Add($"\"{cfgPath}\""); }
            if (File.Exists(appExe)) { runArgs.Add("--relaunch"); runArgs.Add($"\"{appExe}\""); }
            StartUpdaterDetached(updaterPath, string.Join(' ', runArgs));

            try { Environment.Exit(0); } catch { }
        }
        catch
        {
            // swallow exceptions; if anything fails just continue application startup
        }
    }

    private static bool AskUserToRunUpdater()
    {
        try
        {
#if WINDOWS
            const uint MB_YESNO = 0x00000004;
            const uint MB_ICONQUESTION = 0x00000020;
            const int IDYES = 6;
            int result = MessageBoxW(IntPtr.Zero, "Ein Update ist verfügbar. Updater jetzt ausführen?", "BabylonMap", MB_YESNO | MB_ICONQUESTION);
            return result == IDYES;
#else
            try
            {
                Console.Write("Ein Update ist verfügbar. Updater jetzt ausführen? [y/N]: ");
                var input = Console.ReadLine();
                if (string.IsNullOrWhiteSpace(input)) return false;
                input = input.Trim().ToLowerInvariant();
                return input == "y" || input == "yes" || input == "j" || input == "ja";
            }
            catch { return false; }
#endif
        }
        catch { return false; }
    }

#if WINDOWS
    [DllImport("user32.dll", CharSet = CharSet.Unicode, SetLastError = true, EntryPoint = "MessageBoxW")]
    private static extern int MessageBoxW(IntPtr hWnd, string text, string caption, uint type);
#endif

    private static int StartUpdaterAndWait(string updaterPath, string args)
    {
        try
        {
            var psi = new ProcessStartInfo
            {
                FileName = updaterPath,
                Arguments = args,
                UseShellExecute = false,
                CreateNoWindow = true,
                WorkingDirectory = Path.GetDirectoryName(updaterPath) ?? AppContext.BaseDirectory,
                RedirectStandardOutput = false,
                RedirectStandardError = false
            };
            using var proc = Process.Start(psi);
            if (proc == null) return -1;
            try { proc.WaitForExit(); } catch { }
            return proc.ExitCode;
        }
        catch { return -1; }
    }

    private static void StartUpdaterDetached(string updaterPath, string args)
    {
        try
        {
#if WINDOWS
            // Properly quote entire command sequence. /c executes then closes after timeout.
            // We wrap whole chain in one quoted argument after /c to avoid breaking on embedded quotes.
            var sequence = $"\"{updaterPath}\" {args} & echo. & echo Update Vorgang beendet. Fenster schliesst in 10s... & timeout /t 10 >nul";
            var psi = new ProcessStartInfo
            {
                FileName = "cmd.exe",
                Arguments = $"/c \"{sequence}\"",
                UseShellExecute = true,
                WorkingDirectory = Path.GetDirectoryName(updaterPath) ?? AppContext.BaseDirectory,
                WindowStyle = ProcessWindowStyle.Normal
            };
#else
            var psi = new ProcessStartInfo
            {
                FileName = updaterPath,
                Arguments = args,
                UseShellExecute = true,
                WorkingDirectory = Path.GetDirectoryName(updaterPath) ?? AppContext.BaseDirectory
            };
#endif
            Process.Start(psi); // no wait
        }
        catch { /* ignore */ }
    }
}
