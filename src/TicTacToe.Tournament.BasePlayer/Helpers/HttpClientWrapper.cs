using System.Diagnostics.CodeAnalysis;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using TicTacToe.Tournament.BasePlayer.Interfaces;

namespace TicTacToe.Tournament.BasePlayer.Helpers;

public class HttpClientWrapper : IHttpClient
{
    private static readonly HttpClient _httpClient = new HttpClient();

    public HttpRequestHeaders DefaultRequestHeaders => _httpClient.DefaultRequestHeaders;

    public Task<HttpResponseMessage> PostAsJsonAsync<TValue>(
        [StringSyntax("Uri")] string? requestUri, 
        TValue value, 
        JsonSerializerOptions? options = null, 
        CancellationToken cancellationToken = default)
    {
        return _httpClient.PostAsJsonAsync(
            requestUri,
            value,
            options,
            cancellationToken);
    }
}
