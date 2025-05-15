namespace TicTacToe.Tournament.Player.Tests
{
    using System;
    using System.Collections.Generic;
    using Shouldly;
    using TicTacToe.Tournament.BasePlayer;
    using TicTacToe.Tournament.Models;
    using TicTacToe.Tournament.Models.DTOs;
    using Xunit;

    public class GameConsoleUITests
    {
        private GameConsoleUI MakeSut() => new GameConsoleUI();

        [Fact]
        public void SetPlayerA_WhenCalled_SetsPlayerA()
        {
            var sut = MakeSut();
            sut.SetPlayerA("Alice");
            sut.PlayerA.ShouldBe("Alice");
        }

        [Fact]
        public void SetPlayerB_WhenCalled_SetsPlayerB()
        {
            var sut = MakeSut();
            sut.SetPlayerB("Bob");
            sut.PlayerB.ShouldBe("Bob");
        }

        [Fact]
        public void SetBoard_WhenCalled_SetsBoard()
        {
            var sut = MakeSut();
            var board = new[]
            {
                [Mark.X, Mark.O, Mark.X],
                [Mark.O, Mark.X, Mark.O],
                new[] { Mark.X, Mark.Empty, Mark.O }
            };
            sut.SetBoard(board);
            sut.Board.ShouldBe(board);
        }

        [Fact]
        public void Duration_WhenStartAndEndSet_ReturnsTimeSpan()
        {
            var sut = MakeSut();
            var start = DateTime.UtcNow.AddMinutes(-5);
            var end = DateTime.UtcNow;
            sut.SetMatchStartTime(start);
            sut.SetMatchEndTime(end);

            sut.Duration.ShouldBe(end - start);
        }

        [Fact]
        public void LoadTournament_WithValidDto_InitializesProperties()
        {
            var sut = MakeSut();
            var playerId = Guid.NewGuid();
            var tournament = new TournamentDto
            {
                Id = Guid.NewGuid(),
                Name = "Test Tournament",
                RegisteredPlayers = new Dictionary<Guid, string> { { playerId, "Player A" } },
                Matches =
                [
                    new MatchDto
                    {
                        PlayerAId = playerId,
                        PlayerBId = Guid.NewGuid(),
                        Status = MatchStatus.Ongoing
                    }
                ],
                Leaderboard = new List<LeaderboardDto>(),
                Status = "Ongoing"
            };

            sut.LoadTournament(tournament);

            sut.TournamentName.ShouldBe("Test Tournament");
            sut.TotalPlayers.ShouldBe(1);
            sut.TotalMatches.ShouldBe(1);
            sut.TournamentStatus.ShouldBe("Ongoing");
            sut.PlayerA.ShouldBe("Player A");
        }
    }
}
