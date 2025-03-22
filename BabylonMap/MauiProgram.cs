using AshesMapBib.Models;
using Microsoft.Extensions.Logging;
using MudBlazor;
using MudBlazor.Services;

namespace BabylonMap;

public static class MauiProgram
{
	public static MauiApp CreateMauiApp()
	{
		var builder = MauiApp.CreateBuilder();
		builder
			.UseMauiApp<App>()
			.ConfigureFonts(fonts =>
			{
				fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
			});

		builder.Services.AddMauiBlazorWebView();
        builder.Services.AddMudServices();
        builder.Services.AddScoped<MapHandler>();
        builder.Services.AddScoped(sp => new HttpClient { BaseAddress = new Uri("https://your-api-url.com") });


        builder.Services.AddScoped<ResourceApiClient<Node>>(sp =>
    new ResourceApiClient<Node>(
        sp.GetRequiredService<HttpClient>(),
        sp.GetRequiredService<ILogger<ResourceApiClient<Node>>>() // Logger hinzufügen
    )
);

        builder.Services.AddScoped<ResourceApiClient<NodePosition>>(sp =>
            new ResourceApiClient<NodePosition>(
                sp.GetRequiredService<HttpClient>(),
                sp.GetRequiredService<ILogger<ResourceApiClient<NodePosition>>>() // Logger hinzufügen
            )
        );


#if DEBUG
        builder.Services.AddBlazorWebViewDeveloperTools();
		builder.Logging.AddDebug();
#endif

		return builder.Build();
	}
}
