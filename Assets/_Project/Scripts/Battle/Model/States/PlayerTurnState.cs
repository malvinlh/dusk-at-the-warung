namespace DuskWarung.Battle
{
    /// <summary>
    /// Waits for the player to choose a command through the UI. The UI submits via
    /// <see cref="BattleStateMachine.SubmitPlayerCommand"/>; this state consumes it,
    /// resolves it, and moves to action resolution.
    /// </summary>
    public sealed class PlayerTurnState : BattleStateBase
    {
        /// <summary>Creates the player-turn state.</summary>
        public PlayerTurnState(BattleStateMachine machine) : base(machine) { }

        /// <inheritdoc/>
        public override void Enter()
        {
            Machine.PendingPlayerCommand = null; // Discard any input queued before our turn.
        }

        /// <inheritdoc/>
        public override void Tick()
        {
            if (!Machine.PendingPlayerCommand.HasValue)
            {
                return;
            }

            BattleCommand command = Machine.PendingPlayerCommand.Value;
            Machine.PendingPlayerCommand = null;

            ActionResult result = Machine.Resolve(command);
            Machine.ChangeState(new ActionResolutionState(Machine, result));
        }
    }
}
