using System;
using System.Collections.Generic;

namespace Sudoku.Game.Screens
{
    /// <summary>
    /// The single seam every screen change routes through.
    ///
    /// Screens are keyed by their own type, so registering one is the whole of
    /// adding it - no enum to extend, no switch to widen, and no existing screen
    /// to edit. The composition root registers each screen and wires the
    /// buttons that lead to it.
    ///
    /// It keeps a back stack rather than a single current screen, because the
    /// later pause and results screens sit on top of the game rather than
    /// replacing it.
    /// </summary>
    public sealed class Navigator
    {
        readonly Dictionary<Type, IScreen> _screens = new Dictionary<Type, IScreen>();
        readonly List<IScreen> _stack = new List<IScreen>();

        /// <summary>The screen on top of the back stack, or null before the first navigation.</summary>
        public IScreen Current => _stack.Count > 0 ? _stack[_stack.Count - 1] : null;

        public bool CanGoBack => _stack.Count > 1;

        /// <summary>
        /// Raised whenever a different screen comes to the front, whichever way
        /// it got there. Analytics (#13) subscribes once here rather than once
        /// per screen, so a screen registered later is reported without being
        /// wired up - which is the same bargain <see cref="Register"/> makes.
        /// </summary>
        public event Action<IScreen> Navigated;

        /// <summary>
        /// Adds a screen and hides it. Registration order does not matter -
        /// nothing is shown until the first <see cref="Go{TScreen}"/>.
        /// </summary>
        public void Register(IScreen screen)
        {
            if (screen == null) throw new ArgumentNullException(nameof(screen));

            var key = screen.GetType();
            if (_screens.ContainsKey(key))
                throw new InvalidOperationException($"A screen of type {key.Name} is already registered.");

            _screens.Add(key, screen);
            screen.Root.gameObject.SetActive(false);
        }

        /// <summary>Pushes a screen onto the back stack.</summary>
        public void Go<TScreen>() where TScreen : IScreen
        {
            var next = Lookup(typeof(TScreen));
            if (Current == next) return;

            Hide(Current);
            _stack.Add(next);
            Show(next);
        }

        /// <summary>
        /// Swaps the top of the back stack. Difficulty Select uses this on the
        /// way to the game, so that going back from a puzzle lands on Home
        /// rather than on the picker the player has already finished with.
        /// </summary>
        public void Replace<TScreen>() where TScreen : IScreen
        {
            var next = Lookup(typeof(TScreen));

            Hide(Current);
            if (_stack.Count > 0) _stack.RemoveAt(_stack.Count - 1);
            _stack.Add(next);
            Show(next);
        }

        /// <summary>
        /// Empties the back stack and makes the given screen the new root.
        /// Leaving a puzzle from the pause screen is not two steps backwards:
        /// the player asked for Home, and walking back into a pause screen over
        /// a puzzle they have already left would be nonsense. Nothing is torn
        /// down, so the screens left behind keep whatever they were holding.
        /// </summary>
        public void ResetTo<TScreen>() where TScreen : IScreen
        {
            var next = Lookup(typeof(TScreen));

            Hide(Current);
            _stack.Clear();
            _stack.Add(next);
            Show(next);
        }

        /// <summary>Pops the back stack. A no-op at the root, so a back button
        /// can be wired unconditionally.</summary>
        public void Back()
        {
            if (!CanGoBack) return;

            Hide(Current);
            _stack.RemoveAt(_stack.Count - 1);
            Show(Current);
        }

        IScreen Lookup(Type type)
        {
            if (_screens.TryGetValue(type, out var screen)) return screen;
            throw new InvalidOperationException(
                $"No screen of type {type.Name} is registered with the navigator.");
        }

        void Show(IScreen screen)
        {
            if (screen == null) return;
            screen.Root.gameObject.SetActive(true);
            screen.OnShow();
            Navigated?.Invoke(screen);
        }

        static void Hide(IScreen screen)
        {
            if (screen == null) return;
            screen.OnHide();
            screen.Root.gameObject.SetActive(false);
        }
    }
}
