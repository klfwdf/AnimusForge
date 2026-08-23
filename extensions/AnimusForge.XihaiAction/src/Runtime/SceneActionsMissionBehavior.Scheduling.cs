using System.Collections.Generic;
using AnimusForge.SceneActions.Core;

namespace AnimusForge.XihaiAction
{
    internal sealed partial class SceneActionsMissionBehavior
    {
        /// <summary>
        /// Owns the stable queue boundary used by one Mission session. Program barriers
        /// remain in the Mission state machine, while enqueue/dequeue/cancel semantics are
        /// isolated here so another scheduler can be introduced without touching playback.
        /// </summary>
        private sealed class SceneActionScheduleQueue
        {
            private readonly StableScheduler<PlannedTarget> _inner;

            public SceneActionScheduleQueue(int capacity)
            {
                _inner = new StableScheduler<PlannedTarget>(capacity);
            }

            public int Capacity => _inner.Capacity;
            public int Count => _inner.Count;

            public bool TryEnqueue(
                double dueMissionTime,
                PlannedTarget value,
                out long stableSequence)
            {
                return _inner.TryEnqueue(dueMissionTime, value, out stableSequence);
            }

            public bool TryDequeueDue(
                double missionTime,
                out ScheduledItem<PlannedTarget> item)
            {
                return _inner.TryDequeueDue(missionTime, out item);
            }

            public IReadOnlyList<ScheduledItem<PlannedTarget>> CancelAll()
            {
                return _inner.CancelAll();
            }
        }
    }
}
