using Sudoku.Core.Session;

namespace Sudoku.Game.Board
{
    /// <summary>
    /// Where the nine 3x3 boxes are, and whether one of them stands finished.
    ///
    /// The session does not announce a finished box - it is not a rule, it
    /// changes nothing about the game, and Core is right not to carry it. But it
    /// is one of the moments the player feels most, so both the audio (#11) and
    /// the motion (#10) react to it, and two copies of "finished" that could
    /// quietly drift apart would be two copies too many.
    /// </summary>
    public static class BoardBoxes
    {
        /// <summary>How many boxes a classic grid has, and how many cells are in
        /// each of them.</summary>
        public const int Count = 9;
        public const int Size = 9;

        /// <summary>One of a box's nine cells, by its index within the box in
        /// reading order.</summary>
        public static int Cell(int box, int slot)
        {
            var row = box / 3 * 3 + slot / 3;
            var column = box % 3 * 3 + slot % 3;
            return row * Core.Model.Board.Size + column;
        }

        /// <summary>
        /// Full and right. A box filled with a wrong digit in it has not been
        /// solved, and saying so would be worse than saying nothing.
        /// </summary>
        public static bool IsComplete(GameSession session, int box)
        {
            if (session == null) return false;

            for (var slot = 0; slot < Size; slot++)
            {
                var index = Cell(box, slot);
                if (session.ValueAt(index) == Core.Model.Board.Empty) return false;
                if (session.IsMistakeAt(index)) return false;
            }

            return true;
        }
    }
}
