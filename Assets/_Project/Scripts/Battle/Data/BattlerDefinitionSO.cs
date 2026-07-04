using System.Collections.Generic;
using UnityEngine;

namespace DuskWarung.Battle
{
    /// <summary>
    /// Shared, designer-authored base stats for any battler. Concrete subclasses
    /// (<see cref="PlayerDefinitionSO"/>, <see cref="EnemyDefinitionSO"/>) add
    /// role-specific data. A <see cref="BattlerRuntime"/> is seeded from one of these.
    /// </summary>
    public abstract class BattlerDefinitionSO : ScriptableObject
    {
        [Tooltip("Name shown in the HUD and dialog.")]
        public string displayName = "Battler";

        [Tooltip("Sprite used for the battler in the battle scene.")]
        public Sprite battleSprite;

        [Header("Stats")]
        [Min(1)] public int maxHp = 30;
        [Min(0)] public int maxMp = 10;
        [Min(0)] public int attack = 8;
        [Min(0)] public int defense = 4;
        [Min(1)] public int speed = 6;

        [Tooltip("Skills this battler can use. The player exposes these on the Skill command.")]
        public List<SkillSO> skills = new List<SkillSO>();
    }
}
