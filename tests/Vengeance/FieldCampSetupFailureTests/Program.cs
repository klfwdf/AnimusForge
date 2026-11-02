using AnimusForge;
using TaleWorlds.Library;
using TaleWorlds.CampaignSystem;

static class Program
{
    static int checks;
    static void Check(bool ok, string why) { checks++; if (!ok) throw new Exception(why); }
    static FieldCampMissionLogic Run(params string[] failures)
    {
        Fixture.Reset(failures);
        var host = new FieldCampMissionLogic(); host.RunSetup();
        Check(host.Mission.EndCalls == 0, "never request mission exit: " + string.Join(",", failures));
        Check(Fixture.Calls.Count(x => x == "cache") == 1, "one ground cache attempt after props");
        Check(Fixture.Calls.IndexOf("cache") > Fixture.Calls.IndexOf("props"), "cache after final prop attempt");
        return host;
    }
    static void Main()
    {
        var host = Run();
        Check(host.Initialized && FieldCampBehavior.Instance.PlayerReady, "normal camp remains usable and charges only after player is ready");
        Check(!host.Degraded && !Fixture.Calls.Contains("notice"), "successful camp has no false failure notice");
        foreach (string stage in new[] { "layout", "site", "clutter", "preset", "props", "cache", "troops", "companions", "lords", "prisoners", "horses", "envoys" })
        {
            host = Run(stage);
            Check(host.Initialized && FieldCampBehavior.Instance.PlayerReady, "player still spawned after " + stage + " failure");
            Check(host.Degraded && Fixture.Calls.Contains("notice"), "degradation shown for " + stage);
            Check(Fixture.Calls.Contains("prisoners"), "later actor stages survive " + stage);
            if (stage == "site") Check(Fixture.Calls.Contains("fallback"), "failed full site tries player-ground fallback");
        }
        host = Run("site", "fallback");
        Check(host.Initialized, "unavailable full/fallback layout still reaches independent player spawn");
        host = Run("player");
        Check(!host.Initialized && !FieldCampBehavior.Instance.PlayerReady, "no initialized player means no patrol-ready receipt");
        Check(!Fixture.Calls.Contains("horses") && !Fixture.Calls.Contains("envoys"), "no optional life owners without a player");
        Check(Fixture.Calls.Contains("troops") && Fixture.Calls.Contains("prisoners"), "player exception does not abort other actor cleanup/creation");
        host = Run("team");
        Check(!host.Initialized && !FieldCampBehavior.Instance.PlayerReady, "team creation exception stays open and cannot charge a visit");
        host = Run("props", "notice");
        Check(host.Initialized && host.Degraded, "a notification exception cannot exit the camp");
        host = Run("placement_degraded");
        Check(host.Initialized && host.Degraded, "optional skipped tent/furniture reports degradation without cancelling actors");
        Fixture.Reset(Array.Empty<string>());Fixture.NoPlayer = true;host = new FieldCampMissionLogic();host.RunSetup();
        Check(!host.Initialized && !FieldCampBehavior.Instance.PlayerReady && host.Mission.EndCalls == 0, "no safe player position stays open without a time receipt");
        Check(Fixture.Logs.Any(x => x.Contains("player=False")), "missing player is logged, not falsely claimed ready");
        Console.WriteLine($"PASS {checks} camp setup failure assertions (linked production initialization; fake native scene/actors)");
    }
}

namespace AnimusForge
{
    static class Fixture
    {
        internal static HashSet<string> Failures = new();
        internal static List<string> Calls = new(), Logs = new();
        internal static bool NoPlayer;
        internal static void Reset(IEnumerable<string> failures)
        { Failures = new(failures); Calls.Clear(); Logs.Clear(); NoPlayer = false; FieldCampBehavior.Instance = new(); }
        internal static void Step(string stage) { Calls.Add(stage); if (Failures.Contains(stage)) throw new InvalidOperationException("injected " + stage); }
    }
    sealed class FakeScene { }
    sealed class FakeAgent { internal bool IsActive() => true; }
    sealed class FakeTeam { }
    sealed class FakeTeams
    {
        internal FakeTeam Add(TaleWorlds.Core.BattleSideEnum side, uint c1, uint c2, object banner, bool a, bool b)
        { Fixture.Step("team"); return new(); }
    }
    sealed class FakeMission
    {
        internal FakeScene Scene = new(); internal string SceneName = "native_scene_stub";
        internal FakeAgent MainAgent; internal FakeTeam PlayerTeam; internal FakeTeams Teams = new();
        internal int EndCalls; internal void EndMission() => EndCalls++;
    }
    sealed class FieldCampLayout
    {
        internal enum PropKind { ExecutionBlock, Gallows, Tent }
        internal struct Prop { internal PropKind Kind; }
        internal List<Prop> Props = new();
        internal float HalfX, Back, Front; internal Vec2 PlayerFacing;
        internal static FieldCampLayout Build(List<object> parties, int agents, int tents)
        { Fixture.Step("layout"); return new(); }
    }
    static class FieldCampSettings { internal static int AgentCap => 80; internal static int TentCap => 20; }
    static class FieldCampSite
    {
        internal static object Resolve(FakeMission m, FieldCampLayout l, out float s) { s = 1f;Fixture.Step("site");return new(); }
        internal static object ResolveFallback(FakeMission m, FieldCampLayout l) { Fixture.Step("fallback");return new(); }
        internal static int ClearClutter(FakeScene s, object f, FieldCampLayout l, float k) { Fixture.Step("clutter");return 2; }
        internal static void InitializeGroundCache(FakeScene s, object f, FieldCampLayout l, float k) => Fixture.Step("cache");
    }
    static class FieldCampProps
    {
        internal static bool HadPlacementFailures => Fixture.Failures.Contains("placement_degraded");
        internal static int Place(FakeScene s, FieldCampLayout l, object f, float k) { Fixture.Step("props");return 4; }
    }
    sealed class FieldCampBehavior
    {
        internal static FieldCampBehavior Instance = new(); internal bool PlayerReady;
        internal void MarkPatrolPlayerReady() => PlayerReady = true;
    }
    static class Logger { internal static void Log(string source, string line) => Fixture.Logs.Add(line); }
    static class FieldCampText { internal static object SetupDegraded() => new(); }
    static class AnimusForgeQuickInfo { internal static void Show(object text) => Fixture.Step("notice"); }
    public sealed partial class FieldCampMissionLogic
    {
        const string LogSource = "FieldCamp";
        List<object> _parties = new(), _agents = new(), _prisoners = new();
        FieldCampLayout _layout; object _campFrame; float _campScale = 1f; FakeTeam _team;
        internal FakeMission Mission = new();
        internal bool Initialized => _campInitialized; internal bool Degraded => _setupDegraded;
        internal void RunSetup() => SetUpCamp();
        bool TryPlaceExecutionPreset() { Fixture.Step("preset");return false; }
        void SpawnPlayer() { Fixture.Step("player");if (!Fixture.NoPlayer) Mission.MainAgent = new(); }
        void SpawnTroops() => Fixture.Step("troops");
        void SpawnCompanions() => Fixture.Step("companions");
        void SpawnArmyLords() => Fixture.Step("lords");
        void SpawnPrisoners() => Fixture.Step("prisoners");
        int SpawnHorses() { Fixture.Step("horses");return 3; }
        void QueueEnvoys() => Fixture.Step("envoys");
    }
}
namespace TaleWorlds.Library { struct Vec2 { internal static Vec2 Forward => new(); } }
namespace TaleWorlds.Core { enum BattleSideEnum { Attacker } }
namespace TaleWorlds.CampaignSystem
{
    sealed class Faction { internal uint Color = 1, Color2 = 2; }
    sealed class Clan { internal object Banner; }
    sealed class Hero { internal static Hero MainHero = new(); internal Faction MapFaction = new(); internal Clan Clan = new(); }
}
