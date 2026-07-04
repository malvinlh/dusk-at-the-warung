namespace DuskWarung.Battle
{
    /// <summary>
    /// A single battle action, unifying Attack / Skill / Item / Run behind one type
    /// (Command pattern). Immutable: the UI and the AI both build these, and the
    /// state machine resolves them without caring where they came from.
    /// </summary>
    public readonly struct BattleCommand
    {
        /// <summary>The category of action this command represents.</summary>
        public enum Kind
        {
            Attack,
            Skill,
            Item,
            Run
        }

        /// <summary>Which kind of action this is.</summary>
        public Kind Type { get; }

        /// <summary>The battler performing the action.</summary>
        public BattlerRuntime Actor { get; }

        /// <summary>The battler the action is aimed at (self for Item; the opponent otherwise).</summary>
        public BattlerRuntime Target { get; }

        /// <summary>The skill used, or null unless <see cref="Type"/> is <see cref="Kind.Skill"/>.</summary>
        public SkillSO Skill { get; }

        /// <summary>The item used, or null unless <see cref="Type"/> is <see cref="Kind.Item"/>.</summary>
        public ItemSO Item { get; }

        /// <summary>Creates a command. Prefer the static factory helpers for clarity.</summary>
        public BattleCommand(Kind type, BattlerRuntime actor, BattlerRuntime target,
                             SkillSO skill = null, ItemSO item = null)
        {
            Type = type;
            Actor = actor;
            Target = target;
            Skill = skill;
            Item = item;
        }

        /// <summary>Basic attack from <paramref name="actor"/> against <paramref name="target"/>.</summary>
        public static BattleCommand Attack(BattlerRuntime actor, BattlerRuntime target)
            => new BattleCommand(Kind.Attack, actor, target);

        /// <summary>Uses <paramref name="skill"/> from <paramref name="actor"/> against <paramref name="target"/>.</summary>
        public static BattleCommand UseSkill(BattlerRuntime actor, BattlerRuntime target, SkillSO skill)
            => new BattleCommand(Kind.Skill, actor, target, skill: skill);

        /// <summary>Uses <paramref name="item"/> on <paramref name="actor"/> (self-targeted).</summary>
        public static BattleCommand UseItem(BattlerRuntime actor, ItemSO item)
            => new BattleCommand(Kind.Item, actor, actor, item: item);

        /// <summary>Attempts to flee from <paramref name="target"/>.</summary>
        public static BattleCommand Run(BattlerRuntime actor, BattlerRuntime target)
            => new BattleCommand(Kind.Run, actor, target);
    }
}
