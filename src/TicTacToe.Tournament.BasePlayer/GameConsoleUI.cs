namespace TicTacToe.Tournament.BasePlayer
{
    using System.Collections.Concurrent;
    using System.Text;
    using Spectre.Console;
    using TicTacToe.Tournament.BasePlayer.Helpers;
    using TicTacToe.Tournament.BasePlayer.Interfaces;
    using TicTacToe.Tournament.Models;
    using TicTacToe.Tournament.Models.DTOs;

    public class GameConsoleUI : IGameConsoleUI
    {
        private int i = 0;
        private readonly ConcurrentBag<string> _logs = new();
        private IDictionary<Guid, string> _players;

        public Mark[][] Board { get; set; }

        public string TournamentName { get; set; }

        public string PlayerA { get; set; }

        public string PlayerB { get; set; }

        public Mark? CurrentTurn { get; set; }

        public Mark? PlayerMark { get; set; }

        public DateTime? MatchStartTime { get; set; }

        public DateTime? MatchEndTime { get; set; }

        public TimeSpan? Duration
        {
            get
            {
                if (MatchStartTime.HasValue && MatchStartTime.Value < (MatchEndTime ?? DateTime.Now))
                {
                    return (MatchEndTime ?? DateTime.Now).Subtract(MatchStartTime.Value);
                }

                return null;
            }
        }

        public bool IsPlaying { get; set; }
        public int TotalPlayers { get; set; }
        public int? TotalMatches { get; set; }
        public int? MatchesFinished { get; set; }
        public int? MatchesPlanned { get; set; }
        public int? MatchesOngoing { get; set; }
        public int? MatchesCancelled { get; set; }

        public string TournamentStatus { get; set; }

        public string MatchStatus { get; set; }

        public List<LeaderboardDto> Leaderboard { get; set; }

        public void Log(string message)
        {
            _logs.Add($"{Markup.Escape(DateTime.Now.AddSeconds(new Random().Next(100)).ToShortTimeString())}: {Markup.Escape(message)}");
        }

        public void Start()
        {
            Task.Run(() =>
            {
                var settings = new AnsiConsoleSettings()
                {
                    Ansi = AnsiSupport.Detect,
                    ColorSystem = ColorSystemSupport.Detect,
                };
                var console = AnsiConsole.Create(settings);
                var board = new Layout("Board").Size(17);
                var logs = new Layout("Info");
                var layout = new Layout("Root")
                    .SplitRows(
                        new Layout("Top")
                            .Size(10)
                            .SplitColumns(board, logs),
                        new Layout("Middle"),
                        new Layout("Bottom").Size(3)
                    );

                var liveDisplay = console
                    .Live(layout)
                    .AutoClear(true)
                    .Cropping(VerticalOverflowCropping.Bottom);

                liveDisplay.Start(ctx =>
                {
                    while (true)
                    {
                        var ongoing = (TournamentStatus == "Ongoing");
                        if (ongoing)
                        {
                            board.Visible();
                        }
                        else
                        {
                            board.Invisible();
                        }

                        layout["Board"].Update(RenderBoard());
                        layout["Info"].Update(RenderInfoPanel());
                        layout["Middle"].Update(
                            TournamentStatus == "Finished"
                            ? RenderLeaderboard()
                            : RenderLogPanel()
                        );
                        layout["Bottom"].Update(RenderInputPrompt());
                        ctx.Refresh();
                    }
                });
            });

            Console.SetCursorPosition(2, Console.BufferHeight - 1);
        }

        private Panel RenderBoard()
        {
            var boardText = "";
            if (Board != null && Board.All(row => row.Length == 3))
            {
                using (var textWriter = new StringWriter())
                {
                    var boardRenderer = new BoardRenderer(textWriter);
                    boardRenderer.Draw(Board);
                    boardText = textWriter.ToString();
                }
                boardText = boardText.Replace("X", "[blue]X[/]");
                boardText = boardText.Replace("O", "[yellow]O[/]");
            }

            var panel = new Panel(Align.Center(
                    new Markup(boardText),
                    VerticalAlignment.Middle)
                )
                .HeaderAlignment(Justify.Center)
                .Header("Board")
                .Expand()
                .Border(BoxBorder.Rounded);

            ColorizePanel(panel);

            return panel;
        }

        private Panel RenderInfoPanel()
        {
            var sb = new StringBuilder();
            if (!string.IsNullOrEmpty(TournamentName))
            {
                sb.Append($"{Markup.Escape(TournamentName ?? "")}");

                if (TotalPlayers > 0 || TotalMatches.HasValue)
                {
                    sb.Append($" (");

                    if (TotalPlayers > 0)
                    {
                        sb.Append($"{TotalPlayers} players");

                        if (TotalMatches.HasValue)
                        {
                            sb.Append($" | ");
                        }
                    }

                    if (TotalMatches.HasValue)
                    {
                        sb.Append($"{TotalPlayers} matches");
                    }

                    sb.Append($")");
                }

                sb.Append($"\n");
            }

            if (!string.IsNullOrEmpty(PlayerA))
            {
                sb.Append($"{Markup.Escape(PlayerA ?? "")}");
                if (string.IsNullOrEmpty(PlayerB))
                {
                    sb.Append($"\n");
                }
                else
                {
                    sb.Append($" vs {Markup.Escape(PlayerB ?? "")}\n");
                }
            }

            if (CurrentTurn.HasValue && CurrentTurn != Mark.Empty)
            {
                if (CurrentTurn == PlayerMark)
                {
                    sb.AppendLine($"It's your turn as {Markup.Escape(CurrentTurn?.ToString("G") ?? "")}");
                }
                else
                {
                    sb.AppendLine($"{Markup.Escape(PlayerB ?? "")} is playing as {Markup.Escape(CurrentTurn?.ToString("G") ?? "")}");
                }
            }

            if (Duration.HasValue)
            {
                sb.AppendLine($"{Markup.Escape(Duration.Value.ToString())}");
            }

            if (TotalMatches.HasValue)
            {
                sb.AppendLine($"[bold]Matches[/]: ");
                if ((MatchesFinished ?? 0) > 0) { sb.AppendFormat("{0} finished, ", MatchesFinished.Value); }
                if ((MatchesOngoing ?? 0) > 0) { sb.AppendFormat("{0} ongoing, ", MatchesOngoing.Value); }
                if ((MatchesPlanned ?? 0) > 0) { sb.AppendFormat("{0} planned, ", MatchesPlanned.Value); }
                if ((MatchesCancelled ?? 0) > 0) { sb.AppendFormat("{0} cancelled", MatchesCancelled.Value); }
                if (sb[sb.Length - 1] == ' ') { sb.Remove(sb.Length - 1, 1); }
                if (sb[sb.Length - 1] == ',') { sb.Remove(sb.Length - 1, 1); }
                sb.Append("\n");
            }

            var panel = new Panel(
                    Align.Left(
                        new Markup(sb.ToString()),
                        VerticalAlignment.Middle
                    )
                )
                .Header("Info: " + (IsPlaying ? $"You are playing as {PlayerMark:G}" : $"You are spectating"))
                .Expand()
                .HeaderAlignment(Justify.Left)
                .Border(BoxBorder.Rounded);

            if (TournamentStatus == "Finished" ||
                TournamentStatus == "Cancelled")
            {
                panel.Header("Info");
            }

            ColorizePanel(panel);

            return panel;
        }

        private Panel RenderLeaderboard()
        {
            var table = new Table()
                .AddColumn("#").Alignment(Justify.Left).Expand()
                .AddColumn("Player").Alignment(Justify.Left).Expand()
                .AddColumn("Points").Alignment(Justify.Center).Collapse()
                .AddColumn("Matches").Alignment(Justify.Center).Collapse()
                .AddColumn("W").Alignment(Justify.Center).Collapse()
                .AddColumn("D").Alignment(Justify.Center).Collapse()
                .AddColumn("L").Alignment(Justify.Center).Collapse()
                .AddColumn("WO").Alignment(Justify.Center).Collapse()
                .Border(TableBorder.Heavy)
                .ShowHeaders();

            Leaderboard = Leaderboard
                .OrderByDescending(item => item.TotalPoints)
                .ThenByDescending(item => item.Wins)
                .ThenByDescending(item => item.Draws)
                .ThenByDescending(item => item.Losses)
                .ThenByDescending(item => item.Walkovers)
                .ThenByDescending(item => item.PlayerName)
                .ToList();

            var position = 0;
            foreach (var item in Leaderboard)
            {
                table.AddRow(
                    (++position).ToString(),
                    item.PlayerName,
                    item.TotalPoints.ToString(),
                    (item.Wins + item.Draws + item.Losses + item.Walkovers).ToString(),
                    item.Wins.ToString(),
                    item.Draws.ToString(),
                    item.Losses.ToString(),
                    item.Walkovers.ToString()
                );

                if (position > 5)
                {
                    break;
                }
            }

            var panel = new Panel(
                    table
                )
                .Header("Game Log")
                .HeaderAlignment(Justify.Center)
                .Expand()
                .Border(BoxBorder.Rounded);

            ColorizePanel(panel);

            return panel;
        }

        private Panel RenderLogPanel()
        {
            var logs = _logs.ToList().OrderByDescending(s => s);

            var panel = new Panel(
                    new Rows(
                            logs.Select(log => new Text(ReplaceUserIds(log)))
                        )
                    )
                .Header("Game Log")
                .HeaderAlignment(Justify.Center)
                .Expand()
                .Border(BoxBorder.Rounded);

            ColorizePanel(panel);

            return panel;
        }

        private string ReplaceUserIds(string message)
        {
            if (Leaderboard == null)
            {
                return message;
            }

            foreach (var player in Leaderboard)
            {
                message.Replace(player.PlayerId.ToString(), player.PlayerName);
            }

            return message;
        }

        private Panel RenderInputPrompt()
        {
            var mkup = new Markup(" ");
            var prompt = (IsPlaying && CurrentTurn == PlayerMark)
                ? "> "
                : "  ";

            var panel = new Panel(mkup)
                .Header("Inputs")
                .Expand();

            if (IsPlaying && CurrentTurn == PlayerMark)
            {
                panel.Border(BoxBorder.Rounded);
            }
            else
            {
                panel.NoBorder();
            }

            ColorizePanel(panel);

            return panel;
        }

        private void ColorizePanel(Panel panel)
        {
            if (!IsPlaying)
            {
                panel.BorderColor(Color.White);
            }
            else
            {
                if (CurrentTurn == PlayerMark)
                {
                    if (CurrentTurn == Mark.X)
                    {
                        panel.BorderColor(Color.Blue);
                    }
                    else
                    {
                        panel.BorderColor(Color.Yellow);
                    }
                }
            }
        }

        public T Read<T>(string message)
        {
            return AnsiConsole.Ask<T>(message);
        }

        public void LoadTournament(TournamentDto value)
        {
            _players = value.RegisteredPlayers;

            var currentMatch = value
                .Matches
                .FirstOrDefault(m => m.Status == Models.MatchStatus.Ongoing);

            TournamentName = value.Name;
            TournamentStatus = value.Status;
            PlayerA = GetPlayerName(currentMatch?.PlayerAId);
            PlayerB = GetPlayerName(currentMatch?.PlayerBId);
            TotalPlayers = value?.RegisteredPlayers?.Keys.Count() ?? 0;
            TotalMatches = value?.Matches?.Count();
            MatchesFinished = value?.Matches?.Count(m => m.Status == Models.MatchStatus.Finished);
            MatchesPlanned = value?.Matches?.Count(m => m.Status == Models.MatchStatus.Planned);
            MatchesOngoing = value?.Matches?.Count(m => m.Status == Models.MatchStatus.Ongoing);
            MatchesCancelled = value?.Matches?.Count(m => m.Status == Models.MatchStatus.Cancelled);
            Leaderboard = value?.Leaderboard?.ToList();
            TournamentStatus = value.Status;
            Board = currentMatch?.Board;
        }

        public void SetIsPlaying(bool isPlaying)
        {
            IsPlaying = isPlaying;
        }

        public void SetPlayerA(string value)
        {
            PlayerA = value;
        }

        public void SetPlayerB(string value)
        {
            PlayerB = value;
        }

        public void SetBoard(Mark[][]? marks)
        {
            Board = marks;
        }

        public void SetMatchEndTime(DateTime? now)
        {
            MatchEndTime = now;
        }

        protected string GetPlayerName(Guid? playerId)
        {
            if (playerId.HasValue)
            {
                if (_players?.ContainsKey(playerId.Value) ?? false)
                {
                    return _players[playerId.Value]?
                        .ToString() ?? playerId.Value.ToString();

                }
            }

            return "";
        }

        public void SetMatchStartTime(DateTime? now)
        {
            MatchStartTime = now;
        }

        public void SetPlayerMark(Mark mark)
        {
            PlayerMark = mark;
        }

        public void SetCurrentTurn(Mark mark)
        {
            CurrentTurn = mark;
        }
    }
}