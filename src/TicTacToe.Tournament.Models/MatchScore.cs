namespace TicTacToe.Tournament.Models
{
    public enum MatchScore : int
    {
        Win = 3,
        Draw = 1,
        Lose = 0,
        Walkover = -1
    }
}