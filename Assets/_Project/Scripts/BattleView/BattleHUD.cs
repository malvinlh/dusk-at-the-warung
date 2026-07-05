using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace DuskWarung.Battle.View
{
    /// <summary>
    /// Displays the combatants' names and HP/MP, tweening the bars (rather than snapping) when
    /// stats change. Subscribes to the model's <c>OnBattlerChanged</c> event and reads state;
    /// it never writes to the model.
    /// </summary>
    public class BattleHUD : MonoBehaviour
    {
        [Header("Player")]
        [SerializeField, Tooltip("Image (type = Filled) for player HP.")]
        private Image playerHpFill;

        [SerializeField, Tooltip("Optional pale 'ghost' bar behind the HP fill that drains slowly to show the damage chip.")]
        private Image playerHpGhost;

        [SerializeField, Tooltip("Image (type = Filled) for player MP.")]
        private Image playerMpFill;

        [SerializeField] private TMP_Text playerNameLabel;
        [SerializeField] private TMP_Text playerHpLabel;
        [SerializeField] private TMP_Text playerMpLabel;

        [Header("Enemy")]
        [SerializeField, Tooltip("Image (type = Filled) for enemy HP.")]
        private Image enemyHpFill;

        [SerializeField, Tooltip("Optional pale 'ghost' bar behind the enemy HP fill (damage chip).")]
        private Image enemyHpGhost;

        [SerializeField] private TMP_Text enemyNameLabel;

        [SerializeField, Tooltip("How fast the coloured fill snaps to the new value.")]
        private float tweenDuration = 0.18f;

        [SerializeField, Tooltip("How slowly the ghost chip drains behind it (the recent-damage streak).")]
        private float ghostDuration = 0.45f;

        [SerializeField, Tooltip("Delay before the ghost chip starts draining, so the hit reads.")]
        private float ghostDelay = 0.18f;

        private BattleStateMachine _machine;

        /// <summary>Binds the HUD to a battle, wiring labels and subscribing to stat changes.</summary>
        public void Bind(BattleStateMachine machine)
        {
            _machine = machine;
            _machine.OnBattlerChanged += HandleBattlerChanged;

            SetText(playerNameLabel, machine.Player.DisplayName);
            SetText(enemyNameLabel, machine.Enemy.DisplayName);

            RefreshPlayer(instant: true);
            RefreshEnemy(instant: true);
        }

        private void OnDestroy()
        {
            if (_machine != null)
            {
                _machine.OnBattlerChanged -= HandleBattlerChanged;
            }
        }

        private void HandleBattlerChanged(BattlerRuntime battler)
        {
            if (_machine == null)
            {
                return;
            }

            if (battler == _machine.Player)
            {
                RefreshPlayer(instant: false);
            }
            else if (battler == _machine.Enemy)
            {
                RefreshEnemy(instant: false);
            }
        }

        private void RefreshPlayer(bool instant)
        {
            BattlerRuntime p = _machine.Player;
            SetFill(playerHpFill, playerHpGhost, Ratio(p.Hp, p.MaxHp), instant);
            SetFill(playerMpFill, null, Ratio(p.Mp, p.MaxMp), instant);
            SetText(playerHpLabel, $"{p.Hp}/{p.MaxHp}");
            SetText(playerMpLabel, $"{p.Mp}/{p.MaxMp}");
        }

        private void RefreshEnemy(bool instant)
        {
            BattlerRuntime e = _machine.Enemy;
            SetFill(enemyHpFill, enemyHpGhost, Ratio(e.Hp, e.MaxHp), instant);
        }

        private static float Ratio(int value, int max) => max > 0 ? Mathf.Clamp01(value / (float)max) : 0f;

        /// <summary>Drives a bar to <paramref name="target"/>: the <paramref name="fill"/> snaps, while the
        /// optional <paramref name="ghost"/> behind it drains slowly on damage to show a "chip".</summary>
        private void SetFill(Image fill, Image ghost, float target, bool instant)
        {
            if (fill == null)
            {
                return;
            }

            fill.DOKill();
            if (instant)
            {
                fill.fillAmount = target;
            }
            else
            {
                fill.DOFillAmount(target, tweenDuration).SetEase(Ease.OutQuad);
            }

            if (ghost == null)
            {
                return;
            }

            ghost.DOKill();
            if (instant || target >= ghost.fillAmount)
            {
                ghost.fillAmount = target; // init or heal: no chip
            }
            else
            {
                ghost.DOFillAmount(target, ghostDuration).SetDelay(ghostDelay).SetEase(Ease.InQuad);
            }
        }

        private static void SetText(TMP_Text label, string text)
        {
            if (label != null)
            {
                label.text = text;
            }
        }
    }
}
