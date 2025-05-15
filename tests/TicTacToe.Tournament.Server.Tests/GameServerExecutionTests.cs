namespace TicTacToe.Tournament.Server.Tests;

using Microsoft.AspNetCore.SignalR;
using Moq;
using Shouldly;
using TicTacToe.Tournament.Models;
using TicTacToe.Tournament.Models.Interfaces;
using TicTacToe.Tournament.Server.Hubs;

public class GameServerExecutionTests
{
    private readonly Mock<IHubContext<TournamentHub>> _hubContextMock = new();
    private readonly Mock<IClientProxy> _clientProxyMock = new();
    private readonly List<(Guid playerId, MatchScore score)> _leaderboardCalls = new();

    private GameServer MakeSut(out Guid playerAId, out Guid playerBId)
    {
        var clientsMock = new Mock<IHubClients>();
        clientsMock.Setup(c => c.User(It.IsAny<string>())).Returns(_clientProxyMock.Object);
        clientsMock.Setup(c => c.Group(It.IsAny<string>())).Returns(_clientProxyMock.Object);
        _hubContextMock.Setup(c => c.Clients).Returns(clientsMock.Object);

        var tournament = new Tournament(Guid.NewGuid(), "Tournament Test", 1);
        var server = new GameServer(
            tournament,
            _hubContextMock.Object,
            (id, score) =>
            {
                _leaderboardCalls.Add((id, score));
                tournament.AgreggateScoreToPlayer(id, score);
            },
            () => { },
            TimeSpan.FromSeconds(2)
        );

        playerAId = Guid.NewGuid();
        playerBId = Guid.NewGuid();

        var botA = new Mock<IPlayerBot>();
        var botB = new Mock<IPlayerBot>();

        botA.SetupGet(x => x.Id).Returns(playerAId);
        botA.SetupGet(x => x.Name).Returns("Bot A");
        botB.SetupGet(x => x.Id).Returns(playerBId);
        botB.SetupGet(x => x.Name).Returns("Bot B");

        server.RegisterPlayer(botA.Object);
        server.RegisterPlayer(botB.Object);

        server.SubmitMove(playerAId, 0, 0);
        server.SubmitMove(playerBId, 1, 1);
        server.SubmitMove(playerAId, 0, 1);
        server.SubmitMove(playerBId, 2, 2);
        server.SubmitMove(playerAId, 0, 2);

        return server;
    }

    [Fact]
    public async Task StartTournamentAsync_ShouldFinishAllMatches()
    {
        var sut = MakeSut(out _, out _);
        var tournament = sut.Tournament;

        await sut.StartTournamentAsync(tournament);

        tournament.Status.ShouldBe(TournamentStatus.Finished);
        tournament.EndTime.ShouldNotBeNull();
        tournament.Matches.ShouldAllBe(m => m.Status == MatchStatus.Finished);
    }

    [Fact]
    public void GenerateMatches_ShouldCreateExpectedMatches_WithMatchRepetition()
    {
        var tournament = new Tournament(Guid.NewGuid(), "Teste", 3); // MatchRepetition = 3
        var hubContextMock = new Mock<Microsoft.AspNetCore.SignalR.IHubContext<Server.Hubs.TournamentHub>>();
        var server = new GameServer(
            tournament,
            hubContextMock.Object,
            (_, _) => { },
            () => { },
            TimeSpan.FromSeconds(1)
        );

        var bot1 = new Mock<IPlayerBot>();
        var bot2 = new Mock<IPlayerBot>();
        bot1.SetupGet(b => b.Id).Returns(Guid.NewGuid());
        bot2.SetupGet(b => b.Id).Returns(Guid.NewGuid());
        bot1.SetupGet(b => b.Name).Returns("Bot 1");
        bot2.SetupGet(b => b.Name).Returns("Bot 2");

        server.RegisterPlayer(bot1.Object);
        server.RegisterPlayer(bot2.Object);

        tournament.Matches.Clear();
        server.GenerateMatches();

        tournament.Matches.Count.ShouldBe(6);
        tournament.Matches.ShouldAllBe(m => m.Status == MatchStatus.Planned);
    }

    [Fact]
    public async Task PlayMatchAsync_ShouldHandleValidMovesAndDeclareWinner()
    {
        var sut = MakeSut(out var playerA, out var playerB);
        var tournament = sut.Tournament;

        sut.InitializeLeaderboard();
        await sut.StartTournamentAsync(tournament);

        Thread.Sleep(tournament.Matches.Count * 1100);

        var fisrtMatch = tournament.Matches.First();
        var secondMatch = tournament.Matches.Last();

        tournament.Status.ShouldBe(TournamentStatus.Finished);

        fisrtMatch.WinnerMark.ShouldBe(Mark.O);
        secondMatch.WinnerMark.ShouldBe(Mark.X);

        var leaderboardPlayerA = tournament.Leaderboard.First(p => p.PlayerId == playerA);
        var leaderboardPlayerB = tournament.Leaderboard.First(p => p.PlayerId == playerA);

        leaderboardPlayerA.GamesPlayed.ShouldBe(2);
        leaderboardPlayerA.Walkovers.ShouldBe((uint)1);

        leaderboardPlayerB.GamesPlayed.ShouldBe(2);
        leaderboardPlayerB.Walkovers.ShouldBe((uint)1);
    }

    [Fact]
    public async Task PlayMatchAsync_ShouldHandleTimeoutAndDeclareWalkover()
    {
        var clientsMock = new Mock<IHubClients>();
        clientsMock.Setup(c => c.User(It.IsAny<string>())).Returns(_clientProxyMock.Object);
        clientsMock.Setup(c => c.Group(It.IsAny<string>())).Returns(_clientProxyMock.Object);
        _hubContextMock.Setup(c => c.Clients).Returns(clientsMock.Object);

        var tournament = new Tournament(Guid.NewGuid(), "Walkover Test", 1);

        var gameServer = new GameServer(tournament, _hubContextMock.Object,
            (id, score) =>
            {
                _leaderboardCalls.Add((id, score));
                tournament.AgreggateScoreToPlayer(id, score);
            },
            () => { },
            TimeSpan.FromSeconds(2)
        );

        var playerA = Guid.NewGuid();
        var playerB = Guid.NewGuid();

        var botA = new Mock<IPlayerBot>();
        var botB = new Mock<IPlayerBot>();

        botA.SetupGet(x => x.Id).Returns(playerA);
        botA.SetupGet(x => x.Name).Returns("Bot A");

        botB.SetupGet(x => x.Id).Returns(playerB);
        botB.SetupGet(x => x.Name).Returns("Bot B");

        gameServer.RegisterPlayer(botA.Object);
        gameServer.RegisterPlayer(botB.Object);

        gameServer.SubmitMove(playerA, 0, 0);

        var tournamentRef = gameServer.Tournament;

        await gameServer.StartTournamentAsync(tournamentRef);

        var match = tournamentRef.Matches.First();

        match.Status.ShouldBe(MatchStatus.Finished);
        match.WinnerMark.ShouldNotBe(Mark.Empty);
        match.EndTime.ShouldNotBeNull();

        var winnerId = match.WinnerMark == Mark.X ? match.PlayerA : match.PlayerB;
        var loserId = match.WinnerMark == Mark.X ? match.PlayerB : match.PlayerA;

        _leaderboardCalls.ShouldContain((winnerId, MatchScore.Win));
        _leaderboardCalls.ShouldContain((loserId, MatchScore.Walkover));

        var winner = tournamentRef.Leaderboard.First(x => x.PlayerId == winnerId);
        var loser = tournamentRef.Leaderboard.First(x => x.PlayerId == loserId);

        winner.Wins.ShouldBe((uint)2);
        loser.Walkovers.ShouldBe((uint)2);
    }

    [Fact]
    public async Task SignalR_ShouldSendMatchStartedMessageToPlayers()
    {
        var clientProxyMock = new Mock<IClientProxy>();

        var clientsMock = new Mock<IHubClients>();
        clientsMock.Setup(c => c.User(It.IsAny<string>())).Returns(clientProxyMock.Object);
        clientsMock.Setup(c => c.Group(It.IsAny<string>())).Returns(clientProxyMock.Object);

        var hubContextMock = new Mock<IHubContext<TournamentHub>>();
        hubContextMock.Setup(c => c.Clients).Returns(clientsMock.Object);

        var tournamentId = Guid.NewGuid();
        var tournament = new Tournament(tournamentId, "SignalR Tournament", 1);
        var leaderboardLog = new List<(Guid, MatchScore)>();

        var server = new GameServer(tournament, hubContextMock.Object,
            (id, score) => leaderboardLog.Add((id, score)),
            () => { },
            TimeSpan.FromSeconds(1)
        );

        var playerAId = Guid.NewGuid();
        var playerBId = Guid.NewGuid();

        var botA = new Mock<IPlayerBot>();
        var botB = new Mock<IPlayerBot>();
        botA.SetupGet(x => x.Id).Returns(playerAId);
        botA.SetupGet(x => x.Name).Returns("Bot A");
        botB.SetupGet(x => x.Id).Returns(playerBId);
        botB.SetupGet(x => x.Name).Returns("Bot B");

        server.RegisterPlayer(botA.Object);
        server.RegisterPlayer(botB.Object);

        server.SubmitMove(playerAId, 0, 0);

        await server.StartTournamentAsync(tournament);

        var firstMatch = tournament.Matches.First();
        var secondMatch = tournament.Matches.Last();

        botA.Verify(b => b.OnMatchStarted(firstMatch.Id, playerAId, playerBId, Mark.X, true), Times.Once);
        botB.Verify(b => b.OnMatchStarted(firstMatch.Id, playerBId, playerAId, Mark.O, false), Times.Once);

        botA.Verify(b => b.OnMatchStarted(secondMatch.Id, playerAId, playerBId, Mark.O, false), Times.Once);
        botB.Verify(b => b.OnMatchStarted(secondMatch.Id, playerBId, playerAId, Mark.X, true), Times.Once);
    }

    [Fact]
    public async Task BothBotsPlayEachMatch_AndWinOneEach()
    {
        var hubContextMock = new Mock<IHubContext<TournamentHub>>();
        var clientProxyMock = new Mock<IClientProxy>();
        var leaderboard = new List<(Guid playerId, MatchScore score)>();

        var clientsMock = new Mock<IHubClients>();
        clientsMock.Setup(c => c.User(It.IsAny<string>())).Returns(clientProxyMock.Object);
        clientsMock.Setup(c => c.Group(It.IsAny<string>())).Returns(clientProxyMock.Object);
        hubContextMock.Setup(c => c.Clients).Returns(clientsMock.Object);

        var tournament = new Tournament(Guid.NewGuid(), "Match Duel", 1);
        var server = new GameServer(
            tournament,
            hubContextMock.Object,
            (id, score) =>
            {
                leaderboard.Add((id, score));
                tournament.AgreggateScoreToPlayer(id, score);
            },
            () => { },
            TimeSpan.FromSeconds(2));

        var botAId = Guid.NewGuid();
        var botBId = Guid.NewGuid();

        var botA = new Mock<IPlayerBot>();
        var botB = new Mock<IPlayerBot>();

        botA.SetupGet(x => x.Id).Returns(botAId);
        botA.SetupGet(x => x.Name).Returns("Bot A");

        botB.SetupGet(x => x.Id).Returns(botBId);
        botB.SetupGet(x => x.Name).Returns("Bot B");

        server.RegisterPlayer(botA.Object);
        server.RegisterPlayer(botB.Object);

        // Match 1: Bot A vs Bot B — Bot A
        server.SubmitMove(botAId, 0, 0);
        server.SubmitMove(botBId, 1, 1);
        server.SubmitMove(botAId, 0, 1);
        server.SubmitMove(botBId, 2, 2);
        server.SubmitMove(botAId, 0, 2);

        // Match 2: Bot B vs Bot A — Bot B
        server.SubmitMove(botBId, 0, 0);
        server.SubmitMove(botAId, 1, 1);
        server.SubmitMove(botBId, 0, 1);
        server.SubmitMove(botAId, 2, 2);
        server.SubmitMove(botBId, 0, 2);

        await server.StartTournamentAsync(tournament);

        tournament.Status.ShouldBe(TournamentStatus.Finished);

        var match1 = tournament.Matches.First();
        var match2 = tournament.Matches.Last();

        match1.Status.ShouldBe(MatchStatus.Finished);
        match2.Status.ShouldBe(MatchStatus.Finished);

        match1.WinnerMark.ShouldBe(Mark.O);
        match2.WinnerMark.ShouldBe(Mark.O);

        var winner1 = match1.PlayerA; // Bot A
        var winner2 = match2.PlayerA; // Bot B

        winner1.ShouldNotBe(winner2);

        var lbA = tournament.Leaderboard.First(p => p.PlayerId == botAId);
        var lbB = tournament.Leaderboard.First(p => p.PlayerId == botBId);

        lbA.GamesPlayed.ShouldBe(2);
        lbB.GamesPlayed.ShouldBe(2);

        lbA.Wins.ShouldBe((uint)1);
        lbA.Walkovers.ShouldBe((uint)0);
        lbB.Wins.ShouldBe((uint)1);
        lbB.Walkovers.ShouldBe((uint)0);
    }
}
