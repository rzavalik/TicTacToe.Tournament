namespace TicTacToe.Tournament.BasePlayer.Interfaces
{
    using TicTacToe.Tournament.Models;
    using TicTacToe.Tournament.Models.DTOs;

    public interface IGameConsoleUI
    {
        Mark[][] Board { get; }
        string TournamentName { get; }
        string PlayerA { get; }
        string PlayerB { get; }
        Mark? CurrentTurn { get; }
        Mark? PlayerMark { get; }
        DateTime? MatchStartTime { get; }
        DateTime? MatchEndTime { get; }
        TimeSpan? Duration { get; }
        bool IsPlaying { get; }
        int TotalPlayers { get; }
        int? TotalMatches { get; }
        int? MatchesFinished { get; }
        int? MatchesPlanned { get; }
        int? MatchesOngoing { get; }
        int? MatchesCancelled { get; }
        string TournamentStatus { get; }
        string MatchStatus { get; }
        List<LeaderboardDto> Leaderboard { get; }

        void Log(string message);
        void Start();
        T Read<T>(string message);
        void LoadTournament(TournamentDto value);
        void SetIsPlaying(bool isPlaying);
        void SetPlayerA(string value);
        void SetPlayerB(string value);
        void SetBoard(Mark[][]? marks);
        void SetMatchEndTime(DateTime? now);
        void SetMatchStartTime(DateTime? now);
        void SetPlayerMark(Mark mark);
        void SetCurrentTurn(Mark mark);
    }
}
