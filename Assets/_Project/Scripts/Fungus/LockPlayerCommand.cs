using DuskWarung.World;
using Fungus;
using UnityEngine;

namespace DuskWarung.FungusCommands
{
    /// <summary>
    /// Fungus command that locks or unlocks player control from inside a flowchart, so
    /// narrative authors can freeze the avatar during dialog without touching code.
    /// </summary>
    [CommandInfo("Dusk", "Lock Player",
        "Enables or disables player movement/interaction during narrative.")]
    [AddComponentMenu("")]
    public class LockPlayerCommand : Command
    {
        [Tooltip("Player movement component to gate.")]
        [SerializeField] protected PlayerMovement player;

        [Tooltip("True = lock (disable control); False = unlock.")]
        [SerializeField] protected bool locked = true;

        /// <inheritdoc/>
        public override void OnEnter()
        {
            if (player != null)
            {
                player.SetControlEnabled(!locked);
            }

            Continue();
        }

        /// <inheritdoc/>
        public override string GetSummary() => locked ? "Lock player" : "Unlock player";

        /// <inheritdoc/>
        public override Color GetButtonColor() => new Color32(200, 190, 140, 255);
    }
}
