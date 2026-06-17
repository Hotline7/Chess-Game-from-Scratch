using System;

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
    }
}