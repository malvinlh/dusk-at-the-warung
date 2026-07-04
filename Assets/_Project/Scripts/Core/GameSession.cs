using System.Collections.Generic;
using DuskWarung.Battle;
using UnityEngine;

namespace DuskWarung.Core
{
    /// <summary>
    /// The one sanctioned global: static data that must survive a scene load, namely the
    /// hand-off between the overworld and the battle scene. Deliberately a plain static
    /// class rather than a singleton MonoBehaviour — there is no per-frame behaviour to host,
    /// and this keeps the cross-scene state explicit and trivially resettable.
    /// </summary>
    public static class GameSession
    {
        /// <summary>Encounter queued by a trigger before the battle scene loads.</summary>
        public static EncounterSO PendingEncounter;

        /// <summary>Result written by the battle scene, read when the overworld resumes.</summary>
        public static BattleOutcome LastBattleResult = BattleOutcome.None;

        /// <summary>Where to place the player when returning to the overworld.</summary>
        public static Vector3 ReturnPosition;

        /// <summary>Whether a return position has been set (false on a fresh overworld load).</summary>
        public static bool HasReturnPosition;

        /// <summary>Simple story flags (e.g. "talked_to_sari", "beat_genderuwo").</summary>
        public static readonly HashSet<string> StoryFlags = new HashSet<string>();

        /// <summary>Clears all session state. Call when starting a brand-new playthrough.</summary>
        public static void Reset()
        {
            PendingEncounter = null;
            LastBattleResult = BattleOutcome.None;
            ReturnPosition = Vector3.zero;
            HasReturnPosition = false;
            StoryFlags.Clear();
        }
    }
}
