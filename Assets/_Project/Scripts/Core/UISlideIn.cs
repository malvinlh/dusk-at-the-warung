using DG.Tweening;
using UnityEngine;

namespace DuskWarung.Core
{
    /// <summary>Slides and fades a UI element into place on <see cref="Start"/> so panels animate in instead
    /// of snapping. Adds a <see cref="CanvasGroup"/> if one is not present.</summary>
    [RequireComponent(typeof(RectTransform))]
    public class UISlideIn : MonoBehaviour
    {
        [SerializeField, Tooltip("Pixel offset the element slides in FROM (e.g. (0,-40) rises from below).")]
        private Vector2 fromOffset = new Vector2(0f, -40f);

        [SerializeField] private float duration = 0.35f;
        [SerializeField] private float delay;
        [SerializeField] private Ease ease = Ease.OutCubic;

        private void Start()
        {
            var rect = (RectTransform)transform;
            var group = GetComponent<CanvasGroup>();
            if (group == null)
            {
                group = gameObject.AddComponent<CanvasGroup>();
            }

            Vector2 target = rect.anchoredPosition;
            rect.anchoredPosition = target + fromOffset;
            group.alpha = 0f;

            rect.DOAnchorPos(target, duration).SetDelay(delay).SetEase(ease).SetUpdate(true);
            group.DOFade(1f, duration).SetDelay(delay).SetUpdate(true);
        }
    }
}
