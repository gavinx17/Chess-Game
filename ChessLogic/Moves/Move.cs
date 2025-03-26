using System.Runtime.CompilerServices;

namespace ChessLogic
{
    public abstract class Move
    {
        public abstract MoveType Type { get; }
        public abstract Position FromPos { get; }
        public abstract Position ToPos { get; }
        public PieceType PieceType { get; internal set; }

        public abstract void Execute(Board board);

        public virtual bool IsLegal(Board board)
        {
            if (board[FromPos] == null)
            {
                return false; // Or handle the error appropriately
            }
            Player player = board[FromPos].Color;
            Board copy = board.Copy();
            Execute(copy);
            return !copy.IsInCheck(player);
        }

    }
}
