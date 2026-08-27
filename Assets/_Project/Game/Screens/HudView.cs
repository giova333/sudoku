using System;
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

        Text _tierLabel;
        Text _status;
        Text _banner;

        public Action BackTapped;

        public static HudView Create(Transform parent, float width, float y)
        {
            var rect = Ui.Rect("Hud", parent);
            var view = rect.gameObject.AddComponent<HudView>();
            Ui.Place(rect, new Vector2(0, y), new Vector2(width, 120));

            var back = Ui.Button("Back", rect, "Back", 18, ButtonColor, Label);
            Ui.Place((RectTransform)back.transform, new Vector2(-width / 2f + 62, 34), new Vector2(116, 46));
            back.onClick.AddListener(() => view.BackTapped?.Invoke());

            view._tierLabel = Ui.Label("Tier", rect, 22, Muted);
            Ui.Place(view._tierLabel.rectTransform, new Vector2(0, 34), new Vector2(width - 260, 46));

            view._status = Ui.Label("Status", rect, 20, Label);
            Ui.Place(view._status.rectTransform, new Vector2(0, -14), new Vector2(width, 30));

            view._banner = Ui.Label("Banner", rect, 24, Danger);
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
            _status.color = session.HeartsRemaining <= 1 ? Danger : Muted;

            switch (session.Status)
            {
                case SessionStatus.Completed:
                    _banner.text = $"Solved in {minutes:00}:{seconds:00}";
                    _banner.color = new Color(0.15f, 0.55f, 0.30f);
                    break;
                case SessionStatus.Failed:
                    _banner.text = "Out of hearts";
                    _banner.color = Danger;
                    break;
                default:
                    _banner.text = "";
                    break;
            }
        }
    }
}
