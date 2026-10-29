using AnimusForge;
using AnimusForge.Refactor.Domain;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

internal static class HistoryFragmentReplay
{
    private static WorldDiplomacyStorage Storage(bool snapshot=false)
    {
        var storage=new WorldDiplomacyStorage();
        string original=string.Concat(Enumerable.Repeat("The envoy alleged an insult; aid is conditional, not executed. 条件未成立，尚未履约。\n",2000)).TrimEnd();
        if(snapshot)
        {
            storage.CanonicalHistory.Snapshot.Content=original; storage.CanonicalHistory.Snapshot.CoveredThroughSequence=50;
            storage.CanonicalHistory.Snapshot.EstimatedTokens=original.Length;
            storage.CanonicalHistory.Snapshot.ProtectedFacts.Add(new(){SourceId="protected",Text="actual historical result"});
            storage.CanonicalHistory.NextSequence=51;
        }
        else
        {
            storage.CanonicalHistory.DeltaEntries.Add(new(){EntryId="giant",Sequence=1,SourceId="original_source",SourceKey="source_key",Kind="event_boundary",
                Verified=false,Text=original,EstimatedTokens=original.Length}); storage.CanonicalHistory.NextSequence=2;
        }
        storage.CanonicalHistory.EstimatedTokens=original.Length;
        return storage;
    }
    private static int _ids;
    private static WorldDiplomacyJob Queue(WorldDiplomacyStorage storage)
    {
        storage.Jobs.Clear();
        WorldDiplomacyHistoryCompressionApplication.EnqueueCompressionJob(storage,storage.CanonicalHistory.NextSequence-1,storage.CanonicalHistory.EstimatedTokens,48000,
            ()=>{},()=> (0,300),()=>"common",text=>text.Length,128000,80,512,72,24,()=>100,()=>4,()=>4096,
            prefix=>prefix+ ++_ids,(job,seq)=>job.HistoryThroughSequence=seq,_=>{},DiplomacyPromptTexts.Get);
        return storage.Jobs.Single();
    }
    private static JObject Result(WorldDiplomacyJob job,bool approved=true) => new() {
        ["source_hash"]=job.CompressionFragmentHash,["offset"]=job.CompressionFragmentOffset,["length"]=job.CompressionFragmentLength,
        ["summary"]="The envoy alleged an insult; aid remains conditional and unexecuted. Source range "+job.CompressionFragmentOffset,
        ["approved"]=approved };
    private static void Commit(WorldDiplomacyStorage storage,WorldDiplomacyJob job,JObject json)
        => WorldDiplomacyHistoryCompressionApplication.CommitCompression(storage,job,json.ToString(),()=>{},s=>s.Length,()=>4,48000,128000,()=>{},_=>{});
    internal static void Run()
    {
        foreach(bool snapshot in new[]{false,true})
        {
            var storage=Storage(snapshot); string original=snapshot?storage.CanonicalHistory.Snapshot.Content:storage.CanonicalHistory.DeltaEntries[0].Text;
            int jobs=0;
            do
            {
                var job=Queue(storage); var work=storage.HistoryFragmentWork;
                Test.True(!string.IsNullOrEmpty(job.CompressionFragmentId) && WorldDiplomacyPromptContractRules.HasCurrentCanonicalPromptContract(job),"fragment uses actual canonical compression job admission contract");
                var messages=WorldDiplomacyLlmMessageApplication.BuildLlmMessagesForJob(job,_=>throw new Exception("full original injected"));
                Test.True(messages.Count==2 && messages.Sum(x=>x.Content.Length)<DiplomacyCompressionBudget.Input(storage),"fragment request excludes full giant history and fits current input budget");
                int offset=work.Offset;
                Commit(storage,job,Result(job));
                if(!job.CompressionFragmentAudit)
                {
                    Test.True(work.Offset==offset && work.PendingSummary.Length>0,"draft alone never advances coverage or replaces source");
                    storage=JsonConvert.DeserializeObject<WorldDiplomacyStorage>(JsonConvert.SerializeObject(storage))!;
                    var host = new ConcurrentOralMigrationReplay.Host();
                    var loaded = new WorldDiplomacyOrchestration(host,new WorldDiplomacyRuntimeState()); host.Owner=loaded;
                    loaded.ReplaceStorage(storage); storage=loaded.CurrentStorage;
                    Test.True(storage.HistoryFragmentWork.PendingSummary.Length>0,"save reload preserves pending audit and exact source range");
                }
                if(storage.HistoryFragmentWork!=null)
                    Test.True((snapshot?storage.CanonicalHistory.Snapshot.Content:storage.CanonicalHistory.DeltaEntries[0].Text)==original,"all intermediate drafts and audited partial ranges leave original intact");
                Test.True(++jobs<100,"finite fragment workflow completes");
            } while(storage.HistoryFragmentWork!=null);
            Test.True(jobs>2,"giant source processed as several audited segments");
            string compact=snapshot?storage.CanonicalHistory.Snapshot.Content:storage.CanonicalHistory.DeltaEntries[0].Text;
            Test.True(compact.Length<original.Length && compact.Contains("conditional") && compact.Contains("alleged"),"whole validated replacement retains uncertainty and pending obligation wording");
            if(snapshot) Test.True(storage.CanonicalHistory.Snapshot.CoveredThroughSequence==50 && storage.CanonicalHistory.Snapshot.ProtectedFacts.Single().SourceId=="protected","snapshot compaction preserves coverage and protected owners");
            else Test.True(storage.CanonicalHistory.DeltaEntries.Single().SourceId=="original_source" && !storage.CanonicalHistory.DeltaEntries[0].Verified,"entry compaction preserves source identity and does not certify allegation");
        }
        var failure=Storage(); var writer=Queue(failure); string before=failure.CanonicalHistory.DeltaEntries[0].Text;
        var wrong=Result(writer); wrong["length"]=1; bool rejected=false;
        try{Commit(failure,writer,wrong);}catch(InvalidOperationException){rejected=true;}
        Test.True(rejected && failure.CanonicalHistory.DeltaEntries[0].Text==before && failure.HistoryFragmentWork.Offset==0,"wrong range cannot publish partial replacement");
        Commit(failure,writer,Result(writer)); var audit=Queue(failure); rejected=false;
        try{Commit(failure,audit,Result(audit,false));}catch(InvalidOperationException){rejected=true;}
        Test.True(rejected && failure.HistoryFragmentWork.PendingSummary=="" && failure.CanonicalHistory.DeltaEntries[0].Text==before,"rejected semantic audit retains original and requires a fresh draft");
        writer=Queue(failure); Commit(failure,writer,Result(writer)); audit=Queue(failure); Commit(failure,audit,Result(audit));
        int advanced=failure.HistoryFragmentWork.Offset; Commit(failure,audit,Result(audit));
        Test.True(failure.HistoryFragmentWork.Offset==advanced,"late duplicate audit cannot cover a range twice");
        failure.CanonicalHistory.DeltaEntries[0].Text+=" New source evidence.";
        failure.Jobs.Clear(); WorldDiplomacyHistoryFragmentApplication.TryQueue(failure,"system",20000,1,s=>s.Length,x=>x,1,80,24,4096,DiplomacyPromptTexts.Get);
        Test.True(DiplomacyCompressionBudget.Held(failure) && failure.CompressionHoldReason=="fragment_original_source_changed" && failure.CanonicalHistory.DeltaEntries[0].Text.EndsWith("New source evidence."),"changed original stops old fragment work and preserves new evidence");

        var changedDraft=Storage(); writer=Queue(changedDraft); Commit(changedDraft,writer,Result(writer)); audit=Queue(changedDraft);
        changedDraft.HistoryFragmentWork.PendingSummary="An unaudited replacement"; rejected=false;
        try{Commit(changedDraft,audit,Result(audit));}catch(InvalidOperationException){rejected=true;}
        Test.True(rejected && changedDraft.HistoryFragmentWork.Offset==0,"audit approval binds exact draft hash, not a later altered summary");
        var corrupt=Storage(); bool damaged=false; int attempts=0;
        while(!DiplomacyCompressionBudget.Held(corrupt) && ++attempts<100)
        {
            var job=Queue(corrupt); Commit(corrupt,job,Result(job));
            if(!damaged && job.CompressionFragmentAudit && corrupt.HistoryFragmentWork!=null)
            { corrupt.HistoryFragmentWork.Pieces[0].Length--; damaged=true; }
        }
        Test.True(damaged && corrupt.CompressionHoldReason=="fragment_incomplete_coverage" && corrupt.CanonicalHistory.DeltaEntries[0].Text==before,
            "damaged or missing coverage cannot become a successful empty or partial archive");

        var layered=Storage(); int highestPass=0, mergeJobs=0;
        do
        {
            var job=Queue(layered); layered.HistoryFragmentWork.TargetTokens=256;
            highestPass=Math.Max(highestPass,layered.HistoryFragmentWork.Pass);
            Commit(layered,job,Result(job));
            Test.True(++mergeJobs<100,"layered fragment merge remains bounded");
        } while(layered.HistoryFragmentWork!=null);
        Test.True(highestPass>0 && layered.CanonicalHistory.DeltaEntries[0].Text.Length<=256,"oversized combined segment summaries are recursively reduced before original replacement");
    }
}
