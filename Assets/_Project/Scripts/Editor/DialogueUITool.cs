using Fungus;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace DuskWarung.EditorTools
{
    /// <summary>
    /// Builds the project's styled dialogue skins: restyled Fungus <c>SayDialog</c> and <c>MenuDialog</c>
    /// prefabs saved under <c>_Project/Prefabs/UI/</c>. These are the programmer-owned dialogue *skin* — dark
    /// panels, the m5x7 pixel font, a prominent portrait — while lines, character assignment, and choice
    /// branching stay the designer's job (Fungus <c>Character</c> assets, Say and Menu commands). Fungus feeds
    /// each speaker's portrait into the SayDialog's <c>characterImage</c> automatically.
    ///
    /// Restyling follows each component's OWN serialized references where possible, so it does not depend on
    /// Fungus's internal child names.
    /// </summary>
    public static class DialogueUITool
    {
        private const string FungusSayDialog = "Assets/Fungus/Resources/Prefabs/SayDialog.prefab";
        private const string FungusMenuDialog = "Assets/Fungus/Resources/Prefabs/MenuDialog.prefab";
        private const string PixelFontTtf = "Assets/Fonts/m5x7/m5x7.ttf";

        private static string SayDest => DuskEditorUtil.PrefabsDir + "/UI/SayDialog.prefab";
        private static string MenuDest => DuskEditorUtil.PrefabsDir + "/UI/MenuDialog.prefab";
        private static readonly Color PanelColor = new Color(0.06f, 0.06f, 0.09f, 0.92f);
        private static readonly Color TextColor = new Color(0.96f, 0.94f, 0.88f);

        [MenuItem("Tools/Dusk Warung/10. Build Dialogue UI", priority = 10)]
        public static void Run()
        {
            DuskEditorUtil.EnsureFolder(DuskEditorUtil.PrefabsDir + "/UI");
            bool say = BuildSayDialog();
            bool menu = BuildMenuDialog();
            AssetDatabase.SaveAssets();
            Debug.Log($"[Dusk] Dialogue UI built (SayDialog:{say}, MenuDialog:{menu}). Scenes spawn them; Characters feed the portraits.");
        }

        private static bool BuildSayDialog()
        {
            GameObject instance = Instantiate(FungusSayDialog);
            if (instance == null)
            {
                return false;
            }

            SwapFontsAndRecolor(instance);

            // SayDialog-specific: name-plate accent + a prominent portrait (Fungus reflows text around it).
            var say = instance.GetComponentInChildren<SayDialog>(true);
            if (say != null)
            {
                var so = new SerializedObject(say);
                if (so.FindProperty("nameText").objectReferenceValue is Text nameText)
                {
                    nameText.color = new Color(1f, 0.86f, 0.5f);
                    nameText.fontStyle = FontStyle.Bold;
                }

                if (so.FindProperty("characterImage").objectReferenceValue is Image portrait)
                {
                    portrait.preserveAspect = true;
                    portrait.rectTransform.sizeDelta = new Vector2(180f, 180f);
                }
            }

            Save(instance, SayDest);
            return true;
        }

        private static bool BuildMenuDialog()
        {
            GameObject instance = Instantiate(FungusMenuDialog);
            if (instance == null)
            {
                return false;
            }

            SwapFontsAndRecolor(instance);
            Save(instance, MenuDest);
            return true;
        }

        // ---- shared helpers ----

        private static GameObject Instantiate(string sourcePath)
        {
            var source = AssetDatabase.LoadAssetAtPath<GameObject>(sourcePath);
            if (source == null)
            {
                Debug.LogWarning($"[Dusk] Fungus prefab not found at {sourcePath} — skipped.");
                return null;
            }

            var instance = (GameObject)PrefabUtility.InstantiatePrefab(source);
            PrefabUtility.UnpackPrefabInstance(instance, PrefabUnpackMode.Completely, InteractionMode.AutomatedAction);
            return instance;
        }

        private static void Save(GameObject instance, string destPath)
        {
            PrefabUtility.SaveAsPrefabAsset(instance, destPath);
            Object.DestroyImmediate(instance);
        }

        /// <summary>Swaps every Text to the m5x7 pixel font + a readable colour, and recolours the panel.</summary>
        private static void SwapFontsAndRecolor(GameObject instance)
        {
            Font pixelFont = AssetDatabase.LoadAssetAtPath<Font>(PixelFontTtf);
            foreach (Text text in instance.GetComponentsInChildren<Text>(true))
            {
                if (pixelFont != null)
                {
                    text.font = pixelFont;
                }

                text.color = TextColor;
                text.fontSize = Mathf.Max(text.fontSize, 28);
            }

            RecolorPanel(instance);
        }

        /// <summary>Recolours the dialog's background panel (best-effort: the largest full-rect Image).</summary>
        private static void RecolorPanel(GameObject instance)
        {
            Image panel = null;
            float bestArea = 0f;
            foreach (Image img in instance.GetComponentsInChildren<Image>(true))
            {
                // Skip small icons / portraits; the panel stretches to fill, so it wins on area.
                Vector2 size = img.rectTransform.rect.size;
                float area = Mathf.Abs(size.x * size.y);
                if (area >= bestArea)
                {
                    bestArea = area;
                    panel = img;
                }
            }

            if (panel != null)
            {
                panel.color = PanelColor;
            }
        }
    }
}
