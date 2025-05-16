using System.IdentityModel.Tokens.Jwt;
using global::TicTacToe.Tournament.Server.Security;
using Shouldly;

namespace TicTacToe.Tournament.Server.Tests.Security;

public class SignalRAccessHelperTests
{
    private const string ConnectionString = "Endpoint=https://fake-signalr.service.signalr.net;AccessKey=FakeAccessKeyFakeAccessKeyFakeAccessKey;Version=1.0;";
    private const string HubName = "tournamentHub";
    private const string UserId = "TestUser";
    private const string AccessKey = "mysupersecretkey_that_is_long_enough";

    public static IEnumerable<object?[]> TournamentIds =>
    new List<object?[]>
    {
            new object?[] { null },
            new object?[] { Guid.Parse("11111111-1111-1111-1111-111111111111") }
    };

    [Theory]
    [MemberData(nameof(TournamentIds))]
    public void GenerateSignalRAccessToken_ShouldReturnValidToken_WithExpectedClaims(Guid? tournamentId = null)
    {
        var sut = SignalRAccessHelper.GenerateSignalRAccessToken(
            ConnectionString,
            HubName,
            UserId,
            tournamentId
        );

        var parts = ConnectionString
            .Split(';', StringSplitOptions.RemoveEmptyEntries)
            .Select(p => p.Split('=', 2))
            .ToDictionary(p => p[0], p => p[1]);

        var endpoint = parts["Endpoint"];
        var accessKey = parts["AccessKey"];

        sut.ShouldNotBeNullOrWhiteSpace();

        var handler = new JwtSecurityTokenHandler();
        var token = handler.ReadJwtToken(sut);

        if (tournamentId.HasValue)
        {
            var tournamentClaim = token.Claims.FirstOrDefault(c => c.Type == "tournamentId");
            tournamentClaim.ShouldNotBeNull();
            tournamentClaim.Value.ShouldBe(tournamentId.ToString());
        }

        token.Claims.ShouldContain(c => c.Type == JwtRegisteredClaimNames.Sub && c.Value == UserId);
        token.Audiences.ShouldContain($"{endpoint}/client/?hub={HubName.ToLower()}");

        var expiry = token.ValidTo;
        var expectedMin = DateTime.UtcNow.AddHours(23);
        var expectedMax = DateTime.UtcNow.AddDays(1.1);
        expiry.ShouldBeInRange(expectedMin, expectedMax);
    }

    [Fact]
    public void GenerateSignalRAccessToken_ShouldIncludeTournamentIdClaim_WhenProvided()
    {
        var tournamentId = Guid.NewGuid();

        var sut = SignalRAccessHelper.GenerateSignalRAccessToken(
            ConnectionString,
            HubName,
            UserId,
            tournamentId
        );

        var parts = ConnectionString
            .Split(';', StringSplitOptions.RemoveEmptyEntries)
            .Select(p => p.Split('=', 2))
            .ToDictionary(p => p[0], p => p[1]);

        var endpoint = parts["Endpoint"];
        var accessKey = parts["AccessKey"];

        var handler = new JwtSecurityTokenHandler();
        var token = handler.ReadJwtToken(sut);

        token.Claims.ShouldContain(c => c.Type == "tournamentId" && c.Value == tournamentId.ToString());
    }

    [Fact]
    public void GenerateSignalRAccessToken_ShouldRespectCustomLifetime()
    {
        var lifetime = TimeSpan.FromMinutes(10);

        var sut = SignalRAccessHelper.GenerateSignalRAccessToken(
            ConnectionString,
            HubName,
            UserId,
            null,
            lifetime
        );

        var handler = new JwtSecurityTokenHandler();
        var token = handler.ReadJwtToken(sut);

        var diff = token.ValidTo - DateTime.UtcNow;
        diff.TotalMinutes.ShouldBeInRange(9, 11);
    }
}
