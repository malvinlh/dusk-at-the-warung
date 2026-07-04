namespace DuskWarung.Battle
{
    /// <summary>
    /// Immutable outcome of resolving a <see cref="BattleCommand"/>. The model raises this
    /// through <c>OnActionResolved</c>; views render entirely from it (damage numbers,
    /// flash, HP tweens) without re-reading the model or re-running any math.
    /// </summary>
    public readonly struct ActionResult
    {
        /// <summary>Which kind of action produced this result.</summary>
        public BattleCommand.Kind Kind { get; }

        /// <summary>The battler that acted.</summary>
        public BattlerRuntime Actor { get; }

        /// <summary>The battler that was affected (self for heals).</summary>
        public BattlerRuntime Target { get; }

        /// <summary>Damage dealt to <see cref="Target"/> (0 for non-damaging actions).</summary>
        public int Damage { get; }

        /// <summary>HP restored to <see cref="Target"/> (0 for non-healing actions).</summary>
        public int Heal { get; }

        /// <summary>True when the hit was a critical.</summary>
        public bool Crit { get; }

        /// <summary>True when a Run action succeeded.</summary>
        public bool Fled { get; }

        /// <summary>True when the action failed to connect (e.g. a failed Run).</summary>
        public bool Missed { get; }

        /// <summary>The skill involved, when the action was a skill (for presentation).</summary>
        public SkillSO Skill { get; }

        /// <summary>The item involved, when the action was an item (for presentation).</summary>
        public ItemSO Item { get; }

        /// <summary>Creates an action result. Use the static factory helpers where possible.</summary>
        public ActionResult(BattleCommand.Kind kind, BattlerRuntime actor, BattlerRuntime target,
                            int damage, int heal, bool crit, bool fled, bool missed,
                            SkillSO skill = null, ItemSO item = null)
        {
            Kind = kind;
            Actor = actor;
            Target = target;
            Damage = damage;
            Heal = heal;
            Crit = crit;
            Fled = fled;
            Missed = missed;
            Skill = skill;
            Item = item;
        }
    }
}
