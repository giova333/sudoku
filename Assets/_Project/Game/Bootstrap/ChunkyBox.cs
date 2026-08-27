using System;
using Sudoku.Game.Theme;
using UnityEngine;
using UnityEngine.UI;

namespace Sudoku.Game.Bootstrap
{
    /// <summary>
    /// One chunky rectangle: a hard shadow, a filled face sitting above it, and
    /// a thick stroke round the face. Every button, key, card and sheet in the
    /// game is one of these, which is what makes the interface look like one
    /// thing rather than nine screens that agree.
    ///
    /// The shadow never moves. Pressing lowers the face onto it, so
    /// <see cref="ShadowDepth"/> - the gap between the two - is the entire
    /// depress: six units at rest, two while held. That is a state change here,
    /// not an animation; see <see cref="PressAnimator"/>.
    ///
    /// Content goes on <see cref="Face"/>, never on this object, so a label
    /// rides down with the button it is written on.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class ChunkyBox : MonoBehaviour
    {
        /// <summary>How far a box that is not available fades. Not a colour -
        /// the whole box dims together, so a disabled control never ends up
        /// with a live stroke round a dead face.</summary>
        const float UnavailableAlpha = 0.45f;

        /// <summary>
        /// How a box travels between resting and pressed, when something wants
        /// it to travel rather than snap.
        ///
        /// Left null the press is instant, which is correct and complete on its
        /// own - the skin is a state change, and a game with no tweening in it
        /// still feels physical. Ticket #10 assigns this once, from the
        /// composition root, and drives <see cref="ShadowDepth"/> with the
        /// overshoot easing the rest of the motion pass uses; nothing here has
        /// to learn what a tween is, and no view has to be touched.
        /// </summary>
        public static Action<ChunkyBox, bool> PressAnimator;

        RectTransform _rect;
        RectTransform _face;
        Image _shadow;
        Image _fill;
        Image _stroke;
        ThemedGraphic _fillTheme;
        CanvasGroup _group;

        float _depth = Skin.RestingShadow;
        bool _pressed;

        /// <summary>The box's own rect - what a caller positions.</summary>
        public RectTransform Rect => _rect;

        /// <summary>What rides on the face and moves with it. Labels, badges and
        /// anything else drawn on the box belong here.</summary>
        public RectTransform Face => _face;

        /// <summary>The face itself, which is what a button takes as its target
        /// graphic and what a raycast lands on.</summary>
        public Image Fill => _fill;

        /// <summary>The hard shadow under the face.</summary>
        public Image Shadow => _shadow;

        /// <summary>The stroke drawn round the face.</summary>
        public Image Stroke => _stroke;

        /// <summary>The face's role, so a view that changes state - a digit
        /// running out, a destructive button arming - moves the slot.</summary>
        public ThemedGraphic Theme => _fillTheme;

        /// <summary>Whether the box is currently held down.</summary>
        public bool Pressed => _pressed;

        /// <summary>
        /// The gap between the face and its shadow, in interface units.
        ///
        /// A plain settable float rather than a two-state flag, because that is
        /// what a tween needs to be able to drive frame by frame. Writing it is
        /// the whole of the depress: the shadow stays where it is and the face
        /// comes down to meet it.
        /// </summary>
        public float ShadowDepth
        {
            get => _depth;
            set
            {
                _depth = value;
                _face.anchoredPosition = new Vector2(0f, value - Skin.RestingShadow);
            }
        }

        public static ChunkyBox Create(string name, Transform parent, ThemeSlot fill)
        {
            var rect = Ui.Rect(name, parent);
            var box = rect.gameObject.AddComponent<ChunkyBox>();
            box._rect = rect;
            box._group = rect.gameObject.AddComponent<CanvasGroup>();

            // The shadow is the same silhouette as the face, one flat colour,
            // offset straight down - no blur and no gradient. A soft shadow
            // would need a second texture and would stop reading as a solid
            // object the face is sitting on top of.
            box._shadow = Ui.Rounded("Shadow", rect, ThemeSlot.Shadow, Skin.CornerRadius);
            Ui.Stretch(box._shadow.rectTransform, 0f, -Skin.RestingShadow, 0f, Skin.RestingShadow);

            box._face = Ui.Rect("Face", rect);
            Ui.Stretch(box._face);

            box._fill = Ui.Rounded("Fill", box._face, fill, Skin.CornerRadius);
            box._fill.raycastTarget = true;
            Ui.Stretch(box._fill.rectTransform);
            box._fillTheme = box._fill.GetComponent<ThemedGraphic>();

            // The stroke is a separate graphic rather than a border baked into
            // the fill, so a theme can colour the two apart and a state change
            // can repaint the face without touching the outline.
            box._stroke = Ui.Panel("Stroke", box._face, ThemeSlot.Outline);
            box._stroke.sprite = Shapes.Outline(Skin.CornerRadius, Skin.BorderWidth);
            box._stroke.type = Image.Type.Sliced;
            Ui.Stretch(box._stroke.rectTransform);

            return box;
        }

        /// <summary>Moves the face to another role and repaints it now.</summary>
        public void Use(ThemeSlot slot) => _fillTheme.Use(slot);

        /// <summary>
        /// Lowers the face onto its shadow, or lifts it off again. Idempotent,
        /// because it is driven from a selectable's state transitions and those
        /// repeat.
        /// </summary>
        public void SetPressed(bool pressed)
        {
            if (_pressed == pressed) return;
            _pressed = pressed;

            if (PressAnimator != null)
            {
                PressAnimator(this, pressed);
                return;
            }

            ShadowDepth = pressed ? Skin.PressedShadow : Skin.RestingShadow;
        }

        /// <summary>Dims the box - face, stroke and shadow at once - when the
        /// control it dresses cannot be used.</summary>
        public void SetAvailable(bool available) =>
            _group.alpha = available ? 1f : UnavailableAlpha;
    }
}
