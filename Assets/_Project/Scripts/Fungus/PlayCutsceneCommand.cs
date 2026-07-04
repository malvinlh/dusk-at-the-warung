using System.Collections;
using DuskWarung.World;
using Fungus;
using UnityEngine;

namespace DuskWarung.FungusCommands
{
    /// <summary>
    /// Fungus command that runs a <see cref="CutsceneDirector"/> and only advances the
    /// flowchart once the whole cutscene has finished, so authored narrative and scripted
    /// movement stay in lock-step.
    /// </summary>
    [CommandInfo("Dusk", "Play Cutscene",
        "Plays a self-driving cutscene, then advances the flowchart.")]
    [AddComponentMenu("")]
    public class PlayCutsceneCommand : Command
    {
        [Tooltip("Cutscene director to run.")]
        [SerializeField] protected CutsceneDirector cutscene;

        /// <inheritdoc/>
        public override void OnEnter()
        {
            if (cutscene == null)
            {
                Continue();
                return;
            }

            StartCoroutine(Run());
        }

        private IEnumerator Run()
        {
            yield return cutscene.Play();
            Continue();
        }

        /// <inheritdoc/>
        public override string GetSummary()
            => cutscene != null ? "Play: " + cutscene.name : "Assign a cutscene!";

        /// <inheritdoc/>
        public override Color GetButtonColor() => new Color32(160, 200, 180, 255);
    }
}
