using System;
using System.Diagnostics.Metrics;

namespace ChessLogic
{
    public class GameState
    {
        public Board Board { get; }
        public Player CurrentPlayer { get; set; }

        public bool isRealPlayer = true;
        public Result Result { get; private set; } = null;


        public GameState(Player player, Board board)
        {
            CurrentPlayer = player;
            Board = board;
        }

        public void NextTurn()
        {
            // Switch the player to the other side
            CurrentPlayer = (CurrentPlayer == Player.White) ? Player.Black : Player.White;

            // If it's Black's turn, invoke the AI
            if (CurrentPlayer == Player.Black)
            {
                Move bestMove = GetBestMove(3); // Using depth 3 for Minimax
                if (bestMove != null)
                {
                    Console.WriteLine(bestMove.ToString());
                    bestMove.Execute(Board);        // Execute the AI's move
                    CheckForGameOver();                // Check if the game is over after AI's move
                }
            }
        }
        public IEnumerable<Move> LegalMovesForPiece(Position pos)
        {
            if(Board.IsEmpty(pos) || Board[pos].Color != CurrentPlayer)
            {
                return Enumerable.Empty<Move>();
            }

            Piece piece = Board[pos];
            IEnumerable<Move> moveCanidates = piece.GetMoves(pos, Board);
            return moveCanidates.Where(move => move.IsLegal(Board));
        }

        public GameState Copy()
        {
            // Create a new board that is a deep copy of the current board
            Board boardCopy = this.Board.Copy();

            // Create a new game state with the copied board and the current player
            GameState newGameState = new GameState(this.CurrentPlayer, boardCopy);

            return newGameState;
        }

        public IEnumerable<Move> AllLegalMoves(Player player)
        {
            IEnumerable<Move> moveCanidates = Board.PiecePositionsFor(player).SelectMany(pos =>
            {
                Piece piece = Board[pos];
                return piece.GetMoves(pos, Board);
            });

            return moveCanidates.Where(move => move.IsLegal(Board));
        }

        public IEnumerable<Move> GetAllLegalMoves()
        {
            foreach (Position pos in Board.PiecePositionsFor(CurrentPlayer))
            {
                foreach (Move move in LegalMovesForPiece(pos))
                {
                    yield return move;
                }
            }
        }


        public void CheckForGameOver()
        {
            if(!AllLegalMoves(CurrentPlayer).Any())
            {
                if(Board.IsInCheck(CurrentPlayer))
                {
                    Result = Result.Win(CurrentPlayer.Opponent());
                }
                else
                {
                    Result = Result.Draw(EndReason.Stalemate);
                }
            }
        }

        public bool IsGameOver()
        {
            return Result != null;
        }
        public int EvaluateBoard()
        {
            int score = 0;
            foreach (Position pos in Board.PiecePositions())
            {
                Piece piece = Board[pos];
                int pieceValue = GetPieceValue(piece.Type);
                if (piece.Color == CurrentPlayer)
                    score += pieceValue;
                else
                    score -= pieceValue;
            }
            return score;
        }

        private int GetPieceValue(PieceType type)
        {
            switch (type)
            {
                case PieceType.Pawn: return 1;
                case PieceType.Knight: return 3;
                case PieceType.Bishop: return 3;
                case PieceType.Rook: return 5;
                case PieceType.Queen: return 9;
                default: return 0;
            }
        }
        public Move GetBestMove(int depth)
        {
            Move bestMove = null;
            int bestScore = int.MinValue;

            foreach (Move move in GetAllLegalMoves())
            {
                GameState copy = this.Copy();
                move.Execute(copy.Board);

                int score = Minimax(copy, depth - 1, false);  // Run minimax for Black's turn

                if (score > bestScore)
                {
                    bestScore = score;
                    bestMove = move;
                }
            }

            return bestMove;
        }


        private int Minimax(GameState state, int depth, bool isMaximizingPlayer)
        {
            if (depth == 0 || state.IsGameOver())
                return state.EvaluateBoard();

            if (isMaximizingPlayer)
            {
                int maxEval = int.MinValue;
                foreach (Move move in state.GetAllLegalMoves())
                {
                    GameState copy = state.Copy();
                    move.Execute(copy.Board);
                    int eval = Minimax(copy, depth - 1, false);
                    maxEval = Math.Max(maxEval, eval);
                }
                return maxEval;
            }
            else
            {
                int minEval = int.MaxValue;
                foreach (Move move in state.GetAllLegalMoves())
                {
                    GameState copy = state.Copy();
                    move.Execute(copy.Board);
                    int eval = Minimax(copy, depth - 1, true);
                    minEval = Math.Min(minEval, eval);
                }
                return minEval;
            }
        }

    }
}
