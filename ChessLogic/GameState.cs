﻿using System;
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

        public PieceType getPieceFromPos(Position pos)
        {
            Piece piece = Board[pos.Row, pos.Column];
            return piece.Type;
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

        public Dictionary<PieceType, int> CountPieces(Board board)
        {
            Dictionary<PieceType, int> count = null;
            if (CurrentPlayer == Player.White)
            {
                IEnumerable<Position> whitePositions = board.PiecePositionsFor(CurrentPlayer);
                IEnumerable<Piece> whitePieces = Position.GetPieces(whitePositions, board);
                foreach (Piece piece in whitePieces)
                {
                    if (piece.Color == Player.White)
                    {
                        switch (piece.Type)
                        {
                            case (PieceType.Pawn):
                                count[PieceType.Pawn] += 1;
                                continue;
                            case PieceType.Knight:
                                count[PieceType.Knight] += 1;
                                continue;
                            case PieceType.Bishop:
                                count[PieceType.Bishop] += 1;
                                continue;
                            case PieceType.Rook:
                                count[PieceType.Rook] += 1;
                                continue;
                            case PieceType.Queen:
                                count[PieceType.Queen] += 1;
                                continue;
                        }
                    }
                }
                return count;
            }
            else
            {
                IEnumerable<Position> blackPositions = board.PiecePositionsFor(CurrentPlayer);
                IEnumerable<Piece> blackPieces = Position.GetPieces(blackPositions, board);
                foreach (Piece piece in blackPieces)
                {
                    if (piece.Color == Player.Black)
                    {
                        switch (piece.Type)
                        {
                            case (PieceType.Pawn):
                                count[PieceType.Pawn] += 1;
                                continue;
                            case PieceType.Knight:
                                count[PieceType.Knight] += 1;
                                continue;
                            case PieceType.Bishop:
                                count[PieceType.Bishop] += 1;
                                continue;
                            case PieceType.Rook:
                                count[PieceType.Rook] += 1;
                                continue;
                            case PieceType.Queen:
                                count[PieceType.Queen] += 1;
                                continue;
                        }
                    }
                }
                return count;
            }
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
                int pieceValue = GetPieceValue(piece.Type, pos);
                score += pieceValue;
            }
            return score;
        }

        private int GetPieceValue(PieceType type, Position pos)
        {
            wp = len(board.pieces(chess.PAWN, chess.WHITE))
            bp = len(board.pieces(chess.PAWN, chess.BLACK))
            wn = len(board.pieces(chess.KNIGHT, chess.WHITE))
            bn = len(board.pieces(chess.KNIGHT, chess.BLACK))
            wb = len(board.pieces(chess.BISHOP, chess.WHITE))
            bb = len(board.pieces(chess.BISHOP, chess.BLACK))
            wr = len(board.pieces(chess.ROOK, chess.WHITE))
            br = len(board.pieces(chess.ROOK, chess.BLACK))
            wq = len(board.pieces(chess.QUEEN, chess.WHITE))
            bq = len(board.pieces(chess.QUEEN, chess.BLACK))
            switch (type)
            {
                case PieceType.Pawn: return pieceSquareScore[0, pos.Row * 8 + pos.Column];
                case PieceType.Knight: return pieceSquareScore[1, pos.Row * 8 + pos.Column];
                case PieceType.Bishop: return pieceSquareScore[2, pos.Row * 8 + pos.Column];
                case PieceType.Rook: return pieceSquareScore[3, pos.Row * 8 + pos.Column];
                case PieceType.Queen: return pieceSquareScore[4, pos.Row * 8 + pos.Column];
                case PieceType.King: return pieceSquareScore[5, pos.Row * 8 + pos.Column];
                default: return 0;
            }
        }
        public Move GetBestMove(int depth)
        {
            int score = 0;
            Move bestMove = null;
            int bestScore = int.MinValue;
            List<Move> captures = new List<Move>();

            foreach (Move move in GetAllLegalMoves())
            {
                GameState copy = this.Copy();
                move.Execute(copy.Board);
                if (move.IsLegal(this.Board) && move.ToPos != null)
                    captures.Add(move);
                score = Minimax(copy, depth - 1, false, int.MinValue, int.MaxValue);  // Run minimax for Black's turn
            
                if (score > bestScore)
                {
                    bestScore = score;
                    bestMove = move;
                }
            }
            if (captures.Count > 0)
                bestMove = FindBestCapture(captures);
            return bestMove;
        }

        public Move FindBestCapture(List<Move> captures)
        {
            int bestScore = 0;
            Move bestMove = null;
            int score = 0;
            foreach (Move move in captures)
            {
                score = GetPieceValue(getPieceFromPos(move.FromPos), move.ToPos);
                if (score > bestScore)
                {
                    bestScore = score;
                    bestMove = move;
                }
            }
            return bestMove;
        }

        private int Minimax(GameState state, int depth, bool isMaximizingPlayer, int alpha, int beta)
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
                    int eval = Minimax(copy, depth - 1, false, alpha, beta);
                    maxEval = Math.Max(maxEval, eval);
                    alpha = Math.Max(alpha, maxEval);
                    if (beta <= alpha)
                        break;
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
                    int eval = Minimax(copy, depth - 1, true, alpha, beta);
                    minEval = Math.Min(minEval, eval);
                    beta = Math.Min(beta, minEval);
                    if (beta <= alpha)
                        break;
                }
                return minEval;
            }
        }
        public static readonly int[,] pieceSquareScore = new int[6, 64]
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

    }
}
