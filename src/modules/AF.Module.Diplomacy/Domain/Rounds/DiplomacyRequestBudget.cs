using System;
using Newtonsoft.Json;

namespace AnimusForge;

// Independent player accounting and ordinary NPC admission. The HTTP adapter calls
// TryAdmit immediately before each send, including retries/thinking fallback.
// No Campaign, host objects, credentials or messages enter this bank.
public sealed class DiplomacyRequestBudget
{
    public const int DailyLimit = 12;
    public const int PlayerReserve = 0;
    private readonly object _gate = new object();
    private int _day = -1, _started, _nonPlayer;
    private int _player;
    private int _schemaVersion;
    [JsonProperty("schemaVersion")] public int SchemaVersion { get { lock (_gate) return _schemaVersion; } set { lock (_gate) _schemaVersion = value; } }
    [JsonProperty("playerStarted")] public int PlayerStarted { get { lock (_gate) return _player; } set { lock (_gate) _player = Math.Max(0, value); } }
    [JsonProperty("day")] public int Day { get { lock (_gate) return _day; } set { lock (_gate) _day = value; } }
    [JsonProperty("started")] public int Started { get { lock (_gate) return _started; } set { lock (_gate) _started = Math.Max(0, value); } }
    [JsonProperty("nonPlayerStarted")] public int NonPlayerStarted { get { lock (_gate) return _nonPlayer; } set { lock (_gate) _nonPlayer = Math.Max(0, value); } }
    public void AdvanceDay(int day)
    {
        lock (_gate)
        {
            UpgradeLegacyNoLock();
            if (_day == day) return;
            _day = day; _started = _nonPlayer = _player = 0;
        }
    }
    public bool CanAdmit(bool player)
    {
        lock (_gate) { UpgradeLegacyNoLock(); return player || _nonPlayer < DailyLimit; }
    }
    public bool TryAdmit(bool player)
    {
        lock (_gate)
        {
            UpgradeLegacyNoLock();
            if (player) { _player++; return true; }
            if (_nonPlayer >= DailyLimit) return false;
            _started = ++_nonPlayer;
            return true;
        }
    }
    public DiplomacyRequestBudget Snapshot()
    {
        lock (_gate)
        {
            UpgradeLegacyNoLock();
            return new DiplomacyRequestBudget { SchemaVersion = _schemaVersion, Day = _day, Started = _started, NonPlayerStarted = _nonPlayer, PlayerStarted = _player };
        }
    }
    private void UpgradeLegacyNoLock()
    {
        if (_schemaVersion >= 1) return;
        _player += Math.Max(0, _started - _nonPlayer);
        _started = _nonPlayer;
        _schemaVersion = 1;
    }
}
