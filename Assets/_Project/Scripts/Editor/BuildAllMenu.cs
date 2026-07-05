using UnityEditor;
using UnityEngine;

namespace DuskWarung.EditorTools
{
    /// <summary>Runs the whole build pipeline in order, and prints the remaining manual steps.</summary>
    public static class BuildAllMenu
    {
        [MenuItem("Tools/Dusk Warung/Build Everything (0 → 10)", priority = 20)]
        public static void BuildEverything()
        {
            AssetConsolidationTool.Consolidate();
            AssetImportTool.Run();
            TileAssetTool.Run();
            DataAssetTool.Run();
            AnimatorTool.Run();
            PrefabTool.Run();
            FontAssetTool.Run();      // fonts before scenes, so UI text is crisp on first build
            DialogueUITool.Run();     // styled SayDialog prefab, before scenes spawn it
            SceneBuildTool.Run();
            FungusDialogueTool.Run();
            MapPaintTool.Run();
            AssetDatabase.SaveAssets();
            Debug.Log("[Dusk] ✔ Build Everything complete. See 'Tools ▸ Dusk Warung ▸ Help' for the remaining manual steps.");
        }

        [MenuItem("Tools/Dusk Warung/Help — remaining manual steps", priority = 21)]
        public static void Help()
        {
            Debug.Log(
                "[Dusk] Build Everything is a ONE-TIME BOOTSTRAP. It generates a complete first pass; from here " +
                "content is refined by hand in the Editor. See WORKFLOW.md for the full guide. Key points:\n" +
                "• Safe to re-run anytime: 0 Consolidate, 3 Data, 4 Animators, 5 Prefabs, 9 Rebuild Fonts, 10 Build Dialogue UI.\n" +
                "• Bootstrap ONCE then hand-edit (do NOT re-run — they overwrite scenes): 6 Build Scenes, " +
                "7 Seed Sample Dialogue. 8 Paint Greybox Map is a DEPRECATED throwaway pass — hand-paint instead.\n" +
                "DESIGNER — edit dialogue in Tools ▸ Fungus ▸ Flowchart Window (plain text; no <b>/<i> tags). Each Say " +
                "has a Character dropdown (Traveller/Bu Sari/Genderuwo) that drives the name plate + portrait; add new " +
                "speakers as Fungus Characters. Tune stats in _Project/Data/*.asset.\n" +
                "FONTS — if text looks doubled/blurry, run 9 Rebuild Fonts (regenerates m5x7/monogram as SDFAA); " +
                "fallback is Window ▸ TextMeshPro ▸ Font Asset Creator (padding 5, SDFAA) — see WORKFLOW.md.\n" +
                "LEVEL DESIGNER — paint the map by hand in Window ▸ 2D ▸ Tile Palette (tiles in _Project/Art/Tiles) " +
                "using .doc/map-blockout.png as the reference; add the warung/lamp/props; move actors, waypoints, grove trigger.\n" +
                "PLAYTEST — Title ▸ Overworld ▸ talk to Bu Sari ▸ cutscene ▸ Battle ▸ Victory ▸ End. " +
                "Then (optional) Cleanup ▸ delete vendor packs, and Build Windows x64.");
        }
    }
}
