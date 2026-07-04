using UnityEngine;

namespace DuskWarung.Core
{
    /// <summary>
    /// End screen: after a short settle delay, waits for a key/click and returns to the title
    /// so the vertical slice can be replayed.
    /// </summary>
    public class EndScreenController : MonoBehaviour
    {
        [SerializeField] private SceneLoader loader;
        [SerializeField] private string titleSceneName = "Title";

        [SerializeField, Tooltip("Ignore input for this long so the incoming transition settles first.")]
        private float inputDelay = 0.6f;

        private float _timer;
        private bool _done;

        private void Update()
        {
            _timer += Time.deltaTime;
            if (_done || _timer < inputDelay)
            {
                return;
            }

            if (Input.GetKeyDown(KeyCode.Space) || Input.GetMouseButtonDown(0))
            {
                _done = true;
                if (loader != null)
                {
                    loader.LoadWithFade(titleSceneName);
                }
            }
        }
    }
}
