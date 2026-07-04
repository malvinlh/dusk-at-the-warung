using System;
using UnityEngine;

namespace DuskWarung.Battle
{
    /// <summary>
    /// Deterministic damage math, kept static and free of any battle state so it can be
    /// unit-tested in isolation. The random roll (variance + crit) is separated from the
    /// pure formula so tests can assert exact numbers for fixed inputs.
    /// </summary>
    public static class DamageCalculator
    {
        /// <summary>Lowest random damage multiplier.</summary>
        public const float VarianceMin = 0.9f;

        /// <summary>Highest random damage multiplier.</summary>
        public const float VarianceMax = 1.1f;

        /// <summary>Probability [0..1] that a hit is a critical.</summary>
        public const float CritChance = 0.05f;

        /// <summary>Damage multiplier applied on a critical hit.</summary>
        public const float CritMultiplier = 1.5f;

        /// <summary>Minimum damage any connecting hit deals.</summary>
        public const int MinimumDamage = 1;

        /// <summary>
        /// The pure formula: <c>damage = max(1, round((attack * power - defense) * variance))</c>,
        /// multiplied by <see cref="CritMultiplier"/> when <paramref name="crit"/> is true.
        /// Fully deterministic — no randomness.
        /// </summary>
        public static int ComputeDamage(int attack, float power, int defense, float variance, bool crit)
        {
            float raw = (attack * power - defense) * variance;
            if (crit)
            {
                raw *= CritMultiplier;
            }

            return Mathf.Max(MinimumDamage, Mathf.RoundToInt(raw));
        }

        /// <summary>
        /// Rolls variance and a critical from <paramref name="rng"/>, then applies the formula.
        /// </summary>
        /// <param name="crit">Set to true when the roll produced a critical hit.</param>
        public static int Compute(int attack, float power, int defense, System.Random rng, out bool crit)
        {
            if (rng == null)
            {
                throw new ArgumentNullException(nameof(rng));
            }

            float variance = VarianceMin + (float)rng.NextDouble() * (VarianceMax - VarianceMin);
            crit = rng.NextDouble() < CritChance;
            return ComputeDamage(attack, power, defense, variance, crit);
        }
    }
}
