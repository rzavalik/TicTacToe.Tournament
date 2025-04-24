using Moq;
using TicTacToe.Tournament.Server.App;
using TicTacToe.Tournament.Server.Interfaces;

namespace TicTacToe.Tournament.Server.Tests;

public class TournamentHostedServiceTests
{
    [Fact]
    public async Task ExecuteAsync_ShouldLoadTournamentsFromStorage()
    {
        var tournamentIds = new List<Guid> { Guid.NewGuid(), Guid.NewGuid() };

        var storageMock = new Mock<IAzureStorageService>();
        storageMock.Setup(s => s.ListTournamentsAsync()).ReturnsAsync(tournamentIds);

        var managerMock = new Mock<ITournamentManager>();

        var sut = new TournamentHostedService(managerMock.Object, storageMock.Object);

        using var cts = new CancellationTokenSource(TimeSpan.FromMilliseconds(100));

        await sut.StartAsync(cts.Token);

        storageMock.Verify(s => s.ListTournamentsAsync(), Times.Once);
        foreach (var id in tournamentIds)
        {
            managerMock.Verify(m => m.InitializeTournamentAsync(id, null), Times.Once);
        }
    }

    [Fact]
    public async Task ExecuteAsync_WhenInitializeFails_ShouldContinueOtherTournaments()
    {
        var id1 = Guid.NewGuid();
        var id2 = Guid.NewGuid();

        var storageMock = new Mock<IAzureStorageService>();
        storageMock.Setup(s => s.ListTournamentsAsync()).ReturnsAsync(new[] { id1, id2 });

        var managerMock = new Mock<ITournamentManager>();
        managerMock.Setup(m => m.InitializeTournamentAsync(id1, null)).ThrowsAsync(new Exception("boom"));
        managerMock.Setup(m => m.InitializeTournamentAsync(id2, null)).Returns(Task.CompletedTask);

        var sut = new TournamentHostedService(managerMock.Object, storageMock.Object);

        using var cts = new CancellationTokenSource(TimeSpan.FromMilliseconds(100));

        await sut.StartAsync(cts.Token);

        storageMock.Verify(s => s.ListTournamentsAsync(), Times.Once);
        managerMock.Verify(m => m.InitializeTournamentAsync(id1, null), Times.Once);
        managerMock.Verify(m => m.InitializeTournamentAsync(id2, null), Times.Once);
    }
}
