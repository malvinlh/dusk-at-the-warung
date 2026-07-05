using System.Collections.Generic;
using System.IO;
using Cinemachine;
using DuskWarung.Battle;
using DuskWarung.Battle.View;
using DuskWarung.Core;
using DuskWarung.FungusCommands;
using DuskWarung.World;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;
using UnityEngine.Tilemaps;
using UnityEngine.UI;
using UnityEngine.EventSystems;

namespace DuskWarung.EditorTools
{
    /// <summary>
    /// Populates the four existing scenes (Title, Overworld, Battle, End) with cameras, lights, UI,
    /// prefab instances and fully-wired script references, then registers them in Build Settings.
    /// The result is a playable loop even before the Fungus dialogue is authored (missing blocks are
    /// skipped gracefully). Tile painting and aesthetic polish are left to the user.
    /// </summary>
    public static class SceneBuildTool
    {
        private static Sprite _uiSprite;
        private static TMP_FontAsset _fontBody;
        private static TMP_FontAsset _fontNumbers;

        [MenuItem("Tools/Dusk Warung/6. Build Scenes", priority = 6)]
        public static void Run()
        {
            _uiSprite = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/UISprite.psd");
            _fontBody = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(DuskEditorUtil.FontM5x7);
            _fontNumbers = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(DuskEditorUtil.FontMonogram);

            BuildTitle();
            BuildOverworld();
            BuildBattle();
            BuildEnd();

            AddToBuildSettings(new[]
            {
                DuskEditorUtil.ScenesDir + "/Title.unity",
                DuskEditorUtil.ScenesDir + "/Overworld.unity",
                DuskEditorUtil.ScenesDir + "/Battle.unity",
                DuskEditorUtil.ScenesDir + "/End.unity"
            });

            Debug.Log("[Dusk] Scenes built + registered in Build Settings. Author the Fungus dialogue and paint the map to finish.");
        }

        // ---------------------------------------------------------------- Title / End

        private static void BuildTitle()
        {
            Scene scene = OpenFresh(DuskEditorUtil.ScenesDir + "/Title.unity");
            MakeCamera(null);
            MakeEventSystem();
            SceneLoader loader = MakeFadeCanvas();

            Canvas canvas = MakeCanvas("TitleUI");
            AddText(canvas.transform, "Title", "Dusk at the Warung", _fontBody, 12f, TextAlignmentOptions.Center,
                new Vector2(0.5f, 0.62f), new Vector2(700, 120));
            TextMeshProUGUI prompt = AddText(canvas.transform, "Prompt", "Press Space to begin", _fontBody, 6f,
                TextAlignmentOptions.Center, new Vector2(0.5f, 0.32f), new Vector2(500, 60));

            var controllerGo = new GameObject("TitleController");
            var title = controllerGo.AddComponent<TitleScreenController>();
            DuskEditorUtil.WireObject(title, "loader", loader);
            DuskEditorUtil.WireString(title, "firstSceneName", "Overworld");
            DuskEditorUtil.WireObject(title, "pressStartPrompt", prompt.gameObject);

            MakeAudio(controllerGo, "Title");
            Save(scene);
        }

        private static void BuildEnd()
        {
            Scene scene = OpenFresh(DuskEditorUtil.ScenesDir + "/End.unity");
            MakeCamera(null);
            MakeEventSystem();
            SceneLoader loader = MakeFadeCanvas();

            Canvas canvas = MakeCanvas("EndUI");
            AddText(canvas.transform, "EndText",
                "You reached the next village at dusk's end.\nThe pisang goreng was, in fact, worth it.\n\n— The End (Vertical Slice) —",
                _fontBody, 7f, TextAlignmentOptions.Center, new Vector2(0.5f, 0.5f), new Vector2(760, 300));
            AddText(canvas.transform, "Prompt", "Press Space to return to the title", _fontBody, 5f,
                TextAlignmentOptions.Center, new Vector2(0.5f, 0.15f), new Vector2(600, 50));

            var controllerGo = new GameObject("EndController");
            var end = controllerGo.AddComponent<EndScreenController>();
            DuskEditorUtil.WireObject(end, "loader", loader);
            DuskEditorUtil.WireString(end, "titleSceneName", "Title");

            Save(scene);
        }

        // ---------------------------------------------------------------- Overworld

        private static void BuildOverworld()
        {
            Scene scene = OpenFresh(DuskEditorUtil.ScenesDir + "/Overworld.unity");
            MakeEventSystem();
            SceneLoader loader = MakeFadeCanvas();

            // Tilemap layers (empty — user paints; boundary colliders keep the player in).
            var grid = new GameObject("Grid").AddComponent<Grid>();
            MakeTilemap(grid.transform, "Ground", 0);
            MakeTilemap(grid.transform, "Props", 5);
            MakeBoundary(new Vector2(-9, -6), new Vector2(9, 6));

            // Lighting (dusk).
            MakeGlobalLight(new Color(0.55f, 0.5f, 0.65f), 0.85f);
            MakePointLight(new Vector3(-3f, -2f, 0f), new Color(1f, 0.75f, 0.4f), 3.5f, 1.4f); // warung lamp

            // Fungus.
            var flowchartGo = new GameObject("Flowchart");
            var flowchart = flowchartGo.AddComponent<Fungus.Flowchart>();
            var bridge = flowchartGo.AddComponent<FungusBridge>();
            DuskEditorUtil.WireObject(bridge, "flowchart", flowchart);

            // Actors.
            GameObject player = Spawn("World/Player", new Vector3(0f, -1f, 0f));
            GameObject buSari = Spawn("World/BuSari", new Vector3(-3f, -1.6f, 0f));
            var npc = buSari != null ? buSari.GetComponent<NpcInteractable>() : null;
            if (npc != null)
            {
                DuskEditorUtil.WireObject(npc, "fungus", bridge);
            }

            var playerMovement = player != null ? player.GetComponent<PlayerMovement>() : null;

            // Camera follows the player.
            MakeCamera(player != null ? player.transform : null);

            // Cutscene: walk from start toward the grove.
            var waypointA = MakePoint("Waypoint_PathMid", new Vector3(2f, 0.5f, 0f));
            var waypointB = MakePoint("Waypoint_GroveMouth", new Vector3(5f, 2.5f, 0f));
            var cutsceneGo = new GameObject("CutsceneDirector");
            var cutscene = cutsceneGo.AddComponent<CutsceneDirector>();
            DuskEditorUtil.WireObject(cutscene, "player", playerMovement);
            DuskEditorUtil.WireObject(cutscene, "fungus", bridge);
            WireCutsceneSteps(cutscene, waypointA.transform, waypointB.transform);

            // Encounter trigger at the grove mouth.
            var triggerGo = new GameObject("GroveEncounter");
            triggerGo.transform.position = new Vector3(5.5f, 3f, 0f);
            var triggerCol = triggerGo.AddComponent<BoxCollider2D>();
            triggerCol.isTrigger = true;
            triggerCol.size = new Vector2(2.5f, 1.5f);
            var encounterTrigger = triggerGo.AddComponent<EncounterTrigger>();
            DuskEditorUtil.WireObject(encounterTrigger, "encounter", LoadEncounter());
            DuskEditorUtil.WireObject(encounterTrigger, "loader", loader);

            // Return-from-battle handler.
            var benchSpawn = MakePoint("Spawn_Bench", new Vector3(-2.5f, -1.6f, 0f));
            var flowGo = new GameObject("OverworldFlow");
            var flow = flowGo.AddComponent<OverworldFlowController>();
            DuskEditorUtil.WireObject(flow, "playerTransform", player != null ? player.transform : null);
            DuskEditorUtil.WireObject(flow, "fungus", bridge);
            DuskEditorUtil.WireObject(flow, "defeatSpawn", benchSpawn.transform);

            MakeAudio(flowGo, "Overworld");
            Save(scene);
        }

        // ---------------------------------------------------------------- Battle

        private static void BuildBattle()
        {
            Scene scene = OpenFresh(DuskEditorUtil.ScenesDir + "/Battle.unity");
            MakeEventSystem();
            SceneLoader loader = MakeFadeCanvas();
            MakeCamera(null, out CinemachineVirtualCamera vcam);
            var listener = vcam.gameObject.AddComponent<CinemachineImpulseListener>();
            listener.m_Use2DDistance = true;

            // Backdrop.
            var bgGo = new GameObject("Background");
            var bgSr = bgGo.AddComponent<SpriteRenderer>();
            bgSr.sprite = AssetDatabase.LoadAssetAtPath<Sprite>(DuskEditorUtil.SpritesDir + "/battleback1.png");
            bgSr.sortingOrder = -100;
            FitBackground(bgSr);

            // Battlers.
            GameObject playerBattler = Spawn("Battle/PlayerBattler", new Vector3(-3f, -0.5f, 0f));
            GameObject enemyBattler = Spawn("Battle/EnemyBattler", new Vector3(3f, 0f, 0f));
            var playerView = playerBattler != null ? playerBattler.GetComponent<BattlerView>() : null;
            var enemyView = enemyBattler != null ? enemyBattler.GetComponent<BattlerView>() : null;

            // UI.
            BattleHUD hud = BuildHud();
            CommandMenuUI menu = BuildCommandMenu();
            var damagePopup = AssetDatabase.LoadAssetAtPath<DamagePopup>(DuskEditorUtil.PrefabsDir + "/Battle/DamagePopup.prefab");

            // Fungus.
            var flowchartGo = new GameObject("Flowchart");
            var flowchart = flowchartGo.AddComponent<Fungus.Flowchart>();
            var bridge = flowchartGo.AddComponent<FungusBridge>();
            DuskEditorUtil.WireObject(bridge, "flowchart", flowchart);

            // Controller — the hub that wires model ↔ views.
            var controllerGo = new GameObject("BattleController");
            var controller = controllerGo.AddComponent<BattleController>();
            MakeAudio(controllerGo, "Battle");
            var sfx = controllerGo.AddComponent<AudioSource>();
            sfx.playOnAwake = false;

            DuskEditorUtil.WireObject(controller, "debugEncounter", LoadEncounter());
            DuskEditorUtil.WireObject(controller, "playerView", playerView);
            DuskEditorUtil.WireObject(controller, "enemyView", enemyView);
            DuskEditorUtil.WireObject(controller, "backgroundRenderer", bgSr);
            DuskEditorUtil.WireObject(controller, "hud", hud);
            DuskEditorUtil.WireObject(controller, "commandMenu", menu);
            DuskEditorUtil.WireObject(controller, "damagePopupPrefab", damagePopup);
            DuskEditorUtil.WireObject(controller, "loader", loader);
            DuskEditorUtil.WireObject(controller, "fungus", bridge);
            DuskEditorUtil.WireObject(controller, "sfxSource", sfx);
            DuskEditorUtil.WireObject(controller, "hitClip", FirstClipIn(DuskEditorUtil.AudioDir + "/SFX/Hit"));
            DuskEditorUtil.WireObject(controller, "healClip", FirstClipIn(DuskEditorUtil.AudioDir + "/SFX/Bonus"));

            Save(scene);
        }

        private static BattleHUD BuildHud()
        {
            Canvas canvas = MakeCanvas("BattleHUD");
            var hud = canvas.gameObject.AddComponent<BattleHUD>();

            // Player panel (bottom-left).
            RectTransform playerPanel = Panel(canvas.transform, "PlayerPanel", new Vector2(0f, 0f), new Vector2(0f, 0f),
                new Vector2(0f, 0f), new Vector2(20, 20), new Vector2(300, 110));
            TextMeshProUGUI pName = AddChildText(playerPanel, "Name", "Traveller", _fontBody, 6f, new Vector2(10, -8), new Vector2(200, 24));
            Image pHp = Bar(playerPanel, "HpBar", new Color(0.3f, 0.85f, 0.35f), new Vector2(10, -34), new Vector2(280, 18));
            Image pMp = Bar(playerPanel, "MpBar", new Color(0.3f, 0.55f, 0.95f), new Vector2(10, -58), new Vector2(280, 14));
            TextMeshProUGUI pHpLabel = AddChildText(playerPanel, "HpLabel", "30/30", _fontNumbers, 5f, new Vector2(10, -78), new Vector2(140, 20));
            TextMeshProUGUI pMpLabel = AddChildText(playerPanel, "MpLabel", "10/10", _fontNumbers, 5f, new Vector2(160, -78), new Vector2(140, 20));

            // Enemy panel (top-right).
            RectTransform enemyPanel = Panel(canvas.transform, "EnemyPanel", new Vector2(1f, 1f), new Vector2(1f, 1f),
                new Vector2(1f, 1f), new Vector2(-20, -20), new Vector2(300, 70));
            TextMeshProUGUI eName = AddChildText(enemyPanel, "Name", "Genderuwo", _fontBody, 6f, new Vector2(10, -8), new Vector2(200, 24));
            Image eHp = Bar(enemyPanel, "HpBar", new Color(0.85f, 0.3f, 0.3f), new Vector2(10, -34), new Vector2(280, 18));

            DuskEditorUtil.WireObject(hud, "playerHpFill", pHp);
            DuskEditorUtil.WireObject(hud, "playerMpFill", pMp);
            DuskEditorUtil.WireObject(hud, "playerNameLabel", pName);
            DuskEditorUtil.WireObject(hud, "playerHpLabel", pHpLabel);
            DuskEditorUtil.WireObject(hud, "playerMpLabel", pMpLabel);
            DuskEditorUtil.WireObject(hud, "enemyHpFill", eHp);
            DuskEditorUtil.WireObject(hud, "enemyNameLabel", eName);
            return hud;
        }

        private static CommandMenuUI BuildCommandMenu()
        {
            Canvas canvas = MakeCanvas("CommandMenu");
            var menu = canvas.gameObject.AddComponent<CommandMenuUI>();

            RectTransform root = Panel(canvas.transform, "Root", new Vector2(1f, 0f), new Vector2(1f, 0f),
                new Vector2(1f, 0f), new Vector2(-20, 20), new Vector2(220, 210));

            Button attack = MenuButton(root, "AttackButton", "Attack", new Vector2(10, -10));
            Button skill = MenuButton(root, "SkillButton", "Skill", new Vector2(10, -60));
            Button item = MenuButton(root, "ItemButton", "Item", new Vector2(10, -110));
            Button run = MenuButton(root, "RunButton", "Run", new Vector2(10, -160));
            TextMeshProUGUI tooltip = AddChildText(root, "Tooltip", "", _fontBody, 4.5f, new Vector2(10, -200), new Vector2(200, 40));

            var sfx = canvas.gameObject.AddComponent<AudioSource>();
            sfx.playOnAwake = false;

            DuskEditorUtil.WireObject(menu, "root", root.gameObject);
            DuskEditorUtil.WireObject(menu, "attackButton", attack);
            DuskEditorUtil.WireObject(menu, "skillButton", skill);
            DuskEditorUtil.WireObject(menu, "itemButton", item);
            DuskEditorUtil.WireObject(menu, "runButton", run);
            DuskEditorUtil.WireObject(menu, "skillLabel", skill.GetComponentInChildren<TextMeshProUGUI>());
            DuskEditorUtil.WireObject(menu, "itemLabel", item.GetComponentInChildren<TextMeshProUGUI>());
            DuskEditorUtil.WireObject(menu, "tooltipLabel", tooltip);
            DuskEditorUtil.WireObject(menu, "sfxSource", sfx);
            DuskEditorUtil.WireObject(menu, "confirmClip", FirstClipIn(DuskEditorUtil.AudioDir + "/SFX/Menu"));
            DuskEditorUtil.WireObject(menu, "hoverClip", FirstClipIn(DuskEditorUtil.AudioDir + "/SFX/Menu"));
            return menu;
        }

        // ---------------------------------------------------------------- shared builders

        private static Scene OpenFresh(string path)
        {
            Scene scene = EditorSceneManager.OpenScene(path, OpenSceneMode.Single);
            foreach (GameObject root in scene.GetRootGameObjects())
            {
                Object.DestroyImmediate(root);
            }

            return scene;
        }

        private static void Save(Scene scene)
        {
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
        }

        private static void MakeCamera(Transform follow) => MakeCamera(follow, out _);

        private static void MakeCamera(Transform follow, out CinemachineVirtualCamera vcam)
        {
            var camGo = new GameObject("Main Camera") { tag = "MainCamera" };
            var cam = camGo.AddComponent<Camera>();
            cam.orthographic = true;
            cam.orthographicSize = 4.5f;
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = new Color(0.08f, 0.07f, 0.12f);
            camGo.AddComponent<AudioListener>();
            camGo.AddComponent<CinemachineBrain>();
            camGo.transform.position = new Vector3(0f, 0f, -10f);

            var vcamGo = new GameObject("VCam");
            vcam = vcamGo.AddComponent<CinemachineVirtualCamera>();
            vcam.m_Lens.Orthographic = true;
            vcam.m_Lens.OrthographicSize = 4.5f;
            if (follow != null)
            {
                vcam.Follow = follow;
                vcam.AddCinemachineComponent<CinemachineFramingTransposer>();
            }
            else
            {
                // Static view: keep the camera back from the sprite plane.
                vcamGo.transform.position = new Vector3(0f, 0f, -10f);
            }
        }

        private static void MakeEventSystem()
        {
            var go = new GameObject("EventSystem");
            go.AddComponent<EventSystem>();
            go.AddComponent<StandaloneInputModule>();
        }

        private static SceneLoader MakeFadeCanvas()
        {
            var canvasGo = new GameObject("FadeCanvas");
            var canvas = canvasGo.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 100;
            canvasGo.AddComponent<CanvasScaler>();
            canvasGo.AddComponent<GraphicRaycaster>();

            var imageGo = new GameObject("FadeImage", typeof(RectTransform));
            imageGo.transform.SetParent(canvasGo.transform, false);
            var image = imageGo.AddComponent<Image>();
            image.color = Color.black;
            Stretch(image.rectTransform);
            var group = imageGo.AddComponent<CanvasGroup>();
            group.alpha = 1f;

            var loader = canvasGo.AddComponent<SceneLoader>();
            DuskEditorUtil.WireObject(loader, "fade", group);
            return loader;
        }

        private static Canvas MakeCanvas(string name)
        {
            var go = new GameObject(name);
            var canvas = go.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            var scaler = go.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            go.AddComponent<GraphicRaycaster>();
            return canvas;
        }

        private static AudioManager MakeAudio(GameObject host, string bgmName)
        {
            var music = host.AddComponent<AudioSource>();
            music.playOnAwake = false;
            var audio = host.AddComponent<AudioManager>();
            DuskEditorUtil.WireObject(audio, "musicSource", music);
            var clip = FirstClipIn(DuskEditorUtil.AudioDir + "/BGM", bgmName);
            if (clip != null)
            {
                DuskEditorUtil.WireObject(audio, "sceneMusic", clip);
            }

            return audio;
        }

        private static Tilemap MakeTilemap(Transform parent, string name, int order)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            var tm = go.AddComponent<Tilemap>();
            var renderer = go.AddComponent<TilemapRenderer>();
            renderer.sortingOrder = order;
            return tm;
        }

        private static void MakeBoundary(Vector2 min, Vector2 max)
        {
            var go = new GameObject("Boundary");
            AddWall(go, new Vector2((min.x + max.x) / 2, max.y + 0.5f), new Vector2(max.x - min.x + 2, 1)); // top
            AddWall(go, new Vector2((min.x + max.x) / 2, min.y - 0.5f), new Vector2(max.x - min.x + 2, 1)); // bottom
            AddWall(go, new Vector2(min.x - 0.5f, (min.y + max.y) / 2), new Vector2(1, max.y - min.y + 2));  // left
            AddWall(go, new Vector2(max.x + 0.5f, (min.y + max.y) / 2), new Vector2(1, max.y - min.y + 2));  // right
        }

        private static void AddWall(GameObject parent, Vector2 center, Vector2 size)
        {
            var col = parent.AddComponent<BoxCollider2D>();
            col.offset = center;
            col.size = size;
        }

        private static void MakeGlobalLight(Color color, float intensity)
        {
            var go = new GameObject("GlobalLight2D");
            var light = go.AddComponent<Light2D>();
            light.lightType = Light2D.LightType.Global;
            light.color = color;
            light.intensity = intensity;
        }

        private static void MakePointLight(Vector3 pos, Color color, float radius, float intensity)
        {
            var go = new GameObject("WarungLamp");
            go.transform.position = pos;
            var light = go.AddComponent<Light2D>();
            light.lightType = Light2D.LightType.Point;
            light.color = color;
            light.pointLightOuterRadius = radius;
            light.intensity = intensity;
        }

        private static GameObject MakePoint(string name, Vector3 pos)
        {
            var go = new GameObject(name);
            go.transform.position = pos;
            return go;
        }

        private static void WireCutsceneSteps(CutsceneDirector cutscene, Transform a, Transform b)
        {
            var so = new SerializedObject(cutscene);
            SerializedProperty steps = so.FindProperty("steps");
            if (steps == null)
            {
                return;
            }

            steps.ClearArray();
            AddStep(steps, 0, a);   // MoveTo waypoint A
            AddStep(steps, 0, b);   // MoveTo waypoint B (grove mouth)
            AddFungusStep(steps, "GroveApproach");
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void AddStep(SerializedProperty steps, int kind, Transform target)
        {
            steps.InsertArrayElementAtIndex(steps.arraySize);
            SerializedProperty step = steps.GetArrayElementAtIndex(steps.arraySize - 1);
            step.FindPropertyRelative("kind").enumValueIndex = kind; // 0 = MoveTo
            step.FindPropertyRelative("target").objectReferenceValue = target;
            step.FindPropertyRelative("seconds").floatValue = 0f;
            step.FindPropertyRelative("blockName").stringValue = string.Empty;
        }

        private static void AddFungusStep(SerializedProperty steps, string block)
        {
            steps.InsertArrayElementAtIndex(steps.arraySize);
            SerializedProperty step = steps.GetArrayElementAtIndex(steps.arraySize - 1);
            step.FindPropertyRelative("kind").enumValueIndex = 2; // 2 = Fungus
            step.FindPropertyRelative("target").objectReferenceValue = null;
            step.FindPropertyRelative("seconds").floatValue = 0f;
            step.FindPropertyRelative("blockName").stringValue = block;
        }

        // ---------------------------------------------------------------- UI helpers

        private static RectTransform Panel(Transform parent, string name, Vector2 anchorMin, Vector2 anchorMax,
            Vector2 pivot, Vector2 anchoredPos, Vector2 size)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            var image = go.AddComponent<Image>();
            image.sprite = _uiSprite;
            image.type = Image.Type.Sliced;
            image.color = new Color(0f, 0f, 0f, 0.55f);
            RectTransform rt = image.rectTransform;
            rt.anchorMin = anchorMin;
            rt.anchorMax = anchorMax;
            rt.pivot = pivot;
            rt.anchoredPosition = anchoredPos;
            rt.sizeDelta = size;
            return rt;
        }

        private static Image Bar(Transform parent, string name, Color color, Vector2 anchoredPos, Vector2 size)
        {
            // background
            var bgGo = new GameObject(name + "_BG", typeof(RectTransform));
            bgGo.transform.SetParent(parent, false);
            var bg = bgGo.AddComponent<Image>();
            bg.sprite = _uiSprite;
            bg.type = Image.Type.Sliced;
            bg.color = new Color(0.1f, 0.1f, 0.1f, 0.9f);
            SetTopLeft(bg.rectTransform, anchoredPos, size);

            // fill
            var fillGo = new GameObject(name, typeof(RectTransform));
            fillGo.transform.SetParent(bgGo.transform, false);
            var fill = fillGo.AddComponent<Image>();
            fill.sprite = _uiSprite;
            fill.type = Image.Type.Filled;
            fill.fillMethod = Image.FillMethod.Horizontal;
            fill.fillOrigin = (int)Image.OriginHorizontal.Left;
            fill.color = color;
            fill.fillAmount = 1f;
            Stretch(fill.rectTransform);
            return fill;
        }

        private static Button MenuButton(Transform parent, string name, string label, Vector2 anchoredPos)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            var image = go.AddComponent<Image>();
            image.sprite = _uiSprite;
            image.type = Image.Type.Sliced;
            image.color = new Color(0.85f, 0.82f, 0.7f, 1f);
            SetTopLeft(image.rectTransform, anchoredPos, new Vector2(200, 40));
            var button = go.AddComponent<Button>();
            button.targetGraphic = image;

            var text = AddChildText(image.rectTransform, "Label", label, _fontBody, 6f, Vector2.zero, new Vector2(200, 40));
            text.color = Color.black;
            text.alignment = TextAlignmentOptions.Center;
            Stretch(text.rectTransform);
            return button;
        }

        private static TextMeshProUGUI AddText(Transform parent, string name, string text, TMP_FontAsset font,
            float size, TextAlignmentOptions align, Vector2 anchor, Vector2 sizeDelta)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            var tmp = go.AddComponent<TextMeshProUGUI>();
            tmp.text = text;
            tmp.fontSize = size * 6f; // canvas reference res is large, scale up
            tmp.alignment = align;
            tmp.enableWordWrapping = true;
            if (font != null)
            {
                tmp.font = font;
            }

            RectTransform rt = tmp.rectTransform;
            rt.anchorMin = anchor;
            rt.anchorMax = anchor;
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.anchoredPosition = Vector2.zero;
            rt.sizeDelta = sizeDelta;
            return tmp;
        }

        private static TextMeshProUGUI AddChildText(RectTransform parent, string name, string text, TMP_FontAsset font,
            float size, Vector2 topLeft, Vector2 sizeDelta)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            var tmp = go.AddComponent<TextMeshProUGUI>();
            tmp.text = text;
            tmp.fontSize = size * 6f;
            tmp.alignment = TextAlignmentOptions.Left;
            tmp.color = Color.white;
            if (font != null)
            {
                tmp.font = font;
            }

            SetTopLeft(tmp.rectTransform, topLeft, sizeDelta);
            return tmp;
        }

        private static void SetTopLeft(RectTransform rt, Vector2 topLeft, Vector2 size)
        {
            rt.anchorMin = new Vector2(0f, 1f);
            rt.anchorMax = new Vector2(0f, 1f);
            rt.pivot = new Vector2(0f, 1f);
            rt.anchoredPosition = topLeft;
            rt.sizeDelta = size;
        }

        private static void Stretch(RectTransform rt)
        {
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;
        }

        // ---------------------------------------------------------------- misc helpers

        private static GameObject Spawn(string prefabRelPath, Vector3 position)
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(DuskEditorUtil.PrefabsDir + "/" + prefabRelPath + ".prefab");
            if (prefab == null)
            {
                Debug.LogWarning($"[Dusk] prefab missing (run 'Build Prefabs' first): {prefabRelPath}");
                return null;
            }

            var instance = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
            instance.transform.position = position;
            return instance;
        }

        private static EncounterSO LoadEncounter()
            => AssetDatabase.LoadAssetAtPath<EncounterSO>(DuskEditorUtil.DataDir + "/Encounters/Encounter_GroveDusk.asset");

        private static AudioClip FirstClipIn(string folder, string preferredName = null)
        {
            if (!AssetDatabase.IsValidFolder(folder))
            {
                return null;
            }

            AudioClip fallback = null;
            foreach (string guid in AssetDatabase.FindAssets("t:AudioClip", new[] { folder }))
            {
                var clip = AssetDatabase.LoadAssetAtPath<AudioClip>(AssetDatabase.GUIDToAssetPath(guid));
                if (clip == null)
                {
                    continue;
                }

                if (preferredName != null && clip.name == preferredName)
                {
                    return clip;
                }

                fallback ??= clip;
            }

            return fallback;
        }

        private static void FitBackground(SpriteRenderer sr)
        {
            if (sr.sprite == null)
            {
                return;
            }

            // Scale the backdrop to roughly fill the 9×5 view.
            Vector2 spriteSize = sr.sprite.bounds.size;
            if (spriteSize.x > 0.01f && spriteSize.y > 0.01f)
            {
                sr.transform.localScale = new Vector3(18f / spriteSize.x, 10f / spriteSize.y, 1f);
            }
        }

        private static void AddToBuildSettings(string[] scenePaths)
        {
            var list = new List<EditorBuildSettingsScene>();
            foreach (string path in scenePaths)
            {
                if (File.Exists(path))
                {
                    list.Add(new EditorBuildSettingsScene(path, true));
                }
            }

            EditorBuildSettings.scenes = list.ToArray();
        }
    }
}
