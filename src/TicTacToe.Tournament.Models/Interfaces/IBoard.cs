namespace TicTacToe.Tournament.Models.Interfaces;

public interface IBoard
{
    /// <summary>
    /// Get's the Board Id (Game Id).
    /// </summary>
    Guid Id { get; }

    /// <summary>
    /// Replies the current state of the board.
    /// </summary>
    /// <returns>Array of 3x3 of Marks</returns>
    Mark[][] GetState();

    /// <summary>
    /// Checks if the move is valid.
    /// </summary>
    /// <param name="row">Row</param>
    /// <param name="column">Column</param>
    /// <returns>True for a valid movement.</returns>
    bool IsValidMove(int row, int column);

    /// <summary>
    /// Try to apply a move on the board.
    /// </summary>
    /// <param name="row">Row</param>
    /// <param name="column">Column</param>
    /// <param name="mark">Mark</param>
    /// <returns>True for success</returns>
    bool TryApplyMove(int row, int column, Mark mark);

    /// <summary>
    /// Indicates the board is complete.
    /// </summary>
    /// <returns>True for completed</returns>
    bool IsFull();

    /// <summary>
    /// Checks if the game is over.
    /// </summary>
    /// <returns>Winner Mark</returns>
    Mark? GetWinner();

    /// <summary>
    /// Prints to console the current state of the board.
    /// </summary>
    void PrintToConsole();
}
