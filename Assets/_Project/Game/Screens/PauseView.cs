using System;
using Sudoku.Game.Bootstrap;
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
        static readonly Color TitleColor = new Color(0.16f, 0.17f, 0.20f);
        static readonly Color LabelColor = new Color(0.16f, 0.17f, 0.20f);
        static readonly Color MutedColor = new Color(0.45f, 0.47f, 0.52f);
        static readonly Color ButtonColor = new Color(0.93f, 0.94f, 0.96f);
        static readonly Color PrimaryColor = new Color(0.55f, 0.75f, 0.98f);
        static readonly Color WarnColor = new Color(0.97f, 0.80f, 0.55f);

        const string RestartLabel = "Restart";
        const string ConfirmLabel = "Start over? Tap again";

        RectTransform _root;
        Image _restartFill;
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

            var title = Ui.Label("Title", rect, 96, TitleColor);
            Ui.Place(title.rectTransform, new Vector2(0, 520), new Vector2(800, 140));
            title.text = "Paused";

            var resume = AddButton(rect, "Resume", 140, PrimaryColor, LabelColor);
            resume.onClick.AddListener(() =>
            {
                view.DisarmRestart();
                view.ResumeTapped?.Invoke();
            });

            var restart = AddButton(rect, RestartLabel, 0, ButtonColor, LabelColor);
            view._restartFill = restart.targetGraphic as Image;
            view._restartText = restart.GetComponentInChildren<Text>();
            restart.onClick.AddListener(view.OnRestartTapped);

            var home = AddButton(rect, "Home", -140, ButtonColor, LabelColor);
            home.onClick.AddListener(() =>
            {
                view.DisarmRestart();
                view.HomeTapped?.Invoke();
            });

            var note = Ui.Label("Note", rect, 24, MutedColor);
            Ui.Place(note.rectTransform, new Vector2(0, -280), new Vector2(800, 40));
            note.text = "Your puzzle is kept when you leave.";

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
                _restartText.text = ConfirmLabel;
                _restartFill.color = WarnColor;
                return;
            }

            DisarmRestart();
            RestartTapped?.Invoke();
        }

        void DisarmRestart()
        {
            _restartArmed = false;
            _restartText.text = RestartLabel;
            _restartFill.color = ButtonColor;
        }

        static Button AddButton(Transform parent, string text, float y, Color fill, Color textColor)
        {
            var button = Ui.Button(text, parent, text, 34, fill, textColor);
            Ui.Place((RectTransform)button.transform, new Vector2(0, y), new Vector2(640, 110));
            return button;
        }
    }
}
