using System;
using UnityEngine;
using UnityEngine.UI;

namespace Sudoku.Game.Bootstrap
{
    /// <summary>
    /// Small helpers for building the greybox interface in code.
    ///
    /// The whole UI is constructed at runtime rather than authored as prefabs.
    /// For the greybox that is a feature: the layout is diffable, reviewable and
    /// reproducible, with no binary scene merges. The skin pass revisits this
    /// once the visual language is settled.
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
        /// The engine's built-in font. TextMeshPro needs its essential
        /// resources imported and real font assets generated, which is skin-pass
        /// work; greybox text only has to be legible.
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

        public static Image Panel(string name, Transform parent, Color color)
        {
            var rect = Rect(name, parent);
            var image = rect.gameObject.AddComponent<Image>();
            image.color = color;
            image.raycastTarget = false;
            return image;
        }

        public static Text Label(string name, Transform parent, int size, Color color,
            TextAnchor anchor = TextAnchor.MiddleCenter)
        {
            var rect = Rect(name, parent);
            var text = rect.gameObject.AddComponent<Text>();
            text.font = Font;
            text.fontSize = size;
            text.color = color;
            text.alignment = anchor;
            text.raycastTarget = false;
            text.horizontalOverflow = HorizontalWrapMode.Overflow;
            text.verticalOverflow = VerticalWrapMode.Overflow;
            return text;
        }

        /// <summary>
        /// A filled panel carrying a centred label and a <see cref="UnityEngine.UI.Button"/>.
        /// Every greybox screen builds the same three objects, so the recipe
        /// lives here rather than once per screen. Callers position the result
        /// with <see cref="Place"/>.
        /// </summary>
        public static Button Button(string name, Transform parent, string text, int size,
            Color fill, Color textColor)
        {
            var image = Panel(name, parent, fill);
            image.raycastTarget = true;

            var label = Label("Label", image.rectTransform, size, textColor);
            Stretch(label.rectTransform);
            label.text = text;

            var button = image.gameObject.AddComponent<Button>();
            button.targetGraphic = image;
            button.onClick.AddListener(() => ButtonTapped?.Invoke());
            return button;
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
