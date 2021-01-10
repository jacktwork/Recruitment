using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Moq.Protected;
using Newtonsoft.Json;
using Recruitment.API.Controllers;
using Recruitment.Contracts;
using System;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace Recuitment.Tests
{
    public class RecruitmentTests
    {
        private readonly HashController _hashController;
        private readonly Mock<ILogger<HashController>> _loggerMock = new Mock<ILogger<HashController>>();
        private readonly Mock<IConfiguration> _configMock = new Mock<IConfiguration>();
        private readonly Mock<IHttpClientFactory> _httpClientFactoryMock = new Mock<IHttpClientFactory>();

        private static Mock<HttpRequest> CreateMockRequest(object body)
        {
            var ms = new MemoryStream();
            var sw = new StreamWriter(ms);

            var json = JsonConvert.SerializeObject(body);

            sw.Write(json);
            sw.Flush();

            ms.Position = 0;

            var mockRequest = new Mock<HttpRequest>();
            mockRequest.Setup(x => x.Body).Returns(ms);

            return mockRequest;
        }

        public RecruitmentTests()
        {
            // prepare mock httpclient 
            var mockHttpMessageHandler = new Mock<HttpMessageHandler>();

            HashValue hashValue = new HashValue();
            hashValue.hash_value = "4ED9407630EB1000C0F6B63842DEFA7D";

            mockHttpMessageHandler.Protected()
                .Setup<Task<HttpResponseMessage>>("SendAsync", ItExpr.IsAny<HttpRequestMessage>(), ItExpr.IsAny<CancellationToken>())
                .ReturnsAsync(new HttpResponseMessage
                {
                    StatusCode = HttpStatusCode.OK,
                    Content = new StringContent(JsonConvert.SerializeObject(hashValue))
                });

            var client = new HttpClient(mockHttpMessageHandler.Object);
            client.BaseAddress = new Uri("https://alternis.azurewebsites.net/api/CalcHash?code=5Fo6KFASKuXSHN5TwZOhlX9QoIk/bZn5mNANuhG7KQX4Rx5NaQat8g==");
            _httpClientFactoryMock.Setup(_ => _.CreateClient(It.IsAny<string>())).Returns(client);

            // prepare mock config 
            _configMock.SetupGet(m => m[It.Is<string>(s => s == "functionUrl")]).Returns("https://alternis.azurewebsites.net/api/CalcHash");
            _configMock.SetupGet(m => m[It.Is<string>(s => s == "functionCode")]).Returns("5Fo6KFASKuXSHN5TwZOhlX9QoIk/bZn5mNANuhG7KQX4Rx5NaQat8g==");

            // 
            _hashController = new HashController(_loggerMock.Object, _configMock.Object, _httpClientFactoryMock.Object);
        }

        [Fact]
        public async Task Get()
        {
            string response = _hashController.Get();
            Assert.Equal("Hello World", response);
        }


        [Fact]
        public async Task CalcHashAPIGood()
        {
            // Arrange
            Credentials credentials = new Credentials();
            credentials.login = "abc";
            credentials.password = "def";

            // Act  
            var response = await _hashController.Post(credentials);

            // Assert  
            Assert.NotNull(response);
            Assert.IsAssignableFrom<OkObjectResult>(response);
            Assert.IsAssignableFrom<HashValue>((response as OkObjectResult)?.Value);
            HashValue hashValue = (response as OkObjectResult)?.Value as HashValue;
            Assert.Equal("4ED9407630EB1000C0F6B63842DEFA7D", hashValue.hash_value);
        }

        [Fact]
        public async Task CalcHashAPIBad()
        {
            // Arrange
            Credentials credentials = new Credentials();
            credentials.login = "abc";
            _hashController.ModelState.AddModelError("key", "error message");

            // Act  
            var response = await _hashController.Post(credentials);

            // Assert  
            Assert.NotNull(response);
            Assert.IsType<BadRequestObjectResult>(response);
            var objectResponse = response as BadRequestObjectResult;
            Assert.Equal(400, objectResponse.StatusCode);
        }

        [Fact]
        public async Task CalcHashFunctionGood()
        {
            // Arrange
            Credentials credentials = new Credentials();
            credentials.login = "abc";
            credentials.password = "def";

            Mock<HttpRequest> mockRequest = CreateMockRequest(credentials);
            var logger = NullLoggerFactory.Instance.CreateLogger("Null Logger");

            // Act
            var response = (OkObjectResult) await Recruitment.Functions.CalcHash.Run(mockRequest.Object, logger);

            // Assert  
            Assert.NotNull(response);
            Assert.IsAssignableFrom<OkObjectResult>(response);
            Assert.IsAssignableFrom<HashValue>((response as OkObjectResult)?.Value);
            HashValue hashValue = (response as OkObjectResult)?.Value as HashValue;
            Assert.Equal("4ED9407630EB1000C0F6B63842DEFA7D", hashValue.hash_value);
        }

        [Fact]
        public async Task CalcHashFunctionBad()
        {
            // Arrange
            // missing password
            Credentials credentials = new Credentials();
            credentials.login = "abc";

            Mock<HttpRequest> mockRequest = CreateMockRequest(credentials);
            var logger = NullLoggerFactory.Instance.CreateLogger("Null Logger");

            // Act
            var response = await Recruitment.Functions.CalcHash.Run(mockRequest.Object, logger);

            // Assert
            Assert.NotNull(response);
            Assert.IsType<BadRequestResult>(response);
            var objectResponse = response as BadRequestResult;
            Assert.Equal(400, objectResponse.StatusCode);
        }

    }
}
