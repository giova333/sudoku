using System.Collections.Generic;
using UnityEngine;

namespace Sudoku.Game.Bootstrap
{
    /// <summary>
    /// The rounded rectangle, drawn rather than imported.
    ///
    /// The whole skin is fat rounded rectangles - a fill, a stroke round it and
    /// a hard shadow under it - which is exactly what makes it buildable with
    /// no illustrator. A signed distance field over a few thousand pixels
    /// answers every shape the interface needs, and the result nine-slices, so
    /// one 52x52 texture dresses a button, a card and the board alike at any
    /// size they are built at.
    ///
    /// The textures carry white with the shape in the alpha channel and no
    /// colour of their own: an <see cref="UnityEngine.UI.Image"/> tinted by
    /// <see cref="Sudoku.Game.Theme.ThemedGraphic"/> is what gives a shape its
    /// colour, so a shape is never baked in one palette's terms.
    ///
    /// Nothing here is an asset. There is no file, nothing to import and
    /// nothing to license - the sprites are built on the frame they are first
    /// asked for and cached for the life of the process.
    /// </summary>
    public static class Shapes
    {
        /// <summary>
        /// The stretchable middle of the nine-slice. Two pixels rather than
        /// one: a single row would be sampled on its own edge by bilinear
        /// filtering and bleed the corner back into the middle.
        /// </summary>
        const int Middle = 2;

        static readonly Dictionary<int, Sprite> Cache = new Dictionary<int, Sprite>();

        /// <summary>A filled rounded rectangle.</summary>
        public static Sprite Rounded(float radius) => Get(Round(radius), 0);

        /// <summary>
        /// The same rectangle as a stroke of the given thickness, drawn inwards
        /// from the edge so a fill and its outline of the same radius line up
        /// exactly.
        /// </summary>
        public static Sprite Outline(float radius, float thickness) =>
            Get(Round(radius), Mathf.Max(1, Round(thickness)));

        static int Round(float value) => Mathf.Max(1, Mathf.RoundToInt(value));

        static Sprite Get(int radius, int thickness)
        {
            var key = radius * 1000 + thickness;

            // A cached sprite can have been destroyed under us: the project
            // runs with domain reload disabled, so this dictionary outlives the
            // textures it points at when Play mode ends.
            if (Cache.TryGetValue(key, out var cached) && cached != null)
                return cached;

            var built = Build(radius, thickness);
            Cache[key] = built;
            return built;
        }

        static Sprite Build(int radius, int thickness)
        {
            var size = radius * 2 + Middle;
            var half = size / 2f;

            var pixels = new Color32[size * size];
            for (var y = 0; y < size; y++)
            for (var x = 0; x < size; x++)
            {
                var px = x + 0.5f - half;
                var py = y + 0.5f - half;

                var coverage = Coverage(px, py, half, radius);
                if (thickness > 0)
                    coverage -= Coverage(px, py, half - thickness, radius - thickness);

                pixels[y * size + x] =
                    new Color32(255, 255, 255, (byte)Mathf.RoundToInt(Mathf.Clamp01(coverage) * 255f));
            }

            var texture = new Texture2D(size, size, TextureFormat.RGBA32, false)
            {
                name = $"Rounded{radius}x{thickness}",
                filterMode = FilterMode.Bilinear,
                wrapMode = TextureWrapMode.Clamp,
                hideFlags = HideFlags.HideAndDontSave
            };
            texture.SetPixels32(pixels);
            texture.Apply(false);

            // Pixels per unit of 100 against the canvas's own reference of 100
            // is what makes a texel one interface unit, so a 24-pixel corner in
            // the texture is the 24-unit corner the skin asks for.
            var sprite = Sprite.Create(texture, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f),
                100f, 0, SpriteMeshType.FullRect,
                new Vector4(radius, radius, radius, radius), false);
            sprite.name = texture.name;
            sprite.hideFlags = HideFlags.HideAndDontSave;
            return sprite;
        }

        /// <summary>
        /// How much of a pixel the rounded rectangle covers: the standard
        /// rounded-box distance field, read through a one-pixel band so the
        /// corners come out smooth instead of stepped. Nothing else in the
        /// skin needs antialiasing, because nothing else in the skin has a
        /// diagonal in it.
        /// </summary>
        static float Coverage(float x, float y, float half, float radius)
        {
            if (half <= 0f) return 0f;

            radius = Mathf.Clamp(radius, 0f, half);

            var qx = Mathf.Abs(x) - (half - radius);
            var qy = Mathf.Abs(y) - (half - radius);

            var outside = new Vector2(Mathf.Max(qx, 0f), Mathf.Max(qy, 0f)).magnitude;
            var inside = Mathf.Min(Mathf.Max(qx, qy), 0f);

            return Mathf.Clamp01(0.5f - (outside + inside - radius));
        }
    }
}
