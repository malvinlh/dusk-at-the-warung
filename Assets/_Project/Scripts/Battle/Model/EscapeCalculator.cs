using System;
using UnityEngine;

namespace DuskWarung.Battle
{
    /// <summary>
    /// Deterministic flee-chance math for the Run command. Kept static and pure so the
    /// odds are testable and tunable in one place ("Bold strategy. Rarely works.").
    /// </summary>
    public static class EscapeCalculator
    {
        /// <summary>Flee chance when both battlers are equally fast.</summary>
        public const float BaseChance = 0.35f;

        /// <summary>How strongly a speed advantage shifts the chance (± this at the extremes).</summary>
        public const float SpeedInfluence = 0.5f;

        /// <summary>
        /// Chance [0..1] that the actor escapes, nudged up when the actor is faster than the
        /// target and down when slower. Equal speeds give exactly <see cref="BaseChance"/>.
        /// </summary>
        public static float ComputeChance(int actorSpeed, int targetSpeed)
        {
            int total = Mathf.Max(1, actorSpeed + targetSpeed);
            float ratio = actorSpeed / (float)total; // 0..1, 0.5 when equal
            return Mathf.Clamp01(BaseChance + SpeedInfluence * (ratio - 0.5f));
        }

        /// <summary>Rolls a flee attempt against <see cref="ComputeChance"/>.</summary>
        /// <param name="chance">The computed escape chance that was rolled against.</param>
        public static bool TryEscape(int actorSpeed, int targetSpeed, System.Random rng, out float chance)
        {
            if (rng == null)
            {
                throw new ArgumentNullException(nameof(rng));
            }

            chance = ComputeChance(actorSpeed, targetSpeed);
            return rng.NextDouble() < chance;
        }
    }
}
