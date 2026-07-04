using System;
using System.Collections.Generic;
using UnityEngine;

namespace DuskWarung.Battle
{
    /// <summary>
    /// Definition for an enemy battler. Adds a weighted AI table used by
    /// <see cref="EnemyAI"/> to pick an action each turn.
    /// </summary>
    [CreateAssetMenu(menuName = "Dusk/Battler/Enemy", fileName = "Enemy_")]
    public class EnemyDefinitionSO : BattlerDefinitionSO
    {
        /// <summary>One weighted option in the enemy's AI table.</summary>
        [Serializable]
        public struct AiEntry
        {
            [Tooltip("Skill to use. Leave empty for a basic Attack.")]
            public SkillSO skill;

            [Tooltip("Relative likelihood of picking this entry (higher = more often).")]
            [Min(0f)] public float weight;
        }

        [Tooltip("Weighted action table. If empty (or MP is too low for every skill), the enemy falls back to a basic Attack.")]
        public List<AiEntry> aiTable = new List<AiEntry>();
    }
}
