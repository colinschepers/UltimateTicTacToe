using Core;
using Core.Games;

namespace GameLogic
{
    public class Move : IMove
    {
        public int BoardNr { get; set; }
        public int Position { get; set; }

        public Move(int boardNr, int position)
        {
            BoardNr = boardNr;
            Position = position;
        }

        public override bool Equals(object obj)
        {
            return obj is Move move && BoardNr == move.BoardNr && Position == move.Position;
        }

        public override int GetHashCode()
        {
            return 997 + 101 * BoardNr.GetHashCode() + 103 * Position.GetHashCode();
        }

        public override string ToString()
        {
            return string.Format("({0},{1})", BoardNr, Position);
        }
    }
}