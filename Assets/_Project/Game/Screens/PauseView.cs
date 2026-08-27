using System;
using Sudoku.Core.Copy;
using Sudoku.Game.Bootstrap;
using Sudoku.Game.Theme;
using UnityEngine;

namespace Sudoku.Game.Screens
{
    /// <summary>
    /// The puzzle put down for a moment. It is a screen rather than a panel
    /// inside the game screen so that the navigator does the stopping: showing
    /// it hides the board, which suspends the session's clock and puts every
    /// tappable cell out of reach at once.
    ///
    /// Only Restart destroys anything, so only Restart asks twice. Leaving for
    /// Home costs the player nothing - the puzzle is still there to continue.
    /// </summary>
    public sealed class PauseView : MonoBehaviour, IScreen
    {
        RectTransform _root;

        /// <summary>Restart is the only answer here that destroys anything, so
        /// it is the only one that asks twice.</summary>
        ArmedButton _restart;

        public Action ResumeTapped;
        public Action RestartTapped;
        public Action HomeTapped;

        public RectTransform Root => _root;

        public static PauseView Create(Transform parent)
        {
            var rect = Ui.Rect("Pause", parent);
            var view = rect.gameObject.AddComponent<PauseView>();
            view._root = rect;
            Ui.Stretch(rect);

            var title = Ui.Label("Title", rect, 96, ThemeSlot.Title);
            Ui.Place(title.rectTransform, new Vector2(0, 520), new Vector2(800, 140));
            title.text = CopyTable.PauseTitle;

            var resume = Ui.ScreenButton(rect, CopyTable.PauseResume, 140,
                ThemeSlot.PrimaryFill, ThemeSlot.PrimaryText);
            resume.onClick.AddListener(() =>
            {
                view.DisarmRestart();
                view.ResumeTapped?.Invoke();
            });

            var restart = Ui.ScreenButton(rect, CopyTable.PauseRestart, 0,
                ThemeSlot.ButtonFill, ThemeSlot.ButtonText);
            view._restart = new ArmedButton(restart, CopyTable.PauseRestart,
                CopyTable.PauseRestartConfirm);
            restart.onClick.AddListener(view.OnRestartTapped);

            var home = Ui.ScreenButton(rect, CopyTable.PauseHome, -140,
                ThemeSlot.ButtonFill, ThemeSlot.ButtonText);
            home.onClick.AddListener(() =>
            {
                view.DisarmRestart();
                view.HomeTapped?.Invoke();
            });

            var note = Ui.Label("Note", rect, 24, ThemeSlot.Muted);
            Ui.Place(note.rectTransform, new Vector2(0, -280), new Vector2(800, 40));
            note.text = CopyTable.PauseNote;

            return view;
        }

        public void OnShow() => DisarmRestart();

        public void OnHide() => DisarmRestart();

        /// <summary>
        /// A restart throws away everything the player has entered, so the first
        /// tap only arms it - see <see cref="ArmedButton"/>. Anything else the
        /// player does disarms it again.
        /// </summary>
        void OnRestartTapped()
        {
            if (_restart.Tapped()) RestartTapped?.Invoke();
        }

        void DisarmRestart() => _restart.Disarm();
    }
}
