﻿namespace TicTacToe.Tournament.MyBotPlayer
{
    using TicTacToe.Tournament.BasePlayer;
    using TicTacToe.Tournament.BasePlayer.Helpers;
    using TicTacToe.Tournament.BasePlayer.Interfaces;
    using TicTacToe.Tournament.Models;

    public class MyBotPlayer : BasePlayerClient
    {
        private IPlayerStrategy? _strategy;

        public MyBotPlayer(string botName, Guid tournamentId, string webAppEndpoint, string signalrEndpoint, IHttpClient httpClient, ISignalRClientBuilder signalRBuilder)
            : base(botName, tournamentId, webAppEndpoint, signalrEndpoint, httpClient, signalRBuilder)
        {
        }

        public MyBotPlayer()
            : base(
                botName: "MyBot",
                tournamentId: Guid.NewGuid(),
                webAppEndpoint: "http://localhost",
                signalrEndpoint: "http://localhost",
                httpClient: new FakeHttpClient(),
                signalRBuilder: new FakeSignalRClientBuilder())
        { }

        protected override void OnMatchStarted(Guid matchId, Guid playerId, Guid opponentId, Mark mark, bool starts)
        {
            base.OnMatchStarted(matchId, playerId, opponentId, mark, starts);

            _strategy = new MyBotStrategy(
                playerMark: mark,
                opponentMark: mark == Mark.X ? Mark.O : Mark.X,
                (log) => base.ConsoleWrite(log),
                (prompt) => base.ConsoleRead<int>(prompt)
            );
        }

        protected override Task<(byte row, byte col)> MakeMove(Guid matchId, Mark[][] board)
        {
            try
            {
                if (_strategy != null)
                {
                    return Task.FromResult(_strategy.MakeMove(board));
                }
            }
            catch (Exception ex)
            {
                base.ConsoleWrite($"Error in MakeMoveAsync: {ex.Message}");
            }

            return Task.FromResult(((byte)255, (byte)255));
        }
    }
}