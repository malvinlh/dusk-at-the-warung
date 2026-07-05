using Cinemachine;
using DuskWarung.Battle.View;
using DuskWarung.Core;
using DuskWarung.World;
using TMPro;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;

namespace DuskWarung.EditorTools
{
    /// <summary>
    /// Builds the actor/object prefabs (Player, Bu Sari, the two battlers, the damage popup) with their
    /// components added and <c>[SerializeField]</c> references wired. Scene-specific UI (HUD, command
    /// menu, fade) is built by <see cref="SceneBuildTool"/>. Idempotent: overwrites existing prefabs.
    /// </summary>
    public static class PrefabTool
    {
        [MenuItem("Tools/Dusk Warung/5. Build Prefabs", priority = 5)]
        public static void Run()
        {
            DuskEditorUtil.EnsureFolder(DuskEditorUtil.PrefabsDir + "/World");
            DuskEditorUtil.EnsureFolder(DuskEditorUtil.PrefabsDir + "/Battle");

            BuildPlayer();
            BuildBuSari();
            BuildPlayerBattler();
            BuildEnemyBattler();
            BuildDamagePopup();

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("[Dusk] Prefabs built under _Project/Prefabs/{World,Battle}.");
        }

        private static void BuildPlayer()
        {
            var go = new GameObject("Player") { tag = "Player" };

            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = DuskEditorUtil.FirstSprite(DuskEditorUtil.PlayerSheet);
            sr.sortingOrder = 10;

            var rb = go.AddComponent<Rigidbody2D>();
            rb.gravityScale = 0f;
            rb.constraints = RigidbodyConstraints2D.FreezeRotation;
            rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
            rb.interpolation = RigidbodyInterpolation2D.Interpolate;

            var col = go.AddComponent<CapsuleCollider2D>();
            col.direction = CapsuleDirection2D.Vertical;
            col.size = new Vector2(0.5f, 0.45f);
            col.offset = new Vector2(0f, -0.1f);

            var animator = go.AddComponent<Animator>();
            animator.runtimeAnimatorController = Load<AnimatorController>(DuskEditorUtil.AnimatorsDir + "/AC_Character.controller");

            var input = go.AddComponent<InputReader>();
            var movement = go.AddComponent<PlayerMovement>();
            var interactor = go.AddComponent<PlayerInteractor>();

            DuskEditorUtil.WireObject(movement, "input", input);
            DuskEditorUtil.WireObject(movement, "animator", animator);
            DuskEditorUtil.WireObject(interactor, "input", input);
            DuskEditorUtil.WireObject(interactor, "movement", movement);

            Save(go, DuskEditorUtil.PrefabsDir + "/World/Player.prefab");
        }

        private static void BuildBuSari()
        {
            var go = new GameObject("BuSari");

            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = DuskEditorUtil.FirstSprite(DuskEditorUtil.OldWomanSheet);
            sr.sortingOrder = 10;

            var col = go.AddComponent<BoxCollider2D>();
            col.size = new Vector2(0.6f, 0.5f);
            col.offset = new Vector2(0f, -0.1f);

            var npc = go.AddComponent<NpcInteractable>();
            DuskEditorUtil.WireString(npc, "blockName", "TalkToSari");
            // 'fungus' is a scene reference — wired by SceneBuildTool.

            Save(go, DuskEditorUtil.PrefabsDir + "/World/BuSari.prefab");
        }

        private static void BuildPlayerBattler()
        {
            BuildBattler("PlayerBattler", DuskEditorUtil.PlayerSheet, "AC_PlayerBattler",
                tint: Color.white, scale: 1f);
        }

        private static void BuildEnemyBattler()
        {
            BuildBattler("EnemyBattler", DuskEditorUtil.EnemyIdle, "AC_EnemyBattler",
                tint: new Color(0.38f, 0.32f, 0.48f), scale: 1.5f); // dark shadow tint + looming size
        }

        private static void BuildBattler(string name, string spritePath, string controllerName, Color tint, float scale)
        {
            var go = new GameObject(name);
            go.transform.localScale = Vector3.one * scale;

            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = DuskEditorUtil.FirstSprite(spritePath);
            sr.color = tint;
            sr.sortingOrder = 5;
            var flash = Load<Material>(DuskEditorUtil.FlashMat);
            if (flash != null)
            {
                sr.sharedMaterial = flash;
            }

            var animator = go.AddComponent<Animator>();
            animator.runtimeAnimatorController = Load<AnimatorController>(DuskEditorUtil.AnimatorsDir + "/" + controllerName + ".controller");

            var impulse = go.AddComponent<CinemachineImpulseSource>();
            var view = go.AddComponent<BattlerView>();
            DuskEditorUtil.WireObject(view, "spriteRenderer", sr);
            DuskEditorUtil.WireObject(view, "animator", animator);
            DuskEditorUtil.WireObject(view, "impulseSource", impulse);

            Save(go, DuskEditorUtil.PrefabsDir + "/Battle/" + name + ".prefab");
        }

        private static void BuildDamagePopup()
        {
            var go = new GameObject("DamagePopup");
            go.transform.localScale = Vector3.one * 0.25f;

            var tmp = go.AddComponent<TextMeshPro>();
            tmp.text = "0";
            tmp.fontSize = 8;
            tmp.alignment = TextAlignmentOptions.Center;
            tmp.enableWordWrapping = false;
            var font = Load<TMP_FontAsset>(DuskEditorUtil.FontMonogram);
            if (font != null)
            {
                tmp.font = font;
            }

            var meshRenderer = go.GetComponent<MeshRenderer>();
            if (meshRenderer != null)
            {
                meshRenderer.sortingOrder = 100;
            }

            var popup = go.AddComponent<DamagePopup>();
            DuskEditorUtil.WireObject(popup, "label", tmp);

            Save(go, DuskEditorUtil.PrefabsDir + "/Battle/DamagePopup.prefab");
        }

        private static T Load<T>(string path) where T : Object => AssetDatabase.LoadAssetAtPath<T>(path);

        private static void Save(GameObject go, string prefabPath)
        {
            PrefabUtility.SaveAsPrefabAsset(go, prefabPath);
            Object.DestroyImmediate(go);
        }
    }
}
