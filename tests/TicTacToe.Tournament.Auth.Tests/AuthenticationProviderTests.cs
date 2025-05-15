using System.Net;
using System.Text.Json;
using Moq;
using Shouldly;
using TicTacToe.Tournament.Models.Interfaces;

namespace TicTacToe.Tournament.Auth.Tests
{
    public class AuthenticationProviderTests
    {
        private const string BaseEndpoint = "http://test";

        [Fact]
        public async Task GetTokenAsync_ValidRequest_ReturnsTokenAndSetsProperties()
        {
            var tournamentId = Guid.NewGuid();
            var playerId = Guid.NewGuid();
            var token = "abc123";
            var message = "Success";

            var requestUrl = $"{BaseEndpoint}/tournament/{tournamentId}/authenticate";
            var response = new TournamentAuthResponse
            {
                Success = true,
                Message = message,
                Token = token,
                PlayerId = playerId,
                TournamentId = tournamentId
            };

            var httpResponse = new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(JsonSerializer.Serialize(response))
            };

            var clientMock = new Mock<IHttpClient>();
            clientMock.Setup(c => c.PostAsJsonAsync<TournamentAuthRequest>(requestUrl, It.IsAny<TournamentAuthRequest>(), null, default))
                      .ReturnsAsync(httpResponse);

            var sut = MakeSut(clientMock);

            var result = await sut.GetTokenAsync(tournamentId);

            result.ShouldBe(token);
            sut.PlayerId.ShouldBe(playerId);
            sut.TournamentId.ShouldBe(tournamentId);
            sut.LastMessage.ShouldBe(message);
        }

        [Fact]
        public async Task GetTokenAsync_RequestFails_ThrowsAccessViolation()
        {
            var tournamentId = Guid.NewGuid();
            var requestUrl = $"{BaseEndpoint}/tournament/{tournamentId}/authenticate";
            var httpResponse = new HttpResponseMessage(HttpStatusCode.BadRequest);

            var clientMock = new Mock<IHttpClient>();
            clientMock.Setup(c => c.PostAsJsonAsync<TournamentAuthRequest>(requestUrl, It.IsAny<TournamentAuthRequest>(), null, default))
                      .ReturnsAsync(httpResponse);

            var sut = MakeSut(clientMock);

            await Should.ThrowAsync<AccessViolationException>(() => sut.GetTokenAsync(tournamentId));
        }

        [Fact]
        public async Task GetTokenAsync_NullToken_ThrowsException()
        {
            var tournamentId = Guid.NewGuid();

            var response = new TournamentAuthResponse
            {
                Success = true,
                Message = "Missing token",
                Token = string.Empty,
                PlayerId = Guid.NewGuid(),
                TournamentId = tournamentId
            };

            var requestUrl = $"{BaseEndpoint}/tournament/{tournamentId}/authenticate";
            var httpResponse = new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(JsonSerializer.Serialize(response))
            };

            var clientMock = new Mock<IHttpClient>();
            clientMock.Setup(c => c.PostAsJsonAsync<TournamentAuthRequest>(requestUrl, It.IsAny<TournamentAuthRequest>(), null, default))
                      .ReturnsAsync(httpResponse);

            var sut = MakeSut(clientMock);

            await Should.ThrowAsync<Exception>(() => sut.GetTokenAsync(tournamentId));
        }

        [Fact]
        public async Task GetTokenAsync_NullHttpResponse_ThrowsException()
        {
            var tournamentId = Guid.NewGuid();
            var requestUrl = $"{BaseEndpoint}/tournament/{tournamentId}/authenticate";

            var clientMock = new Mock<IHttpClient>();
            clientMock.Setup(c => c.PostAsJsonAsync<TournamentAuthRequest>(requestUrl, It.IsAny<TournamentAuthRequest>(), null, default))
                      .ReturnsAsync((HttpResponseMessage?)null);

            var sut = MakeSut(clientMock);

            await Should.ThrowAsync<Exception>(() => sut.GetTokenAsync(tournamentId));
        }

        [Fact]
        public async Task GetTokenAsync_InvalidContent_ThrowsException()
        {
            var tournamentId = Guid.NewGuid();
            var requestUrl = $"{BaseEndpoint}/tournament/{tournamentId}/authenticate";

            var httpResponse = new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent("this-is-not-json")
            };

            var clientMock = new Mock<IHttpClient>();
            clientMock.Setup(c => c.PostAsJsonAsync<TournamentAuthRequest>(requestUrl, It.IsAny<TournamentAuthRequest>(), null, default))
                      .ReturnsAsync(httpResponse);

            var sut = MakeSut(clientMock);

            await Should.ThrowAsync<JsonException>(() => sut.GetTokenAsync(tournamentId));
        }

        [Fact]
        public void Constructor_NullHttpClient_ThrowsArgumentNullException()
        {
            Should.Throw<ArgumentNullException>(() =>
            {
                var sut = new AuthenticationProvider(null!, BaseEndpoint);
            });
        }

        private AuthenticationProvider MakeSut(Mock<IHttpClient> clientMock)
        {
            return new AuthenticationProvider(
                clientMock.Object, 
                BaseEndpoint);
        }
    }
}
