using System;
using System.Collections.Generic;
using UnityEngine;

namespace Sudoku.Game.Theme
{
    /// <summary>
    /// The theme service: which look is in force, and everything that has to
    /// change when it stops being.
    ///
    /// Static because the whole interface is built by static factories called
    /// from the composition root, and a colour is needed at the moment a
    /// graphic is created - threading a service instance through a dozen
    /// <c>Create</c> signatures buys nothing when there is exactly one look per
    /// process. <see cref="Sudoku.Game.Bootstrap.Ui.ButtonTapped"/> made the
    /// same trade for the same reason.
    ///
    /// Switching is one pass over the live <see cref="ThemedGraphic"/>s. That
    /// includes the screens the navigator has deactivated, which is the point:
    /// a player who switches theme from Settings must not find the board still
    /// wearing the old one when they go back to it.
    /// </summary>
    public static class Themes
    {
        const string ResourceRoot = "Themes";

        static readonly List<ThemeDefinition> Definitions = new List<ThemeDefinition>();

        static ThemeDefinition _current;

        /// <summary>
        /// The theme in force. Never null: with nothing installed - a broken
        /// asset, an editor script poking at a view - it answers with a
        /// definition built from <see cref="ThemeDefinition"/>'s own defaults,
        /// which is Light. A missing asset should look plain, not invisible.
        /// </summary>
        public static ThemeDefinition Current =>
            _current != null ? _current : (_current = ScriptableObject.CreateInstance<ThemeDefinition>());

        /// <summary>
        /// Raised after a switch has been applied. Every image and label is
        /// already handled by <see cref="ThemedGraphic"/>; this is for the
        /// things that are not graphics - a camera's clear colour, a particle
        /// tint - and for anything that has to recompute rather than recolour.
        /// </summary>
        public static event Action<ThemeDefinition> Changed;

        /// <summary>
        /// Loads the shipped definitions. Called once by the composition root,
        /// before any screen is built.
        ///
        /// It clears the listener list as well as the definitions, because the
        /// project runs with domain reload disabled: without this, every Play
        /// mode session would leave its subscribers behind on the next one,
        /// pointed at objects that no longer exist.
        /// </summary>
        public static void Install()
        {
            Changed = null;
            Install(Resources.LoadAll<ThemeDefinition>(ResourceRoot));
        }

        /// <summary>
        /// Installs an explicit set. The seam a downloaded or purchased theme
        /// pack arrives through later, and the one a test would use.
        /// </summary>
        public static void Install(IEnumerable<ThemeDefinition> definitions)
        {
            if (definitions == null) throw new ArgumentNullException(nameof(definitions));

            Definitions.Clear();
            foreach (var definition in definitions)
                if (definition != null)
                    Definitions.Add(definition);

            if (Definitions.Count == 0)
                Debug.LogWarning($"No theme definitions found under Resources/{ResourceRoot}. " +
                                 "Falling back to the built-in light palette.");
        }

        /// <summary>
        /// Puts a theme in force and repaints everything wearing the old one.
        ///
        /// Asking for the theme that is already on is silent, so the settings
        /// screen painting itself cannot trigger a full repaint.
        /// </summary>
        public static void Use(ThemeChoice choice)
        {
            var next = Definition(choice);
            if (next == null || next == _current) return;

            _current = next;
            Repaint(next);
            Changed?.Invoke(next);
        }

        /// <summary>The installed definition for a choice, or null when nothing ships
        /// that look.</summary>
        public static ThemeDefinition Definition(ThemeChoice choice)
        {
            foreach (var definition in Definitions)
                if (definition.Choice == choice)
                    return definition;

            return null;
        }

        static void Repaint(ThemeDefinition theme)
        {
            var themed = UnityEngine.Object.FindObjectsByType<ThemedGraphic>(
                FindObjectsInactive.Include, FindObjectsSortMode.None);

            foreach (var graphic in themed)
                graphic.Paint(theme);
        }
    }
}
