using System;
using System.Collections.Generic;
using System.Linq;

namespace AnimusForge;

internal sealed class Hero { internal string ClanId, KingdomId; internal float Distance; }
internal sealed class Settlement { }
internal sealed class Clan { internal string Id; }
internal sealed class Kingdom { internal string Id; }
internal readonly struct CampaignVec2
{
    internal static readonly CampaignVec2 Invalid = new CampaignVec2(false);
    private readonly bool _valid;
    internal CampaignVec2(bool valid) { _valid = valid; }
    internal bool IsValid() => _valid;
}
internal static class Logger { internal static void Log(string c, string m) { } }
internal static partial class WorldEntityRetrievalService
{
    private static bool TryResolveHeroCampaignPosition(Hero hero, out CampaignVec2 position)
    { position = hero == null ? CampaignVec2.Invalid : new CampaignVec2(true); return hero != null; }
    private static void PopulateGlobalEntityScopeIds(GlobalEntityCandidate candidate, object value)
    {
        if (value is Hero hero) { candidate.HeroClanId = hero.ClanId; candidate.HeroKingdomId = hero.KingdomId; }
        else if (value is Clan clan) candidate.ScopeClanId = clan.Id;
        else if (value is Kingdom kingdom) candidate.ScopeKingdomId = kingdom.Id;
    }
    private static void PopulateGlobalEntityDistanceMetadata(GlobalEntityCandidate candidate, Hero hero, CampaignVec2? context)
    {
        if (hero == null || !context.HasValue) return;
        float? bonus = EntityInjectionAllocator.ComputeDistanceBonus(hero.Distance);
        if (bonus.HasValue) { candidate.HeroDistance = hero.Distance; candidate.HeroDistanceBonus = bonus.Value; }
    }
    internal static string Live(int cap, int mentions, Hero context, List<EntityMatch<Hero>> heroes, List<EntityMatch<Clan>> clans)
    {
        var settlements = new List<EntityMatch<Settlement>>(); var kingdoms = new List<EntityMatch<Kingdom>>();
        ApplyGlobalInjectionLimit(cap, mentions, context, ref heroes, ref settlements, ref clans, ref kingdoms);
        return Format(heroes.Select(x => (x.Id, x.Score)).Concat(clans.Select(x => (x.Id, x.Score))));
    }
    internal static string Detached(int cap, int mentions, List<EntityMatch<DetachedEntityCandidate>> heroes, List<EntityMatch<DetachedEntityCandidate>> clans)
    {
        var settlements = new List<EntityMatch<DetachedEntityCandidate>>(); var kingdoms = new List<EntityMatch<DetachedEntityCandidate>>();
        ApplyDetachedGlobalInjectionLimit(cap, mentions, ref heroes, ref settlements, ref clans, ref kingdoms);
        return Format(heroes.Select(x => (x.Id, x.Score)).Concat(clans.Select(x => (x.Id, x.Score))));
    }
    private static string Format(IEnumerable<(string Id, float Score)> selected)
        => string.Join("|", selected.Select(x => x.Id + "@" + x.Score.ToString("0.000", System.Globalization.CultureInfo.InvariantCulture)));
}
internal static class Program
{
    private static int _checks;
    private static void Check(bool okay, string reason) { _checks++; if (!okay) throw new Exception("FAIL " + reason); }
    private static WorldEntityRetrievalService.EntityMatch<T> M<T>(T value, string id, string name, string mention, int priority, float score)
        => new WorldEntityRetrievalService.EntityMatch<T> { Value = value, Id = id, Name = name, Mention = mention, MentionPriority = priority, Score = score };
    private static void Run()
    {
        var context = new Hero();
        var near = new Hero { ClanId = "c1", KingdomId = "k1", Distance = 2f };
        var far = new Hero { ClanId = "c2", KingdomId = "k2", Distance = 70f };
        var clan = new Clan { Id = "c1" };
        var liveHeroes = new List<WorldEntityRetrievalService.EntityMatch<Hero>>
        {
            M(far, "hero:far", "Alex", "Alex", 0, 0.96f), M(near, "hero:near", "Alex", "Alex", 0, 0.95f)
        };
        var liveClans = new List<WorldEntityRetrievalService.EntityMatch<Clan>> { M(clan, "clan:c1", "Clan One", "Clan One", 1, 1f) };
        var detachedHeroes = new List<WorldEntityRetrievalService.EntityMatch<WorldEntityRetrievalService.DetachedEntityCandidate>>
        {
            M(new WorldEntityRetrievalService.DetachedEntityCandidate { Id = "hero:far", Name = "Alex", HeroClanId = "c2", HeroKingdomId = "k2", HeroDistance = 70f, HeroDistanceBonus = EntityInjectionAllocator.ComputeDistanceBonus(70f).Value }, "hero:far", "Alex", "Alex", 0, 0.96f),
            M(new WorldEntityRetrievalService.DetachedEntityCandidate { Id = "hero:near", Name = "Alex", HeroClanId = "c1", HeroKingdomId = "k1", HeroDistance = 2f, HeroDistanceBonus = EntityInjectionAllocator.ComputeDistanceBonus(2f).Value }, "hero:near", "Alex", "Alex", 0, 0.95f)
        };
        var detachedClans = new List<WorldEntityRetrievalService.EntityMatch<WorldEntityRetrievalService.DetachedEntityCandidate>>
        { M(new WorldEntityRetrievalService.DetachedEntityCandidate { Id = "clan:c1", Name = "Clan One", ScopeClanId = "c1" }, "clan:c1", "Clan One", "Clan One", 1, 1f) };
        foreach (int cap in new[] { 1, 2, 6 })
        {
            string live = WorldEntityRetrievalService.Live(cap, 2, context, liveHeroes.Select(Clone).ToList(), liveClans.Select(Clone).ToList());
            string detached = WorldEntityRetrievalService.Detached(cap, 2, detachedHeroes.Select(Clone).ToList(), detachedClans.Select(Clone).ToList());
            Check(live == detached, "live/detached selection and score parity cap=" + cap + " live=" + live + " detached=" + detached);
        }
        near.Distance = -1f;
        far.Distance = -1f;
        foreach (var match in detachedHeroes) { match.Value.HeroDistance = float.MaxValue; match.Value.HeroDistanceBonus = 0f; }
        string scopeLive = WorldEntityRetrievalService.Live(1, 2, context, liveHeroes.Select(Clone).ToList(), liveClans.Select(Clone).ToList());
        string scopeDetached = WorldEntityRetrievalService.Detached(1, 2, detachedHeroes.Select(Clone).ToList(), detachedClans.Select(Clone).ToList());
        Check(scopeLive == scopeDetached && scopeLive.StartsWith("hero:near@", StringComparison.Ordinal),
            "scope-only selection parity live=" + scopeLive + " detached=" + scopeDetached);
        Console.WriteLine("PASS knowledge-entity-allocation-parity checks=" + _checks + " source=production-wrappers game=stubbed");
    }
    private static WorldEntityRetrievalService.EntityMatch<T> Clone<T>(WorldEntityRetrievalService.EntityMatch<T> x)
        => M(x.Value, x.Id, x.Name, x.Mention, x.MentionPriority, x.Score);
    private static int Main() { try { Run(); return 0; } catch (Exception ex) { Console.Error.WriteLine(ex.Message); return 1; } }
}
