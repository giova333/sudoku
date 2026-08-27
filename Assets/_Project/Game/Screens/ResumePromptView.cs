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
    /// The question asked when the player picks a difficulty they already have
    /// a game under: continue it, or start again. Neither answer is assumed,
    /// because both are reasonable and only one of them is reversible.
    ///
    /// It is a screen rather than a panel over the picker so that the same
    /// back stack that got the player here takes them out again, and so the
    /// composition root keeps owning who leads where.
    /// </summary>
    public sealed class ResumePromptView : MonoBehaviour, IScreen
    {
        RectTransform _root;
        Text _title;
        Text _detail;
        ThemedGraphic _freshFill;
        Text _freshText;
        bool _freshArmed;
        SaveSlot _slot;

        /// <summary>Both answers carry the slot they were asked about, so the
        /// composition root never has to remember what the question was.</summary>
        public Action<SaveSlot> ResumeTapped;

        /// <summary>Fires only after the warning has been acknowledged.</summary>
        public Action<SaveSlot> StartFreshConfirmed;

        public Action BackTapped;

        public RectTransform Root => _root;

        public static ResumePromptView Create(Transform parent)
        {
            var rect = Ui.Rect("ResumePrompt", parent);
            var view = rect.gameObject.AddComponent<ResumePromptView>();
            view._root = rect;
            Ui.Stretch(rect);

            view._title = Ui.Label("Title", rect, 56, ThemeSlot.Title);
            Ui.Place(view._title.rectTransform, new Vector2(0, 520), new Vector2(800, 100));

            view._detail = Ui.Label("Detail", rect, 26, ThemeSlot.Muted);
            Ui.Place(view._detail.rectTransform, new Vector2(0, 420), new Vector2(800, 60));

            var resume = AddButton(rect, CopyTable.ResumeResume, 140,
                ThemeSlot.PrimaryFill, ThemeSlot.PrimaryText);
            resume.onClick.AddListener(() =>
            {
                var slot = view._slot;
                view.Disarm();
                view.ResumeTapped?.Invoke(slot);
            });

            var fresh = AddButton(rect, CopyTable.ResumeStartFresh, 0,
                ThemeSlot.ButtonFill, ThemeSlot.ButtonText);
            view._freshFill = fresh.targetGraphic.GetComponent<ThemedGraphic>();
            view._freshText = fresh.GetComponentInChildren<Text>();
            fresh.onClick.AddListener(view.OnStartFreshTapped);

            var back = AddButton(rect, CopyTable.ResumeBack, -140,
                ThemeSlot.ButtonFill, ThemeSlot.ButtonText);
            back.onClick.AddListener(() =>
            {
                view.Disarm();
                view.BackTapped?.Invoke();
            });

            var note = Ui.Label("Note", rect, 24, ThemeSlot.Muted);
            Ui.Place(note.rectTransform, new Vector2(0, -280), new Vector2(800, 40));
            note.text = CopyTable.ResumeNote;

            return view;
        }

        /// <summary>Names the puzzle being asked about, so the player is
        /// deciding about a difficulty and a clock rather than about a word.</summary>
        public void Offer(SaveSlot slot)
        {
            _slot = slot ?? throw new ArgumentNullException(nameof(slot));
            _title.text = CopyTable.ResumeTitle(CopyTable.Tier(slot.Tier));
            _detail.text = CopyTable.ResumeDetail(Ui.Clock(slot.ElapsedSeconds));
        }

        public void OnShow() => Disarm();

        public void OnHide() => Disarm();

        /// <summary>
        /// The warning. Starting fresh is the one answer that destroys
        /// something, so the first tap only says so and the second one means
        /// it. Leaving, resuming, or coming back here later takes it back.
        /// </summary>
        void OnStartFreshTapped()
        {
            if (!_freshArmed)
            {
                _freshArmed = true;
                _freshText.text = CopyTable.ResumeStartFreshConfirm;
                _freshFill.Use(ThemeSlot.WarnFill);
                return;
            }

            var slot = _slot;
            Disarm();
            StartFreshConfirmed?.Invoke(slot);
        }

        void Disarm()
        {
            _freshArmed = false;
            _freshText.text = CopyTable.ResumeStartFresh;
            _freshFill.Use(ThemeSlot.ButtonFill);
        }

        static Button AddButton(Transform parent, string text, float y, ThemeSlot fill, ThemeSlot textSlot)
        {
            var button = Ui.Button(text, parent, text, 34, fill, textSlot);
            Ui.Place((RectTransform)button.transform, new Vector2(0, y), new Vector2(640, 110));
            return button;
        }
    }
}
