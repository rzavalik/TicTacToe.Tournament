﻿using System.Collections.Concurrent;
using TicTacToe.Tournament.Models;

namespace TicTacToe.Tournament.Server.Interfaces;

public interface IAzureStorageService
{
    Task<IEnumerable<Guid>> ListTournamentsAsync();

    Task SaveTournamentStateAsync(TournamentContext tContext);

    Task<(
        Models.Tournament? Tournament, 
        List<PlayerInfo>? PlayerInfos, 
        Dictionary<Guid, Guid>? Map, 
        ConcurrentDictionary<Guid, ConcurrentQueue<(int Row, int Col)>>? Moves)>
        LoadTournamentStateAsync(Guid tournamentId);

    Task DeleteTournamentAsync(Guid tournamentId);

    Task<bool> TournamentExistsAsync(Guid tournamentId);
}
