namespace ChessLogic
{
    public class Position
    {
        public int Row { get; set; }
        public int Column { get; set; }

        public Position(int row, int column) {  Row = row; Column = column; }

        public Player SquareColor()
        {
            if((Row + Column) % 2 == 0)
                return Player.White;
            return Player.Black;
        }

        public override bool Equals(object obj)
        {
            return obj is Position position &&
                   Row == position.Row &&
                   Column == position.Column;
        }
        public static IEnumerable<Piece> GetPieces(IEnumerable<Position> positions, Board board)
        {
            foreach (Position pos in positions)
            {
                yield return (Piece)board[pos];
            }

        }


        public override int GetHashCode()
        {
            return HashCode.Combine(Row, Column);
        }

        public static bool operator ==(Position left, Position right)
        {
            return EqualityComparer<Position>.Default.Equals(left, right);
        }

        public static bool operator !=(Position left, Position right)
        {
            return !(left == right);
        }

        public static Position operator +(Position pos, Direction dir)
        {
            Position result = new Position(pos.Row + dir.RowDelta, pos.Column + dir.ColumnDelta);
            return result;
        }

    }
}
