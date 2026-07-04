namespace DuskWarung.Battle
{
    /// <summary>How a battle finished. Written to the session so the overworld can react.</summary>
    public enum BattleOutcome
    {
        /// <summary>Battle has not ended yet.</summary>
        None,
        /// <summary>The player defeated the enemy.</summary>
        Victory,
        /// <summary>The player was defeated.</summary>
        Defeat,
        /// <summary>The player successfully fled the battle.</summary>
        Fled
    }
}
