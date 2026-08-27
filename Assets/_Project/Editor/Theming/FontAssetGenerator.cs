using System.IO;
using Sudoku.Game.Theme;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.TextCore.LowLevel;

namespace Sudoku.Editor.Theming
{
    /// <summary>
    /// Bakes the two shipped typefaces into TextMeshPro font assets, and wires
    /// the results into every shipped <see cref="ThemeDefinition"/>.
    ///
    /// Font asset generation is an editor-only operation - it rasterises glyphs
    /// through the font engine and writes a texture - so unlike the puzzle bake
    /// there is no headless twin of this. Making it a menu command rather than
    /// a hand-driven pass through TextMeshPro's Font Asset Creator window is
    /// what makes the atlases reproducible: the sampling size, the padding, the
    /// render mode and the character set are stated here once instead of being
    /// re-typed into a window every time a font is updated.
    ///
    /// The atlases are static, not dynamic: the game shows ASCII and nothing
    /// else, so there is no reason to ship the font files themselves or to
    /// rasterise anything on a player's device.
    /// </summary>
    public static class FontAssetGenerator
    {
        const string FontsDir = "Assets/_Project/Fonts";
        const string OutputDir = "Assets/_Project/Fonts/Generated";
        const string ThemesDir = "Assets/_Project/Resources/Themes";

        /// <summary>Fredoka: headings, buttons, the numpad.</summary>
        const string DisplaySource = FontsDir + "/Fredoka-Variable.ttf";
        const string DisplayAsset = OutputDir + "/Fredoka SDF.asset";

        /// <summary>Nunito: board digits and pencil marks.</summary>
        const string NumeralSource = FontsDir + "/Nunito-Variable.ttf";
        const string NumeralAsset = OutputDir + "/Nunito SDF.asset";

        /// <summary>
        /// SDF16 at 90pt with 9px padding - TextMeshPro's own defaults for a
        /// UI face, and what the spec asks for. 16 gradient steps is the middle
        /// setting: SDF8 shows banding on the large results-card numerals and
        /// SDF32 doubles the atlas for a difference nobody can see at these
        /// sizes.
        /// </summary>
        const int SamplingPointSize = 90;
        const int AtlasPadding = 9;
        const int AtlasSize = 1024;

        /// <summary>
        /// Printable ASCII, which is the whole of what the game renders: digits,
        /// the interface copy, and the punctuation it uses. Anything outside it
        /// would come back as a missing glyph, which is the correct failure -
        /// it says the copy has drifted out of the character set the atlas was
        /// baked for.
        /// </summary>
        static string Ascii
        {
            get
            {
                var characters = new char[95];
                for (var i = 0; i < characters.Length; i++)
                    characters[i] = (char)(32 + i);
                return new string(characters);
            }
        }

        [MenuItem("Sudoku/Theme/Generate Font Assets")]
        static void Generate()
        {
            // TextMeshPro cannot render anything at all - generated atlas or
            // not - until its essential resources are in the project, and that
            // import is asynchronous. Ask for it and stop; the second run finds
            // the settings in place.
            if (TMP_Settings.instance == null)
            {
                TMP_PackageResourceImporter.ImportResources(true, false, false);
                Debug.LogWarning("TextMeshPro essential resources were missing and are being imported. " +
                                 "Run Sudoku/Theme/Generate Font Assets again once the import finishes.");
                return;
            }

            Directory.CreateDirectory(OutputDir);

            var display = Bake(DisplaySource, DisplayAsset);
            var numerals = Bake(NumeralSource, NumeralAsset);
            if (display == null || numerals == null) return;

            AdoptInto(display, numerals);

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log($"Generated SDF16 ASCII atlases for Fredoka and Nunito into {OutputDir}, " +
                      "and assigned them to every theme.");
        }

        /// <summary>
        /// Rasterises one typeface's ASCII range into a static SDF atlas and
        /// writes it, with its texture and material, as a single asset.
        /// </summary>
        static TMP_FontAsset Bake(string sourcePath, string assetPath)
        {
            var source = AssetDatabase.LoadAssetAtPath<Font>(sourcePath);
            if (source == null)
            {
                Debug.LogError($"No font at {sourcePath}. The OFL font files are committed to the " +
                               "repository; if they are missing, the working copy is incomplete.");
                return null;
            }

            var font = TMP_FontAsset.CreateFontAsset(source, SamplingPointSize, AtlasPadding,
                GlyphRenderMode.SDF16, AtlasSize, AtlasSize, AtlasPopulationMode.Dynamic,
                enableMultiAtlasSupport: false);

            if (font == null)
            {
                Debug.LogError($"TextMeshPro could not read a font face from {sourcePath}.");
                return null;
            }

            font.name = Path.GetFileNameWithoutExtension(assetPath);

            // Kerning and the rest of the OpenType features are baked in here
            // rather than looked up on device: a static atlas has no font file
            // to consult at runtime.
            if (!font.TryAddCharacters(Ascii, out var missing, includeFontFeatures: true))
                Debug.LogWarning($"{font.name}: no glyph for [{missing}]. Either the face does not " +
                                 $"contain them or {AtlasSize}x{AtlasSize} is too small to hold the set.");

            // Static from here on: the source font file is dropped and nothing
            // is rasterised on a player's device.
            font.atlasPopulationMode = AtlasPopulationMode.Static;
            font.ReadFontAssetDefinition();

            AssetDatabase.DeleteAsset(assetPath);
            AssetDatabase.CreateAsset(font, assetPath);

            // The atlas and its material are sub-assets of the font asset, which
            // is how TextMeshPro's own creator saves them - three loose files
            // that have to travel together is three chances to move one.
            var atlas = font.atlasTextures[0];
            atlas.name = font.name + " Atlas";
            AssetDatabase.AddObjectToAsset(atlas, font);

            font.material.name = font.name + " Material";
            AssetDatabase.AddObjectToAsset(font.material, font);

            EditorUtility.SetDirty(font);
            return font;
        }

        /// <summary>
        /// Points every shipped theme at the freshly generated faces. Themes
        /// differ in colour, not in type - the typography is the game's, not a
        /// skin's - so this is a loop rather than a decision.
        /// </summary>
        static void AdoptInto(TMP_FontAsset display, TMP_FontAsset numerals)
        {
            foreach (var guid in AssetDatabase.FindAssets("t:ThemeDefinition", new[] { ThemesDir }))
            {
                var path = AssetDatabase.GUIDToAssetPath(guid);
                var theme = AssetDatabase.LoadAssetAtPath<ThemeDefinition>(path);
                if (theme == null) continue;

                theme.SetFonts(display, numerals);
                EditorUtility.SetDirty(theme);
            }
        }
    }
}
