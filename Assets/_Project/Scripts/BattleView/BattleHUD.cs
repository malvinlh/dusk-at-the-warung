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

        [SerializeField, Tooltip("Image (type = Filled) for player MP.")]
        private Image playerMpFill;

        [SerializeField] private TMP_Text playerNameLabel;
        [SerializeField] private TMP_Text playerHpLabel;
        [SerializeField] private TMP_Text playerMpLabel;

        [Header("Enemy")]
        [SerializeField, Tooltip("Image (type = Filled) for enemy HP.")]
        private Image enemyHpFill;

        [SerializeField] private TMP_Text enemyNameLabel;

        [SerializeField] private float tweenDuration = 0.3f;

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
            SetFill(playerHpFill, Ratio(p.Hp, p.MaxHp), instant);
            SetFill(playerMpFill, Ratio(p.Mp, p.MaxMp), instant);
            SetText(playerHpLabel, $"{p.Hp}/{p.MaxHp}");
            SetText(playerMpLabel, $"{p.Mp}/{p.MaxMp}");
        }

        private void RefreshEnemy(bool instant)
        {
            BattlerRuntime e = _machine.Enemy;
            SetFill(enemyHpFill, Ratio(e.Hp, e.MaxHp), instant);
        }

        private static float Ratio(int value, int max) => max > 0 ? Mathf.Clamp01(value / (float)max) : 0f;

        private void SetFill(Image fill, float target, bool instant)
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
