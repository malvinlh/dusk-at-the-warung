using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.U2D.Sprites;
using UnityEngine;

namespace DuskWarung.EditorTools
{
    /// <summary>Shared paths and sprite helpers for the asset-prep Editor tools (slice / tiles / fonts).</summary>
    public static class DuskEditorUtil
    {
        public const string Root = "Assets/_Project";
        public const string Art = Root + "/Art";
        public const string SpritesDir = Art + "/Sprites";
        public const string FontsDir = Art + "/Fonts";
        public const string TilesheetsDir = SpritesDir + "/Tilesheets";
        public const string TilesDir = Art + "/Tiles";
        public const string FontM5x7 = FontsDir + "/m5x7.asset";
        public const string FontMonogram = FontsDir + "/monogram.asset";

        /// <summary>Creates a nested asset folder (and any missing parents) if it does not exist.</summary>
        public static void EnsureFolder(string folder)
        {
            folder = folder.Replace('\\', '/').TrimEnd('/');
            if (string.IsNullOrEmpty(folder) || folder == "Assets" || AssetDatabase.IsValidFolder(folder))
            {
                return;
            }

            string parent = Path.GetDirectoryName(folder).Replace('\\', '/');
            string leaf = Path.GetFileName(folder);
            EnsureFolder(parent);
            AssetDatabase.CreateFolder(parent, leaf);
        }

        /// <summary>Loads all sliced sub-sprites of a sheet in reading order (top-to-bottom, left-to-right).</summary>
        public static List<Sprite> LoadSpritesRowMajor(string pngPath)
        {
            List<Sprite> sprites = AssetDatabase.LoadAllAssetsAtPath(pngPath).OfType<Sprite>().ToList();
            sprites.Sort((a, b) =>
            {
                Rect ra = a.rect, rb = b.rect;
                if (Mathf.Abs(ra.y - rb.y) > 0.5f)
                {
                    return rb.y.CompareTo(ra.y); // higher y (top of texture) first
                }

                return ra.x.CompareTo(rb.x); // then left to right
            });
            return sprites;
        }

        /// <summary>Grid-slices a texture into cellW×cellH sprites (top row first, left to right).</summary>
        public static void GridSlice(string texturePath, int cellW, int cellH)
        {
            if (AssetImporter.GetAtPath(texturePath) is not TextureImporter importer)
            {
                return;
            }

            if (importer.textureType != TextureImporterType.Sprite || importer.spriteImportMode != SpriteImportMode.Multiple)
            {
                importer.textureType = TextureImporterType.Sprite;
                importer.spriteImportMode = SpriteImportMode.Multiple;
                importer.SaveAndReimport();
            }

            var texture = AssetDatabase.LoadAssetAtPath<Texture2D>(texturePath);
            if (texture == null)
            {
                return;
            }

            int cols = texture.width / cellW;
            int rows = texture.height / cellH;
            if (cols <= 0 || rows <= 0)
            {
                Debug.LogWarning($"[Dusk] {Path.GetFileName(texturePath)} is smaller than one {cellW}×{cellH} cell — skipped.");
                return;
            }

            var factories = new SpriteDataProviderFactories();
            factories.Init();
            ISpriteEditorDataProvider provider = factories.GetSpriteEditorDataProviderFromObject(importer);
            provider.InitSpriteEditorDataProvider();

            string baseName = Path.GetFileNameWithoutExtension(texturePath);
            var rects = new List<SpriteRect>();
            var pairs = new List<SpriteNameFileIdPair>();
            int index = 0;

            for (int r = 0; r < rows; r++)      // top row first
            {
                for (int c = 0; c < cols; c++)  // left to right
                {
                    var spriteRect = new SpriteRect
                    {
                        name = $"{baseName}_{index}",
                        rect = new Rect(c * cellW, texture.height - (r + 1) * cellH, cellW, cellH),
                        pivot = new Vector2(0.5f, 0.5f),
                        alignment = SpriteAlignment.Center,
                        border = Vector4.zero,
                        spriteID = GUID.Generate()
                    };
                    rects.Add(spriteRect);
                    pairs.Add(new SpriteNameFileIdPair(spriteRect.name, spriteRect.spriteID));
                    index++;
                }
            }

            provider.SetSpriteRects(rects.ToArray());
            provider.GetDataProvider<ISpriteNameFileIdDataProvider>()?.SetNameFileIdPairs(pairs);
            provider.Apply();
            importer.SaveAndReimport();
        }
    }
}
