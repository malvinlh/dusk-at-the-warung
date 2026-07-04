using UnityEngine;

namespace DuskWarung.Core
{
    /// <summary>
    /// Title screen: blinks a "press to start" prompt, then on the first key/click resets the
    /// session and fades into the overworld.
    /// </summary>
    public class TitleScreenController : MonoBehaviour
    {
        [SerializeField] private SceneLoader loader;
        [SerializeField] private string firstSceneName = "Overworld";

        [SerializeField, Tooltip("Optional prompt object blinked on and off.")]
        private GameObject pressStartPrompt;

        [SerializeField] private float promptBlinkInterval = 0.6f;

        private bool _started;
        private float _blinkTimer;

        private void Update()
        {
            BlinkPrompt();

            if (_started)
            {
                return;
            }

            if (Input.GetKeyDown(KeyCode.Space) || Input.GetMouseButtonDown(0))
            {
                _started = true;
                GameSession.Reset();

                if (loader != null)
                {
                    loader.LoadWithFade(firstSceneName);
                }
            }
        }

        private void BlinkPrompt()
        {
            if (pressStartPrompt == null)
            {
                return;
            }

            _blinkTimer += Time.deltaTime;
            if (_blinkTimer >= promptBlinkInterval)
            {
                _blinkTimer = 0f;
                pressStartPrompt.SetActive(!pressStartPrompt.activeSelf);
            }
        }
    }
}
