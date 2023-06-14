using System.Dynamic;

using Microsoft.OpenApi.Any;
using Microsoft.OpenApi.Models;

using Newtonsoft.Json;

namespace API.Services
{
    public class SwaggerExampleResponseBuilderService : ISwaggerExampleResponseBuilderService
    {
        private readonly ILogger<SwaggerExampleResponseBuilderService> _logger;
        private readonly ISwaggerService _swaggerService;

        public SwaggerExampleResponseBuilderService(ILogger<SwaggerExampleResponseBuilderService> logger, 
            ISwaggerService swaggerService)
        {
            _logger = logger;
            _swaggerService = swaggerService;
        }

        public async Task<KeyValuePair<string,string?>> GetResponse(string baseUrl,string apiName, string requestPath, HttpRequest request)
        {
            var openApiSpec = await _swaggerService.GetOpenApiDocumentAsync(baseUrl, apiName);
            if (openApiSpec == null)
            {
                _logger.LogError("Null open api spec for API : [{apiName}]. Unable to process.", apiName);
                return default;
            }
                

            var apiPaths = openApiSpec.Paths.Keys.ToList();
            var matchingApiPath = FindMatchingPath(apiPaths, requestPath);
            var path = openApiSpec.Paths.FirstOrDefault(p => p.Key == matchingApiPath);
            if (path.Equals(default(KeyValuePair<string, OpenApiPathItem>)))
            {
                _logger.LogError("Unable to find matching path for API : [{apiName}] for Path : [{path}]. Unable to process.", apiName, requestPath);
                return default;
            }
                

            Enum.TryParse(typeof(OperationType), request.Method, true, out var operationTypeObj);
            var operationType = (OperationType)operationTypeObj!;
            var operation = path.Value.Operations.FirstOrDefault(op => op.Key == operationType);
            if (operation.Equals(default(KeyValuePair<OperationType, OpenApiOperation>)))
            {
                _logger.LogError("Unable to find matching method for API : [{apiName}] for Method : [{method}]. Unable to process.", apiName, request.Method);
                return default;
            }
                

            var response = operation.Value.Responses.FirstOrDefault(r => r.Key == "200");
            if (response.Equals(default(KeyValuePair<string, OpenApiResponse>)))
                response = operation.Value.Responses.FirstOrDefault();
            if (response.Equals(default(KeyValuePair<string, OpenApiResponse>)))
            {
                _logger.LogError("No responses are defined for the API : [{apiName}] for Path : [{path}]. Unable to process.", apiName, requestPath);
                return default;
            }
               
            var content = response.Value.Content.FirstOrDefault(c => c.Key == request.ContentType);
            if (content.Equals(default(KeyValuePair<string,OpenApiMediaType>)))
                content = response.Value.Content.FirstOrDefault();
            if (content.Equals(default(KeyValuePair<string, OpenApiMediaType>)))
            {
                _logger.LogError("No contents are defined for the API : [{apiName}] for Path : [{path}]. Unable to process.", apiName, requestPath);
                return default;
            }

            var responseCode = response.Key;
            var example = GenerateExample(content.Value, requestPath);
            var exampleJson = JsonConvert.SerializeObject(example);
            return new KeyValuePair<string, string?>(responseCode, exampleJson);
        }

        private dynamic? GenerateExample(OpenApiMediaType mediaType, string requestPath)
        {
            IOpenApiAny? example = null;
            var matchedExamplePath = false;
            var examples = mediaType.Examples;
            if (examples.Any())
            {
                var match = examples.Keys.FirstOrDefault(requestPath.Contains);
                matchedExamplePath = !string.IsNullOrWhiteSpace(match);
                example = matchedExamplePath ? examples[match!].Value : examples.Values.First().Value;
            }
            example = !matchedExamplePath && mediaType.Example != null ? mediaType.Example : example;

            if (example != null)
            {
                return GenerateForType(example);
            }

            var schema = mediaType.Schema;
            return GenerateFromSchema(schema);
        }

        private string FindMatchingPath(IList<string> apiPaths, string requestPath)
        {
            if (apiPaths.Contains(requestPath))
                return requestPath;

            var requestPathTokens = requestPath.Split('/', StringSplitOptions.RemoveEmptyEntries);
            foreach (string apiPath in apiPaths)
            {
                var apiPathTokens = apiPath.Split('/', StringSplitOptions.RemoveEmptyEntries);
                if (apiPathTokens.Length == requestPathTokens.Length)
                {
                    bool isMatch = true;
                    for (int index = 0; index < apiPathTokens.Length; index++)
                    {
                        string apiPathToken = apiPathTokens[index];
                        isMatch = apiPathToken == requestPathTokens[index] || apiPathToken.StartsWith("{");
                    }
                    if (isMatch) return apiPath;
                }
            }

            return string.Empty;
        }
        
        private dynamic? GenerateFromSchema(OpenApiSchema? schema)
        {
            if (schema == null || schema.Properties == null)
                return null;

            var result = new ExpandoObject() as IDictionary<string, object>;
            var props = schema.Properties;
            foreach (var prop in props)
            {
                var propSchema = prop.Value;
                object? value = propSchema.Properties.Count > 0
                    ? GenerateFromSchema(propSchema)
                    : GenerateForType(propSchema.Example);

                if (value != null)
                {
                    result.Add(prop.Key, value);
                }
            }
            return result;
        }

        private object? GenerateForType(IOpenApiAny? openApiAny)
        {
            object? value = null;
            if (openApiAny == null)
            {
                return null;
            }
            if (openApiAny.AnyType == AnyType.Object)
            {
                var obj = new ExpandoObject() as IDictionary<string, object?>;
                var openApiObject = (OpenApiObject)openApiAny;
                foreach (var keyValuePair in openApiObject)
                {
                    obj.Add(keyValuePair.Key, GenerateForType(keyValuePair.Value));
                }
                value = obj;
            }
            else if (openApiAny.AnyType == AnyType.Array)
            {
                var array = (OpenApiArray)openApiAny;
                var list = new List<object?>();
                foreach (var item in array)
                {
                    list.Add(GenerateForType(item));
                }
                value = list.ToArray();
            }
            else if (openApiAny.AnyType == AnyType.Primitive)
            {
                var primitive = (IOpenApiPrimitive)openApiAny;
                switch (primitive.PrimitiveType)
                {
                    case PrimitiveType.Integer:
                        value = ((OpenApiPrimitive<int>)openApiAny).Value;
                        break;
                    case PrimitiveType.Long:
                        value = ((OpenApiPrimitive<long>)openApiAny).Value;
                        break;
                    case PrimitiveType.Float:
                        value = ((OpenApiPrimitive<float>)openApiAny).Value;
                        break;
                    case PrimitiveType.Double:
                        value = ((OpenApiPrimitive<double>)openApiAny).Value;
                        break;
                    case PrimitiveType.String:
                        value = ((OpenApiPrimitive<string>)openApiAny).Value;
                        break;
                    case PrimitiveType.Byte:
                        value = ((OpenApiPrimitive<byte[]>)openApiAny).Value;
                        break;
                    case PrimitiveType.Binary:
                        value = ((OpenApiPrimitive<byte[]>)openApiAny).Value;
                        break;
                    case PrimitiveType.Boolean:
                        value = ((OpenApiPrimitive<bool>)openApiAny).Value;
                        break;
                    case PrimitiveType.Date:
                        value = ((OpenApiPrimitive<DateTime>)openApiAny).Value;
                        break;
                    case PrimitiveType.DateTime:
                        value = ((OpenApiPrimitive<DateTimeOffset>)openApiAny).Value;
                        break;
                    case PrimitiveType.Password:
                        value = ((OpenApiPrimitive<string>)openApiAny).Value;
                        break;
                }
            }
            return value;
        }
    }
}
