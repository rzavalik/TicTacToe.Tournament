namespace TicTacToe.Tournament.Models.Tests
{
    using System.Text.Json;
    using System.Text.Json.Serialization;
    using Shouldly;
    using TicTacToe.Tournament.Models;

    public class SerializationTests
    {
        private static readonly JsonSerializerOptions _jsonOptions = new()
        {
            WriteIndented = false,
            Converters = { new JsonStringEnumConverter(JsonNamingPolicy.CamelCase) },
            DefaultIgnoreCondition = JsonIgnoreCondition.Never
        };

        [Fact]
        public void Board_ShouldSerializeAndDeserializePreservingState()
        {
            var board = new Board();
            board.ApplyMove(0, 0, Mark.X);
            board.ApplyMove(1, 1, Mark.O);

            var json = JsonSerializer.Serialize(board, _jsonOptions);
            var result = JsonSerializer.Deserialize<Board>(json, _jsonOptions);

            result.ShouldNotBeNull();
            result.State[0][0].ShouldBe(Mark.X);
            result.State[1][1].ShouldBe(Mark.O);
            result.Movements.Count.ShouldBe(2);
        }

        [Fact]
        public void GameResult_ShouldSerializeAndDeserializeFully()
        {
            var board = new Board();
            board.ApplyMove(0, 0, Mark.X);
            var result = new GameResult(Guid.NewGuid(), Guid.NewGuid(), board, true);

            var json = JsonSerializer.Serialize(result, _jsonOptions);
            var restored = JsonSerializer.Deserialize<GameResult>(json, _jsonOptions);

            restored.ShouldNotBeNull();
            restored.IsDraw.ShouldBeTrue();
            restored.Board.State[0][0].ShouldBe(Mark.X);
        }

        [Fact]
        public void LeaderboardEntry_ShouldSerializeAndDeserializeCorrectly()
        {
            var entry = new LeaderboardEntry("Alice", Guid.NewGuid());
            entry.RegisterResult(MatchScore.Win);
            entry.RegisterResult(MatchScore.Draw);

            var json = JsonSerializer.Serialize(entry, _jsonOptions);
            var restored = JsonSerializer.Deserialize<LeaderboardEntry>(json, _jsonOptions);

            restored.ShouldNotBeNull();
            restored.PlayerName.ShouldBe("Alice");
            restored.TotalPoints.ShouldBe(4);
            restored.Wins.ShouldBe((uint)1);
            restored.Draws.ShouldBe((uint)1);
        }

        [Fact]
        public void Match_ShouldSerializeAndDeserializeWithBoard()
        {
            var match = new Match(Guid.NewGuid(), Guid.NewGuid());
            match.MakeMove(match.PlayerA, 0, 0);
            match.MakeMove(match.PlayerB, 1, 1);

            var json = JsonSerializer.Serialize(match, _jsonOptions);
            var restored = JsonSerializer.Deserialize<Match>(json, _jsonOptions);

            restored.ShouldNotBeNull();
            restored.Board.State[0][0].ShouldBe(Mark.X);
            restored.Board.State[1][1].ShouldBe(Mark.O);
        }

        [Fact]
        public void Movement_ShouldSerializeAndDeserialize()
        {
            var move = new Movement(2, 1, Mark.X);

            var json = JsonSerializer.Serialize(move, _jsonOptions);
            var restored = JsonSerializer.Deserialize<Movement>(json, _jsonOptions);

            restored.ShouldNotBeNull();
            restored.Row.ShouldBe((byte)2);
            restored.Column.ShouldBe((byte)1);
            restored.Mark.ShouldBe(Mark.X);
        }

        [Fact]
        public void Player_ShouldSerializeAndDeserialize()
        {
            var player = new Player(Guid.NewGuid(), "Bob", Guid.NewGuid());

            var json = JsonSerializer.Serialize(player, _jsonOptions);
            var restored = JsonSerializer.Deserialize<Player>(json, _jsonOptions);

            restored.ShouldNotBeNull();
            restored.Name.ShouldBe("Bob");
        }

        [Fact]
        public void PlayerInfo_ShouldSerializeAndDeserialize()
        {
            var info = new PlayerInfo(Guid.NewGuid(), "Carla");

            var json = JsonSerializer.Serialize(info, _jsonOptions);
            var restored = JsonSerializer.Deserialize<PlayerInfo>(json, _jsonOptions);

            restored.ShouldNotBeNull();
            restored.Name.ShouldBe("Carla");
        }

        [Fact]
        public void Tournament_ShouldSerializeAndDeserializeAllProperties()
        {
            var tournament = new Tournament(Guid.NewGuid(), "World Cup", 3);
            tournament.RegisterPlayer(Guid.NewGuid(), "Zico");
            tournament.RegisterPlayer(Guid.NewGuid(), "Pelé");
            tournament.InitializeLeaderboard();

            var json = JsonSerializer.Serialize(tournament, _jsonOptions);
            var restored = JsonSerializer.Deserialize<Tournament>(json, _jsonOptions);

            restored.ShouldNotBeNull();
            restored.Name.ShouldBe("World Cup");
            restored.RegisteredPlayers.Count.ShouldBe(2);
            restored.Leaderboard.Count.ShouldBe(2);
        }
    }
}
