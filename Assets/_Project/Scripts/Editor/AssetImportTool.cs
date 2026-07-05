using UnityEditor;
using UnityEngine;

namespace DuskWarung.EditorTools
{
    /// <summary>
    /// Grid-slices the tileset sheets (imported but left un-sliced) into 16×16 tiles. The character
    /// sprites are already imported and sliced correctly, so they are left untouched.
    /// </summary>
    public static class AssetImportTool
    {
        private const int TileSize = 16;

        [MenuItem("Tools/Dusk Warung/1. Slice Tilesets", priority = 1)]
        public static void Run()
        {
            if (!AssetDatabase.IsValidFolder(DuskEditorUtil.TilesheetsDir))
            {
                Debug.LogWarning($"[Dusk] Tilesheets folder missing: {DuskEditorUtil.TilesheetsDir}");
                return;
            }

            int sliced = 0;
            foreach (string guid in AssetDatabase.FindAssets("t:Texture2D", new[] { DuskEditorUtil.TilesheetsDir }))
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                try
                {
                    DuskEditorUtil.GridSlice(path, TileSize, TileSize);
                    sliced++;
                }
                catch (System.Exception e)
                {
                    Debug.LogError($"[Dusk] Failed to slice {path}: {e.Message}\n" +
                                   "Slice it manually (Sprite Editor ▸ Slice ▸ Grid By Cell Size 16×16).");
                }
            }

            AssetDatabase.Refresh();
            Debug.Log($"[Dusk] Sliced {sliced} tileset sheet(s) into 16×16 tiles.");
        }
    }
}
