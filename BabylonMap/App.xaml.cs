using System.Reflection;
namespace BabylonMap;

public partial class App : Application
{
	public App()
	{
		InitializeComponent();
	}

	protected override Window CreateWindow(IActivationState? activationState)
	{
		var version = GetAppVersion();
		return new Window(new MainPage()) { Title = $"BabylonMap ({version})" };
	}

	private static string GetAppVersion()
	{
		// 1) Versuche, die SemVer aus AssemblyInformationalVersion zu lesen (entspricht meist <Version> im csproj)
		var infoVer = typeof(App).Assembly
			.GetCustomAttribute<AssemblyInformationalVersionAttribute>()?
			.InformationalVersion;
		if (!string.IsNullOrWhiteSpace(infoVer))
		{
			return NormalizeSemVer(infoVer!);
		}

		try
		{
#if ANDROID || IOS || MACCATALYST || WINDOWS
			// 2) Sichtbare Plattformversion (kann auf Windows unverpackt 4-teilige Assemblyversion liefern)
			var v = Microsoft.Maui.ApplicationModel.AppInfo.Current?.VersionString;
			if (!string.IsNullOrWhiteSpace(v)) return NormalizeSemVer(v!);
#endif
		}
		catch { /* ignore and fallback */ }

		// 3) Fallback: Assembly-Version (Major.Minor.Build)
		return typeof(App).Assembly.GetName().Version?.ToString(3) ?? "0.0.0";
	}

	private static string NormalizeSemVer(string v)
	{
		// Reduziere auf x.y.z (erste 3 Teile)
		var parts = v.Split('+')[0].Split('-')[0].Split('.');
		if (parts.Length >= 3)
			return string.Join('.', parts[0], parts[1], parts[2]);
		return v;
	}
}
