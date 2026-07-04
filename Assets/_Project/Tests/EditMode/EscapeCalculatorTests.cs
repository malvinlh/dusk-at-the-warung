using NUnit.Framework;

namespace DuskWarung.Battle.Tests
{
    /// <summary>Verifies the flee-chance curve: base at parity, higher when faster, always in [0,1].</summary>
    public class EscapeCalculatorTests
    {
        [Test]
        public void EqualSpeeds_GiveBaseChance()
        {
            Assert.AreEqual(EscapeCalculator.BaseChance, EscapeCalculator.ComputeChance(6, 6), 0.0001f);
        }

        [Test]
        public void FasterActor_HasHigherChanceThanSlower()
        {
            float slower = EscapeCalculator.ComputeChance(4, 8);
            float even = EscapeCalculator.ComputeChance(6, 6);
            float faster = EscapeCalculator.ComputeChance(12, 4);

            Assert.Less(slower, even);
            Assert.Greater(faster, even);
        }

        [Test]
        public void Chance_IsAlwaysWithinZeroToOne()
        {
            Assert.GreaterOrEqual(EscapeCalculator.ComputeChance(1, 1000), 0f);
            Assert.LessOrEqual(EscapeCalculator.ComputeChance(1000, 1), 1f);
        }
    }
}
