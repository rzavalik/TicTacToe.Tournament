using global::TicTacToe.Tournament.Models;
using global::TicTacToe.Tournament.Models.DTOs;
using global::TicTacToe.Tournament.Server.Hubs;
using global::TicTacToe.Tournament.Server.Interfaces;
using Microsoft.AspNetCore.SignalR;
using Moq;
using Shouldly;

namespace TicTacToe.Tournament.Server.Tests.Hubs;

public class TournamentHubTests
{
    private TournamentHub MakeSut(
        Mock<ITournamentManager> managerMock,
        out Mock<IHubCallerClients> clientsMock,
        out Mock<IClientProxy> callerMock,
        out Mock<IGroupManager> groupManagerMock,
        out Mock<HubCallerContext> contextMock,
        string? userId = null)
    {
        clientsMock = new Mock<IHubCallerClients>();
        callerMock = new Mock<IClientProxy>();
        groupManagerMock = new Mock<IGroupManager>();
        contextMock = new Mock<HubCallerContext>();

        clientsMock.Setup(c => c.Caller).Returns(callerMock.Object);
        clientsMock.Setup(c => c.All).Returns(callerMock.Object);

        if (userId != null)
            contextMock.Setup(c => c.UserIdentifier).Returns(userId);

        var sut = new TournamentHub(managerMock.Object)
        {
            Clients = clientsMock.Object,
            Context = contextMock.Object,
            Groups = groupManagerMock.Object
        };

        return sut;
    }

    [Fact]
    public async Task GetTournament_WhenTournamentExists_ShouldReturnDto()
    {
        var tournamentId = Guid.NewGuid();
        var managerMock = new Mock<ITournamentManager>();
        managerMock.Setup(m => m.GetOrLoadTournamentAsync(tournamentId))
            .ReturnsAsync(new Models.Tournament
            {
                Id = tournamentId,
                Name = "Test",
                Status = TournamentStatus.Planned,
                RegisteredPlayers = new Dictionary<Guid, string>(),
                Leaderboard = new Dictionary<Guid, int>(),
                Matches = new List<Models.Match>()
            });

        var sut = MakeSut(
            managerMock, 
            out _, 
            out _, 
            out _, 
            out _);
        var result = await sut.GetTournament(tournamentId);

        result.ShouldNotBeNull();
        result!.Id.ShouldBe(tournamentId);
        result.Name.ShouldBe("Test");
    }

    [Fact]
    public async Task GetAllTournaments_ShouldReturnSummaries()
    {
        var managerMock = new Mock<ITournamentManager>();
        managerMock.Setup(m => m.GetAllTournaments())
            .Returns(new List<Models.Tournament>
            {
                new Models.Tournament { 
                    Id = Guid.NewGuid(), 
                    Name = "A",
                    Status = TournamentStatus.Planned,
                    RegisteredPlayers = new Dictionary<Guid, string>(),
                    Matches = new List<Models.Match>()
                }
            });

        var sut = MakeSut(
            managerMock, 
            out _, 
            out _, 
            out _, 
            out _);
        var result = await sut.GetAllTournaments();

        result.ShouldNotBeEmpty();
        result.ShouldContain(r => r.Name == "A");
    }

    [Fact]
    public async Task TournamentExists_WhenExists_ShouldReturnTrue()
    {
        var tournamentId = Guid.NewGuid();
        var managerMock = new Mock<ITournamentManager>();
        managerMock.Setup(m => m.GetOrLoadTournamentAsync(tournamentId))
            .ReturnsAsync(new Models.Tournament { Id = tournamentId });

        var sut = MakeSut(
            managerMock, 
            out _, 
            out _, 
            out _,
            out _);
        var exists = sut.TournamentExistsAsync(tournamentId);
        exists.ShouldBeTrue();
    }

    [Fact]
    public async Task TournamentExists_WhenNotExists_ShouldReturnFalse()
    {
        var tournamentId = Guid.NewGuid();
        var managerMock = new Mock<ITournamentManager>();
        managerMock.Setup(m => m.GetOrLoadTournamentAsync(tournamentId))
            .ReturnsAsync((Models.Tournament?)null);

        var sut = MakeSut(
            managerMock, 
            out _, 
            out _, 
            out _, 
            out _);
        var exists = sut.TournamentExistsAsync(tournamentId);
        exists.ShouldBeFalse();
    }

    [Fact]
    public async Task SpectateTournament_ShouldAddConnectionToGroup()
    {
        var tournamentId = Guid.NewGuid();
        var managerMock = new Mock<ITournamentManager>();

        var sut = MakeSut(
            managerMock, 
            out _, 
            out _, 
            out var groupMock, 
            out var contextMock);
        contextMock.Setup(c => c.ConnectionId).Returns("connection-123");

        await sut.SpectateTournament(tournamentId);

        groupMock.Verify(g => g.AddToGroupAsync("connection-123", tournamentId.ToString(), default), Times.Once);
    }

    [Fact]
    public async Task RegisterPlayer_ShouldSendOnRegisteredToCaller()
    {
        var tournamentId = Guid.NewGuid();
        var playerId = Guid.NewGuid();
        var playerName = "BotX";

        var managerMock = new Mock<ITournamentManager>();
        var groupClientProxyMock = new Mock<IClientProxy>();
        var callerClientProxyMock = new Mock<IClientProxy>();

        var clientsMock = new Mock<IHubCallerClients>();
        clientsMock.Setup(c => c.Caller).Returns(callerClientProxyMock.Object);
        clientsMock.Setup(c => c.Group(tournamentId.ToString())).Returns(groupClientProxyMock.Object);

        var groupMock = new Mock<IGroupManager>();
        var contextMock = new Mock<HubCallerContext>();
        contextMock.Setup(c => c.UserIdentifier).Returns(playerId.ToString());
        contextMock.Setup(c => c.ConnectionId).Returns("connection-abc");

        var sut = new TournamentHub(managerMock.Object)
        {
            Clients = clientsMock.Object,
            Groups = groupMock.Object,
            Context = contextMock.Object
        };

        await sut.RegisterPlayer(playerName, tournamentId);

        callerClientProxyMock.Verify(c =>
            c.SendCoreAsync("OnRegistered", It.Is<object[]>(args => (Guid)args[0] == playerId), default),
            Times.Once);

        groupClientProxyMock.Verify(c =>
            c.SendCoreAsync("OnPlayerRegistered", It.Is<object[]>(args => (Guid)args[0] == playerId), default),
            Times.Once);

    }
}
