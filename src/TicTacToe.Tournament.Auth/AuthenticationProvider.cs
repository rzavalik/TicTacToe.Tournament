namespace TicTacToe.Tournament.Auth
{
    using System.Text.Json;
    using TicTacToe.Tournament.Models.Interfaces;

    public class AuthenticationProvider : IAuthenticationProvider
    {
        private readonly IHttpClient _httpClient;
        private string _baseEndpoint;

        public AuthenticationProvider(
            IHttpClient httpClient,
            string baseEndpoint)
        {
            _httpClient = httpClient
                ?? throw new ArgumentNullException(nameof(httpClient), "HttpClient must be provided");
            _baseEndpoint = baseEndpoint
                ?? throw new ArgumentNullException(nameof(baseEndpoint), "BaseEndpoint must be provided");
        }

        public Guid? PlayerId { get; set; }

        public Guid? TournamentId { get; set; }

        public string? LastMessage { get; set; }

        public async Task<string> GetTokenAsync(Guid tournamentId)
        {
            if (_baseEndpoint.EndsWith("/"))
            {
                _baseEndpoint = _baseEndpoint[..^1];
            }

            var requestUrl = $"{_baseEndpoint}/tournament/{tournamentId}/authenticate";
            var response = await _httpClient.PostAsJsonAsync(
                requestUrl,
                new TournamentAuthRequest
                {
                    TournamentId = tournamentId
                });

            if (!(response?.IsSuccessStatusCode ?? false))
            {
                throw new AccessViolationException($"Failed to authenticate tournament. Status code: {response?.StatusCode}");
            }

            var json = await response.Content.ReadAsStringAsync();

            var jsonOptions = new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            };

            var authResponse = JsonSerializer.Deserialize<TournamentAuthResponse>(json, jsonOptions);

            if (string.IsNullOrEmpty(authResponse?.Token))
            {
                throw new Exception($"Failed to deserialize authentication response.\r\n{json}");
            }

            PlayerId = authResponse.PlayerId;
            TournamentId = authResponse.TournamentId;
            LastMessage = authResponse.Message;

            return authResponse.Token;
        }
    }
}