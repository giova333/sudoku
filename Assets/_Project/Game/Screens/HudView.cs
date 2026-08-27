using System;
using Sudoku.Core.Copy;
using Sudoku.Core.Difficulty;
using Sudoku.Core.Session;
using Sudoku.Game.Bootstrap;
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
        static readonly Color Label = new Color(0.16f, 0.17f, 0.20f);
        static readonly Color Muted = new Color(0.45f, 0.47f, 0.52f);
        static readonly Color Danger = new Color(0.83f, 0.21f, 0.24f);
        static readonly Color ButtonColor = new Color(0.93f, 0.94f, 0.96f);

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
        Text _banner;

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
            var back = Ui.Button("Back", rect, CopyTable.HudBack, 18, ButtonColor, Label);
            Ui.Place((RectTransform)back.transform,
                new Vector2(-width / 2f + Edge + ButtonWidth / 2f, ButtonRow),
                new Vector2(ButtonWidth, ButtonHeight));
            back.onClick.AddListener(() => view.BackTapped?.Invoke());

            // Settings sits inside the game rather than only on Home, because
            // the moment a player wants the timer gone is the moment it is
            // ticking at them.
            var settings = Ui.Button("Settings", rect, CopyTable.HudSettings, 18, ButtonColor, Label);
            Ui.Place((RectTransform)settings.transform,
                new Vector2(width / 2f - Edge - ButtonWidth / 2f, ButtonRow),
                new Vector2(ButtonWidth, ButtonHeight));
            settings.onClick.AddListener(() => view.SettingsTapped?.Invoke());

            // The way to put the puzzle down without leaving it. It takes the
            // slot next to Settings rather than the one opposite Back, so a
            // mis-tap costs a screen the player can back out of instead of
            // dropping them out of the puzzle.
            var pause = Ui.Button("Pause", rect, CopyTable.HudPause, 18, ButtonColor, Label);
            Ui.Place((RectTransform)pause.transform,
                new Vector2(width / 2f - Edge - ButtonWidth - Gap - ButtonWidth / 2f, ButtonRow),
                new Vector2(ButtonWidth, ButtonHeight));
            pause.onClick.AddListener(() => view.PauseTapped?.Invoke());

            // What is left between Back's right edge and Pause's left edge,
            // which is off-centre in the strip - the buttons are the things
            // that have to be reachable.
            var tierLeft = -width / 2f + Edge + ButtonWidth + Gap;
            var tierRight = width / 2f - Edge - ButtonWidth * 2f - Gap * 2f;

            view._tierLabel = Ui.Label("Tier", rect, 22, Muted);
            Ui.Place(view._tierLabel.rectTransform,
                new Vector2((tierLeft + tierRight) / 2f, ButtonRow),
                new Vector2(tierRight - tierLeft, ButtonHeight));

            view._status = Ui.Label("Status", rect, 20, Label);
            Ui.Place(view._status.rectTransform, new Vector2(0, -14), new Vector2(width, 30));

            view._banner = Ui.Label("Banner", rect, 24, Danger);
            Ui.Place(view._banner.rectTransform, new Vector2(0, -46), new Vector2(width, 30));
            view._banner.text = string.Empty;

            return view;
        }

        public void Render(GameSession session, DifficultyTier tier, bool timerVisible)
        {
            _tierLabel.text = CopyTable.Tier(tier);

            var minutes = Mathf.FloorToInt(session.ElapsedSeconds / 60f);
            var seconds = Mathf.FloorToInt(session.ElapsedSeconds % 60f);
            var clock = timerVisible ? $"{minutes:00}:{seconds:00}" : CopyTable.HudTimerHidden;

            _status.text = CopyTable.HudStatus(clock, session.HeartsRemaining,
                session.MistakeCount, session.EmptyCellCount);
            _status.color = session.HeartsRemaining <= 1 ? Danger : Muted;

            switch (session.Status)
            {
                case SessionStatus.Completed:
                    _banner.text = CopyTable.HudSolvedBanner($"{minutes:00}:{seconds:00}");
                    _banner.color = new Color(0.15f, 0.55f, 0.30f);
                    break;
                case SessionStatus.Failed:
                    _banner.text = CopyTable.HudFailedBanner;
                    _banner.color = Danger;
                    break;
                default:
                    _banner.text = string.Empty;
                    break;
            }
        }
    }
}
