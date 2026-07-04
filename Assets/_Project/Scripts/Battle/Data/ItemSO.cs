using UnityEngine;

namespace DuskWarung.Battle
{
    /// <summary>
    /// Designer-authored definition of a consumable item (e.g. "Kelapa Muda").
    /// Items in this slice restore HP to their user.
    /// </summary>
    [CreateAssetMenu(menuName = "Dusk/Item", fileName = "Item_")]
    public class ItemSO : ScriptableObject
    {
        [Tooltip("Name shown on the command button.")]
        public string displayName = "New Item";

        [Tooltip("HP restored to the user (clamped to their maximum HP).")]
        [Min(0)] public int healAmount = 12;

        [Tooltip("Flavor text shown when the item is highlighted.")]
        [TextArea] public string tooltip = "";

        [Tooltip("Optional icon for the command button (leave empty to use a text-only button).")]
        public Sprite icon;
    }
}
