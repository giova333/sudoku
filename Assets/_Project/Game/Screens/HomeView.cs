using System;
using Sudoku.Core.Persistence;
using Sudoku.Game.Bootstrap;
using UnityEngine;
using UnityEngine.UI;

namespace Sudoku.Game.Screens
{
    /// <summary>
    /// The screen the app launches into. Landing here rather than in a puzzle
    /// is what lets the player choose a difficulty instead of being handed
    /// whatever the build defaulted to.
    /// </summary>
    public sealed class HomeView : MonoBehaviour, IScreen
    {
        static readonly Color TitleColor = new Color(0.16f, 0.17f, 0.20f);
        static readonly Color LabelColor = new Color(0.16f, 0.17f, 0.20f);
        static readonly Color MutedColor = new Color(0.62f, 0.64f, 0.68f);
        static readonly Color DetailColor = new Color(0.45f, 0.47f, 0.52f);
        static readonly Color ButtonColor = new Color(0.93f, 0.94f, 0.96f);
        static readonly Color PrimaryColor = new Color(0.55f, 0.75f, 0.98f);

        /// <summary>The column of buttons: where the first one sits and how far
        /// apart they are, so a hidden Continue closes its own gap.</summary>
        const float TopRow = 140f;
        const float RowStep = 140f;

        RectTransform _root;
        Button _continue;
        Text _continueDetail;
        Button _newGame;
        Button _daily;
        Button _settings;

        public Action ContinueTapped;
        public Action NewGameTapped;
        public Action SettingsTapped;

        /// <summary>
        /// The puzzle Continue would resume, or null when the player has
        /// nothing waiting. Asked every time Home appears, rather than being
        /// told when the session changes - Home cannot then hold a stale
        /// answer, and the answer comes from the save file, so a puzzle
        /// survives a cold start with no session in memory to speak for it.
        /// </summary>
        public Func<SaveSlot> ContinueTarget;

        public RectTransform Root => _root;

        public static HomeView Create(Transform parent)
        {
            var rect = Ui.Rect("Home", parent);
            var view = rect.gameObject.AddComponent<HomeView>();
            view._root = rect;
            Ui.Stretch(rect);

            var title = Ui.Label("Title", rect, 96, TitleColor);
            Ui.Place(title.rectTransform, new Vector2(0, 520), new Vector2(800, 140));
            title.text = "Sudoku";

            view._continue = view.AddButton(rect, "Continue", PrimaryColor, LabelColor);
            view._continue.onClick.AddListener(() => view.ContinueTapped?.Invoke());

            // Which puzzle Continue means, not just that there is one: a
            // difficulty and a clock are what tell the player whether this is
            // the ten-second Easy or the stalled Expert.
            view._continueDetail = Ui.Label("ContinueDetail", rect, 24, DetailColor);

            view._newGame = view.AddButton(rect, "New Game", ButtonColor, LabelColor);
            view._newGame.onClick.AddListener(() => view.NewGameTapped?.Invoke());

            // The daily bank and its date seeding are built; the calendar and
            // streak screen are not, so the entry point is present and off.
            view._daily = view.AddButton(rect, "Daily", ButtonColor, MutedColor);
            view._daily.interactable = false;

            view._settings = view.AddButton(rect, "Settings", ButtonColor, LabelColor);
            view._settings.onClick.AddListener(() => view.SettingsTapped?.Invoke());

            return view;
        }

        public void OnShow()
        {
            var slot = ContinueTarget != null ? ContinueTarget() : null;

            // Nothing waiting means no Continue at all, rather than a greyed
            // one: a button that is never pressable is furniture.
            _continue.gameObject.SetActive(slot != null);
            _continueDetail.gameObject.SetActive(slot != null);

            if (slot != null)
                _continueDetail.text = $"{slot.Tier}  -  {Ui.Clock(slot.ElapsedSeconds)}";

            Layout();
        }

        public void OnHide()
        {
        }

        /// <summary>
        /// Stacks whichever buttons are showing, so the column has no hole in
        /// it on a first launch.
        /// </summary>
        void Layout()
        {
            var y = TopRow;

            if (_continue.gameObject.activeSelf)
            {
                Place(_continue, y);
                Ui.Place(_continueDetail.rectTransform, new Vector2(0, y - 74f), new Vector2(640, 34));
                y -= RowStep;
            }

            Place(_newGame, y);
            Place(_daily, y - RowStep);
            Place(_settings, y - RowStep * 2f);
        }

        Button AddButton(Transform parent, string text, Color fill, Color textColor)
        {
            var button = Ui.Button(text, parent, text, 34, fill, textColor);
            Place(button, 0);
            return button;
        }

        static void Place(Button button, float y) =>
            Ui.Place((RectTransform)button.transform, new Vector2(0, y), new Vector2(640, 110));
    }
}
