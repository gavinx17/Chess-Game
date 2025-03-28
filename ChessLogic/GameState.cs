using System;
using System.Diagnostics.Metrics;
using static System.Formats.Asn1.AsnWriter;
using System.Text.RegularExpressions;

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

        public Piece getPieceFromPos(Position pos)
        {
            Piece piece = Board[pos.Row, pos.Column];
            return piece;
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
                    bestMove.Execute(Board);        // Execute the AI's move
                    CheckForGameOver(); 
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
        private static readonly Dictionary<PieceType, int> MaterialValues = new()
        {
            { PieceType.Pawn, 100 },
            { PieceType.Knight, 320 },
            { PieceType.Bishop, 330 },
            { PieceType.Rook, 500 },
            { PieceType.Queen, 900 },
            { PieceType.King, 20000 }
        };
        private int GetEarlyGamePieceValue(Piece piece, Position pos)
        {
            if (piece == null) return 0; // Avoid null reference errors

            switch (piece.Type)
            {
                case PieceType.Pawn:
                    return EarlyGamePieceSquareScore[0, pos.Row * 8 + pos.Column] + 100;
                case PieceType.Knight:
                    return EarlyGamePieceSquareScore[1, pos.Row * 8 + pos.Column] + 320;
                case PieceType.Bishop:
                    return EarlyGamePieceSquareScore[2, pos.Row * 8 + pos.Column] + 330;
                case PieceType.Rook:
                    return EarlyGamePieceSquareScore[3, pos.Row * 8 + pos.Column] + 500;
                case PieceType.Queen:
                    return EarlyGamePieceSquareScore[4, pos.Row * 8 + pos.Column] + 900;
                case PieceType.King:
                    return EarlyGamePieceSquareScore[5, pos.Row * 8 + pos.Column] + 20000;
                default:
                    return 0;
            }
        }

        public int EvaluateBoard()
        {
            int score = 0;
            foreach (Position pos in Board.PiecePositions())
            {
                Piece piece = Board[pos];
                int pieceValue = MaterialValues[piece.Type] + GetEarlyGamePieceValue(piece, pos);
                score += (piece.Color == Player.White) ? pieceValue : -pieceValue;
            }
            return score;
        }

        private int GetPieceValue(PieceType type, Position pos, int gamePhase)
        {
            int earlyValue = EarlyGamePieceSquareScore[(int)type, pos.Row * 8 + pos.Column];
            int lateValue = EndGamePieceSquareScore[(int)type, pos.Row * 8 + pos.Column];
            return (earlyValue * (24 - gamePhase) + lateValue * gamePhase) / 24; // Smooth transition
        }
        private int GetGamePhase()
        {
            int phase = 0;
            foreach (Position pos in Board.PiecePositions())
            {
                Piece piece = Board[pos];
                phase += (piece.Type == PieceType.Pawn) ? 0 : 1; // Pawns don’t contribute much to game phase
            }
            return Math.Min(phase, 24); // Scale phase from 0 (opening) to 24 (endgame)
        }
        public Move GetBestMove(int depth)
        {
            Move bestMove = null;
            int bestScore = int.MinValue;

            IEnumerable<Move> moveEnumerable = GetAllLegalMoves()
            .OrderByDescending(moveEnumerable => MoveHeuristic(moveEnumerable));
            List<Move> moves = GetAllLegalMoves()
            .OrderByDescending(moveEnumerable => MoveHeuristic(moveEnumerable))
            .ToList();

            foreach (Move move in moves)
            {
                GameState copy = this.Copy();
                move.Execute(copy.Board);
                int score = Minimax(copy, depth - 1, false, int.MinValue, int.MaxValue);

                if (score > bestScore)
                {
                    bestScore = score;
                    bestMove = move;
                }
            }
            return bestMove;
        }

        private int MoveHeuristic(Move move)
        {
            Piece capturedPiece = Board[move.ToPos]; // Get piece being captured
            return capturedPiece != null ? MaterialValues[capturedPiece.Type] : 0; // Higher value moves are prioritized
        }

        public Move FindBestCapture(List<Move> captures, int bestScore)
        {
            int maxScore = 0;
            Move bestMove = null;
            int score = 0;
            foreach (Move move in captures)
            {
                score = GetEarlyGamePieceValue(getPieceFromPos(move.FromPos), move.ToPos);
                if (score > maxScore)
                {
                    maxScore = score;
                    bestMove = move;
                }
            }
            if(maxScore >= bestScore)
                return bestMove;
            return null;
        }

        public bool IsCheck(Board board, Move move)
        {
            if (board == null) return false;
            if (move == null) return false;
            Board copy = board.Copy();
            return copy.IsInCheck(CurrentPlayer);
        }

        private int Minimax(GameState state, int depth, bool isMaximizingPlayer, int alpha, int beta)
        {
            if (depth == 0 || state.IsGameOver())
                return state.EvaluateBoard();

            int bestEval = isMaximizingPlayer ? int.MinValue : int.MaxValue;
            foreach (Move move in state.GetAllLegalMoves())
            {
                GameState copy = state.Copy();
                move.Execute(copy.Board);

                int eval = Minimax(copy, depth - (move.IsCapture(copy.Board) ? 0 : 1), !isMaximizingPlayer, alpha, beta);
                if (isMaximizingPlayer)
                {
                    bestEval = Math.Max(bestEval, eval);
                    alpha = Math.Max(alpha, bestEval);
                }
                else
                {
                    bestEval = Math.Min(bestEval, eval);
                    beta = Math.Min(beta, bestEval);
                }

                if (beta <= alpha)
                    break;
            }
            return bestEval;
        }
        public static readonly int[,] EarlyGamePieceSquareScore = new int[6, 64]
        {
            {   // Pawn
                0,   0,   0,   0,   0,   0,  0,   0,
                98, 134,  61,  95,  68, 126, 34, -11,
                -6,   7,  26,  31,  65,  56, 25, -20,
                -14,  13,   6,  21,  23,  12, 17, -23,
                -27,  -2,  -5,  12,  17,   6, 10, -25,
                -26,  -4,  -4, -10,   3,   3, 33, -12,
                -35,  -1, -20, -23, -15,  24, 38, -22,
                    0,   0,   0,   0,   0,   0,  0,   0
            },
            {   // Knight
                -167, -15, -34, -49,  61, -97, -15, -107,
                -73, -41,  72,  36,  23,  62,   7,  -17,
                -47,  60,  37,  65,  84, 94,  73,   44,
                -9,  17,  19,  53,  37,  69,  18,   22,
                -13,   4,  16,  13,  28,  19,  21,   -8,
                -23,  -9,  12,  10,  19,  17,  25,  -16,
                -29, -53, -12,  -3,  -1,  18, -14,  -19,
                -105, -21, -58, -33, -17, -28, -19,  -23
            },
            {   // Bishop
                -29,   4, -82, -37, -25, -42,   7,  -8,
                -26,  16, -18, -13,  30,  59,  18, -47,
                -16,  37,  43,  40,  35,  50,  37,  -2,
                -4,   5,  19,  50,  37,  37,   7,  -2,
                -6,  13,  13,  26,  34,  12,  10,   4,
                    0,  15,  15,  15,  14,  27,  18,  10,
                    4,  15,  16,   0,   7,  21,  33,   1,
                -33,  -3, -14, -21, -13, -12, -39, -21
            },
            {   // Rook
                32,  42,  32,  51, 63,  9,  31,  43,
                27,  32,  58,  62, 80, 67,  26,  44,
                -5,  19,  26,  36, 17, 45,  61,  16,
                -24, -11,   7,  26, 24, 35,  -8, -20,
                -36, -26, -12,  -1,  9, -7,   6, -23,
                -45, -25, -16, -17,  3,  0,  -5, -33,
                -44, -16, -20,  -9, -1, 11,  -6, -71,
                -19, -13,   1,  17, 16,  7, -37, -26
            },
            {   // Queen
                -28,   0,  29,  12,  59,  44,  43,  45,
                -24, -39,  -5,   1, -16,  57,  28,  54,
                -13, -17,   7,   8,  29,  56,  47,  57,
                -27, -27, -16, -16,  -1,  17,  -2,   1,
                -9, -26,  -9, -10,  -2,  -4,   3,  -3,
                -14,   2, -11,  -2,  -5,   2,  14,   5,
                -35,  -8,  11,   2,   8,  15,  -3,   1,
                    -1, -18,  -9,  10, -15, -25, -31, -50
            },
            {   // King
                -65,  23,  16, -15, -56, -34,   2,  13,
                    29,  -1, -20,  -7,  -8,  -4, -38, -29,
                    -9,  24,   2, -16, -20,   6,  22, -22,
                -17, -20, -12, -27, -30, -25, -14, -36,
                -49,  -1, -27, -39, -46, -44, -33, -51,
                -14, -14, -22, -46, -44, -30, -15, -27,
                    1,   7,  -8, -64, -43, -16,   9,   8,
                -15,  36,  12, -54,   8, -28,  24,  14
            }
    };
        public static readonly int[,] EndGamePieceSquareScore = new int[6, 64]
{
    {   // Pawn (Endgame)
        0,   0,   0,   0,   0,   0,  0,   0,
        178, 173, 158, 134, 147, 132, 165, 187,
        94,  100,  85,  67,  56,  53,  82,  84,
        32,   24,  13,   5, -2,   4,  17,  17,
        13,    9,  -3,  -7, -7,  -8,   3,  -1,
        4,   7,   6,  -6, -10,  -3,  -9,  -2,
        9,    8,   8,  -6, -3,  -9,  -4,  11,
        0,   0,   0,   0,   0,   0,  0,   0
    },
    {   // Knight (Endgame)
        -58, -38, -13, -28, -31, -27, -63, -99,
        -25,  -8, -25,  -2,  -9, -25, -24, -52,
        -24,  -20, 10,   9,  -1,  -9, -19, -41,
        -17,    3,  22,  22,  22,  11,   8, -18,
        -18,   -6,  16,  25,  16,  17,   4, -18,
        -23,   -3,  -1,  15,  10,  -3,  -20, -22,
        -42,  -20, -10,  -5,  -2, -20, -23, -44,
        -29, -51, -23, -15, -22, -18, -50, -64
    },
    {   // Bishop (Endgame)
        -14, -21, -11,  -8, -7, -9, -17, -24,
        -8,   2,  -3,  -1, -2,  6,   0,  -9,
        -6,   6,   9,   7,  6,  10,  3,  -9,
        -3,   6,  13,   7, 10,  13,   3,  -7,
        4,    7,   12,  18,  19,  12,  7,  -1,
        -6,   0,   15,  25,  25,  15,   0, -8,
        -8,  -5,  -7, -2, -6, -3, -5, -12,
        -16, -14, -9, -8, -10, -13, -17, -21
    },
    {   // Rook (Endgame)
        13,  10,  18,  15, 12,  12,   8,   5,
        11,  13,  13,  11, -3,   3,   8,   3,
        7,    7,   7,   5,   4,  -3, -5,  -3,
        4,    3,  13,   1,   2,   1,  -1,   2,
        3,    5,   8,   4,  -5,  -6,  -8, -11,
        -4,   0,  -5, -1,  -7, -12,  -8, -16,
        -6,  -6,   0,   2,  -9,  -9, -11,  -3,
        -9,   2,   3,  -1,  -5,  -13,  4, -20
    },
    {   // Queen (Endgame)
        -9,   22,  22,  27,  27,  19,  10,  20,
        -17,  20,  32,  41,  58,  25,  30,   0,
        -20,  12,  23,  45,  49,  56,  41,  12,
        -16,  16,  23,  29,  32,  26,  24,  13,
        -15,   7,  19,  24,  24,  25,   3, -12,
        -8,   11,   3,  13,   7,  13,   9,  -2,
        -12,  -6,   0,   8,   3,   7,  -5, -10,
        -20,  -7, -15, -17, -25, -24, -20, -32
    },
    {   // King (Endgame)
        -50, -30, -30, -50, -50, -30, -30, -50,
        -30, -20, -10,   0,   0, -10, -20, -30,
        -30, -10,  20,  30,  30,  20, -10, -30,
        -30, -10,  30,  40,  40,  30, -10, -30,
        -30, -10,  30,  40,  40,  30, -10, -30,
        -30, -10,  20,  30,  30,  20, -10, -30,
        -30, -20, -10,   0,   0, -10, -20, -30,
        -50, -30, -30, -50, -50, -30, -30, -50
    }
};
    }
}
