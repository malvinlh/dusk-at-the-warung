namespace DuskWarung.Battle
{
    /// <summary>
    /// Runtime stack of a consumable item with a mutable remaining count.
    /// Seeded from <see cref="PlayerDefinitionSO.StartingItem"/> when a battler is built.
    /// </summary>
    public class ItemSlot
    {
        /// <summary>The item definition this slot holds.</summary>
        public ItemSO Item { get; }

        /// <summary>Remaining uses this battle.</summary>
        public int Count { get; private set; }

        /// <summary>True when the item exists and has at least one use left.</summary>
        public bool IsAvailable => Item != null && Count > 0;

        /// <summary>Creates a slot for <paramref name="item"/> with <paramref name="count"/> uses.</summary>
        public ItemSlot(ItemSO item, int count)
        {
            Item = item;
            Count = count < 0 ? 0 : count;
        }

        /// <summary>Consumes one use. Returns false when none remain.</summary>
        public bool TryConsume()
        {
            if (Count <= 0)
            {
                return false;
            }

            Count--;
            return true;
        }
    }
}
