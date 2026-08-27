using UnityEngine.UI;

namespace Sudoku.Game.Bootstrap
{
    /// <summary>
    /// A button whose look is a <see cref="ChunkyBox"/>.
    ///
    /// It exists so that pressing is driven by the selectable's own state
    /// machine rather than by a second set of pointer handlers keeping step
    /// with it. Unity already knows when a control is held, when the finger has
    /// slid off it, and when it is not available at all; a subclass gets told,
    /// and passes it on to the box.
    ///
    /// Its transition is <see cref="Selectable.Transition.None"/> on purpose.
    /// The stock colour tint multiplies one graphic - the face - and would
    /// leave the stroke and the shadow at full strength round a dimmed button;
    /// the box fades all three together instead.
    /// </summary>
    public sealed class ChunkyButton : Button
    {
        ChunkyBox _box;

        /// <summary>The box this button wears. Set once, when the button is
        /// built.</summary>
        public ChunkyBox Box
        {
            get => _box;
            set
            {
                _box = value;
                if (_box != null) DoStateTransition(currentSelectionState, true);
            }
        }

        protected override void DoStateTransition(SelectionState state, bool instant)
        {
            base.DoStateTransition(state, instant);

            // Assigned after the component is added, so the first transitions -
            // the ones Selectable runs from its own OnEnable - arrive before
            // there is a box to move.
            if (_box == null) return;

            _box.SetAvailable(state != SelectionState.Disabled);
            _box.SetPressed(state == SelectionState.Pressed);
        }
    }
}
