using System.Net;
using Moq;
using Moq.Protected;
using Shouldly;
using TicTacToe.Tournament.Models.Interfaces;

namespace TicTacToe.Tournament.Models.Tests;

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

    private IHttpClient MakeSut(HttpClient client)
    {
        return (IHttpClient)new HttpClientWrapper(client);
    }
}
