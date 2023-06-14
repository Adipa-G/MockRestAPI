using Newtonsoft.Json;

namespace API.Services
{
    public class GlobalPathsHandlerService : IGlobalPathsHandlerService
    {
        private readonly ILogger<GlobalPathsHandlerService> _logger;
        private readonly ISwaggerService _swaggerService;
        private readonly ISwaggerExampleResponseBuilderService _swaggerExampleResponseBuilderService;

        public GlobalPathsHandlerService(ILogger<GlobalPathsHandlerService> logger,
            ISwaggerService swaggerService,
            ISwaggerExampleResponseBuilderService swaggerExampleResponseBuilderService)
        {
            _logger = logger;
            _swaggerService = swaggerService;
            _swaggerExampleResponseBuilderService = swaggerExampleResponseBuilderService;
        }

        public async Task HandleAsync(HttpContext context)
        {
            var baseUrl = $"{context.Request.Scheme}://{context.Request.Host.Value}";
            var path = context.Request.Path;
            var pathStr = path.HasValue ? path.Value : string.Empty;
            if (pathStr.Contains(baseUrl))
            {
                pathStr = pathStr.Split(baseUrl)[1];
            }
            var tokens = pathStr.TrimStart('/').Split('/');
            var apiName = tokens[0];
            var restOfThePath = pathStr.Replace($"{apiName}/", string.Empty);
           

            _logger.LogInformation("Handling the path {path}", pathStr);
            if (restOfThePath.Contains("swagger/v2/swagger.json"))
            {
                var swaggerJson = await _swaggerService.GetSwaggerJsonAsync(baseUrl, apiName);
                await WriteToResponseJsonAsync(context, "200", swaggerJson);
            }
            else
            {
                var response =
                    await _swaggerExampleResponseBuilderService.GetResponse(baseUrl, apiName, restOfThePath,
                        context.Request);
                if (response.Equals(default(KeyValuePair<string, string>)))
                {
                    _logger.LogWarning("Unable to handle the path {path}", pathStr);
                    var noIdeaMessage =
                        new KeyValuePair<string, string>("message", "I have never met this man in my life.");
                    await WriteToResponseJsonAsync(context, "400", JsonConvert.SerializeObject(noIdeaMessage));
                }
                else
                {
                    await WriteToResponseJsonAsync(context, response.Key, response.Value);
                }
            }
        }

        private async Task WriteToResponseJsonAsync(HttpContext context, string responseCodeStr, string? responseJson)
        {
            int.TryParse(responseCodeStr, out var responseCode  );

            using var buffer = new MemoryStream();
            var stream = context.Response.Body;
            context.Response.Body = buffer;
            context.Response.Headers.ContentType = "application/json";
            context.Response.StatusCode = responseCode == 0 ? 200 : responseCode;
            buffer.Seek(0, SeekOrigin.Begin);
            if (responseJson != null)
            {
                using (new StreamReader(buffer))
                {
                    await context.Response.WriteAsync(responseJson);
                    context.Response.Body.Seek(0, SeekOrigin.Begin);

                    await context.Response.Body.CopyToAsync(stream);
                    context.Response.Body = stream;
                }
            }
        }
    }
}
