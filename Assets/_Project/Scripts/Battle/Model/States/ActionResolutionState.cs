namespace DuskWarung.Battle
{
    /// <summary>
    /// Holds the turn while the view plays its presentation (lunge, flash, hit-stop, shake,
    /// damage popup). The model has already mutated; this state simply waits for the view to
    /// report completion, then ends the battle on a successful flee or advances the turn.
    /// </summary>
    public sealed class ActionResolutionState : BattleStateBase
    {
        private readonly ActionResult _result;

        /// <summary>Creates the resolution state for a just-resolved action.</summary>
        public ActionResolutionState(BattleStateMachine machine, ActionResult result) : base(machine)
        {
            _result = result;
        }

        /// <inheritdoc/>
        public override void Tick()
        {
            if (!Machine.PresentationComplete)
            {
                return; // Wait for the view to finish its juice before advancing.
            }

            if (_result.Fled)
            {
                Machine.ChangeState(new BattleEndState(Machine, BattleOutcome.Fled));
                return;
            }

            Machine.ChangeState(new TurnAdvanceState(Machine));
        }
    }
}
