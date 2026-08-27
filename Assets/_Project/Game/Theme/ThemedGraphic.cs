using UnityEngine;
using UnityEngine.UI;

namespace Sudoku.Game.Theme
{
    /// <summary>
    /// The tag that makes one image or label themed: it remembers which
    /// <see cref="ThemeSlot"/> the graphic beneath it plays, and repaints from
    /// whichever theme is in force.
    ///
    /// Holding the slot on the object is what makes an instant switch possible
    /// without every screen keeping a list of its own graphics and a matching
    /// repaint method. A view that changes state - a digit running out, a
    /// destructive button arming - moves the slot with <see cref="Use"/> and
    /// the colour follows; it never names a colour.
    ///
    /// It deliberately does not subscribe to <see cref="Themes"/>. There are
    /// close to a thousand of these once the board is built, most of them under
    /// a deactivated screen, and a thousand delegates that have to be unhooked
    /// correctly is a leak waiting to happen. The service finds them instead,
    /// once, on the rare frame a player switches theme.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class ThemedGraphic : MonoBehaviour
    {
        [SerializeField] ThemeSlot _slot;

        Graphic _graphic;

        /// <summary>The role this graphic plays.</summary>
        public ThemeSlot Slot => _slot;

        /// <summary>
        /// Tags a graphic and paints it immediately, so a screen built while a
        /// theme is already in force never flashes the wrong colour.
        /// </summary>
        public static ThemedGraphic Attach(Graphic graphic, ThemeSlot slot)
        {
            if (graphic == null) return null;

            var themed = graphic.gameObject.AddComponent<ThemedGraphic>();
            themed._graphic = graphic;
            themed._slot = slot;
            themed.Paint(Themes.Current);
            return themed;
        }

        /// <summary>Moves this graphic to another role and repaints it now.</summary>
        public void Use(ThemeSlot slot)
        {
            if (_slot == slot) return;

            _slot = slot;
            Paint(Themes.Current);
        }

        /// <summary>Repaints from the given theme. Called by <see cref="Themes"/> on
        /// every switch, and by <see cref="Attach"/> and <see cref="Use"/> at once.</summary>
        public void Paint(ThemeDefinition theme)
        {
            if (theme == null) return;
            if (_graphic == null) _graphic = GetComponent<Graphic>();
            if (_graphic == null) return;

            _graphic.color = theme.Of(_slot);
        }
    }
}
