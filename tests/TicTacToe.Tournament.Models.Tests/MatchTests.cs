using Shouldly;

namespace TicTacToe.Tournament.Models.Tests
{
    public class MatchTests
    {
        private readonly Guid _playerA = Guid.NewGuid();
        private readonly Guid _playerB = Guid.NewGuid();

        private Match MakeSut() => new(_playerA, _playerB);

        [Fact]
        public void Board_ShouldBe3By3Matrix()
        {
            var sut = MakeSut();
            sut.Board.GetState().Length.ShouldBe(3);
            foreach (var row in sut.Board.GetState())
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
                match.MakeMove(invalidPlayer, 0, 0);
            });
        }

        [Fact]
        public async Task MakeMoveAsync_NotPlayersTurn_ThrowsInvalidOperationException()
        {
            var player1 = Guid.NewGuid();
            var player2 = Guid.NewGuid();
            var match = new Match(player1, player2);

            // Player1 makes first move (ok)
            match.MakeMove(player1, 0, 0);

            // Player1 tries again immediately (should not be allowed)
            await Should.ThrowAsync<InvalidOperationException>(async () =>
            {
                match.MakeMove(player1, 1, 0);
            });
        }

        [Fact]
        public async Task MakeMoveAsync_PlayerWins_FinishesMatch()
        {
            var player1 = Guid.NewGuid();
            var player2 = Guid.NewGuid();
            var match = new Match(player1, player2);

            // Player1 - (0,0)
            match.MakeMove(player1, 0, 0);

            // Player2 - (1,0)
            match.MakeMove(player2, 1, 0);

            // Player1 - (0,1)
            match.MakeMove(player1, 0, 1);

            // Player2 - (1,1)
            match.MakeMove(player2, 1, 1);

            // Player1 - (0,2) => Win!
            match.MakeMove(player1, 0, 2);

            match.Status.ShouldBe(MatchStatus.Finished);
            match.EndTime.ShouldNotBeNull();
        }

        [Fact]
        public void Constructor_ShouldSetPlayers()
        {
            var sut = MakeSut();

            sut.PlayerA.ShouldBe(_playerA);
            sut.PlayerB.ShouldBe(_playerB);
            sut.Status.ShouldBe(MatchStatus.Planned);
        }

        [Fact]
        public void Created_ShouldBeSetOnInstantiation()
        {
            var before = DateTime.UtcNow;
            var sut = MakeSut();
            var after = DateTime.UtcNow;

            sut.Created.ShouldBeInRange(before, after);
        }

        [Fact]
        public void ETag_ShouldMatch()
        {
            var sut = MakeSut();
            sut.ETag.ShouldBe($"\"{(sut.Modified ?? sut.Created).ToUniversalTime().Ticks}\"");
        }

        [Fact]
        public void Start_ShouldSetStatusAndStartTime()
        {
            var sut = MakeSut();
            sut.Start();

            sut.Status.ShouldBe(MatchStatus.Ongoing);
            sut.StartTime.ShouldNotBeNull();
        }

        [Fact]
        public void MakeMove_FirstMove_ShouldSetStartTimeAndAlternateTurn()
        {
            var sut = MakeSut();

            sut.MakeMove(_playerA, 0, 0);

            sut.StartTime.ShouldNotBeNull();
            sut.CurrentTurn.ShouldBe(_playerB);
            sut.Board.State[0][0].ShouldBe(Mark.X);
        }

        [Fact]
        public void MakeMove_InvalidPlayer_ShouldThrow()
        {
            var sut = MakeSut();

            var invalidPlayer = Guid.NewGuid();
            Should.Throw<AccessViolationException>(() =>
                sut.MakeMove(invalidPlayer, 0, 0));
        }

        [Fact]
        public void MakeMove_NotYourTurn_ShouldThrow()
        {
            var sut = MakeSut();
            sut.MakeMove(_playerA, 0, 0); // turn becomes _playerB

            Should.Throw<InvalidOperationException>(() =>
                sut.MakeMove(_playerA, 1, 1));
        }

        [Fact]
        public void MakeMove_GameAlreadyFinished_ShouldThrow()
        {
            var sut = MakeSut();
            sut.Finish();

            Should.Throw<InvalidOperationException>(() =>
                sut.MakeMove(_playerA, 0, 0));
        }

        [Fact]
        public void MakeMove_WinningMove_ShouldSetWinnerAndFinishMatch()
        {
            var sut = MakeSut();

            sut.MakeMove(_playerA, 0, 0); // X
            sut.MakeMove(_playerB, 1, 0); // O
            sut.MakeMove(_playerA, 0, 1); // X
            sut.MakeMove(_playerB, 1, 1); // O
            sut.MakeMove(_playerA, 0, 2); // X wins

            sut.Status.ShouldBe(MatchStatus.Finished);
            sut.WinnerMark.ShouldBe(Mark.X);
            sut.EndTime.ShouldNotBeNull();
        }

        [Fact]
        public void Draw_ShouldSetStatusAndClearWinner()
        {
            var sut = MakeSut();

            sut.Draw();

            sut.Status.ShouldBe(MatchStatus.Finished);
            sut.WinnerMark.ShouldBeNull();
            sut.EndTime.ShouldNotBeNull();
        }

        [Fact]
        public void Finish_WithMark_ShouldSetOppositeWinner()
        {
            var sut = MakeSut();

            sut.Finish(Mark.X);

            sut.Status.ShouldBe(MatchStatus.Finished);
            sut.WinnerMark.ShouldBe(Mark.O);
            sut.EndTime.ShouldNotBeNull();
        }

        [Fact]
        public void Duration_ShouldReturnDifference_WhenStartAndEndSet()
        {
            var sut = MakeSut();
            sut.StartTime = DateTime.UtcNow;
            Thread.Sleep(10);
            sut.EndTime = DateTime.UtcNow;

            sut.Duration.ShouldNotBeNull();
            sut.Duration.Value.TotalMilliseconds.ShouldBeGreaterThan(0);
        }

        [Fact]
        public void GetPlayerMark_ShouldReturnCorrectMark()
        {
            var sut = MakeSut();

            sut.GetPlayerMark(_playerA).ShouldBe(Mark.X);
            sut.GetPlayerMark(_playerB).ShouldBe(Mark.O);
        }

        [Fact]
        public void GetPlayerMark_InvalidPlayer_ShouldThrow()
        {
            var sut = MakeSut();

            Should.Throw<InvalidOperationException>(() =>
                sut.GetPlayerMark(Guid.NewGuid()));
        }
    }
}
