using System;
using System.Collections.Generic;
using UnityEngine;

namespace DuskWarung.Battle
{
    /// <summary>
    /// Mutable combat state for one participant in a battle, seeded from a
    /// <see cref="BattlerDefinitionSO"/>. This is pure model state: it never touches
    /// the Unity scene, UI, or any view. Views read it; they never mutate it directly.
    /// </summary>
    public class BattlerRuntime
    {
        /// <summary>The definition this runtime was built from (used by enemy AI).</summary>
        public BattlerDefinitionSO Definition { get; }

        public string DisplayName { get; }
        public Sprite BattleSprite { get; }

        public int MaxHp { get; }
        public int Hp { get; private set; }
        public int MaxMp { get; }
        public int Mp { get; private set; }

        public int Attack { get; }
        public int Defense { get; }
        public int Speed { get; }

        /// <summary>True for the player-controlled battler, false for the enemy.</summary>
        public bool IsPlayer { get; }

        /// <summary>Skills this battler can use.</summary>
        public IReadOnlyList<SkillSO> Skills { get; }

        /// <summary>Consumable items this battler carries (empty for the enemy).</summary>
        public IReadOnlyList<ItemSlot> Items => _items;

        /// <summary>True while HP remains.</summary>
        public bool IsAlive => Hp > 0;

        private readonly List<ItemSlot> _items = new List<ItemSlot>();

        /// <summary>Builds a runtime battler from a definition.</summary>
        /// <param name="def">Source stat block. Must not be null.</param>
        /// <param name="isPlayer">Whether this battler is player-controlled.</param>
        public BattlerRuntime(BattlerDefinitionSO def, bool isPlayer)
        {
            if (def == null)
            {
                throw new ArgumentNullException(nameof(def));
            }

            Definition = def;
            DisplayName = def.displayName;
            BattleSprite = def.battleSprite;

            MaxHp = Mathf.Max(1, def.maxHp);
            Hp = MaxHp;
            MaxMp = Mathf.Max(0, def.maxMp);
            Mp = MaxMp;
            Attack = Mathf.Max(0, def.attack);
            Defense = Mathf.Max(0, def.defense);
            Speed = Mathf.Max(1, def.speed);
            IsPlayer = isPlayer;

            Skills = def.skills != null ? new List<SkillSO>(def.skills) : new List<SkillSO>();

            if (def is PlayerDefinitionSO player && player.startingItems != null)
            {
                foreach (PlayerDefinitionSO.StartingItem entry in player.startingItems)
                {
                    if (entry.item != null && entry.count > 0)
                    {
                        _items.Add(new ItemSlot(entry.item, entry.count));
                    }
                }
            }
        }

        /// <summary>Applies damage, clamped so HP never drops below zero. Returns the HP actually removed.</summary>
        public int TakeDamage(int amount)
        {
            int dealt = Mathf.Clamp(amount, 0, Hp);
            Hp -= dealt;
            return dealt;
        }

        /// <summary>Restores HP, clamped to <see cref="MaxHp"/>. Non-positive amounts are ignored.</summary>
        public void Heal(int amount)
        {
            if (amount <= 0)
            {
                return;
            }

            Hp = Mathf.Min(MaxHp, Hp + amount);
        }

        /// <summary>True when the battler has at least <paramref name="mpCost"/> MP.</summary>
        public bool CanAfford(int mpCost) => Mp >= mpCost;

        /// <summary>Spends MP if affordable. Returns false (and spends nothing) when too poor.</summary>
        public bool TrySpendMp(int cost)
        {
            if (cost < 0 || Mp < cost)
            {
                return false;
            }

            Mp -= cost;
            return true;
        }

        /// <summary>Finds the runtime slot for an item, or null if the battler does not carry it.</summary>
        public ItemSlot FindItem(ItemSO item)
        {
            if (item == null)
            {
                return null;
            }

            foreach (ItemSlot slot in _items)
            {
                if (slot.Item == item)
                {
                    return slot;
                }
            }

            return null;
        }
    }
}
