namespace TicTacToe.Tournament.Models.Interfaces
{
    public interface IPlayerBot
    {
        /// <summary>
        /// Player Unique ID
        /// </summary>
        Guid Id { get; }

        /// <summary>
        /// Player Name
        /// </summary>
        string Name { get; set; }

        /// <summary>
        /// Event called when the player is registered in the tournament.
        /// </summary>
        /// <param name="playerId">Player ID registered</param>
        void OnRegistered(Guid playerId);

        /// <summary>
        /// Event called when the match starts.
        /// </summary>
        /// <param name="matchId">Match Id</param>
        /// <param name="opponentId">Player Id</param>
        /// <param name="opponentId">Opponent Id</param>
        /// <param name="mark">Player Mark on board</param>
        /// <param name="starts">Flag indicating if its your turn</param>
        void OnMatchStarted(Guid matchId, Guid playerId, Guid opponentId, Mark mark, bool starts);

        /// <summary>
        /// Event called when the opponent makes a move.
        /// </summary>
        /// <param name="matchId">Match ID.</param>
        /// <param name="row">Row indicating where the player has moved</param>
        /// <param name="column">Column indicating where the player has moved</param>
        void OnOpponentMoved(Guid matchId, int row, int column);

        /// <summary>
        /// Called when it's the player's turn to make a move.
        /// Receives the current board state and should return the row and column of the move.
        /// </summary>
        /// <param name="matchId">Match ID.</param>
        /// <param name="board">A 3x3 matrix representing the current board state.</param>
        /// <returns>A tuple (row, column) indicating the player's move.</returns>
        Task<(int row, int col)> MakeMoveAsync(Guid matchId, Mark[][] board);

        /// <summary>
        /// Event called when the match ends.
        /// </summary>
        /// <param name="result">GameResult for the match</param>
        void OnMatchEnded(GameResult result);
    }
}