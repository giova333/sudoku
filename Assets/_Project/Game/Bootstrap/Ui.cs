using System;
using Sudoku.Game.Theme;
using UnityEngine;
using UnityEngine.UI;

namespace Sudoku.Game.Bootstrap
{
    /// <summary>
    /// Small helpers for building the interface in code.
    ///
    /// The whole UI is constructed at runtime rather than authored as prefabs.
    /// The layout is diffable, reviewable and reproducible, with no binary
    /// scene merges - and now that the skin is a shape rather than a picture,
    /// there is nothing an authored prefab would have held that this does not.
    /// </summary>
    public static class Ui
    {
        static Font _font;

        /// <summary>
        /// Raised by every button this helper builds, so the interface chrome
        /// can be given a voice in one place instead of a sound call being
        /// remembered at each of the dozen screens that build buttons.
        ///
        /// It is deliberately not raised by the numpad, which builds its own
        /// buttons: a digit already announces itself as a placement or a
        /// mistake, and a click on top of that is one sound too many.
        /// </summary>
        public static Action ButtonTapped;

        /// <summary>
        /// The engine's built-in font.
        ///
        /// The only typeface reference left in the game, and the one thing
        /// still hardcoded here: the real faces are Fredoka and Nunito, held by
        /// <see cref="ThemeDefinition.DisplayFont"/> and
        /// <see cref="ThemeDefinition.NumeralFont"/>, and reaching them means
        /// swapping this whole path from <see cref="UnityEngine.UI.Text"/> to
        /// TextMeshPro. That swap needs the atlases the editor's
        /// Sudoku/Theme/Generate Font Assets command bakes, so it is not a
        /// change that can be made and verified without opening the editor.
        /// </summary>
        public static Font Font =>
            _font != null ? _font : (_font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf"));

        public static RectTransform Rect(string name, Transform parent)
        {
            var go = new GameObject(name, typeof(RectTransform));
            var rect = (RectTransform)go.transform;
            rect.SetParent(parent, false);
            return rect;
        }

        /// <summary>
        /// A flat rectangle in one of the theme's colours. Callers name the
        /// role, never the colour - <see cref="ThemedGraphic"/> is attached
        /// here so the panel repaints itself when the theme changes.
        /// </summary>
        public static Image Panel(string name, Transform parent, ThemeSlot slot)
        {
            var rect = Rect(name, parent);
            var image = rect.gameObject.AddComponent<Image>();
            image.raycastTarget = false;
            ThemedGraphic.Attach(image, slot);
            return image;
        }

        /// <summary>
        /// A panel cut with the skin's corner. Flat - no stroke and no shadow -
        /// for the things that are a surface rather than an object: a board
        /// cell, the ground behind a screen.
        /// </summary>
        public static Image Rounded(string name, Transform parent, ThemeSlot slot, float radius) =>
            Round(Panel(name, parent, slot), radius);

        /// <summary>Cuts an image that already exists to the skin's corner, for
        /// the few graphics a view builds itself.</summary>
        public static Image Round(Image image, float radius)
        {
            image.sprite = Shapes.Rounded(radius);
            image.type = Image.Type.Sliced;
            return image;
        }

        /// <summary>
        /// The whole skin in one object: a hard shadow, a filled face above it
        /// and a thick stroke round the face. Everything that reads as a
        /// physical thing - a button, a numpad key, a card, the board - is one
        /// of these, and content goes on <see cref="ChunkyBox.Face"/>.
        /// </summary>
        public static ChunkyBox Box(string name, Transform parent, ThemeSlot fill) =>
            ChunkyBox.Create(name, parent, fill);

        /// <summary>
        /// Makes a box tappable, and makes the tap show. Separate from
        /// <see cref="Button"/> because the numpad builds its own keys - it
        /// wants the press and not <see cref="ButtonTapped"/>'s click.
        /// </summary>
        public static ChunkyButton Pressable(ChunkyBox box)
        {
            var button = box.gameObject.AddComponent<ChunkyButton>();
            button.transition = Selectable.Transition.None;
            button.targetGraphic = box.Fill;
            button.Box = box;
            return button;
        }

        public static Text Label(string name, Transform parent, int size, ThemeSlot slot,
            TextAnchor anchor = TextAnchor.MiddleCenter)
        {
            var rect = Rect(name, parent);
            var text = rect.gameObject.AddComponent<Text>();
            text.font = Font;
            text.fontSize = size;
            text.alignment = anchor;
            text.raycastTarget = false;
            text.horizontalOverflow = HorizontalWrapMode.Overflow;
            text.verticalOverflow = VerticalWrapMode.Overflow;
            ThemedGraphic.Attach(text, slot);
            return text;
        }

        /// <summary>
        /// A chunky box carrying a centred label, wired to depress when it is
        /// held. Every screen builds the same button, so the recipe lives here
        /// rather than once per screen. Callers position the result with
        /// <see cref="Place"/>.
        ///
        /// It still answers as a plain <see cref="UnityEngine.UI.Button"/>: a
        /// caller that wants the fill's role reaches through
        /// <see cref="Selectable.targetGraphic"/> and one that wants the label
        /// reaches through the children, exactly as before.
        /// </summary>
        public static Button Button(string name, Transform parent, string text, int size,
            ThemeSlot fill, ThemeSlot textSlot)
        {
            var box = Box(name, parent, fill);

            var label = Label("Label", box.Face, size, textSlot);
            Stretch(label.rectTransform);
            label.text = text;

            var button = Pressable(box);
            button.onClick.AddListener(() => ButtonTapped?.Invoke());
            return button;
        }

        /// <summary>
        /// Where anything extra printed on a button belongs: the face, so it
        /// rides down with the press instead of hovering over a button that has
        /// moved out from under it.
        /// </summary>
        public static Transform Face(Button button)
        {
            var chunky = button as ChunkyButton;
            return chunky != null && chunky.Box != null ? chunky.Box.Face : button.transform;
        }

        /// <summary>
        /// An elapsed time as mm:ss. Home, difficulty select and the resume
        /// prompt all quote the same clock at the player, so how it reads lives
        /// in one place rather than three.
        /// </summary>
        public static string Clock(float seconds)
        {
            var whole = Mathf.Max(0, Mathf.FloorToInt(seconds));
            return $"{whole / 60:00}:{whole % 60:00}";
        }

        /// <summary>Anchors a rect to fill its parent with the given insets.</summary>
        public static void Stretch(RectTransform rect, float left = 0, float bottom = 0, float right = 0, float top = 0)
        {
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = new Vector2(left, bottom);
            rect.offsetMax = new Vector2(-right, -top);
        }

        /// <summary>Places a rect by explicit size and centre, anchored to the parent's centre.</summary>
        public static void Place(RectTransform rect, Vector2 center, Vector2 size)
        {
            rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.sizeDelta = size;
            rect.anchoredPosition = center;
        }
    }
}
