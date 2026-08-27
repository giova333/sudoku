using System;
using Sudoku.Core.Session;
using Sudoku.Game.Bootstrap;
using UnityEngine;
using UnityEngine.UI;

namespace Sudoku.Game.Screens
{
    /// <summary>The action row plus the nine digits.</summary>
    public enum PadAction
    {
        Undo,
        Erase,
        Notes,
        Hint
    }

    /// <summary>
    /// One row of nine digits with a remaining-count badge under each, above an
    /// action row. The row-of-nine keeps the board as large as portrait allows,
    /// and the badges give progress feedback without the player counting.
    /// </summary>
    public sealed class NumpadView : MonoBehaviour
    {
        static readonly Color ButtonColor = new Color(0.93f, 0.94f, 0.96f);
        static readonly Color ButtonDisabled = new Color(0.86f, 0.87f, 0.89f);
        static readonly Color ButtonActive = new Color(0.55f, 0.75f, 0.98f);
        static readonly Color LabelColor = new Color(0.16f, 0.17f, 0.20f);
        static readonly Color BadgeColor = new Color(0.50f, 0.52f, 0.57f);

        readonly Button[] _digits = new Button[10];
        readonly Image[] _digitBacks = new Image[10];
        readonly Text[] _digitLabels = new Text[10];
        readonly Text[] _digitBadges = new Text[10];

        Image _notesBack;
        Text _hintLabel;

        public Action<int> DigitTapped;
        public Action<int> DigitHeld;
        public Action<PadAction> ActionTapped;

        public static NumpadView Create(Transform parent, float width, float y)
        {
            var rect = Ui.Rect("Numpad", parent);
            var view = rect.gameObject.AddComponent<NumpadView>();
            Ui.Place(rect, new Vector2(0, y), new Vector2(width, 190));

            view.BuildActionRow(rect, width);
            view.BuildDigitRow(rect, width);
            return view;
        }

        void BuildActionRow(Transform parent, float width)
        {
            var actions = new[] { PadAction.Undo, PadAction.Erase, PadAction.Notes, PadAction.Hint };
            var buttonWidth = width / actions.Length - 8;

            for (var i = 0; i < actions.Length; i++)
            {
                var action = actions[i];
                var x = -width / 2f + width / actions.Length * (i + 0.5f);

                var image = Ui.Panel($"Action{action}", parent, ButtonColor);
                image.raycastTarget = true;
                Ui.Place(image.rectTransform, new Vector2(x, 58), new Vector2(buttonWidth, 56));

                var label = Ui.Label("Label", image.rectTransform, 20, LabelColor);
                Ui.Stretch(label.rectTransform);
                label.text = action.ToString();

                var button = image.gameObject.AddComponent<Button>();
                button.targetGraphic = image;
                button.onClick.AddListener(() => ActionTapped?.Invoke(action));

                if (action == PadAction.Notes) _notesBack = image;
                if (action == PadAction.Hint) _hintLabel = label;
            }
        }

        void BuildDigitRow(Transform parent, float width)
        {
            var slot = width / 9f;

            for (var digit = 1; digit <= 9; digit++)
            {
                var value = digit;
                var x = -width / 2f + slot * (digit - 0.5f);

                var image = Ui.Panel($"Digit{digit}", parent, ButtonColor);
                image.raycastTarget = true;
                Ui.Place(image.rectTransform, new Vector2(x, -32), new Vector2(slot - 6, 84));

                var label = Ui.Label("Label", image.rectTransform, 32, LabelColor);
                Ui.Stretch(label.rectTransform, 0, 16, 0, 0);
                label.text = digit.ToString();

                var badge = Ui.Label("Badge", image.rectTransform, 14, BadgeColor);
                Ui.Stretch(badge.rectTransform, 0, 4, 0, 62);

                var hold = image.gameObject.AddComponent<HoldDetector>();
                hold.Held = () => DigitHeld?.Invoke(value);

                var button = image.gameObject.AddComponent<Button>();
                button.targetGraphic = image;
                button.onClick.AddListener(() =>
                {
                    // A long press already entered a note; ignore the release.
                    if (hold.ConsumeHeld()) return;
                    DigitTapped?.Invoke(value);
                });

                _digits[digit] = button;
                _digitBacks[digit] = image;
                _digitLabels[digit] = label;
                _digitBadges[digit] = badge;
            }
        }

        /// <summary>
        /// Greys out digits that are fully placed, and shows how many of each
        /// remain.
        /// </summary>
        public void Render(GameSession session, bool notesMode)
        {
            var placed = new int[10];
            for (var i = 0; i < Core.Model.Board.CellCount; i++)
            {
                var value = session.ValueAt(i);
                if (value != Core.Model.Board.Empty && !session.IsMistakeAt(i))
                    placed[value]++;
            }

            for (var digit = 1; digit <= 9; digit++)
            {
                var remaining = 9 - placed[digit];
                var exhausted = remaining <= 0;

                _digits[digit].interactable = !exhausted;
                _digitBacks[digit].color = exhausted ? ButtonDisabled : ButtonColor;
                _digitLabels[digit].color = exhausted
                    ? new Color(LabelColor.r, LabelColor.g, LabelColor.b, 0.35f)
                    : LabelColor;
                _digitBadges[digit].text = exhausted ? "" : remaining.ToString();
            }

            _notesBack.color = notesMode ? ButtonActive : ButtonColor;
            _hintLabel.text = $"Hint {session.HintsRemaining}";
        }
    }
}
