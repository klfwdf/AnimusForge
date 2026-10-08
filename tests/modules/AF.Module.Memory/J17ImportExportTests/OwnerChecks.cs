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
        var authority = new MemoryBusinessStateOwner { Drafts=state.DailyDrafts, Blocks=state.Blocks,
            DailyQueue=state.SummaryQueue, Overviews=state.Overviews, OverviewQueue=state.OverviewQueue };
        var view = MemoryImportExportOwner.Capture(authority);
        Check(ReferenceEquals(view.DailyDrafts,authority.Drafts) && ReferenceEquals(view.SummaryQueue,authority.DailyQueue), "authority capture shares stores not second warehouse");
        authority.Drafts=null; authority.Blocks=null; authority.DailyQueue=null; authority.Overviews=null; authority.OverviewQueue=null;
        Check(MemoryImportExportOwner.ApplyToAuthority("hero",incoming,true,authority,null), "authority apply accepts original import");
        Check(authority.Drafts["hero"][0].GameDayIndex==2 && authority.DailyQueue.Count==1 && authority.OverviewQueue.Count==1, "authority receives replacement stores and queues");
        var before=authority.Drafts;
        Check(MemoryImportExportOwner.ApplyToAuthority("hero",new CompressedMemoryExportBundle(),false,authority,null) && ReferenceEquals(before,authority.Drafts), "skip keeps original store reference");
        var order=new List<string>();
        int count=MemoryDeveloperEditOwner.SaveOverviewForAuthority(()=>{order.Add("id");return "hero";},()=>{order.Add("name");return "Hero";},"saved",()=>{order.Add("blocks");return authority.Blocks["hero"];},()=>{order.Add("ticks");return 77;},authority,x=>order.Add("enqueue"));
        Check(count==1 && order.SequenceEqual(new[]{"id","blocks","name","ticks"}),"overview actual domain capture order and included count preserved");
        Check(authority.Overviews["hero"].UpdatedUtcTicks==77 && authority.Overviews["hero"].Summary=="saved","overview edit publishes same sole authority");
        order.Clear();
        count=MemoryDeveloperEditOwner.SaveOverviewForAuthority(()=>{order.Add("id");return "hero";},()=>{order.Add("name");return "Hero";}," ",()=>{order.Add("blocks");return authority.Blocks["hero"];},()=>{order.Add("ticks");return 78;},authority,x=>order.Add("enqueue"));
        Check(count==-1 && order.SequenceEqual(new[]{"id","name","ticks","blocks","enqueue"}) && !authority.Overviews.ContainsKey("hero"),"overview clear skips early blocks and reschedules only after pointer publish");
        authority.Overviews["hero"] = new() { HeroId="hero", Summary="stale" };
        authority.OverviewQueue.Add(new() { HeroId="hero" });
        order.Clear();
        bool changed=MemoryDeveloperEditOwner.EditBlockForAuthority(()=>order.Add("load"),()=>{order.Add("id");return "hero";},()=>{order.Add("name");return "Hero";},"changed",x=>x.Id="edited",authority,_=>order.Add("dirty"),x=>{Check(!authority.Overviews.ContainsKey("hero") && authority.OverviewQueue.Count==0,"edit clears same overview and publishes before enqueue");order.Add("enqueue");},()=>{order.Add("reload");return authority.Blocks["hero"];},x=>order.Add("log"));
        Check(changed && authority.Blocks["hero"][0].Id=="edited" && order.SequenceEqual(new[]{"load","id","name","dirty","id","reload","enqueue","log"}),"edit original capture order and conditional invalidation: changed="+changed+" id="+authority.Blocks["hero"][0].Id+" order="+string.Join(",",order));
        order.Clear();
        changed=MemoryDeveloperEditOwner.DeleteBlockForAuthority(()=>order.Add("load"),()=>{order.Add("id");return "hero";},"missing",authority,_=>order.Add("dirty"),x=>order.Add("enqueue"),()=>{order.Add("reload");return new();},x=>order.Add("log"));
        Check(!changed && order.SequenceEqual(new[]{"load","id"}) && authority.Blocks["hero"][0].Id=="edited","missing delete preserves state and does not invalidate or reload");
        changed=MemoryDeveloperEditOwner.DeleteBlockForAuthority(()=>{},()=>"hero","edited",authority,_=>{},x=>{},()=>new(),x=>{});
        Check(changed && !authority.Blocks.ContainsKey("hero"),"delete publishes removed block into sole authority");
        order.Clear();
        MemoryDeveloperEditOwner.InvalidateOverviewForAuthority(()=>" ",authority,()=>{order.Add("reload");return new();},x=>order.Add("enqueue"),null,x=>order.Add("log"));
        Check(order.Count==0,"invalid identity skips view lookup reload and logging");
        ImportSchemaChecks.Run();
        Console.WriteLine($"PASS {checks} memory owner assertions");
    }
}

// Store shape dependency: capture/apply uses these same references. Rules above remain controlled historical fixture leaves.
internal sealed class MemoryBusinessStateOwner
{
 internal Dictionary<string,List<MyBehavior.DialogueDay>> History;
 internal Dictionary<string,List<DailyMemoryDraft>> Drafts;
 internal Dictionary<string,List<CompressedMemoryBlock>> Blocks;
 internal List<MemorySummaryJob> DailyQueue;
 internal Dictionary<string,MemoryOverviewState> Overviews;
 internal List<MemoryOverviewJob> OverviewQueue;
}
internal sealed partial class MyBehavior { internal sealed class DialogueDay { } }
