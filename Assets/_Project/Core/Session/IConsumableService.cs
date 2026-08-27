using System;

namespace Sudoku.Core.Session
{
    /// <summary>
    /// Who owns the hearts and the hints. <see cref="GameSession"/> keeps no
    /// count of its own and has no way to change one except through here, so a
    /// heart can only ever be lost by asking, and only ever be given back by a
    /// source that agrees to give it.
    ///
    /// That is the entire point of the interface: rewarded-ad refills and IAP
    /// bundles arrive later as another implementation, and gameplay does not
    /// change to accommodate them. An interface the rules can route around is a
    /// decoration, which is why the balances live behind it rather than beside
    /// it.
    ///
    /// It sits in Core, with no engine reference, because what a heart costs is
    /// a rule and rules are tested without an editor.
    /// </summary>
    public interface IConsumableService
    {
        /// <summary>Raised after a balance moves, whoever moved it.</summary>
        event Action<Consumable> Changed;

        /// <summary>How many are left to spend.</summary>
        int Remaining(Consumable consumable);

        /// <summary>Whether one could be spent right now, asked without spending it.</summary>
        bool CanSpend(Consumable consumable);

        /// <summary>
        /// Spends exactly one. Returns false when the balance was already empty,
        /// which is the caller's signal that the move does not happen.
        /// </summary>
        bool Spend(Consumable consumable);

        /// <summary>
        /// Sets a balance outright. This is dealing, restarting and restoring a
        /// puzzle - not a purchase and not a reward, which is why it is separate
        /// from <see cref="Refill"/>.
        /// </summary>
        void Reset(Consumable consumable, int amount);

        /// <summary>
        /// Whether more can be had at this moment. Answering false is what makes
        /// a "get more hearts" button honestly disabled rather than absent: the
        /// offer exists, nothing is currently selling it.
        /// </summary>
        bool CanRefill(Consumable consumable);

        /// <summary>
        /// Asks for more and returns how many actually arrived - zero when no
        /// source could supply them. A count rather than a bool because an ad
        /// that half-loads and a bundle that grants five are the same call.
        /// </summary>
        int Refill(Consumable consumable, int amount);
    }
}
