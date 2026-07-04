using DG.Tweening;
using TMPro;
using UnityEngine;

namespace DuskWarung.Battle.View
{
    /// <summary>
    /// A world-space floating combat number. Spawned per hit, it drifts up while fading, then
    /// destroys itself. Deliberately not pooled — a battle produces only a handful, so pooling
    /// would be premature optimisation.
    /// </summary>
    public class DamagePopup : MonoBehaviour
    {
        [SerializeField, Tooltip("The TextMeshPro label (world-space TMP).")]
        private TMP_Text label;

        [SerializeField] private float floatDistance = 1f;
        [SerializeField] private float duration = 0.8f;

        [Header("Colours")]
        [SerializeField] private Color damageColor = Color.white;
        [SerializeField] private Color critColor = new Color(1f, 0.85f, 0.2f);
        [SerializeField] private Color healColor = new Color(0.45f, 1f, 0.55f);
        [SerializeField] private Color neutralColor = new Color(0.85f, 0.85f, 0.85f);

        [SerializeField, Tooltip("Extra scale applied to critical-hit numbers.")]
        private float critScale = 1.3f;

        /// <summary>Shows a damage number (larger and gold on a crit).</summary>
        public void ShowDamage(int amount, bool crit)
        {
            Play(amount.ToString(), crit ? critColor : damageColor, crit ? critScale : 1f);
        }

        /// <summary>Shows a heal amount in green.</summary>
        public void ShowHeal(int amount)
        {
            Play("+" + amount, healColor, 1f);
        }

        /// <summary>Shows arbitrary text (e.g. "Miss!", "Fled!") in a neutral colour.</summary>
        public void ShowText(string text)
        {
            Play(text, neutralColor, 1f);
        }

        private void Play(string text, Color color, float scale)
        {
            if (label != null)
            {
                label.text = text;
                label.color = color;
                label.alpha = 1f;
            }

            transform.localScale = Vector3.one * scale;

            Sequence sequence = DOTween.Sequence();
            sequence.Append(transform.DOMoveY(transform.position.y + floatDistance, duration).SetEase(Ease.OutQuad));
            if (label != null)
            {
                // Tween the TMP alpha directly (works without the optional DOTween TMP module).
                sequence.Join(DOTween.To(() => label.alpha, a => label.alpha = a, 0f, duration).SetEase(Ease.InQuad));
            }

            sequence.OnComplete(() => Destroy(gameObject));
        }
    }
}
