using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.ExceptionServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Shapes;
using ChessLogic;

namespace ChessUI
{
    public partial class MainWindow : Window
    {
        private readonly Image[,] pieceImages = new Image[8, 8];
        private readonly Rectangle[,] hightlights = new Rectangle[8, 8];
        private readonly Dictionary<Position, Move> moveCache = new();

        private GameState gameState;
        private Position selectedPos = null;
        public MainWindow()
        {
            InitializeComponent();
            InitializeBoard();

            gameState = new GameState(Player.White, Board.Initital());
            DrawBoard(gameState.Board);
            SetCursor(gameState.CurrentPlayer);
        }

        private void InitializeBoard()
        {
            for (int r = 0; r < 8; r++)
            {
                for (int c = 0; c < 8; c++)
                {
                    Image image = new Image();
                    pieceImages[r, c] = image;
                    PieceGrid.Children.Add(image);

                    Rectangle highlight = new Rectangle();
                    hightlights[r, c] = highlight;
                    HighLightGrid.Children.Add(highlight);
                }
            }
        }

        private void DrawBoard(Board board)
        {
            for (int r = 0; r < 8; r++)
            {
                for (int c = 0; c < 8; c++)
                {
                    Piece piece = board[r, c];
                    pieceImages[r, c].Source = Images.GetImage(piece);
                }
            }
        }

        private void SetCursor(Player player)
        {
            if (player == Player.White)
                Cursor = ChessCursors.WhiteCursor;
            else
                Cursor = ChessCursors.BlackCursor;
        }

        private void BoardGrid_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (IsMenuOnScreen())
                return;

            // Get the clicked position
            Point point = e.GetPosition(BoardGrid);
            Position clickedPos = ToSquarePosition(point);

            if (gameState.CurrentPlayer == Player.White)
            {
                HandlePlayerMove(clickedPos);

                if (gameState.IsGameOver())
                {
                    ShowGameOver();
                    return;
                }
            }
            // Now it's AI's turn (Black player)
            HandleAIMove();
        }

        private void HandlePlayerMove(Position clickedPos)
        {
            gameState.CheckForGameOver();

            if (gameState.IsGameOver())
            {
                ShowGameOver();
                return;
            }
            if (selectedPos == null)
            {
                selectedPos = clickedPos;
                moveCache.Clear();

                foreach (Move move in gameState.LegalMovesForPiece(selectedPos))
                {
                    moveCache[move.ToPos] = move;
                }

                ShowHighlights();
            }
            else
            {
                if (moveCache.ContainsKey(clickedPos))
                {

                    Move playerMove = moveCache[clickedPos];
                    playerMove.Execute(gameState.Board);

                    DrawBoard(gameState.Board);
                    selectedPos = null;
                    HideHighlights();
                    gameState.CurrentPlayer = Player.Black;
                    // Switch to the next turn (which will be AI's if it's black's turn)
                }
                else
                {
                    selectedPos = null;
                    HideHighlights();
                }
            }
        }

        public void HandleAIMove()
        {
            if (gameState.CurrentPlayer == Player.Black)
            {
                gameState.CheckForGameOver();

                if (gameState.IsGameOver())
                {
                    ShowGameOver();
                    return;
                }
                Move bestMove = gameState.GetBestMove(3);  // Minimax AI with depth 3
                bestMove.Execute(gameState.Board);

                DrawBoard(gameState.Board);  // Update the UI after AI move
                gameState.CurrentPlayer = Player.White;
                SetCursor(gameState.CurrentPlayer);  // Set the cursor back to white
            }
        }

        private Position ToSquarePosition(Point point)
        {
            double squareSize = BoardGrid.ActualWidth / 8;
            int row = (int)(point.Y / squareSize);
            int col = (int)(point.X / squareSize);

            return new Position(row, col);
        }

        private void CacheMoves(IEnumerable<Move> moves)
        {
            moveCache.Clear();
            foreach (Move move in moves)
            {
                moveCache[move.ToPos] = move;
            }
        }

        private void ShowHighlights()
        {
            Color colorWhite = Color.FromRgb(255, 255, 255);
            Color colorBlack = Color.FromRgb(0, 0, 0);

            foreach (Position to in moveCache.Keys)
            {
                if (gameState.CurrentPlayer == Player.White)
                {
                    hightlights[to.Row, to.Column].Fill = new SolidColorBrush(colorWhite);
                    hightlights[to.Row, to.Column].Opacity = 0.4;
                }
                else
                {
                    hightlights[to.Row, to.Column].Fill = new SolidColorBrush(colorBlack);
                    hightlights[to.Row, to.Column].Opacity = 0.4;
                }
            }
        }

        private void HideHighlights()
        {
            foreach (Position to in moveCache.Keys)
            {
                hightlights[to.Row, to.Column].Fill = Brushes.Transparent;
            }
        }

        private bool IsMenuOnScreen()
        {
            return MenuContainer.Content != null;
        }

        public void ShowGameOver()
        {
            GameOverMenu gameOverMenu = new GameOverMenu(gameState);
            MenuContainer.Content = gameOverMenu;

            gameOverMenu.OptionSelected += option =>
            {
                if (option == Option.Restart) { MenuContainer.Content = null; RestartGame(); }
                else Application.Current.Shutdown();
            };
        }

        private void RestartGame()
        {
            HideHighlights();
            moveCache.Clear();
            gameState = new GameState(Player.White, Board.Initital());
            DrawBoard(gameState.Board);
            SetCursor(gameState.CurrentPlayer);
        }
    }
}
