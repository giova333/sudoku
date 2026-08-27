using NUnit.Framework;
using Sudoku.Core.Model;
using Sudoku.Core.Solving;

namespace Sudoku.Core.Tests.Solving
{
    [TestFixture]
    public class TechniqueSolverTests
    {
        static int[] Grid(string text) => GridParser.Parse(text);

        /// <summary>
        /// Row 0 holds 1-8, so its last cell can only be 9. That cell has
        /// exactly one candidate, which is a naked single.
        /// </summary>
        const string NakedSingleGrid =
            "123456780" +
            "000000000" +
            "000000000" +
            "000000000" +
            "000000000" +
            "000000000" +
            "000000000" +
            "000000000" +
            "000000000";

        [Test]
        public void A_cell_with_one_remaining_candidate_is_a_naked_single()
        {
            var step = TechniqueSolver.NextStep(Grid(NakedSingleGrid), ConstraintSet.Classic);

            Assert.That(step, Is.Not.Null);
            Assert.That(step.Technique, Is.EqualTo(Technique.NakedSingle));
            Assert.That(step.CellIndex, Is.EqualTo(8));
            Assert.That(step.Digit, Is.EqualTo(9));
        }

        [Test]
        public void A_step_names_the_cells_that_justify_it()
        {
            var step = TechniqueSolver.NextStep(Grid(NakedSingleGrid), ConstraintSet.Classic);

            // The eight filled cells of row 0 are what rule out every other digit.
            Assert.That(step.ReasonCells, Is.Not.Empty);
            CollectionAssert.IsSubsetOf(new[] { 0, 1, 2, 3, 4, 5, 6, 7 }, step.ReasonCells);
        }

        [Test]
        public void A_solved_grid_offers_no_further_step()
        {
            var solved = Grid(
                "534678912" + "672195348" + "198342567" +
                "859761423" + "426853791" + "713924856" +
                "961537284" + "287419635" + "345286179");

            Assert.That(TechniqueSolver.NextStep(solved, ConstraintSet.Classic), Is.Null);
        }

        /// <summary>
        /// Only 5s are placed. In box 0 the 5 is barred from row 0 (by r0c4),
        /// from row 1 (by r1c5), from column 1 (by r5c1) and from column 2
        /// (by r6c2) - leaving r2c0 as the only cell in box 0 that can hold a 5.
        /// That cell still has nine candidates of its own, so this is a hidden
        /// single, not a naked one.
        /// </summary>
        const string HiddenSingleGrid =
            "000050000" +
            "000005000" +
            "000000000" +
            "000000000" +
            "000000000" +
            "050000000" +
            "005000000" +
            "000000000" +
            "000000000";

        [Test]
        public void A_digit_with_only_one_home_in_a_group_is_a_hidden_single()
        {
            var grid = Grid(HiddenSingleGrid);

            var step = TechniqueSolver.NextStep(grid, ConstraintSet.Classic);

            Assert.That(step, Is.Not.Null);
            Assert.That(step.Technique, Is.EqualTo(Technique.HiddenSingle));
            Assert.That(step.CellIndex, Is.EqualTo(18), "r2c0");
            Assert.That(step.Digit, Is.EqualTo(5));
        }

        [Test]
        public void The_hidden_single_cell_is_not_itself_a_naked_single()
        {
            var candidates = TechniqueSolver.BuildCandidates(Grid(HiddenSingleGrid), ConstraintSet.Classic);

            // Nothing rules any digit out of r2c0 directly - that is what makes
            // the deduction "hidden".
            Assert.That(TechniqueSolver.PopCountOf(candidates[18]), Is.EqualTo(9));
        }

        [Test]
        public void Naked_singles_are_preferred_over_hidden_ones()
        {
            // This grid offers both; the easier technique must win, because the
            // hardest technique a puzzle *requires* is what sets its difficulty.
            var step = TechniqueSolver.NextStep(Grid(NakedSingleGrid), ConstraintSet.Classic);

            Assert.That(step.Technique, Is.EqualTo(Technique.NakedSingle));
        }


        [Test]
        public void The_classic_puzzle_falls_to_singles_alone()
        {
            var report = TechniqueSolver.Solve(
                Grid(Fixtures.KnownPuzzles.ClassicClues), ConstraintSet.Classic);

            Assert.That(report.Solved, Is.True);
            Assert.That(report.HardestTechnique, Is.LessThanOrEqualTo(Technique.HiddenSingle));
            Assert.That(report.TotalSteps, Is.GreaterThan(0));
        }

        [Test]
        public void Solving_by_technique_reaches_the_published_solution()
        {
            var report = TechniqueSolver.Solve(
                Grid(Fixtures.KnownPuzzles.ClassicClues), ConstraintSet.Classic);

            Assert.That(GridParser.ToText(report.Grid),
                Is.EqualTo(Fixtures.KnownPuzzles.ClassicSolution));
        }

        [Test]
        public void A_genuinely_hard_puzzle_does_not_fall_to_singles_alone()
        {
            var report = TechniqueSolver.Solve(
                Grid(Fixtures.KnownPuzzles.AiEscargot), ConstraintSet.Classic);

            var singlesWereEnough = report.Solved && report.HardestTechnique <= Technique.HiddenSingle;
            Assert.That(singlesWereEnough, Is.False,
                "AI Escargot is published as one of the hardest puzzles ever built");
        }

        [Test]
        public void A_report_counts_how_often_each_technique_fired()
        {
            var report = TechniqueSolver.Solve(
                Grid(Fixtures.KnownPuzzles.ClassicClues), ConstraintSet.Classic);

            var counted = 0;
            foreach (Technique t in System.Enum.GetValues(typeof(Technique)))
                counted += report.CountOf(t);

            Assert.That(counted, Is.EqualTo(report.TotalSteps));
        }


        /// <summary>
        /// Box 0 has 1,2,3 across row 1 and 4,5,6 across row 2, so the digits
        /// 7, 8 and 9 must all live in row 0 of that box. They are therefore
        /// barred from the rest of row 0. Nothing here is a naked or hidden
        /// single: r0c0, r0c1 and r0c2 each hold exactly the candidates
        /// {7,8,9}, and every other digit has several homes in every group.
        /// </summary>
        const string LockedCandidatesGrid =
            "000000000" +
            "123000000" +
            "456000000" +
            "000000000" +
            "000000000" +
            "000000000" +
            "000000000" +
            "000000000" +
            "000000000";

        [Test]
        public void Digits_confined_to_one_row_of_a_box_are_barred_from_the_rest_of_that_row()
        {
            var step = TechniqueSolver.NextStep(Grid(LockedCandidatesGrid), ConstraintSet.Classic);

            Assert.That(step, Is.Not.Null);
            Assert.That(step.Technique, Is.EqualTo(Technique.LockedCandidates));
            Assert.That(step.Digit, Is.AnyOf(7, 8, 9));
            Assert.That(step.Eliminations, Is.Not.Null.And.Not.Empty);

            foreach (var e in step.Eliminations)
            {
                Assert.That(e.Cell / 9, Is.EqualTo(0), "eliminations stay in row 0");
                Assert.That(e.Cell % 9, Is.GreaterThan(2), "and outside box 0");
                Assert.That(e.Digit, Is.EqualTo(step.Digit));
            }
        }

        [Test]
        public void The_locked_candidates_grid_offers_no_easier_step()
        {
            var candidates = TechniqueSolver.BuildCandidates(Grid(LockedCandidatesGrid), ConstraintSet.Classic);

            // Each of r0c0..r0c2 holds exactly {7,8,9} - three candidates, so
            // no naked single is available to fire first.
            foreach (var cell in new[] { 0, 1, 2 })
                Assert.That(TechniqueSolver.PopCountOf(candidates[cell]), Is.EqualTo(3));
        }

        [Test]
        public void Every_technique_the_solver_claims_to_know_actually_fires()
        {
            // Guards against a technique being declared but never reachable -
            // dead code that silently mis-grades every puzzle above its tier.
            var fired = new System.Collections.Generic.HashSet<Technique>();

            for (var seed = 1; seed <= 120 && fired.Count < 7; seed++)
            {
                var puzzle = Sudoku.Core.Generation.PuzzleGenerator.Generate(seed, ConstraintSet.Classic);
                var clues = new int[Board.CellCount];
                for (var i = 0; i < Board.CellCount; i++) clues[i] = puzzle.ClueAt(i);

                var report = TechniqueSolver.Solve(clues, ConstraintSet.Classic);
                foreach (Technique t in System.Enum.GetValues(typeof(Technique)))
                    if (report.CountOf(t) > 0)
                        fired.Add(t);
            }

            foreach (Technique t in System.Enum.GetValues(typeof(Technique)))
                Assert.That(fired, Contains.Item(t), $"{t} never fired on any generated puzzle");
        }

    }
}
