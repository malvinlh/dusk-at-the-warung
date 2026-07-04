using System;
using NUnit.Framework;

namespace DuskWarung.Battle.Tests
{
    /// <summary>
    /// Pins the deterministic damage formula to the worked examples in the design doc
    /// (Appendix B), so a change to the math is caught immediately.
    /// </summary>
    public class DamageCalculatorTests
    {
        [Test]
        public void BasicAttack_MatchesWorkedExample()
        {
            // (8 * 1.0 - 4) * 1.0 = 4
            Assert.AreEqual(4, DamageCalculator.ComputeDamage(8, 1f, 4, 1.0f, crit: false));
        }

        [Test]
        public void BasicAttack_Crit_MatchesWorkedExample()
        {
            // 4 * 1.5 = 6
            Assert.AreEqual(6, DamageCalculator.ComputeDamage(8, 1f, 4, 1.0f, crit: true));
        }

        [Test]
        public void Skill_HighVariance_RoundsToNine()
        {
            // (8 * 1.5 - 4) * 1.1 = 8.8 -> 9
            Assert.AreEqual(9, DamageCalculator.ComputeDamage(8, 1.5f, 4, 1.1f, crit: false));
        }

        [Test]
        public void Skill_HighVariance_Crit_RoundsToThirteen()
        {
            // 8.8 * 1.5 = 13.2 -> 13
            Assert.AreEqual(13, DamageCalculator.ComputeDamage(8, 1.5f, 4, 1.1f, crit: true));
        }

        [Test]
        public void EnemyAttack_LowVariance_RoundsToFive()
        {
            // (11 * 1.0 - 5) * 0.9 = 5.4 -> 5
            Assert.AreEqual(5, DamageCalculator.ComputeDamage(11, 1f, 5, 0.9f, crit: false));
        }

        [Test]
        public void WeakHit_ClampsToMinimumOne()
        {
            // (6 - 5) * 0.9 = 0.9 -> 1
            Assert.AreEqual(1, DamageCalculator.ComputeDamage(6, 1f, 5, 0.9f, crit: false));
        }

        [Test]
        public void NonPositiveRawDamage_ClampsToMinimumOne()
        {
            // Defense >= attack would give <= 0; must never fall below 1.
            Assert.AreEqual(DamageCalculator.MinimumDamage, DamageCalculator.ComputeDamage(5, 1f, 20, 1f, crit: false));
        }

        [Test]
        public void Compute_NeverBelowMinimum_AcrossManyRolls()
        {
            var rng = new Random(1234);
            for (int i = 0; i < 500; i++)
            {
                int dmg = DamageCalculator.Compute(8, 1f, 30, rng, out _);
                Assert.GreaterOrEqual(dmg, DamageCalculator.MinimumDamage);
            }
        }

        [Test]
        public void Compute_NullRng_Throws()
        {
            Assert.Throws<ArgumentNullException>(() => DamageCalculator.Compute(8, 1f, 4, null, out _));
        }
    }
}
