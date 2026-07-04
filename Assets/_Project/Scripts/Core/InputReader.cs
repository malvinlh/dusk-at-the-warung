using UnityEngine;

namespace DuskWarung.Core
{
    /// <summary>
    /// The single place the game reads player input, so nothing else touches
    /// <see cref="Input"/> directly. Control can be gated (cutscenes/battle) and overridden
    /// with scripted axes (self-driving cutscenes), which keeps movement code identical
    /// whether a human or a script is "holding the stick".
    /// </summary>
    public class InputReader : MonoBehaviour
    {
        /// <summary>Raw movement axis in [-1, 1] per component, from WASD/Arrows.</summary>
        public Vector2 MoveAxis { get; private set; }

        /// <summary>True on the frame the interact (Space) button is pressed.</summary>
        public bool InteractPressed { get; private set; }

        /// <summary>Whether player-driven input is currently accepted.</summary>
        public bool ControlEnabled => _enabled;

        private bool _enabled = true;
        private bool _useScripted;
        private Vector2 _scripted;

        /// <summary>Enables or disables player-driven input (cutscene/battle lock).</summary>
        public void SetEnabled(bool value) => _enabled = value;

        /// <summary>Overrides movement with a scripted axis for a self-driving cutscene.</summary>
        public void SetScriptedInput(Vector2 axis)
        {
            _useScripted = true;
            _scripted = axis;
        }

        /// <summary>Stops scripted movement and returns control to normal reads.</summary>
        public void ClearScriptedInput()
        {
            _useScripted = false;
            _scripted = Vector2.zero;
        }

        private void Update()
        {
            if (_useScripted)
            {
                MoveAxis = _scripted;
                InteractPressed = false;
                return;
            }

            if (!_enabled)
            {
                MoveAxis = Vector2.zero;
                InteractPressed = false;
                return;
            }

            MoveAxis = new Vector2(Input.GetAxisRaw("Horizontal"), Input.GetAxisRaw("Vertical"));
            InteractPressed = Input.GetKeyDown(KeyCode.Space);
        }
    }
}
