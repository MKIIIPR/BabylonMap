using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;

public class ApiKeyAuthenticationMiddleware
{
    private readonly RequestDelegate _next;
    private readonly string _apiKey;

    public ApiKeyAuthenticationMiddleware(RequestDelegate next, IConfiguration configuration)
    {
        _next = next;
        _apiKey = configuration["ApiConfig:ApiKey"];
    }

    public async Task InvokeAsync(HttpContext context)
    {
        //app.UseMiddleware<ApiKeyAuthenticationMiddleware>(); // Middleware hinzufügen

        if (!context.Request.Headers.TryGetValue("x-api-key", out var receivedApiKey))
        {
            context.Response.StatusCode = StatusCodes.Status401Unauthorized;
            await context.Response.WriteAsync("API key is missing");
            return;
        }

        if (!string.Equals(_apiKey, receivedApiKey))
        {
            context.Response.StatusCode = StatusCodes.Status403Forbidden;
            await context.Response.WriteAsync("Invalid API key");
            return;
        }

        await _next(context);
    }
}