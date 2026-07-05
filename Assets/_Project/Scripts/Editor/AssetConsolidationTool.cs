using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace DuskWarung.EditorTools
{
    /// <summary>
    /// Copies the audio and UI assets the game actually uses into <c>_Project</c> (clean names),
    /// making the project self-contained so the bulky vendor packs can be deleted later. Also offers
    /// a separate, confirmed deletion of the Fungus demo scenes (bloat).
    /// </summary>
    public static class AssetConsolidationTool
    {
        private static readonly string[] Audio = { ".ogg", ".wav", ".mp3" };
        private static readonly string[] Images = { ".png" };

        [MenuItem("Tools/Dusk Warung/0. Consolidate Audio + UI into _Project", priority = 0)]
        public static void Consolidate()
        {
            DuskEditorUtil.EnsureOutputFolders();

            // Music — copied to stable, clean names the scene tool expects.
            CopyIfExists(DuskEditorUtil.VendorMusics + "/1 - Adventure Begin.ogg", DuskEditorUtil.AudioDir + "/BGM/Title.ogg");
            CopyIfExists(DuskEditorUtil.VendorMusics + "/26 - Lost Village.ogg", DuskEditorUtil.AudioDir + "/BGM/Overworld.ogg");
            CopyIfExists(DuskEditorUtil.VendorMusics + "/17 - Fight.ogg", DuskEditorUtil.AudioDir + "/BGM/Battle.ogg");

            // SFX — copy whole small folders (avoids depending on exact file names).
            CopyFolder(DuskEditorUtil.VendorSounds + "/Hit & Impact", DuskEditorUtil.AudioDir + "/SFX/Hit", Audio);
            CopyFolder(DuskEditorUtil.VendorSounds + "/Menu", DuskEditorUtil.AudioDir + "/SFX/Menu", Audio);
            CopyFolder(DuskEditorUtil.VendorSounds + "/Bonus", DuskEditorUtil.AudioDir + "/SFX/Bonus", Audio);

            // Kenney UI sprites (grey set: buttons/panels) — available for the user to swap into the HUD.
            CopyFolder(DuskEditorUtil.VendorKenneyUi + "/Grey/Default", DuskEditorUtil.UiDir, Images);

            // Battle backdrop.
            CopyIfExists(DuskEditorUtil.VendorBattleback, DuskEditorUtil.SpritesDir + "/battleback1.png");

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("[Dusk] Consolidation complete → _Project/Audio + _Project/Art/UI. (Vendor packs can be deleted later once everything works.)");
        }

        [MenuItem("Tools/Dusk Warung/Cleanup/Delete FungusExamples (demo bloat)", priority = 200)]
        public static void DeleteFungusExamples()
        {
            const string path = "Assets/FungusExamples";
            if (!AssetDatabase.IsValidFolder(path))
            {
                Debug.Log("[Dusk] FungusExamples already gone.");
                return;
            }

            if (!EditorUtility.DisplayDialog("Delete FungusExamples?",
                    "Permanently delete Assets/FungusExamples (Fungus demo scenes, ~243 files)? " +
                    "It is git-recoverable and not used by the game.", "Delete", "Cancel"))
            {
                return;
            }

            AssetDatabase.DeleteAsset(path);
            AssetDatabase.Refresh();
            Debug.Log("[Dusk] Deleted Assets/FungusExamples.");
        }

        private static void CopyIfExists(string src, string dst)
        {
            if (AssetDatabase.LoadAssetAtPath<Object>(src) == null)
            {
                Debug.LogWarning($"[Dusk] source missing (skipped): {src}");
                return;
            }

            if (AssetDatabase.LoadAssetAtPath<Object>(dst) != null)
            {
                return; // idempotent
            }

            DuskEditorUtil.EnsureFolder(Path.GetDirectoryName(dst).Replace('\\', '/'));
            AssetDatabase.CopyAsset(src, dst);
        }

        private static void CopyFolder(string srcFolder, string dstFolder, string[] extensions)
        {
            if (!AssetDatabase.IsValidFolder(srcFolder))
            {
                Debug.LogWarning($"[Dusk] source folder missing (skipped): {srcFolder}");
                return;
            }

            DuskEditorUtil.EnsureFolder(dstFolder);
            string absolute = Path.Combine(Directory.GetCurrentDirectory(), srcFolder);
            foreach (string file in Directory.GetFiles(absolute))
            {
                if (file.EndsWith(".meta"))
                {
                    continue;
                }

                string ext = Path.GetExtension(file).ToLowerInvariant();
                if (!extensions.Contains(ext))
                {
                    continue;
                }

                string name = Path.GetFileName(file);
                string dst = dstFolder + "/" + name;
                if (AssetDatabase.LoadAssetAtPath<Object>(dst) != null)
                {
                    continue; // idempotent
                }

                AssetDatabase.CopyAsset(srcFolder + "/" + name, dst);
            }
        }
    }
}
