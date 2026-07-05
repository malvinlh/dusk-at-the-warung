using UnityEngine;

namespace DuskWarung.Core
{
    /// <summary>
    /// Thin per-scene entry point for scene changes; delegates the fade + load to the persistent
    /// <see cref="SceneTransition"/>. The optional local overlay is kept transparent for backward compatibility.
    /// </summary>
    public class SceneLoader : MonoBehaviour
    {
        [SerializeField, Tooltip("Legacy per-scene overlay; kept transparent (the persistent SceneTransition fades).")]
        private CanvasGroup fade;

        private void Awake()
        {
            if (fade != null)
            {
                fade.alpha = 0f;
                fade.blocksRaycasts = false;
            }
        }

        /// <summary>Fades to black (via the persistent transition overlay) and loads <paramref name="sceneName"/>.</summary>
        public void LoadWithFade(string sceneName)
        {
            SceneTransition.Instance.Transition(sceneName);
        }
    }
}
