namespace DuskWarung.Battle
{
    /// <summary>
    /// Decides who acts next. Ends the battle when a side has fallen, otherwise pops the
    /// next battler from the queue (rebuilding it when the round is exhausted) and routes
    /// to the player or enemy turn.
    /// </summary>
    public sealed class TurnAdvanceState : BattleStateBase
    {
        /// <summary>Creates the turn-advance state.</summary>
        public TurnAdvanceState(BattleStateMachine machine) : base(machine) { }

        /// <inheritdoc/>
        public override void Tick()
        {
            if (!Machine.Enemy.IsAlive)
            {
                Machine.ChangeState(new BattleEndState(Machine, BattleOutcome.Victory));
                return;
            }

            if (!Machine.Player.IsAlive)
            {
                Machine.ChangeState(new BattleEndState(Machine, BattleOutcome.Defeat));
                return;
            }

            if (!Machine.Turns.HasNext)
            {
                Machine.Turns.Rebuild(Machine.AllBattlers);
            }

            Machine.Current = Machine.Turns.Next();
            if (!Machine.Current.IsAlive)
            {
                return; // Skip a battler that died mid-round; re-evaluate next tick.
            }

            IBattleState next = Machine.Current.IsPlayer
                ? new PlayerTurnState(Machine)
                : (IBattleState)new EnemyTurnState(Machine);
            Machine.ChangeState(next);
        }
    }
}
