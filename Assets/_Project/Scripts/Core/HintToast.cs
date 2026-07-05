using DG.Tweening;
using UnityEngine;

namespace DuskWarung.Core
{
    /// <summary>
    /// A one-shot on-screen hint: fades in on <see cref="Start"/>, holds, then fades out and disables itself.
    /// Used for the unobtrusive "WASD to move" reminder when the overworld first loads, so controls are
    /// discoverable in-game without a persistent HUD element. Adds its own <see cref="CanvasGroup"/> if needed.
    /// </summary>
    [RequireComponent(typeof(RectTransform))]
    public class HintToast : MonoBehaviour
    {
        [SerializeField] private float fadeIn = 0.4f;
        [SerializeField, Tooltip("Seconds fully visible before fading out.")]
        private float hold = 3.5f;
        [SerializeField] private float fadeOut = 0.6f;

        private void Start()
        {
            var group = GetComponent<CanvasGroup>();
            if (group == null)
            {
                group = gameObject.AddComponent<CanvasGroup>();
            }

            group.alpha = 0f;
            group.blocksRaycasts = false;
            group.interactable = false;

            DOTween.Sequence()
                .Append(group.DOFade(1f, fadeIn))
                .AppendInterval(hold)
                .Append(group.DOFade(0f, fadeOut))
                .AppendCallback(() => gameObject.SetActive(false));
        }
    }
}
