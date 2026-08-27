using System;
using Sudoku.Core.Copy;
using Sudoku.Game.Bootstrap;
using Sudoku.Game.Settings;
using UnityEngine;
using UnityEngine.UI;

namespace Sudoku.Game.Screens
{
    /// <summary>
    /// The toggles that shape how the game plays.
    ///
    /// Every row reads and writes the settings service and paints itself from
    /// the preference's own change notification, so the screen holds no copy of
    /// anything. A row cannot show "On" while the game plays "Off".
    ///
    /// It is one screen used two ways: Home pushes it as a destination, and the
    /// game pushes it on top of the puzzle. Nothing here knows which - the
    /// navigator's back stack answers both, and the game screen suspends its own
    /// clock on the way out.
    /// </summary>
    public sealed class SettingsView : MonoBehaviour, IScreen
    {
        static readonly Color TitleColor = new Color(0.16f, 0.17f, 0.20f);
        static readonly Color LabelColor = new Color(0.16f, 0.17f, 0.20f);
        static readonly Color MutedColor = new Color(0.55f, 0.57f, 0.62f);
        static readonly Color ButtonColor = new Color(0.93f, 0.94f, 0.96f);
        static readonly Color OnColor = new Color(0.55f, 0.75f, 0.98f);
        static readonly Color OffColor = new Color(0.87f, 0.88f, 0.90f);

        /// <summary>Vertical centre of the first row; each one below sits <see cref="RowStep"/> lower.</summary>
        const float FirstRow = 480f;
        const float RowStep = 150f;

        RectTransform _root;

        public Action BackTapped;

        public RectTransform Root => _root;

        public static SettingsView Create(Transform parent, GameSettings settings)
        {
            if (settings == null) throw new ArgumentNullException(nameof(settings));

            var rect = Ui.Rect("Settings", parent);
            var view = rect.gameObject.AddComponent<SettingsView>();
            view._root = rect;
            Ui.Stretch(rect);

            var title = Ui.Label("Title", rect, 56, TitleColor);
            Ui.Place(title.rectTransform, new Vector2(0, 700), new Vector2(800, 100));
            title.text = CopyTable.SettingsTitle;

            // The rows are listed in the order the player meets the things they
            // govern: the rules of the puzzle first, then how it is presented,
            // then how it sounds.
            AddRow(rect, 0, CopyTable.SettingsMistakeLimit, CopyTable.SettingsMistakeLimitNote,
                settings.MistakeLimit);
            AddRow(rect, 1, CopyTable.SettingsHighlightMistakes, null, settings.HighlightMistakes);
            AddRow(rect, 2, CopyTable.SettingsAutoRemoveNotes, null, settings.AutoRemoveNotes);
            AddRow(rect, 3, CopyTable.SettingsShowTimer, null, settings.TimerVisible);
            AddRow(rect, 4, CopyTable.SettingsSound, null, settings.SoundEnabled);
            AddRow(rect, 5, CopyTable.SettingsHaptics, null, settings.HapticsEnabled);

            // The screen's one aside. Settings is a place the voice is allowed
            // to speak, and the reassurance is the joke.
            var note = Ui.Label("Note", rect, 22, MutedColor);
            Ui.Place(note.rectTransform, new Vector2(0, -430), new Vector2(900, 40));
            note.text = CopyTable.SettingsNote;

            var back = Ui.Button("Back", rect, CopyTable.SettingsBack, 28, ButtonColor, LabelColor);
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

        /// <summary>
        /// One label, an optional line of small print, and a button that reads
        /// out the preference it flips.
        /// </summary>
        static void AddRow(Transform parent, int index, string label, string note, Preference<bool> preference)
        {
            var row = Ui.Rect("Row" + label, parent);
            Ui.Place(row, new Vector2(0, FirstRow - index * RowStep), new Vector2(900, 120));

            var caption = Ui.Label("Label", row, 30, LabelColor, TextAnchor.MiddleLeft);
            Ui.Place(caption.rectTransform, new Vector2(-130, note == null ? 0 : 18), new Vector2(620, 44));
            caption.text = label;

            if (note != null)
            {
                var smallPrint = Ui.Label("Note", row, 20, MutedColor, TextAnchor.MiddleLeft);
                Ui.Place(smallPrint.rectTransform, new Vector2(-130, -22), new Vector2(620, 34));
                smallPrint.text = note;
            }

            var toggle = Ui.Button("Toggle", row, "", 28, OffColor, LabelColor);
            Ui.Place((RectTransform)toggle.transform, new Vector2(340, 0), new Vector2(180, 84));

            var fill = (Image)toggle.targetGraphic;
            var state = toggle.GetComponentInChildren<Text>();

            // Painting from the preference rather than from the tap means an
            // outside change - a reset, a future "restore defaults" - shows up
            // here for free.
            preference.Observe(on =>
            {
                state.text = CopyTable.SettingsToggle(on);
                fill.color = on ? OnColor : OffColor;
            });

            toggle.onClick.AddListener(() => preference.Value = !preference.Value);
        }
    }
}
