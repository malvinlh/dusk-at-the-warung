using System.Collections;
using DG.Tweening;
using DuskWarung.Core;
using DuskWarung.FungusCommands;
using UnityEngine;

namespace DuskWarung.Battle.View
{
    /// <summary>
    /// The battle scene's conductor. It builds the pure <see cref="BattleStateMachine"/> from
    /// the pending encounter, wires the model's events to the views, ticks the FSM, turns each
    /// resolved action into a sequenced presentation, and gates turn advancement on that
    /// presentation finishing. It is the ONLY place that bridges model ↔ view, keeping every
    /// other view a passive subscriber and the model entirely view-free.
    /// </summary>
    public class BattleController : MonoBehaviour
    {
        [Header("Encounter")]
        [SerializeField, Tooltip("Fallback encounter used when the Battle scene is played directly (no session hand-off).")]
        private EncounterSO debugEncounter;

        [Header("Battlers")]
        [SerializeField] private BattlerView playerView;
        [SerializeField] private BattlerView enemyView;
        [SerializeField, Tooltip("Optional: SpriteRenderer whose sprite is set to the encounter background.")]
        private SpriteRenderer backgroundRenderer;

        [Header("UI")]
        [SerializeField] private BattleHUD hud;
        [SerializeField, Tooltip("CanvasGroup on the HUD canvas; hidden during narrative so dialogue has focus.")]
        private CanvasGroup hudGroup;
        [SerializeField] private CommandMenuUI commandMenu;
        [SerializeField] private DamagePopup damagePopupPrefab;
        [SerializeField, Tooltip("Optional parent for spawned popups (leave empty for world root).")]
        private Transform popupParent;
        [SerializeField] private float popupYOffset = 0.8f;

        [Header("Flow")]
        [SerializeField] private SceneLoader loader;
        [SerializeField] private FungusBridge fungus;
        [SerializeField] private string introBlock = "BattleIntro";
        [SerializeField] private string victoryBlock = "Victory";
        [SerializeField] private string defeatBlock = "";
        [SerializeField] private string fledBlock = "";
        [SerializeField] private string endSceneName = "End";
        [SerializeField] private string overworldSceneName = "Overworld";
        [SerializeField] private float endDelay = 0.6f;
        [SerializeField] private float enemyThinkDelay = 0.35f;

        [Header("Audio (optional)")]
        [SerializeField] private AudioSource sfxSource;
        [SerializeField] private AudioClip hitClip;
        [SerializeField] private AudioClip healClip;

        private BattleStateMachine _machine;
        private bool _active;

        /// <summary>The enemy battler (used by the command menu to target).</summary>
        public BattlerRuntime Enemy => _machine?.Enemy;

        /// <summary>The player battler.</summary>
        public BattlerRuntime Player => _machine?.Player;

        private void Start()
        {
            EncounterSO encounter = GameSession.PendingEncounter != null ? GameSession.PendingEncounter : debugEncounter;
            if (encounter == null)
            {
                Debug.LogError("[BattleController] No encounter set (GameSession.PendingEncounter and debugEncounter are both null).");
                return;
            }

            _machine = new BattleStateMachine(encounter);

            if (playerView != null) playerView.Bind(_machine.Player);
            if (enemyView != null) enemyView.Bind(_machine.Enemy);
            if (backgroundRenderer != null && encounter.background != null) backgroundRenderer.sprite = encounter.background;
            if (hud != null) hud.Bind(_machine);
            if (commandMenu != null) commandMenu.Initialize(this);

            SetHudVisible(false, instant: true); // keep the intro dialogue clean; revealed when combat begins

            _machine.OnStateChanged += HandleStateChanged;
            _machine.OnActionResolved += HandleActionResolved;
            _machine.OnBattleEnded += HandleBattleEnded;

            StartCoroutine(IntroThenBattle());
        }

        /// <summary>Waits for the scene transition to finish revealing the battle, then plays the intro dialog —
        /// so the dialog can't race the fade-in and pop into view.</summary>
        private IEnumerator IntroThenBattle()
        {
            // Instance auto-creates (non-busy) when the Battle scene is played directly, so this is safe.
            while (SceneTransition.Instance.IsBusy)
            {
                yield return null;
            }

            if (fungus != null && !string.IsNullOrEmpty(introBlock))
            {
                fungus.ExecuteBlock(introBlock, BeginBattle);
            }
            else
            {
                BeginBattle();
            }
        }

        private void OnDestroy()
        {
            if (_machine == null)
            {
                return;
            }

            _machine.OnStateChanged -= HandleStateChanged;
            _machine.OnActionResolved -= HandleActionResolved;
            _machine.OnBattleEnded -= HandleBattleEnded;
        }

        private void Update()
        {
            if (_active)
            {
                _machine.Tick();
            }
        }

        private void BeginBattle()
        {
            SetHudVisible(true); // combat starting — bring the HP/MP HUD back
            _active = true;
            _machine.Start();
        }

        /// <summary>Shows/hides the HP/MP HUD — hidden during narrative so dialogue holds focus, faded back for combat.</summary>
        private void SetHudVisible(bool visible, bool instant = false)
        {
            if (hudGroup == null)
            {
                return;
            }

            hudGroup.DOKill();
            if (instant)
            {
                hudGroup.alpha = visible ? 1f : 0f;
            }
            else
            {
                hudGroup.DOFade(visible ? 1f : 0f, 0.25f);
            }

            hudGroup.blocksRaycasts = visible;
        }

        /// <summary>Receives the player's chosen command from the command menu.</summary>
        public void SubmitCommand(BattleCommand command)
        {
            _machine?.SubmitPlayerCommand(command);
        }

        private void HandleStateChanged(IBattleState state)
        {
            if (commandMenu == null)
            {
                return;
            }

            if (state is PlayerTurnState)
            {
                commandMenu.Show(_machine.Player);
            }
            else
            {
                commandMenu.Hide();
            }
        }

        private void HandleActionResolved(ActionResult result)
        {
            StartCoroutine(PresentAction(result));
        }

        private IEnumerator PresentAction(ActionResult result)
        {
            if (!result.Actor.IsPlayer && enemyThinkDelay > 0f)
            {
                yield return new WaitForSeconds(enemyThinkDelay);
            }

            switch (result.Kind)
            {
                case BattleCommand.Kind.Run:
                    yield return PresentRun(result);
                    break;
                case BattleCommand.Kind.Item:
                    yield return PresentHeal(result);
                    break;
                default:
                    yield return PresentDamage(result);
                    break;
            }

            _machine.NotifyPresentationFinished();
        }

        private IEnumerator PresentRun(ActionResult result)
        {
            BattlerView actorView = ViewFor(result.Actor);
            if (result.Fled)
            {
                SpawnTextPopup(result.Actor, "Fled!");
                if (actorView != null) yield return actorView.PlayFleeRoutine();
            }
            else
            {
                SpawnTextPopup(result.Actor, "Can't escape!");
                yield return new WaitForSeconds(0.5f);
            }
        }

        private IEnumerator PresentHeal(ActionResult result)
        {
            PlaySfx(healClip);
            SpawnHealPopup(result.Target, result.Heal);

            BattlerView actorView = ViewFor(result.Actor);
            if (actorView != null)
            {
                yield return actorView.PlayHealRoutine();
            }
        }

        private IEnumerator PresentDamage(ActionResult result)
        {
            BattlerView actorView = ViewFor(result.Actor);
            BattlerView targetView = ViewFor(result.Target);

            if (actorView != null && targetView != null)
            {
                yield return actorView.PlayAttackRoutine(targetView.transform.position);
            }

            PlaySfx(hitClip);
            SpawnDamagePopup(result.Target, result.Damage, result.Crit);

            if (targetView != null)
            {
                yield return targetView.PlayHitRoutine(KnockbackDirection(result.Actor, result.Target));
            }

            if (!result.Target.IsAlive && targetView != null)
            {
                yield return targetView.PlayDeathRoutine();
            }
        }

        private void HandleBattleEnded(BattleOutcome outcome)
        {
            _active = false;
            if (commandMenu != null) commandMenu.Hide();

            GameSession.LastBattleResult = outcome;

            if (outcome == BattleOutcome.Victory && playerView != null)
            {
                playerView.PlayVictory();
            }

            StartCoroutine(EndSequence(outcome));
        }

        private IEnumerator EndSequence(BattleOutcome outcome)
        {
            yield return new WaitForSeconds(endDelay);

            string block = outcome == BattleOutcome.Victory ? victoryBlock
                : outcome == BattleOutcome.Defeat ? defeatBlock
                : fledBlock;

            if (fungus != null && !string.IsNullOrEmpty(block))
            {
                SetHudVisible(false); // clear the HUD for the closing line
                bool done = false;
                fungus.ExecuteBlock(block, () => done = true);
                yield return new WaitUntil(() => done);
            }

            string next = outcome == BattleOutcome.Victory ? endSceneName : overworldSceneName;
            if (loader != null)
            {
                loader.LoadWithFade(next);
            }
            else
            {
                Debug.LogWarning("[BattleController] No SceneLoader assigned; cannot transition after battle.");
            }
        }

        private BattlerView ViewFor(BattlerRuntime battler)
            => battler != null && battler.IsPlayer ? playerView : enemyView;

        private Vector2 KnockbackDirection(BattlerRuntime actor, BattlerRuntime target)
        {
            BattlerView actorView = ViewFor(actor);
            BattlerView targetView = ViewFor(target);
            if (actorView == null || targetView == null)
            {
                return Vector2.left;
            }

            Vector2 delta = targetView.transform.position - actorView.transform.position;
            return delta.sqrMagnitude > 0.0001f ? delta.normalized : Vector2.left;
        }

        private void SpawnDamagePopup(BattlerRuntime who, int amount, bool crit)
        {
            DamagePopup popup = SpawnPopup(who);
            if (popup != null) popup.ShowDamage(amount, crit);
        }

        private void SpawnHealPopup(BattlerRuntime who, int amount)
        {
            DamagePopup popup = SpawnPopup(who);
            if (popup != null) popup.ShowHeal(amount);
        }

        private void SpawnTextPopup(BattlerRuntime who, string text)
        {
            DamagePopup popup = SpawnPopup(who);
            if (popup != null) popup.ShowText(text);
        }

        private DamagePopup SpawnPopup(BattlerRuntime who)
        {
            if (damagePopupPrefab == null)
            {
                return null;
            }

            BattlerView view = ViewFor(who);
            Vector3 position = (view != null ? view.transform.position : transform.position) + Vector3.up * popupYOffset;
            return Instantiate(damagePopupPrefab, position, Quaternion.identity, popupParent);
        }

        private void PlaySfx(AudioClip clip)
        {
            if (sfxSource != null && clip != null)
            {
                sfxSource.PlayOneShot(clip);
            }
        }
    }
}
