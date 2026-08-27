using System;
using Sudoku.Core.Difficulty;
using Sudoku.Core.Session;
using Sudoku.Game.Bootstrap;
using UnityEngine;
using UnityEngine.UI;

namespace Sudoku.Game.Screens
{
    /// <summary>
    /// The status strip above the board, plus the greybox difficulty picker.
    /// A proper home and difficulty-select screen replaces the picker later.
    /// </summary>
    public sealed class HudView : MonoBehaviour
    {
        static readonly Color Label = new Color(0.16f, 0.17f, 0.20f);
        static readonly Color Muted = new Color(0.45f, 0.47f, 0.52f);
        static readonly Color Danger = new Color(0.83f, 0.21f, 0.24f);
        static readonly Color ButtonColor = new Color(0.93f, 0.94f, 0.96f);
        static readonly Color ButtonActive = new Color(0.55f, 0.75f, 0.98f);

        Text _status;
        Text _banner;
        readonly System.Collections.Generic.Dictionary<DifficultyTier, Image> _tierButtons =
            new System.Collections.Generic.Dictionary<DifficultyTier, Image>();

        public Action<DifficultyTier> TierChosen;

        public static HudView Create(Transform parent, float width, float y)
        {
            var rect = Ui.Rect("Hud", parent);
            var view = rect.gameObject.AddComponent<HudView>();
            Ui.Place(rect, new Vector2(0, y), new Vector2(width, 120));

            var tiers = (DifficultyTier[])Enum.GetValues(typeof(DifficultyTier));
            var slot = width / tiers.Length;

            for (var i = 0; i < tiers.Length; i++)
            {
                var tier = tiers[i];
                var x = -width / 2f + slot * (i + 0.5f);

                var image = Ui.Panel($"Tier{tier}", rect, ButtonColor);
                image.raycastTarget = true;
                Ui.Place(image.rectTransform, new Vector2(x, 34), new Vector2(slot - 6, 46));

                var label = Ui.Label("Label", image.rectTransform, 16, Label);
                Ui.Stretch(label.rectTransform);
                label.text = tier.ToString();

                var button = image.gameObject.AddComponent<Button>();
                button.targetGraphic = image;
                button.onClick.AddListener(() => view.TierChosen?.Invoke(tier));

                view._tierButtons[tier] = image;
            }

            view._status = Ui.Label("Status", rect, 20, Label);
            Ui.Place(view._status.rectTransform, new Vector2(0, -14), new Vector2(width, 30));

            view._banner = Ui.Label("Banner", rect, 24, Danger);
            Ui.Place(view._banner.rectTransform, new Vector2(0, -46), new Vector2(width, 30));
            view._banner.text = "";

            return view;
        }

        public void Render(GameSession session, DifficultyTier tier, bool timerVisible)
        {
            foreach (var pair in _tierButtons)
                pair.Value.color = pair.Key == tier ? ButtonActive : ButtonColor;

            var minutes = Mathf.FloorToInt(session.ElapsedSeconds / 60f);
            var seconds = Mathf.FloorToInt(session.ElapsedSeconds % 60f);
            var clock = timerVisible ? $"{minutes:00}:{seconds:00}" : "--:--";

            _status.text = $"{clock}    Hearts {session.HeartsRemaining}    " +
                           $"Mistakes {session.MistakeCount}    Left {session.EmptyCellCount}";
            _status.color = session.HeartsRemaining <= 1 ? Danger : Muted;

            switch (session.Status)
            {
                case SessionStatus.Completed:
                    _banner.text = $"Solved in {minutes:00}:{seconds:00} - pick a difficulty for another";
                    _banner.color = new Color(0.15f, 0.55f, 0.30f);
                    break;
                case SessionStatus.Failed:
                    _banner.text = "Out of hearts - pick a difficulty to try again";
                    _banner.color = Danger;
                    break;
                default:
                    _banner.text = "";
                    break;
            }
        }
    }
}
