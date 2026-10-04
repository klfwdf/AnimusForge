using System;
using Newtonsoft.Json;

namespace AnimusForge;

// One admission bank for all three scheduling lanes. The HTTP adapter calls
// TryAdmit immediately before each send, including retries/thinking fallback.
// No Campaign, host objects, credentials or messages enter this bank.
public sealed class DiplomacyRequestBudget
{
    public const int DailyLimit = 12;
    public const int PlayerReserve = 4;
    private readonly object _gate = new object();
    private int _day = -1, _started, _nonPlayer;
    [JsonProperty("day")] public int Day { get { lock (_gate) return _day; } set { lock (_gate) _day = value; } }
    [JsonProperty("started")] public int Started { get { lock (_gate) return _started; } set { lock (_gate) _started = Math.Max(0, value); } }
    [JsonProperty("nonPlayerStarted")] public int NonPlayerStarted { get { lock (_gate) return _nonPlayer; } set { lock (_gate) _nonPlayer = Math.Max(0, value); } }
    public void AdvanceDay(int day)
    {
        lock (_gate)
        {
            if (_day == day) return;
            _day = day; _started = _nonPlayer = 0;
        }
    }
    public bool CanAdmit(bool player)
    {
        lock (_gate) return _started < DailyLimit && (player || _nonPlayer < DailyLimit - PlayerReserve);
    }
    public bool TryAdmit(bool player)
    {
        lock (_gate)
        {
            if (_started >= DailyLimit || (!player && _nonPlayer >= DailyLimit - PlayerReserve)) return false;
            _started++;
            if (!player) _nonPlayer++;
            return true;
        }
    }
    public DiplomacyRequestBudget Snapshot()
    {
        lock (_gate) return new DiplomacyRequestBudget { Day = _day, Started = _started, NonPlayerStarted = _nonPlayer };
    }
}
