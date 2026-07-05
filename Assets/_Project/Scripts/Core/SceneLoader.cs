using UnityEngine;

namespace DuskWarung.Core
{
    /// <summary>
    /// Thin per-scene entry point for scene changes. It delegates the actual fade + load to the persistent
    /// <see cref="SceneTransition"/> overlay, which covers both scenes' seams and so avoids the one-frame
    /// flashes a per-scene fader leaves. The optional local overlay (kept from earlier scene setups) is
    /// simply held transparent — the persistent overlay does the fading now. Existing Inspector wiring
    /// (EncounterTrigger/BattleController/Title/End all reference a SceneLoader) keeps working unchanged.
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
