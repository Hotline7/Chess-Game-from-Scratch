using System;
using System.Diagnostics;

namespace ChessConsole
{
    class Program
    {
        static void Main(string[] args)
        {
            // 1. Create our board state with the pieces in starting positions
            Bitboard board = new Bitboard();

            // 2. Choose a square index for our imaginary Rook (27 = d4)
            int d4Square = 27; 
            
            // 3. Pass BOTH parameters: the square index AND the dynamic occupancy mask
            ulong rookAttacksFromD4 = MoveGenerator.GetRookMoves(d4Square, board.CombinedOccupancy);

            Console.WriteLine("Rook Attack Pattern from d4 (with standard starting layout blockers):");
            PrintBitboard(rookAttacksFromD4);
            //Console.WriteLine(Convert.ToString((long)rookAttacksFromD4, 2));
            //Console.WriteLine($"0x{rookAttacksFromD4:X16}");
            //Console.WriteLine($"Occupied: 0x{board.CombinedOccupancy:X16}");

            PrintBitboard(1UL << 27);

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