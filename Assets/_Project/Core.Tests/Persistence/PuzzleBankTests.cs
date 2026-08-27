using NUnit.Framework;
using Sudoku.Core.Content;
using Sudoku.Core.Difficulty;
using Sudoku.Core.Generation;
using Sudoku.Core.Model;

namespace Sudoku.Core.Tests.Persistence
{
    [TestFixture]
    public class PuzzleBankTests
    {
        static Puzzle[] SamplePuzzles(int count)
        {
            var puzzles = new Puzzle[count];
            for (var i = 0; i < count; i++)
                puzzles[i] = PuzzleGenerator.Generate(100 + i, ConstraintSet.Classic);
            return puzzles;
        }

        [Test]
        public void A_bank_round_trips_every_puzzle_unchanged()
        {
            var original = SamplePuzzles(8);

            var bytes = PuzzleBankSerializer.Write(DifficultyTier.Hard, original);
            var bank = PuzzleBankSerializer.Read(bytes);

            Assert.That(bank.Count, Is.EqualTo(original.Length));
            Assert.That(bank.Tier, Is.EqualTo(DifficultyTier.Hard));

            for (var i = 0; i < original.Length; i++)
            {
                var restored = bank.PuzzleAt(i);
                for (var cell = 0; cell < Board.CellCount; cell++)
                {
                    Assert.That(restored.ClueAt(cell), Is.EqualTo(original[i].ClueAt(cell)), $"puzzle {i} clue {cell}");
                    Assert.That(restored.SolutionAt(cell), Is.EqualTo(original[i].SolutionAt(cell)), $"puzzle {i} solution {cell}");
                }
            }
        }

        [Test]
        public void A_bank_costs_81_bytes_per_puzzle_plus_a_small_header()
        {
            var bytes = PuzzleBankSerializer.Write(DifficultyTier.Easy, SamplePuzzles(10));

            Assert.That(bytes.Length, Is.EqualTo(PuzzleBankSerializer.HeaderSize + 10 * Board.CellCount));
        }

        [Test]
        public void A_puzzle_can_be_read_by_index_without_touching_the_others()
        {
            var original = SamplePuzzles(20);
            var bank = PuzzleBankSerializer.Read(PuzzleBankSerializer.Write(DifficultyTier.Expert, original));

            var seventh = bank.PuzzleAt(7);

            for (var cell = 0; cell < Board.CellCount; cell++)
                Assert.That(seventh.ClueAt(cell), Is.EqualTo(original[7].ClueAt(cell)));
        }

        [Test]
        public void A_bank_from_a_future_version_is_rejected_rather_than_misread()
        {
            var bytes = PuzzleBankSerializer.Write(DifficultyTier.Easy, SamplePuzzles(2));
            bytes[4] = 99; // bump the version byte

            Assert.Throws<PuzzleBankFormatException>(() => PuzzleBankSerializer.Read(bytes));
        }

        [Test]
        public void Something_that_is_not_a_bank_is_rejected()
        {
            Assert.Throws<PuzzleBankFormatException>(
                () => PuzzleBankSerializer.Read(new byte[] { 1, 2, 3, 4, 5, 6, 7, 8 }));
        }

        [Test]
        public void Asking_for_a_puzzle_outside_the_bank_is_an_error_not_garbage()
        {
            var bank = PuzzleBankSerializer.Read(PuzzleBankSerializer.Write(DifficultyTier.Easy, SamplePuzzles(3)));

            Assert.Throws<System.ArgumentOutOfRangeException>(() => bank.PuzzleAt(3));
            Assert.Throws<System.ArgumentOutOfRangeException>(() => bank.PuzzleAt(-1));
        }
    }
}
