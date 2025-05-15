using System.Diagnostics.CodeAnalysis;
using System.Net.Http.Headers;
using System.Text.Json;

namespace TicTacToe.Tournament.BasePlayer.Interfaces;

public interface IHttpClient
{
    HttpRequestHeaders DefaultRequestHeaders { get; }

    Task<HttpResponseMessage> PostAsJsonAsync<TValue>(
        [StringSyntax("Uri")] string? requestUri, 
        TValue value, 
        JsonSerializerOptions? options = null, 
        CancellationToken cancellationToken = default);
}
