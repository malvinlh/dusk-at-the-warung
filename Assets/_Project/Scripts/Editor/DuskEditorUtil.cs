using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.U2D.Sprites;
using UnityEngine;

namespace DuskWarung.EditorTools
{
    /// <summary>
    /// Shared helpers and path constants for the "Dusk Warung" Editor build tools. Everything here
    /// is Editor-only (this file lives under an <c>Editor/</c> folder → compiled into
    /// Assembly-CSharp-Editor, which can see the gameplay MonoBehaviours and all packages).
    /// </summary>
    public static class DuskEditorUtil
    {
        // --- Authored (_Project) paths ---
        public const string Root = "Assets/_Project";
        public const string Art = Root + "/Art";
        public const string SpritesDir = Art + "/Sprites";
        public const string FontsDir = Art + "/Fonts";
        public const string TilesheetsDir = SpritesDir + "/Tilesheets";
        public const string DataDir = Root + "/Data";
        public const string PrefabsDir = Root + "/Prefabs";
        public const string ScenesDir = Root + "/Scenes";
        public const string AnimatorsDir = Art + "/Animators";
        public const string AnimationsDir = Art + "/Animations";
        public const string TilesDir = Art + "/Tiles";
        public const string AudioDir = Root + "/Audio";
        public const string UiDir = Art + "/UI";
        public const string VfxDir = Art + "/VFX";

        // --- Key authored assets ---
        public const string PlayerSheet = SpritesDir + "/Player/SpriteSheet.png";
        public const string PlayerFaceset = SpritesDir + "/Player/Faceset.png";
        public const string OldWomanSheet = SpritesDir + "/NPC_OldWoman/SpriteSheet.png";
        public const string OldWomanFaceset = SpritesDir + "/NPC_OldWoman/Faceset.png";
        public const string EnemyIdle = SpritesDir + "/Enemy_Genderuwo/Idle.png";
        public const string EnemyAttack = SpritesDir + "/Enemy_Genderuwo/Attack.png";
        public const string FlashMat = VfxDir + "/Flash.mat";
        public const string FontM5x7 = FontsDir + "/m5x7.asset";
        public const string FontMonogram = FontsDir + "/monogram.asset";

        // --- Vendor sources used by the consolidation tool (copied into _Project) ---
        public const string VendorMusics = "Assets/Sprites/ninja-adventure/Audio/Musics";
        public const string VendorSounds = "Assets/Sprites/ninja-adventure/Audio/Sounds";
        public const string VendorKenneySfx = "Assets/Sounds/kenney-interface-sounds";
        public const string VendorKenneyUi = "Assets/Sprites/kenney-ui-pack/PNG";
        public const string VendorBattleback = "Assets/Sprites/battle-background/battleback1.png";

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

        /// <summary>Ensures all of the tool output folders exist.</summary>
        public static void EnsureOutputFolders()
        {
            foreach (string f in new[]
            {
                Art, SpritesDir, FontsDir, DataDir, PrefabsDir, PrefabsDir + "/World",
                PrefabsDir + "/Battle", PrefabsDir + "/UI", AnimatorsDir, AnimationsDir,
                TilesDir, AudioDir, AudioDir + "/BGM", AudioDir + "/SFX", UiDir, VfxDir,
                DataDir + "/Skills", DataDir + "/Items", DataDir + "/Battlers", DataDir + "/Encounters"
            })
            {
                EnsureFolder(f);
            }
        }

        /// <summary>
        /// Loads all sliced sub-sprites of a sheet ordered top-to-bottom, then left-to-right
        /// (natural reading order), so frame indices are stable for animation authoring.
        /// </summary>
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

        /// <summary>Returns the first sub-sprite of a sheet (or the single sprite), or null.</summary>
        public static Sprite FirstSprite(string pngPath)
        {
            List<Sprite> all = LoadSpritesRowMajor(pngPath);
            if (all.Count > 0)
            {
                return all[0];
            }

            return AssetDatabase.LoadAssetAtPath<Sprite>(pngPath);
        }

        /// <summary>Loads (or creates + saves) a ScriptableObject asset of type T at the given path.</summary>
        public static T LoadOrCreateSO<T>(string assetPath) where T : ScriptableObject
        {
            var existing = AssetDatabase.LoadAssetAtPath<T>(assetPath);
            if (existing != null)
            {
                return existing;
            }

            EnsureFolder(Path.GetDirectoryName(assetPath).Replace('\\', '/'));
            var created = ScriptableObject.CreateInstance<T>();
            AssetDatabase.CreateAsset(created, assetPath);
            return created;
        }

        /// <summary>
        /// Grid-slices a texture into cellW×cellH sprites (top row first, left to right) using the
        /// supported sprite data provider API. Safe to call on an already-sliced sheet (re-slices it).
        /// </summary>
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

        // --- Serialized-field wiring (for private [SerializeField] fields on components) ---

        public static void WireObject(Object target, string field, Object value)
        {
            SerializedProperty prop = Prop(target, field, out SerializedObject so);
            if (prop == null)
            {
                return;
            }

            prop.objectReferenceValue = value;
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        public static void WireFloat(Object target, string field, float value)
        {
            SerializedProperty prop = Prop(target, field, out SerializedObject so);
            if (prop == null)
            {
                return;
            }

            prop.floatValue = value;
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        public static void WireString(Object target, string field, string value)
        {
            SerializedProperty prop = Prop(target, field, out SerializedObject so);
            if (prop == null)
            {
                return;
            }

            prop.stringValue = value;
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        public static void WireBool(Object target, string field, bool value)
        {
            SerializedProperty prop = Prop(target, field, out SerializedObject so);
            if (prop == null)
            {
                return;
            }

            prop.boolValue = value;
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        private static SerializedProperty Prop(Object target, string field, out SerializedObject so)
        {
            so = new SerializedObject(target);
            SerializedProperty prop = so.FindProperty(field);
            if (prop == null)
            {
                Debug.LogWarning($"[Dusk] Serialized field '{field}' not found on {target.GetType().Name} ({target.name}).");
            }

            return prop;
        }
    }
}
