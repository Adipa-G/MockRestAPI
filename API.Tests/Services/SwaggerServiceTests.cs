using System.IO.Abstractions;
using System.IO.Abstractions.Extensions;

using API.Options;
using API.Services;
using API.Tests.Helpers;

using FluentAssertions;

using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.OpenApi.Models;

using NSubstitute;
using NSubstitute.ExceptionExtensions;

using RichardSzalay.MockHttp;

using Xunit;

namespace API.Tests.Services
{
    public class SwaggerServiceTests
    {
        private readonly LoggerMock<SwaggerService> _logger;
        private readonly IOptions<EndpointOptions> _endpointOptions;
        private readonly IMemoryCache _memoryCache;
        private readonly IFileSystem _fileSystem;
        private readonly IHttpClientFactory _httpClientFactory;

        private const string ApiName = "testApi";

        public SwaggerServiceTests()
        {
            _logger = new LoggerMock<SwaggerService>();
            _endpointOptions = Substitute.For<IOptions<EndpointOptions>>();
            _memoryCache = Substitute.For<IMemoryCache>();
            _fileSystem = Substitute.For<IFileSystem>();
            _httpClientFactory = Substitute.For<IHttpClientFactory>();
        }

        [Fact]
        public async Task GivenCacheEntry_WhenGetOpenApiDocumentAsync_ThenReturnFromCache()
        {
            //Arrange
            var cacheKey = $"open-api-document-{ApiName}";
            var openApiDocument = new OpenApiDocument();

            _memoryCache.TryGetValue(cacheKey, out cacheKey).Returns(x => {
                x[1] = openApiDocument;
                return true;
            });

            //Act
            var sut = CreateSut();
            var resultDoc = await sut.GetOpenApiDocumentAsync("http://localhost:3030", ApiName);

            //Assert
            resultDoc.Should().Be(openApiDocument);
        }

        [Fact]
        public async Task GivenNoApiDef_WhenGetOpenApiDocumentAsync_ThenReturnNull()
        {
            //Arrange
            _endpointOptions.Value.Returns(new EndpointOptions()
            {
                Apis = new List<EndpointOptionsApi>()
                {
                    new() { ApiName = "otherApi", SwaggerLocation = "something else" }
                }
            });

            //Act
            var sut = CreateSut();
            var resultDoc = await sut.GetOpenApiDocumentAsync("http://localhost:3030", ApiName);

            //Assert
            resultDoc.Should().BeNull();
            _logger.ReceivedOnce(LogLevel.Error, "Could not find the API definition");
        }

        [Fact]
        public async Task GivenUnableToFindBaseDirectory_WhenGetOpenApiDocumentAsync_ThenReturnNull()
        {
            //Arrange
            var apiDefDirectoryName = "apiDefs";
            _endpointOptions.Value.Returns(new EndpointOptions()
            {
                RootFolderName = apiDefDirectoryName,
                Apis = new List<EndpointOptionsApi>()
                {
                    new() { ApiName = ApiName, SwaggerLocation = $"{ApiName}\\swagger.json" }
                }
            });
            var appDir = Substitute.For<IDirectoryInfo>();
            appDir.Parent.Returns((IDirectoryInfo?)null);

            _fileSystem.DirectoryInfo.New(Arg.Any<string>()).Returns(appDir);

            //Act
            var sut = CreateSut();
            var resultDoc = await sut.GetOpenApiDocumentAsync("http://localhost:3030", ApiName);

            //Assert
            resultDoc.Should().BeNull();
            _logger.ReceivedOnce(LogLevel.Error, "Could not find the Directory");
        }

        [Fact]
        public async Task GivenFileDoesNotExists_WhenGetOpenApiDocumentAsync_ThenReturnNull()
        {
            //Arrange
            var apiDefDirectoryName = "apiDefs";
            _endpointOptions.Value.Returns(new EndpointOptions()
            {
                RootFolderName = apiDefDirectoryName,
                Apis = new List<EndpointOptionsApi>()
                {
                    new() { ApiName = ApiName, SwaggerLocation = $"{ApiName}\\swagger.json"  }
                }
            });


            var apiDefDirectory = Substitute.For<IDirectoryInfo>();
            apiDefDirectory.Name.Returns(apiDefDirectoryName);
            apiDefDirectory.FullName.Returns($"z:\\\\{apiDefDirectoryName}");

            var appDir = Substitute.For<IDirectoryInfo>();
            appDir.GetDirectories().Returns(new[] { apiDefDirectory });

            _fileSystem.DirectoryInfo.New(Arg.Any<string>()).Returns(appDir);
            _fileSystem.File.Exists($"{apiDefDirectory.FullName}\\{ApiName}\\swagger.json").Returns(false);

            //Act
            var sut = CreateSut();
            var resultDoc = await sut.GetOpenApiDocumentAsync("http://localhost:3030", ApiName);

            //Assert
            resultDoc.Should().BeNull();
            _logger.ReceivedOnce(LogLevel.Error, "Could not find the swagger file in Path");
        }

        [Fact]
        public async Task GivenUnknownErrorOpeningTheFile_WhenGetOpenApiDocumentAsync_ThenReturnNull()
        {
            //Arrange
            var apiDefDirectoryName = "apiDefs";
            _endpointOptions.Value.Returns(new EndpointOptions()
            {
                RootFolderName = apiDefDirectoryName,
                Apis = new List<EndpointOptionsApi>()
                {
                    new() { ApiName = ApiName, SwaggerLocation = $"{ApiName}\\swagger.json"  }
                }
            });


            var apiDefDirectory = Substitute.For<IDirectoryInfo>();
            apiDefDirectory.Name.Returns(apiDefDirectoryName);
            apiDefDirectory.FullName.Returns($"z:\\\\{apiDefDirectoryName}");

            var appDir = Substitute.For<IDirectoryInfo>();
            appDir.GetDirectories().Returns(new[] { apiDefDirectory });

            _fileSystem.DirectoryInfo.New(Arg.Any<string>()).Returns(appDir);
            _fileSystem.File.Exists($"{apiDefDirectory.FullName}\\{ApiName}\\swagger.json").Throws(new Exception("whatever"));

            //Act
            var sut = CreateSut();
            var resultDoc = await sut.GetOpenApiDocumentAsync("http://localhost:3030", ApiName);

            //Assert
            resultDoc.Should().BeNull();
            _logger.ReceivedOnce(LogLevel.Error, "Unknown error trying to open the swagger file");
        }

        [Fact]
        public async Task GivenUnableToGetFileFromWeb_WhenGetOpenApiDocumentAsync_ThenReturnNull()
        {
            //Arrange
            var swggerUrl = "http://localhost/swagger";
            var mockHttp = new MockHttpMessageHandler();
            mockHttp.When(swggerUrl).Throw(new Exception());
            var client = mockHttp.ToHttpClient();
            _httpClientFactory.CreateClient().Returns(client);

            _endpointOptions.Value.Returns(new EndpointOptions()
            {
                RootFolderName = "test",
                Apis = new List<EndpointOptionsApi>()
                {
                    new() { ApiName = ApiName, SwaggerLocation = swggerUrl }
                }
            });

            //Act
            var sut = CreateSut();
            var resultDoc = await sut.GetOpenApiDocumentAsync("http://localhost:3030", ApiName);

            //Assert
            resultDoc.Should().BeNull();
            _logger.ReceivedOnce(LogLevel.Error, "Error trying to get the swagger file from");
        }

        [Fact]
        public async Task GivenUnableToParseDocument_WhenGetOpenApiDocumentAsync_ThenReturnNull()
        {
            var apiDefDirectoryName = "apiDefs";
            _endpointOptions.Value.Returns(new EndpointOptions()
            {
                RootFolderName = apiDefDirectoryName,
                Apis = new List<EndpointOptionsApi>()
                {
                    new() { ApiName = ApiName, SwaggerLocation = $"{ApiName}\\swagger.json"  }
                }
            });


            var apiDefDirectory = Substitute.For<IDirectoryInfo>();
            apiDefDirectory.Name.Returns(apiDefDirectoryName);
            apiDefDirectory.FullName.Returns($"z:\\\\{apiDefDirectoryName}");

            var appDir = Substitute.For<IDirectoryInfo>();
            appDir.GetDirectories().Returns(new[] { apiDefDirectory });

            _fileSystem.DirectoryInfo.New(Arg.Any<string>()).Returns(appDir);
            _fileSystem.File.Exists($"{apiDefDirectory.FullName}\\{ApiName}\\swagger.json").Returns(true);

            var current = new FileSystem().CurrentDirectory();
            var stream = current.FileSystem.FileInfo.New("Samples\\invalid-open-api-def.json").OpenRead();
            _fileSystem.File.Open($"{apiDefDirectory.FullName}\\{ApiName}\\swagger.json", FileMode.Open).Returns(stream);

            //Act
            var sut = CreateSut();
            var resultDoc = await sut.GetOpenApiDocumentAsync("http://localhost:3030", ApiName);

            //Assert
            resultDoc.Should().BeNull();
            _logger.ReceivedOnce(LogLevel.Error, "Could not parse the open api spec");
        }

        [Fact]
        public async Task GivenValidDocument_WhenGetOpenApiDocumentAsync_ThenReturnTheAPIDefinition()
        {
            var apiDefDirectoryName = "apiDefs";
            _endpointOptions.Value.Returns(new EndpointOptions()
            {
                RootFolderName = apiDefDirectoryName,
                Apis = new List<EndpointOptionsApi>()
                {
                    new() { ApiName = ApiName, SwaggerLocation = $"{ApiName}\\swagger.json"  }
                }
            });


            var apiDefDirectory = Substitute.For<IDirectoryInfo>();
            apiDefDirectory.Name.Returns(apiDefDirectoryName);
            apiDefDirectory.FullName.Returns($"z:\\\\{apiDefDirectoryName}");

            var appDir = Substitute.For<IDirectoryInfo>();
            appDir.GetDirectories().Returns(new[] { apiDefDirectory });

            _fileSystem.DirectoryInfo.New(Arg.Any<string>()).Returns(appDir);
            _fileSystem.File.Exists($"{apiDefDirectory.FullName}\\{ApiName}\\swagger.json").Returns(true);

            var current = new FileSystem().CurrentDirectory();
            var stream = current.FileSystem.FileInfo.New("Samples\\valid-open-api-def.json").OpenRead();
            _fileSystem.File.Open($"{apiDefDirectory.FullName}\\{ApiName}\\swagger.json", FileMode.Open).Returns(stream);

            //Act
            var sut = CreateSut();
            var resultDoc = await sut.GetOpenApiDocumentAsync("http://localhost:3030", ApiName);

            //Assert
            resultDoc.Should().NotBeNull();
            resultDoc?.Paths.Should().HaveCount(1);
            resultDoc?.Servers.Should().HaveCount(1);
            resultDoc?.Servers[0].Should().BeEquivalentTo(new {
                Url = $"http://localhost:3030/{ApiName}"
            });
        }

        [Fact]
        public async Task GivenDocumentInParentFolder_WhenGetOpenApiDocumentAsync_ThenReturnTheAPIDefinition()
        {
            var apiDefDirectoryName = "apiDefs";
            _endpointOptions.Value.Returns(new EndpointOptions()
            {
                RootFolderName = apiDefDirectoryName,
                Apis = new List<EndpointOptionsApi>()
                {
                    new() { ApiName = ApiName, SwaggerLocation = $"{ApiName}\\swagger.json"  }
                }
            });


            var apiDefDirectory = Substitute.For<IDirectoryInfo>();
            apiDefDirectory.Name.Returns(apiDefDirectoryName);
            apiDefDirectory.FullName.Returns($"z:\\\\{apiDefDirectoryName}");

            var appParentDir = Substitute.For<IDirectoryInfo>();
            appParentDir.GetDirectories().Returns(new[] { apiDefDirectory });

            var appDir = Substitute.For<IDirectoryInfo>();
            appDir.Parent.Returns(appParentDir);

            _fileSystem.DirectoryInfo.New(Arg.Any<string>()).Returns(appDir);
            _fileSystem.File.Exists($"{apiDefDirectory.FullName}\\{ApiName}\\swagger.json").Returns(true);

            var current = new FileSystem().CurrentDirectory();
            var stream = current.FileSystem.FileInfo.New("Samples\\valid-open-api-def.json").OpenRead();
            _fileSystem.File.Open($"{apiDefDirectory.FullName}\\{ApiName}\\swagger.json", FileMode.Open).Returns(stream);

            //Act
            var sut = CreateSut();
            var resultDoc = await sut.GetOpenApiDocumentAsync("http://localhost:3030", ApiName);

            //Assert
            resultDoc.Should().NotBeNull();
            resultDoc?.Paths.Should().HaveCount(1);
        }

        [Fact]
        public async Task GivenNullDocument_WhenGetSwaggerJsonAsync_ThenReturnNull()
        {
            var apiDefDirectoryName = "apiDefs";
            _endpointOptions.Value.Returns(new EndpointOptions()
            {
                RootFolderName = apiDefDirectoryName,
                Apis = new List<EndpointOptionsApi>()
                {
                    new() { ApiName = ApiName, SwaggerLocation = $"{ApiName}\\swagger.json"  }
                }
            });


            var apiDefDirectory = Substitute.For<IDirectoryInfo>();
            apiDefDirectory.Name.Returns(apiDefDirectoryName);
            apiDefDirectory.FullName.Returns($"z:\\\\{apiDefDirectoryName}");

            var appDir = Substitute.For<IDirectoryInfo>();
            appDir.GetDirectories().Returns(new[] { apiDefDirectory });

            _fileSystem.DirectoryInfo.New(Arg.Any<string>()).Returns(appDir);
            _fileSystem.File.Exists($"{apiDefDirectory.FullName}\\{ApiName}\\swagger.json").Returns(true);

            var current = new FileSystem().CurrentDirectory();
            var stream = current.FileSystem.FileInfo.New("Samples\\invalid-open-api-def.json").OpenRead();
            _fileSystem.File.Open($"{apiDefDirectory.FullName}\\{ApiName}\\swagger.json", FileMode.Open).Returns(stream);

            //Act
            var sut = CreateSut();
            var swaggerJson = await sut.GetSwaggerJsonAsync("http://localhost:3030", ApiName);

            //Assert
            swaggerJson.Should().BeNullOrWhiteSpace();
            _logger.ReceivedOnce(LogLevel.Error, "Could not parse the open api spec");
        }

        [Fact]
        public async Task GivenValidDocument_WhenGetSwaggerJsonAsync_ThenReturnJson()
        {
            var apiDefDirectoryName = "apiDefs";
            _endpointOptions.Value.Returns(new EndpointOptions()
            {
                RootFolderName = apiDefDirectoryName,
                Apis = new List<EndpointOptionsApi>()
                {
                    new() { ApiName = ApiName, SwaggerLocation = $"{ApiName}\\swagger.json"  }
                }
            });


            var apiDefDirectory = Substitute.For<IDirectoryInfo>();
            apiDefDirectory.Name.Returns(apiDefDirectoryName);
            apiDefDirectory.FullName.Returns($"z:\\\\{apiDefDirectoryName}");

            var appDir = Substitute.For<IDirectoryInfo>();
            appDir.GetDirectories().Returns(new[] { apiDefDirectory });

            _fileSystem.DirectoryInfo.New(Arg.Any<string>()).Returns(appDir);
            _fileSystem.File.Exists($"{apiDefDirectory.FullName}\\{ApiName}\\swagger.json").Returns(true);

            var current = new FileSystem().CurrentDirectory();
            var stream = current.FileSystem.FileInfo.New("Samples\\valid-open-api-def.json").OpenRead();
            _fileSystem.File.Open($"{apiDefDirectory.FullName}\\{ApiName}\\swagger.json", FileMode.Open).Returns(stream);

            //Act
            var sut = CreateSut();
            var swaggerJson = await sut.GetSwaggerJsonAsync("http://localhost:3030", ApiName);

            //Assert
            swaggerJson.Should().NotBeNullOrWhiteSpace();
        }

        private ISwaggerService CreateSut()
        {
            return new SwaggerService(_logger, _endpointOptions, _memoryCache, _fileSystem, _httpClientFactory);
        }
    }
}
