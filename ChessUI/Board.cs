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

        // NEW: Castling tracking tracking mask (4 bits)
        // Bit 0 (1): White King-side (WK)
        // Bit 1 (2): White Queen-side (WQ)
        // Bit 2 (4): Black King-side (BK)
        // Bit 3 (8): Black Queen-side (BQ)
        public byte CastlingRights = 15; 

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
            
            // NEW: Reset castling state to fully enabled on fresh initialization
            CastlingRights = 15; 

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
            
            // NEW: Ensure deep clones inherit active performance bitmasks perfectly
            clone.CastlingRights = this.CastlingRights; 
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

            // 3. Pawn Promotion Execution
            if (movingPieceType == (int)Piece.Pawn && move.IsPromotion)
            {
                Pieces[(int)us, (int)Piece.Pawn] &= ~toMask;

                int promotionCode = move.Flags & 0x3; 
                
                int chosenPiece = (int)Piece.Queen; // Fallback
                if (promotionCode == 0) chosenPiece = (int)Piece.Knight;
                else if (promotionCode == 1) chosenPiece = (int)Piece.Bishop;
                else if (promotionCode == 2) chosenPiece = (int)Piece.Rook;
                else if (promotionCode == 3) chosenPiece = (int)Piece.Queen;

                Pieces[(int)us, chosenPiece] |= toMask;
            }

            // NEW 4. Castling Secondary Piece Manipulation (Snap the Rook over)
            if (movingPieceType == (int)Piece.King && (move.Flags == 2 || move.Flags == 3))
            {
                int rookFrom = -1;
                int rookTo = -1;

                if (us == Colour.White)
                {
                    if (move.Flags == 2) { rookFrom = 7;  rookTo = 5; } // h1 -> f1
                    else                 { rookFrom = 0;  rookTo = 3; } // a1 -> d1
                }
                else // Black
                {
                    if (move.Flags == 2) { rookFrom = 63; rookTo = 61; } // h8 -> f8
                    else                 { rookFrom = 56; rookTo = 59; } // a8 -> d8
                }

                ulong rookFromMask = 1UL << rookFrom;
                ulong rookToMask = 1UL << rookTo;

                // Move Rook on bitboard
                Pieces[(int)us, (int)Piece.Rook] &= ~rookFromMask;
                Pieces[(int)us, (int)Piece.Rook] |= rookToMask;
            }

            // NEW 5. Update Historical Castling Rights (Strip status on movement or capture)
            if (movingPieceType == (int)Piece.King)
            {
                // King moves wipe out both rights for that player permanently
                if (us == Colour.White) CastlingRights &= 0b1100; // Keep Black, strip White
                else                    CastlingRights &= 0b0011; // Keep White, strip Black
            }

            // Clear individual options if rooks leave initial corners or are taken
            if (move.FromSquare == 7  || move.ToSquare == 7)  CastlingRights &= 0b1110; // Strip WK
            if (move.FromSquare == 0  || move.ToSquare == 0)  CastlingRights &= 0b1101; // Strip WQ
            if (move.FromSquare == 63 || move.ToSquare == 63) CastlingRights &= 0b1011; // Strip BK
            if (move.FromSquare == 56 || move.ToSquare == 56) CastlingRights &= 0b0111; // Strip BQ

            // 6. Recompute entire occupancy and pass the turn
            IsWhiteToMove = !IsWhiteToMove;
            UpdateOccupancy();
        }
    }
}