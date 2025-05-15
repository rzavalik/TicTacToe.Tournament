namespace TicTacToe.Tournament.Models.Tests
{
    using System.Net;
    using Moq;
    using Moq.Protected;
    using Shouldly;
    using TicTacToe.Tournament.Models.Interfaces;


    public class HttpClientWrapperTests
    {
        [Fact]
        public async Task PostAsJsonAsync_ValidCall_DelegatesToHttpClient()
        {
            var handlerMock = new Mock<HttpMessageHandler>();
            var expectedResponse = new HttpResponseMessage(HttpStatusCode.OK);

            handlerMock.Protected()
                .Setup<Task<HttpResponseMessage>>(
                    "SendAsync",
                    ItExpr.IsAny<HttpRequestMessage>(),
                    ItExpr.IsAny<CancellationToken>()
                )
                .ReturnsAsync(expectedResponse);

            var httpClient = new HttpClient(handlerMock.Object);
            var sut = MakeSut(httpClient);

            var response = await sut.PostAsJsonAsync("https://example.com", new { test = "value" });

            response.ShouldBe(expectedResponse);
        }

        [Fact]
        public async Task PostAsJsonAsync_NullUrl_ThrowsArgumentNullException()
        {
            var httpClient = new HttpClient();
            var sut = MakeSut(httpClient);

            await Should.ThrowAsync<ArgumentNullException>(async () =>
            {
                await sut.PostAsJsonAsync(null!, new { });
            });
        }

        [Fact]
        public async Task PostAsJsonAsync_NullContent_ThrowsArgumentNullException()
        {
            var httpClient = new HttpClient();
            var sut = MakeSut(httpClient);

            await Should.ThrowAsync<ArgumentNullException>(async () =>
            {
                await sut.PostAsJsonAsync<object>("https://example.com", null!);
            });
        }

        [Fact]
        public async Task PostAsJsonAsync_HttpClientThrows_PropagatesException()
        {
            var handlerMock = new Mock<HttpMessageHandler>();

            handlerMock.Protected()
                .Setup<Task<HttpResponseMessage>>(
                    "SendAsync",
                    ItExpr.IsAny<HttpRequestMessage>(),
                    ItExpr.IsAny<CancellationToken>()
                )
                .ThrowsAsync(new HttpRequestException("Simulated failure"));

            var httpClient = new HttpClient(handlerMock.Object);
            var sut = MakeSut(httpClient);

            await Should.ThrowAsync<HttpRequestException>(async () =>
            {
                await sut.PostAsJsonAsync("https://example.com", new { });
            });
        }


        private IHttpClient MakeSut(HttpClient client)
        {
            return (IHttpClient)new HttpClientWrapper(client);
        }
    }
}