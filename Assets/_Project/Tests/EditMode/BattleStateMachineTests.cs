using NUnit.Framework;

namespace DuskWarung.Battle.Tests
{
    /// <summary>
    /// Drives the whole finite-state machine headlessly (no views) to prove it reaches the
    /// correct terminal outcome. This is exactly the payoff of the model/view split: the combat
    /// loop is testable without the engine's presentation layer.
    /// </summary>
    public class BattleStateMachineTests
    {
        [Test]
        public void StrongPlayer_ReachesVictory()
        {
            var encounter = TestBattlerFactory.Encounter(
                TestBattlerFactory.PlayerDef(hp: 50, atk: 100, def: 0, spd: 10),
                TestBattlerFactory.EnemyDef(hp: 10, atk: 1, def: 0, spd: 5));

            var machine = new BattleStateMachine(encounter, seed: 1);
            DriveToEnd(machine);

            Assert.IsTrue(machine.IsFinished);
            Assert.AreEqual(BattleOutcome.Victory, machine.Outcome);
            Assert.IsFalse(machine.Enemy.IsAlive);
        }

        [Test]
        public void StrongEnemy_ReachesDefeat()
        {
            var encounter = TestBattlerFactory.Encounter(
                TestBattlerFactory.PlayerDef(hp: 5, atk: 1, def: 0, spd: 3),
                TestBattlerFactory.EnemyDef(hp: 50, atk: 100, def: 0, spd: 10));

            var machine = new BattleStateMachine(encounter, seed: 1);
            DriveToEnd(machine);

            Assert.IsTrue(machine.IsFinished);
            Assert.AreEqual(BattleOutcome.Defeat, machine.Outcome);
            Assert.IsFalse(machine.Player.IsAlive);
        }

        // Feeds player commands and presentation-finished signals the way a live scene would,
        // but instantly, so the battle plays out to completion in one loop.
        private static void DriveToEnd(BattleStateMachine machine)
        {
            machine.Start();

            int safety = 2000;
            while (!machine.IsFinished && safety-- > 0)
            {
                if (machine.CurrentState is PlayerTurnState)
                {
                    machine.SubmitPlayerCommand(BattleCommand.Attack(machine.Player, machine.Enemy));
                }
                else if (machine.CurrentState is ActionResolutionState)
                {
                    machine.NotifyPresentationFinished();
                }

                machine.Tick();
            }

            Assert.Greater(safety, 0, "Battle did not terminate within the safety bound.");
        }
    }
}
