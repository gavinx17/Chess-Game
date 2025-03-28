namespace ChessLogic
{
    public class Direction
    {
        public readonly static Direction North = new Direction(-1, 0);
        public readonly static Direction South = new Direction(1, 0);
        public readonly static Direction East = new Direction(0, 1);
        public readonly static Direction West = new Direction(0, -1);

        // Directly define diagonal directions to avoid recursion
        public readonly static Direction NorthEast = new Direction(-1, 1);
        public readonly static Direction NorthWest = new Direction(-1, -1);
        public readonly static Direction SouthEast = new Direction(1, 1);
        public readonly static Direction SouthWest = new Direction(1, -1);

        public int RowDelta { get; }
        public int ColumnDelta { get; }

        public Direction(int rowDelta, int columnDelta) { RowDelta = rowDelta; ColumnDelta = columnDelta; }

        public static Direction operator +(Direction a, Direction b)
        {
            return new Direction(a.RowDelta + b.RowDelta, a.ColumnDelta + b.ColumnDelta);
        }

        public static Direction operator *(int scalar, Direction dir)
        {
            return new Direction(dir.RowDelta * scalar, dir.ColumnDelta * scalar);
        }
    }
}
