using Microsoft.AspNetCore.SignalR.Client;
using Moq;
using Shouldly;
using System.Net;
using System.Net.Http.Json;
using TicTacToe.Tournament.Auth;
using TicTacToe.Tournament.BasePlayer;
using TicTacToe.Tournament.BasePlayer.Interfaces;
using TicTacToe.Tournament.Models;

namespace TicTacToe.Tournament.Player.Tests;

public class TestPlayerClient : BasePlayerClient
{
    public TestPlayerClient(
        string botName,
        Guid tournamentId,
        string webAppEndpoint,
        string signalrEndpoint,
        IHttpClient httpClient,
        ISignalRClientBuilder signalRBuilder)
        : base(
            botName,
            tournamentId,
            webAppEndpoint,
            signalrEndpoint,
            httpClient,
            signalRBuilder)
    { }

    protected override Task<(int row, int col)> MakeMove(Guid matchId, Mark[][] board) => Task.FromResult((0, 0));
}

public class BasePlayerClientTests
{
    private BasePlayerClient MakeSut(
        Guid? tournamentId = null,
        Guid? playerId = null,
        string token = "fake-token")
    {
        tournamentId ??= Guid.NewGuid();
        playerId ??= Guid.NewGuid();

        var httpClientMock = new Mock<IHttpClient>();
        var signalRClientMock = new Mock<ISignalRClient>();
        var signalRBuilderMock = new Mock<ISignalRClientBuilder>();

        var fakeResponse = new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = JsonContent.Create(new TournamentAuthResponse
            {
                Token = token,
                PlayerId = playerId.Value,
                TournamentId = tournamentId.Value
            })
        };

        httpClientMock.Setup(h => h.PostAsJsonAsync(
            It.IsAny<string>(),
            It.IsAny<object>(),
            null,
            default))
            .ReturnsAsync(fakeResponse);

        signalRBuilderMock
            .Setup(b => b.Build(It.IsAny<string>(), It.IsAny<Func<Task<string?>>>()))
            .Returns(() => signalRClientMock.Object);

        var sut = new TestPlayerClient(
            "BotX",
            tournamentId.Value,
            "http://localhost:5000",
            "http://signalr",
            httpClientMock.Object,
            signalRBuilderMock.Object);

        return sut;
    }


    [Fact]
    public async Task AuthenticateAsync_WithValidResponse_ShouldSetPlayerIdAndToken()
    {
        var tournamentId = Guid.NewGuid();
        var playerId = Guid.NewGuid();
        const string expectedToken = "fake-token";

        var httpClientMock = new Mock<IHttpClient>();
        var signalRClientMock = new Mock<ISignalRClient>();
        var signalRBuilderMock = new Mock<ISignalRClientBuilder>();

        var response = new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = JsonContent.Create(new TournamentAuthResponse
            {
                PlayerId = playerId,
                Token = expectedToken,
                TournamentId = tournamentId
            })
        };

        var requestUrl = $"http://webapp/tournament/{tournamentId}/authenticate";

        httpClientMock
            .Setup(h => h.PostAsJsonAsync(
                            requestUrl,
                            It.Is<TournamentAuthRequest>(r =>
                                r.TournamentId == tournamentId),
                            null,
                            default))
            .ReturnsAsync(response);

        signalRBuilderMock
            .Setup(b => b.Build(It.IsAny<string>(), It.IsAny<Func<Task<string?>>>()))
            .Returns(signalRClientMock.Object);

        var sut = new TestPlayerClient(
            "BotX",
            tournamentId,
            "http://webapp",
            "http://signalr",
            httpClientMock.Object,
            signalRBuilderMock.Object);

        await sut.AuthenticateAsync();

        sut.PlayerId.ShouldBe(playerId);
        sut.Authenticated.ShouldBe(true);
        sut.Token.ShouldBe(expectedToken);
    }

}
