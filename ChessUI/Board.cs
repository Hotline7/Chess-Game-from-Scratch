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

        // Castling tracking tracking mask (4 bits)
        // Bit 0 (1): White King-side (WK)
        // Bit 1 (2): White Queen-side (WQ)
        // Bit 2 (4): Black King-side (BK)
        // Bit 3 (8): Black Queen-side (BQ)
        public byte CastlingRights = 15;
        public int EnPassantTarget = -1;

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
            
            CastlingRights = 15;
            EnPassantTarget = -1;

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
            
            // Ensure deep clones inherit active performance bitmasks perfectly
            clone.CastlingRights = this.CastlingRights;
            clone.EnPassantTarget = this.EnPassantTarget;
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

            // Cache if this move was marked as an En Passant capture BEFORE executing
            bool isEpCapture = movingPieceType == (int)Piece.Pawn && move.Flags == 5;

            // 1. Move our piece execution
            Pieces[(int)us, movingPieceType] &= ~fromMask; // Remove from source
            Pieces[(int)us, movingPieceType] |= toMask;  // Place on target

            // 2. Handle standard captures
            for (int p = 0; p < 6; p++)
            {
                if ((Pieces[(int)them, p] & toMask) != 0)
                {
                    Pieces[(int)them, p] &= ~toMask;
                    break;
                }
            }
            //3. En Passant Mechanics
            if (isEpCapture)
            {
                // The enemy pawn sits on the same rank as our starting pawn, but on the landing file
                int enemyPawnSquare = (us == Colour.White) ? (move.ToSquare - 8) : (move.ToSquare + 8);
                ulong enemyPawnMask = 1UL << enemyPawnSquare;

                // Vaporize the enemy pawn out of existence
                Pieces[(int)them, (int)Piece.Pawn] &= ~enemyPawnMask;
            }

            // 4. Pawn Promotion Execution
            if (movingPieceType == (int)Piece.Pawn && move.IsPromotion)
            {
                Pieces[(int)us, (int)Piece.Pawn] &= ~toMask;
                int promotionCode = move.Flags & 0x3; 
                int chosenPiece = (int)Piece.Queen;
                if (promotionCode == 0) chosenPiece = (int)Piece.Knight;
                else if (promotionCode == 1) chosenPiece = (int)Piece.Bishop;
                else if (promotionCode == 2) chosenPiece = (int)Piece.Rook;
                Pieces[(int)us, chosenPiece] |= toMask;
            }

            // 5. Castling Secondary Piece Manipulation
            if (movingPieceType == (int)Piece.King && (move.Flags == 2 || move.Flags == 3))
            {
                int rookFrom = -1; int rookTo = -1;
                if (us == Colour.White)
                {
                    if (move.Flags == 2) { rookFrom = 7;  rookTo = 5; }
                    else                 { rookFrom = 0;  rookTo = 3; }
                }
                else
                {
                    if (move.Flags == 2) { rookFrom = 63; rookTo = 61; }
                    else                 { rookFrom = 56; rookTo = 59; }
                }
                Pieces[(int)us, (int)Piece.Rook] &= ~(1UL << rookFrom);
                Pieces[(int)us, (int)Piece.Rook] |= (1UL << rookTo);
            }

            // 6. EN PASSANT STATE HISTORICAL TRACKING
            // If a pawn just pushed 2 squares forward, mark the square it skipped over
            if (movingPieceType == (int)Piece.Pawn && Math.Abs(move.ToSquare - move.FromSquare) == 16)
            {
                EnPassantTarget = (us == Colour.White) ? (move.FromSquare + 8) : (move.FromSquare - 8);
            }
            else
            {
                // Any other move type completely wipes the en passant window out
                EnPassantTarget = -1;
            }

            // 7. Update Historical Castling Rights
            if (movingPieceType == (int)Piece.King)
            {
                if (us == Colour.White) CastlingRights &= 0b1100;
                else                    CastlingRights &= 0b0011;
            }
            if (move.FromSquare == 7  || move.ToSquare == 7)  CastlingRights &= 0b1110;
            if (move.FromSquare == 0  || move.ToSquare == 0)  CastlingRights &= 0b1101;
            if (move.FromSquare == 63 || move.ToSquare == 63) CastlingRights &= 0b1011;
            if (move.FromSquare == 56 || move.ToSquare == 56) CastlingRights &= 0b0111;

            // 8. Recompute entire occupancy and pass the turn
            IsWhiteToMove = !IsWhiteToMove;
            UpdateOccupancy();
        }
    }
}