using System;
#pragma warning disable CS0649 // Fixture fields intentionally default to idle/alive.
using System.Collections.Generic;
using System.Linq;
using TaleWorlds.CampaignSystem.Map;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.Library;

namespace TaleWorlds.Library
{
    [AttributeUsage(AttributeTargets.Property)] internal sealed class CachedDataAttribute : Attribute { }
    internal static class Debug
    { internal static void FailedAssert(string text, string file, string member, int line) => throw new Exception(text); }
    internal static class MathF { internal static int Floor(float value) => (int)Math.Floor(value); }
    public struct Vec2
    {
        public float x, y;
        public Vec2(float px, float py) { x = px; y = py; }
        public Vec2 ToVec2() => this;
        public float DistanceSquared(Vec2 other) => (x - other.x) * (x - other.x) + (y - other.y) * (y - other.y);
        public static Vec2 operator +(Vec2 a, Vec2 b) => new Vec2(a.x + b.x, a.y + b.y);
        public static Vec2 operator -(Vec2 a, Vec2 b) => new Vec2(a.x - b.x, a.y - b.y);
    }
}
internal sealed class DuelSettings { }
internal sealed class Clan { }
internal sealed class Kingdom { }
internal sealed class Faction { internal bool IsAtWarWith(Faction other) => false; }
internal sealed class Hero
{
    internal static Hero MainHero = new Hero();
    internal bool IsLord = true, IsPrisoner, IsDead;
    internal Clan Clan = new Clan();
}
namespace TaleWorlds.CampaignSystem.Party
{
    internal sealed class MobileParty : ILocatable<MobileParty>
    {
        internal static LocatorGrid<MobileParty> Grid = new LocatorGrid<MobileParty>();
        internal static IEnumerable<MobileParty> AllLordParties => throw new Exception("global enumeration forbidden");
        internal bool IsActive = true, IsVisible = true, IsLordParty = true;
        internal Hero LeaderHero = new Hero();
        internal object MapEvent, CurrentSettlement, Army, BesiegedSettlement;
        internal Faction MapFaction = new Faction();
        internal float SeeingRange = 10f, RouteDistance = 5f;
        internal bool AtSea;
        internal Vec2 Position;
        public int LocatorNodeIndex { get; set; } = -1;
        public MobileParty NextLocatable { get; set; }
        public Vec2 GetPosition2D => Position;
        internal static LocatableSearchData<MobileParty> StartFindingLocatablesAroundPosition(Vec2 position, float radius)
            => Grid.StartFindingLocatablesAroundPosition(position, radius);
        internal static MobileParty FindNextLocatable(ref LocatableSearchData<MobileParty> search) => Grid.FindNextLocatable(ref search);
    }
}
internal static class MapSeaContextGuard
{ internal static bool IsMobilePartyAtSeaOrOnWater(MobileParty party) => party.AtSea; }
internal sealed partial class ProactiveNpcRequestBehavior
{
    internal int ExpensiveSnapshots, RouteCalls;
    private sealed class ProactiveCandidate { }
    private sealed class Cooldown { internal bool IsHeroOnCooldown(string key, float day) => false; }
    private readonly Cooldown _cooldownOwner = new Cooldown();
    private static float Clamp(float value, float min, float max) => Math.Max(min, Math.Min(max, value));
    private static string GetHeroKey(Hero hero) => "fixture";
    private static float NowDays() => 0f;
    private static Kingdom ResolveHeroKingdom(Hero hero) => new Kingdom();
    private static bool CanBuildWartimeDiplomacyCandidate(Hero hero, Kingdom kingdom) => false;
    private static bool TryGetPlayerNativeActivityBusyReason(MobileParty party, out string reason) { reason = ""; return false; }
    private float GetDistanceToMainParty(MobileParty party, MobileParty main) { RouteCalls++; return party.RouteDistance; }
    internal bool Qualify(MobileParty party, MobileParty main, DuelSettings settings, out string reason)
        => TryBuildBaseCandidate(party, main, settings, out _, out reason);
    internal static List<MobileParty> Capture(MobileParty main) => CaptureNearbyRequestParties(main);
    internal static float Radius(MobileParty main) => GetProactiveContactRange(main);
}
internal static class Program
{
    private static int Checks;
    private static void Check(bool value, string label) { Checks++; if (!value) throw new Exception(label); }
    private static MobileParty Party(float x, float y = 0f)
    {
        var party = new MobileParty { Position = new Vec2(x, y) };
        MobileParty.Grid.UpdateLocator(party);
        return party;
    }
    private static void Main()
    {
        var settings = new DuelSettings();
        var main = Party(0f);
        var near = Party(3f, 4f);
        var boundary = Party(20f);
        var outsideMargin = Party(20.005f);
        var invisible = Party(1f); invisible.IsVisible = false;
        var inactive = Party(1f); inactive.IsActive = false;
        var bandit = Party(1f); bandit.IsLordParty = false;
        for (int i = 0; i < 2000; i++) Party(500f + i);
        var captured = ProactiveNpcRequestBehavior.Capture(main);
        Check(captured.Count == 2 && captured.Contains(near) && captured.Contains(boundary), "only nearby visible active lords captured; inclusive boundary");
        Check(!captured.Contains(outsideMargin), "locator query margin must not enlarge contact range");
        Check(captured.Distinct().Count() == captured.Count, "hashed grid returns no duplicate candidates");
        Check(ProactiveNpcRequestBehavior.Radius(main) == 20f, "contact range is fixed at twice player sight range");
        main.SeeingRange = 5f;
        Check(ProactiveNpcRequestBehavior.Radius(main) == 10f, "fixed multiplier follows changing sight range");
        main.SeeingRange = 100f;
        Check(ProactiveNpcRequestBehavior.Radius(main) == 200f, "fixed multiplier has no configurable cap");
        main.SeeingRange = 0f;
        Check(ProactiveNpcRequestBehavior.Radius(main) == 1f, "minimum radius retained");
        main.SeeingRange = 10f;
        var host = new ProactiveNpcRequestBehavior();
        outsideMargin.Position = new Vec2(40.01f, 0f);
        Check(!host.Qualify(outsideMargin, main, settings, out var reason) && reason == "out_of_range"
            && host.RouteCalls == 0 && host.ExpensiveSnapshots == 0, "far party rejected before route or snapshots");
        near.Position = new Vec2(50f, 0f);
        Check(!host.Qualify(near, main, settings, out reason) && reason == "out_of_range"
            && host.RouteCalls == 0 && host.ExpensiveSnapshots == 0, "captured party moving away rejected before heavy work");
        near.Position = new Vec2(2f, 0f); near.RouteDistance = 21f;
        Check(!host.Qualify(near, main, settings, out reason) && reason == "out_of_range"
            && host.RouteCalls == 1 && host.ExpensiveSnapshots == 0, "nearby party with long route rejected before snapshots");
        near.RouteDistance = -1f;
        Check(!host.Qualify(near, main, settings, out reason) && reason == "distance_invalid" && host.ExpensiveSnapshots == 0, "invalid route rejects");
        near.RouteDistance = 20f;
        Check(host.Qualify(near, main, settings, out reason) && host.ExpensiveSnapshots == 1, "inclusive route boundary reaches snapshots");
        near.RouteDistance = 4f; near.AtSea = true;
        Check(!host.Qualify(near, main, settings, out reason) && host.ExpensiveSnapshots == 1, "native sea exclusion retained");
        near.AtSea = false; near.MapEvent = new object();
        Check(!host.Qualify(near, main, settings, out reason) && host.ExpensiveSnapshots == 1, "busy party exclusion retained");
        MobileParty.Grid = new LocatorGrid<MobileParty>();
        Check(ProactiveNpcRequestBehavior.Capture(main).Count == 0, "empty neighborhood produces no candidates");
        Console.WriteLine("PASS nearby scan " + Checks + " assertions; native locator + production capture/prefix; game and snapshot tail substituted");
    }
}
