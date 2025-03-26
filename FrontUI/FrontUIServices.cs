using AshesMapBib.Models;
using Blazored.LocalStorage;
using FrontUI.FtpService;
using FrontUI.Helper.MapHelper;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using MudBlazor.Services;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.NetworkInformation;
using System.Text;
using System.Threading.Tasks;

namespace FrontUI
{
    public static class ServicesConfiguration
    {
        public static void ADDFrontUIServices(this IServiceCollection services)
        {
            //services.AddMudBlazorSnackbar();
            //services.AddMudBlazorDialog();

            services.AddMudServices();
            services.AddScoped<MapHandler>();
            services.AddScoped(sp => new HttpClient { BaseAddress = new Uri("https://your-api-url.com") });
            services.AddScoped<FTPConnection>();   

            services.AddScoped<ResourceApiClient<Node>>(sp =>
        new ResourceApiClient<Node>(
            sp.GetRequiredService<HttpClient>(),
            sp.GetRequiredService<ILogger<ResourceApiClient<Node>>>() // Logger hinzufügen
        )
    );

            services.AddScoped<ResourceApiClient<NodePosition>>(sp =>
                new ResourceApiClient<NodePosition>(
                    sp.GetRequiredService<HttpClient>(),
                    sp.GetRequiredService<ILogger<ResourceApiClient<NodePosition>>>() // Logger hinzufügen
                )
            );


        }
    }

}
