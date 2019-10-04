using Core;
using Core.Games;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace GameLogic
{
    public class State : IState
    {
        static State()
        {
            FillMovesCache();
        }

        private static void FillMovesCache()
        {
            for (int b = 0; b < 9; b++)
            {
                for (int i = 0; i < 0b1000000000; i++)
                {
                    _moves[b, i] = Enumerable.Range(0, 9)
                        .Where(x => (i & (1 << x)) == 0)
                        .Select(x => new Move(b, x))
                        .ToArray();
                }
            }
        }

        private static readonly int[] _bitMove = new[]
            {
            0b000001000000001000000001,
            0b000000000001000000000010,
            0b001000001000000000000100,
            0b000000000000010000001000,
            0b010010000010000000010000,
            0b000000010000000000100000,
            0b100000000000100001000000,
            0b000000000100000010000000,
            0b000100100000000100000000
        };

        private static readonly int _lineFilter =
            0b001001001001001001001001;

        private static readonly Move[,][] _moves = new Move[9, 512][];
        private static readonly Random _random = new Random();

        public int PlayerToMove => Round & 1;
        public int Round { get; private set; }
        public bool GameOver { get; private set; }

        private int _nextBoard = 9;
        private int[,] _boards = new int[2, 10];
        private int[] _counts = new int[10];
        private int _drawBoard = 0;
        private double _score = 0;

        public void Play(IMove move)
        {
            if(!IsValid(move))
            {
                throw new ArgumentException("Invalid move " + move);
            }

            var m = (Move)move;
            var player = Round++ & 1;
            var board = _boards[player, m.BoardNr] |= _bitMove[_nextBoard = m.Position];
            var count = ++_counts[m.BoardNr];

            if (IsLine(board))
            {
                board = _boards[player, 9] |= _bitMove[m.BoardNr];
                _score += player - 0.5;
                count = ++_counts[9];

                if (IsLine(board))
                {
                    GameOver = true;
                    _score = 1 - player;
                    return;
                }
                else if (count == 9)
                {
                    GameOver = true;
                    _score = _score < 0 ? 1 : _score > 0 ? 0 : 0.5;
                }
            }
            else if (count == 9)
            {
                _drawBoard |= _bitMove[m.BoardNr];
                count = ++_counts[9];

                if (count == 9)
                {
                    GameOver = true;
                    _score = _score < 0 ? 1 : _score > 0 ? 0 : 0.5;
                }
            }

            if (_counts[_nextBoard] == 9 || (GetMergedBoard(9) & _bitMove[_nextBoard]) != 0)
            {
                _nextBoard = 9;
            }
        }

        private bool IsLine(int bitBoard)
        {
            return (bitBoard & (bitBoard >> 1) & (bitBoard >> 2) & _lineFilter) != 0;
        }

        private int GetMergedBoard(int b)
        {
            return b < 9 
                ? (_boards[0, b] | _boards[1, b]) & 0b111111111
                : (_boards[0, b] | _boards[1, b] | _drawBoard) & 0b111111111;
        }

        public bool IsValid(IMove move)
        {
            if (move is Move utttMove && (utttMove.BoardNr == _nextBoard || _nextBoard == 9))
            {
                return (GetMergedBoard(utttMove.BoardNr) & _bitMove[utttMove.Position]) == 0;
            }
            return false;
        }

        public IEnumerable<IMove> GetValidMoves()
        {
            if (_nextBoard == 9)
            {
                var possibleBoards = _moves[0, GetMergedBoard(_nextBoard)];
                return possibleBoards.SelectMany(b => _moves[b.Position, GetMergedBoard(b.Position)]);
            }
            return _moves[_nextBoard, GetMergedBoard(_nextBoard)];
        }

        public double GetScore(int playerNr)
        {
            return playerNr == 0 ? _score : 1 - _score;
        }

        public void Set(IState state)
        {
            var s = (State)state;
            _nextBoard = s._nextBoard;
            for (int i = 0; i < 10; i++)
            {
                _counts[i] = s._counts[i];
                for (int j = 0; j < 2; j++)
                {
                    _boards[j, i] = s._boards[j, i];
                }
            }
            _drawBoard = s._drawBoard;
            Round = s.Round;
            GameOver = s.GameOver;
            _score = s._score;
        }

        public IState Clone()
        {
            var state = new State();
            state.Set(this);
            return state;
        }

        public override string ToString()
        {
            var board = new char[9, 9];
            for (int b = 0; b < 9; b++)
            {
                var x = b % 3 * 3;
                var y = b / 3 * 3;
                var p0Win = IsLine(_boards[0, b]);
                var p1Win = IsLine(_boards[1, b]);
                var gameOver = p0Win || p1Win || _counts[b] == 9;

                for (int i = 0; i < 9; i++)
                {
                    var p0 = (_boards[0, b] & (1 << i)) != 0;
                    var p1 = (_boards[1, b] & (1 << i)) != 0;
                    board[x + i % 3, y + i / 3] = gameOver ? ' '
                        : p0 && p1 ? '*'
                        : p0 ? 'X'
                        : p1 ? 'O'
                        : '.';

                    if (i == 4 && p0Win)
                        board[x + i % 3, y + i / 3] = 'X';
                    else if (i == 4 && p1Win)
                        board[x + i % 3, y + i / 3] = 'O';
                }

            }

            var builder = new StringBuilder();
            for (int y = 0; y < 9; y++)
            {
                for (int x = 0; x < 9; x++)
                {
                    builder.Append(board[x, y]);

                    if ((x + 1) % 3 == 0 && (x + 1) < 9)
                    {
                        builder.Append('|');
                    }
                }
                builder.AppendLine();

                if ((y + 1) % 3 == 0 && (y + 1) < 9)
                {
                    builder.AppendLine("---+---+---");
                }
            }

            return builder.ToString();
        }
    }
}