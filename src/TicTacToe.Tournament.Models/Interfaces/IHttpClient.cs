using System.Diagnostics.CodeAnalysis;
using System.Text.Json;

namespace TicTacToe.Tournament.Models.Interfaces
{
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
