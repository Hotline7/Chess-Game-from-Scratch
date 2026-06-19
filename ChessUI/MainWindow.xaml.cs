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
                    Border square = new Border
                    {
                        Background = new SolidColorBrush(isLightSquare ? Color.FromRgb(240, 217, 181) : Color.FromRgb(181, 136, 99)),
                        Tag = squareIndex // Track the bitboard structural coordinate directly on the UI object
                    };

                    square.MouseLeftButtonDown += Square_MouseLeftButtonDown;

                    // 2. Query our bitboard to identify what piece occupies this block
                    Image pieceImage = GetPieceImageForSquare(squareIndex);
                    if (pieceImage != null)
                    {
                        square.Child = pieceImage;
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

                    // Verify they clicked one of their own active pieces before selecting
                    if ((friendlyOccupancy & mask) != 0)
                    {
                        _selectedSquare = squareIndex;
                        clickedSquare.Background = new SolidColorBrush(Colors.LightGreen); // Highlight it!
                    }
                }
                // Case 2: Second click (Target destination destination)
                else
                {
                    int fromSquare = _selectedSquare;
                    int toSquare = squareIndex;
                    _selectedSquare = -1; // Clear selection state immediately

                    // 1. Fetch all strict legal moves for the current board state
                    System.Collections.Generic.List<Move> legalMoves = MoveGenerator.GenerateLegalMoves(_board);

                    // 2. See if our intended action matches any legal moves
                    Move chosenMove = new Move();
                    bool isValid = false;

                    foreach (Move move in legalMoves)
                    {
                        // Note: Checking basic coordinates (ignoring special flags for now)
                        if (move.FromSquare == fromSquare && move.ToSquare == toSquare)
                        {
                            chosenMove = move;
                            isValid = true;
                            break;
                        }
                    }

                    if (isValid)
                    {
                        // Execute permanently on our core bitboard structures!
                        _board.MakeMove(chosenMove);
                    }

                    // Refresh the graphics layer entirely
                    RenderBoard();
                }
            }
        }
    }
}