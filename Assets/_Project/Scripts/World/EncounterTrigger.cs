using DuskWarung.Battle;
using DuskWarung.Core;
using UnityEngine;

namespace DuskWarung.World
{
    /// <summary>
    /// A trigger volume (e.g. the grove mouth) that queues an encounter and loads the battle
    /// scene once when the player enters. Also used as the free-roam fallback if the player
    /// walks into the grove without the scripted cutscene.
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

        private bool _fired;

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
