namespace TicTacToe.Tournament.Models
{
    public class Match
    {
        public Match()
        {
            Board = Models.Board.Empty;
        }

        public Match(Guid playera, Guid playerb)
            : base()
        {
            PlayerA = playera;
            PlayerB = playerb;
        }

        public Guid Id { get; set; } = Guid.NewGuid();

        public Guid PlayerA { get; set; } = default!;
        public Guid PlayerB { get; set; } = default!;

        public MatchStatus Status { get; set; } = MatchStatus.Planned;

        public DateTime? StartTime { get; set; }
        public DateTime? EndTime { get; set; }

        public TimeSpan? Duration =>
            StartTime.HasValue && EndTime.HasValue
                ? EndTime - StartTime
                : null;

        public Mark[][] Board { get; set; } = Models.Board.Empty;

        public Guid CurrentTurn { get; set; } = default!;

        public Mark? WinnerMark { get; set; } = Mark.Empty;

        public async Task MakeMoveAsync(Guid playerId, int row, int col)
        {
            if (Status == MatchStatus.Finished)
                throw new InvalidOperationException("Match already finished.");

            if (playerId != PlayerA && playerId != PlayerB)
                throw new AccessViolationException("Invalid player making move.");

            if (CurrentTurn != Guid.Empty && CurrentTurn != playerId)
                throw new InvalidOperationException("Not your turn.");

            if (Board[row][col] != Mark.Empty)
                throw new InvalidOperationException("Cell already occupied.");

            var mark = playerId == PlayerA ? Mark.X : Mark.O;
            Board[row][col] = mark;

            if (StartTime == null)
            {
                StartTime = DateTime.UtcNow;
                Status = MatchStatus.Ongoing;
            }

            if (CheckWinner(mark))
            {
                WinnerMark = mark;
                Status = MatchStatus.Finished;
                EndTime = DateTime.UtcNow;
                return;
            }

            CurrentTurn = (playerId == PlayerA) ? PlayerB : PlayerA;
        }

        private bool CheckWinner(Mark mark)
        {
            // Rows
            for (int i = 0; i < 3; i++)
                if (Board[i][0] == mark && Board[i][1] == mark && Board[i][2] == mark)
                    return true;

            // Columns
            for (int j = 0; j < 3; j++)
                if (Board[0][j] == mark && Board[1][j] == mark && Board[2][j] == mark)
                    return true;

            // Diagonals
            if (Board[0][0] == mark && Board[1][1] == mark && Board[2][2] == mark)
                return true;

            if (Board[0][2] == mark && Board[1][1] == mark && Board[2][0] == mark)
                return true;

            return false;
        }

    }
}