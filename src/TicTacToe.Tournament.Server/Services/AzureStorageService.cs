using System.Text.Json;
using System.Text;
using Azure.Storage.Blobs;
using System.Collections.Concurrent;
using TicTacToe.Tournament.Models.Interfaces;
using TicTacToe.Tournament.Models;
using System.Text.Json.Serialization;
using TicTacToe.Tournament.Server.Interfaces;

namespace TicTacToe.Tournament.Server.Services;

public class AzureStorageService : IAzureStorageService
{
    private BlobContainerClient _containerClient;
    private readonly string _connectionString;
    private static readonly JsonSerializerOptions _jsonOptions = new()
    {
        WriteIndented = true,
        Converters = { new JsonStringEnumConverter(JsonNamingPolicy.CamelCase) }
    };

    public AzureStorageService(string connectionString)
    {
        _connectionString = connectionString;

        Initialize();
    }

    public bool IsInitialized { get; private set; } = false;

    public void Initialize()
    {
        var blobServiceClient = new BlobServiceClient(_connectionString);
        _containerClient = blobServiceClient.GetBlobContainerClient("games");

        _containerClient
            .CreateIfNotExistsAsync()
            .GetAwaiter()
            .GetResult();

        IsInitialized = true;
    }

    private async Task UploadAsync<T>(string path, T obj)
    {
        Console.WriteLine($"[AzureStorageService] Uploading {path}...");
        var blobClient = _containerClient.GetBlobClient(path);
        var content = JsonSerializer.Serialize(obj, _jsonOptions);
        using var stream = new MemoryStream(Encoding.UTF8.GetBytes(content));
        await blobClient.UploadAsync(stream, overwrite: true);
        Console.WriteLine($"[AzureStorageService] Uploaded {path}");
    }

    private async Task<T?> DownloadAsync<T>(string path)
    {
        var blobClient = _containerClient.GetBlobClient(path);
        if (!await blobClient.ExistsAsync()) return default;

        var download = await blobClient.DownloadContentAsync();
        return JsonSerializer.Deserialize<T>(download.Value.Content.ToStream(), _jsonOptions);
    }

    public async Task<IEnumerable<Guid>> ListTournamentsAsync()
    {
        var result = new HashSet<Guid>();

        await foreach (var blob in _containerClient.GetBlobsByHierarchyAsync(delimiter: "/"))
        {
            if (Guid.TryParse(blob.Prefix?.TrimEnd('/'), out var tournamentId))
            {
                result.Add(tournamentId);
            }
        }

        return result;
    }

    public async Task SaveTournamentStateAsync(Guid tournamentId,
        Models.Tournament tournament,
        Dictionary<Guid, IPlayerBot> players,
        Dictionary<Guid, Guid> playerTournamentMap,
        ConcurrentDictionary<Guid, ConcurrentQueue<(int Row, int Col)>> pendingMoves)
    {
        var folder = $"{tournamentId}/";
        var playerInfos = players.Select(p => new PlayerInfo
        {
            PlayerId = p.Key,
            Name = p.Value.Name
        }).ToList();

        await UploadAsync($"{folder}tournament.json", tournament);
        await UploadAsync($"{folder}players.json", playerInfos);
        await UploadAsync($"{folder}playerTournamentMap.json", playerTournamentMap);
        await UploadAsync($"{folder}pendingMoves.json", pendingMoves);
    }

    public async Task<(Models.Tournament? Tournament, List<PlayerInfo>? PlayerInfos, Dictionary<Guid, Guid>? Map, ConcurrentDictionary<Guid, ConcurrentQueue<(int Row, int Col)>>? Moves)>
        LoadTournamentStateAsync(Guid tournamentId)
    {
        var folder = $"{tournamentId}/";

        var tournament = await DownloadAsync<Models.Tournament>($"{folder}tournament.json");
        var playerInfos = await DownloadAsync<List<PlayerInfo>>($"{folder}players.json");
        var playerMap = await DownloadAsync<Dictionary<Guid, Guid>>($"{folder}playerTournamentMap.json");
        var moves = await DownloadAsync<ConcurrentDictionary<Guid, ConcurrentQueue<(int Row, int Col)>>?>($"{folder}pendingMoves.json");

        return (tournament, playerInfos, playerMap, moves);
    }
}