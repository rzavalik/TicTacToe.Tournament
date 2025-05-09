﻿namespace TicTacToe.Tournament.BasePlayer.Helpers
{
    using TicTacToe.Tournament.Models;
    using TicTacToe.Tournament.BasePlayer.Interfaces;


    public class BoardRenderer : IBoardRenderer
    {
        private readonly TextWriter _output;

        public BoardRenderer(TextWriter output)
        {
            _output = output;
        }

        public void Draw(Mark[][] board)
        {
            for (var r = 0; r < 3; r++)
            {
                for (var c = 0; c < 3; c++)
                {
                    var mark = board[r][c];
                    var symbol = mark switch
                    {
                        Mark.X => "X",
                        Mark.O => "O",
                        _ => " "
                    };
                    _output.Write($" {symbol} ");
                    if (c < 2)
                    {
                        _output.Write("|");
                    }
                }
                _output.WriteLine();
                if (r < 2)
                {
                    _output.WriteLine("---+---+---");
                }
            }
        }
    }
}