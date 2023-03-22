using Blazored.Toast;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.Extensions.Configuration;
using RestApiNExApiV6.BlazorServerTestApp.Data;
using RestApiNExApiV6.BlazorServerTestApp.Services;
using System.Configuration;
using System.Text.Encodings.Web;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddRazorPages();
builder.Services.AddServerSideBlazor();
builder.Services.AddSingleton<WeatherForecastService>();
builder.Services.AddHttpClient<IRestApiService, RestApiService>();
builder.Services.AddSingleton<RestApiService>();
builder.Services.AddHttpContextAccessor();
builder.Services.Configure<ServiceOptions>(builder.Configuration.GetSection(nameof(ServiceOptions)));
builder.Services.AddControllers(options =>
{
}).AddJsonOptions(options =>
{
    options.JsonSerializerOptions.PropertyNameCaseInsensitive = true;
    options.JsonSerializerOptions.Encoder = JavaScriptEncoder.Default;
    options.JsonSerializerOptions.WriteIndented = true;
});

builder.Services.AddBlazoredToast();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error");
}


app.UseStaticFiles();

app.UseRouting();

app.MapBlazorHub();
app.MapFallbackToPage("/_Host");

app.Run();
