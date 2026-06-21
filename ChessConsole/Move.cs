namespace ChessConsole
{
    public readonly struct Move
    {
        // The single 16-bit container holding all our packed data
        public readonly ushort Value;

        public Move(ushort value)
        {
            Value = value;
        }

        // Constructor that packs the fields using bitwise shifting
        public Move(int fromSquare, int toSquare, int flags = 0)
        {
            // Pack 'from' into bits 0-5
            // Pack 'to' into bits 6-11 (shift left by 6)
            // Pack 'flags' into bits 12-15 (shift left by 12)
            Value = (ushort)(fromSquare | (toSquare << 6) | (flags << 12));
        }

        // Unpacking properties using bitwise masks
        public int FromSquare => Value & 0x3F;          // Extract bits 0-5 (0x3F = 00111111)
        public int ToSquare => (Value >> 6) & 0x3F;     // Shift right 6, extract bits 0-5
        public int Flags => (Value >> 12) & 0x0F;       // Shift right 12, extract bits 0-3 (0x0F = 00001111)

        public bool IsCapture => (Flags & 0x4) != 0;
        public bool IsPromotion => (Flags & 0x8) != 0;
        public bool IsKingSideCastle => Flags == 2;
        public bool IsQueenSideCastle => Flags == 3;
        public bool IsCastle => IsKingSideCastle || IsQueenSideCastle;

        public override string ToString()
        {
            // Helper to print out human-readable algebraic squares later (e.g., "e2e4")
            string[] squareNames = {
                "a1","b1","c1","d1","e1","f1","g1","h1",
                "a2","b2","c2","d2","e2","f2","g2","h2",
                "a3","b3","c3","d3","e3","f3","g3","h3",
                "a4","b4","c4","d4","e4","f4","g4","h4",
                "a5","b5","c5","d5","e5","f5","g5","h5",
                "a6","b6","c6","d6","e6","f6","g6","h6",
                "a7","b7","c7","d7","e7","f7","g7","h7",
                "a8","b8","c8","d8","e8","f8","g8","h8"
            };
            return $"{squareNames[FromSquare]}{squareNames[ToSquare]}";
        }
    }
}