using System.Collections;
using DG.Tweening;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace DuskWarung.Core
{
    /// <summary>
    /// Per-scene screen fader and scene changer. Each scene owns a full-screen black Image
    /// (a <see cref="CanvasGroup"/>): this component reveals the scene on start (fade from
    /// black) and covers it before loading the next one (fade to black), giving a seamless
    /// hand-off with no hard cuts and no persistent objects.
    /// </summary>
    public class SceneLoader : MonoBehaviour
    {
        [SerializeField, Tooltip("Full-screen black Image's CanvasGroup used for the fade overlay.")]
        private CanvasGroup fade;

        [SerializeField, Tooltip("Seconds for a single fade in or out.")]
        private float fadeDuration = 0.35f;

        [SerializeField, Tooltip("Fade the screen in from black when this scene starts.")]
        private bool fadeInOnStart = true;

        private bool _isLoading;

        private void Awake()
        {
            if (fade != null)
            {
                // Start fully covered so the first frame never flashes un-faded content.
                fade.alpha = fadeInOnStart ? 1f : 0f;
                fade.blocksRaycasts = fadeInOnStart;
            }
        }

        private void Start()
        {
            if (fadeInOnStart && fade != null)
            {
                fade.DOFade(0f, fadeDuration).SetUpdate(true)
                    .OnComplete(() => fade.blocksRaycasts = false);
            }
        }

        /// <summary>Fades to black and loads <paramref name="sceneName"/>.</summary>
        public void LoadWithFade(string sceneName)
        {
            if (_isLoading)
            {
                return; // Ignore repeat requests once a transition is under way.
            }

            _isLoading = true;
            StartCoroutine(LoadRoutine(sceneName));
        }

        private IEnumerator LoadRoutine(string sceneName)
        {
            if (fade != null)
            {
                fade.blocksRaycasts = true;
                fade.DOFade(1f, fadeDuration).SetUpdate(true);
                yield return new WaitForSecondsRealtime(fadeDuration);
            }

            // Restore normal time in case we transition while a hit-stop was active.
            Time.timeScale = 1f;

            AsyncOperation op = SceneManager.LoadSceneAsync(sceneName);
            while (op != null && !op.isDone)
            {
                yield return null;
            }
        }
    }
}
