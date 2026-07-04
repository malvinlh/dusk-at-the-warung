using System;
using System.Collections.Generic;

namespace DuskWarung.Battle
{
    /// <summary>
    /// Owns the battlers and the battle finite-state machine, resolves commands, and raises
    /// C# events that the presentation layer subscribes to (Observer pattern). This is the
    /// deterministic heart of combat: it holds no reference to any view, UI, or scene object,
    /// and communication flows strictly one way (model raises events; views listen).
    /// </summary>
    public class BattleStateMachine
    {
        /// <summary>Raised whenever the active state changes (UI shows/hides per phase).</summary>
        public event Action<IBattleState> OnStateChanged;

        /// <summary>Raised after a command resolves, carrying an immutable result to render.</summary>
        public event Action<ActionResult> OnActionResolved;

        /// <summary>Raised for a battler whose HP or MP changed (HUD refreshes its bars).</summary>
        public event Action<BattlerRuntime> OnBattlerChanged;

        /// <summary>Raised once when the battle ends, carrying the outcome.</summary>
        public event Action<BattleOutcome> OnBattleEnded;

        /// <summary>The player-controlled battler.</summary>
        public BattlerRuntime Player { get; }

        /// <summary>The enemy battler.</summary>
        public BattlerRuntime Enemy { get; }

        /// <summary>Turn order for the current round.</summary>
        public TurnQueue Turns { get; } = new TurnQueue();

        /// <summary>The battler whose turn it currently is.</summary>
        public BattlerRuntime Current { get; internal set; }

        /// <summary>Shared RNG for all combat rolls (seedable for deterministic tests).</summary>
        public Random Rng { get; }

        /// <summary>The final outcome, or <see cref="BattleOutcome.None"/> while the battle runs.</summary>
        public BattleOutcome Outcome { get; private set; } = BattleOutcome.None;

        /// <summary>True once the battle has ended.</summary>
        public bool IsFinished => Outcome != BattleOutcome.None;

        // Input latches consumed by the current state during Tick.
        internal BattleCommand? PendingPlayerCommand;
        internal bool PresentationComplete;

        private IBattleState _state;

        /// <summary>The active state (mainly for the view to switch UI on phase changes).</summary>
        public IBattleState CurrentState => _state;

        /// <summary>Every battler in the fight, in a stable order.</summary>
        public IEnumerable<BattlerRuntime> AllBattlers
        {
            get
            {
                yield return Player;
                yield return Enemy;
            }
        }

        /// <summary>Builds a battle from an encounter definition.</summary>
        /// <param name="encounter">Encounter data (player + enemy). Must not be null.</param>
        /// <param name="seed">Optional RNG seed for reproducible combat (used by tests).</param>
        public BattleStateMachine(EncounterSO encounter, int? seed = null)
        {
            if (encounter == null)
            {
                throw new ArgumentNullException(nameof(encounter));
            }

            if (encounter.player == null || encounter.enemy == null)
            {
                throw new ArgumentException("Encounter must define both a player and an enemy.", nameof(encounter));
            }

            Player = new BattlerRuntime(encounter.player, true);
            Enemy = new BattlerRuntime(encounter.enemy, false);
            Rng = seed.HasValue ? new Random(seed.Value) : new Random();
        }

        /// <summary>Enters the first state and begins the battle.</summary>
        public void Start()
        {
            ChangeState(new SetupState(this));
        }

        /// <summary>Transitions to <paramref name="next"/>, running Exit/Enter around the swap.</summary>
        public void ChangeState(IBattleState next)
        {
            _state?.Exit();
            _state = next;
            OnStateChanged?.Invoke(next);
            next?.Enter();
        }

        /// <summary>Advances the active state. Call once per frame from the scene controller.</summary>
        public void Tick() => _state?.Tick();

        /// <summary>Queues the player's chosen command for the current <see cref="PlayerTurnState"/>.</summary>
        public void SubmitPlayerCommand(BattleCommand command) => PendingPlayerCommand = command;

        /// <summary>Signals that the view finished presenting the last action (gates turn advance).</summary>
        public void NotifyPresentationFinished() => PresentationComplete = true;

        /// <summary>
        /// Applies a command to the model, raises the resulting events, and returns the
        /// immutable <see cref="ActionResult"/>. Resets the presentation gate so the view
        /// must report completion before the turn advances.
        /// </summary>
        public ActionResult Resolve(BattleCommand command)
        {
            PresentationComplete = false;
            ActionResult result;

            switch (command.Type)
            {
                case BattleCommand.Kind.Attack:
                    result = ResolveDamage(command, 1f, skill: null);
                    break;

                case BattleCommand.Kind.Skill:
                    command.Actor.TrySpendMp(command.Skill != null ? command.Skill.mpCost : 0);
                    float power = command.Skill != null ? command.Skill.power : 1f;
                    result = ResolveDamage(command, power, command.Skill);
                    OnBattlerChanged?.Invoke(command.Actor); // MP spent
                    break;

                case BattleCommand.Kind.Item:
                    result = ResolveItem(command);
                    break;

                default: // Run
                    bool fled = EscapeCalculator.TryEscape(command.Actor.Speed, command.Target.Speed, Rng, out _);
                    result = new ActionResult(command.Type, command.Actor, command.Target,
                        damage: 0, heal: 0, crit: false, fled: fled, missed: !fled);
                    break;
            }

            OnActionResolved?.Invoke(result);
            return result;
        }

        private ActionResult ResolveDamage(BattleCommand command, float power, SkillSO skill)
        {
            int damage = DamageCalculator.Compute(command.Actor.Attack, power, command.Target.Defense, Rng, out bool crit);
            command.Target.TakeDamage(damage);
            OnBattlerChanged?.Invoke(command.Target);
            return new ActionResult(command.Type, command.Actor, command.Target,
                damage, heal: 0, crit: crit, fled: false, missed: false, skill: skill);
        }

        private ActionResult ResolveItem(BattleCommand command)
        {
            command.Actor.FindItem(command.Item)?.TryConsume();

            int before = command.Actor.Hp;
            command.Actor.Heal(command.Item != null ? command.Item.healAmount : 0);
            int healed = command.Actor.Hp - before;

            OnBattlerChanged?.Invoke(command.Actor);
            return new ActionResult(command.Type, command.Actor, command.Actor,
                damage: 0, heal: healed, crit: false, fled: false, missed: false, item: command.Item);
        }

        /// <summary>Records the outcome and notifies listeners that the battle has ended.</summary>
        public void EndBattle(BattleOutcome outcome)
        {
            if (IsFinished)
            {
                return; // Guard against a double-raise.
            }

            Outcome = outcome;
            OnBattleEnded?.Invoke(outcome);
        }
    }
}
