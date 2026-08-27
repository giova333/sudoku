using System;
using Sudoku.Game.Bootstrap;
using Sudoku.Game.Settings;
using Sudoku.Game.Theme;
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

            var title = Ui.Label("Title", rect, 56, ThemeSlot.Title);
            Ui.Place(title.rectTransform, new Vector2(0, 700), new Vector2(800, 100));
            title.text = "Settings";

            // The rows are listed in the order the player meets the things they
            // govern: the rules of the puzzle first, then how it is presented,
            // then how it sounds.
            AddRow(rect, 0, "Mistake limit", "Applies from the next puzzle", settings.MistakeLimit);
            AddRow(rect, 1, "Highlight mistakes", null, settings.HighlightMistakes);
            AddRow(rect, 2, "Auto-remove notes", null, settings.AutoRemoveNotes);
            AddRow(rect, 3, "Show timer", null, settings.TimerVisible);
            AddRow(rect, 4, "Sound", null, settings.SoundEnabled);
            AddRow(rect, 5, "Haptics", null, settings.HapticsEnabled);

            // The theme sits last because it is the one row that changes the
            // screen it is on: the player sees the answer to what they just
            // asked for without leaving.
            AddRow(rect, 6, "Dark theme", null, settings.Theme);

            var back = Ui.Button("Back", rect, "Back", 28, ThemeSlot.ButtonFill, ThemeSlot.ButtonText);
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

        /// <summary>A row over a two-state preference.</summary>
        static void AddRow(Transform parent, int index, string label, string note,
            Preference<bool> preference) =>
            AddRow(parent, index, label, note,
                paint => preference.Observe(paint),
                () => preference.Value = !preference.Value);

        /// <summary>
        /// A row over the theme, which is two-state today and will not stay
        /// that way. When a third theme ships this row becomes a picker; until
        /// then, asking a player to page through a list of two is worse than a
        /// switch.
        /// </summary>
        static void AddRow(Transform parent, int index, string label, string note,
            Preference<ThemeChoice> preference) =>
            AddRow(parent, index, label, note,
                paint => preference.Observe(choice => paint(choice == ThemeChoice.Dark)),
                () => preference.Value = preference.Value == ThemeChoice.Dark
                    ? ThemeChoice.Light
                    : ThemeChoice.Dark);

        /// <summary>
        /// One label, an optional line of small print, and a button that reads
        /// out the preference it flips.
        ///
        /// It is given a way to watch and a way to flip rather than a
        /// preference, so a preference that is not a bool - the theme - wears
        /// the same row as the ones that are, without the row learning what a
        /// theme is.
        /// </summary>
        static void AddRow(Transform parent, int index, string label, string note,
            Action<Action<bool>> observe, Action flip)
        {
            var row = Ui.Rect("Row" + label, parent);
            Ui.Place(row, new Vector2(0, FirstRow - index * RowStep), new Vector2(900, 120));

            var caption = Ui.Label("Label", row, 30, ThemeSlot.Body, TextAnchor.MiddleLeft);
            Ui.Place(caption.rectTransform, new Vector2(-130, note == null ? 0 : 18), new Vector2(620, 44));
            caption.text = label;

            if (note != null)
            {
                var smallPrint = Ui.Label("Note", row, 20, ThemeSlot.Muted, TextAnchor.MiddleLeft);
                Ui.Place(smallPrint.rectTransform, new Vector2(-130, -22), new Vector2(620, 34));
                smallPrint.text = note;
            }

            var toggle = Ui.Button("Toggle", row, "", 28, ThemeSlot.ToggleOff, ThemeSlot.ButtonText);
            Ui.Place((RectTransform)toggle.transform, new Vector2(340, 0), new Vector2(180, 84));

            var fill = toggle.targetGraphic.GetComponent<ThemedGraphic>();
            var state = toggle.GetComponentInChildren<Text>();

            // Painting from the preference rather than from the tap means an
            // outside change - a reset, a future "restore defaults" - shows up
            // here for free.
            observe(on =>
            {
                state.text = on ? "On" : "Off";
                fill.Use(on ? ThemeSlot.ToggleOn : ThemeSlot.ToggleOff);
            });

            toggle.onClick.AddListener(() => flip());
        }
    }
}
