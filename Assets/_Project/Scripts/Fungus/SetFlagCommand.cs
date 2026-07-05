using DuskWarung.Core;
using Fungus;
using UnityEngine;

namespace DuskWarung.FungusCommands
{
    /// <summary>
    /// Fungus command that raises a story flag on <see cref="GameSession"/>, letting authors record
    /// progression from a flowchart. Gates such as <see cref="World.EncounterTrigger"/> read it back.
    /// </summary>
    [CommandInfo("Dusk", "Set Flag",
        "Raises a story flag on the game session (used to gate later progression).")]
    [AddComponentMenu("")]
    public class SetFlagCommand : Command
    {
        [Tooltip("The flag to raise, e.g. \"met_bu_sari\".")]
        [SerializeField] protected string flag = "met_bu_sari";

        /// <inheritdoc/>
        public override void OnEnter()
        {
            GameSession.SetFlag(flag);
            Continue();
        }

        /// <inheritdoc/>
        public override string GetSummary()
            => string.IsNullOrEmpty(flag) ? "Set a flag name!" : "Set flag: " + flag;

        /// <inheritdoc/>
        public override Color GetButtonColor() => new Color32(180, 170, 210, 255);
    }
}
