using System.Collections;
using DG.Tweening;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace DuskWarung.Core
{
    /// <summary>
    /// Persistent full-screen fade overlay (<see cref="Object.DontDestroyOnLoad"/>) at max sorting order.
    /// One overlay covers the whole transition — fade-out, load, and the new scene's first frames — so
    /// per-scene fade flashes can't occur. Auto-created; no scene wires it.
    /// </summary>
    public class SceneTransition : MonoBehaviour
    {
        private static SceneTransition _instance;

        /// <summary>The persistent transition overlay, created on first access.</summary>
        public static SceneTransition Instance
        {
            get
            {
                if (_instance == null)
                {
                    Create();
                }

                return _instance;
            }
        }

        private CanvasGroup _group;
        private bool _busy;

        /// <summary>True while a transition is running; incoming scenes wait on it before showing dialog.</summary>
        public bool IsBusy => _busy;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void Bootstrap() => Create();

        private static void Create()
        {
            if (_instance != null)
            {
                return;
            }

            var go = new GameObject("[SceneTransition]");
            DontDestroyOnLoad(go);
            _instance = go.AddComponent<SceneTransition>();
            _instance.Build();
        }

        private void Build()
        {
            var canvas = gameObject.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = short.MaxValue; // above everything, including Fungus dialogs
            gameObject.AddComponent<CanvasScaler>();
            gameObject.AddComponent<GraphicRaycaster>();

            var imageGo = new GameObject("Fade", typeof(RectTransform));
            imageGo.transform.SetParent(transform, false);
            var image = imageGo.AddComponent<Image>();
            image.color = Color.black;
            RectTransform rt = image.rectTransform;
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;

            _group = imageGo.AddComponent<CanvasGroup>();
            _group.alpha = 0f;
            _group.blocksRaycasts = false;
        }

        /// <summary>Fades to black, loads <paramref name="sceneName"/>, then fades back in.</summary>
        public void Transition(string sceneName, float fadeDuration = 0.35f)
        {
            if (_busy)
            {
                return; // ignore overlapping requests
            }

            StartCoroutine(TransitionRoutine(sceneName, fadeDuration));
        }

        private IEnumerator TransitionRoutine(string sceneName, float fadeDuration)
        {
            _busy = true;
            _group.blocksRaycasts = true;

            _group.DOFade(1f, fadeDuration).SetUpdate(true);
            yield return new WaitForSecondsRealtime(fadeDuration);

            Time.timeScale = 1f; // restore in case a battle hit-stop was active

            AsyncOperation op = SceneManager.LoadSceneAsync(sceneName);
            while (op != null && !op.isDone)
            {
                yield return null;
            }

            // Hold black a couple of frames so the new scene's Awake/Start runs behind the cover.
            yield return null;
            yield return null;

            _group.DOFade(0f, fadeDuration).SetUpdate(true);
            yield return new WaitForSecondsRealtime(fadeDuration);

            _group.blocksRaycasts = false;
            _busy = false;
        }
    }
}
