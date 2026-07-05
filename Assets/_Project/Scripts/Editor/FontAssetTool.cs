using System.IO;
using System.Text;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.TextCore.LowLevel;

namespace DuskWarung.EditorTools
{
    /// <summary>
    /// Regenerates the project's TMP pixel-font assets (m5x7, monogram) from their source TTFs with
    /// CORRECT atlas settings. The bundled assets were generated with <c>padding: 0</c> in a non-AA SDF
    /// mode, so at the non-integer sizes the UI uses their glyph edges bleed and duplicate (text reads
    /// "doubled"). This bakes them as <see cref="GlyphRenderMode.SDFAA"/> with padding, which then scales
    /// crisply at any size. It overwrites the existing <c>.asset</c> paths, so every scene's
    /// <c>tmp.font</c> reference (which points at the asset GUID) keeps working — no re-wiring needed.
    ///
    /// If the scripted sub-asset handling ever misbehaves, the canonical fallback is the manual
    /// Font Asset Creator (Window ▸ TextMeshPro ▸ Font Asset Creator) — see WORKFLOW.md.
    /// </summary>
    public static class FontAssetTool
    {
        private const int SamplingPointSize = 64;
        private const int Padding = 5;
        private const int AtlasSize = 512;

        // Source TTFs (vendor fonts) → destination TMP asset (authored, referenced by every UI scene).
        private static readonly (string ttf, string asset)[] Fonts =
        {
            ("Assets/Fonts/m5x7/m5x7.ttf", DuskEditorUtil.FontM5x7),
            ("Assets/Fonts/monogram/ttf/monogram.ttf", DuskEditorUtil.FontMonogram),
        };

        [MenuItem("Tools/Dusk Warung/3. Rebuild Fonts", priority = 3)]
        public static void Run()
        {
            int rebuilt = 0;
            foreach ((string ttf, string asset) in Fonts)
            {
                if (Rebuild(ttf, asset))
                {
                    rebuilt++;
                }
            }

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log($"[Dusk] Rebuilt {rebuilt}/{Fonts.Length} font atlas(es) as SDFAA (padding {Padding}). " +
                      "Reopen Title/End/Battle to confirm the text is crisp.");
        }

        private static bool Rebuild(string ttfPath, string assetPath)
        {
            var sourceFont = AssetDatabase.LoadAssetAtPath<Font>(ttfPath);
            if (sourceFont == null)
            {
                Debug.LogWarning($"[Dusk] Source TTF missing (skipped): {ttfPath}");
                return false;
            }

            // Build a fresh SDFAA font asset from the TTF and bake the glyphs the game actually uses.
            TMP_FontAsset fontAsset = TMP_FontAsset.CreateFontAsset(
                sourceFont, SamplingPointSize, Padding, GlyphRenderMode.SDFAA,
                AtlasSize, AtlasSize, AtlasPopulationMode.Dynamic, enableMultiAtlasSupport: true);

            if (fontAsset == null)
            {
                Debug.LogError($"[Dusk] Failed to create font asset from {ttfPath}.");
                return false;
            }

            fontAsset.TryAddCharacters(PrintableAscii());
            fontAsset.name = Path.GetFileNameWithoutExtension(assetPath);

            // Overwrite the existing .asset in place. CreateAsset over an existing path keeps the
            // original .meta (and therefore the GUID), so scene references survive.
            AssetDatabase.CreateAsset(fontAsset, assetPath);

            // Re-attach the regenerated atlas texture(s) + material as sub-assets of the font asset.
            if (fontAsset.atlasTextures != null)
            {
                for (int i = 0; i < fontAsset.atlasTextures.Length; i++)
                {
                    Texture2D atlas = fontAsset.atlasTextures[i];
                    if (atlas == null)
                    {
                        continue;
                    }

                    atlas.name = fontAsset.name + " Atlas" + (i > 0 ? $" {i}" : string.Empty);
                    AssetDatabase.AddObjectToAsset(atlas, fontAsset);
                }
            }

            if (fontAsset.material != null)
            {
                fontAsset.material.name = fontAsset.name + " Material";
                AssetDatabase.AddObjectToAsset(fontAsset.material, fontAsset);
            }

            EditorUtility.SetDirty(fontAsset);
            AssetDatabase.ImportAsset(assetPath);
            Debug.Log($"[Dusk] Rebuilt font: {assetPath} (from {Path.GetFileName(ttfPath)}).");
            return true;
        }

        /// <summary>The printable ASCII range (space … tilde) — every glyph the UI strings use.</summary>
        private static string PrintableAscii()
        {
            var sb = new StringBuilder(95);
            for (char c = ' '; c <= '~'; c++)
            {
                sb.Append(c);
            }

            return sb.ToString();
        }
    }
}
