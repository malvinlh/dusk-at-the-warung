using DuskWarung.Battle;
using DuskWarung.Core;
using Fungus;
using UnityEngine;

namespace DuskWarung.FungusCommands
{
    /// <summary>
    /// Fungus command that queues an encounter and transitions to the battle scene with a
    /// fade, letting a flowchart hand off directly into combat at the end of a cutscene.
    /// </summary>
    [CommandInfo("Dusk", "Start Encounter",
        "Sets the pending encounter and transitions to the Battle scene.")]
    [AddComponentMenu("")]
    public class StartEncounterCommand : Command
    {
        [Tooltip("Encounter data to fight.")]
        [SerializeField] protected EncounterSO encounter;

        // Fully qualified because Fungus also defines a SceneLoader type.
        [Tooltip("Scene loader that performs the fade + load.")]
        [SerializeField] protected DuskWarung.Core.SceneLoader loader;

        [Tooltip("Name of the battle scene to load.")]
        [SerializeField] protected string battleSceneName = "Battle";

        [Tooltip("Optional: capture this transform's position as the overworld return point.")]
        [SerializeField] protected Transform returnPositionSource;

        /// <inheritdoc/>
        public override void OnEnter()
        {
            GameSession.PendingEncounter = encounter;

            if (returnPositionSource != null)
            {
                GameSession.ReturnPosition = returnPositionSource.position;
                GameSession.HasReturnPosition = true;
            }

            // Hide any lingering Say dialog so it can't flash during the fade-to-black.
            if (SayDialog.ActiveSayDialog != null)
            {
                SayDialog.ActiveSayDialog.Stop();
            }

            if (loader != null)
            {
                loader.LoadWithFade(battleSceneName);
            }

            Continue();
        }

        /// <inheritdoc/>
        public override string GetSummary()
            => encounter != null ? "Fight: " + encounter.name : "Assign an encounter!";

        /// <inheritdoc/>
        public override Color GetButtonColor() => new Color32(205, 140, 140, 255);
    }
}
