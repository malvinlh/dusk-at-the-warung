using UnityEngine;

namespace DuskWarung.Battle
{
    /// <summary>
    /// Designer-authored definition of an offensive skill (e.g. "Cracker Toss").
    /// Skills spend MP and deal damage scaled by <see cref="power"/>.
    /// </summary>
    [CreateAssetMenu(menuName = "Dusk/Skill", fileName = "Skill_")]
    public class SkillSO : ScriptableObject
    {
        [Tooltip("Name shown on the command button.")]
        public string displayName = "New Skill";

        [Tooltip("MP consumed when the skill is used. The skill is unusable when the caster has less MP than this.")]
        [Min(0)] public int mpCost = 5;

        [Tooltip("Attack multiplier fed into the damage formula: damage scales with attack * power.")]
        [Min(0f)] public float power = 1.5f;

        [Tooltip("Flavor text shown when the skill is highlighted.")]
        [TextArea] public string tooltip = "";

        [Tooltip("Optional icon for the command button (leave empty to use a text-only button).")]
        public Sprite icon;
    }
}
