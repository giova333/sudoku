namespace Sudoku.Core.Model
{
    /// <summary>Two constraint groups and the cells they share.</summary>
    public sealed class GroupIntersection
    {
        public GroupIntersection(int[] source, int[] target, int[] shared)
        {
            Source = source;
            Target = target;
            Shared = shared;
        }

        /// <summary>The group a digit's candidates are confined within.</summary>
        public int[] Source { get; }

        /// <summary>The group the digit can then be eliminated from.</summary>
        public int[] Target { get; }

        public int[] Shared { get; }
    }
}
