using System.Collections.Concurrent;
using System.IO.Abstractions;

using API;
using API.Options;
using API.Services;

using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;

var builder = WebApplication.CreateBuilder(args);
var configurationRoot = builder.Configuration
    .AddJsonFile("appsettings.json")
    .AddJsonFile("appsettings.Development.json", true)
    .AddEnvironmentVariables()
    .Build();
// Add services to the container.

builder.Services
    .Configure<ConfigOptions>(options => configurationRoot.GetSection("Endpoints").Bind(options))
    .AddSingleton<IMemoryCache, MemoryCache>()
    .AddSingleton<IFileSystem, FileSystem>()
    .AddHttpClient()
    .AddScoped<IGlobalPathsHandlerService, GlobalPathsHandlerService>()
    .AddScoped<ISwaggerService,SwaggerService>()
    .AddScoped<ISwaggerExampleResponseBuilderService, SwaggerExampleResponseBuilderService>()
    .AddScoped<IResponseGeneratorService, ResponseGeneratorService>()
    .AddSingleton<IMockCallsLoader, MockCallsLoader>()
    .AddControllers();

var app = builder.Build();

app.UseHttpsRedirection();
app.MapControllers();
app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

var endpointOptions = app.Services.GetService<IOptions<ConfigOptions>>();
if (endpointOptions?.Value.Apis != null)
{
    var apiDefs = endpointOptions.Value.Apis;
    foreach (var apiDef in apiDefs)
    {
        app.UseSwaggerUI(c =>
        {
            c.SwaggerEndpoint($"{apiDef.ApiName}/swagger/v2/swagger.json", $"{apiDef.ApiName} API");
            c.RoutePrefix = $"{apiDef.ApiName}/swagger";
        });
    }
}
app.UseSwaggerUI(c =>
{
    c.SwaggerEndpoint($"{Constants.ManagementApiName}/swagger/v2/swagger.json", $"{Constants.ManagementApiName} API");
    c.RoutePrefix = $"{Constants.ManagementApiName}/swagger";
});

app.Use(async (context, next) =>
{
    var handlerService = context.RequestServices.GetService<IGlobalPathsHandlerService>();
    if (handlerService != null)
    {
        var handled = await handlerService.HandleAsync(context);
        if (handled)
        {
            await context.Response.CompleteAsync();
        }
        else
        {
            await next(context);
        }
    }
});

var memoryCache = app.Services.GetService<IMemoryCache>();
var idMappings = new ConcurrentDictionary<string, string>();
memoryCache?.Set(Constants.IdMappingCacheKey, idMappings);

var mockCallsLoader = app.Services.GetService<IMockCallsLoader>()!;
await mockCallsLoader.LoadMockCallsAsync();

app.Run();
