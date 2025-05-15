namespace TicTacToe.Tournament.Server.Tests
{
    using System.Collections.Concurrent;
    using Microsoft.AspNetCore.SignalR;
    using Moq;
    using Shouldly;
    using TicTacToe.Tournament.Models;
    using TicTacToe.Tournament.Server;
    using TicTacToe.Tournament.Server.Bots;
    using TicTacToe.Tournament.Server.Hubs;
    using TicTacToe.Tournament.Server.Interfaces;
    using Xunit;

    public class TournamentManagerTests
    {
        private TournamentManager MakeSut(Guid? fixedTournamentId = null)
        {
            fixedTournamentId = fixedTournamentId ?? Guid.NewGuid();
            var hubContextMock = new Mock<IHubContext<TournamentHub>>();
            var clientsMock = new Mock<IHubClients>();
            var clientProxyMock = new Mock<IClientProxy>();

            clientsMock.Setup(c => c.All).Returns(clientProxyMock.Object);
            clientsMock.Setup(c => c.Group(It.IsAny<string>())).Returns(clientProxyMock.Object);
            hubContextMock.Setup(c => c.Clients).Returns(clientsMock.Object);
            hubContextMock.Setup(c => c.Clients.User(It.IsAny<string>())).Returns(clientProxyMock.Object);

            var storageServiceMock = new Mock<IAzureStorageService>();
            storageServiceMock.Setup(s => s.ListTournamentsAsync()).ReturnsAsync(new List<Guid>());
            storageServiceMock.Setup(s => s.LoadTournamentStateAsync(fixedTournamentId.Value))
                .ReturnsAsync(
                    (
                        new Models.Tournament(fixedTournamentId.Value, "Loaded Tournament", 2),
                        new List<PlayerInfo>(),
                        new Dictionary<Guid, Guid>(),
                        new ConcurrentDictionary<Guid, ConcurrentQueue<(byte, byte)>>()
                    )
                );
            storageServiceMock.Setup(s => s.SaveTournamentStateAsync(It.IsAny<TournamentContext>())).Returns(Task.CompletedTask);
            storageServiceMock.Setup(s => s.DeleteTournamentAsync(fixedTournamentId.Value)).Returns(Task.CompletedTask);
            storageServiceMock.Setup(s => s.TournamentExistsAsync(fixedTournamentId.Value)).ReturnsAsync(true);

            return new TournamentManager(hubContextMock.Object, storageServiceMock.Object);
        }

        [Fact]
        public async Task RegisterPlayerAsync_ShouldAddPlayerToTournament()
        {
            var playerId = Guid.NewGuid();
            var tournamentId = Guid.NewGuid();
            var bot = new DummyPlayerBot(playerId, "Test Player", tournamentId);

            var sut = MakeSut(tournamentId);

            await sut.InitializeTournamentAsync(tournamentId, "Test Tournament", 1);
            await sut.RegisterPlayerAsync(tournamentId, bot);

            var tournament = sut.GetTournament(tournamentId);
            tournament.ShouldNotBeNull();
            tournament.RegisteredPlayers.ContainsKey(playerId).ShouldBeTrue();
        }

        [Fact]
        public async Task TournamentExists_WhenCalled_ShouldReturnTrueAfterInitialization()
        {
            var tournamentId = Guid.NewGuid();
            var sut = MakeSut(tournamentId);

            var result = await sut.TournamentExistsAsync(tournamentId);

            result.ShouldBeTrue();
        }

        [Fact]
        public async Task GetTournament_WhenTournamentExists_ShouldReturnTournament()
        {
            var tournamentId = Guid.NewGuid();
            var sut = MakeSut(tournamentId);

            await sut.InitializeTournamentAsync(tournamentId, "Test Tournament", 1);

            var tournament = sut.GetTournament(tournamentId);

            tournament.ShouldNotBeNull();
            tournament.Id.ShouldBe(tournamentId);
            tournament.Name.ShouldBe("Loaded Tournament");
        }

        [Fact]
        public async Task CancelTournament_ShouldSetStatusToCancelled()
        {
            var sut = MakeSut();
            var tournamentId = Guid.NewGuid();

            await sut.InitializeTournamentAsync(tournamentId, "Test Tournament", 1);
            await sut.CancelTournamentAsync(tournamentId);

            var tournament = sut.GetTournament(tournamentId);
            tournament.Status.ShouldBe(TournamentStatus.Cancelled);
        }

        [Fact]
        public async Task StartTournamentAsync_WithNoPlayers_ShouldSetStatusFinished()
        {
            var tournamentId = Guid.NewGuid();
            var sut = MakeSut(tournamentId);

            await sut.InitializeTournamentAsync(tournamentId, "Test Tournament", 1);
            await sut.StartTournamentAsync(tournamentId);

            var tournament = sut.GetTournament(tournamentId);
            tournament.Status.ShouldBe(TournamentStatus.Finished);
        }

        [Fact]
        public async Task StartTournamentAsync_ShouldSetStatusOngoing()
        {
            var tournamentId = Guid.NewGuid();
            var sut = MakeSut(tournamentId);

            var player1 = Guid.NewGuid();
            var player2 = Guid.NewGuid();

            await sut.RegisterPlayerAsync(tournamentId, new RemotePlayerBot(player1, "BotA", new Mock<IClientProxy>().Object, tournamentId));
            await sut.RegisterPlayerAsync(tournamentId, new RemotePlayerBot(player2, "BotB", new Mock<IClientProxy>().Object, tournamentId));
            await sut.InitializeTournamentAsync(tournamentId, "Test Tournament", 1);
            Task.Run(() => sut.StartTournamentAsync(tournamentId));
            await Task.Delay(100);
            var tournament = sut.GetTournament(tournamentId);
            tournament.Status.ShouldBe(TournamentStatus.Ongoing);
        }

        [Fact]
        public async Task SubmitMove_ShouldNotThrowException()
        {
            var sut = MakeSut();
            var tournamentId = Guid.NewGuid();
            var playerId = Guid.NewGuid();
            var bot = new DummyPlayerBot(playerId, "Test Player", tournamentId);

            await sut.InitializeTournamentAsync(tournamentId, "Test Tournament", 1);
            await sut.RegisterPlayerAsync(tournamentId, bot);

            var exception = await Record.ExceptionAsync(async () =>
            {
                await sut.SubmitMoveAsync(tournamentId, playerId, 0, 0);
            });

            exception.ShouldBeNull();
        }

        [Fact]
        public async Task RenameTournamentAsync_ShouldChangeName()
        {
            var tournamentId = Guid.NewGuid();
            var sut = MakeSut(tournamentId);

            await sut.InitializeTournamentAsync(tournamentId, "Old Name", 1);
            await sut.RenameTournamentAsync(tournamentId, "New Name");

            var tournament = sut.GetTournament(tournamentId);
            tournament.Name.ShouldBe("New Name");
        }

        [Fact]
        public async Task RenamePlayerAsync_ShouldChangePlayerName()
        {
            var sut = MakeSut();
            var tournamentId = Guid.NewGuid();
            var playerId = Guid.NewGuid();
            var bot = new DummyPlayerBot(playerId, "OldPlayerName", tournamentId);

            await sut.InitializeTournamentAsync(tournamentId, "Test Tournament", 1);
            await sut.RegisterPlayerAsync(tournamentId, bot);

            await sut.RenamePlayerAsync(tournamentId, playerId, "NewPlayerName");

            var tournament = sut.GetTournament(tournamentId);
            tournament.RegisteredPlayers[playerId].ShouldBe("NewPlayerName");
        }

        [Fact]
        public async Task DeleteTournamentAsync_ShouldRemoveTournament()
        {
            var sut = MakeSut();
            var tournamentId = Guid.NewGuid();

            await sut.InitializeTournamentAsync(tournamentId, "Test Tournament", 1);
            await sut.DeleteTournamentAsync(tournamentId);

            var tournament = sut.GetTournament(tournamentId);
            tournament.ShouldBeNull();
        }

        [Fact]
        public async Task LoadFromDataSourceAsync_ShouldNotThrow()
        {
            var sut = MakeSut();

            var exception = await Record.ExceptionAsync(async () =>
            {
                await sut.LoadFromDataSourceAsync();
            });

            exception.ShouldBeNull();
        }

        [Fact]
        public async Task SaveTournamentAsync_ShouldNotThrow()
        {
            var sut = MakeSut();
            var tournament = new Models.Tournament(Guid.NewGuid(), "Saving Tournament", 1);

            var exception = await Record.ExceptionAsync(async () =>
            {
                await sut.SaveTournamentAsync(tournament);
            });

            exception.ShouldBeNull();
        }

        [Fact]
        public async Task SubmitMove_WhenTournamentNotFound_ShouldNotThrow()
        {
            var sut = MakeSut();

            var exception = await Record.ExceptionAsync(async () =>
            {
                await sut.SubmitMoveAsync(Guid.NewGuid(), Guid.NewGuid(), 1, 1);
            });

            exception.ShouldBeNull();
        }

        [Fact]
        public async Task GetLeaderboard_ShouldReturnCorrectScores()
        {
            var tournamentId = Guid.NewGuid();
            var hubContextMock = new Mock<IHubContext<TournamentHub>>();
            var clientsMock = new Mock<IHubClients>();
            var clientProxyMock = new Mock<IClientProxy>();

            clientsMock.Setup(c => c.All).Returns(clientProxyMock.Object);
            clientsMock.Setup(c => c.Group(It.IsAny<string>())).Returns(clientProxyMock.Object);
            hubContextMock.Setup(c => c.Clients).Returns(clientsMock.Object);
            hubContextMock.Setup(c => c.Clients.User(It.IsAny<string>())).Returns(clientProxyMock.Object);

            var storageServiceMock = new Mock<IAzureStorageService>();
            storageServiceMock.Setup(s => s.ListTournamentsAsync()).ReturnsAsync(new List<Guid>());
            storageServiceMock.Setup(s => s.LoadTournamentStateAsync(tournamentId))
                .ReturnsAsync(
                    (
                        new TicTacToe.Tournament.Models.Tournament(tournamentId, "Test", 1),
                        new List<PlayerInfo>(),
                        new Dictionary<Guid, Guid>(),
                        new System.Collections.Concurrent.ConcurrentDictionary<Guid, System.Collections.Concurrent.ConcurrentQueue<(byte, byte)>>()
                    )
                );
            storageServiceMock.Setup(s => s.SaveTournamentStateAsync(It.IsAny<TournamentContext>())).Returns(Task.CompletedTask);
            storageServiceMock.Setup(s => s.TournamentExistsAsync(tournamentId)).ReturnsAsync(true);

            var manager = new TournamentManager(hubContextMock.Object, storageServiceMock.Object);
            await manager.InitializeTournamentAsync(tournamentId, "Test", 1);

            var playerA = Guid.NewGuid();
            var playerB = Guid.NewGuid();

            var botA = new DummyPlayerBot(playerA, "Bot A", tournamentId);
            var botB = new DummyPlayerBot(playerB, "Bot B", tournamentId);

            await manager.RegisterPlayerAsync(tournamentId, botA);
            await manager.RegisterPlayerAsync(tournamentId, botB);

            var tournament = manager.GetTournament(tournamentId)!;

            tournament.Matches.Add(new Models.Match(playerA, playerB)
            {
                Status = MatchStatus.Finished,
                WinnerMark = Mark.X // Bot A wins
            });

            tournament.Matches.Add(new Models.Match(playerA, playerB)
            {
                Status = MatchStatus.Finished,
                WinnerMark = null // Draw
            });

            var leaderboard = manager.GetLeaderboard(tournamentId);

            leaderboard[playerA].ShouldBe((int)MatchScore.Win + (int)MatchScore.Draw);
            leaderboard[playerB].ShouldBe((int)MatchScore.Lose + (int)MatchScore.Draw);
        }
    }
}