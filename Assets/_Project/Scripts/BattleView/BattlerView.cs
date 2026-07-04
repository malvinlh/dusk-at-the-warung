using System.Collections;
using System.Collections.Generic;
using Cinemachine;
using DG.Tweening;
using UnityEngine;

namespace DuskWarung.Battle.View
{
    /// <summary>
    /// Presents a single battler: shows its sprite, fires Animator triggers, and plays the
    /// juicy three-beat hit reaction (recoil + white flash + camera shake + hit-stop). It owns
    /// only presentation/timing; it never mutates the model. Each routine returns an
    /// <see cref="IEnumerator"/> so the <see cref="BattleController"/> can sequence beats
    /// across two battlers and report completion back to the model.
    /// </summary>
    public class BattlerView : MonoBehaviour
    {
        // Animator trigger names (present only if the wired AC_Battler declares them).
        private const string AttackTrigger = "PlayAttack";
        private const string HitTrigger = "PlayHit";
        private const string VictoryTrigger = "PlayVictory";
        private const string DownTrigger = "PlayDown";
        private const string ItemTrigger = "PlayItem";

        [Header("References")]
        [SerializeField, Tooltip("The battler's SpriteRenderer (should use a DuskWarung/SpriteFlash material for the flash).")]
        private SpriteRenderer spriteRenderer;

        [SerializeField, Tooltip("Optional Animator (params: triggers PlayAttack/PlayHit/PlayVictory/PlayDown/PlayItem).")]
        private Animator animator;

        [SerializeField, Tooltip("Optional impulse source used to shake the camera when a hit lands on this battler.")]
        private CinemachineImpulseSource impulseSource;

        [Header("Flash")]
        [SerializeField] private Color hitFlashColor = Color.white;
        [SerializeField] private Color healFlashColor = new Color(0.45f, 1f, 0.55f);
        [SerializeField] private float flashDuration = 0.09f;

        [Header("Motion")]
        [SerializeField] private float lungeDistance = 0.5f;
        [SerializeField] private float lungeDuration = 0.14f;
        [SerializeField] private float recoilStrength = 0.22f;
        [SerializeField] private float recoilDuration = 0.22f;

        [Header("Hit-stop")]
        [SerializeField, Range(0f, 1f)] private float hitStopScale = 0.05f;
        [SerializeField] private float hitStopDuration = 0.06f;

        [Header("Death / Flee")]
        [SerializeField] private float deathFadeDuration = 0.4f;
        [SerializeField] private float fleeDuration = 0.35f;

        private MaterialPropertyBlock _mpb;
        private readonly HashSet<string> _triggers = new HashSet<string>();

        private static readonly int FlashAmountId = Shader.PropertyToID("_FlashAmount");
        private static readonly int FlashColorId = Shader.PropertyToID("_FlashColor");

        private void Awake()
        {
            _mpb = new MaterialPropertyBlock();
            CacheAnimatorTriggers();
            SetFlash(0f);
        }

        /// <summary>Binds this view to a runtime battler, adopting its sprite when one is provided.</summary>
        public void Bind(BattlerRuntime runtime)
        {
            if (spriteRenderer != null && runtime != null && runtime.BattleSprite != null)
            {
                spriteRenderer.sprite = runtime.BattleSprite;
            }
        }

        /// <summary>Attacker beat: lunge toward the opponent and settle back.</summary>
        public IEnumerator PlayAttackRoutine(Vector3 opponentWorldPosition)
        {
            TriggerAnimator(AttackTrigger);
            Vector3 direction = (opponentWorldPosition - transform.position).normalized;
            transform.DOPunchPosition(direction * lungeDistance, lungeDuration, 1, 0.5f);
            yield return new WaitForSeconds(lungeDuration);
        }

        /// <summary>Target beat: flash + camera shake + hit-stop, then recoil away from the hit.</summary>
        public IEnumerator PlayHitRoutine(Vector2 knockbackDirection)
        {
            TriggerAnimator(HitTrigger);
            if (impulseSource != null)
            {
                impulseSource.GenerateImpulse();
            }

            SetFlash(1f, hitFlashColor);

            // Hit-stop: freeze briefly for impact weight (real-time wait so it actually elapses).
            Time.timeScale = hitStopScale;
            yield return new WaitForSecondsRealtime(hitStopDuration);
            Time.timeScale = 1f;

            transform.DOPunchPosition((Vector3)knockbackDirection.normalized * recoilStrength, recoilDuration, 8, 0.7f);
            yield return FadeFlashOut(flashDuration);

            float remaining = recoilDuration - flashDuration;
            if (remaining > 0f)
            {
                yield return new WaitForSeconds(remaining);
            }
        }

        /// <summary>Support beat: a soft green pulse when healed by an item.</summary>
        public IEnumerator PlayHealRoutine()
        {
            TriggerAnimator(ItemTrigger);

            const float duration = 0.4f;
            float t = 0f;
            while (t < duration)
            {
                t += Time.deltaTime;
                float amount = Mathf.Sin(Mathf.Clamp01(t / duration) * Mathf.PI) * 0.7f;
                SetFlash(amount, healFlashColor);
                yield return null;
            }

            SetFlash(0f);
        }

        /// <summary>Death beat: play the down trigger and fade the sprite out.</summary>
        public IEnumerator PlayDeathRoutine()
        {
            TriggerAnimator(DownTrigger);
            if (spriteRenderer != null)
            {
                spriteRenderer.DOFade(0f, deathFadeDuration);
            }

            yield return new WaitForSeconds(deathFadeDuration);
        }

        /// <summary>Flee beat: a quick hop and fade as the battler escapes.</summary>
        public IEnumerator PlayFleeRoutine()
        {
            transform.DOPunchPosition(Vector3.up * 0.3f, fleeDuration, 6, 0.6f);
            if (spriteRenderer != null)
            {
                spriteRenderer.DOFade(0f, fleeDuration);
            }

            yield return new WaitForSeconds(fleeDuration);
        }

        /// <summary>Fires the victory animation, if the Animator declares it.</summary>
        public void PlayVictory() => TriggerAnimator(VictoryTrigger);

        private IEnumerator FadeFlashOut(float duration)
        {
            float t = 0f;
            while (t < duration)
            {
                t += Time.deltaTime;
                SetFlash(1f - Mathf.Clamp01(t / duration), hitFlashColor);
                yield return null;
            }

            SetFlash(0f);
        }

        private void SetFlash(float amount) => SetFlash(amount, hitFlashColor);

        private void SetFlash(float amount, Color color)
        {
            if (spriteRenderer == null)
            {
                return;
            }

            spriteRenderer.GetPropertyBlock(_mpb);
            _mpb.SetFloat(FlashAmountId, amount);
            _mpb.SetColor(FlashColorId, color);
            spriteRenderer.SetPropertyBlock(_mpb);
        }

        private void CacheAnimatorTriggers()
        {
            if (animator == null)
            {
                return;
            }

            foreach (AnimatorControllerParameter parameter in animator.parameters)
            {
                if (parameter.type == AnimatorControllerParameterType.Trigger)
                {
                    _triggers.Add(parameter.name);
                }
            }
        }

        private void TriggerAnimator(string triggerName)
        {
            if (animator != null && _triggers.Contains(triggerName))
            {
                animator.SetTrigger(triggerName);
            }
        }
    }
}
