using AshesMapBib.Models;
using FrontUI;
using FrontUI.Helper.MapHelper;
using Microsoft.Extensions.Logging;
using MudBlazor;
using MudBlazor.Services;
using BabylonMap.Updater;

namespace BabylonMap;

public static class MauiProgram
{
	public static MauiApp CreateMauiApp()
	{
		// Run updater pre-start
		PreStartUpdater.RunIfAvailable();

		var builder = MauiApp.CreateBuilder();
		builder
			.UseMauiApp<App>()
			.ConfigureFonts(fonts =>
			{
				fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
			});

		builder.Services.AddMauiBlazorWebView();
		builder.Services.ADDFrontUIServices();


#if DEBUG
        builder.Services.AddBlazorWebViewDeveloperTools();
		builder.Logging.AddDebug();
#endif

		return builder.Build();
	}
}
