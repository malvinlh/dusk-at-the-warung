using UnityEngine;

namespace DuskWarung.World
{
    /// <summary>Anything the player can interact with by facing it and pressing interact.</summary>
    public interface IInteractable
    {
        /// <summary>Invoked when the player interacts while facing this object.</summary>
        /// <param name="interactor">The GameObject that initiated the interaction (the player).</param>
        void Interact(GameObject interactor);
    }
}
