using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;

namespace DuskWarung.EditorTools
{
    /// <summary>
    /// Generates the Animator Controllers + sprite clips:
    /// <list type="bullet">
    /// <item><c>AC_Character</c> — top-down Idle/Walk 2D directional blend trees (MoveX/MoveY/Speed).</item>
    /// <item><c>AC_PlayerBattler</c> / <c>AC_EnemyBattler</c> — Idle loop + Attack (trigger <c>PlayAttack</c>).</item>
    /// </list>
    /// ASSUMPTION: the character Walk sheet lays out facings across columns (<b>Down, Up, Left, Right</b>).
    /// If a facing comes out wrong, reorder <see cref="ColumnOrder"/> (a one-line change; the spin is
    /// already fixed by grouping per column).
    /// </summary>
    public static class AnimatorTool
    {
        private enum Dir { Down = 0, Up = 1, Left = 2, Right = 3 }

        // The Ninja Adventure Walk sheet lays out the four facings across COLUMNS (each row is a walk
        // frame). This maps each facing to its column index. If a facing comes out wrong after playtest,
        // reorder this array — the "spinning" is already fixed by grouping per column regardless of order.
        private static readonly Dir[] ColumnOrder = { Dir.Down, Dir.Up, Dir.Left, Dir.Right };

        private const string PlayerWalk = DuskEditorUtil.SpritesDir + "/Player/SeparateAnim/Walk.png";
        private const string PlayerAttack = DuskEditorUtil.SpritesDir + "/Player/SeparateAnim/Attack.png";

        [MenuItem("Tools/Dusk Warung/4. Create Animators", priority = 4)]
        public static void Run()
        {
            DuskEditorUtil.EnsureFolder(DuskEditorUtil.AnimatorsDir);
            DuskEditorUtil.EnsureFolder(DuskEditorUtil.AnimationsDir);

            // Slice the player animation strips (characters' SpriteSheet is already sliced; these aren't).
            DuskEditorUtil.GridSlice(PlayerWalk, 16, 16);
            DuskEditorUtil.GridSlice(PlayerAttack, 16, 16);
            AssetDatabase.Refresh();

            BuildCharacterController();
            BuildBattlerController("AC_PlayerBattler", PlayerBattlerIdle(), PlayerBattlerAttack());
            BuildBattlerController("AC_EnemyBattler",
                DuskEditorUtil.LoadSpritesRowMajor(DuskEditorUtil.EnemyIdle).ToArray(),
                DuskEditorUtil.LoadSpritesRowMajor(DuskEditorUtil.EnemyAttack).ToArray());

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("[Dusk] Animators created (AC_Character, AC_PlayerBattler, AC_EnemyBattler). " +
                      "If the avatar faces the wrong way, tell me to flip the Walk row order.");
        }

        // --- Overworld directional controller ---

        private static void BuildCharacterController()
        {
            List<Sprite> walk = DuskEditorUtil.LoadSpritesRowMajor(PlayerWalk);
            if (walk.Count < 4)
            {
                Debug.LogError("[Dusk] Player Walk.png produced too few frames — cannot build AC_Character.");
                return;
            }

            int cols = WalkColumnCount(); // facings are laid out across columns

            AnimationClip WalkClip(Dir d) => MakeClip($"Char_Walk_{d}", Column(walk, cols, d), 8f, true);
            AnimationClip IdleClip(Dir d) => MakeClip($"Char_Idle_{d}", new[] { Column(walk, cols, d)[0] }, 1f, true);

            string path = DuskEditorUtil.AnimatorsDir + "/AC_Character.controller";
            AnimatorController ac = RecreateController(path);
            ac.AddParameter("MoveX", AnimatorControllerParameterType.Float);
            ac.AddParameter("MoveY", AnimatorControllerParameterType.Float);
            ac.AddParameter("Speed", AnimatorControllerParameterType.Float);

            AnimatorStateMachine sm = ac.layers[0].stateMachine;
            AnimatorState idle = AddDirectionalState(ac, sm, "Idle",
                IdleClip(Dir.Down), IdleClip(Dir.Up), IdleClip(Dir.Left), IdleClip(Dir.Right));
            AnimatorState walkState = AddDirectionalState(ac, sm, "Walk",
                WalkClip(Dir.Down), WalkClip(Dir.Up), WalkClip(Dir.Left), WalkClip(Dir.Right));

            sm.defaultState = idle;

            AnimatorStateTransition toWalk = idle.AddTransition(walkState);
            toWalk.hasExitTime = false;
            toWalk.duration = 0f;
            toWalk.AddCondition(AnimatorConditionMode.Greater, 0.01f, "Speed");

            AnimatorStateTransition toIdle = walkState.AddTransition(idle);
            toIdle.hasExitTime = false;
            toIdle.duration = 0f;
            toIdle.AddCondition(AnimatorConditionMode.Less, 0.01f, "Speed");

            EditorUtility.SetDirty(ac);
        }

        private static AnimatorState AddDirectionalState(AnimatorController ac, AnimatorStateMachine sm, string name,
            AnimationClip down, AnimationClip up, AnimationClip left, AnimationClip right)
        {
            var tree = new BlendTree
            {
                name = name,
                blendType = BlendTreeType.SimpleDirectional2D,
                blendParameter = "MoveX",
                blendParameterY = "MoveY"
            };
            AssetDatabase.AddObjectToAsset(tree, ac);
            tree.AddChild(down, new Vector2(0f, -1f));
            tree.AddChild(up, new Vector2(0f, 1f));
            tree.AddChild(left, new Vector2(-1f, 0f));
            tree.AddChild(right, new Vector2(1f, 0f));

            AnimatorState state = sm.AddState(name);
            state.motion = tree;
            return state;
        }

        // --- Battler controllers (Idle loop + Attack trigger) ---

        private static void BuildBattlerController(string controllerName, Sprite[] idleFrames, Sprite[] attackFrames)
        {
            if (idleFrames == null || idleFrames.Length == 0)
            {
                Debug.LogWarning($"[Dusk] {controllerName}: no idle frames found — skipped.");
                return;
            }

            if (attackFrames == null || attackFrames.Length == 0)
            {
                attackFrames = idleFrames; // fall back so the state exists
            }

            AnimationClip idleClip = MakeClip(controllerName + "_Idle", idleFrames, 4f, true);
            AnimationClip attackClip = MakeClip(controllerName + "_Attack", attackFrames, 10f, false);

            string path = DuskEditorUtil.AnimatorsDir + "/" + controllerName + ".controller";
            AnimatorController ac = RecreateController(path);
            ac.AddParameter("PlayAttack", AnimatorControllerParameterType.Trigger);

            AnimatorStateMachine sm = ac.layers[0].stateMachine;
            AnimatorState idle = sm.AddState("Idle");
            idle.motion = idleClip;
            AnimatorState attack = sm.AddState("Attack");
            attack.motion = attackClip;
            sm.defaultState = idle;

            AnimatorStateTransition toAttack = idle.AddTransition(attack);
            toAttack.hasExitTime = false;
            toAttack.duration = 0f;
            toAttack.AddCondition(AnimatorConditionMode.If, 0f, "PlayAttack");

            AnimatorStateTransition toIdle = attack.AddTransition(idle);
            toIdle.hasExitTime = true;
            toIdle.exitTime = 1f;
            toIdle.duration = 0f;

            EditorUtility.SetDirty(ac);
        }

        private static Sprite[] PlayerBattlerIdle()
        {
            // Use the front-facing (Down) walk column as a gentle standing idle.
            List<Sprite> walk = DuskEditorUtil.LoadSpritesRowMajor(PlayerWalk);
            if (walk.Count == 0)
            {
                return System.Array.Empty<Sprite>();
            }

            return Column(walk, WalkColumnCount(), Dir.Down);
        }

        private static int WalkColumnCount()
        {
            var texture = AssetDatabase.LoadAssetAtPath<Texture2D>(PlayerWalk);
            return texture != null ? Mathf.Max(1, texture.width / 16) : 4;
        }

        /// <summary>Returns every frame in the given facing's column (stride = column count).</summary>
        private static Sprite[] Column(List<Sprite> frames, int cols, Dir dir)
        {
            int col = System.Array.IndexOf(ColumnOrder, dir);
            if (col < 0)
            {
                col = 0;
            }

            var result = new List<Sprite>();
            for (int row = 0; row * cols + col < frames.Count; row++)
            {
                result.Add(frames[row * cols + col]);
            }

            return result.Count > 0 ? result.ToArray() : new[] { frames[0] };
        }

        private static Sprite[] PlayerBattlerAttack()
        {
            List<Sprite> attack = DuskEditorUtil.LoadSpritesRowMajor(PlayerAttack);
            return attack.Count > 0 ? attack.ToArray() : PlayerBattlerIdle();
        }

        // --- helpers ---

        private static AnimatorController RecreateController(string path)
        {
            if (AssetDatabase.LoadAssetAtPath<AnimatorController>(path) != null)
            {
                AssetDatabase.DeleteAsset(path);
            }

            return AnimatorController.CreateAnimatorControllerAtPath(path);
        }

        private static AnimationClip MakeClip(string name, Sprite[] frames, float fps, bool loop)
        {
            var clip = new AnimationClip { frameRate = fps };
            var binding = new EditorCurveBinding { path = string.Empty, type = typeof(SpriteRenderer), propertyName = "m_Sprite" };

            var keys = new ObjectReferenceKeyframe[frames.Length];
            for (int i = 0; i < frames.Length; i++)
            {
                keys[i] = new ObjectReferenceKeyframe { time = i / fps, value = frames[i] };
            }

            AnimationUtility.SetObjectReferenceCurve(clip, binding, keys);

            if (loop)
            {
                AnimationClipSettings settings = AnimationUtility.GetAnimationClipSettings(clip);
                settings.loopTime = true;
                AnimationUtility.SetAnimationClipSettings(clip, settings);
            }

            string path = DuskEditorUtil.AnimationsDir + "/" + name + ".anim";
            if (AssetDatabase.LoadAssetAtPath<AnimationClip>(path) != null)
            {
                AssetDatabase.DeleteAsset(path);
            }

            AssetDatabase.CreateAsset(clip, path);
            return clip;
        }
    }
}
