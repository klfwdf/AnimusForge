using AnimusForge;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

internal static partial class MaterialSupplyReplay
{
    internal static void MaterialBalance()
    {
        var (h,o,j)=Start();
        var rows=new JArray();
        // Incident shape: many cheap personal records displace all complete lore.
        for(int i=0;i<40;i++) rows.Add(new JObject {
            ["key"]="persona:sample-"+i,["known_by"]="b",["source_kind"]="character_setting",
            ["topic_query"]=true,["text"]="Cautious lord; no recorded persecution. Relation to ruler -2." });
        string body="The passage promise applies ONLY during peace; both courts deny any obligation to shelter fugitives. "+new string('x',400);
        for(int i=0;i<6;i++) rows.Add(new JObject {
            ["key"]=i==0?"lore:succession":"lore:account-"+i,["known_by"]="b",
            ["source_kind"]="world_book_account",["text"]=body });
        rows.Add(new JObject { ["key"]="lore:oversized",["known_by"]="b",
            ["source_kind"]="world_book_account",["text"]=new string('z',40000) });
        h.MaterialOverride=rows.ToString();
        o.PrepareSharedRequest(j);
        var sources=(JArray)JObject.Parse(j.UserPrompt)["sources"]!;
        Test.True(sources.Count<=32 && sources.OfType<JObject>().Count(x=>(string)x["source_kind"]! == "world_book_account")==6,
            "complete lore survives 40 cheaper topic-matched personal records at the real candidate request boundary");
        Test.True(sources.OfType<JObject>().Any(x=>(string)x["source_kind"]! == "character_setting")
            && sources.OfType<JObject>().Any(x=>(string)x["key"]! == "world:current"),
            "lore priority preserves current world and useful personal evidence");
        Test.True(sources.OfType<JObject>().Where(x=>(string)x["source_kind"]! == "world_book_account")
            .All(x=>(string)x["text"]! == body && !(bool)x["excerpt"]!)
            && !sources.ToString().Contains("lore:oversized"),
            "only whole records fit: conditions and denials are not clipped to fill a budget");
        string snapshot=j.EventMaterialSnapshot;
        o.ReplaceStorage(JsonConvert.DeserializeObject<WorldDiplomacyStorage>(JsonConvert.SerializeObject(o.CurrentStorage))!);
        j=o.CurrentStorage.Jobs.Single(x=>x.JobId==j.JobId);
        o.PrepareSharedRequest(j);
        Test.True(j.EventMaterialSnapshot==snapshot,"reload retains the same selected evidence rather than resampling story causes");
        Test.True(j.SystemPrompt.Contains(DiplomacyPromptTexts.Get("EventStructureSelection","MISSING")),
            "actual candidate writer receives causal support review even without prior events");
        Complete(o,j,new JObject { ["candidates"]=new JArray(Candidate()) });
        o.PrepareSharedRequest(j);
        var comparison=JObject.Parse(j.UserPrompt);
        Test.True(j.EventSelectionStage=="compare" && comparison["sources"]!.ToString().Contains(body)
            && j.SystemPrompt.Contains(DiplomacyPromptTexts.Get("EventStructureSelection","MISSING")),
            "independent comparison receives the full selected cause and formal support criteria");
        var failed=Compare(); failed["assessments"]![0]!["qualified"]=false;
        failed["assessments"]![0]!["weakness"]="A relation of -2 and caution do not substantiate persecution or a foreign patronage motive.";
        failed["best_equivalent"]=new JArray(); failed["comparison_reason"]="The invented purge is being used as its own causal evidence.";
        Complete(o,j,failed);
        Test.True(j.MaterialSearchPass==1 && j.SuspendedReason=="" && o.CurrentStorage.EventAnnouncements.Count==0,
            "unsupported first-event causality uses existing bounded research rather than publishing or adding a technical hold");
        Console.WriteLine("PASS material balance: full records, real candidate/compare requests, reload and bounded rejection; provider semantics NOT_RUN");
    }
}
