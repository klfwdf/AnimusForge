using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

namespace AnimusForge;

internal sealed class Clan { internal string StringId; internal Kingdom Kingdom; }
internal sealed class Kingdom { internal string StringId; }
internal sealed class Hero { internal Clan Clan; internal float Distance; }
internal readonly struct CampaignVec2
{
    private readonly float _point;
    internal CampaignVec2(float point) { _point = point; }
    internal bool IsValid() => true;
    internal float Distance(CampaignVec2 other) => Math.Abs(_point - other._point);
}
internal static class EntityInjectionAllocator
{
    internal static float? ComputeDistanceBonus(float distance) => distance < 50 ? 1f / (1f + distance) : null;
}
internal static partial class WorldEntityRetrievalService
{
    private const int EntityRetrievalBudgetCheckInterval = 64;
    private sealed class WorldEntityRetrievalBudget { internal int StopAfterChecks; }
    private static int _budgetChecks;
    private static void LogSoftBudgetOnceIfNeeded(string phase, string category, string mention, int scanned, int total, int selected, WorldEntityRetrievalBudget budget)
    { _budgetChecks++; }
    private static bool IsHardBudgetExceeded(WorldEntityRetrievalBudget budget)
        => budget.StopAfterChecks > 0 && _budgetChecks >= budget.StopAfterChecks;
    private static void LogWorldEntityBudgetStop(string phase, string category, string mention, int scanned, int total, int selected, WorldEntityRetrievalBudget budget)
    { }
    private static string SafeSelectorValue<T>(Func<T, string> selector, T value) where T : class => selector(value);
    private static IEnumerable<string> SafeAliases<T>(Func<T, IEnumerable<string>> selector, T value) where T : class => selector(value);
    private static string NormalizeScopeEntityId(string value) => value ?? "";
    private static Kingdom ResolveHeroKingdomForResidentEntity(Hero hero, Clan clan) => clan?.Kingdom;
    private static bool TryResolveHeroCampaignPosition(Hero hero, out CampaignVec2 position)
    { position = new CampaignVec2(hero.Distance); return true; }
    internal static (int Captured, int Scopes, int Distances, int Checks, double Milliseconds) Measure(Hero[] heroes, bool metadata, int stopAfterChecks = 0)
    {
        var detached = new List<DetachedEntityCandidate>();
        var live = new Dictionary<DetachedEntityCandidate, Hero>();
        _budgetChecks = 0;
        var watch = Stopwatch.StartNew();
        CaptureCandidates(heroes, detached, live, x => new[] { "alias" }, x => "hero", x => "Name", new WorldEntityRetrievalBudget { StopAfterChecks = stopAfterChecks },
            metadata ? (Action<Hero, DetachedEntityCandidate>)((hero, candidate) => CaptureDetachedMetadata(candidate, hero, new CampaignVec2(0))) : null);
        watch.Stop();
        return (detached.Count, detached.Count(x => x.HeroClanId == "c1" && x.HeroKingdomId == "k1"),
            detached.Count(x => x.HeroDistance != float.MaxValue), _budgetChecks, watch.Elapsed.TotalMilliseconds);
    }
}
internal static class Program
{
    private static void Check(bool condition, string message) { if (!condition) throw new Exception("FAIL " + message); }
    private static int Main()
    {
        try
        {
            const int count = 2000;
            var kingdom = new Kingdom { StringId = "k1" };
            var clan = new Clan { StringId = "c1", Kingdom = kingdom };
            var heroes = Enumerable.Range(0, count).Select(i => new Hero { Clan = clan, Distance = i % 40 }).ToArray();
            for (int i = 0; i < 3; i++) WorldEntityRetrievalService.Measure(heroes, true);
            double plain = 0, captured = 0;
            for (int i = 0; i < 9; i++)
            {
                var baseline = WorldEntityRetrievalService.Measure(heroes, false);
                var actual = WorldEntityRetrievalService.Measure(heroes, true);
                Check(actual.Captured == count && actual.Scopes == count && actual.Distances == count,
                    "all candidates retain scope and distance metadata");
                Check(actual.Checks == count / 64, "budget is checked at each 64 candidates");
                plain += baseline.Milliseconds;
                captured += actual.Milliseconds;
            }
            var bounded = WorldEntityRetrievalService.Measure(heroes, true, stopAfterChecks: 1);
            Check(bounded.Captured == 64 && bounded.Checks == 1,
                "hard budget stops after first 64-candidate batch");
            Console.WriteLine($"PASS knowledge-j06-capture-performance candidates={count} scopeMetadata={count} distanceMetadata={count} budgetChecks={count / 64} runs=9 baselineMeanMs={plain / 9:F3} metadataMeanMs={captured / 9:F3} deltaMeanMs={(captured - plain) / 9:F3} source=production-capture-methods game=stubbed");
            return 0;
        }
        catch (Exception ex) { Console.Error.WriteLine(ex.Message); return 1; }
    }
}
