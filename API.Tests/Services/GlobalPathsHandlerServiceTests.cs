using System.Net;
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
        private readonly ISwaggerExampleResponseBuilderService _exampleResponseBuilder;

        private MemoryStream _responseBody;

        public GlobalPathsHandlerServiceTests()
        {
            _logger = Substitute.For<LoggerMock<GlobalPathsHandlerService>>();
            _swaggerService = Substitute.For<ISwaggerService>();
            _exampleResponseBuilder = Substitute.For<ISwaggerExampleResponseBuilderService>();

            _responseBody = new MemoryStream();
        }

        [Fact]
        public async Task GivenMockAPISwaggerRequest_WhenHandleAsync_ThenReturnSwaggerContent()
        {
            //Arrange
            string swaggerJson = "{ \"id\" = \"test\" }";

            var context = CreateContext("get", "/mockApi/swagger/v2/swagger.json");
            _swaggerService.GetSwaggerJsonAsync(Arg.Any<string>(), Arg.Any<string>())
                .Returns(swaggerJson);

            //Act
            var sut = CreateSut();
            await sut.HandleAsync(context);

            //Assert
            _logger.ReceivedOnce(LogLevel.Information, "Handling the path");
            await _swaggerService.Received(1).GetSwaggerJsonAsync("http://localhost", "mockApi");
            GetResponseBody().Should().Be(swaggerJson);
            context.Response.StatusCode.Should().Be(200);
        }

        [Fact]
        public async Task GivenMockAPIRequestAndReturnsValidResponse_WhenHandleAsync_ThenReturnResponse()
        {
            //Arrange
            string responsePayload = "{ \"id\" = \"test\" }";

            var context = CreateContext("get", "/mockApi/pet/21");
            _exampleResponseBuilder.GetResponse(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<HttpRequest>())
                .Returns(new KeyValuePair<string, string?>("200", responsePayload));

            //Act
            var sut = CreateSut();
            await sut.HandleAsync(context);

            //Assert
            _logger.ReceivedOnce(LogLevel.Information, "Handling the path");
            await _exampleResponseBuilder.Received(1).GetResponse("http://localhost", "mockApi", "/pet/21", context.Request);
            GetResponseBody().Should().Be(responsePayload);
            context.Response.StatusCode.Should().Be(200);
        }

        [Fact]
        public async Task GivenMockAPIRequestAndReturnsEmptyResponse_WhenHandleAsync_ThenReturnResponse()
        {
            //Arrange
            var context = CreateContext("get", "/mockApi/pet/21");
            _exampleResponseBuilder.GetResponse(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>(),
                Arg.Any<HttpRequest>()).Returns(default(KeyValuePair<string, string?>));

            //Act
            var sut = CreateSut();
            await sut.HandleAsync(context);

            //Assert
            _logger.ReceivedOnce(LogLevel.Information, "Handling the path");
            await _exampleResponseBuilder.Received(1).GetResponse("http://localhost", "mockApi", "/pet/21", context.Request);
            GetResponseBody().Should().Contain("I have never met this man in my life.");
            context.Response.StatusCode.Should().Be(400);
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
            return new GlobalPathsHandlerService(_logger, _swaggerService, _exampleResponseBuilder);
        }
    }
}
