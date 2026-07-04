using System.Collections;
using System.Collections.Generic;
using DuskWarung.FungusCommands;
using UnityEngine;

namespace DuskWarung.World
{
    /// <summary>
    /// A data-driven cutscene: an ordered list of steps (walk to a waypoint, wait, or run a
    /// Fungus block). While it plays, player control is locked and the avatar is driven by
    /// scripted input, satisfying the "avatar moves by itself" requirement. Control is
    /// restored when the sequence finishes.
    /// </summary>
    public class CutsceneDirector : MonoBehaviour
    {
        /// <summary>The kind of action a cutscene step performs.</summary>
        public enum StepKind
        {
            MoveTo,
            Wait,
            Fungus
        }

        /// <summary>One step in a cutscene.</summary>
        [System.Serializable]
        public struct Step
        {
            [Tooltip("What this step does.")]
            public StepKind kind;

            [Tooltip("Waypoint to walk to (MoveTo).")]
            public Transform target;

            [Tooltip("Seconds to wait (Wait).")]
            public float seconds;

            [Tooltip("Fungus block to run and wait for (Fungus).")]
            public string blockName;
        }

        [SerializeField, Tooltip("The player movement to drive.")]
        private PlayerMovement player;

        [SerializeField, Tooltip("Bridge to the scene's Fungus Flowchart (for Fungus steps).")]
        private FungusBridge fungus;

        [SerializeField, Tooltip("How close counts as 'arrived' at a MoveTo waypoint.")]
        private float arriveDistance = 0.05f;

        [SerializeField, Tooltip("The ordered steps of this cutscene.")]
        private List<Step> steps = new List<Step>();

        /// <summary>Runs the cutscene: locks control, executes each step in order, then restores control.</summary>
        public IEnumerator Play()
        {
            if (player == null)
            {
                yield break;
            }

            player.SetControlEnabled(false);

            foreach (Step step in steps)
            {
                yield return RunStep(step);
            }

            player.ClearScriptedInput();
            player.SetControlEnabled(true);
        }

        private IEnumerator RunStep(Step step)
        {
            switch (step.kind)
            {
                case StepKind.MoveTo:
                    yield return MoveTo(step.target);
                    break;
                case StepKind.Wait:
                    yield return new WaitForSeconds(step.seconds);
                    break;
                case StepKind.Fungus:
                    yield return RunFungus(step.blockName);
                    break;
            }
        }

        private IEnumerator MoveTo(Transform target)
        {
            if (target == null)
            {
                yield break;
            }

            while (Vector2.Distance(player.transform.position, target.position) > arriveDistance)
            {
                Vector2 toTarget = (Vector2)target.position - (Vector2)player.transform.position;
                player.SetScriptedInput(toTarget.normalized);
                yield return null;
            }

            player.SetScriptedInput(Vector2.zero);
        }

        private IEnumerator RunFungus(string blockName)
        {
            if (fungus == null || string.IsNullOrEmpty(blockName))
            {
                yield break;
            }

            bool done = false;
            fungus.ExecuteBlock(blockName, () => done = true);
            yield return new WaitUntil(() => done);
        }
    }
}
