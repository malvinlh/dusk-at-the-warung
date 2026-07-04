using NUnit.Framework;

namespace DuskWarung.Battle.Tests
{
    /// <summary>Verifies turn ordering by speed and exclusion of the dead.</summary>
    public class TurnQueueTests
    {
        [Test]
        public void Rebuild_OrdersBySpeedDescending()
        {
            BattlerRuntime fast = TestBattlerFactory.Player(spd: 10);
            BattlerRuntime slow = TestBattlerFactory.Enemy(spd: 3);

            var queue = new TurnQueue();
            queue.Rebuild(new[] { slow, fast });

            Assert.IsTrue(queue.HasNext);
            Assert.AreSame(fast, queue.Next());
            Assert.AreSame(slow, queue.Next());
            Assert.IsFalse(queue.HasNext);
        }

        [Test]
        public void Rebuild_ExcludesDeadBattlers()
        {
            BattlerRuntime alive = TestBattlerFactory.Player(hp: 10, spd: 5);
            BattlerRuntime dead = TestBattlerFactory.Enemy(hp: 10, spd: 9);
            dead.TakeDamage(10);

            var queue = new TurnQueue();
            queue.Rebuild(new[] { alive, dead });

            Assert.AreEqual(1, queue.Count);
            Assert.AreSame(alive, queue.Next());
        }

        [Test]
        public void Rebuild_WithNull_IsSafeAndEmpty()
        {
            var queue = new TurnQueue();
            queue.Rebuild(null);
            Assert.IsFalse(queue.HasNext);
        }
    }
}
