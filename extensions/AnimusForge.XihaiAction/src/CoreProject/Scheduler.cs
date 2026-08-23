using System;
using System.Collections.Generic;

namespace AnimusForge.SceneActions.Core
{
    public sealed class ScheduledItem<T>
    {
        public double ExecuteAtMissionTime { get; set; }
        public long StableSequence { get; set; }
        public T Value { get; set; }
    }

    public sealed class StableScheduler<T>
    {
        private readonly List<ScheduledItem<T>> _heap = new List<ScheduledItem<T>>();
        private long _nextSequence;

        public StableScheduler(int capacity)
        {
            if (capacity < 1)
            {
                throw new ArgumentOutOfRangeException(nameof(capacity));
            }
            Capacity = capacity;
        }

        public int Capacity { get; }
        public int Count => _heap.Count;

        public bool TryEnqueue(double executeAtMissionTime, T value, out long sequence)
        {
            sequence = -1;
            if (_heap.Count >= Capacity || double.IsNaN(executeAtMissionTime))
            {
                return false;
            }

            sequence = ++_nextSequence;
            ScheduledItem<T> item = new ScheduledItem<T>
            {
                ExecuteAtMissionTime = executeAtMissionTime,
                StableSequence = sequence,
                Value = value
            };
            _heap.Add(item);
            SiftUp(_heap.Count - 1);
            return true;
        }

        public bool TryDequeueDue(double missionTime, out ScheduledItem<T> item)
        {
            item = null;
            if (_heap.Count == 0 || _heap[0].ExecuteAtMissionTime > missionTime)
            {
                return false;
            }

            item = _heap[0];
            int last = _heap.Count - 1;
            _heap[0] = _heap[last];
            _heap.RemoveAt(last);
            if (_heap.Count > 0)
            {
                SiftDown(0);
            }
            return true;
        }

        public IReadOnlyList<ScheduledItem<T>> CancelAll()
        {
            List<ScheduledItem<T>> cancelled = new List<ScheduledItem<T>>(_heap);
            _heap.Clear();
            return cancelled;
        }

        private void SiftUp(int index)
        {
            while (index > 0)
            {
                int parent = (index - 1) / 2;
                if (Compare(_heap[parent], _heap[index]) <= 0)
                {
                    break;
                }
                Swap(parent, index);
                index = parent;
            }
        }

        private void SiftDown(int index)
        {
            while (true)
            {
                int left = (index * 2) + 1;
                if (left >= _heap.Count)
                {
                    return;
                }
                int right = left + 1;
                int smallest = right < _heap.Count &&
                               Compare(_heap[right], _heap[left]) < 0
                    ? right
                    : left;
                if (Compare(_heap[index], _heap[smallest]) <= 0)
                {
                    return;
                }
                Swap(index, smallest);
                index = smallest;
            }
        }

        private static int Compare(ScheduledItem<T> left, ScheduledItem<T> right)
        {
            int time = left.ExecuteAtMissionTime.CompareTo(right.ExecuteAtMissionTime);
            return time != 0 ? time : left.StableSequence.CompareTo(right.StableSequence);
        }

        private void Swap(int left, int right)
        {
            ScheduledItem<T> value = _heap[left];
            _heap[left] = _heap[right];
            _heap[right] = value;
        }
    }

    public static class DeterministicSelector
    {
        public static int PickIndex(string requestId, string targetId, int ordinal, int count)
        {
            if (count <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(count));
            }
            unchecked
            {
                uint hash = 2166136261;
                string text = (requestId ?? string.Empty) + "\u001f" +
                              (targetId ?? string.Empty) + "\u001f" + ordinal;
                foreach (char character in text)
                {
                    hash ^= character;
                    hash *= 16777619;
                }
                return (int)(hash % (uint)count);
            }
        }

        public static double PickIndependentStaggerSeconds(
            string requestId,
            string targetId,
            int stepIndex,
            int targetOrdinal,
            double minimumSeconds,
            double maximumSeconds)
        {
            if (stepIndex < 0 || targetOrdinal < 0 ||
                double.IsNaN(minimumSeconds) || double.IsInfinity(minimumSeconds) ||
                double.IsNaN(maximumSeconds) || double.IsInfinity(maximumSeconds) ||
                minimumSeconds < 0d || maximumSeconds < minimumSeconds)
            {
                throw new ArgumentOutOfRangeException(nameof(minimumSeconds));
            }
            if (targetOrdinal == 0)
            {
                return 0d;
            }

            unchecked
            {
                uint hash = 2166136261;
                string text = (requestId ?? string.Empty) + "\u001f" +
                              (targetId ?? string.Empty) + "\u001f" +
                              stepIndex + "\u001f" + targetOrdinal;
                foreach (char character in text)
                {
                    hash ^= character;
                    hash *= 16777619;
                }
                double unit = (hash & 0x00FFFFFFu) / 16777215d;
                return minimumSeconds + ((maximumSeconds - minimumSeconds) * unit);
            }
        }
    }
}
