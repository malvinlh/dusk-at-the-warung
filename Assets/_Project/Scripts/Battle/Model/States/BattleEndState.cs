namespace DuskWarung.Battle
{
    /// <summary>
    /// Terminal state. Records the outcome and raises <c>OnBattleEnded</c> so the scene
    /// controller can play the closing dialog and transition out. Parameterised by outcome
    /// instead of split into Victory/Defeat/Fled classes to keep the ending logic in one place.
    /// </summary>
    public sealed class BattleEndState : BattleStateBase
    {
        private readonly BattleOutcome _outcome;

        /// <summary>Creates the end state for the given <paramref name="outcome"/>.</summary>
        public BattleEndState(BattleStateMachine machine, BattleOutcome outcome) : base(machine)
        {
            _outcome = outcome;
        }

        /// <inheritdoc/>
        public override void Enter()
        {
            Machine.EndBattle(_outcome);
        }
    }
}
