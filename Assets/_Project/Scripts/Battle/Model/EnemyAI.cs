using System;
using UnityEngine;

namespace DuskWarung.Battle
{
    /// <summary>
    /// Chooses an enemy action from its weighted AI table (a light Strategy: behaviour is
    /// data-driven rather than hard-coded). Falls back to a basic Attack when the table is
    /// empty or every listed skill is unaffordable.
    /// </summary>
    public static class EnemyAI
    {
        /// <summary>Picks a command for <paramref name="enemy"/> to use against <paramref name="player"/>.</summary>
        public static BattleCommand ChooseCommand(BattlerRuntime enemy, BattlerRuntime player, System.Random rng)
        {
            if (rng == null)
            {
                throw new ArgumentNullException(nameof(rng));
            }

            var definition = enemy.Definition as EnemyDefinitionSO;
            if (definition == null || definition.aiTable == null || definition.aiTable.Count == 0)
            {
                return BattleCommand.Attack(enemy, player);
            }

            // Sum the weights of the entries the enemy can currently afford.
            float totalWeight = 0f;
            foreach (EnemyDefinitionSO.AiEntry entry in definition.aiTable)
            {
                if (IsAffordable(enemy, entry.skill))
                {
                    totalWeight += Mathf.Max(0f, entry.weight);
                }
            }

            if (totalWeight <= 0f)
            {
                return BattleCommand.Attack(enemy, player);
            }

            // Weighted roulette selection over the affordable entries.
            double roll = rng.NextDouble() * totalWeight;
            foreach (EnemyDefinitionSO.AiEntry entry in definition.aiTable)
            {
                if (!IsAffordable(enemy, entry.skill))
                {
                    continue;
                }

                float weight = Mathf.Max(0f, entry.weight);
                if (roll < weight)
                {
                    return entry.skill == null
                        ? BattleCommand.Attack(enemy, player)
                        : BattleCommand.UseSkill(enemy, player, entry.skill);
                }

                roll -= weight;
            }

            // Floating-point safety net: fall back to a basic attack.
            return BattleCommand.Attack(enemy, player);
        }

        private static bool IsAffordable(BattlerRuntime enemy, SkillSO skill)
            => skill == null || enemy.CanAfford(skill.mpCost);
    }
}
