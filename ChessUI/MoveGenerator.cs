using System;
using System.Collections.Generic;
using System.Numerics;

namespace ChessUI
{
    public static class MoveGenerator
    {
        // An array holding the attack patterns for all 64 squares
        public static ulong[] KnightAttacks = new ulong[64];
        public static ulong[] KingAttacks = new ulong[64];

        // File masks to prevent board-wrapping bugs
        private const ulong FileA = 0x0101010101010101UL;
        private const ulong FileB = 0x0202020202020202UL;
        private const ulong FileG = 0x4040404040404040UL;
        private const ulong FileH = 0x8080808080808080UL;

        // Invert them to get "Everything EXCEPT this file"
        private const ulong NotA = ~FileA;
        private const ulong NotAB = ~(FileA | FileB);
        private const ulong NotH = ~FileH;
        private const ulong NotGH = ~(FileG | FileH);

        static MoveGenerator()
        {
            PrecomputeKnightAttacks();
            PrecomputeKingAttacks();
        }

        private static void PrecomputeKnightAttacks()
        {
            for (int square = 0; square < 64; square++)
            {
                ulong knight = 1UL << square;
                ulong attacks = 0;

                attacks |= (knight << 17) & NotA;
                attacks |= (knight << 10) & NotAB;
                attacks |= (knight >> 6)  & NotAB;
                attacks |= (knight >> 15) & NotA;

                attacks |= (knight << 15) & NotH;
                attacks |= (knight << 6)  & NotGH;
                attacks |= (knight >> 10) & NotGH;
                attacks |= (knight >> 17) & NotH;

                KnightAttacks[square] = attacks;
            }
        }

        private static void PrecomputeKingAttacks()
        {
            for (int square = 0; square < 64; square++)
            {
                ulong king = 1UL << square;
                ulong attacks = 0;

                attacks |= (king << 7) & NotH;
                attacks |= (king << 8);
                attacks |= (king << 9) & NotA;
                attacks |= (king >> 1) & NotH;
                attacks |= (king << 1) & NotA;
                attacks |= (king >> 9) & NotH;
                attacks |= (king >> 8);
                attacks |= (king >> 7) & NotA;

                KingAttacks[square] = attacks;
            }
        }

        public static ulong GetRookMoves(int square, ulong occupied)
        {
            ulong moves = 0UL;
            int rank = square / 8;
            int file = square % 8;

            // North
            for (int r = rank + 1; r < 8; r++)
            {
                int targetIndex = r * 8 + file;
                ulong target = 1UL << targetIndex;
                moves |= target;
                if ((occupied & target) != 0) break;
            }
            // South
            for (int r = rank - 1; r >= 0; r--)
            {
                int targetIndex = r * 8 + file;
                ulong target = 1UL << targetIndex;
                moves |= target;
                if ((occupied & target) != 0) break;
            }
            // East
            for (int f = file + 1; f < 8; f++)
            {
                int targetIndex = rank * 8 + f;
                ulong target = 1UL << targetIndex;
                moves |= target;
                if ((occupied & target) != 0) break;
            }
            // West
            for (int f = file - 1; f >= 0; f--)
            {
                int targetIndex = rank * 8 + f;
                ulong target = 1UL << targetIndex;
                moves |= target;
                if ((occupied & target) != 0) break;
            }

            return moves;
        }

        public static ulong GetBishopMoves(int square, ulong occupied)
        {
            ulong moves = 0UL;
            int rank = square / 8;
            int file = square % 8;

            // North-East
            for (int r = rank + 1, f = file + 1; r < 8 && f < 8; r++, f++)
            {
                int targetIndex = r * 8 + f;
                ulong target = 1UL << targetIndex;
                moves |= target;
                if ((occupied & target) != 0) break;
            }
            // South-East
            for (int r = rank - 1, f = file + 1; r >= 0 && f < 8; r--, f++)
            {
                int targetIndex = r * 8 + f;
                ulong target = 1UL << targetIndex;
                moves |= target;
                if ((occupied & target) != 0) break;
            }
            // North-West
            for (int r = rank + 1, f = file - 1; r < 8 && f >= 0; r++, f--)
            {
                int targetIndex = r * 8 + f;
                ulong target = 1UL << targetIndex;
                moves |= target;
                if ((occupied & target) != 0) break;
            }
            // South-West
            for (int r = rank - 1, f = file - 1; r >= 0 && f >= 0; r--, f--)
            {
                int targetIndex = r * 8 + f;
                ulong target = 1UL << targetIndex;
                moves |= target;
                if ((occupied & target) != 0) break;
            }

            return moves;
        }

        public static ulong GetQueenMoves(int square, ulong occupied)
        {
            return GetRookMoves(square, occupied) | GetBishopMoves(square, occupied);
        }

        public static ulong GetPawnMoves(ulong pawns, ulong occupied, ulong enemyPieces, Colour colour)
        {
            ulong moves = 0UL;
            ulong notA = ~0x0101010101010101UL;
            ulong notH = ~0x8080808080808080UL;

            if (colour == Colour.White)
            {
                ulong singlePush = (pawns << 8) & ~occupied;
                moves |= singlePush;

                ulong rank2 = 0x000000000000FF00UL;
                ulong doublePush = ((pawns & rank2) << 8) & ~occupied;
                doublePush = (doublePush << 8) & ~occupied;
                moves |= doublePush;

                ulong captureLeft = (pawns << 7) & notH & enemyPieces;
                ulong captureRight = (pawns << 9) & notA & enemyPieces;
                moves |= captureLeft | captureRight;
            }
            else
            {
                ulong singlePush = (pawns >> 8) & ~occupied;
                moves |= singlePush;

                ulong rank7 = 0x00FF000000000000UL;
                ulong doublePush = ((pawns & rank7) >> 8) & ~occupied;
                doublePush = (doublePush >> 8) & ~occupied;
                moves |= doublePush;

                ulong captureLeft = (pawns >> 9) & notH & enemyPieces;
                ulong captureRight = (pawns >> 7) & notA & enemyPieces;
                moves |= captureLeft | captureRight;
            }

            return moves;
        }

        public static bool IsSquareAttacked(int square, Colour attackerColour, Bitboard board)
        {
            ulong occupied = board.CombinedOccupancy;
            Colour defenderColour = attackerColour == Colour.White ? Colour.Black : Colour.White;
            
            // 1. Check Enemy Pawns
            ulong enemyPawns = board.Pieces[(int)attackerColour, (int)Piece.Pawn];
            ulong notA = ~0x0101010101010101UL;
            ulong notH = ~0x8080808080808080UL;
            ulong pawnAttacks = defenderColour == Colour.White 
                ? (((1UL << square) << 7) & notH) | (((1UL << square) << 9) & notA)
                : (((1UL << square) >> 9) & notH) | (((1UL << square) >> 7) & notA);
            if ((pawnAttacks & enemyPawns) != 0) return true;

            // 2. Check Enemy Knights
            ulong enemyKnights = board.Pieces[(int)attackerColour, (int)Piece.Knight];
            if ((KnightAttacks[square] & enemyKnights) != 0) return true;

            // 3. Check Enemy Kings
            ulong enemyKing = board.Pieces[(int)attackerColour, (int)Piece.King];
            if ((KingAttacks[square] & enemyKing) != 0) return true;

            // 4. Check Enemy Bishops / Queens
            ulong enemyDiagonalSliders = board.Pieces[(int)attackerColour, (int)Piece.Bishop] | board.Pieces[(int)attackerColour, (int)Piece.Queen];
            if ((GetBishopMoves(square, occupied) & enemyDiagonalSliders) != 0) return true;

            // 5. Check Enemy Rooks / Queens
            ulong enemyStraightSliders = board.Pieces[(int)attackerColour, (int)Piece.Rook] | board.Pieces[(int)attackerColour, (int)Piece.Queen];
            if ((GetRookMoves(square, occupied) & enemyStraightSliders) != 0) return true;

            return false;
        }

        public static List<Move> GeneratePseudoLegalMoves(Bitboard board)
        {
            List<Move> moveList = new List<Move>();
            Colour us = board.IsWhiteToMove ? Colour.White : Colour.Black;
            ulong friendlyPieces = board.ColourOccupancy[(int)us];
            ulong occupied = board.CombinedOccupancy;

            // =========================================================================
            // PAWNS
            // =========================================================================
            ulong pawns = board.Pieces[(int)us, (int)Piece.Pawn];
            ulong enemyPieces = board.ColourOccupancy[board.IsWhiteToMove ? (int)Colour.Black : (int)Colour.White];

            while (pawns != 0)
            {
                int fromSquare = BitOperations.TrailingZeroCount(pawns);
                ulong singlePawnMask = 1UL << fromSquare;
                ulong attackMask = GetPawnMoves(singlePawnMask, occupied, enemyPieces, us);

                while (attackMask != 0)
                {
                    int toSquare = BitOperations.TrailingZeroCount(attackMask);
                    int flag = 0;
                    if (Math.Abs(toSquare - fromSquare) == 16) flag = 1;

                    moveList.Add(new Move(fromSquare, toSquare, flag));
                    attackMask &= (attackMask - 1);
                }
                pawns &= (pawns - 1);
            }

            // =========================================================================
            // KNIGHTS
            // =========================================================================
            ulong knights = board.Pieces[(int)us, (int)Piece.Knight];
            while (knights != 0)
            {
                int fromSquare = BitOperations.TrailingZeroCount(knights);
                ulong attackMask = KnightAttacks[fromSquare] & ~friendlyPieces;
                while (attackMask != 0)
                {
                    int toSquare = BitOperations.TrailingZeroCount(attackMask);
                    moveList.Add(new Move(fromSquare, toSquare));
                    attackMask &= (attackMask - 1);
                }
                knights &= (knights - 1);
            }

            // =========================================================================
            // KINGS & CASTLING
            // =========================================================================
            ulong king = board.Pieces[(int)us, (int)Piece.King];
            while (king != 0)
            {
                int fromSquare = BitOperations.TrailingZeroCount(king);
                ulong attackMask = KingAttacks[fromSquare] & ~friendlyPieces;

                while (attackMask != 0)
                {
                    int toSquare = BitOperations.TrailingZeroCount(attackMask);
                    moveList.Add(new Move(fromSquare, toSquare));
                    attackMask &= (attackMask - 1);
                }

                // Castling Generation
                if (us == Colour.White && fromSquare == 4)
                {
                    // White King-side
                    bool hasWhiteKingSideRight = (board.CastlingRights & 0b0001) != 0;
                    bool squaresEmpty = (board.CombinedOccupancy & ((1UL << 5) | (1UL << 6))) == 0;
                    bool rookOnH1 = (board.Pieces[(int)Colour.White, (int)Piece.Rook] & (1UL << 7)) != 0;
                    bool safePath = !IsSquareAttacked(4, Colour.Black, board) &&
                                    !IsSquareAttacked(5, Colour.Black, board) &&
                                    !IsSquareAttacked(6, Colour.Black, board);

                    if (hasWhiteKingSideRight && squaresEmpty && rookOnH1 && safePath)
                        moveList.Add(new Move(4, 6, 2));

                    // White Queen-side
                    bool hasWhiteQueenSideRight = (board.CastlingRights & 0b0010) != 0;
                    squaresEmpty = (board.CombinedOccupancy & ((1UL << 1) | (1UL << 2) | (1UL << 3))) == 0;
                    bool rookOnA1 = (board.Pieces[(int)Colour.White, (int)Piece.Rook] & (1UL << 0)) != 0;
                    safePath = !IsSquareAttacked(4, Colour.Black, board) &&
                               !IsSquareAttacked(3, Colour.Black, board) &&
                               !IsSquareAttacked(2, Colour.Black, board);

                    if (hasWhiteQueenSideRight && squaresEmpty && rookOnA1 && safePath)
                        moveList.Add(new Move(4, 2, 3));
                }
                else if (us == Colour.Black && fromSquare == 60)
                {
                    // Black King-side
                    bool hasBlackKingSideRight = (board.CastlingRights & 0b0100) != 0;
                    bool squaresEmpty = (board.CombinedOccupancy & ((1UL << 61) | (1UL << 62))) == 0;
                    bool rookOnH8 = (board.Pieces[(int)Colour.Black, (int)Piece.Rook] & (1UL << 63)) != 0;
                    bool safePath = !IsSquareAttacked(60, Colour.White, board) &&
                                    !IsSquareAttacked(61, Colour.White, board) &&
                                    !IsSquareAttacked(62, Colour.White, board);

                    if (hasBlackKingSideRight && squaresEmpty && rookOnH8 && safePath)
                        moveList.Add(new Move(60, 62, 2));

                    // Black Queen-side
                    bool hasBlackQueenSideRight = (board.CastlingRights & 0b1000) != 0;
                    squaresEmpty = (board.CombinedOccupancy & ((1UL << 57) | (1UL << 58) | (1UL << 59))) == 0;
                    bool rookOnA8 = (board.Pieces[(int)Colour.Black, (int)Piece.Rook] & (1UL << 56)) != 0;
                    safePath = !IsSquareAttacked(60, Colour.White, board) &&
                               !IsSquareAttacked(59, Colour.White, board) &&
                               !IsSquareAttacked(58, Colour.White, board);

                    if (hasBlackQueenSideRight && squaresEmpty && rookOnA8 && safePath)
                        moveList.Add(new Move(60, 58, 3));
                }

                king &= (king - 1);
            }

            // =========================================================================
            // BISHOPS, ROOKS, QUEENS SLIDERS
            // =========================================================================
            ulong bishops = board.Pieces[(int)us, (int)Piece.Bishop];
            while (bishops != 0)
            {
                int fromSquare = BitOperations.TrailingZeroCount(bishops);
                ulong attackMask = GetBishopMoves(fromSquare, occupied) & ~friendlyPieces;
                while (attackMask != 0)
                {
                    int toSquare = BitOperations.TrailingZeroCount(attackMask);
                    moveList.Add(new Move(fromSquare, toSquare));
                    attackMask &= (attackMask - 1);
                }
                bishops &= (bishops - 1);
            }

            ulong rooks = board.Pieces[(int)us, (int)Piece.Rook];
            while (rooks != 0)
            {
                int fromSquare = BitOperations.TrailingZeroCount(rooks);
                ulong attackMask = GetRookMoves(fromSquare, occupied) & ~friendlyPieces;
                while (attackMask != 0)
                {
                    int toSquare = BitOperations.TrailingZeroCount(attackMask);
                    moveList.Add(new Move(fromSquare, toSquare));
                    attackMask &= (attackMask - 1);
                }
                rooks &= (rooks - 1);
            }

            ulong queens = board.Pieces[(int)us, (int)Piece.Queen];
            while (queens != 0)
            {
                int fromSquare = BitOperations.TrailingZeroCount(queens);
                ulong attackMask = GetQueenMoves(fromSquare, occupied) & ~friendlyPieces;
                while (attackMask != 0)
                {
                    int toSquare = BitOperations.TrailingZeroCount(attackMask);
                    moveList.Add(new Move(fromSquare, toSquare));
                    attackMask &= (attackMask - 1);
                }
                queens &= (queens - 1);
            }

            return moveList;
        }

        public static List<Move> GenerateLegalMoves(Bitboard board)
        {
            List<Move> pseudoMoves = GeneratePseudoLegalMoves(board);
            List<Move> legalMoves = new List<Move>();

            Colour us = board.IsWhiteToMove ? Colour.White : Colour.Black;

            foreach (Move move in pseudoMoves)
            {
                Bitboard simulatedBoard = board.Clone();
                simulatedBoard.MakeMove(move);

                // Find our King's square on the simulated board
                ulong kingMask = simulatedBoard.Pieces[(int)us, (int)Piece.King];
                int kingSquare = BitOperations.TrailingZeroCount(kingMask);

                // FIX: Pass 'us' as the defender color to evaluate if *we* are left in check.
                // The attacker color is the opponent color *on the simulated board*.
                Colour attackerColor = simulatedBoard.IsWhiteToMove ? Colour.White : Colour.Black;

                if (!IsSquareAttacked(kingSquare, attackerColor, simulatedBoard))
                {
                    legalMoves.Add(move);
                }
            }

            return legalMoves;
        }

        public static string EvaluateGameEndState(Bitboard board)
        {
            List<Move> legalMoves = GenerateLegalMoves(board);

            if (legalMoves.Count > 0)
            {
                Colour us = board.IsWhiteToMove ? Colour.White : Colour.Black;
                Colour them = board.IsWhiteToMove ? Colour.Black : Colour.White;
                ulong kingMask = board.Pieces[(int)us, (int)Piece.King];
                int kingSquare = BitOperations.TrailingZeroCount(kingMask);

                if (IsSquareAttacked(kingSquare, them, board)) return "Check";
                return "Active";
            }

            Colour activePlayer = board.IsWhiteToMove ? Colour.White : Colour.Black;
            Colour opponent = board.IsWhiteToMove ? Colour.Black : Colour.White;
            
            ulong activeKingMask = board.Pieces[(int)activePlayer, (int)Piece.King];
            int activeKingSquare = BitOperations.TrailingZeroCount(activeKingMask);

            if (IsSquareAttacked(activeKingSquare, opponent, board))
            {
                string winner = board.IsWhiteToMove ? "Black" : "White";
                return $"Checkmate! {winner} wins the game.";
            }
            
            return "Draw by Stalemate!";
        }
    }
}