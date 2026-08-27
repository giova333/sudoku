using System;
using Sudoku.Core.Persistence;
using Sudoku.Game.Bootstrap;
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
        static readonly Color TitleColor = new Color(0.16f, 0.17f, 0.20f);
        static readonly Color LabelColor = new Color(0.16f, 0.17f, 0.20f);
        static readonly Color MutedColor = new Color(0.45f, 0.47f, 0.52f);
        static readonly Color ButtonColor = new Color(0.93f, 0.94f, 0.96f);
        static readonly Color PrimaryColor = new Color(0.55f, 0.75f, 0.98f);
        static readonly Color WarnColor = new Color(0.97f, 0.80f, 0.55f);

        const string FreshLabel = "Start Fresh";
        const string ConfirmLabel = "Lose this puzzle? Tap again";

        RectTransform _root;
        Text _title;
        Text _detail;
        Image _freshFill;
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

            view._title = Ui.Label("Title", rect, 56, TitleColor);
            Ui.Place(view._title.rectTransform, new Vector2(0, 520), new Vector2(800, 100));

            view._detail = Ui.Label("Detail", rect, 26, MutedColor);
            Ui.Place(view._detail.rectTransform, new Vector2(0, 420), new Vector2(800, 60));

            var resume = AddButton(rect, "Resume", 140, PrimaryColor, LabelColor);
            resume.onClick.AddListener(() =>
            {
                var slot = view._slot;
                view.Disarm();
                view.ResumeTapped?.Invoke(slot);
            });

            var fresh = AddButton(rect, FreshLabel, 0, ButtonColor, LabelColor);
            view._freshFill = fresh.targetGraphic as Image;
            view._freshText = fresh.GetComponentInChildren<Text>();
            fresh.onClick.AddListener(view.OnStartFreshTapped);

            var back = AddButton(rect, "Back", -140, ButtonColor, LabelColor);
            back.onClick.AddListener(() =>
            {
                view.Disarm();
                view.BackTapped?.Invoke();
            });

            var note = Ui.Label("Note", rect, 24, MutedColor);
            Ui.Place(note.rectTransform, new Vector2(0, -280), new Vector2(800, 40));
            note.text = "Starting fresh throws this puzzle away for good.";

            return view;
        }

        /// <summary>Names the puzzle being asked about, so the player is
        /// deciding about a difficulty and a clock rather than about a word.</summary>
        public void Offer(SaveSlot slot)
        {
            _slot = slot ?? throw new ArgumentNullException(nameof(slot));
            _title.text = $"{slot.Tier} in progress";
            _detail.text = $"You are {Ui.Clock(slot.ElapsedSeconds)} into this puzzle.";
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
                _freshText.text = ConfirmLabel;
                _freshFill.color = WarnColor;
                return;
            }

            var slot = _slot;
            Disarm();
            StartFreshConfirmed?.Invoke(slot);
        }

        void Disarm()
        {
            _freshArmed = false;
            _freshText.text = FreshLabel;
            _freshFill.color = ButtonColor;
        }

        static Button AddButton(Transform parent, string text, float y, Color fill, Color textColor)
        {
            var button = Ui.Button(text, parent, text, 34, fill, textColor);
            Ui.Place((RectTransform)button.transform, new Vector2(0, y), new Vector2(640, 110));
            return button;
        }
    }
}
