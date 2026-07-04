using DuskWarung.Core;
using UnityEngine;

namespace DuskWarung.World
{
    /// <summary>
    /// On the interact press, probes a small circle just ahead of the player (in its facing
    /// direction) and triggers the first <see cref="IInteractable"/> it finds.
    /// </summary>
    public class PlayerInteractor : MonoBehaviour
    {
        [SerializeField, Tooltip("Assign the scene's InputReader.")]
        private InputReader input;

        [SerializeField, Tooltip("Assign the PlayerMovement to read facing from.")]
        private PlayerMovement movement;

        [SerializeField, Tooltip("How far ahead of the player to probe.")]
        private float reach = 0.6f;

        [SerializeField, Tooltip("Radius of the interaction probe.")]
        private float radius = 0.25f;

        [SerializeField, Tooltip("Layers to consider (any non-interactable overlaps are ignored anyway).")]
        private LayerMask interactableMask = ~0;

        private void Update()
        {
            if (input == null || !input.InteractPressed)
            {
                return;
            }

            Vector2 origin = (Vector2)transform.position + CurrentFacing() * reach;
            Collider2D hit = Physics2D.OverlapCircle(origin, radius, interactableMask);
            if (hit != null && hit.TryGetComponent(out IInteractable target))
            {
                target.Interact(gameObject);
            }
        }

        private Vector2 CurrentFacing() => movement != null ? movement.Facing : Vector2.down;

        private void OnDrawGizmosSelected()
        {
            Gizmos.color = Color.cyan;
            Vector2 origin = (Vector2)transform.position + CurrentFacing() * reach;
            Gizmos.DrawWireSphere(origin, radius);
        }
    }
}
