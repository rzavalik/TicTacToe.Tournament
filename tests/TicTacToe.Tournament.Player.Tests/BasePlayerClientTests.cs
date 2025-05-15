namespace TicTacToe.Tournament.Player.Tests
{
    using System.Net;
    using System.Net.Http.Json;
    using Microsoft.AspNetCore.SignalR.Client;
    using Moq;
    using Shouldly;
    using TicTacToe.Tournament.Auth;
    using TicTacToe.Tournament.BasePlayer;
    using TicTacToe.Tournament.BasePlayer.Interfaces;
    using TicTacToe.Tournament.Models;
    using TicTacToe.Tournament.Models.DTOs;
    using Xunit;

    public class TestPlayerClient : BasePlayerClient
    {
        public TestPlayerClient(
            string botName,
            Guid tournamentId,
            string webAppEndpoint,
            string signalrEndpoint,
            IHttpClient httpClient,
            ISignalRClientBuilder signalRBuilder)
            : base(botName, tournamentId, webAppEndpoint, signalrEndpoint, httpClient, signalRBuilder)
        { }

        protected override Task<(byte row, byte col)> MakeMove(Guid matchId, Mark[][] board)
        {
            return Task.FromResult(((byte)0, (byte)0));
        }

        public void SetConsoleUI(IGameConsoleUI consoleUi)
        {
            _consoleUI = consoleUi;
        }

        public new async Task StartAsync() => await base.StartAsync();

        public new async Task OnMatchStarted(Guid matchId, Guid playerId, Guid opponentId, Mark playerMark, bool yourTurn)
            => base.OnMatchStarted(matchId, playerId, opponentId, playerMark, yourTurn);

        public new async Task OnMatchEnded(GameResult gameResult)
            => base.OnMatchEnded(gameResult);

        public void SetToken(string token) => base.Token = token;

        public void SetPlayerId(Guid playerId) => base.PlayerId = playerId;

        public new TournamentDto Tournament
        {
            get => base.Tournament;
            set => base.Tournament = value;
        }

        public bool IsUserPlaying(Guid? matchId = null)
        {
            return matchId.HasValue
                ? IsUserPlaying(matchId.Value)
                : IsUserPlaying();
        }

        public new string GetPlayerName(Guid playerId)
        {
            return base.GetPlayerName(playerId);
        }
    }

    public class BasePlayerClientTests
    {
        private TestPlayerClient MakeSut(
            Guid? tournamentId,
            Guid? playerId,
            string token,
            out Mock<IHttpClient> httpClientMock,
            out Mock<ISignalRClient> signalRClientMock,
            out Mock<ISignalRClientBuilder> signalRBuilderMock)
        {
            tournamentId ??= Guid.NewGuid();
            playerId ??= Guid.NewGuid();

            httpClientMock = new Mock<IHttpClient>();
            signalRClientMock = new Mock<ISignalRClient>();
            signalRBuilderMock = new Mock<ISignalRClientBuilder>();

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
                .Returns(signalRClientMock.Object);

            return new TestPlayerClient(
                "BotX",
                tournamentId.Value,
                "http://localhost:5000",
                "http://signalr",
                httpClientMock.Object,
                signalRBuilderMock.Object);
        }

        [Fact]
        public async Task AuthenticateAsync_WithValidResponse_ShouldSetPlayerIdAndToken()
        {
            var tournamentId = Guid.NewGuid();
            var playerId = Guid.NewGuid();
            const string expectedToken = "fake-token";

            var sut = MakeSut(
                tournamentId,
                playerId,
                expectedToken,
                out var httpClientMock,
                out var signalRClientMock,
                out var signalRBuilderMock);

            await sut.AuthenticateAsync();

            sut.UserId.ShouldBe(playerId);
            sut.Authenticated.ShouldBeTrue();
            sut.Token.ShouldBe(expectedToken);
        }

        [Fact]
        public async Task AuthenticateAsync_WithInvalidResponse_ShouldThrowAccessViolationException()
        {
            var tournamentId = Guid.NewGuid();
            var httpClientMock = new Mock<IHttpClient>();
            var signalRBuilderMock = new Mock<ISignalRClientBuilder>();
            var signalRClientMock = new Mock<ISignalRClient>();

            var badResponse = new HttpResponseMessage(HttpStatusCode.BadRequest);

            httpClientMock.Setup(h => h.PostAsJsonAsync(
                It.IsAny<string>(),
                It.IsAny<object>(),
                null,
                default))
                .ReturnsAsync(badResponse);

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

            await Should.ThrowAsync<AccessViolationException>(() => sut.AuthenticateAsync());
        }

        [Fact]
        public async Task OnMatchStarted_ShouldExecuteWithoutErrors()
        {
            var sut = MakeSut(
                null,
                null,
                "fake-token",
                out var httpClientMock,
                out var signalRClientMock,
                out var signalRBuilderMock);

            sut.SetPlayerId(Guid.NewGuid());

            await sut.OnMatchStarted(
                Guid.NewGuid(),
                sut.PlayerId!,
                Guid.NewGuid(),
                Mark.X,
                true);
        }

        [Fact]
        public async Task OnMatchEnded_ShouldExecuteWithoutErrors()
        {
            var sut = MakeSut(
                null,
                null,
                "fake-token",
                out var httpClientMock,
                out var signalRClientMock,
                out var signalRBuilderMock);

            await sut.OnMatchEnded(new GameResult(Guid.NewGuid(), Guid.NewGuid(), new Board(), false));
        }

        [Fact]
        public async Task StartAsync_ShouldStartAndRegisterSuccessfully()
        {
            var sut = MakeSut(null, null, "token", out var httpMock, out var signalRMock, out var builderMock);

            sut.SetConsoleUI(null);

            signalRMock.Setup(s => s.StartAsync()).Returns(Task.CompletedTask);
            signalRMock.Setup(s => s.State).Returns(HubConnectionState.Connected);
            signalRMock.Setup(s => s.GetTournamentAsync(It.IsAny<Guid>())).ReturnsAsync(new TournamentDto());

            var startTask = sut.StartAsync();
            await Task.Delay(500);

            startTask.IsCompleted.ShouldBeFalse();
        }

        [Fact]
        public async Task ConnectToSignalRAsync_WithNullClient_ShouldThrow()
        {
            var sut = MakeSut(null, null, "token", out var httpMock, out var signalRMock, out var builderMock);
            builderMock.Setup(b => b.Build(It.IsAny<string>(), It.IsAny<Func<Task<string?>>>()))
                .Returns((ISignalRClient)null!);

            sut.SetConsoleUI(null);

            await Should.ThrowAsync<ArgumentNullException>(() => sut.StartAsync());
        }

        [Fact]
        public void GetTournamentAsync_WhenClientIsNull_ShouldReturnNull()
        {
            var sut = MakeSut(null, null, "token", out var httpMock, out var signalRMock, out var builderMock);

            var result = sut.Tournament;

            result.ShouldBeNull();
        }

        [Fact]
        public void GetPlayerName_WhenPlayerExists_ShouldReturnName()
        {
            var sut = MakeSut(null, null, "token", out var _, out var _, out var _);
            var playerId = Guid.NewGuid();

            var tournament = new TournamentDto
            {
                RegisteredPlayers = new Dictionary<Guid, string>
                {
                    { playerId, "BotX" }
                }
            };

            sut.Tournament = tournament;

            var result = sut.GetPlayerName(playerId);

            result.ShouldBe("BotX");
        }
    }
}