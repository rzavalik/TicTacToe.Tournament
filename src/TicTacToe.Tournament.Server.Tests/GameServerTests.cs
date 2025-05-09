﻿using System.Collections.Concurrent;
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

        var tournament = new Models.Tournament
        {
            Id = _tournamentId,
            Name = "Test Tournament",
            RegisteredPlayers = new Dictionary<Guid, string>(),
            Leaderboard = new Dictionary<Guid, int>(),
            Matches = new List<Models.Match>(),
            MatchRepetition = 1
        };

        return new GameServer(
            tournament,
            _hubContextMock.Object,
            (id, score) => { _leaderboardCalls.Add((id, score)); });
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
        tournament.Leaderboard.ShouldContainKey(playerId);
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
        moves.ShouldBeOfType<ConcurrentDictionary<Guid, ConcurrentQueue<(int, int)>>>();
    }

    [Fact]
    public void LoadPendingMoves_ShouldOverwriteExisting()
    {
        var sut = MakeSut();

        var playerId = Guid.NewGuid();
        var existing = sut.GetPendingMoves();
        existing[playerId] = new ConcurrentQueue<(int, int)>();
        existing[playerId].Enqueue((1, 1));

        var newQueue = new ConcurrentQueue<(int, int)>();
        newQueue.Enqueue((2, 2));
        var replacement = new ConcurrentDictionary<Guid, ConcurrentQueue<(int, int)>>();
        replacement[playerId] = newQueue;

        sut.LoadPendingMoves(replacement);

        sut.GetPendingMoves()[playerId].TryDequeue(out var move);
        move.ShouldBe((2, 2));
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
        move.ShouldBe((1, 2));
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

    private Models.Tournament GetTournament(IGameServer gameServer)
    {
        return ((GameServer)gameServer).Tournament;
    }
}
