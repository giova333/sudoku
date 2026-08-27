using System;
using Sudoku.Core.Difficulty;
using Sudoku.Game.Bootstrap;
using UnityEngine;
using UnityEngine.UI;

namespace Sudoku.Game.Screens
{
    /// <summary>
    /// The five tiers, one per row. This is where the greybox difficulty picker
    /// that used to sit in the game HUD belongs: choosing a difficulty is a
    /// decision taken before a puzzle starts, not a control inside one.
    /// </summary>
    public sealed class DifficultySelectView : MonoBehaviour, IScreen
    {
        static readonly Color TitleColor = new Color(0.16f, 0.17f, 0.20f);
        static readonly Color LabelColor = new Color(0.16f, 0.17f, 0.20f);
        static readonly Color ButtonColor = new Color(0.93f, 0.94f, 0.96f);

        RectTransform _root;

        public Action<DifficultyTier> TierChosen;
        public Action BackTapped;

        public RectTransform Root => _root;

        public static DifficultySelectView Create(Transform parent)
        {
            var rect = Ui.Rect("DifficultySelect", parent);
            var view = rect.gameObject.AddComponent<DifficultySelectView>();
            view._root = rect;
            Ui.Stretch(rect);

            var title = Ui.Label("Title", rect, 56, TitleColor);
            Ui.Place(title.rectTransform, new Vector2(0, 620), new Vector2(800, 100));
            title.text = "New Game";

            var tiers = (DifficultyTier[])Enum.GetValues(typeof(DifficultyTier));
            var top = (tiers.Length - 1) / 2f;

            for (var i = 0; i < tiers.Length; i++)
            {
                var tier = tiers[i];
                var button = Ui.Button($"Tier{tier}", rect, tier.ToString(), 34, ButtonColor, LabelColor);
                Ui.Place((RectTransform)button.transform,
                    new Vector2(0, (top - i) * 150f), new Vector2(640, 110));
                button.onClick.AddListener(() => view.TierChosen?.Invoke(tier));
            }

            var back = Ui.Button("Back", rect, "Back", 28, ButtonColor, LabelColor);
            Ui.Place((RectTransform)back.transform, new Vector2(0, -620), new Vector2(300, 88));
            back.onClick.AddListener(() => view.BackTapped?.Invoke());

            return view;
        }

        public void OnShow()
        {
        }

        public void OnHide()
        {
        }
    }
}
