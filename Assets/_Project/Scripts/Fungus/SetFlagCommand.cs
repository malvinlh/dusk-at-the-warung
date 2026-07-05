using DuskWarung.Core;
using Fungus;
using UnityEngine;

namespace DuskWarung.FungusCommands
{
    /// <summary>
    /// Fungus command that raises a story flag on <see cref="GameSession"/>, so narrative authors can
    /// record progression ("met the warung owner", "beat the genderuwo") from a flowchart without code.
    /// Progression gates (e.g. an <see cref="World.EncounterTrigger"/> with a required flag) then read it.
    /// This is the designer-facing half of the event-flag system; the programmer ships the verb, the
    /// designer decides when the story sets the flag.
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
