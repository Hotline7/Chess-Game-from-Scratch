using System;
using System.Diagnostics;

namespace ChessConsole
{
    class Program
    {
        static void Main(string[] args)
        {
            Bitboard board = new Bitboard();
            List<Move> activeMoves = MoveGenerator.GeneratePseudoLegalMoves(board);

            Console.WriteLine($"Total Non-Pawn Moves Generated: {activeMoves.Count}");
            foreach (Move move in activeMoves)
            {
                Console.WriteLine($"- {move}");
            }
        }

        public static void PrintBitboard(ulong bitboard)
        {
            // Loop from rank 8 (index 7) down to Rank 1 (index 0)
            for (int rank = 7; rank >=0; rank--)
            {
                // Print the rank number on the left side
                Console.Write($"{rank + 1} ");

                for (int file = 0; file < 8; file++)
                {
                    // Convert 2D coordinates (rank, file) into 1D bit index (0 to 63)
                    int squareIndex = rank * 8 + file;

                    // create a mask with 1 at specific square index
                    ulong mask = 1UL << squareIndex;

                    // Use bitwise AND to check if the bit has 1 in the bitboard
                    if ((bitboard & mask) != 0)
                    {
                        Console.Write("X ");
                    }
                    else
                    {
                        Console.Write(". ");
                    }
                }
                Console.WriteLine();
            }
            // Print the file letters at the bottom
            Console.WriteLine("\n a b c d e f g h");
        }
    }
}