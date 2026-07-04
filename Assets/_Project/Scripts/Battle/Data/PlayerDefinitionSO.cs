using System;
using System.Collections.Generic;
using UnityEngine;

namespace DuskWarung.Battle
{
    /// <summary>
    /// Definition for the player-controlled battler. Adds a starting inventory on top
    /// of the shared <see cref="BattlerDefinitionSO"/> stats.
    /// </summary>
    [CreateAssetMenu(menuName = "Dusk/Battler/Player", fileName = "Player_")]
    public class PlayerDefinitionSO : BattlerDefinitionSO
    {
        /// <summary>A quantity of one item the player carries into battle.</summary>
        [Serializable]
        public struct StartingItem
        {
            [Tooltip("Item carried into battle.")]
            public ItemSO item;

            [Tooltip("How many are available for this encounter.")]
            [Min(0)] public int count;
        }

        [Tooltip("Consumables the player starts the encounter with, exposed on the Item command.")]
        public List<StartingItem> startingItems = new List<StartingItem>();
    }
}
