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
    /// (LockPlayer / SetFlag / PlayCutscene / StartEncounter) wired to the scene objects. Each Say is
    /// assigned its Fungus <c>Character</c> (created by <see cref="SceneBuildTool"/>) so the styled dialog
    /// shows a name plate + portrait and the lines stay clean text. Idempotent — re-running removes and
    /// rebuilds the same blocks.
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

            Character sari = FindCharacter("Bu Sari");
            Character traveller = FindCharacter("Traveller");

            // A branching conversation: a cosmetic player choice (both options converge on the same tail),
            // authored as designer-facing Fungus Menu + Call commands. Create the blocks first so the Menu
            // and Call commands can reference them.
            Block talk = NewBlock(fc, "TalkToSari", new Vector2(80, 80));
            Block whyNot = NewBlock(fc, "SariWhyNot", new Vector2(400, 40));
            Block fine = NewBlock(fc, "SariFine", new Vector2(400, 180));
            Block wrapUp = NewBlock(fc, "SariWrapUp", new Vector2(400, 320));

            AddLockPlayer(fc, talk, player, true);
            AddSay(fc, talk, sari, "Ah, a traveller! Sit, sit. Tea? Fried banana? I even have the good sambal today.");
            AddSay(fc, talk, traveller, "Just directions, actually. I'm cutting through the grove to the next village.");
            AddSay(fc, talk, sari, "The bamboo grove? At this hour? (She lowers her voice.) Anak muda, the genderuwo naps there.");
            AddMenu(fc, talk, "Why not?", whyNot);       // the two choices — same outcome, different flavour
            AddMenu(fc, talk, "I'll be fine.", fine);

            AddSay(fc, whyNot, sari, "Because the old ones say something waits there. Big. Hairy. And very bad at small talk.");
            AddCall(fc, whyNot, wrapUp);

            AddSay(fc, fine, sari, "That's what the last three said. Nice folks. Never wrote back.");
            AddCall(fc, fine, wrapUp);

            AddSay(fc, wrapUp, sari, "(sighs) Take this rice cracker at least. For luck. And for throwing.");
            AddSetFlag(fc, wrapUp, "met_bu_sari"); // unlocks the grove encounter gate
            AddPlayCutscene(fc, wrapUp, cutscene);
            AddStartEncounter(fc, wrapUp, encounter, loader);

            Block grove = NewBlock(fc, "GroveApproach", new Vector2(80, 300));
            AddSay(fc, grove, null, "(No input. The traveller strolls toward the dark tree line. A branch cracks. The light dims.)");
            AddSay(fc, grove, traveller, "...That was definitely not the wind.");

            // Nudge shown if the player reaches the grove BEFORE talking to Bu Sari (gate not yet open).
            Block gatedHint = NewBlock(fc, "GroveGatedHint", new Vector2(80, 620));
            AddSay(fc, gatedHint, traveller, "That grove looks pitch black. Maybe I should ask Bu Sari about it first.");

            Block defeat = NewBlock(fc, "Defeat", new Vector2(80, 460));
            AddLockPlayer(fc, defeat, player, false);
            AddSay(fc, defeat, null, "You wake up on Bu Sari's bench, unharmed but embarrassed. 'Told you,' she says. Try again?");

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

            Character genderuwo = FindCharacter("Genderuwo");
            Character traveller = FindCharacter("Traveller");

            Block intro = NewBlock(fc, "BattleIntro", new Vector2(80, 80));
            AddSay(fc, intro, genderuwo, "WHO DISTURBS THE NAP OF SHADOWS?");
            AddSay(fc, intro, traveller, "Sorry! I'll just squeeze past—");
            AddSay(fc, intro, genderuwo, "NOBODY 'SQUEEZES PAST.' WE DUEL. IT'S TRADITION.");

            Block victory = NewBlock(fc, "Victory", new Vector2(80, 300));
            AddSay(fc, victory, genderuwo, "A worthy duel. ...You may pass. And— (quieter) —tell Bu Sari her sambal is unmatched.");
            AddSay(fc, victory, traveller, "I'll pass that along.");

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

        private static void AddSay(Flowchart fc, Block block, Character speaker, string text)
        {
            Say say = AddCommand<Say>(fc, block);
            var so = new SerializedObject(say);
            so.FindProperty("storyText").stringValue = text;
            if (speaker != null)
            {
                // Assign the speaker: Fungus shows its name plate + portrait, so the line stays clean text.
                so.FindProperty("character").objectReferenceValue = speaker;
            }

            so.ApplyModifiedPropertiesWithoutUndo();
        }

        private static Character FindCharacter(string displayName)
        {
            foreach (Character c in Object.FindObjectsOfType<Character>())
            {
                if (c.NameText == displayName)
                {
                    return c;
                }
            }

            Debug.LogWarning($"[Dusk] Fungus Character '{displayName}' not found — run 'Build Scenes' first. Line will show without a name plate.");
            return null;
        }

        private static void AddLockPlayer(Flowchart fc, Block block, PlayerMovement player, bool locked)
        {
            LockPlayerCommand cmd = AddCommand<LockPlayerCommand>(fc, block);
            var so = new SerializedObject(cmd);
            so.FindProperty("player").objectReferenceValue = player;
            so.FindProperty("locked").boolValue = locked;
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void AddSetFlag(Flowchart fc, Block block, string flag)
        {
            SetFlagCommand cmd = AddCommand<SetFlagCommand>(fc, block);
            var so = new SerializedObject(cmd);
            so.FindProperty("flag").stringValue = flag;
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void AddMenu(Flowchart fc, Block block, string text, Block target)
        {
            // Fully qualified: UnityEditor also defines a 'Menu' type, so bare 'Menu' would be ambiguous.
            Fungus.Menu cmd = AddCommand<Fungus.Menu>(fc, block);
            var so = new SerializedObject(cmd);
            so.FindProperty("text").stringValue = text;
            so.FindProperty("targetBlock").objectReferenceValue = target;
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void AddCall(Flowchart fc, Block block, Block target)
        {
            Fungus.Call cmd = AddCommand<Fungus.Call>(fc, block);
            var so = new SerializedObject(cmd);
            so.FindProperty("targetBlock").objectReferenceValue = target;
            so.FindProperty("callMode").enumValueIndex = 0; // CallMode.Stop: run the target, then end this block
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
