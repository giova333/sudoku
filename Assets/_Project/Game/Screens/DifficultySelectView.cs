using System;
using Sudoku.Core.Copy;
using Sudoku.Core.Difficulty;
using Sudoku.Core.Persistence;
using Sudoku.Game.Bootstrap;
using Sudoku.Game.Theme;
using UnityEngine;
using UnityEngine.UI;

namespace Sudoku.Game.Screens
{
    /// <summary>
    /// The five tiers, one per row. This is where the greybox difficulty picker
    /// that used to sit in the game HUD belongs: choosing a difficulty is a
    /// decision taken before a puzzle starts, not a control inside one.
    ///
    /// A tier the player has a game waiting under says so, because the choice
    /// between continuing and starting fresh cannot be made from a row that
    /// looks like every other row.
    /// </summary>
    public sealed class DifficultySelectView : MonoBehaviour, IScreen
    {
        RectTransform _root;
        DifficultyTier[] _tiers;
        Text[] _waiting;

        public Action<DifficultyTier> TierChosen;
        public Action BackTapped;

        /// <summary>
        /// The puzzle waiting under a tier, or null when it has none. Asked on
        /// every showing rather than pushed, for the same reason Home asks: the
        /// answer lives in the save file and moves without this screen's
        /// knowledge.
        /// </summary>
        public Func<DifficultyTier, SaveSlot> Waiting;

        public RectTransform Root => _root;

        public static DifficultySelectView Create(Transform parent)
        {
            var rect = Ui.Rect("DifficultySelect", parent);
            var view = rect.gameObject.AddComponent<DifficultySelectView>();
            view._root = rect;
            Ui.Stretch(rect);

            var title = Ui.Label("Title", rect, 56, ThemeSlot.Title);
            Ui.Place(title.rectTransform, new Vector2(0, 620), new Vector2(800, 100));
            title.text = CopyTable.DifficultyTitle;

            var tiers = (DifficultyTier[])Enum.GetValues(typeof(DifficultyTier));
            var top = (tiers.Length - 1) / 2f;

            view._tiers = tiers;
            view._waiting = new Text[tiers.Length];

            for (var i = 0; i < tiers.Length; i++)
            {
                var tier = tiers[i];
                var button = Ui.Button($"Tier{tier}", rect, CopyTable.Tier(tier), 34,
                    ThemeSlot.ButtonFill, ThemeSlot.ButtonText);
                Ui.StackAt(button, (top - i) * 150f);
                button.onClick.AddListener(() => view.TierChosen?.Invoke(tier));

                // The marker rides inside the row rather than beside it, so a
                // row's whole hit area still belongs to the tier it names.
                var waiting = Ui.Label("Waiting", Ui.Face(button), 22, ThemeSlot.Muted);
                Ui.Place(waiting.rectTransform, new Vector2(180, 0), new Vector2(260, 60));
                view._waiting[i] = waiting;
            }

            var back = Ui.Button("Back", rect, CopyTable.DifficultyBack, 28,
                ThemeSlot.ButtonFill, ThemeSlot.ButtonText);
            Ui.Place((RectTransform)back.transform, new Vector2(0, -620), new Vector2(300, 88));
            back.onClick.AddListener(() => view.BackTapped?.Invoke());

            return view;
        }

        public void OnShow()
        {
            for (var i = 0; i < _tiers.Length; i++)
            {
                var slot = Waiting != null ? Waiting(_tiers[i]) : null;
                _waiting[i].text = slot == null
                    ? string.Empty
                    : CopyTable.DifficultyWaiting(Ui.Clock(slot.ElapsedSeconds));
            }
        }

        public void OnHide()
        {
        }
    }
}
