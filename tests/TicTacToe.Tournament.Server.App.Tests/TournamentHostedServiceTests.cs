namespace TicTacToe.Tournament.Server.Tests
{
    using Moq;
    using TicTacToe.Tournament.Server.App;
    using TicTacToe.Tournament.Server.Interfaces;
    using Xunit;

    public class TournamentHostedServiceTests
    {
        [Fact]
        public async Task StartAsync_ShouldCallLoadFromDataSource()
        {
            var managerMock = new Mock<ITournamentManager>();
            var storageMock = new Mock<IAzureStorageService>(); // Not used anymore

            var sut = new TournamentHostedService(managerMock.Object, storageMock.Object);

            using var cts = new CancellationTokenSource(TimeSpan.FromMilliseconds(100));

            await sut.StartAsync(cts.Token);

            managerMock.Verify(m => m.LoadFromDataSourceAsync(), Times.Once);
        }
    }
}