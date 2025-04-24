using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Net;
using System.Text.Json;
using TicTacToe.Tournament.Auth;
using TicTacToe.Tournament.BasePlayer.Interfaces;

namespace TicTacToe.Tournament.BasePlayer.Helpers;

public class FakeHttpClient : IHttpClient
{
    public HttpRequestHeaders DefaultRequestHeaders => new HttpClient().DefaultRequestHeaders;

    public Task<HttpResponseMessage> PostAsJsonAsync<TValue>(
        string? requestUri, 
        TValue value, 
        JsonSerializerOptions? options = null, 
        CancellationToken cancellationToken = default)
    {
        var response = new TournamentAuthResponse
        {
            Token = "FAKE_TOKEN",
            PlayerId = Guid.NewGuid(),
            TournamentId = Guid.NewGuid()
        };

        var msg = new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = JsonContent.Create(response)
        };

        return Task.FromResult(msg);
    }
}