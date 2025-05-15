namespace TicTacToe.Tournament.Models.Interfaces
{
    using System.Diagnostics.CodeAnalysis;
    using System.Text.Json;

    public interface IHttpClient
    {
        Task<HttpResponseMessage> PostAsJsonAsync<TValue>(
            [StringSyntax(StringSyntaxAttribute.Uri)]
            string? requestUri,
            TValue value,
            JsonSerializerOptions? options = null,
            CancellationToken cancellationToken = default);
    }
}
