using System;
using System.Drawing;

namespace ChessConsole
{
    public enum Colour
    {
        White = 0, Black = 1
    }

    public enum Piece
    {
        Pawn = 0, Rook = 1, Knight = 2, Bishop = 3, Queen = 4, King = 5
    }

    public class Bitboard
    {
        public ulong[,] Pieces = new ulong [2,6];
        public ulong[] ColourOccupancy = new ulong [2];
        public ulong CombinedOccupancy;
        public bool IsWhiteToMove;

        public Bitboard()
        {
            InitialiseStandardGame();
        }

        public void InitialiseStandardGame()
        {
            Array.Clear(Pieces, 0, Pieces.Length);
            
            // White Pieces Starting Position
            Pieces[(int)Colour.White, (int)Piece.Pawn] = 0x000000000000FF00UL;
            Pieces[(int)Colour.White, (int)Piece.Rook] = 0x0000000000000081UL;
            Pieces[(int)Colour.White, (int)Piece.Knight] = 0x0000000000000042UL;
            Pieces[(int)Colour.White, (int)Piece.Bishop] = 0x0000000000000024UL;
            Pieces[(int)Colour.White, (int)Piece.Queen]  = 0x0000000000000008UL;
            Pieces[(int)Colour.White, (int)Piece.King]   = 0x0000000000000010UL;

            // Black Pieces Starting Position
            Pieces[(int)Colour.Black, (int)Piece.Pawn]   = 0x00FF000000000000UL;
            Pieces[(int)Colour.Black, (int)Piece.Rook]   = 0x8100000000000000UL;
            Pieces[(int)Colour.Black, (int)Piece.Knight] = 0x4200000000000000UL;
            Pieces[(int)Colour.Black, (int)Piece.Bishop] = 0x2400000000000000UL;
            Pieces[(int)Colour.Black, (int)Piece.Queen]  = 0x0800000000000000UL;
            Pieces[(int)Colour.Black, (int)Piece.King]   = 0x1000000000000000UL;

            IsWhiteToMove = true;

            UpdateOccupancy();
        }
        
        public void UpdateOccupancy()
        {
            ColourOccupancy[(int)Colour.White] = 0;
            ColourOccupancy[(int)Colour.Black] = 0;

            for (int p = 0; p < 6; p++)
            {
                ColourOccupancy[(int)Colour.White] |= Pieces[(int)Colour.White, p];
                ColourOccupancy[(int)Colour.Black] |= Pieces[(int)Colour.Black, p];
            }

            CombinedOccupancy = ColourOccupancy[(int)Colour.White] | ColourOccupancy[(int)Colour.Black];
        }
    }
}