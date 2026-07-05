using System.Collections;
using DG.Tweening;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace DuskWarung.Core
{
    /// <summary>
    /// The one deliberately-persistent object in the game: a full-screen black fade overlay that survives
    /// scene loads (<see cref="Object.DontDestroyOnLoad"/>) at the maximum sorting order. Because a single
    /// overlay covers the WHOLE transition — old scene fade-out, the load itself, and the new scene's first
    /// frames — it eliminates the one-frame flashes that per-scene fade canvases leave behind (a dialog box
    /// briefly showing, an un-faded first frame, etc.). Cross-scene visual continuity is the textbook reason
    /// a singleton is justified here; it is auto-created, so no scene ever has to wire it.
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

        /// <summary>
        /// True while a fade-out → load → fade-in is in progress. Incoming scenes wait on this before
        /// showing their first dialog, so UI only ever appears on a fully-revealed, stable scene (rather
        /// than racing the fade-in and popping into view). See <see cref="Battle.View.BattleController"/>.
        /// </summary>
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

            // Hold the cover for a couple of frames so the new scene's first Awake/Start (and any dialog it
            // opens) happens entirely behind black.
            yield return null;
            yield return null;

            _group.DOFade(0f, fadeDuration).SetUpdate(true);
            yield return new WaitForSecondsRealtime(fadeDuration);

            _group.blocksRaycasts = false;
            _busy = false;
        }
    }
}
