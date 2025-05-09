﻿using Shouldly;

namespace TicTacToe.Tournament.Models.Tests
{
    public class MatchTests
    {
        [Fact]
        public void Board_ShouldBe3By3Matrix()
        {
            var sut = MakeSut();
            sut.Board.Length.ShouldBe(3);
            foreach (var row in sut.Board)
            {
                row.Length.ShouldBe(3);
            }
        }

        [Fact]
        public void Duration_WhenStartAndEndDefined_ReturnsCorrectTimespan()
        {
            var sut = MakeSut();
            sut.StartTime = DateTime.UtcNow;
            sut.EndTime = sut.StartTime.Value.AddMinutes(5);

            sut.Duration.ShouldBe(TimeSpan.FromMinutes(5));
        }

        [Fact]
        public void Duration_WhenStartOrEndMissing_ReturnsNull()
        {
            var sut = MakeSut();
            sut.StartTime = DateTime.UtcNow;
            sut.EndTime = null;
            sut.Duration.ShouldBeNull();

            sut.StartTime = null;
            sut.EndTime = DateTime.UtcNow;
            sut.Duration.ShouldBeNull();
        }

        [Fact]
        public async Task MakeMoveAsync_InvalidPlayer_ThrowsAccessViolationException()
        {
            var player1 = Guid.NewGuid();
            var player2 = Guid.NewGuid();
            var match = new Match(player1, player2);

            var invalidPlayer = Guid.NewGuid();

            await Should.ThrowAsync<AccessViolationException>(async () =>
            {
                await match.MakeMoveAsync(invalidPlayer, 0, 0);
            });
        }

        [Fact]
        public async Task MakeMoveAsync_NotPlayersTurn_ThrowsInvalidOperationException()
        {
            var player1 = Guid.NewGuid();
            var player2 = Guid.NewGuid();
            var match = new Match(player1, player2);

            // Player1 makes first move (ok)
            await match.MakeMoveAsync(player1, 0, 0);

            // Player1 tries again immediately (should not be allowed)
            await Should.ThrowAsync<InvalidOperationException>(async () =>
            {
                await match.MakeMoveAsync(player1, 1, 0);
            });
        }

        [Fact]
        public async Task MakeMoveAsync_PlayerWins_FinishesMatch()
        {
            var player1 = Guid.NewGuid();
            var player2 = Guid.NewGuid();
            var match = new Match(player1, player2);

            // Player1 - (0,0)
            await match.MakeMoveAsync(player1, 0, 0);

            // Player2 - (1,0)
            await match.MakeMoveAsync(player2, 1, 0);

            // Player1 - (0,1)
            await match.MakeMoveAsync(player1, 0, 1);

            // Player2 - (1,1)
            await match.MakeMoveAsync(player2, 1, 1);

            // Player1 - (0,2) => Win!
            await match.MakeMoveAsync(player1, 0, 2);

            match.Status.ShouldBe(MatchStatus.Finished);
            match.EndTime.ShouldNotBeNull();
        }

        private Match MakeSut()
        {
            return new Match
            {
                Board = new[]
                {
                    new[] { Mark.Empty, Mark.Empty, Mark.Empty },
                    new[] { Mark.Empty, Mark.Empty, Mark.Empty },
                    new[] { Mark.Empty, Mark.Empty, Mark.Empty }
                }
            };
        }
    }
}
