using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.CampaignSystem.Party;

namespace TaleWorlds.CampaignSystem
{
    public class Event { private Action _callback; public void AddNonSerializedListener(object owner, Action callback) => _callback += callback; public void Raise() => _callback?.Invoke(); }
    public class Event<T> { private Action<T> _callback; public void AddNonSerializedListener(object owner, Action<T> callback) => _callback += callback; public void Raise(T a) => _callback?.Invoke(a); }
    public class Event<T,U> { private Action<T,U> _callback; public void AddNonSerializedListener(object owner, Action<T,U> callback) => _callback += callback; public void Raise(T a,U b) => _callback?.Invoke(a,b); }
    public class Event<T,U,V> { private Action<T,U,V> _callback; public void AddNonSerializedListener(object owner, Action<T,U,V> callback) => _callback += callback; public void Raise(T a,U b,V c) => _callback?.Invoke(a,b,c); }
}
namespace TaleWorlds.CampaignSystem.MapEvents
{
    public class Side { public MobileParty LeaderParty; }
    public class RaidEventComponent { public Settlement MapEventSettlement; public Side AttackerSide; }
    public class MapEvent { public Settlement MapEventSettlement; public Side AttackerSide; }
    public class ForceSuppliesEventComponent { public MapEvent MapEvent; }
    public class ForceVolunteersEventComponent { public MapEvent MapEvent; }
}
namespace TaleWorlds.CampaignSystem.LogEntries
{
    public class LogEntry { public long Id; public CampaignTime GameTime; }
    public class LogEntryHistory { public List<LogEntry> GameActionLogs = new(); }
    public class VillageStateChangedLogEntry : LogEntry { public Village Village; public Village.VillageStates NewState; public Hero RaidLeader; }
    public class SiegeAftermathLogEntry : LogEntry {
        private Hero _decisionMaker; public SiegeAftermathLogEntry(Hero actor) { _decisionMaker = actor; }
        public Settlement CapturedSettlement; public Actions.SiegeAftermathAction.SiegeAftermath SiegeAftermath;
    }
}
