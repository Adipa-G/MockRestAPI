using System.Text;

using API.Services;
using API.Tests.Helpers;

using FluentAssertions;

using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;

using NSubstitute;

using Xunit;

namespace API.Tests.Services
{
    public class GlobalPathsHandlerServiceTests
    {
        private readonly LoggerMock<GlobalPathsHandlerService> _logger;
        private readonly ISwaggerService _swaggerService;
        private readonly IResponseGeneratorService _responseGeneratorService;

        private readonly MemoryStream _responseBody;

        public GlobalPathsHandlerServiceTests()
        {
            _logger = Substitute.For<LoggerMock<GlobalPathsHandlerService>>();
            _swaggerService = Substitute.For<ISwaggerService>();
            _responseGeneratorService = Substitute.For<IResponseGeneratorService>();

            _responseBody = new MemoryStream();
        }

        [Fact]
        public async Task GivenMockAPISwaggerRequest_WhenHandleAsync_ThenReturnSwaggerContent()
        {
            //Arrange
            string method = "get";
            string apiName = "mockApi";
            string swaggerJson = "{ \"id\" = \"test\" }";

            var context = CreateContext(method, $"/{apiName}/swagger/v2/swagger.json");
            _swaggerService.GetSwaggerJsonAsync(Arg.Any<string>())
                .Returns(swaggerJson);

            //Act
            var sut = CreateSut();
            var result = await sut.HandleAsync(context);

            //Assert
            result.Should().BeTrue();
            _logger.ReceivedOnce(LogLevel.Information, "Handling the path");
            await _swaggerService.Received(1).GetSwaggerJsonAsync( apiName);
            GetResponseBody().Should().Be(swaggerJson);
            context.Response.StatusCode.Should().Be(200);
        }

        [Fact]
        public async Task GivenMockAPIRequestAndReturnsValidResponse_WhenHandleAsync_ThenCallResponseGenerator()
        {
            //Arrange
            string method = "get";
            string apiName = "mockApi";
            string requestPath = "pet/21";
            string responsePayload = "{ \"id\" = \"test\" }";

            var context = CreateContext(method, $"/{apiName}/{requestPath}");
            _responseGeneratorService.GenerateJsonResponseAsync( Arg.Any<string>(), Arg.Any<string>(), Arg.Any<HttpRequest>())
                .Returns(new KeyValuePair<string, string?>("200", responsePayload));

            //Act
            var sut = CreateSut();
            var result = await sut.HandleAsync(context);

            //Assert
            result.Should().BeTrue();
            _logger.ReceivedOnce(LogLevel.Information, "Handling the path");
            await _responseGeneratorService.Received(1).GenerateJsonResponseAsync( apiName, requestPath, context.Request);
            GetResponseBody().Should().Be(responsePayload);
            context.Response.StatusCode.Should().Be(200);
        }
        
        private string GetResponseBody()
        {
            return Encoding.UTF8.GetString(_responseBody.ToArray());
        }

        private HttpContext CreateContext(string method, string path)
        {
            var httpContext = new DefaultHttpContext();
            var request = httpContext.Request;
            request.Scheme = "http";
            request.Host = new HostString("localhost");
            request.Method = method;
            request.Path = new PathString(path);
            request.ContentType = "application/json";

            var response = httpContext.Response;
            response.Body = _responseBody;

            return httpContext;
        }

        private GlobalPathsHandlerService CreateSut()
        {
            return new GlobalPathsHandlerService(_logger, _swaggerService, _responseGeneratorService);
        }
    }
}
