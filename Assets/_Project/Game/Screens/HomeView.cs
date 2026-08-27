using System;
using Sudoku.Core.Copy;
using Sudoku.Core.Persistence;
using Sudoku.Game.Bootstrap;
using Sudoku.Game.Theme;
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

            var title = Ui.Label("Title", rect, 96, ThemeSlot.Title);
            Ui.Place(title.rectTransform, new Vector2(0, 520), new Vector2(800, 140));
            title.text = CopyTable.AppTitle;

            // The one line of voice on this screen. Home is where the game
            // is allowed a personality; the puzzle is not.
            var tagline = Ui.Label("Tagline", rect, 26, ThemeSlot.Muted);
            Ui.Place(tagline.rectTransform, new Vector2(0, 430), new Vector2(880, 50));
            tagline.text = CopyTable.HomeTagline;

            view._continue = view.AddButton(rect, CopyTable.HomeContinue, ThemeSlot.PrimaryFill, ThemeSlot.PrimaryText);
            view._continue.onClick.AddListener(() => view.ContinueTapped?.Invoke());

            // Which puzzle Continue means, not just that there is one: a
            // difficulty and a clock are what tell the player whether this is
            // the ten-second Easy or the stalled Expert.
            view._continueDetail = Ui.Label("ContinueDetail", rect, 24, ThemeSlot.Muted);

            view._newGame = view.AddButton(rect, CopyTable.HomeNewGame, ThemeSlot.ButtonFill, ThemeSlot.ButtonText);
            view._newGame.onClick.AddListener(() => view.NewGameTapped?.Invoke());

            // The daily bank and its date seeding are built; the calendar and
            // streak screen are not, so the entry point is present and off.
            view._daily = view.AddButton(rect, CopyTable.HomeDaily, ThemeSlot.ButtonFill, ThemeSlot.Disabled);
            view._daily.interactable = false;

            view._settings = view.AddButton(rect, CopyTable.HomeSettings, ThemeSlot.ButtonFill, ThemeSlot.ButtonText);
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
                _continueDetail.text =
                    CopyTable.HomeContinueDetail(CopyTable.Tier(slot.Tier), Ui.Clock(slot.ElapsedSeconds));

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

        Button AddButton(Transform parent, string text, ThemeSlot fill, ThemeSlot textSlot)
        {
            var button = Ui.Button(text, parent, text, 34, fill, textSlot);
            Place(button, 0);
            return button;
        }

        static void Place(Button button, float y) =>
            Ui.Place((RectTransform)button.transform, new Vector2(0, y), new Vector2(640, 110));
    }
}
