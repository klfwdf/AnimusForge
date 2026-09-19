using System;
using System.Collections.Generic;
using System.Globalization;

namespace AnimusForge.Refactor.Runtime;

/// <summary>
/// Stable day/name ordering with cooperative merge and publication-copy work.
/// The owner keeps the result private and validates its source before publishing.
/// Allocation/key capture is still atomic O(N); a string comparison is not preemptible.
/// </summary>
internal sealed class CooperativeMemoryQueueSort<T>
{
    private struct Entry
    {
        internal T Value;
        internal int Day;
        internal string Name;
    }

    private readonly List<T> _result;
    private readonly CompareInfo _compareInfo;
    private Entry[] _input, _output;
    private int _width = 1, _start, _left, _middle, _right, _end, _write, _copy;
    private bool _runActive;

    internal CooperativeMemoryQueueSort(List<T> values, Func<T, int> day, Func<T, string> name)
    {
        _result = values;
        _compareInfo = CultureInfo.CurrentCulture.CompareInfo;
        _input = new Entry[values.Count];
        _output = new Entry[values.Count];
        // Capture scalar keys in the same uninterrupted owner turn as normalization.
        // Later comparisons never re-read live DTO fields (including transient ABA writes).
        for (int i = 0; i < values.Count; i++)
            _input[i] = new Entry { Value = values[i], Day = day(values[i]), Name = name(values[i]) };
    }

    internal bool IsCultureCurrent => _compareInfo.Equals(CultureInfo.CurrentCulture.CompareInfo);
    internal List<T> Result => _result;

    internal bool Step(MemoryMaintenanceWorkBudget budget)
    {
        int count = _input.Length;
        while (_width < count)
        {
            if (!_runActive)
            {
                _left = _start;
                _middle = _right = _start + Math.Min(_width, count - _start);
                _end = _middle + Math.Min(_width, count - _middle);
                _write = _start;
                _runActive = true;
            }
            while (_write < _end)
            {
                if (!budget.Take(false)) return false;
                // One grant covers at most one comparison and one entry move.
                bool takeLeft = _right >= _end || (_left < _middle && Compare(_input[_left], _input[_right]) <= 0);
                _output[_write++] = takeLeft ? _input[_left++] : _input[_right++];
            }
            _runActive = false;
            _start = _end;
            if (_start == count)
            {
                var swap = _input; _input = _output; _output = swap;
                _start = 0;
                _width = _width >= count - _width ? count : _width * 2;
            }
        }
        while (_copy < count)
        {
            if (!budget.Take(false)) return false;
            _result[_copy] = _input[_copy].Value;
            _copy++;
        }
        return true;
    }

    private int Compare(Entry a, Entry b)
    {
        int day = a.Day.CompareTo(b.Day);
        return day != 0 ? day : _compareInfo.Compare(a.Name, b.Name, CompareOptions.None);
    }
}
