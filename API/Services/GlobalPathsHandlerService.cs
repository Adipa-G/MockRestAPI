namespace API.Services
{
    public class GlobalPathsHandlerService
    {
        private readonly ILogger<GlobalPathsHandlerService> _logger;
        private readonly SwaggerService _swaggerService;

        public GlobalPathsHandlerService(ILogger<GlobalPathsHandlerService> logger, SwaggerService swaggerService)
        {
            _logger = logger;
            _swaggerService = swaggerService;
        }

        public async Task HandleAsync(HttpContext context)
        {
            var path = context.Request.Path;
            var pathStr = path.HasValue ? path.Value : string.Empty;
            var tokens = pathStr.TrimStart('/').Split('/');
            
            var apiName = tokens[0];
            var restOfThePath = pathStr.Replace($"{apiName}/", string.Empty);
            _logger.LogInformation("Handling the path {path}", pathStr);

            if (restOfThePath.Contains("swagger/v2/swagger.json"))
            {
                var baseUrl = $"{context.Request.Scheme}//{context.Request.Host.Value}";
                var swaggerJson = await _swaggerService.GetSwaggerJson(baseUrl, apiName);
                await WriteToResponseJsonAsync(context, swaggerJson);
            }
            else
            {
                _logger.LogWarning("Unable to handle the path {path}", pathStr);
                await WriteToResponseJsonAsync(context, "I don't know what to say.");
            }
        }

        private async Task WriteToResponseJsonAsync(HttpContext context, string json)
        {
            using var buffer = new MemoryStream();
            var stream = context.Response.Body;
            context.Response.Body = buffer;
            buffer.Seek(0, SeekOrigin.Begin);
            using (new StreamReader(buffer))
            {
                await context.Response.WriteAsync(json);
                context.Response.Body.Seek(0, SeekOrigin.Begin);

                await context.Response.Body.CopyToAsync(stream);
                context.Response.Body = stream;
            }
        }
    }
}
