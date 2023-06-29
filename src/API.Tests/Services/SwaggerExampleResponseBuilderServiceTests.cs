using System.Text;

using API.Services;
using API.Tests.Helpers;

using FluentAssertions;

using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Microsoft.OpenApi.Any;
using Microsoft.OpenApi.Models;

using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

using NSubstitute;

using Xunit;

namespace API.Tests.Services
{
    public class SwaggerExampleResponseBuilderServiceTests
    {
        private readonly LoggerMock<SwaggerExampleResponseBuilderService> _logger;
        private readonly ISwaggerService _swaggerService;

        private const string BaseUrl = "https://localhost:3000";
        private const string ApiName = "petstore";
        private const string Path = "/pet";
        private const string Method = "post";

        public SwaggerExampleResponseBuilderServiceTests()
        {
            _logger = new LoggerMock<SwaggerExampleResponseBuilderService>();
            _swaggerService = Substitute.For<ISwaggerService>();
        }

        [Fact]
        public async Task GivenNullSpec_WhenGetResponse_ThenReturnNull()
        {
            //Arrange
            _swaggerService.GetOpenApiDocumentAsync(BaseUrl, ApiName)
                .Returns(Task.FromResult((OpenApiDocument?)null));

            //Act
            var sut = CreateSut();

            //Assert
            var result = await sut.GetResponse(BaseUrl, ApiName, Path, CreateRequest());
            result.Should().Be(default(KeyValuePair<string,string>));
            _logger.ReceivedOnce(LogLevel.Error, "Null open api spec for API");
        }

        [Fact]
        public async Task GivenNoMatchingPath_WhenGetResponse_ThenReturnNull()
        {
            //Arrange
            var openApiDoc = CreateDocument();
            AddPath(openApiDoc, Path);

            _swaggerService.GetOpenApiDocumentAsync(BaseUrl, ApiName)
                .Returns(Task.FromResult((OpenApiDocument?)openApiDoc));

            //Act
            var sut = CreateSut();

            //Assert
            var result = await sut.GetResponse(BaseUrl, ApiName, "/something-else", CreateRequest());
            result.Should().Be(default(KeyValuePair<string, string>));
            _logger.ReceivedOnce(LogLevel.Error, "Unable to find matching path for API");
        }

        [Fact]
        public async Task GivenNoMatchingOperation_WhenGetResponse_ThenReturnNull()
        {
            //Arrange
            var openApiDoc = CreateDocument();
            AddPath(openApiDoc, Path);

            _swaggerService.GetOpenApiDocumentAsync(BaseUrl, ApiName)
                .Returns(Task.FromResult((OpenApiDocument?)openApiDoc));

            //Act
            var sut = CreateSut();

            //Assert
            var result = await sut.GetResponse(BaseUrl, ApiName, Path, CreateRequest());
            result.Should().Be(default(KeyValuePair<string, string>));
            _logger.ReceivedOnce(LogLevel.Error, "Unable to find matching method for API");
        }

        [Fact]
        public async Task GivenNoMatchingResponse_WhenGetResponse_ThenReturnNull()
        {
            //Arrange
            var openApiDoc = CreateDocument();
            var path = AddPath(openApiDoc, Path);
            AddOperation(path, OperationType.Post);

            _swaggerService.GetOpenApiDocumentAsync(BaseUrl, ApiName)
                .Returns(Task.FromResult((OpenApiDocument?)openApiDoc));

            //Act
            var sut = CreateSut();

            //Assert
            var result = await sut.GetResponse(BaseUrl, ApiName, Path, CreateRequest());
            result.Should().Be(default(KeyValuePair<string, string>));
            _logger.ReceivedOnce(LogLevel.Error, "No responses are defined for the API");
        }

        [Fact]
        public async Task GivenNoMatchingContent_WhenGetResponse_ThenReturnNull()
        {
            //Arrange
            var openApiDoc = CreateDocument();
            var path = AddPath(openApiDoc, Path);
            var operation = AddOperation(path, OperationType.Post);
            AddResponse(operation,"200");
            _swaggerService.GetOpenApiDocumentAsync(BaseUrl, ApiName)
                .Returns(Task.FromResult((OpenApiDocument?)openApiDoc));

            //Act
            var sut = CreateSut();

            //Assert
            var result = await sut.GetResponse(BaseUrl, ApiName, Path, CreateRequest());
            result.Should().Be(default(KeyValuePair<string, string>));
            _logger.ReceivedOnce(LogLevel.Error, "No contents are defined for the API");
        }

        [Fact]
        public async Task GivenFullMatch_WhenGetResponse_ThenReturnCorrectExample()
        {
            //Arrange
            var openApiDoc = CreateDocument();
            var path = AddPath(openApiDoc, Path);
            var operation = AddOperation(path, OperationType.Post);
            var response = AddResponse(operation, "200");
            var mediaType = AddContent(response, "application/json");
            AddProperty(mediaType, "id", new OpenApiInteger(10));
            _swaggerService.GetOpenApiDocumentAsync(BaseUrl, ApiName)
                .Returns(Task.FromResult((OpenApiDocument?)openApiDoc));

            //Act
            var sut = CreateSut();

            //Assert
            var result = await sut.GetResponse(BaseUrl, ApiName, Path, CreateRequest());
            result.Should().NotBe(default(KeyValuePair<string, string>));
            result.Value.Should().Be(JsonConvert.SerializeObject(new { id = 10 }));
        }

        [Fact]
        public async Task GivenPathWithParameterMatch_WhenGetResponse_ThenReturnCorrectExample()
        {
            //Arrange
            var openApiDoc = CreateDocument();
            var path = AddPath(openApiDoc, "pet/{id}");
            var operation = AddOperation(path, OperationType.Get);
            var response = AddResponse(operation, "200");
            var mediaType = AddContent(response, "application/json");
            AddProperty(mediaType, "id", new OpenApiInteger(10));
            _swaggerService.GetOpenApiDocumentAsync(BaseUrl, ApiName)
                .Returns(Task.FromResult((OpenApiDocument?)openApiDoc));

            //Act
            var sut = CreateSut();

            //Assert
            var result = await sut.GetResponse(BaseUrl, ApiName, "pet/23", CreateRequest("get"));
            result.Should().NotBe(default(KeyValuePair<string, string>));
            result.Value.Should().Be(JsonConvert.SerializeObject(new { id = 10 }));
        }

        [Fact]
        public async Task GivenMultipleExamplesDefinedAndDefaultExampleDefined_WhenGetResponseWithMatchingPath_ThenReturnCorrectExample()
        {
            //Arrange
            var openApiDoc = CreateDocument();
            var path = AddPath(openApiDoc, "pet/{id}");
            var operation = AddOperation(path, OperationType.Get);
            var response = AddResponse(operation, "200");
            var mediaType = AddContent(response, "application/json");
            AddExample(mediaType, "53",
                new OpenApiObject() { { "id", new OpenApiInteger(10) }, { "name", new OpenApiString("no no baw baw") } });
            AddExample(mediaType, "54",
                new OpenApiObject() { { "id", new OpenApiInteger(11) }, { "name", new OpenApiString("baw baw") } });
            SetExample(mediaType,
                new OpenApiObject() { { "id", new OpenApiInteger(12) }, { "name", new OpenApiString("no baw baw") } });
            _swaggerService.GetOpenApiDocumentAsync(BaseUrl, ApiName)
                .Returns(Task.FromResult((OpenApiDocument?)openApiDoc));

            //Act
            var sut = CreateSut();

            //Assert
            var result = await sut.GetResponse(BaseUrl, ApiName, "pet/54", CreateRequest("get"));
            result.Should().NotBe(default(KeyValuePair<string, string>));
            result.Value.Should().Be(JsonConvert.SerializeObject(new { id = 11, name = "baw baw" }));
        }

        [Fact]
        public async Task GivenMultipleExamplesDefined_WhenGetResponseWithNoMatchingPath_ThenReturnFirstExample()
        {
            //Arrange
            var openApiDoc = CreateDocument();
            var path = AddPath(openApiDoc, "pet/{id}");
            var operation = AddOperation(path, OperationType.Get);
            var response = AddResponse(operation, "200");
            var mediaType = AddContent(response, "application/json");
            AddExample(mediaType, "53",
                new OpenApiObject() { { "id", new OpenApiInteger(10) }, { "name", new OpenApiString("no baw baw") } });
            AddExample(mediaType, "54",
                new OpenApiObject() { { "id", new OpenApiInteger(11) }, { "name", new OpenApiString("baw baw") } });
            _swaggerService.GetOpenApiDocumentAsync(BaseUrl, ApiName)
                .Returns(Task.FromResult((OpenApiDocument?)openApiDoc));

            //Act
            var sut = CreateSut();

            //Assert
            var result = await sut.GetResponse(BaseUrl, ApiName, "pet/65", CreateRequest("get"));
            result.Should().NotBe(default(KeyValuePair<string, string>));
            result.Value.Should().Be(JsonConvert.SerializeObject(new { id = 10, name = "no baw baw" }));
        }

        [Fact]
        public async Task GivenMultipleExamplesDefinedAndDefaultExampleDefined_WhenGetResponseWithNoMatchingPath_ThenReturnFirstExample()
        {
            //Arrange
            var openApiDoc = CreateDocument();
            var path = AddPath(openApiDoc, "pet/{id}");
            var operation = AddOperation(path, OperationType.Get);
            var response = AddResponse(operation, "200");
            var mediaType = AddContent(response, "application/json");
            AddExample(mediaType, "53",
                new OpenApiObject() { { "id", new OpenApiInteger(10) }, { "name", new OpenApiString("no no baw baw") } });
            AddExample(mediaType, "54",
                new OpenApiObject() { { "id", new OpenApiInteger(11) }, { "name", new OpenApiString("no baw baw") } });
            SetExample(mediaType,
                new OpenApiObject() { { "id", new OpenApiInteger(12) }, { "name", new OpenApiString("baw baw") } });
            _swaggerService.GetOpenApiDocumentAsync(BaseUrl, ApiName)
                .Returns(Task.FromResult((OpenApiDocument?)openApiDoc));

            //Act
            var sut = CreateSut();

            //Assert
            var result = await sut.GetResponse(BaseUrl, ApiName, "pet/65", CreateRequest("get"));
            result.Should().NotBe(default(KeyValuePair<string, string>));
            result.Value.Should().Be(JsonConvert.SerializeObject(new { id = 12, name = "baw baw" }));
        }

        [Fact]
        public async Task GivenExampleWithAllPrimitiveTypes_WhenGetResponse_ThenGenerateResponse()
        {
            //Arrange
            var intValue = 10;
            var longValue = 1032323L;
            var floatValue = 1.33324232f;
            var doubleValue = 1322.33d;
            var stringValue = "abc";
            var byteValue = new byte[] { 43 };
            var binaryValue = Encoding.UTF8.GetBytes("abc");
            var boolValue = true;
            var dateValue = DateTime.MaxValue.ToUniversalTime();
            var dateTimeValue = DateTimeOffset.MaxValue.ToUniversalTime();
            var passwordValue = "super-secure-pwd";

            var openApiDoc = CreateDocument();
            var path = AddPath(openApiDoc, Method);
            var operation = AddOperation(path, OperationType.Post);
            var response = AddResponse(operation, "200");
            var mediaType = AddContent(response, "application/json");
            AddProperty(mediaType, nameof(intValue), new OpenApiInteger(intValue));
            AddProperty(mediaType, nameof(longValue), new OpenApiLong(longValue));
            AddProperty(mediaType, nameof(floatValue), new OpenApiFloat(floatValue));
            AddProperty(mediaType, nameof(doubleValue), new OpenApiDouble(doubleValue));
            AddProperty(mediaType, nameof(stringValue), new OpenApiString(stringValue));
            AddProperty(mediaType, nameof(byteValue), new OpenApiByte(byteValue));
            AddProperty(mediaType, nameof(binaryValue), new OpenApiBinary(binaryValue));
            AddProperty(mediaType, nameof(boolValue), new OpenApiBoolean(boolValue));
            AddProperty(mediaType, nameof(dateValue), new OpenApiDateTime(dateValue));
            AddProperty(mediaType, nameof(dateTimeValue), new OpenApiDateTime(dateTimeValue));
            AddProperty(mediaType, nameof(passwordValue), new OpenApiPassword(passwordValue));
            _swaggerService.GetOpenApiDocumentAsync(BaseUrl, ApiName)
                .Returns(Task.FromResult((OpenApiDocument?)openApiDoc));

            //Act
            var sut = CreateSut();

            //Assert
            var result = await sut.GetResponse(BaseUrl, ApiName, Method, CreateRequest());
            result.Should().NotBe(default(KeyValuePair<string, string>));
            result.Value.Should().Be(JsonConvert.SerializeObject(new
            {
                intValue,
                longValue,
                floatValue,
                doubleValue,
                stringValue,
                byteValue,
                binaryValue,
                boolValue,
                dateValue,
                dateTimeValue,
                passwordValue
            }, new IsoDateTimeConverter() { DateTimeFormat = "yyyy-MM-ddTHH:mm:ss.fffffff+00:00" }));
        }

        [Fact]
        public async Task GivenExampleWithNestedObjects_WhenGetResponse_ThenGenerateResponse()
        {
            //Arrange
            var openApiDoc = CreateDocument();
            var path = AddPath(openApiDoc, Method);
            var operation = AddOperation(path, OperationType.Post);
            var response = AddResponse(operation, "200");
            var mediaType = AddContent(response, "application/json");
            AddProperty(mediaType, "id", new OpenApiInteger(10));
            AddProperty(mediaType, "child", new OpenApiObject()
            {
                {"id", new OpenApiInteger(20)}
            });
            _swaggerService.GetOpenApiDocumentAsync(BaseUrl, ApiName)
                .Returns(Task.FromResult((OpenApiDocument?)openApiDoc));

            //Act
            var sut = CreateSut();

            //Assert
            var result = await sut.GetResponse(BaseUrl, ApiName, Method, CreateRequest());
            result.Should().NotBe(default(KeyValuePair<string, string>));
            result.Value.Should().Be(JsonConvert.SerializeObject(new
            {
                id = 10,
                child = new
                {
                    id = 20
                }
            }));
        }

        [Fact]
        public async Task GivenExampleWithArrayOfObjects_WhenGetResponse_ThenGenerateResponse()
        {
            //Arrange
            var openApiDoc = CreateDocument();
            var path = AddPath(openApiDoc, Method);
            var operation = AddOperation(path, OperationType.Post);
            var response = AddResponse(operation, "200");
            var mediaType = AddContent(response, "application/json");
            AddProperty(mediaType, "id", new OpenApiInteger(10));
            AddProperty(mediaType, "child", new OpenApiArray()
            {
                new OpenApiInteger(12),
                new OpenApiInteger(15)
            });
            _swaggerService.GetOpenApiDocumentAsync(BaseUrl, ApiName)
                .Returns(Task.FromResult((OpenApiDocument?)openApiDoc));

            //Act
            var sut = CreateSut();

            //Assert
            var result = await sut.GetResponse(BaseUrl, ApiName, Method, CreateRequest());
            result.Should().NotBe(default(KeyValuePair<string, string>));
            result.Value.Should().Be(JsonConvert.SerializeObject(new
            {
                id = 10,
                child = new[]{12,15}
            }));
        }

        [Fact]
        public async Task GivenExampleWithNestedSchema_WhenGetResponse_ThenGenerateResponse()
        {
            //Arrange
            var openApiDoc = CreateDocument();
            var path = AddPath(openApiDoc, Method);
            var operation = AddOperation(path, OperationType.Post);
            var response = AddResponse(operation, "200");
            var mediaType = AddContent(response, "application/json");
            AddProperty(mediaType, "id", new OpenApiInteger(10));
            AddProperty(mediaType, "child", new OpenApiSchema()
            {
                Properties = new Dictionary<string, OpenApiSchema>()
                {
                    {"id", new OpenApiSchema(){Example = new OpenApiInteger(20)}},
                    {"name", new OpenApiSchema(){Example = new OpenApiString("test")}}
                }
            });
            _swaggerService.GetOpenApiDocumentAsync(BaseUrl, ApiName)
                .Returns(Task.FromResult((OpenApiDocument?)openApiDoc));

            //Act
            var sut = CreateSut();

            //Assert
            var result = await sut.GetResponse(BaseUrl, ApiName, Method, CreateRequest());
            result.Should().NotBe(default(KeyValuePair<string, string>));
            result.Value.Should().Be(JsonConvert.SerializeObject(new
            {
                id = 10, 
                child = new
                {
                    id = 20, 
                    name = "test"
                }
            }));
        }

        private OpenApiDocument CreateDocument()
        {
            return new OpenApiDocument() { Paths = new OpenApiPaths() };
        }

        private OpenApiPathItem AddPath(OpenApiDocument document, string path)
        {
            var pathItem = new OpenApiPathItem();
            document.Paths.Add(path, pathItem);
            return pathItem;
        }

        private OpenApiOperation AddOperation(OpenApiPathItem path, OperationType operationType)
        {
            var operation = new OpenApiOperation();
            path.Operations.Add(operationType, operation);
            return operation;
        }

        private OpenApiResponse AddResponse(OpenApiOperation operation, string responseCode)
        {
            var response = new OpenApiResponse();
            operation.Responses.Add(responseCode, response);
            return response;
        }

        private OpenApiMediaType AddContent(OpenApiResponse response, string contentType)
        {
            var mediaType = new OpenApiMediaType()
            {
                Schema = new OpenApiSchema()
            };
            response.Content.Add(contentType, mediaType);
            return mediaType;
        }

        private void AddProperty(OpenApiMediaType mediaType, string propertyName, IOpenApiAny example)
        {
            mediaType.Schema.Properties.Add(propertyName, new OpenApiSchema() { Example = example });
        }

        private void AddProperty(OpenApiMediaType mediaType, string propertyName, OpenApiSchema schema)
        {
            mediaType.Schema.Properties.Add(propertyName, schema);
        }

        private void AddExample(OpenApiMediaType mediaType, string exampleName, IOpenApiAny example)
        {
            mediaType.Examples.Add(exampleName, new OpenApiExample() { Value = example });
        }

        private void SetExample(OpenApiMediaType mediaType, IOpenApiAny example)
        {
            mediaType.Example = example;
        }

        private HttpRequest CreateRequest(string method = Method)
        {
            var httpContext = new DefaultHttpContext();
            var request = httpContext.Request;
            request.Method = method;
            request.ContentType = "application/json";
            return request;
        }

        private SwaggerExampleResponseBuilderService CreateSut()
        {
            return new SwaggerExampleResponseBuilderService(_logger, _swaggerService);
        }
    }
}
