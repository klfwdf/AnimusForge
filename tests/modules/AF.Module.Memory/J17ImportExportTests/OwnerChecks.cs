using System;
using System.Collections.Generic;
using System.Linq;

namespace AnimusForge;

internal sealed class DailyMemoryDraft { public string HeroId = ""; public int GameDayIndex; }
internal sealed class CompressedMemoryBlock { public string Id = ""; public string HeroId = ""; public string HeroName = ""; public int GameDayIndex; }
internal sealed class MemorySummaryJob { public string HeroId = ""; }
internal sealed class MemoryOverviewJob { public string HeroId = ""; }
internal sealed class MemoryOverviewState
{
    public string HeroId = ""; public string HeroName = ""; public string Summary = "";
    public string LastError = ""; public List<string> IncludedBlockIds = new(); public long UpdatedUtcTicks;
    public MemoryOverviewState CopyForSummary() => new() { HeroId = HeroId, HeroName = HeroName,
        Summary = Summary, LastError = LastError, IncludedBlockIds = IncludedBlockIds.ToList(), UpdatedUtcTicks = UpdatedUtcTicks };
}
internal static class MemoryRecordRules
{
    internal static string NormalizeMemoryHeroId(string x) => (x ?? "").Trim().ToLowerInvariant();
    internal static List<DailyMemoryDraft> SanitizeDailyMemoryDrafts(IEnumerable<DailyMemoryDraft> x) => x?.Where(v => v != null).ToList() ?? new();
    internal static List<CompressedMemoryBlock> SanitizeCompressedMemoryBlocks(IEnumerable<CompressedMemoryBlock> x) => x?.Where(v => v != null).ToList() ?? new();
    internal static List<MemorySummaryJob> SanitizeMemorySummaryQueue(IEnumerable<MemorySummaryJob> x) => x?.Where(v => v != null).ToList() ?? new();
    internal static MemoryOverviewState SanitizeMemoryOverviewState(MemoryOverviewState x) => x;
    internal static List<MemoryOverviewJob> SanitizeMemoryOverviewQueue(IEnumerable<MemoryOverviewJob> x) => x?.Where(v => v != null).ToList() ?? new();
    internal static string BuildCompressedMemoryBlockId(string x, int day) => NormalizeMemoryHeroId(x) + ":" + day;
}
internal static class Program
{
    static int checks;
    static void Check(bool ok, string name) { if (!ok) throw new Exception(name); checks++; }
    static MemoryImportExportState State() => new()
    {
        DailyDrafts = new(StringComparer.OrdinalIgnoreCase), Blocks = new(StringComparer.OrdinalIgnoreCase),
        SummaryQueue = new(), Overviews = new(StringComparer.OrdinalIgnoreCase), OverviewQueue = new()
    };
    static void Main()
    {
        var fields = typeof(CompressedMemoryExportBundle).GetFields().Select(x => x.Name).OrderBy(x => x).ToArray();
        Check(fields.SequenceEqual(new[] { "Blocks", "DailyDrafts", "Overview", "OverviewQueue", "SummaryQueue" }), "five wire fields");
        var state = State();
        var original = new DailyMemoryDraft { HeroId = "hero", GameDayIndex = 1 };
        state.DailyDrafts["hero"] = new() { original };
        state.Blocks["hero"] = new() { new() { HeroId = "hero", Id = "b0" } };
        state.SummaryQueue.Add(new() { HeroId = "hero" });
        state.OverviewQueue.Add(new() { HeroId = "hero" });
        state.Overviews["hero"] = new() { HeroId = "hero", Summary = "old" };
        var snapshot = MemoryImportExportOwner.Build(" HERO ", state);
        Check(ReferenceEquals(original, snapshot.DailyDrafts[0]), "main-thread sanitizer alias");
        Check(snapshot.SummaryQueue.Count == 1 && snapshot.OverviewQueue.Count == 1, "snapshot queues");
        Check(!ReferenceEquals(snapshot.Overview, state.Overviews["hero"]), "overview snapshot copy");
        Check(!MemoryImportExportOwner.Apply(" ", snapshot, true, state, null), "bad id rejected");
        Check(!MemoryImportExportOwner.Apply("hero", null, true, state, null), "null bundle rejected");
        int dirty = 0;
        var incoming = new CompressedMemoryExportBundle {
            DailyDrafts = new() { new() { HeroId = "other", GameDayIndex = 2 } },
            Blocks = new() { new() { HeroId = "other", GameDayIndex = 2 } },
            SummaryQueue = new() { new() { HeroId = "other" } },
            OverviewQueue = new() { new() { HeroId = "other" } },
            Overview = new() { HeroId = "other", Summary = "new" }
        };
        Check(MemoryImportExportOwner.Apply("hero", incoming, false, state, _ => dirty++), "merge applies");
        Check(ReferenceEquals(state.DailyDrafts["hero"][0], original) && state.Blocks["hero"][0].Id == "b0", "merge preserves existing");
        Check(state.SummaryQueue.Count == 1 && state.OverviewQueue.Count == 1 && state.Overviews["hero"].Summary == "old", "merge skips queues/overview");
        Check(dirty == 0, "merge does not dirty skipped blocks");
        Check(MemoryImportExportOwner.Apply("hero", incoming, true, state, _ => dirty++), "overwrite applies");
        Check(state.DailyDrafts["hero"][0].GameDayIndex == 2 && state.Blocks["hero"][0].Id == "hero:2", "overwrite and fallback block id");
        Check(state.SummaryQueue.Count == 1 && state.OverviewQueue.Count == 1 && state.Overviews["hero"].Summary == "new", "overwrite queues/overview");
        Check(dirty == 1 && MemoryImportExportOwner.HasData("hero", state), "dirty/has data");
        Check(MemoryDeveloperEditOwner.SaveOverview("hero", "Hero", " A\r\nB ", state.Blocks["hero"], 42, state), "manual overview save");
        Check(state.Overviews["hero"].Summary == "A\nB" && state.Overviews["hero"].IncludedBlockIds.SequenceEqual(new[] { "hero:2" }), "manual overview content/ids");
        Check(!MemoryDeveloperEditOwner.SaveOverview("hero", "Hero", " ", state.Blocks["hero"], 43, state) && !state.Overviews.ContainsKey("hero"), "manual overview clear");
        Check(MemoryImportExportOwner.Apply("hero", new CompressedMemoryExportBundle(), false, state, _ => dirty++), "empty merge applies");
        Check(MemoryImportExportOwner.HasData("hero", state) && dirty == 1, "empty merge preserves data");
        Check(MemoryImportExportOwner.Apply("hero", new CompressedMemoryExportBundle(), true, state, _ => dirty++), "empty overwrite applies");
        Check(!MemoryImportExportOwner.HasData("hero", state) && dirty == 1, "empty overwrite clears all without dirty");
        Check(MemoryImportExportOwner.Apply("hero", incoming, true, state, _ => dirty++), "repopulate for manual clear");
        Check(MemoryDeveloperEditOwner.EditBlock("hero", "Hero", "hero:2", x => x.Id = "changed", state, _ => dirty++), "manual block edit");
        Check(state.Blocks["hero"][0].Id == "changed" && state.Blocks["hero"][0].HeroName == "Hero", "manual block metadata");
        Check(!MemoryDeveloperEditOwner.EditBlock("hero", "Hero", "missing", x => x.Id = "bad", state, _ => dirty++), "missing block refuses edit");
        Check(MemoryDeveloperEditOwner.DeleteBlock("hero", "changed", state, _ => dirty++) && !state.Blocks.ContainsKey("hero"), "manual block delete");
        Check(MemoryDeveloperEditOwner.ParseLineList(" A\r\na\nB ", 2, true).SequenceEqual(new[] { "A", "B" }), "line edit dedup/cap");
        Check(MemoryDeveloperEditOwner.ParseLineList(" A\na ", 2, false).SequenceEqual(new[] { "A", "a" }), "line edit case semantics");
        Check(MemoryDeveloperEditOwner.Clear("hero", state) && !MemoryImportExportOwner.HasData("hero", state), "manual clear five domains");
        Console.WriteLine($"PASS {checks}");
    }
}
