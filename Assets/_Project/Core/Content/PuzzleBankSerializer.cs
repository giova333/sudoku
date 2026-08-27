using Sudoku.Core.Difficulty;
using Sudoku.Core.Model;

namespace Sudoku.Core.Content
{
    /// <summary>
    /// The bank file format. Deliberately blunt: a short header, then one byte
    /// per cell per puzzle. 2,000 puzzles cost about 162KB, and reading one is
    /// an array index rather than a parse.
    /// </summary>
    public static class PuzzleBankSerializer
    {
        /// <summary>'S','D','K','B'.</summary>
        static readonly byte[] Magic = { 0x53, 0x44, 0x4B, 0x42 };

        public const byte Version = 1;

        /// <summary>magic(4) + version(1) + tier(1) + count(4).</summary>
        public const int HeaderSize = 10;

        public static byte[] Write(DifficultyTier tier, Puzzle[] puzzles)
        {
            var bytes = new byte[HeaderSize + puzzles.Length * Board.CellCount];

            for (var i = 0; i < Magic.Length; i++) bytes[i] = Magic[i];
            bytes[4] = Version;
            bytes[5] = (byte)tier;
            WriteInt32(bytes, 6, puzzles.Length);

            var at = HeaderSize;
            foreach (var puzzle in puzzles)
            {
                for (var cell = 0; cell < Board.CellCount; cell++)
                {
                    var clue = puzzle.ClueAt(cell) & 0xF;
                    var solution = puzzle.SolutionAt(cell) & 0xF;
                    bytes[at + cell] = (byte)((clue << 4) | solution);
                }
                at += Board.CellCount;
            }

            return bytes;
        }

        public static PuzzleBank Read(byte[] bytes)
        {
            if (bytes == null || bytes.Length < HeaderSize)
                throw new PuzzleBankFormatException("Too short to be a puzzle bank.");

            for (var i = 0; i < Magic.Length; i++)
                if (bytes[i] != Magic[i])
                    throw new PuzzleBankFormatException("Not a puzzle bank.");

            if (bytes[4] != Version)
                throw new PuzzleBankFormatException(
                    $"Bank is version {bytes[4]}; this build reads version {Version}.");

            var tier = (DifficultyTier)bytes[5];
            var count = ReadInt32(bytes, 6);

            var expected = HeaderSize + count * Board.CellCount;
            if (bytes.Length != expected)
                throw new PuzzleBankFormatException(
                    $"Bank claims {count} puzzles ({expected} bytes) but is {bytes.Length} bytes.");

            return new PuzzleBank(bytes, HeaderSize, tier, count);
        }

        static void WriteInt32(byte[] bytes, int at, int value)
        {
            bytes[at] = (byte)value;
            bytes[at + 1] = (byte)(value >> 8);
            bytes[at + 2] = (byte)(value >> 16);
            bytes[at + 3] = (byte)(value >> 24);
        }

        static int ReadInt32(byte[] bytes, int at) =>
            bytes[at] | (bytes[at + 1] << 8) | (bytes[at + 2] << 16) | (bytes[at + 3] << 24);
    }
}
