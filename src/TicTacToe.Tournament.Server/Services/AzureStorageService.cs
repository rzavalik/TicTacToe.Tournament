namespace TicTacToe.Tournament.Server.Services;

using System.Collections.Concurrent;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using TicTacToe.Tournament.Models;
using TicTacToe.Tournament.Server.Interfaces;

public class AzureStorageService : IAzureStorageService
{
    private BlobContainerClient? _containerClient;
    private readonly string _connectionString;
    private static readonly JsonSerializerOptions _jsonOptions = new()
    {
        WriteIndented = false,
        Converters = { new JsonStringEnumConverter(JsonNamingPolicy.CamelCase) },
        DefaultIgnoreCondition = JsonIgnoreCondition.Never
    };

    public AzureStorageService(string connectionString)
    {
        _connectionString = connectionString
            ?? throw new ArgumentNullException(nameof(connectionString), "ConnectionString must be provided.");

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
        if (_containerClient == null)
        {
            throw new InvalidOperationException("Service is not initialized.");
        }

        Console.WriteLine($"[AzureStorageService] Uploading {path}...");

        var blobClient = _containerClient.GetBlobClient(path);
        var newContent = JsonSerializer.Serialize(obj, _jsonOptions);

        if (await blobClient.ExistsAsync())
        {
            var existing = await blobClient.DownloadContentAsync();
            var existingContent = existing.Value.Content.ToString();

            if (existingContent == newContent)
            {
                return;
            }
        }

        using var stream = new MemoryStream(Encoding.UTF8.GetBytes(newContent));
        await blobClient.UploadAsync(stream, overwrite: true);
        Console.WriteLine($"[AzureStorageService] Uploaded {path}");
    }

    private async Task<T?> DownloadAsync<T>(string path)
    {
        if (_containerClient == null)
        {
            throw new InvalidOperationException("Service is not initialized.");
        }

        var blobClient = _containerClient.GetBlobClient(path);
        if (!await blobClient.ExistsAsync()) return default;

        var download = await blobClient.DownloadContentAsync();
        return JsonSerializer.Deserialize<T>(download.Value.Content.ToStream(), _jsonOptions);
    }

    public async Task<IEnumerable<Guid>> ListTournamentsAsync()
    {
        if (_containerClient == null)
        {
            throw new InvalidOperationException("Service is not initialized.");
        }

        var result = new HashSet<Guid>();

        await foreach (var blob in _containerClient.GetBlobsByHierarchyAsync(prefix: "active/", delimiter: "/"))
        {
            var prefix = blob.Prefix?.TrimEnd('/');

            if (string.IsNullOrEmpty(prefix))
                continue;

            var parts = prefix.Split('/');
            if (parts.Length != 2)
                continue;

            var tournamentIdString = parts[1];

            if (Guid.TryParse(tournamentIdString, out var tournamentId))
            {
                var hasRealBlob = false;
                await foreach (var item in _containerClient.GetBlobsAsync(prefix: $"{prefix}/"))
                {
                    hasRealBlob = true;
                    break;
                }

                if (hasRealBlob)
                {
                    result.Add(tournamentId);
                }
            }
        }

        return result;
    }


    public async Task<bool> TournamentExistsAsync(Guid tournamentId)
    {
        if (_containerClient == null)
        {
            throw new InvalidOperationException("Service is not initialized.");
        }

        var list = await ListTournamentsAsync();
        if (list == null)
        {
            return false;
        }

        return list.Any(tId => tId == tournamentId);
    }

    public async Task DeleteTournamentAsync(Guid tournamentId)
    {
        if (_containerClient == null)
        {
            throw new InvalidOperationException("Service is not initialized.");
        }

        var prefix = $"active/{tournamentId}/";

        await foreach (var blobItem in _containerClient.GetBlobsAsync(prefix: prefix))
        {
            var sourceBlob = _containerClient.GetBlobClient(blobItem.Name);

            var newBlobName = blobItem.Name.Replace($"active/", "deleted/");

            var destinationBlob = _containerClient.GetBlobClient(newBlobName);

            await destinationBlob.StartCopyFromUriAsync(sourceBlob.Uri);

            await sourceBlob.DeleteIfExistsAsync(DeleteSnapshotsOption.IncludeSnapshots);
        }
    }

    public async Task SaveTournamentStateAsync(TournamentContext tContext)
    {
        if (_containerClient == null)
        {
            throw new InvalidOperationException("Service is not initialized.");
        }

        var folder = $"{tContext.Tournament.Id}";

        await UploadAsync($"active/{folder}/tournament.json", tContext.Tournament);
        await UploadAsync($"active/{folder}/pendingMoves.json", tContext.GameServer.GetPendingMoves() ??
            new ConcurrentDictionary<Guid, ConcurrentQueue<(byte Row, byte Col)>>());
    }

    public async Task<(Models.Tournament? Tournament, List<PlayerInfo>? PlayerInfos, Dictionary<Guid, Guid>? Map, ConcurrentDictionary<Guid, ConcurrentQueue<(byte Row, byte Col)>>? Moves)>
        LoadTournamentStateAsync(Guid tournamentId)
    {
        if (_containerClient == null)
        {
            throw new InvalidOperationException("Service is not initialized.");
        }

        var folder = $"{tournamentId}";

        var tournament = await DownloadAsync<Models.Tournament>($"active/{folder}/tournament.json");
        var playerInfos = await DownloadAsync<List<PlayerInfo>>($"active/{folder}/players.json");
        var playerMap = await DownloadAsync<Dictionary<Guid, Guid>>($"active/{folder}/playerTournamentMap.json");
        var moves = await DownloadAsync<ConcurrentDictionary<Guid, ConcurrentQueue<(byte Row, byte Col)>>?>($"active/{folder}/pendingMoves.json");

        return (tournament, playerInfos, playerMap, moves);
    }
}
