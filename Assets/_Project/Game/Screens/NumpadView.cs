using System;
using Sudoku.Core.Copy;
using Sudoku.Core.Session;
using Sudoku.Game.Bootstrap;
using Sudoku.Game.Theme;
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
        readonly Button[] _digits = new Button[10];
        readonly ThemedGraphic[] _digitBacks = new ThemedGraphic[10];
        readonly ThemedGraphic[] _digitLabels = new ThemedGraphic[10];
        readonly Text[] _digitBadges = new Text[10];

        ThemedGraphic _notesBack;
        ThemedGraphic _hintBack;
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

                var box = Ui.Box($"Action{action}", parent, ThemeSlot.NumpadFill);
                Ui.Place(box.Rect, new Vector2(x, 62), new Vector2(buttonWidth, 60));

                var label = Ui.Label("Label", box.Face, 20, ThemeSlot.NumpadLabel);
                Ui.Stretch(label.rectTransform);
                label.text = ActionLabel(action);

                var button = Ui.Pressable(box);
                button.onClick.AddListener(() => ActionTapped?.Invoke(action));

                if (action == PadAction.Notes) _notesBack = box.Theme;
                if (action == PadAction.Hint)
                {
                    _hintBack = box.Theme;
                    _hintLabel = label;
                }
            }
        }

        void BuildDigitRow(Transform parent, float width)
        {
            var slot = width / 9f;

            for (var digit = 1; digit <= 9; digit++)
            {
                var value = digit;
                var x = -width / 2f + slot * (digit - 0.5f);

                var box = Ui.Box($"Digit{digit}", parent, ThemeSlot.NumpadFill);
                Ui.Place(box.Rect, new Vector2(x, -34), new Vector2(slot - 8, 88));

                var label = Ui.Label("Label", box.Face, 32, ThemeSlot.NumpadLabel);
                Ui.Stretch(label.rectTransform, 0, 16, 0, 0);
                label.text = digit.ToString();

                var badge = Ui.Label("Badge", box.Face, 14, ThemeSlot.NumpadBadge);
                Ui.Stretch(badge.rectTransform, 0, 6, 0, 64);

                // The hold rides the key rather than its face, because a
                // pointer that leaves the key has to cancel it - and the face
                // is the thing that moves out from under the finger.
                var hold = box.gameObject.AddComponent<HoldDetector>();
                hold.Held = () => DigitHeld?.Invoke(value);

                var button = Ui.Pressable(box);
                button.onClick.AddListener(() =>
                {
                    // A long press already entered a note; ignore the release.
                    if (hold.ConsumeHeld()) return;
                    DigitTapped?.Invoke(value);
                });

                _digits[digit] = button;
                _digitBacks[digit] = box.Theme;
                _digitLabels[digit] = label.GetComponent<ThemedGraphic>();
                _digitBadges[digit] = badge;
            }
        }

        /// <summary>
        /// What an action button says. The pad's enum lives here and the copy
        /// table lives in Core, so the two are married in one place rather than
        /// Core being taught about a presentation enum.
        /// </summary>
        static string ActionLabel(PadAction action)
        {
            switch (action)
            {
                case PadAction.Undo: return CopyTable.PadUndo;
                case PadAction.Erase: return CopyTable.PadErase;
                case PadAction.Notes: return CopyTable.PadNotes;
                default: return CopyTable.PadHint;
            }
        }

        /// <summary>
        /// Greys out digits that are fully placed, and shows how many of each
        /// remain.
        ///
        /// <paramref name="showMistakes"/> is the immediate-feedback preference,
        /// and the badge honours it because refusing to count a digit is a way
        /// of saying it is wrong: a nine that stays on nine remaining names the
        /// mistake as surely as painting it red would. Off, every filled cell
        /// counts and the player checks their own work.
        /// </summary>
        public void Render(GameSession session, bool notesMode, bool showMistakes)
        {
            var placed = new int[10];
            for (var i = 0; i < Core.Model.Board.CellCount; i++)
            {
                var value = session.ValueAt(i);
                if (value == Core.Model.Board.Empty) continue;
                if (showMistakes && session.IsMistakeAt(i)) continue;

                placed[value]++;
            }

            for (var digit = 1; digit <= 9; digit++)
            {
                var remaining = 9 - placed[digit];
                var exhausted = remaining <= 0;

                _digits[digit].interactable = !exhausted;
                _digitBacks[digit].Use(exhausted ? ThemeSlot.NumpadDisabled : ThemeSlot.NumpadFill);
                _digitLabels[digit].Use(exhausted
                    ? ThemeSlot.NumpadLabelExhausted
                    : ThemeSlot.NumpadLabel);
                _digitBadges[digit].text = exhausted ? string.Empty : remaining.ToString();
            }

            _notesBack.Use(notesMode ? ThemeSlot.NumpadActive : ThemeSlot.NumpadFill);

            // While a hint is revealed the button is the second half of the
            // gesture, so it says what the next tap will do rather than how
            // many hints are left.
            var pending = session.PendingHint != null;
            _hintBack.Use(pending ? ThemeSlot.NumpadHintPending : ThemeSlot.NumpadFill);
            _hintLabel.text = pending
                ? CopyTable.PadHintFill
                : CopyTable.PadHintCount(session.HintsRemaining);
        }
    }
}
