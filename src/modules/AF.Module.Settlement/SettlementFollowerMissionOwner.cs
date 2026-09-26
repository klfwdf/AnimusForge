using System;
using System.Collections.Generic;

namespace AnimusForge.Refactor.Modules;

// Tracks actual Agent identities for one SETS Mission; indexes are only lookup keys.
internal sealed class SettlementFollowerMissionOwner<TMission, TAgent>
    where TMission : class where TAgent : class
{
    private readonly Dictionary<int, TAgent> _agentsByIndex = new Dictionary<int, TAgent>();

    internal TMission Mission { get; private set; }
    internal bool Active { get; private set; }
    internal int Count => _agentsByIndex.Count;

    internal bool SetActive(TMission mission, bool active)
    {
        bool changed = mission == null || !ReferenceEquals(Mission, mission);
        if (changed)
        {
            _agentsByIndex.Clear();
            Mission = mission;
        }
        Active = active && mission != null;
        return changed;
    }

    internal bool Register(int index, TAgent agent)
    {
        if (!Active || Mission == null || index < 0 || agent == null) return false;
        if (_agentsByIndex.TryGetValue(index, out TAgent existing)
            && ReferenceEquals(existing, agent)) return false;
        _agentsByIndex[index] = agent;
        return true;
    }

    internal bool IsTracked(TMission currentMission, int index, TAgent agent)
        => Active && ReferenceEquals(Mission, currentMission) && index >= 0 && agent != null
            && _agentsByIndex.TryGetValue(index, out TAgent tracked)
            && ReferenceEquals(tracked, agent);

    // Retained for the legacy index-only internal overload; callers with Agent use IsTracked.
    internal bool ContainsIndex(TMission currentMission, int index)
        => Active && ReferenceEquals(Mission, currentMission) && index >= 0
            && _agentsByIndex.ContainsKey(index);

    internal bool Remove(int index, TAgent agent)
    {
        if (index < 0 || agent == null || !_agentsByIndex.TryGetValue(index, out TAgent tracked)
            || !ReferenceEquals(tracked, agent)) return false;
        return _agentsByIndex.Remove(index);
    }

    internal void Clear()
    {
        _agentsByIndex.Clear();
        Mission = null;
        Active = false;
    }
}
