using System;
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
        static readonly Color ButtonColor = new Color(0.93f, 0.94f, 0.96f);
        static readonly Color PrimaryColor = new Color(0.55f, 0.75f, 0.98f);

        RectTransform _root;
        Button _continue;

        public Action ContinueTapped;
        public Action NewGameTapped;

        /// <summary>
        /// Asked every time Home appears, rather than being told when the
        /// session changes - Home cannot then hold a stale answer. Ticket #6
        /// swaps the source from memory to the save file without Home noticing.
        /// </summary>
        public Func<bool> ContinueAvailable;

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

            view._continue = view.AddButton(rect, "Continue", 140, PrimaryColor, LabelColor);
            view._continue.onClick.AddListener(() => view.ContinueTapped?.Invoke());

            var newGame = view.AddButton(rect, "New Game", 0, ButtonColor, LabelColor);
            newGame.onClick.AddListener(() => view.NewGameTapped?.Invoke());

            // The daily bank and its date seeding are built; the calendar and
            // streak screen are not, so the entry point is present and off.
            var daily = view.AddButton(rect, "Daily", -140, ButtonColor, MutedColor);
            daily.interactable = false;

            return view;
        }

        public void OnShow()
        {
            _continue.interactable = ContinueAvailable != null && ContinueAvailable();
        }

        public void OnHide()
        {
        }

        Button AddButton(Transform parent, string text, float y, Color fill, Color textColor)
        {
            var button = Ui.Button(text, parent, text, 34, fill, textColor);
            Ui.Place((RectTransform)button.transform, new Vector2(0, y), new Vector2(640, 110));
            return button;
        }
    }
}
