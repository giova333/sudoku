using System;
using Sudoku.Core.Copy;
using Sudoku.Game.Bootstrap;
using Sudoku.Game.Theme;
using UnityEngine;
using UnityEngine.UI;

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
        ThemedGraphic _restartFill;
        Text _restartText;
        bool _restartArmed;

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

            var resume = AddButton(rect, CopyTable.PauseResume, 140,
                ThemeSlot.PrimaryFill, ThemeSlot.PrimaryText);
            resume.onClick.AddListener(() =>
            {
                view.DisarmRestart();
                view.ResumeTapped?.Invoke();
            });

            var restart = AddButton(rect, CopyTable.PauseRestart, 0,
                ThemeSlot.ButtonFill, ThemeSlot.ButtonText);
            view._restartFill = restart.targetGraphic.GetComponent<ThemedGraphic>();
            view._restartText = restart.GetComponentInChildren<Text>();
            restart.onClick.AddListener(view.OnRestartTapped);

            var home = AddButton(rect, CopyTable.PauseHome, -140,
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
        /// A restart throws away everything the player has entered, so the
        /// first tap only arms it. Anything else they do - resuming, leaving,
        /// or coming back to this screen later - disarms it again.
        /// </summary>
        void OnRestartTapped()
        {
            if (!_restartArmed)
            {
                _restartArmed = true;
                _restartText.text = CopyTable.PauseRestartConfirm;
                _restartFill.Use(ThemeSlot.WarnFill);
                return;
            }

            DisarmRestart();
            RestartTapped?.Invoke();
        }

        void DisarmRestart()
        {
            _restartArmed = false;
            _restartText.text = CopyTable.PauseRestart;
            _restartFill.Use(ThemeSlot.ButtonFill);
        }

        static Button AddButton(Transform parent, string text, float y, ThemeSlot fill, ThemeSlot textSlot)
        {
            var button = Ui.Button(text, parent, text, 34, fill, textSlot);
            Ui.Place((RectTransform)button.transform, new Vector2(0, y), new Vector2(640, 110));
            return button;
        }
    }
}
