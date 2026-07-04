using DuskWarung.FungusCommands;
using UnityEngine;

namespace DuskWarung.World
{
    /// <summary>An NPC that runs a named Fungus block when the player interacts with it.</summary>
    public class NpcInteractable : MonoBehaviour, IInteractable
    {
        [SerializeField, Tooltip("Bridge to the scene's Fungus Flowchart.")]
        private FungusBridge fungus;

        [SerializeField, Tooltip("Name of the Fungus block to run on interaction.")]
        private string blockName = "TalkToSari";

        /// <inheritdoc/>
        public void Interact(GameObject interactor)
        {
            if (fungus != null)
            {
                fungus.ExecuteBlock(blockName);
            }
        }
    }
}
