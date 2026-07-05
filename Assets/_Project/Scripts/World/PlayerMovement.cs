using DuskWarung.Core;
using UnityEngine;

namespace DuskWarung.World
{
    /// <summary>
    /// Top-down movement driven by the <see cref="InputReader"/>, plus a control gate that
    /// cutscenes and battles reuse. Because it reads movement from the InputReader (not
    /// <see cref="Input"/> directly), the very same code moves the avatar whether a human or
    /// a cutscene script is supplying the axis.
    /// </summary>
    [RequireComponent(typeof(Rigidbody2D))]
    public class PlayerMovement : MonoBehaviour
    {
        [SerializeField, Tooltip("World units per second.")]
        private float moveSpeed = 3.5f;

        [SerializeField, Tooltip("Assign the scene's InputReader.")]
        private InputReader input;

        [SerializeField, Tooltip("Assign the character Animator (params: MoveX, MoveY, Speed).")]
        private Animator animator;

        private Rigidbody2D _rb;
        private bool _hasMoveX;
        private bool _hasMoveY;
        private bool _hasSpeed;

        private static readonly int MoveXHash = Animator.StringToHash("MoveX");
        private static readonly int MoveYHash = Animator.StringToHash("MoveY");
        private static readonly int SpeedHash = Animator.StringToHash("Speed");

        /// <summary>The last direction the player faced (defaults to down/south).</summary>
        public Vector2 Facing { get; private set; } = Vector2.down;

        /// <summary>
        /// Whether the human currently has control. False while a cutscene or battle has locked the avatar.
        /// Systems that should only react to *free-roam* movement (e.g. <see cref="EncounterTrigger"/>) read
        /// this so they don't fire during a scripted cutscene, whose own flow drives the outcome.
        /// </summary>
        public bool ControlEnabled { get; private set; } = true;

        /// <summary>Enables or disables player control (used for cutscene/battle locks).</summary>
        public void SetControlEnabled(bool value)
        {
            ControlEnabled = value;
            if (input != null)
            {
                input.SetEnabled(value);
            }
        }

        /// <summary>Feeds a scripted movement axis for self-driving cutscenes.</summary>
        public void SetScriptedInput(Vector2 axis)
        {
            if (input != null)
            {
                input.SetScriptedInput(axis);
            }
        }

        /// <summary>Stops scripted movement and hands control back to the player.</summary>
        public void ClearScriptedInput()
        {
            if (input != null)
            {
                input.ClearScriptedInput();
            }
        }

        private void Awake()
        {
            _rb = GetComponent<Rigidbody2D>();
            CacheAnimatorParameters();
        }

        private void FixedUpdate()
        {
            Vector2 axis = input != null ? input.MoveAxis : Vector2.zero;
            Vector2 move = axis.sqrMagnitude > 1f ? axis.normalized : axis; // Diagonals aren't faster.

            _rb.MovePosition(_rb.position + move * (moveSpeed * Time.fixedDeltaTime));

            if (move.sqrMagnitude > 0.0001f)
            {
                Facing = move.normalized;
            }

            UpdateAnimator(move);
        }

        private void UpdateAnimator(Vector2 move)
        {
            if (animator == null)
            {
                return;
            }

            if (_hasMoveX)
            {
                animator.SetFloat(MoveXHash, Facing.x);
            }

            if (_hasMoveY)
            {
                animator.SetFloat(MoveYHash, Facing.y);
            }

            if (_hasSpeed)
            {
                animator.SetFloat(SpeedHash, move.sqrMagnitude);
            }
        }

        private void CacheAnimatorParameters()
        {
            if (animator == null)
            {
                return;
            }

            // Cache which params exist so we never spam warnings before the Animator is wired.
            foreach (AnimatorControllerParameter parameter in animator.parameters)
            {
                if (parameter.name == "MoveX") _hasMoveX = true;
                else if (parameter.name == "MoveY") _hasMoveY = true;
                else if (parameter.name == "Speed") _hasSpeed = true;
            }
        }
    }
}
