using UnityEditor;
using UnityEngine;
using UnityEngine.Tilemaps;

namespace DuskWarung.EditorTools
{
    /// <summary>
    /// Turns the sliced tileset sheets into individual <see cref="Tile"/> assets (skipping fully
    /// transparent cells) so there is a ready palette to paint the overworld with. Run
    /// "1. Slice Tilesets" first.
    /// </summary>
    public static class TileAssetTool
    {
        private static readonly string[] Sheets =
        {
            "TilesetFloor", "TilesetNature", "TilesetHouse"
        };

        [MenuItem("Tools/Dusk Warung/2. Create Tile Assets", priority = 2)]
        public static void Run()
        {
            DuskEditorUtil.EnsureFolder(DuskEditorUtil.TilesDir);

            int total = 0;
            foreach (string sheet in Sheets)
            {
                string path = DuskEditorUtil.TilesheetsDir + "/" + sheet + ".png";
                if (AssetDatabase.LoadAssetAtPath<Texture2D>(path) == null)
                {
                    Debug.LogWarning($"[Dusk] tilesheet missing (skipped): {path}");
                    continue;
                }

                total += CreateTilesForSheet(path, sheet);
            }

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log($"[Dusk] Created {total} tile assets under {DuskEditorUtil.TilesDir}. " +
                      "To paint: Window ▸ 2D ▸ Tile Palette (drag a tilesheet in, or use these tiles).");
        }

        private static int CreateTilesForSheet(string pngPath, string sheet)
        {
            // Make the texture readable first so we can skip transparent cells. We do this BEFORE loading
            // the sprites (a reimport recreates the sub-sprites) and we do NOT restore afterwards — a second
            // reimport would invalidate the sprite references the tiles were just given.
            var importer = AssetImporter.GetAtPath(pngPath) as TextureImporter;
            if (importer != null && !importer.isReadable)
            {
                importer.isReadable = true;
                importer.SaveAndReimport();
            }

            var sprites = DuskEditorUtil.LoadSpritesRowMajor(pngPath);
            if (sprites.Count == 0)
            {
                Debug.LogWarning($"[Dusk] {sheet} isn't sliced yet — run '1. Slice Tilesets' first.");
                return 0;
            }

            string outFolder = DuskEditorUtil.TilesDir + "/" + sheet;
            DuskEditorUtil.EnsureFolder(outFolder);

            var texture = AssetDatabase.LoadAssetAtPath<Texture2D>(pngPath);
            int made = 0;
            foreach (Sprite sprite in sprites)
            {
                if (IsFullyTransparent(texture, sprite.rect))
                {
                    continue;
                }

                string tilePath = outFolder + "/" + sprite.name + ".asset";
                if (AssetDatabase.LoadAssetAtPath<Tile>(tilePath) != null)
                {
                    continue; // idempotent
                }

                var tile = ScriptableObject.CreateInstance<Tile>();
                tile.sprite = sprite;
                tile.colliderType = Tile.ColliderType.None;
                AssetDatabase.CreateAsset(tile, tilePath);
                made++;
            }

            return made;
        }

        private static bool IsFullyTransparent(Texture2D texture, Rect rect)
        {
            if (texture == null)
            {
                return false;
            }

            try
            {
                Color[] pixels = texture.GetPixels((int)rect.x, (int)rect.y, (int)rect.width, (int)rect.height);
                foreach (Color p in pixels)
                {
                    if (p.a > 0.01f)
                    {
                        return false;
                    }
                }

                return true;
            }
            catch
            {
                return false; // If unreadable, keep the tile rather than dropping it.
            }
        }
    }
}
