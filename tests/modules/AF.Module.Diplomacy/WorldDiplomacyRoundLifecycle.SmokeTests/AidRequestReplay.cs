using System.Reflection;
using AnimusForge;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

internal static class AidRequestReplay
{
    internal class Host : ConcurrentOralMigrationReplay.Host, IDiplomacyAidHost
    {
        internal bool Enabled = true;
        internal int WarExecutions, CostExecutions;
        internal readonly HashSet<string> Wars = new();
        private static string Key(string a, string b) => string.CompareOrdinal(a, b) < 0 ? a + ":" + b : b + ":" + a;
        internal void War(string a, string b) => Wars.Add(Key(a, b));
        internal void Peace(string a, string b) => Wars.Remove(Key(a, b));
        public override bool PartiesAtWar(string a, string b) => Wars.Contains(Key(a, b));
        public override bool IsAtWarByKingdomIds(string a, string b) => PartiesAtWar(a, b);
        public override bool PartiesAllied(string a, string b) => a != b && b != "enemy" && a != "enemy";
        public override bool WorldDiplomacyEnabled() => Enabled;
        public bool SettleLegacyAidCost(DiplomacyAidRequest r, out string reason) { reason = ""; CostExecutions++; return true; }
        public override IWorldDiplomacyImmediateActionPort ImmediateAction() => new WarPort(this);
        private sealed class WarPort : IWorldDiplomacyImmediateActionPort
        {
            private readonly Host h;
            internal WarPort(Host host) { h = host; }
            public WorldDiplomacyStorage Storage => h.Owner.CurrentStorage;
            public int CurrentDay => h.CurrentDayValue;
            public bool CanAiAuthor(string id, out string reason) { reason = ""; return true; }

            public WorldDiplomacyImmediateActionReceipt DeclareWar(string a, string b, WorldDiplomacyDocument d)
            {
                if (h.PartiesAtWar(a, b)) return new(false, "already_at_war");
                h.WarExecutions++; h.War(a, b); h.Owner.HandleWarDeclared(a, b); return new(true, "actual war");
            }
            public WorldDiplomacyImmediateActionReceipt BreakAlliance(string a, string b, WorldDiplomacyDocument d) => new(false, "not_used");
            public WorldDiplomacyImmediateActionReceipt CancelTrade(string a, string b, WorldDiplomacyDocument d) => new(false, "not_used");
            public void Log(string value) { }
        }
    }
    private static object? Invoke(WorldDiplomacyOrchestration owner, string name, params object[] args) =>
        typeof(WorldDiplomacyOrchestration).GetMethod(name, BindingFlags.Instance | BindingFlags.NonPublic)!.Invoke(owner, args);
    private static (Host h, WorldDiplomacyOrchestration o) Fixture(string caller = "a")
    {
        var h = new Host { PublishEnabled = true, PlayerAwareExecution = true, OralEffectPortsEnabled = true };
        var o = new WorldDiplomacyOrchestration(h, new WorldDiplomacyRuntimeState()); h.Owner = o; h.War(caller, "enemy"); return (h, o);
    }
    internal static void Run()
    {
        var (h, o) = Fixture();
        var request = o.CaptureAidRequest("a", "b", "enemy", "war_1", "native_request");
        Test.True(request != null && request.Status == "requested" && h.WarExecutions == 0, "alliance request is not implicit consent or war");
        Invoke(o, "MaintainAidRequests");
        var round = o.ResolveRound(request!.RoundId); var source = o.ResolveDocument(request.RequestDocumentId);
        Test.True(round.BirthLane == "normal" && source?.IsReadyForPublication == true && source.RequiresResponse, "NPC aid request publishes through shared event owner");
        Test.True(h.WarExecutions == 0 && h.CostExecutions == 0, "request causes no native payment or automatic entry into war");
        o.RecordNativeAidRefusal("a", "b", "enemy");
        Test.True(request.Status == "rejected", "explicit native rejection is persisted without breach penalty");
        h.CurrentDayValue += 100;
        var duplicate = o.CaptureAidRequest("a", "b", "enemy", "war_1", "native_rewording");
        Invoke(o, "MaintainAidRequests");
        Test.True(ReferenceEquals(request, duplicate) && o.CurrentStorage.AidRequests.Count == 1 && request.Status == "rejected", "time and rewording never reopen a refused request");
        var saved = JsonConvert.DeserializeObject<WorldDiplomacyStorage>(JsonConvert.SerializeObject(o.CurrentStorage))!;
        var loaded = new WorldDiplomacyOrchestration(h, new WorldDiplomacyRuntimeState()); h.Owner = loaded; loaded.ReplaceStorage(saved);
        Test.True(loaded.CaptureAidRequest("a", "b", "enemy", "war_1", "native_request").Status == "rejected" && saved.AidRequests.Count == 1, "refusal survives reload");
        Test.True(loaded.CaptureAidRequest("a", "b", "enemy", "war_2", "native_request").Id != request.Id, "new underlying war is a substantive new request");
        Invoke(loaded, "MaintainAidRequests");
        Test.True(saved.ConcurrentRounds.Count == 1, "normal aid admission respects the one-event lane");
        var (ah, ao) = Fixture();
        var accepted = ao.CaptureAidRequest("a", "b", "enemy", "war_1", "legacy_accepted_decision", true, 250);
        Invoke(ao, "MaintainAidRequests");
        Test.True(accepted.Status == "fulfilled" && ah.WarExecutions == 1, "legacy explicit acceptance produces a declaration and real war once");
        Test.True(ah.CostExecutions == 1 && accepted.LegacyCostState == "complete", "already accepted legacy cost settles once only after actual war");
        ah.CurrentDayValue++; Invoke(ao, "MaintainAidRequests");
        Test.True(ah.WarExecutions == 1 && ah.CostExecutions == 1, "maintenance cannot replay accepted aid or cost");
        ah.Peace("b", "enemy"); ah.CurrentDayValue++; Invoke(ao, "MaintainAidRequests");
        Test.True(accepted.Status == "fulfilled" && ah.WarExecutions == 1, "Q50 does not reopen a fulfilled promise after peace or require minimum war duration");
        var (ph, po) = Fixture("p");
        var suggested = po.CaptureAidRequest("p", "b", "enemy", "war_1", "player_suggestion");
        Invoke(po, "MaintainAidRequests");
        Test.True(suggested.Status == "suggested" && po.CurrentStorage.Documents.Count == 0, "automatic native suggestion never forges a player declaration");
        po.CaptureAidRequest("p", "b", "enemy", "war_1", "player_request");
        Invoke(po, "MaintainAidRequests");
        Test.True(suggested.Status == "requested" && po.ResolveDocument(suggested.RequestDocumentId)?.IsPlayerAuthored == true, "explicit player request publishes only after actual player confirmation");
        var (dh, d) = Fixture(); dh.Enabled = false;
        Test.True(d.CaptureAidRequest("a", "b", "enemy", "war_1", "native_request") == null && d.CurrentStorage.AidRequests.Count == 0, "disabled diplomacy releases native request interception");
        dh.Enabled = true; var ended = d.CaptureAidRequest("a", "b", "enemy", "war_1", "native_request"); dh.Peace("a", "enemy");
        Invoke(d, "MaintainAidRequests");
        Test.True(ended.Status == "obsolete" && dh.WarExecutions == 0, "ended underlying war cannot trigger obsolete war draft");
    }
}
