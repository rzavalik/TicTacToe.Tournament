using Microsoft.AspNetCore.SignalR;
using Moq;
using Shouldly;
using System.Collections.Concurrent;
using TicTacToe.Tournament.Models;
using TicTacToe.Tournament.Models.Interfaces;
using TicTacToe.Tournament.Server.Bots;
using TicTacToe.Tournament.Server.Hubs;
using TicTacToe.Tournament.Server.Interfaces;
using TicTacToe.Tournament.Server.Services;

namespace TicTacToe.Tournament.Server.Tests;

public class TournamentManagerTests
{
    private readonly Mock<IHubContext<TournamentHub>> _hubContextMock = new();
    private readonly Mock<IClientProxy> _clientProxyMock = new();
    private readonly Mock<IAzureStorageService> _storageServiceMock = new();

    private Models.Tournament GetTournament(
        Guid? tournamentId = null,
        Guid? championPlayerId = null,
        DateTime? endTime = null,
        DateTime? startTime = null,
        string name = "Test Tournament",
        Dictionary<Guid, string>? registeredPlayers = null,
        TournamentStatus tournamentStatus = TournamentStatus.Planned)
    {
        tournamentId ??= Guid.NewGuid();

        return new Models.Tournament
        {
            Id = tournamentId.Value,
            Champion = championPlayerId,
            EndTime = endTime,
            Matches = new List<Models.Match>(),
            Leaderboard = new Dictionary<Guid, int>(),
            Name = name,
            RegisteredPlayers = registeredPlayers ?? new Dictionary<Guid, string>(),
            StartTime = startTime,
            Status = tournamentStatus
        };
    }

    private DummyPlayerBot GetDummyBot(
        Guid? playerId = null,
        string? name = null)
    {
        playerId ??= Guid.NewGuid();
        name ??= "DummyBot";

        return new DummyPlayerBot(
            playerId.Value!,
            name!);
    }

    private ITournamentManager MakeSut(
        Models.Tournament tournament,
        DummyPlayerBot dummyBot)
    {
        var playersInfo = tournament?
            .RegisteredPlayers?
            .Select(p => new PlayerInfo
            {
                PlayerId = p.Key,
                Name = p.Value
            })
            .ToList();

        var playerTournamentMap = tournament?
            .RegisteredPlayers?
            .ToDictionary(p => p.Key, p => tournament?.Id ?? Guid.Empty);

        _storageServiceMock
            .Setup(s => s.LoadTournamentStateAsync(tournament!.Id))
            .ReturnsAsync((tournament, playersInfo, playerTournamentMap, new System.Collections.Concurrent.ConcurrentDictionary<Guid, System.Collections.Concurrent.ConcurrentQueue<(int Row, int Col)>>()));

        var clients = new Mock<IHubClients>();
        clients.Setup(c => c.Group(It.IsAny<string>())).Returns(_clientProxyMock.Object);
        clients.Setup(c => c.All).Returns(_clientProxyMock.Object);
        _hubContextMock.Setup(c => c.Clients).Returns(clients.Object);

        return new TournamentManager(_hubContextMock.Object, _storageServiceMock.Object);
    }

    [Fact]
    public async Task InitializeTournamentAsync_ShouldReuseSavedTournament()
    {
        var player1 = new KeyValuePair<Guid, string>(Guid.NewGuid(), "Player1");
        var player2 = new KeyValuePair<Guid, string>(Guid.NewGuid(), "Player2");
        var registeredPlayers = new Dictionary<Guid, string>(
        [
            player1,
            player2
        ]);

        var tournament = GetTournament(
            registeredPlayers: registeredPlayers
        );

        var dummyBot = GetDummyBot();

        var sut = MakeSut(tournament, dummyBot);

        await sut.InitializeTournamentAsync(tournament.Id);

        _storageServiceMock.Verify(x => x.SaveTournamentStateAsync(
            tournament.Id,
            tournament,
            It.IsAny<Dictionary<Guid, IPlayerBot>>(),
            It.IsAny<Dictionary<Guid, Guid>>(),
            It.IsAny<ConcurrentDictionary<Guid, ConcurrentQueue<(int, int)>>>()
        ), Times.Once);
    }

    [Fact]
    public async Task InitializeTournamentAsync_ShouldCreateTournament()
    {
        var player1 = new KeyValuePair<Guid, string>(Guid.NewGuid(), "Player1");
        var player2 = new KeyValuePair<Guid, string>(Guid.NewGuid(), "Player2");
        var registeredPlayers = new Dictionary<Guid, string>(
        [
            player1,
            player2
        ]);

        var tournament = GetTournament(
            registeredPlayers: registeredPlayers
        );

        var dummyBot = GetDummyBot();

        _storageServiceMock.Reset();
        _storageServiceMock
            .Setup(s => s.LoadTournamentStateAsync(tournament!.Id))
            .ReturnsAsync((null, null, null, null));

        var sut = MakeSut(tournament, dummyBot);

        await sut.InitializeTournamentAsync(tournament.Id);

        _storageServiceMock.Verify(x => x.SaveTournamentStateAsync(
            tournament.Id,
            tournament,
            It.IsAny<Dictionary<Guid, IPlayerBot>>(),
            It.IsAny<Dictionary<Guid, Guid>>(),
            It.IsAny<ConcurrentDictionary<Guid, ConcurrentQueue<(int, int)>>>()
        ), Times.Once);
    }

    [Fact]
    public async Task RegisterPlayerAsync_ShouldAddPlayerToTournament()
    {
        var tournament = GetTournament();
        var dummyBot = GetDummyBot();
        var sut = MakeSut(tournament, dummyBot);

        await sut.InitializeTournamentAsync(tournament.Id);
        await sut.RegisterPlayerAsync(tournament.Id, dummyBot);

        var loaded = await sut.GetOrLoadTournamentAsync(tournament.Id);
        loaded!.RegisteredPlayers.ShouldContainKey(dummyBot.Id);
        loaded.RegisteredPlayers[dummyBot.Id].ShouldBe(dummyBot.Name);
    }

    [Fact]
    public async Task TournamentExists_WhenCalled_ShouldReturnTrueAfterInitialization()
    {
        var tournament = GetTournament();
        var dummyBot = GetDummyBot();
        var sut = MakeSut(tournament, dummyBot);

        await sut.InitializeTournamentAsync(tournament.Id);
        sut.TournamentExists(tournament.Id).ShouldBeTrue();
    }

    [Fact]
    public async Task GetTournament_WhenTournamentExists_ShouldReturnTournament()
    {
        var tournament = GetTournament();
        var dummyBot = GetDummyBot();
        var sut = MakeSut(tournament, dummyBot);

        await sut.InitializeTournamentAsync(tournament.Id);
        var result = sut.GetTournament(tournament.Id);

        result.ShouldNotBeNull();
        result!.Id.ShouldBe(tournament.Id);
    }

    [Fact]
    public async Task GetGameServerForPlayer_WhenPlayerIsRegistered_ShouldReturnGameServer()
    {
        var tournament = GetTournament();
        var dummyBot = GetDummyBot();
        var sut = MakeSut(tournament, dummyBot);

        await sut.InitializeTournamentAsync(tournament.Id);
        await sut.RegisterPlayerAsync(tournament.Id, dummyBot);

        var server = sut.GetGameServerForPlayer(dummyBot.Id);
        server.ShouldNotBeNull();
    }

    [Fact]
    public async Task CancelTournament_ShouldSetStatusToCancelled()
    {
        var tournament = GetTournament();
        var dummyBot = GetDummyBot();
        var sut = MakeSut(tournament, dummyBot);

        await sut.InitializeTournamentAsync(tournament.Id);

        await sut.CancelTournament(tournament.Id);

        var cancelled = sut.GetTournament(tournament.Id);
        cancelled.ShouldNotBeNull();
        cancelled!.Status.ShouldBe(TournamentStatus.Cancelled);
    }
}
