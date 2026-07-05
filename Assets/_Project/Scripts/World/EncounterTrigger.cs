using DuskWarung.Battle;
using DuskWarung.Core;
using DuskWarung.FungusCommands;
using UnityEngine;

namespace DuskWarung.World
{
    /// <summary>
    /// A trigger volume (e.g. the grove mouth) that queues an encounter and loads the battle
    /// scene once when the player enters. Also used as the free-roam fallback if the player
    /// walks into the grove without the scripted cutscene.
    ///
    /// Progression is gated by a story flag: if <see cref="requiredFlag"/> is set and not yet raised on
    /// <see cref="GameSession"/>, the trigger holds and (optionally) plays a nudge block instead of starting
    /// the battle. This keeps the intended narrative order (talk to Bu Sari first) without hard-coding the
    /// dependency — the gate reads the event-flag layer, which scales to any number of quests.
    /// </summary>
    [RequireComponent(typeof(Collider2D))]
    public class EncounterTrigger : MonoBehaviour
    {
        [SerializeField, Tooltip("Encounter to fight when entered.")]
        private EncounterSO encounter;

        [SerializeField, Tooltip("Scene loader that performs the fade + load.")]
        private SceneLoader loader;

        [SerializeField, Tooltip("Name of the battle scene to load.")]
        private string battleSceneName = "Battle";

        [SerializeField, Tooltip("Tag the entering collider must have.")]
        private string playerTag = "Player";

        [Header("Gating (optional)")]
        [SerializeField, Tooltip("If set, the battle only starts once this story flag has been raised (e.g. \"met_bu_sari\").")]
        private string requiredFlag;

        [SerializeField, Tooltip("Optional Fungus bridge used to play the nudge block when gated.")]
        private FungusBridge fungus;

        [SerializeField, Tooltip("Optional block played (once) when the player reaches the gate without the required flag.")]
        private string gatedHintBlock;

        private bool _fired;
        private bool _hintShown;

        private void Reset()
        {
            // Make the attached collider a trigger by default for convenience.
            var col = GetComponent<Collider2D>();
            if (col != null)
            {
                col.isTrigger = true;
            }
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            if (_fired || !other.CompareTag(playerTag))
            {
                return;
            }

            // Only react to FREE-ROAM movement. During a scripted cutscene the avatar is under scripted
            // control and the cutscene's own Start Encounter command drives the battle; if this physical
            // trigger also fired here it would start the fade while the approach dialogue (GroveApproach) is
            // still playing — the "dialogue flash". So when control is locked, stay out of the way.
            var mover = other.GetComponent<PlayerMovement>();
            if (mover != null && !mover.ControlEnabled)
            {
                return;
            }

            // Gate: hold the battle until the prerequisite flag is raised (e.g. after talking to Bu Sari).
            if (!string.IsNullOrEmpty(requiredFlag) && !GameSession.HasFlag(requiredFlag))
            {
                if (!_hintShown && fungus != null && !string.IsNullOrEmpty(gatedHintBlock))
                {
                    _hintShown = true;
                    fungus.ExecuteBlock(gatedHintBlock);
                }

                return;
            }

            _fired = true;
            GameSession.PendingEncounter = encounter;
            GameSession.ReturnPosition = other.transform.position;
            GameSession.HasReturnPosition = true;

            if (loader != null)
            {
                loader.LoadWithFade(battleSceneName);
            }
        }
    }
}
