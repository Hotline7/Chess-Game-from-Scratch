using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace ChessUI
{
    public partial class MainWindow : Window
    {
        private Bitboard _board;
        private bool _isFlipped = false;
        private int _selectedSquare = -1; // -1 means no piece is selected right now
        private System.Collections.Generic.HashSet<int> _legalTargetsForSelectedPiece = new System.Collections.Generic.HashSet<int>();

        public MainWindow()
        {
            InitializeComponent();
            _board = new Bitboard();
            RenderBoard();
        }

        private void RenderBoard()
        {
            // Clear current graphical elements
            ChessBoardGrid.Children.Clear();

            // Iterate through files and ranks to create the visual representation
            for (int r = 7; r >= 0; r--) // Rank 8 down to 1
            {
                for (int f = 0; f < 8; f++) // File A to H
                {
                    // Apply our mathematical transformation if the perspective is flipped
                    int rank = _isFlipped ? 7 - r : r;
                    int file = _isFlipped ? 7 - f : f;
                    int squareIndex = rank * 8 + file;

                    // 1. Establish background styling framework
                    bool isLightSquare = (rank + file) % 2 != 0;
                    Color baseColor = isLightSquare ? Color.FromRgb(240, 217, 181) : Color.FromRgb(181, 136, 99);

                    // If this is the piece the player clicked, keep it highlighted Green
                    if (squareIndex == _selectedSquare)
                    {
                        baseColor = Colors.LightGreen;
                    }

                    Border square = new Border
                    {
                        Background = new SolidColorBrush(baseColor),
                        Tag = squareIndex
                    };

                    square.MouseLeftButtonDown += Square_MouseLeftButtonDown;

                    // Create a Grid container inside the border so we can layer the piece image and the move indicator on top of each other
                    Grid cellGrid = new Grid();
                    square.Child = cellGrid;

                    // 2. Query our bitboard to identify what piece occupies this block
                    Image? pieceImage = GetPieceImageForSquare(squareIndex);
                    if (pieceImage != null)
                    {
                        cellGrid.Children.Add(pieceImage);
                    }

                    // 3. NEW: Draw the legal move visual indicator shadow
                    if (_legalTargetsForSelectedPiece.Contains(squareIndex))
                    {
                        // Create a subtle dot overlay
                        System.Windows.Shapes.Ellipse dot = new System.Windows.Shapes.Ellipse
                        {
                            Width = 16,
                            Height = 16,
                            Fill = new SolidColorBrush(Color.FromArgb(80, 0, 0, 0)), // Semi-transparent black shadow
                            HorizontalAlignment = HorizontalAlignment.Center,
                            VerticalAlignment = VerticalAlignment.Center,
                            IsHitTestVisible = false // Ensure click passes through
                        };
                        cellGrid.Children.Add(dot);
                    }

                    // Append grid element to layout hierarchy
                    ChessBoardGrid.Children.Add(square);
                }
            }
        }

        private Image? GetPieceImageForSquare(int squareIndex)
        {
            ulong mask = 1UL << squareIndex;

            // Step through colours and variants to verify occupancy
            for (int c = 0; c < 2; c++)
            {
                for (int p = 0; p < 6; p++)
                {
                    if ((_board.Pieces[c, p] & mask) != 0)
                    {
                        string colourName = (c == 0) ? "White" : "Black";
                        string pieceName = Enum.GetName(typeof(Piece), p) ?? "Pawn";

                        // Locate image binary string inside embedded asset bundle pipeline
                        string uriPath = $"pack://application:,,,/Assets/{colourName}/{pieceName}.png";
                        
                        return new Image
                        {
                            Source = new BitmapImage(new Uri(uriPath)),
                            Margin = new Thickness(4),
                            IsHitTestVisible = false
                        };
                    }
                }
            }
            return null;
        }

        private void FlipBoard_Click(object sender, RoutedEventArgs e)
        {
            _isFlipped = !_isFlipped;
            RenderBoard();
        }

        private void Square_MouseLeftButtonDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            if (sender is Border clickedSquare && clickedSquare.Tag is int squareIndex)
            {
                // Case 1: First click (Selecting a piece)
                if (_selectedSquare == -1)
                {
                    ulong mask = 1UL << squareIndex;
                    Colour activeColor = _board.IsWhiteToMove ? Colour.White : Colour.Black;
                    ulong friendlyOccupancy = _board.ColourOccupancy[(int)activeColor];

                    if ((friendlyOccupancy & mask) != 0)
                    {
                        _selectedSquare = squareIndex;
                        
                        // Fetch all strict legal moves and harvest target squares for just this piece
                        System.Collections.Generic.List<Move> legalMoves = MoveGenerator.GenerateLegalMoves(_board);
                        _legalTargetsForSelectedPiece.Clear();
                        
                        foreach (Move move in legalMoves)
                        {
                            if (move.FromSquare == _selectedSquare)
                            {
                                _legalTargetsForSelectedPiece.Add(move.ToSquare);
                            }
                        }

                        RenderBoard();
                    }
                }
                // Case 2: Second click (Target destination)
                else
                {
                    int fromSquare = _selectedSquare;
                    int toSquare = squareIndex;
                    
                    // Clear selection states immediately
                    _selectedSquare = -1; 
                    _legalTargetsForSelectedPiece.Clear();

                    System.Collections.Generic.List<Move> legalMoves = MoveGenerator.GenerateLegalMoves(_board);
                    Move chosenMove = new Move();
                    bool isValid = false;

                    foreach (Move move in legalMoves)
                    {
                        if (move.FromSquare == fromSquare && move.ToSquare == toSquare)
                        {
                            chosenMove = move;
                            isValid = true;
                            break;
                        }
                    }

                    if (isValid)
                    {
                        // 1. Physically update the board layout configurations
                        _board.MakeMove(chosenMove);

                        // 2. FORCE the UI to draw the piece landing on its new square BEFORE any alert box fires
                        RenderBoard();

                        // 3. Let WPF refresh its window handles instantly
                        Application.Current.Dispatcher.Invoke(System.Windows.Threading.DispatcherPriority.Render, new Action(() => { }));

                        // 4. Query the game state engine safely
                        string gameState = MoveGenerator.EvaluateGameEndState(_board);

                        if (gameState.StartsWith("Checkmate") || gameState.StartsWith("Draw"))
                        {
                            MessageBox.Show(gameState, "Game Over", MessageBoxButton.OK, MessageBoxImage.Information);
                            
                            // Reset game structure
                            _board = new Bitboard(); 
                            RenderBoard();
                            return;
                        }
                        else if (gameState == "Check")
                        {
                            MessageBox.Show("Check!", "Warning", MessageBoxButton.OK, MessageBoxImage.Warning);
                            return;
                        }
                    }

                    // Fallback render to clear selections if the click was invalid
                    RenderBoard();
                }
            }
        }
    }
}