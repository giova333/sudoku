using System;
using Sudoku.Core.Copy;
using Sudoku.Core.Difficulty;
using Sudoku.Game.Bootstrap;
using UnityEngine;
using UnityEngine.UI;

namespace Sudoku.Game.Screens
{
    /// <summary>
    /// Running out of hearts, given its own screen so that it reads as an
    /// outcome rather than as the app breaking.
    ///
    /// The tone is deliberately flat: it says what happened and what the player
    /// can do next, and it never scolds. The puzzle is still there, starting it
    /// over costs nothing, and leaving is offered as plainly as staying.
    /// </summary>
    public sealed class GameOverView : MonoBehaviour, IScreen
    {
        static readonly Color TitleColor = new Color(0.16f, 0.17f, 0.20f);
        static readonly Color LabelColor = new Color(0.16f, 0.17f, 0.20f);
        static readonly Color MutedColor = new Color(0.45f, 0.47f, 0.52f);
        static readonly Color DisabledColor = new Color(0.62f, 0.64f, 0.68f);
        static readonly Color ButtonColor = new Color(0.93f, 0.94f, 0.96f);
        static readonly Color PrimaryColor = new Color(0.55f, 0.75f, 0.98f);

        RectTransform _root;
        Text _tier;
        Text _counters;
        Button _moreHearts;
        Text _refillNote;

        public Action MoreHeartsTapped;
        public Action RestartTapped;
        public Action HomeTapped;

        /// <summary>
        /// Whether hearts can currently be had. Asked every time the screen
        /// appears rather than being told once, so this screen can never hold a
        /// stale answer - and answered by the session's
        /// <see cref="Sudoku.Core.Session.IConsumableService"/>, which is the
        /// only thing that knows.
        /// </summary>
        public Func<bool> RefillAvailable;

        public RectTransform Root => _root;

        public static GameOverView Create(Transform parent)
        {
            var rect = Ui.Rect("GameOver", parent);
            var view = rect.gameObject.AddComponent<GameOverView>();
            view._root = rect;
            Ui.Stretch(rect);

            var title = Ui.Label("Title", rect, 80, TitleColor);
            Ui.Place(title.rectTransform, new Vector2(0, 520), new Vector2(880, 130));
            title.text = CopyTable.GameOverTitle;

            var blurb = Ui.Label("Blurb", rect, 30, MutedColor);
            Ui.Place(blurb.rectTransform, new Vector2(0, 420), new Vector2(880, 60));
            blurb.text = CopyTable.GameOverBlurb;

            view._tier = Ui.Label("Tier", rect, 32, MutedColor);
            Ui.Place(view._tier.rectTransform, new Vector2(0, 330), new Vector2(800, 50));

            view._counters = Ui.Label("Counters", rect, 28, MutedColor);
            Ui.Place(view._counters.rectTransform, new Vector2(0, 270), new Vector2(800, 50));

            // The offer is present and off rather than hidden. A button that
            // appears the day monetization ships is a surprise; a button that
            // has always been there, greyed, with a reason under it, is not.
            view._moreHearts = AddButton(rect, CopyTable.GameOverMoreHearts, 120, PrimaryColor, LabelColor);
            view._moreHearts.onClick.AddListener(() => view.MoreHeartsTapped?.Invoke());

            view._refillNote = Ui.Label("RefillNote", rect, 24, DisabledColor);
            Ui.Place(view._refillNote.rectTransform, new Vector2(0, 46), new Vector2(880, 40));

            var restart = AddButton(rect, CopyTable.GameOverRestart, -60, ButtonColor, LabelColor);
            restart.onClick.AddListener(() => view.RestartTapped?.Invoke());

            var home = AddButton(rect, CopyTable.GameOverHome, -200, ButtonColor, LabelColor);
            home.onClick.AddListener(() => view.HomeTapped?.Invoke());

            return view;
        }

        /// <summary>
        /// Hands the screen the run that just ended. Story 25: the mistakes are
        /// shown at the end whether the end was a solve or a loss.
        /// </summary>
        public void Show(DifficultyTier tier, int mistakeCount)
        {
            _tier.text = CopyTable.Tier(tier);
            _counters.text = CopyTable.GameOverMistakes(mistakeCount);
        }

        public void OnShow()
        {
            var available = RefillAvailable != null && RefillAvailable();

            _moreHearts.interactable = available;
            _refillNote.text = available ? string.Empty : CopyTable.GameOverRefillUnavailable;
        }

        public void OnHide()
        {
        }

        static Button AddButton(Transform parent, string text, float y, Color fill, Color textColor)
        {
            var button = Ui.Button(text, parent, text, 34, fill, textColor);
            Ui.Place((RectTransform)button.transform, new Vector2(0, y), new Vector2(640, 110));
            return button;
        }
    }
}
