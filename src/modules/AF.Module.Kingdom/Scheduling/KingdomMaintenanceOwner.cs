using System;
using System.Collections.Generic;

namespace AnimusForge;

// Main-thread lifecycle only: T can be a live Kingdom, never sent to a worker.
internal sealed class KingdomMaintenanceOwner<T>
{
    private int _week = -1;
    private List<T> _kingdoms = new List<T>();
    private int _index;
    private int _relationIndex;

    internal bool Complete => _index >= _kingdoms.Count;
    internal void BeginWeek(int week, Func<List<T>> capture)
    {
        if (_week == week) return;
        _week = week;
        _kingdoms = capture();
        _index = 0;
    }
    internal bool TryTake(out T kingdom)
    {
        if (Complete) { kingdom = default(T); return false; }
        kingdom = _kingdoms[_index++];
        return true;
    }
    internal void ResetWeek()
    {
        _week = -1;
        _kingdoms = new List<T>();
        _index = 0;
    }
    internal void ResetRelations() => _relationIndex = 0;
    internal bool AdvanceRelations(int count, Action<int> apply)
    {
        if (count == 0) { ResetRelations(); return true; }
        if (_relationIndex < 0 || _relationIndex >= count) _relationIndex = 0;
        apply(_relationIndex);
        _relationIndex++;
        if (_relationIndex < count) return false;
        ResetRelations();
        return true;
    }
}
