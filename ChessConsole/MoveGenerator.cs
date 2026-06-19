using System;
using System.Collections.Generic;
using System.Numerics;

namespace ChessConsole
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

                // Up 2, Right 1 (+17) - Cannot wrap to File A
                attacks |= (knight << 17) & NotA;
                // Up 1, Right 2 (+10) - Cannot wrap to File A or B
                attacks |= (knight << 10) & NotAB;
                // Down 1, Right 2 (-6)  - Cannot wrap to File A or B
                attacks |= (knight >> 6)  & NotAB;
                // Down 2, Right 1 (-15) - Cannot wrap to File A
                attacks |= (knight >> 15) & NotA;

                // Up 2, Left 1 (+15)  - Cannot wrap to File H
                attacks |= (knight << 15) & NotH;
                // Up 1, Left 2 (+6)   - Cannot wrap to File G or H
                attacks |= (knight << 6)  & NotGH;
                // Down 1, Left 2 (-10) - Cannot wrap to File G or H
                attacks |= (knight >> 10) & NotGH;
                // Down 2, Left 1 (-17) - Cannot wrap to File H
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

                // Up 1. Left 1 (+ 7) - Cannot Wrap to File H
                attacks |= (king << 7) & NotH;
                // Up 1 (+ 8)
                attacks |= (king << 8);
                // Up 1, Right 1 (+ 9) - Cannot Wrap to File A
                attacks |= (king << 9) & NotA;
                // Left 1 (-1) - Cannot Crao to File H
                attacks |= (king >> 1) & NotH;
                // Right 1 (+1) - Cannot Wrap to File A
                attacks |= (king << 1) & NotA;
                // Down 1. Left 1 (- 9) - Cannot Wrap to File H
                attacks |= (king >> 9) & NotH;
                // Down 1 (- 8)
                attacks |= (king >> 8);
                // Down 1, Right 1 (- 7) - Cannot Wrap to File A
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

                if ((occupied & target) != 0)
                    break;
            }

            // South
            for (int r = rank - 1; r >= 0; r--)
            {
                int targetIndex = r * 8 + file;
                ulong target = 1UL << targetIndex;

                moves |= target;

                if ((occupied & target) != 0)
                    break;
            }

            // East
            for (int f = file + 1; f < 8; f++)
            {
                int targetIndex = rank * 8 + f;
                ulong target = 1UL << targetIndex;

                moves |= target;

                if ((occupied & target) != 0)
                    break;
            }

            // West
            for (int f = file - 1; f >= 0; f--)
            {
                int targetIndex = rank * 8 + f;
                ulong target = 1UL << targetIndex;

                moves |= target;

                if ((occupied & target) != 0)
                    break;
            }

            return moves;
        }

        public static ulong GetBishopMoves(int square, ulong occupied)
        {
            ulong moves = 0UL;

            int rank = square / 8;
            int file = square % 8;

            // 1. North-East (Up-Right)
            for (int r = rank + 1, f = file + 1; r < 8 && f < 8; r++, f++)
            {
                int targetIndex = r * 8 + f;
                ulong target = 1UL << targetIndex;
                moves |= target;
                if ((occupied & target) != 0) break; // Blocker hit
            }

            // 2. South-East (Down-Right)
            for (int r = rank - 1, f = file + 1; r >= 0 && f < 8; r--, f++)
            {
                int targetIndex = r * 8 + f;
                ulong target = 1UL << targetIndex;
                moves |= target;
                if ((occupied & target) != 0) break; // Blocker hit
            }

            // 3. North-West (Up-Left)
            for (int r = rank + 1, f = file - 1; r < 8 && f >= 0; r++, f--)
            {
                int targetIndex = r * 8 + f;
                ulong target = 1UL << targetIndex;
                moves |= target;
                if ((occupied & target) != 0) break; // Blocker hit
            }

            // 4. South-West (Down-Left)
            for (int r = rank - 1, f = file - 1; r >= 0 && f >= 0; r--, f--)
            {
                int targetIndex = r * 8 + f;
                ulong target = 1UL << targetIndex;
                moves |= target;
                if ((occupied & target) != 0) break; // Blocker hit
            }

            return moves;
        }

        public static ulong GetQueenMoves(int square, ulong occupied)
        {
            return GetRookMoves(square, occupied) | GetBishopMoves (square, occupied);
        }


        public static ulong GetPawnMoves(ulong pawns, ulong occupied, ulong enemyPieces, Colour colour)
        {
            ulong moves = 0UL;
            
            // File masks from class constants
            ulong notA = ~0x0101010101010101UL;
            ulong notH = ~0x8080808080808080UL;

            if (colour == Colour.White)
            {
                // Single Push
                ulong singlePush = (pawns << 8) & ~occupied;
                moves |= singlePush;

                // Double Push (Only from Rank 2: bits 8-15 -> 0x000000000000FF00UL)
                ulong rank2 = 0x000000000000FF00UL;
                ulong doublePush = ((pawns & rank2) << 8) & ~occupied; // first step empty
                doublePush = (doublePush << 8) & ~occupied;            // second step empty
                moves |= doublePush;

                // Captures (Left = +7, Right = +9)
                ulong captureLeft = (pawns << 7) & notH & enemyPieces;
                ulong captureRight = (pawns << 9) & notA & enemyPieces;
                moves |= captureLeft | captureRight;
            }
            else // Black's Turn
            {
                // Single Push (Down 1 = >> 8)
                ulong singlePush = (pawns >> 8) & ~occupied;
                moves |= singlePush;

                // Double Push (Only from Rank 7: bits 48-55 -> 0x00FF000000000000UL)
                ulong rank7 = 0x00FF000000000000UL;
                ulong doublePush = ((pawns & rank7) >> 8) & ~occupied;
                doublePush = (doublePush >> 8) & ~occupied;
                moves |= doublePush;

                // Captures (Left = -9, Right = -7)
                ulong captureLeft = (pawns >> 9) & notH & enemyPieces;
                ulong captureRight = (pawns >> 7) & notA & enemyPieces;
                moves |= captureLeft | captureRight;
            }

            return moves;
        }

        public static bool IsSquareAttacked(int square, Colour attackerColour, Bitboard board)
        {
            ulong occupied = board.CombinedOccupancy;

            // 1. Check Enemy Pawns
            // If we pretend a friendly pawn is on this square, can it strike an enemy pawn?
            Colour defenderColour = attackerColour == Colour.White ? Colour.Black : Colour.White;
            ulong enemyPawns = board.Pieces[(int)attackerColour, (int)Piece.Pawn];
            // Use the pawn attack mechanics we already wrote, but looking from our square
            ulong pawnMask = GetPawnMoves(1UL << square, occupied, 0UL, defenderColour);
            // Filter down to only pawn capture squares
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

            // 4. Check Enemy Bishops / Queens (Diagonals)
            ulong enemyDiagonalSliders = board.Pieces[(int)attackerColour, (int)Piece.Bishop] | 
                                        board.Pieces[(int)attackerColour, (int)Piece.Queen];
            if ((GetBishopMoves(square, occupied) & enemyDiagonalSliders) != 0) return true;

            // 5. Check Enemy Rooks / Queens (Straight Lines)
            ulong enemyStraightSliders = board.Pieces[(int)attackerColour, (int)Piece.Rook] | 
                                        board.Pieces[(int)attackerColour, (int)Piece.Queen];
            if ((GetRookMoves(square, occupied) & enemyStraightSliders) != 0) return true;

            return false;
        }

        public static List<Move> GeneratePseudoLegalMoves(Bitboard board)
        {
            List<Move> moveList = new List<Move>();

            // 1. Identify active player and opponent positions
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
                
                // Isolate this single pawn to calculate its specific moves
                ulong singlePawnMask = 1UL << fromSquare;
                ulong attackMask = GetPawnMoves(singlePawnMask, occupied, enemyPieces, us);

                while (attackMask != 0)
                {
                    int toSquare = BitOperations.TrailingZeroCount(attackMask);
                    
                    // Check if this move was a double pawn push to flag it properly
                    int flag = 0;
                    if (Math.Abs(toSquare - fromSquare) == 16)
                    {
                        flag = 1; // Double Pawn Push flag
                    }

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
            // KINGS
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
                king &= (king - 1);
            }

            // =========================================================================
            // BISHOPS
            // =========================================================================
            ulong bishops = board.Pieces[(int)us, (int)Piece.Bishop];
            while (bishops != 0)
            {
                int fromSquare = BitOperations.TrailingZeroCount(bishops);
                // Feed live board occupancy into your custom ray caster
                ulong attackMask = GetBishopMoves(fromSquare, occupied) & ~friendlyPieces;
                while (attackMask != 0)
                {
                    int toSquare = BitOperations.TrailingZeroCount(attackMask);
                    moveList.Add(new Move(fromSquare, toSquare));
                    attackMask &= (attackMask - 1);
                }
                bishops &= (bishops - 1);
            }

            // =========================================================================
            // ROOKS
            // =========================================================================
            ulong rooks = board.Pieces[(int)us, (int)Piece.Rook];
            while (rooks != 0)
            {
                int fromSquare = BitOperations.TrailingZeroCount(rooks);
                // Feed live board occupancy into your custom ray caster
                ulong attackMask = GetRookMoves(fromSquare, occupied) & ~friendlyPieces;
                while (attackMask != 0)
                {
                    int toSquare = BitOperations.TrailingZeroCount(attackMask);
                    moveList.Add(new Move(fromSquare, toSquare));
                    attackMask &= (attackMask - 1);
                }
                rooks &= (rooks - 1);
            }

            // =========================================================================
            // QUEENS
            // =========================================================================
            ulong queens = board.Pieces[(int)us, (int)Piece.Queen];
            while (queens != 0)
            {
                int fromSquare = BitOperations.TrailingZeroCount(queens);
                // Feed live board occupancy into your custom ray caster
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
            Colour them = board.IsWhiteToMove ? Colour.Black : Colour.White;

            foreach (Move move in pseudoMoves)
            {
                // 1. Clone the current board layout to simulate safely
                Bitboard simulatedBoard = board.Clone();

                // 2. Execute the move on the simulator
                simulatedBoard.MakeMove(move);

                // 3. Locate where our King is standing right now
                // (Note: if the moving piece WAS the king, it's now on move.ToSquare!)
                ulong kingMask = simulatedBoard.Pieces[(int)us, (int)Piece.King];
                int kingSquare = System.Numerics.BitOperations.TrailingZeroCount(kingMask);

                // 4. Run tactical radar. If enemy cannot attack our king square, the move is legal!
                if (!IsSquareAttacked(kingSquare, them, simulatedBoard))
                {
                    legalMoves.Add(move);
                }
            }

            return legalMoves;
        }
        public static string EvaluateGameEndState(Bitboard board)
        {
            // 1. Get all strict rule-legal moves for the active player
            System.Collections.Generic.List<Move> legalMoves = GenerateLegalMoves(board);

            // If they have legal options left, the game is definitely not over
            if (legalMoves.Count > 0)
            {
                // Double check if they are in "Check" just to display a warning
                Colour us = board.IsWhiteToMove ? Colour.White : Colour.Black;
                Colour them = board.IsWhiteToMove ? Colour.Black : Colour.White;
                ulong kingMask = board.Pieces[(int)us, (int)Piece.King];
                int kingSquare = System.Numerics.BitOperations.TrailingZeroCount(kingMask);

                // Debugging Start
                Console.WriteLine($"Legal moves found: {legalMoves.Count}");

                foreach (Move move in legalMoves)
                {
                    Console.WriteLine($"{move.FromSquare} -> {move.ToSquare}");
                }
                // Debugging End

                if (IsSquareAttacked(kingSquare, them, board))
                {
                    return "Check";
                }
                return "Active";
            }

            // 2. If we reach here, legal moves == 0. The game is over!
            Colour activePlayer = board.IsWhiteToMove ? Colour.White : Colour.Black;
            Colour opponent = board.IsWhiteToMove ? Colour.Black : Colour.White;
            
            ulong activeKingMask = board.Pieces[(int)activePlayer, (int)Piece.King];
            int activeKingSquare = System.Numerics.BitOperations.TrailingZeroCount(activeKingMask);

            // Check if the helpless King is currently under fire
            if (IsSquareAttacked(activeKingSquare, opponent, board))
            {
                string winner = board.IsWhiteToMove ? "Black" : "White";
                return $"Checkmate! {winner} wins the game.";
            }
            else
            {
                return "Draw by Stalemate!";
            }
        }
    }
}