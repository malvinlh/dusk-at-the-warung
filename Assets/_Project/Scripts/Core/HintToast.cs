using DG.Tweening;
using UnityEngine;

namespace DuskWarung.Core
{
    /// <summary>One-shot hint that fades in on <see cref="Start"/>, holds, then fades out and disables itself
    /// (the overworld's "WASD to move" reminder). Adds a <see cref="CanvasGroup"/> if one is not present.</summary>
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
