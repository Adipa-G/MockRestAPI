using API.Options;
using API.Services;
using Microsoft.Extensions.Options;

var builder = WebApplication.CreateBuilder(args);
var configurationRoot = builder.Configuration.AddJsonFile("appsettings.json").Build();
// Add services to the container.

builder.Services
    .Configure<EndpointOptions>(options => configurationRoot.GetSection("Endpoints").Bind(options))
    .AddScoped<GlobalPathsHandlerService>()
    .AddScoped<SwaggerService>();

var app = builder.Build();

// Configure the HTTP request pipeline.
app.UseHttpsRedirection();

var endpointOptions = app.Services.GetService<IOptions<EndpointOptions>>();
if (endpointOptions?.Value != null)
{
    var apiDefs = endpointOptions.Value.Apis;
    foreach (var apiDef in apiDefs)
    {
        app.UseSwaggerUI(c =>
        {
            c.SwaggerEndpoint($"{apiDef.ApiName}/swagger/v2/swagger.json", $"API {apiDef.ApiName}");
            c.RoutePrefix = $"{apiDef.ApiName}/swagger";
        });
    }
}


app.Run(async (context) =>
{
    var handlerService = context.RequestServices.GetService<GlobalPathsHandlerService>();
    if (handlerService != null)
    {
        await handlerService.HandleAsync(context);
    }
    await context.Response.CompleteAsync();
});

app.Run();
