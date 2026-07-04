namespace DuskWarung.Battle
{
    /// <summary>
    /// One phase of the turn-based battle (State pattern). The owning
    /// <see cref="BattleStateMachine"/> calls <see cref="Enter"/> once on entry,
    /// <see cref="Tick"/> every frame, and <see cref="Exit"/> once on leaving.
    /// States request transitions from <see cref="Tick"/>, never from <see cref="Enter"/>.
    /// </summary>
    public interface IBattleState
    {
        /// <summary>Runs once when the state becomes active.</summary>
        void Enter();

        /// <summary>Runs every frame while the state is active; drives transitions.</summary>
        void Tick();

        /// <summary>Runs once when the state is left.</summary>
        void Exit();
    }
}
