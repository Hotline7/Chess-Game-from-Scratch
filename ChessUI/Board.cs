using System;
using System.Drawing;

namespace ChessUI
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

        public Bitboard Clone()
        {
            Bitboard clone = new Bitboard();
            Array.Copy(this.Pieces, clone.Pieces, this.Pieces.Length);
            Array.Copy(this.ColourOccupancy, clone.ColourOccupancy, this.ColourOccupancy.Length);
            clone.CombinedOccupancy = this.CombinedOccupancy;
            clone.IsWhiteToMove = this.IsWhiteToMove;
            return clone;
        }

        public void MakeMove(Move move)
        {
            Colour us = IsWhiteToMove ? Colour.White : Colour.Black;
            Colour them = IsWhiteToMove ? Colour.Black : Colour.White;

            ulong fromMask = 1UL << move.FromSquare;
            ulong toMask = 1UL << move.ToSquare;

            // Find what piece is moving
            int movingPieceType = -1;
            for (int p = 0; p < 6; p++)
            {
                if ((Pieces[(int)us, p] & fromMask) != 0)
                {
                    movingPieceType = p;
                    break;
                }
            }

            if (movingPieceType == -1) return; // Safety check

            // 1. Move our piece execution
            Pieces[(int)us, movingPieceType] &= ~fromMask; // Remove from source
            Pieces[(int)us, movingPieceType] |= toMask;  // Place on target

            // 2. Handle captures (if an enemy piece sits on the target square, vaporize it)
            for (int p = 0; p < 6; p++)
            {
                if ((Pieces[(int)them, p] & toMask) != 0)
                {
                    Pieces[(int)them, p] &= ~toMask;
                    break;
                }
            }

            // 3. NEW: Pawn Promotion Execution
            if (movingPieceType == (int)Piece.Pawn)
            {
                int targetRank = move.ToSquare / 8;
                // White reaches rank 8 (index 7) or Black reaches rank 1 (index 0)
                if ((us == Colour.White && targetRank == 7) || (us == Colour.Black && targetRank == 0))
                {
                    // Vaporize the pawn from the target square
                    Pieces[(int)us, (int)Piece.Pawn] &= ~toMask;
                    
                    // Default promote directly to a Queen for now (Auto-Queen framework)
                    Pieces[(int)us, (int)Piece.Queen] |= toMask;
                }
            }

            // 4. Recompute entire occupancy and pass the turn
            IsWhiteToMove = !IsWhiteToMove;
            UpdateOccupancy();
        }
    }
}