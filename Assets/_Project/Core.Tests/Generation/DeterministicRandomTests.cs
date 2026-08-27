using NUnit.Framework;
using Sudoku.Core.Generation;

namespace Sudoku.Core.Tests.Generation
{
    [TestFixture]
    public class DeterministicRandomTests
    {
        [Test]
        public void The_same_seed_produces_the_same_sequence()
        {
            var a = new DeterministicRandom(12345);
            var b = new DeterministicRandom(12345);

            for (var i = 0; i < 100; i++)
                Assert.That(a.Next(1000), Is.EqualTo(b.Next(1000)));
        }

        [Test]
        public void Different_seeds_produce_different_sequences()
        {
            var a = new DeterministicRandom(1);
            var b = new DeterministicRandom(2);

            var same = 0;
            for (var i = 0; i < 100; i++)
                if (a.Next(1000) == b.Next(1000))
                    same++;

            Assert.That(same, Is.LessThan(20), "sequences should not track each other");
        }

        [Test]
        public void Values_stay_inside_the_requested_range()
        {
            var random = new DeterministicRandom(7);

            for (var i = 0; i < 1000; i++)
            {
                var value = random.Next(10);
                Assert.That(value, Is.InRange(0, 9));
            }
        }

        [Test]
        public void Shuffling_permutes_without_losing_or_duplicating_anything()
        {
            var items = new int[50];
            for (var i = 0; i < items.Length; i++) items[i] = i;

            new DeterministicRandom(99).Shuffle(items);

            System.Array.Sort(items);
            for (var i = 0; i < items.Length; i++)
                Assert.That(items[i], Is.EqualTo(i));
        }

        [Test]
        public void Shuffling_actually_reorders()
        {
            var items = new int[50];
            for (var i = 0; i < items.Length; i++) items[i] = i;

            new DeterministicRandom(99).Shuffle(items);

            var inPlace = 0;
            for (var i = 0; i < items.Length; i++)
                if (items[i] == i) inPlace++;

            Assert.That(inPlace, Is.LessThan(items.Length / 2));
        }
    }
}
