using System.Collections.Generic;
using System.Linq;

namespace DuskWarung.Battle
{
    /// <summary>
    /// Turn order for one round, ordered by Speed (highest first). Rebuilt each round
    /// from the living battlers so the dead never get a turn.
    /// </summary>
    public class TurnQueue
    {
        private readonly Queue<BattlerRuntime> _queue = new Queue<BattlerRuntime>();

        /// <summary>True while battlers remain to act this round.</summary>
        public bool HasNext => _queue.Count > 0;

        /// <summary>Number of battlers still waiting to act this round.</summary>
        public int Count => _queue.Count;

        /// <summary>Clears and refills the queue with the living battlers, fastest first.</summary>
        public void Rebuild(IEnumerable<BattlerRuntime> battlers)
        {
            _queue.Clear();
            if (battlers == null)
            {
                return;
            }

            foreach (BattlerRuntime battler in battlers.Where(b => b != null && b.IsAlive)
                                                       .OrderByDescending(b => b.Speed))
            {
                _queue.Enqueue(battler);
            }
        }

        /// <summary>Dequeues the next battler to act. Check <see cref="HasNext"/> first.</summary>
        public BattlerRuntime Next() => _queue.Dequeue();
    }
}
