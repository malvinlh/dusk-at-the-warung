namespace DuskWarung.Battle
{
    /// <summary>
    /// The enemy's turn: <see cref="EnemyAI"/> picks a command from the enemy's weighted
    /// table, the machine resolves it, and control moves to action resolution.
    /// </summary>
    public sealed class EnemyTurnState : BattleStateBase
    {
        /// <summary>Creates the enemy-turn state.</summary>
        public EnemyTurnState(BattleStateMachine machine) : base(machine) { }

        /// <inheritdoc/>
        public override void Tick()
        {
            BattleCommand command = EnemyAI.ChooseCommand(Machine.Current, Machine.Player, Machine.Rng);
            ActionResult result = Machine.Resolve(command);
            Machine.ChangeState(new ActionResolutionState(Machine, result));
        }
    }
}
