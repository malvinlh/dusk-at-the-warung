using UnityEngine;

namespace DuskWarung.Battle
{
    /// <summary>
    /// A self-contained battle setup: who fights whom, and the backdrop to show.
    /// An <c>EncounterTrigger</c> stores one of these in the session before loading
    /// the battle scene, where the <c>BattleController</c> reads it back.
    /// </summary>
    [CreateAssetMenu(menuName = "Dusk/Encounter", fileName = "Encounter_")]
    public class EncounterSO : ScriptableObject
    {
        [Tooltip("Player battler definition for this encounter.")]
        public PlayerDefinitionSO player;

        [Tooltip("Enemy battler definition for this encounter.")]
        public EnemyDefinitionSO enemy;

        [Tooltip("Battle backdrop sprite (e.g. the dusk-forest battleback).")]
        public Sprite background;
    }
}
