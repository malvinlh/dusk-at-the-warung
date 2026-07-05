using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Tilemaps;

namespace DuskWarung.EditorTools
{
    /// <summary>
    /// Paints a functional dusk-village blockout into the Overworld. Rather than guessing tile indices
    /// in the autotile sheets, it picks tiles by COLOUR SAMPLING (best solid grass / dirt from the Floor
    /// sheet, best foliage from the Nature sheet), then fills the ground with grass, draws a dirt path
    /// from the warung to the grove, and lays a foliage band at the grove edge on the Props layer. Polish
    /// (warung building, lamp, flowers) is left to the user with the full palette. Idempotent — repaints.
    /// </summary>
    public static class MapPaintTool
    {
        private const string FloorSheet = DuskEditorUtil.TilesheetsDir + "/TilesetFloor.png";
        private const string NatureSheet = DuskEditorUtil.TilesheetsDir + "/TilesetNature.png";

        private enum Target { Grass, Dirt, Foliage }

        [MenuItem("Tools/Dusk Warung/8. Paint Greybox Map (deprecated bootstrap)", priority = 8)]
        public static void Run()
        {
            // DEPRECATED BOOTSTRAP. The map is HAND-PAINTED by the level designer in the Tile Palette — that
            // is the source of truth (Overworld builds with empty tilemaps ON PURPOSE). This one-shot only
            // stamps a rough grass/path/grove greybox, and ANY later 'Build Scenes'/'Seed Dialogue' re-save
            // wipes it, so its result is not durable. Prefer hand-painting to .doc/map-blockout.png; see WORKFLOW.md.
            Tile grass = PickTile(FloorSheet, Target.Grass, opacityWeight: 0.5f, variancePenalty: 3f);
            Tile dirt = PickTile(FloorSheet, Target.Dirt, opacityWeight: 0.5f, variancePenalty: 3f);
            Tile foliage = PickTile(NatureSheet, Target.Foliage, opacityWeight: 0.3f, variancePenalty: 0.5f);

            Scene scene = EditorSceneManager.OpenScene(DuskEditorUtil.ScenesDir + "/Overworld.unity", OpenSceneMode.Single);
            Tilemap ground = FindTilemap("Ground");
            Tilemap props = FindTilemap("Props");
            if (ground == null)
            {
                Debug.LogWarning("[Dusk] Overworld has no 'Ground' tilemap — run 'Build Scenes' first.");
                return;
            }

            Paint(ground, props, grass, dirt, foliage);

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            Debug.Log("[Dusk] Greybox stamped (grass + dirt path + grove foliage). This is a throwaway first pass — " +
                      "hand-paint the real map in the Tile Palette (see .doc/map-blockout.png). Any scene rebuild wipes this.");
        }

        private static void Paint(Tilemap ground, Tilemap props, Tile grass, Tile dirt, Tile foliage)
        {
            const int minX = -9, maxX = 9, minY = -6, maxY = 6;

            ground.ClearAllTiles();
            if (props != null)
            {
                props.ClearAllTiles();
            }

            if (grass != null)
            {
                for (int x = minX; x <= maxX; x++)
                {
                    for (int y = minY; y <= maxY; y++)
                    {
                        ground.SetTile(new Vector3Int(x, y, 0), grass);
                    }
                }
            }

            if (dirt != null)
            {
                for (int x = 0; x <= 5; x++)
                {
                    ground.SetTile(new Vector3Int(x, -1, 0), dirt);
                }

                for (int y = -1; y <= 3; y++)
                {
                    ground.SetTile(new Vector3Int(5, y, 0), dirt);
                }
            }

            if (foliage != null)
            {
                Tilemap layer = props != null ? props : ground;

                // Grove cluster (top-right) + a tree line along the top and right edges.
                for (int x = 4; x <= maxX; x++)
                {
                    for (int y = 3; y <= maxY; y++)
                    {
                        layer.SetTile(new Vector3Int(x, y, 0), foliage);
                    }
                }

                for (int x = minX; x <= maxX; x++)
                {
                    layer.SetTile(new Vector3Int(x, maxY, 0), foliage);
                }

                for (int y = minY; y <= maxY; y++)
                {
                    layer.SetTile(new Vector3Int(minX, y, 0), foliage);
                }
            }
        }

        // ---- colour-sampling tile picker ----

        private static Tile PickTile(string sheetPath, Target target, float opacityWeight, float variancePenalty)
        {
            var importer = AssetImporter.GetAtPath(sheetPath) as TextureImporter;
            if (importer == null)
            {
                Debug.LogWarning($"[Dusk] tilesheet missing (skipped): {sheetPath}");
                return null;
            }

            if (!importer.isReadable)
            {
                importer.isReadable = true;
                importer.SaveAndReimport();
            }

            var sprites = DuskEditorUtil.LoadSpritesRowMajor(sheetPath);
            var texture = AssetDatabase.LoadAssetAtPath<Texture2D>(sheetPath);
            if (sprites.Count == 0 || texture == null)
            {
                Debug.LogWarning($"[Dusk] {sheetPath} not sliced — run 'Slice Tilesets' first.");
                return null;
            }

            // Soft scoring (no hard opacity gate): colour match, plus a bonus for solid tiles and a penalty
            // for busy ones. Only fully-empty cells are skipped, so a best match is ALWAYS returned.
            Sprite best = null;
            float bestScore = float.MinValue;
            foreach (Sprite sprite in sprites)
            {
                Analyze(texture, sprite.rect, out float opacity, out Color avg, out float variance);
                if (opacity < 0.25f)
                {
                    continue; // near-empty cell
                }

                float score = ColourScore(target, avg) + opacity * opacityWeight - variance * variancePenalty;
                if (score > bestScore)
                {
                    bestScore = score;
                    best = sprite;
                }
            }

            if (best == null)
            {
                Debug.LogWarning($"[Dusk] No non-empty tiles in {System.IO.Path.GetFileName(sheetPath)} for {target}.");
                return null;
            }

            Debug.Log($"[Dusk] {target} → {best.name} (score {bestScore:0.00}).");
            return CreateTile(best, target);
        }

        private static float ColourScore(Target target, Color c)
        {
            switch (target)
            {
                case Target.Grass:
                case Target.Foliage:
                    return c.g - 0.6f * c.r - 0.6f * c.b; // green-dominant
                case Target.Dirt:
                    return (c.r - c.g) * 1.5f + (c.r + c.g) * 0.2f - c.b; // warm brown, red > green
                default:
                    return 0f;
            }
        }

        private static void Analyze(Texture2D texture, Rect rect, out float opacity, out Color avg, out float variance)
        {
            opacity = 0f;
            avg = Color.black;
            variance = 999f;

            Color[] pixels;
            try
            {
                pixels = texture.GetPixels((int)rect.x, (int)rect.y, (int)rect.width, (int)rect.height);
            }
            catch
            {
                return;
            }

            int opaque = 0;
            Color sum = Color.black;
            foreach (Color p in pixels)
            {
                if (p.a > 0.5f)
                {
                    opaque++;
                    sum += p;
                }
            }

            if (opaque == 0)
            {
                return;
            }

            avg = sum / opaque;
            float v = 0f;
            foreach (Color p in pixels)
            {
                if (p.a > 0.5f)
                {
                    v += (p.r - avg.r) * (p.r - avg.r) + (p.g - avg.g) * (p.g - avg.g) + (p.b - avg.b) * (p.b - avg.b);
                }
            }

            variance = v / opaque;
            opacity = (float)opaque / pixels.Length;
        }

        private static Tile CreateTile(Sprite sprite, Target target)
        {
            string folder = DuskEditorUtil.TilesDir + "/Map";
            DuskEditorUtil.EnsureFolder(folder);
            string path = $"{folder}/{target}_{sprite.name}.asset";

            var existing = AssetDatabase.LoadAssetAtPath<Tile>(path);
            if (existing != null)
            {
                return existing;
            }

            var tile = ScriptableObject.CreateInstance<Tile>();
            tile.sprite = sprite;
            tile.colliderType = Tile.ColliderType.None;
            AssetDatabase.CreateAsset(tile, path);
            return tile;
        }

        private static Tilemap FindTilemap(string tilemapName)
        {
            foreach (Tilemap tm in Object.FindObjectsOfType<Tilemap>())
            {
                if (tm.name == tilemapName)
                {
                    return tm;
                }
            }

            return null;
        }
    }
}
