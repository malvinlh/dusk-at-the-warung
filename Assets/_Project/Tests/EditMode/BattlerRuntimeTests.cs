using System;
using NUnit.Framework;

namespace DuskWarung.Battle.Tests
{
    /// <summary>Verifies the mutable per-battler state: HP/MP clamping, MP spending, and inventory.</summary>
    public class BattlerRuntimeTests
    {
        [Test]
        public void TakeDamage_ReducesHp_AndReturnsDealt()
        {
            BattlerRuntime b = TestBattlerFactory.Player(hp: 30);
            int dealt = b.TakeDamage(10);
            Assert.AreEqual(10, dealt);
            Assert.AreEqual(20, b.Hp);
        }

        [Test]
        public void TakeDamage_NeverBelowZero_AndReportsActualDealt()
        {
            BattlerRuntime b = TestBattlerFactory.Player(hp: 8);
            int dealt = b.TakeDamage(100);
            Assert.AreEqual(8, dealt);
            Assert.AreEqual(0, b.Hp);
            Assert.IsFalse(b.IsAlive);
        }

        [Test]
        public void TakeDamage_NegativeAmount_DealsNothing()
        {
            BattlerRuntime b = TestBattlerFactory.Player(hp: 30);
            int dealt = b.TakeDamage(-5);
            Assert.AreEqual(0, dealt);
            Assert.AreEqual(30, b.Hp);
        }

        [Test]
        public void Heal_ClampsToMaxHp()
        {
            BattlerRuntime b = TestBattlerFactory.Player(hp: 30);
            b.TakeDamage(20); // Hp = 10
            b.Heal(100);
            Assert.AreEqual(30, b.Hp);
        }

        [Test]
        public void Heal_NonPositive_IsIgnored()
        {
            BattlerRuntime b = TestBattlerFactory.Player(hp: 30);
            b.TakeDamage(10); // Hp = 20
            b.Heal(0);
            b.Heal(-5);
            Assert.AreEqual(20, b.Hp);
        }

        [Test]
        public void TrySpendMp_FailsWhenInsufficient()
        {
            BattlerRuntime b = TestBattlerFactory.Player(mp: 3);
            Assert.IsFalse(b.TrySpendMp(5));
            Assert.AreEqual(3, b.Mp);
        }

        [Test]
        public void TrySpendMp_DeductsWhenAffordable()
        {
            BattlerRuntime b = TestBattlerFactory.Player(mp: 10);
            Assert.IsTrue(b.TrySpendMp(5));
            Assert.AreEqual(5, b.Mp);
        }

        [Test]
        public void CanAfford_IsInclusiveAtBoundary()
        {
            BattlerRuntime b = TestBattlerFactory.Player(mp: 5);
            Assert.IsTrue(b.CanAfford(5));
            Assert.IsFalse(b.CanAfford(6));
        }

        [Test]
        public void Inventory_IsSeededFromDefinition()
        {
            PlayerDefinitionSO def = TestBattlerFactory.PlayerDef();
            ItemSO item = TestBattlerFactory.ItemDef(heal: 12);
            def.startingItems.Add(new PlayerDefinitionSO.StartingItem { item = item, count = 2 });

            var b = new BattlerRuntime(def, true);
            ItemSlot slot = b.FindItem(item);

            Assert.IsNotNull(slot);
            Assert.AreEqual(2, slot.Count);
            Assert.IsTrue(slot.TryConsume());
            Assert.AreEqual(1, slot.Count);
        }

        [Test]
        public void ItemSlot_TryConsume_FailsWhenEmpty()
        {
            var slot = new ItemSlot(TestBattlerFactory.ItemDef(), 1);
            Assert.IsTrue(slot.TryConsume());
            Assert.IsFalse(slot.TryConsume());
            Assert.IsFalse(slot.IsAvailable);
        }

        [Test]
        public void Constructor_NullDefinition_Throws()
        {
            Assert.Throws<ArgumentNullException>(() => new BattlerRuntime(null, true));
        }
    }
}
