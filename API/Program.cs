using API.A0Need;
using API.A0Need.Models;

using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using System.Text.Json.Serialization;

var options = new WebApplicationOptions
{
    WebRootPath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot") // Setze den WebRootPath direkt
};

var builder = WebApplication.CreateBuilder(options);

// Add services to the container.
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddControllers().AddJsonOptions(options =>
{
    options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter());
    options.JsonSerializerOptions.IgnoreNullValues = true;
});

ILoggerFactory loggerFactory = LoggerFactory.Create(builder =>
{
    builder.AddConsole();
    builder.AddDebug();
});
builder.Services.AddScoped(sp => new HttpClient { BaseAddress = new Uri("http://localhost:7005") });
builder.Services.AddSingleton<IFilePathService, FilePathService>();

builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAllOrigins",
        builder =>
        {
            builder.AllowAnyOrigin()
                   .AllowAnyMethod()
                   .AllowAnyHeader();
        });
});

var connectionStrings = builder.Configuration.GetConnectionString("SC");

builder.Services.AddDbContext<SCContext>(options =>
    options.UseSqlServer(connectionStrings));

builder.Services.AddIdentity<ApplicationUser, IdentityRole>()
    .AddEntityFrameworkStores<SCContext>()
    .AddDefaultTokenProviders();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}
app.UseCors("AllowAllOrigins");
app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseAuthorization();

app.MapControllers();
Console.WriteLine($"WebRootPath: {builder.Environment.WebRootPath}");

app.Run();




//using API.A0Need;
//using API.A0Need.Models;

//using Microsoft.AspNetCore.Identity;
//using Microsoft.EntityFrameworkCore;
//using System.Text.Json.Serialization;

//var builder = WebApplication.CreateBuilder(args);

//// Add services to the container.

//builder.Services.AddControllers();
//// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
//builder.Services.AddEndpointsApiExplorer();
//builder.Services.AddSwaggerGen();
//builder.Services.AddScoped<JsonStorageService>();
//builder.Services.AddControllers().AddJsonOptions(options =>
//{
//    options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter());

//    options.JsonSerializerOptions.IgnoreNullValues = true;


//});
//ILoggerFactory loggerFactory = LoggerFactory.Create(builder =>
//{
//    builder.AddConsole();
//    builder.AddDebug();
//});
//builder.Services.AddScoped(sp => new HttpClient { BaseAddress = new Uri("http://localhost:7005") });
//builder.Services.AddSingleton<IFilePathService, FilePathService>();

////builder.Services.AddScoped<ShipViewApi>();

//builder.Services.AddCors(options =>
//{
//    options.AddPolicy("AllowAllOrigins",
//        builder =>
//        {
//            builder.AllowAnyOrigin()
//                   .AllowAnyMethod()
//                   .AllowAnyHeader();
//        });
//});

//var connectionStrings = builder.Configuration.GetConnectionString("SC");

//builder.Services.AddDbContext<SCContext>(options =>
//    options.UseSqlServer(connectionStrings));

//builder.Services.AddIdentity<ApplicationUser, IdentityRole>()
//    .AddEntityFrameworkStores<SCContext>()
//    .AddDefaultTokenProviders();
//builder.WebHost.UseWebRoot("wwwroot");
//var app = builder.Build();

//// Configure the HTTP request pipeline.
//if (app.Environment.IsDevelopment())
//{
//    app.UseSwagger();
//    app.UseSwaggerUI();
//}
//app.UseCors("AllowAllOrigins");
//app.UseHttpsRedirection();
//app.UseStaticFiles();
//app.UseAuthorization();

//app.MapControllers();
//Console.WriteLine($"WebRootPath: {builder.Environment.WebRootPath}");

//app.Run();
