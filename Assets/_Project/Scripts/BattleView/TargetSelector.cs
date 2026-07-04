namespace DuskWarung.Battle.View
{
    /// <summary>
    /// Centralises the "who does this command hit?" rule. Trivial for a single-enemy fight
    /// (items self-target; everything else hits the opponent) but kept as one seam so adding
    /// multi-target selection later touches only this file.
    /// </summary>
    public static class TargetSelector
    {
        /// <summary>Returns the battler a command of the given kind should affect.</summary>
        public static BattlerRuntime Resolve(BattleCommand.Kind kind, BattlerRuntime actor, BattlerRuntime opponent)
        {
            return kind == BattleCommand.Kind.Item ? actor : opponent;
        }
    }
}
