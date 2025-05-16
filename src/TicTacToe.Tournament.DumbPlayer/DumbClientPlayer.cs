namespace TicTacToe.Tournament.DumbPlayer
{
    using TicTacToe.Tournament.BasePlayer;
    using TicTacToe.Tournament.BasePlayer.Helpers;
    using TicTacToe.Tournament.BasePlayer.Interfaces;
    using TicTacToe.Tournament.Models;


    public class DumbPlayerClient : BasePlayerClient
    {
        private IPlayerStrategy? _strategy;

        public DumbPlayerClient()
        : base(
            botName: "DumbBot",
            tournamentId: Guid.NewGuid(),
            webAppEndpoint: "http://localhost",
            signalrEndpoint: "http://localhost",
            httpClient: new FakeHttpClient(),
            signalRBuilder: new FakeSignalRClientBuilder())
        { }

        public DumbPlayerClient(
            string botName,
            Guid tournamentId,
            string webAppEndpoint,
            string signalrEndpoint,
            IHttpClient httpClient,
            ISignalRClientBuilder signalRBuilder)
            : base(
                botName,
                tournamentId,
                webAppEndpoint,
                signalrEndpoint,
                httpClient,
                signalRBuilder)
        { }

        protected override void OnMatchStarted(Guid matchId, Guid playerId, Guid opponentId, Mark mark, bool starts)
        {
            base.OnMatchStarted(matchId, playerId, opponentId, mark, starts);

            _strategy = new DumbPlayerStrategy(
                playerMark: mark,
                opponentMark: mark == Mark.X ? Mark.O : Mark.X
            );
        }

        protected override async Task<(byte row, byte col)> MakeMove(Guid matchId, Mark[][] board)
        {
            try
            {
                await Task.Delay(TimeSpan.FromSeconds(1));

                if (_strategy != null)
                {
                    return _strategy.MakeMove(board);
                }
            }
            catch (Exception ex)
            {
                base.ConsoleWrite($"Error in MakeMoveAsync: {ex.Message}");
            }

            return (255, 255);
        }
    }
}