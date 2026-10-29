using System.Reflection;
using AnimusForge;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

internal static class CompressionCapacityReplay
{
    private sealed class Host : ConcurrentOralMigrationReplay.Host, IDiplomacyModelConfiguration
    {
        internal string Fingerprint = "provider-a";
        internal int Reads;
        public string ConfigurationFingerprint() { Reads++; return Fingerprint; }
    }
    internal static void Run()
    {
        var storage = new WorldDiplomacyStorage();
        for (int i = 1; i <= 50; i++) storage.CanonicalHistory.DeltaEntries.Add(new() {
            Sequence=i, SourceId="source"+i, SourceKey="key"+i, Text=new string('x',2000), EstimatedTokens=2000 });
        storage.CanonicalHistory.NextSequence=51; storage.CanonicalHistory.EstimatedTokens=100000;
        WorldDiplomacyJob Enqueue()
        {
            storage.Jobs.Clear();
            WorldDiplomacyHistoryCompressionApplication.EnqueueCompressionJob(storage,50,100000,48000,()=>{},()=> (0,300),()=>"common",
                text=>text.Length,128000,80,512,72,24,()=>100,()=>4,()=>4096,prefix=>prefix+"id",(job,seq)=>job.HistoryThroughSequence=seq,_=>{});
            return storage.Jobs.Single();
        }
        var first=Enqueue(); long firstCutoff=first.CompressionThroughSequence;
        Test.True(firstCutoff>0 && firstCutoff<50, "initial compression batch bounded by internal provider input budget");
        WorldDiplomacyFailureApplication.Commit(first,"context_length_exceeded",storage,72,2,()=>100,null,null,null,null,null,null,id=>storage.Jobs.RemoveAll(x=>x.JobId==id),_=>{});
        var second=Enqueue();
        Test.True(second.CompressionThroughSequence<firstCutoff && second.CompressionThroughSequence>0, "observed capacity failure shrinks real next queued prefix");
        Test.True(storage.CanonicalHistory.DeltaEntries.Count==50 && storage.CanonicalHistory.Snapshot.CoveredThroughSequence==0, "failed batch preserves all original history");
        for(int i=1;i<4;i++) DiplomacyCompressionBudget.Fail(storage,second,"invalid output",100+i,2,72);
        Test.True(DiplomacyCompressionBudget.Held(storage) && storage.CompressionRetryAfterHour==int.MaxValue, "four failures stop timer-driven retries");
        int scheduled=0;
        WorldDiplomacyHistoryCompressionApplication.TryScheduleTokenCompression(storage,()=>true,()=>{},()=>{},()=>int.MaxValue,1,100,(a,b,c)=>scheduled++);
        Test.True(scheduled==0, "elapsed time cannot restart exhausted compression");
        storage=JsonConvert.DeserializeObject<WorldDiplomacyStorage>(JsonConvert.SerializeObject(storage))!;
        Test.True(DiplomacyCompressionBudget.Held(storage) && DiplomacyCompressionBudget.Input(storage)==16384, "save reload preserves both technical hold and observed capacity");
        DiplomacyCompressionBudget.Resume(storage,false);
        Test.True(!DiplomacyCompressionBudget.Held(storage) && DiplomacyCompressionBudget.Input(storage)==16384, "explicit re-enable resumes a finite round without forgetting measured capacity");
        var original=storage.CanonicalHistory;
        var valid=Enqueue();
        bool rejected=false;
        try { WorldDiplomacyHistoryCompressionApplication.CommitCompression(storage,valid,"{\"summary\":\"wrong\",\"covered_through_sequence\":999}",()=>{},s=>s.Length,()=>5,48000,128000,()=>{},_=>{}); }
        catch(InvalidOperationException) { rejected=true; }
        Test.True(rejected && ReferenceEquals(original,storage.CanonicalHistory) && original.DeltaEntries.Count==50,"invalid cutoff cannot replace original archive");
        WorldDiplomacyHistoryCompressionApplication.CommitCompression(storage,valid,new JObject { ["summary"]="Source-qualified summary",["covered_through_sequence"]=valid.CompressionThroughSequence }.ToString(),()=>{},s=>s.Length,()=>5,48000,128000,()=>{},_=>{});
        Test.True(storage.CanonicalHistory.Snapshot.CoveredThroughSequence==valid.CompressionThroughSequence && storage.CanonicalHistory.DeltaEntries.All(x=>x.Sequence>valid.CompressionThroughSequence),"verified prefix commit preserves entire newer suffix");
        var host=new Host(); var owner=new WorldDiplomacyOrchestration(host,new WorldDiplomacyRuntimeState()); host.Owner=owner;
        var refresh=typeof(WorldDiplomacyOrchestration).GetMethod("RefreshCompressionConfiguration",BindingFlags.Instance|BindingFlags.NonPublic)!;
        refresh.Invoke(owner,null); refresh.Invoke(owner,null);
        Test.True(host.Reads==1,"model fingerprint read at most once per campaign hour");
        owner.CurrentStorage.CompressionRetryAttempts=4; owner.CurrentStorage.CompressionInputBudgetTokens=8192;
        host.CurrentDayValue++; refresh.Invoke(owner,null);
        Test.True(DiplomacyCompressionBudget.Held(owner.CurrentStorage),"unchanged provider never resets failure count by time alone");
        host.Fingerprint="provider-b"; host.CurrentDayValue++; refresh.Invoke(owner,null);
        Test.True(!DiplomacyCompressionBudget.Held(owner.CurrentStorage) && DiplomacyCompressionBudget.Input(owner.CurrentStorage)==32768,"real provider configuration change resumes with new capacity baseline");
        DiplomacyCompressionBudget.Fail(owner.CurrentStorage,new(){CompressionConfigurationFingerprint="provider-a"},"context_length_exceeded",1,2,72);
        Test.True(owner.CurrentStorage.CompressionRetryAttempts==0,"stale provider failure cannot consume new provider recovery attempts");
    }
}
