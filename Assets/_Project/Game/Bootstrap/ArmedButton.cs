using System;
using Sudoku.Game.Theme;
using UnityEngine.UI;

namespace Sudoku.Game.Bootstrap
{
    /// <summary>
    /// A button whose action cannot be taken back, so the first tap only says
    /// so and the second one means it. Between the two the button says what is
    /// about to happen and wears the warning colour, which is the whole of the
    /// confirmation - there is no dialog to dismiss and nothing to read.
    ///
    /// It wraps a button rather than being one, because arming is a fact about
    /// what the next tap means and not about how the button is drawn. Any screen
    /// with something to destroy owns one of these and asks it
    /// <see cref="Tapped"/>; the answer is whether to go ahead.
    /// </summary>
    public sealed class ArmedButton
    {
        readonly ThemedGraphic _fill;
        readonly Text _label;
        readonly string _resting;
        readonly string _confirming;

        bool _armed;

        /// <param name="button">The button to take over the label and fill of.</param>
        /// <param name="resting">What it says until it is armed.</param>
        /// <param name="confirming">What it says once it is - the second tap's
        /// consequence, stated plainly.</param>
        public ArmedButton(Button button, string resting, string confirming)
        {
            if (button == null) throw new ArgumentNullException(nameof(button));

            _fill = button.targetGraphic.GetComponent<ThemedGraphic>();
            _label = button.GetComponentInChildren<Text>();
            _resting = resting;
            _confirming = confirming;
        }

        /// <summary>
        /// Answers a tap. True means the player has now confirmed and the caller
        /// should go ahead - the button disarms itself on the way out, so a
        /// screen that comes back later is never left holding a live warning.
        /// False means this tap only armed it.
        /// </summary>
        public bool Tapped()
        {
            if (_armed)
            {
                Disarm();
                return true;
            }

            _armed = true;
            _label.text = _confirming;
            _fill.Use(ThemeSlot.WarnFill);
            return false;
        }

        /// <summary>
        /// Takes the warning back. Anything at all that the player does other
        /// than tapping again - leaving, choosing the other answer, returning to
        /// the screen later - should call this, because a warning that outlives
        /// the moment it was asked in is a trap.
        /// </summary>
        public void Disarm()
        {
            _armed = false;
            _label.text = _resting;
            _fill.Use(ThemeSlot.ButtonFill);
        }
    }
}
