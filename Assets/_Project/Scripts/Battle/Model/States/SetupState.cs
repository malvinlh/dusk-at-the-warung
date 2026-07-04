namespace DuskWarung.Battle
{
    /// <summary>Builds the first turn order, then hands off to turn resolution.</summary>
    public sealed class SetupState : BattleStateBase
    {
        /// <summary>Creates the setup state.</summary>
        public SetupState(BattleStateMachine machine) : base(machine) { }

        /// <inheritdoc/>
        public override void Enter()
        {
            Machine.Turns.Rebuild(Machine.AllBattlers);
        }

        /// <inheritdoc/>
        public override void Tick()
        {
            Machine.ChangeState(new TurnAdvanceState(Machine));
        }
    }
}
