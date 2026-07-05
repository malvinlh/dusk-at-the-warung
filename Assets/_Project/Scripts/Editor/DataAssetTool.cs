using System.Collections.Generic;
using DuskWarung.Battle;
using UnityEditor;
using UnityEngine;

namespace DuskWarung.EditorTools
{
    /// <summary>
    /// Creates the ScriptableObject data instances (skill, item, player, enemy, encounter) with the
    /// stats from the design doc, wiring their sprite/skill/item references. Idempotent: re-running
    /// updates the existing assets in place.
    /// </summary>
    public static class DataAssetTool
    {
        [MenuItem("Tools/Dusk Warung/3. Create Data Assets", priority = 3)]
        public static void Run()
        {
            DuskEditorUtil.EnsureOutputFolders();

            SkillSO skill = DuskEditorUtil.LoadOrCreateSO<SkillSO>(DuskEditorUtil.DataDir + "/Skills/Skill_CrackerToss.asset");
            skill.displayName = "Cracker Toss";
            skill.mpCost = 5;
            skill.power = 1.5f;
            skill.tooltip = "Bu Sari's lucky snack. Startles spirits.";
            EditorUtility.SetDirty(skill);

            ItemSO item = DuskEditorUtil.LoadOrCreateSO<ItemSO>(DuskEditorUtil.DataDir + "/Items/Item_KelapaMuda.asset");
            item.displayName = "Kelapa Muda";
            item.healAmount = 12;
            item.tooltip = "Restores a little HP. Refreshing.";
            EditorUtility.SetDirty(item);

            PlayerDefinitionSO player = DuskEditorUtil.LoadOrCreateSO<PlayerDefinitionSO>(DuskEditorUtil.DataDir + "/Battlers/Player_Traveller.asset");
            player.displayName = "Traveller";
            player.maxHp = 30;
            player.maxMp = 10;
            player.attack = 8;
            player.defense = 5;
            player.speed = 8;
            player.battleSprite = DuskEditorUtil.FirstSprite(DuskEditorUtil.PlayerSheet);
            player.skills = new List<SkillSO> { skill };
            player.startingItems = new List<PlayerDefinitionSO.StartingItem>
            {
                new PlayerDefinitionSO.StartingItem { item = item, count = 3 }
            };
            EditorUtility.SetDirty(player);

            EnemyDefinitionSO enemy = DuskEditorUtil.LoadOrCreateSO<EnemyDefinitionSO>(DuskEditorUtil.DataDir + "/Battlers/Enemy_Genderuwo.asset");
            enemy.displayName = "Genderuwo";
            enemy.maxHp = 30;
            enemy.maxMp = 10;
            enemy.attack = 11;
            enemy.defense = 4;
            enemy.speed = 6;
            enemy.battleSprite = DuskEditorUtil.FirstSprite(DuskEditorUtil.EnemyIdle);
            enemy.aiTable = new List<EnemyDefinitionSO.AiEntry>
            {
                new EnemyDefinitionSO.AiEntry { skill = null, weight = 3f } // null → basic Attack
            };
            EditorUtility.SetDirty(enemy);

            EncounterSO encounter = DuskEditorUtil.LoadOrCreateSO<EncounterSO>(DuskEditorUtil.DataDir + "/Encounters/Encounter_GroveDusk.asset");
            encounter.player = player;
            encounter.enemy = enemy;
            var background = AssetDatabase.LoadAssetAtPath<Sprite>(DuskEditorUtil.SpritesDir + "/battleback1.png");
            if (background != null)
            {
                encounter.background = background;
            }
            EditorUtility.SetDirty(encounter);

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("[Dusk] Data assets created/updated under _Project/Data (Skill, Item, Player, Enemy, Encounter).");
        }
    }
}
