namespace Sudoku.Core.Generation
{
    /// <summary>
    /// A small xorshift generator with an explicit, hand-rolled algorithm.
    ///
    /// This deliberately does not use <c>System.Random</c>: a baked puzzle bank
    /// must be reproducible from its seed on any runtime the project touches -
    /// the editor's .NET, Mono, and IL2CPP - and the framework makes no such
    /// cross-runtime guarantee. Owning the algorithm is what makes "re-run the
    /// bake and get the same bank" true rather than hopeful.
    /// </summary>
    public sealed class DeterministicRandom
    {
        uint _state;

        public DeterministicRandom(int seed)
        {
            // Any non-zero state will do; xorshift is dead at zero.
            _state = (uint)seed;
            if (_state == 0)
                _state = 0x9E3779B9;

            // Discard a few values so nearby seeds diverge immediately.
            for (var i = 0; i < 8; i++)
                NextUInt();
        }

        public uint NextUInt()
        {
            _state ^= _state << 13;
            _state ^= _state >> 17;
            _state ^= _state << 5;
            return _state;
        }

        /// <summary>A value in [0, exclusiveUpperBound).</summary>
        public int Next(int exclusiveUpperBound)
        {
            if (exclusiveUpperBound <= 1)
                return 0;
            return (int)(NextUInt() % (uint)exclusiveUpperBound);
        }

        /// <summary>Fisher-Yates, in place.</summary>
        public void Shuffle(int[] items)
        {
            for (var i = items.Length - 1; i > 0; i--)
            {
                var j = Next(i + 1);
                var tmp = items[i];
                items[i] = items[j];
                items[j] = tmp;
            }
        }
    }
}
