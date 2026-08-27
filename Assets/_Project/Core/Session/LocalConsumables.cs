using System;

namespace Sudoku.Core.Session
{
    /// <summary>
    /// The whole consumable economy of this milestone: two numbers that start at
    /// whatever the rules say and only ever go down.
    ///
    /// It refuses every refill, and says so through
    /// <see cref="CanRefill"/> rather than by throwing or by pretending to
    /// succeed - a screen that offers more hearts can then present the offer and
    /// disable it, which is the honest shape and the one the ad-backed
    /// implementation will slot into without the screen changing.
    /// </summary>
    public sealed class LocalConsumables : IConsumableService
    {
        readonly int[] _balances = new int[2];

        public event Action<Consumable> Changed;

        public int Remaining(Consumable consumable) => _balances[(int)consumable];

        public bool CanSpend(Consumable consumable) => Remaining(consumable) > 0;

        public bool Spend(Consumable consumable)
        {
            if (!CanSpend(consumable))
                return false;

            _balances[(int)consumable]--;
            Announce(consumable);
            return true;
        }

        public void Reset(Consumable consumable, int amount)
        {
            var floored = amount < 0 ? 0 : amount;
            if (_balances[(int)consumable] == floored)
                return;

            _balances[(int)consumable] = floored;
            Announce(consumable);
        }

        /// <summary>
        /// Nothing sells hearts or hints yet. The rewarded-ad and IAP sources
        /// arrive in the monetization milestone as a different implementation of
        /// <see cref="IConsumableService"/>.
        /// </summary>
        public bool CanRefill(Consumable consumable) => false;

        public int Refill(Consumable consumable, int amount) => 0;

        void Announce(Consumable consumable)
        {
            var handler = Changed;
            if (handler == null) return;

            handler(consumable);
        }
    }
}
