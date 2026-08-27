using NUnit.Framework;
using Sudoku.Core.Difficulty;
using Sudoku.Core.Session;

namespace Sudoku.Core.Tests.Session
{
    /// <summary>
    /// What the results card is handed. "Perfect" lives here rather than in the
    /// card because the copy table and any later achievement have to mean the
    /// same thing by it.
    /// </summary>
    [TestFixture]
    public class PuzzleResultTests
    {
        static PuzzleResult Result(int mistakes, int hints) =>
            new PuzzleResult(DifficultyTier.Medium, 300f, mistakes, hints, 300f, true);

        [Test]
        public void A_solve_with_no_mistakes_and_no_hints_is_perfect()
        {
            Assert.That(Result(0, 0).IsPerfect, Is.True);
        }

        [Test]
        public void A_solve_with_a_mistake_is_not_perfect()
        {
            Assert.That(Result(1, 0).IsPerfect, Is.False);
        }

        [Test]
        public void A_solve_that_took_a_hint_is_not_perfect()
        {
            Assert.That(Result(0, 1).IsPerfect, Is.False);
        }
    }
}
