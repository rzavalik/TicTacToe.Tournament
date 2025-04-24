using System.Diagnostics.CodeAnalysis;
using System.Net.Http.Json;
using System.Text.Json;
using TicTacToe.Tournament.Models.Interfaces;

namespace TicTacToe.Tournament.Models;

public class HttpClientWrapper : IHttpClient
{
    private readonly HttpClient _httpClient;

    public HttpClientWrapper(HttpClient? httpClient = null)
    {
        _httpClient = httpClient ?? new HttpClient();
    }

    public async Task<HttpResponseMessage> PostAsJsonAsync<TValue>(
        [StringSyntax("Uri")]
        string? requestUri,
        TValue value,
        JsonSerializerOptions? options = null,
        CancellationToken cancellationToken = default)
    {
        return await _httpClient.PostAsJsonAsync(requestUri, value, options, cancellationToken);
    }
}
