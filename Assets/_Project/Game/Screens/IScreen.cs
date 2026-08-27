using UnityEngine;

namespace Sudoku.Game.Screens
{
    /// <summary>
    /// One full-screen destination the <see cref="Navigator"/> can show.
    ///
    /// The contract is deliberately this small: a screen owns a rect and is
    /// told when it comes and goes, and nothing else. That is what makes a new
    /// screen a registration with the navigator rather than an edit to the
    /// screens already there.
    /// </summary>
    public interface IScreen
    {
        /// <summary>
        /// The rect the navigator activates and deactivates. Screens are built
        /// once at startup and toggled, never destroyed and rebuilt - an
        /// in-progress puzzle has to survive a trip to Home.
        /// </summary>
        RectTransform Root { get; }

        /// <summary>Called after the root is activated, so the screen can refresh
        /// anything that may have changed while it was away.</summary>
        void OnShow();

        /// <summary>Called before the root is deactivated. State is suspended
        /// here, never discarded.</summary>
        void OnHide();
    }
}
