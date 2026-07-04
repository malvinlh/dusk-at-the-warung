namespace DuskWarung.Battle
{
    /// <summary>
    /// Convenience base for battle states: holds the machine reference and provides
    /// no-op <see cref="Enter"/>/<see cref="Tick"/>/<see cref="Exit"/> so each concrete
    /// state only overrides what it needs.
    /// </summary>
    public abstract class BattleStateBase : IBattleState
    {
        /// <summary>The state machine that owns this state.</summary>
        protected readonly BattleStateMachine Machine;

        /// <summary>Creates the state bound to its owning machine.</summary>
        protected BattleStateBase(BattleStateMachine machine)
        {
            Machine = machine;
        }

        /// <inheritdoc/>
        public virtual void Enter() { }

        /// <inheritdoc/>
        public virtual void Tick() { }

        /// <inheritdoc/>
        public virtual void Exit() { }
    }
}
