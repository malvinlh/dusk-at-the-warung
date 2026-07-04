using DuskWarung.Battle;
using DuskWarung.Core;
using DuskWarung.FungusCommands;
using UnityEngine;

namespace DuskWarung.World
{
    /// <summary>
    /// Handles re-entry to the overworld after a battle. On load it inspects the last battle
    /// outcome and, on a defeat or flee, repositions the player and plays a matching return
    /// block, then consumes the result so it fires only once.
    /// </summary>
    public class OverworldFlowController : MonoBehaviour
    {
        [SerializeField, Tooltip("The player transform to reposition on return.")]
        private Transform playerTransform;

        [SerializeField] private FungusBridge fungus;

        [Header("Return spawns")]
        [SerializeField, Tooltip("Where the player wakes after a defeat (e.g. Bu Sari's bench).")]
        private Transform defeatSpawn;

        [SerializeField, Tooltip("Where the player reappears after fleeing (e.g. near the warung).")]
        private Transform fleeSpawn;

        [Header("Return dialog blocks")]
        [SerializeField] private string defeatBlock = "Defeat";
        [SerializeField] private string fleeBlock = "";

        private void Start()
        {
            switch (GameSession.LastBattleResult)
            {
                case BattleOutcome.Defeat:
                    PlaceAt(defeatSpawn);
                    PlayBlock(defeatBlock);
                    break;

                case BattleOutcome.Fled:
                    PlaceAt(fleeSpawn);
                    PlayBlock(fleeBlock);
                    break;
            }

            // Consume so a later, non-battle load of this scene does not replay the reaction.
            GameSession.LastBattleResult = BattleOutcome.None;
        }

        private void PlaceAt(Transform spawn)
        {
            if (playerTransform != null && spawn != null)
            {
                playerTransform.position = spawn.position;
            }
        }

        private void PlayBlock(string block)
        {
            if (fungus != null && !string.IsNullOrEmpty(block))
            {
                fungus.ExecuteBlock(block);
            }
        }
    }
}
