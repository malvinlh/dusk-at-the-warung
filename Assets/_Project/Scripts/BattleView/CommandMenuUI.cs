using DuskWarung.Battle;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
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
        private Button[] _buttons;
        private int _lastIndex; // last submitted command; the keyboard cursor returns here (or the nearest usable button)

        /// <summary>Wires button callbacks once. Call from the controller on start.</summary>
        public void Initialize(BattleController controller)
        {
            _controller = controller;
            _buttons = new[] { attackButton, skillButton, itemButton, runButton };

            AddClick(attackButton, OnAttack);
            AddClick(skillButton, OnSkill);
            AddClick(itemButton, OnItem);
            AddClick(runButton, OnRun);

            Hide();
        }

        private void Update()
        {
            // While the menu is open, a nav key should re-focus the cursor if it was lost (e.g. by clicking
            // empty space) — a plain click-away, with no key, still clears it.
            if (root == null || !root.activeInHierarchy)
            {
                return;
            }

            EventSystem es = EventSystem.current;
            if (es != null && !HasValidSelection(es) && NavKeyPressed())
            {
                SelectNearest(_lastIndex);
            }
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

            // Restore keyboard focus so WASD keeps working even after a button was disabled last turn.
            SelectNearest(_lastIndex);
        }

        /// <summary>Selects the interactable command button nearest to <paramref name="anchor"/> (keeps WASD alive).</summary>
        private void SelectNearest(int anchor)
        {
            EventSystem es = EventSystem.current;
            if (es == null || _buttons == null)
            {
                return;
            }

            for (int d = 0; d < _buttons.Length; d++)
            {
                int lo = anchor - d;
                if (lo >= 0 && IsSelectable(_buttons[lo]))
                {
                    es.SetSelectedGameObject(_buttons[lo].gameObject);
                    return;
                }

                int hi = anchor + d;
                if (d > 0 && hi < _buttons.Length && IsSelectable(_buttons[hi]))
                {
                    es.SetSelectedGameObject(_buttons[hi].gameObject);
                    return;
                }
            }
        }

        private static bool IsSelectable(Button button)
            => button != null && button.interactable && button.gameObject.activeInHierarchy;

        private bool HasValidSelection(EventSystem es)
        {
            int i = IndexOf(es.currentSelectedGameObject);
            return i >= 0 && IsSelectable(_buttons[i]);
        }

        private int IndexOf(GameObject go)
        {
            if (_buttons == null || go == null)
            {
                return -1;
            }

            for (int i = 0; i < _buttons.Length; i++)
            {
                if (_buttons[i] != null && _buttons[i].gameObject == go)
                {
                    return i;
                }
            }

            return -1;
        }

        private static bool NavKeyPressed() =>
            Input.GetKeyDown(KeyCode.W) || Input.GetKeyDown(KeyCode.A) || Input.GetKeyDown(KeyCode.S) || Input.GetKeyDown(KeyCode.D) ||
            Input.GetKeyDown(KeyCode.UpArrow) || Input.GetKeyDown(KeyCode.DownArrow) ||
            Input.GetKeyDown(KeyCode.LeftArrow) || Input.GetKeyDown(KeyCode.RightArrow);

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
            _lastIndex = 0;
            PlayConfirm();
            Submit(BattleCommand.Attack(_player, _controller.Enemy));
        }

        private void OnSkill()
        {
            if (_skill == null || !_player.CanAfford(_skill.mpCost))
            {
                return;
            }

            _lastIndex = 1;
            PlayConfirm();
            Submit(BattleCommand.UseSkill(_player, _controller.Enemy, _skill));
        }

        private void OnItem()
        {
            if (_itemSlot == null || !_itemSlot.IsAvailable)
            {
                return;
            }

            _lastIndex = 2;
            PlayConfirm();
            Submit(BattleCommand.UseItem(_player, _itemSlot.Item));
        }

        private void OnRun()
        {
            _lastIndex = 3;
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
