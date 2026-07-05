using UnityEditor;
using UnityEngine;

namespace DuskWarung.EditorTools
{
    /// <summary>Runs the whole build pipeline in order, and prints the remaining manual steps.</summary>
    public static class BuildAllMenu
    {
        [MenuItem("Tools/Dusk Warung/Build Everything (0 → 6)", priority = 20)]
        public static void BuildEverything()
        {
            AssetConsolidationTool.Consolidate();
            AssetImportTool.Run();
            TileAssetTool.Run();
            DataAssetTool.Run();
            AnimatorTool.Run();
            PrefabTool.Run();
            SceneBuildTool.Run();
            AssetDatabase.SaveAssets();
            Debug.Log("[Dusk] ✔ Build Everything complete. See 'Tools ▸ Dusk Warung ▸ Help' for the remaining manual steps.");
        }

        [MenuItem("Tools/Dusk Warung/Help — remaining manual steps", priority = 21)]
        public static void Help()
        {
            Debug.Log(
                "[Dusk] Remaining MANUAL steps after Build Everything:\n" +
                "1. FUNGUS DIALOGUE — in each scene's Flowchart, author the Blocks (Overworld: TalkToSari, " +
                "GroveApproach; Battle: BattleIntro, Victory; Overworld return: Defeat) using Say commands " +
                "(lines in the design doc §10); assign the m5x7 TMP font on the SayDialog.\n" +
                "2. MAP — paint the Overworld ground/props (Window ▸ 2D ▸ Tile Palette; tiles are under _Project/Art/Tiles).\n" +
                "3. CHECK — verify the avatar faces the right way when walking (else tell me to flip the Walk row order); " +
                "tune battler positions / enemy tint.\n" +
                "4. PLAYTEST — Title ▸ Overworld ▸ (walk to grove) ▸ Battle ▸ Victory ▸ End. The loop works even before dialogue is written.\n" +
                "5. (Optional) delete unused vendor packs (Tools ▸ Dusk Warung ▸ Cleanup), then Build Windows x64.");
        }
    }
}
