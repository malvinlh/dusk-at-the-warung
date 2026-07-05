using System.Collections.Generic;
using DuskWarung.Battle;
using DuskWarung.FungusCommands;
using DuskWarung.World;
using Fungus;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace DuskWarung.EditorTools
{
    /// <summary>
    /// Programmatically authors the Fungus dialogue: creates the named Blocks in each scene's Flowchart,
    /// fills them with Say commands (text from the design doc) and the custom Dusk commands
    /// (LockPlayer / PlayCutscene / StartEncounter) wired to the scene objects. Idempotent — re-running
    /// removes and rebuilds the same blocks. (Note: it uses the default Fungus SayDialog; swap its font
    /// to m5x7 for the pixel look.)
    /// </summary>
    public static class FungusDialogueTool
    {
        // BOOTSTRAP tool: seeds sample dialogue into the Fungus flowcharts once. After this, dialogue is
        // the designer's job — edited visually in Tools ▸ Fungus ▸ Flowchart Window. Do not re-run after
        // hand-editing (it rebuilds the same blocks). Plain text only: manual TMP tags (<b>/<i>) break the
        // Fungus typewriter reveal, so emphasis is done with CAPS / the SayDialog style, never inline tags.
        [MenuItem("Tools/Dusk Warung/7. Seed Sample Dialogue", priority = 7)]
        public static void Run()
        {
            AuthorOverworld();
            AuthorBattle();
            Debug.Log("[Dusk] Seeded sample dialogue (Overworld: TalkToSari/GroveApproach/Defeat; Battle: BattleIntro/Victory). Edit it in the Fungus Flowchart Window from now on.");
        }

        private static void AuthorOverworld()
        {
            Scene scene = EditorSceneManager.OpenScene(DuskEditorUtil.ScenesDir + "/Overworld.unity", OpenSceneMode.Single);
            Flowchart fc = Object.FindObjectOfType<Flowchart>();
            if (fc == null)
            {
                Debug.LogWarning("[Dusk] Overworld: no Flowchart found — run 'Build Scenes' first.");
                return;
            }

            var player = Object.FindObjectOfType<PlayerMovement>();
            var cutscene = Object.FindObjectOfType<CutsceneDirector>();
            var loader = Object.FindObjectOfType<DuskWarung.Core.SceneLoader>();
            var encounter = AssetDatabase.LoadAssetAtPath<EncounterSO>(DuskEditorUtil.DataDir + "/Encounters/Encounter_GroveDusk.asset");

            Block talk = NewBlock(fc, "TalkToSari", new Vector2(80, 80));
            AddLockPlayer(fc, talk, player, true);
            AddSay(fc, talk, "Bu Sari:\nAh, a traveller! Sit, sit. Tea? Fried banana? I even have the good sambal today.");
            AddSay(fc, talk, "Traveller:\nJust directions, actually. I'm cutting through the grove to the next village.");
            AddSay(fc, talk, "Bu Sari:\nThe bamboo grove? At this hour? (She lowers her voice.) Anak muda, the genderuwo naps there. Wake it and it will want a chat. And it is... very bad at small talk.");
            AddSay(fc, talk, "Traveller:\nI'll be quick and quiet.");
            AddSay(fc, talk, "Bu Sari:\n(sighs) Everyone says that. Take this rice cracker at least. For luck. And for throwing.");
            AddPlayCutscene(fc, talk, cutscene);
            AddStartEncounter(fc, talk, encounter, loader);

            Block grove = NewBlock(fc, "GroveApproach", new Vector2(80, 300));
            AddSay(fc, grove, "(No input. The traveller strolls toward the dark tree line. A branch cracks. The light dims.)");
            AddSay(fc, grove, "Traveller:\n...That was definitely not the wind.");

            Block defeat = NewBlock(fc, "Defeat", new Vector2(80, 460));
            AddLockPlayer(fc, defeat, player, false);
            AddSay(fc, defeat, "You wake up on Bu Sari's bench, unharmed but embarrassed. 'Told you,' she says. Try again?");

            Save(fc, scene);
        }

        private static void AuthorBattle()
        {
            Scene scene = EditorSceneManager.OpenScene(DuskEditorUtil.ScenesDir + "/Battle.unity", OpenSceneMode.Single);
            Flowchart fc = Object.FindObjectOfType<Flowchart>();
            if (fc == null)
            {
                Debug.LogWarning("[Dusk] Battle: no Flowchart found — run 'Build Scenes' first.");
                return;
            }

            Block intro = NewBlock(fc, "BattleIntro", new Vector2(80, 80));
            AddSay(fc, intro, "Genderuwo:\nWHO DISTURBS THE NAP OF SHADOWS?");
            AddSay(fc, intro, "Traveller:\nSorry! I'll just squeeze past—");
            AddSay(fc, intro, "Genderuwo:\nNOBODY 'SQUEEZES PAST.' WE DUEL. IT'S TRADITION.");

            Block victory = NewBlock(fc, "Victory", new Vector2(80, 300));
            AddSay(fc, victory, "Genderuwo:\nA worthy duel. ...You may pass. And— (quieter) —tell Bu Sari her sambal is unmatched.");
            AddSay(fc, victory, "Traveller:\nI'll pass that along.");

            Save(fc, scene);
        }

        // ---- authoring helpers ----

        private static Block NewBlock(Flowchart fc, string blockName, Vector2 position)
        {
            Block existing = fc.FindBlock(blockName);
            if (existing != null)
            {
                foreach (Command cmd in new List<Command>(existing.CommandList))
                {
                    if (cmd != null)
                    {
                        Object.DestroyImmediate(cmd);
                    }
                }

                Object.DestroyImmediate(existing);
            }

            Block block = fc.CreateBlock(position);
            block.BlockName = blockName;
            block.hideFlags = HideFlags.HideInInspector;
            return block;
        }

        private static T AddCommand<T>(Flowchart fc, Block block) where T : Command
        {
            T cmd = fc.gameObject.AddComponent<T>();
            cmd.ItemId = fc.NextItemId();
            cmd.hideFlags = HideFlags.HideInInspector;
            block.CommandList.Add(cmd);
            return cmd;
        }

        private static void AddSay(Flowchart fc, Block block, string text)
        {
            Say say = AddCommand<Say>(fc, block);
            var so = new SerializedObject(say);
            so.FindProperty("storyText").stringValue = text;
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void AddLockPlayer(Flowchart fc, Block block, PlayerMovement player, bool locked)
        {
            LockPlayerCommand cmd = AddCommand<LockPlayerCommand>(fc, block);
            var so = new SerializedObject(cmd);
            so.FindProperty("player").objectReferenceValue = player;
            so.FindProperty("locked").boolValue = locked;
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void AddPlayCutscene(Flowchart fc, Block block, CutsceneDirector cutscene)
        {
            PlayCutsceneCommand cmd = AddCommand<PlayCutsceneCommand>(fc, block);
            var so = new SerializedObject(cmd);
            so.FindProperty("cutscene").objectReferenceValue = cutscene;
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void AddStartEncounter(Flowchart fc, Block block, EncounterSO encounter, DuskWarung.Core.SceneLoader loader)
        {
            StartEncounterCommand cmd = AddCommand<StartEncounterCommand>(fc, block);
            var so = new SerializedObject(cmd);
            so.FindProperty("encounter").objectReferenceValue = encounter;
            so.FindProperty("loader").objectReferenceValue = loader;
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void Save(Flowchart fc, Scene scene)
        {
            EditorUtility.SetDirty(fc);
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
        }
    }
}
