using UnityEngine;

namespace DuskWarung.Battle.Tests
{
    /// <summary>Builds throwaway ScriptableObject-backed battlers and encounters for the model tests.</summary>
    internal static class TestBattlerFactory
    {
        public static PlayerDefinitionSO PlayerDef(int hp = 30, int mp = 10, int atk = 8, int def = 4, int spd = 6)
        {
            var def_ = ScriptableObject.CreateInstance<PlayerDefinitionSO>();
            def_.displayName = "Traveller";
            def_.maxHp = hp;
            def_.maxMp = mp;
            def_.attack = atk;
            def_.defense = def;
            def_.speed = spd;
            return def_;
        }

        public static EnemyDefinitionSO EnemyDef(int hp = 30, int mp = 0, int atk = 8, int def = 4, int spd = 6)
        {
            var def_ = ScriptableObject.CreateInstance<EnemyDefinitionSO>();
            def_.displayName = "Genderuwo";
            def_.maxHp = hp;
            def_.maxMp = mp;
            def_.attack = atk;
            def_.defense = def;
            def_.speed = spd;
            return def_;
        }

        public static ItemSO ItemDef(int heal = 12)
        {
            var item = ScriptableObject.CreateInstance<ItemSO>();
            item.displayName = "Kelapa Muda";
            item.healAmount = heal;
            return item;
        }

        public static BattlerRuntime Player(int hp = 30, int mp = 10, int atk = 8, int def = 4, int spd = 6)
            => new BattlerRuntime(PlayerDef(hp, mp, atk, def, spd), true);

        public static BattlerRuntime Enemy(int hp = 30, int mp = 0, int atk = 8, int def = 4, int spd = 6)
            => new BattlerRuntime(EnemyDef(hp, mp, atk, def, spd), false);

        public static EncounterSO Encounter(PlayerDefinitionSO player, EnemyDefinitionSO enemy)
        {
            var encounter = ScriptableObject.CreateInstance<EncounterSO>();
            encounter.player = player;
            encounter.enemy = enemy;
            return encounter;
        }
    }
}
