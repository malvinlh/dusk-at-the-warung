using DuskWarung.Battle;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace DuskWarung.Battle.View
{
    /// <summary>
    /// The player's Attack / Skill / Item / Run menu. Reads the player's current skills and
    /// inventory to gate the Skill button by MP and the Item button by remaining charges, then
    /// builds a <see cref="BattleCommand"/> and hands it to the controller. Pure input surface:
    /// it never resolves anything itself.
    /// </summary>
    public class CommandMenuUI : MonoBehaviour
    {
        [Header("Panel")]
        [SerializeField, Tooltip("Root object toggled to show/hide the menu.")]
        private GameObject root;

        [Header("Buttons")]
        [SerializeField] private Button attackButton;
        [SerializeField] private Button skillButton;
        [SerializeField] private Button itemButton;
        [SerializeField] private Button runButton;

        [Header("Dynamic labels (optional)")]
        [SerializeField] private TMP_Text skillLabel;
        [SerializeField] private TMP_Text itemLabel;
        [SerializeField] private TMP_Text tooltipLabel;

        [Header("Command icons (optional)")]
        [SerializeField, Tooltip("Shows the current skill's icon; hidden when the skill has none.")]
        private Image skillIcon;
        [SerializeField, Tooltip("Shows the current item's icon; hidden when the item has none.")]
        private Image itemIcon;

        [Header("Tooltips")]
        [SerializeField, TextArea] private string attackTooltip = "A firm, respectful whack.";
        [SerializeField, TextArea] private string runTooltip = "Bold strategy. Rarely works.";

        [Header("SFX (optional)")]
        [SerializeField] private AudioSource sfxSource;
        [SerializeField] private AudioClip hoverClip;
        [SerializeField] private AudioClip confirmClip;

        private BattleController _controller;
        private BattlerRuntime _player;
        private SkillSO _skill;
        private ItemSlot _itemSlot;

        /// <summary>Wires button callbacks once. Call from the controller on start.</summary>
        public void Initialize(BattleController controller)
        {
            _controller = controller;

            AddClick(attackButton, OnAttack);
            AddClick(skillButton, OnSkill);
            AddClick(itemButton, OnItem);
            AddClick(runButton, OnRun);

            Hide();
        }

        /// <summary>Shows the menu for the given player, refreshing gating and labels.</summary>
        public void Show(BattlerRuntime player)
        {
            _player = player;
            _skill = FirstSkill(player);
            _itemSlot = FirstAvailableItem(player);

            RefreshButtons();
            ClearTooltip();

            if (root != null)
            {
                root.SetActive(true);
            }
        }

        /// <summary>Hides the menu.</summary>
        public void Hide()
        {
            if (root != null)
            {
                root.SetActive(false);
            }
        }

        // --- Hover hooks (optional: wire to EventTrigger PointerEnter for hover SFX + tooltip) ---

        /// <summary>Hover feedback for the Attack button.</summary>
        public void HoverAttack() => Hover(attackTooltip);

        /// <summary>Hover feedback for the Skill button.</summary>
        public void HoverSkill() => Hover(_skill != null ? _skill.tooltip : "No skill available.");

        /// <summary>Hover feedback for the Item button.</summary>
        public void HoverItem() => Hover(_itemSlot != null ? _itemSlot.Item.tooltip : "No items left.");

        /// <summary>Hover feedback for the Run button.</summary>
        public void HoverRun() => Hover(runTooltip);

        /// <summary>Clears the tooltip text.</summary>
        public void ClearTooltip()
        {
            if (tooltipLabel != null)
            {
                tooltipLabel.text = string.Empty;
            }
        }

        private void OnAttack()
        {
            PlayConfirm();
            Submit(BattleCommand.Attack(_player, _controller.Enemy));
        }

        private void OnSkill()
        {
            if (_skill == null || !_player.CanAfford(_skill.mpCost))
            {
                return;
            }

            PlayConfirm();
            Submit(BattleCommand.UseSkill(_player, _controller.Enemy, _skill));
        }

        private void OnItem()
        {
            if (_itemSlot == null || !_itemSlot.IsAvailable)
            {
                return;
            }

            PlayConfirm();
            Submit(BattleCommand.UseItem(_player, _itemSlot.Item));
        }

        private void OnRun()
        {
            PlayConfirm();
            Submit(BattleCommand.Run(_player, _controller.Enemy));
        }

        private void Submit(BattleCommand command)
        {
            Hide(); // Prevent a second click before the turn resolves.
            _controller.SubmitCommand(command);
        }

        private void RefreshButtons()
        {
            bool canSkill = _skill != null && _player.CanAfford(_skill.mpCost);
            bool canItem = _itemSlot != null && _itemSlot.IsAvailable;

            if (skillButton != null) skillButton.interactable = canSkill;
            if (itemButton != null) itemButton.interactable = canItem;

            if (skillLabel != null)
            {
                skillLabel.text = _skill != null ? $"{_skill.displayName} ({_skill.mpCost} MP)" : "—";
            }

            if (itemLabel != null)
            {
                itemLabel.text = _itemSlot != null ? $"{_itemSlot.Item.displayName} x{_itemSlot.Count}" : "—";
            }

            SetIcon(skillIcon, _skill != null ? _skill.icon : null);
            SetIcon(itemIcon, _itemSlot != null ? _itemSlot.Item.icon : null);
        }

        private static void SetIcon(Image image, Sprite sprite)
        {
            if (image == null)
            {
                return;
            }

            image.sprite = sprite;
            image.enabled = sprite != null;
        }

        private void Hover(string tooltip)
        {
            PlaySfx(hoverClip);
            if (tooltipLabel != null)
            {
                tooltipLabel.text = tooltip;
            }
        }

        private static SkillSO FirstSkill(BattlerRuntime player)
        {
            foreach (SkillSO skill in player.Skills)
            {
                if (skill != null)
                {
                    return skill;
                }
            }

            return null;
        }

        private static ItemSlot FirstAvailableItem(BattlerRuntime player)
        {
            foreach (ItemSlot slot in player.Items)
            {
                if (slot.IsAvailable)
                {
                    return slot;
                }
            }

            return null;
        }

        private static void AddClick(Button button, UnityEngine.Events.UnityAction action)
        {
            if (button != null)
            {
                button.onClick.AddListener(action);
            }
        }

        private void PlayConfirm() => PlaySfx(confirmClip);

        private void PlaySfx(AudioClip clip)
        {
            if (sfxSource != null && clip != null)
            {
                sfxSource.PlayOneShot(clip);
            }
        }
    }
}
