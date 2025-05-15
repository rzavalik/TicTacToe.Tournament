using System.Collections.Concurrent;
using Microsoft.AspNetCore.SignalR;
using Moq;
using Shouldly;
using TicTacToe.Tournament.Models;
using TicTacToe.Tournament.Models.Interfaces;
using TicTacToe.Tournament.Server.Hubs;

namespace TicTacToe.Tournament.Server.Tests;

public class GameServerTests
{
    private readonly Guid _tournamentId = Guid.NewGuid();
    private readonly Mock<IHubContext<TournamentHub>> _hubContextMock = new();
    private readonly Mock<IClientProxy> _clientProxyMock = new();
    private readonly Mock<IPlayerBot> _botMock = new();
    private readonly List<(Guid playerId, MatchScore score)> _leaderboardCalls = new();

    private IGameServer MakeSut()
    {
        var clientsMock = new Mock<IHubClients>();
        clientsMock.Setup(c => c.User(It.IsAny<string>())).Returns(_clientProxyMock.Object);
        clientsMock.Setup(c => c.Group(It.IsAny<string>())).Returns(_clientProxyMock.Object);
        _hubContextMock.Setup(c => c.Clients).Returns(clientsMock.Object);

        var tournament = new Models.Tournament(_tournamentId, "Test Tournament", 2);

        return new GameServer(
            tournament,
            _hubContextMock.Object,
            (id, score) => { _leaderboardCalls.Add((id, score)); },
            () => { },
            Server.GameServer.DefaultTimeout
        );
    }

    private Models.Tournament GetTournament(IGameServer gameServer)
    {
        return ((GameServer)gameServer).Tournament;
    }

    [Fact]
    public void RegisterPlayer_ShouldAddToRegisteredPlayersAndInitializeLeaderboard()
    {
        var sut = MakeSut();
        var playerId = Guid.NewGuid();
        _botMock.SetupGet(b => b.Id).Returns(playerId);
        _botMock.SetupGet(b => b.Name).Returns("BotTest");

        sut.RegisterPlayer(_botMock.Object);

        var tournament = GetTournament(sut);
        tournament.RegisteredPlayers.ShouldContainKey(playerId);
        tournament.Leaderboard.ShouldContain(p => p.PlayerId == playerId);
    }

    [Fact]
    public void RegisterPlayer_WhenTwoPlayers_ShouldGenerateMatches()
    {
        var sut = MakeSut();

        var bot1 = new Mock<IPlayerBot>();
        var bot2 = new Mock<IPlayerBot>();
        bot1.SetupGet(b => b.Id).Returns(Guid.NewGuid());
        bot2.SetupGet(b => b.Id).Returns(Guid.NewGuid());
        bot1.SetupGet(b => b.Name).Returns("Bot1");
        bot2.SetupGet(b => b.Name).Returns("Bot2");

        sut.RegisterPlayer(bot1.Object);
        sut.RegisterPlayer(bot2.Object);

        var tournament = GetTournament(sut);
        tournament.Matches.ShouldNotBeEmpty();
    }

    [Fact]
    public void RegisterPlayer_WhenNewPlayer_ShouldAddToDictionary()
    {
        var sut = MakeSut();
        var playerId = Guid.NewGuid();
        _botMock.SetupGet(b => b.Id).Returns(playerId);
        _botMock.SetupGet(b => b.Name).Returns("BotTest");

        sut.RegisterPlayer(_botMock.Object);

        sut.RegisteredPlayers.Keys.ShouldContain(playerId);
    }

    [Fact]
    public void GetPendingMoves_ShouldReturnSameInstance()
    {
        var sut = MakeSut();

        var moves = sut.GetPendingMoves();

        moves.ShouldNotBeNull();
        moves.ShouldBeOfType<ConcurrentDictionary<Guid, ConcurrentQueue<(byte, byte)>>>();
    }

    [Fact]
    public void LoadPendingMoves_ShouldOverwriteExisting()
    {
        var sut = MakeSut();

        var playerId = Guid.NewGuid();
        var existing = sut.GetPendingMoves();
        existing[playerId] = new ConcurrentQueue<(byte, byte)>();
        existing[playerId].Enqueue((1, 1));

        var newQueue = new ConcurrentQueue<(byte, byte)>();
        newQueue.Enqueue((2, 2));
        var replacement = new ConcurrentDictionary<Guid, ConcurrentQueue<(byte, byte)>>();
        replacement[playerId] = newQueue;

        sut.LoadPendingMoves(replacement);

        sut.GetPendingMoves()[playerId].TryDequeue(out var move);
        move.ShouldBe(((byte)2, (byte)2));
    }

    [Fact]
    public void SubmitMove_ShouldQueueMoveForPlayer()
    {
        var sut = MakeSut();
        var playerId = Guid.NewGuid();

        sut.SubmitMove(playerId, 1, 2);

        var queue = sut.GetPendingMoves()[playerId];
        queue.ShouldNotBeNull();
        queue.TryDequeue(out var move).ShouldBeTrue();
        move.ShouldBe(((byte)1, (byte)2));
    }

    [Fact]
    public void GetBotById_WhenRegistered_ShouldReturnBot()
    {
        var sut = MakeSut();
        var playerId = Guid.NewGuid();
        _botMock.SetupGet(b => b.Id).Returns(playerId);
        _botMock.SetupGet(b => b.Name).Returns("BotA");

        sut.RegisterPlayer(_botMock.Object);

        var bot = sut.GetBotById(playerId);
        bot.ShouldNotBeNull();
        bot.ShouldBe(_botMock.Object);
    }

    [Fact]
    public void GetBotById_WhenNotRegistered_ShouldReturnNull()
    {
        var sut = MakeSut();
        var result = sut.GetBotById(Guid.NewGuid());

        result.ShouldBeNull();
    }

    [Fact]
    public void GenerateMatches_ShouldCreateCorrectNumberOfMatches()
    {
        var sut = MakeSut();
        var tournament = GetTournament(sut);

        var bot1 = new Mock<IPlayerBot>();
        var bot2 = new Mock<IPlayerBot>();
        bot1.SetupGet(b => b.Id).Returns(Guid.NewGuid());
        bot2.SetupGet(b => b.Id).Returns(Guid.NewGuid());
        bot1.SetupGet(b => b.Name).Returns("Bot1");
        bot2.SetupGet(b => b.Name).Returns("Bot2");

        sut.RegisterPlayer(bot1.Object);
        sut.RegisterPlayer(bot2.Object);

        var matchCount = tournament.Matches.Count;
        matchCount.ShouldBe(4);
    }
}
