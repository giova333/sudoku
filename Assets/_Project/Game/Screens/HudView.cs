using System;
using Sudoku.Core.Difficulty;
using Sudoku.Core.Session;
using Sudoku.Game.Bootstrap;
using Sudoku.Game.Theme;
using UnityEngine;
using UnityEngine.UI;

namespace Sudoku.Game.Screens
{
    /// <summary>
    /// The status strip above the board: the way out, the tier being played,
    /// and the run of the session's counters.
    /// </summary>
    public sealed class HudView : MonoBehaviour
    {
        /// <summary>Top-strip geometry. The three buttons are the same size and
        /// are packed from the strip's edges, so the layout holds at any width
        /// the strip is built at.</summary>
        const float ButtonWidth = 116f;
        const float ButtonHeight = 46f;
        const float ButtonRow = 34f;
        const float Edge = 4f;
        const float Gap = 8f;

        Text _tierLabel;
        Text _status;
        ThemedGraphic _statusTheme;
        Text _banner;
        ThemedGraphic _bannerTheme;

        public Action BackTapped;
        public Action PauseTapped;
        public Action SettingsTapped;

        public static HudView Create(Transform parent, float width, float y)
        {
            var rect = Ui.Rect("Hud", parent);
            var view = rect.gameObject.AddComponent<HudView>();
            Ui.Place(rect, new Vector2(0, y), new Vector2(width, 120));

            // Three controls share the top strip, so their hit areas are laid
            // out from the two edges rather than eyeballed: Back alone on the
            // left, Pause and Settings stacked in from the right, and the tier
            // label given whatever is left between them. Nothing overlaps at
            // any width the strip is built at.
            var back = Ui.Button("Back", rect, "Back", 18, ThemeSlot.ButtonFill, ThemeSlot.ButtonText);
            Ui.Place((RectTransform)back.transform,
                new Vector2(-width / 2f + Edge + ButtonWidth / 2f, ButtonRow),
                new Vector2(ButtonWidth, ButtonHeight));
            back.onClick.AddListener(() => view.BackTapped?.Invoke());

            // Settings sits inside the game rather than only on Home, because
            // the moment a player wants the timer gone is the moment it is
            // ticking at them.
            var settings = Ui.Button("Settings", rect, "Settings", 18, ThemeSlot.ButtonFill, ThemeSlot.ButtonText);
            Ui.Place((RectTransform)settings.transform,
                new Vector2(width / 2f - Edge - ButtonWidth / 2f, ButtonRow),
                new Vector2(ButtonWidth, ButtonHeight));
            settings.onClick.AddListener(() => view.SettingsTapped?.Invoke());

            // The way to put the puzzle down without leaving it. It takes the
            // slot next to Settings rather than the one opposite Back, so a
            // mis-tap costs a screen the player can back out of instead of
            // dropping them out of the puzzle.
            var pause = Ui.Button("Pause", rect, "Pause", 18, ThemeSlot.ButtonFill, ThemeSlot.ButtonText);
            Ui.Place((RectTransform)pause.transform,
                new Vector2(width / 2f - Edge - ButtonWidth - Gap - ButtonWidth / 2f, ButtonRow),
                new Vector2(ButtonWidth, ButtonHeight));
            pause.onClick.AddListener(() => view.PauseTapped?.Invoke());

            // What is left between Back's right edge and Pause's left edge,
            // which is off-centre in the strip - the buttons are the things
            // that have to be reachable.
            var tierLeft = -width / 2f + Edge + ButtonWidth + Gap;
            var tierRight = width / 2f - Edge - ButtonWidth * 2f - Gap * 2f;

            view._tierLabel = Ui.Label("Tier", rect, 22, ThemeSlot.Muted);
            Ui.Place(view._tierLabel.rectTransform,
                new Vector2((tierLeft + tierRight) / 2f, ButtonRow),
                new Vector2(tierRight - tierLeft, ButtonHeight));

            view._status = Ui.Label("Status", rect, 20, ThemeSlot.Muted);
            view._statusTheme = view._status.GetComponent<ThemedGraphic>();
            Ui.Place(view._status.rectTransform, new Vector2(0, -14), new Vector2(width, 30));

            view._banner = Ui.Label("Banner", rect, 24, ThemeSlot.Danger);
            view._bannerTheme = view._banner.GetComponent<ThemedGraphic>();
            Ui.Place(view._banner.rectTransform, new Vector2(0, -46), new Vector2(width, 30));
            view._banner.text = "";

            return view;
        }

        public void Render(GameSession session, DifficultyTier tier, bool timerVisible)
        {
            _tierLabel.text = tier.ToString();

            var minutes = Mathf.FloorToInt(session.ElapsedSeconds / 60f);
            var seconds = Mathf.FloorToInt(session.ElapsedSeconds % 60f);
            var clock = timerVisible ? $"{minutes:00}:{seconds:00}" : "--:--";

            _status.text = $"{clock}    Hearts {session.HeartsRemaining}    " +
                           $"Mistakes {session.MistakeCount}    Left {session.EmptyCellCount}";
            _statusTheme.Use(session.HeartsRemaining <= 1 ? ThemeSlot.Danger : ThemeSlot.Muted);

            switch (session.Status)
            {
                case SessionStatus.Completed:
                    _banner.text = $"Solved in {minutes:00}:{seconds:00}";
                    _bannerTheme.Use(ThemeSlot.Celebrate);
                    break;
                case SessionStatus.Failed:
                    _banner.text = "Out of hearts";
                    _bannerTheme.Use(ThemeSlot.Danger);
                    break;
                default:
                    _banner.text = "";
                    break;
            }
        }
    }
}
