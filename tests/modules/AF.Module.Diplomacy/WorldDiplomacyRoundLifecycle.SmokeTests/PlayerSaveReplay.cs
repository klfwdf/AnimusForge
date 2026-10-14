using System.Text;
using AnimusForge;
using Newtonsoft.Json;

namespace TaleWorlds.CampaignSystem
{
    public interface IDataStore { bool IsSaving { get; } bool IsLoading { get; } bool SyncData<T>(string key, ref T data); }
}
namespace AnimusForge
{
    internal static class Logger { internal static bool IsModLogicEnabled => false; internal static void Log(string tag, string message) { } }
}
internal static class PlayerSaveReplay
{
    private sealed class Store : TaleWorlds.CampaignSystem.IDataStore
    {
        private readonly Dictionary<string, object> _records = new();
        internal bool Saving = true;
        public bool IsSaving => Saving; public bool IsLoading => !Saving;
        public bool SyncData<T>(string key, ref T data)
        {
            if (Saving) { _records[key] = data!; return true; }
            if (_records.TryGetValue(key, out object value)) data = (T)value;
            return true;
        }
        internal void Remove(string key) => _records.Remove(key);
        internal IEnumerable<string> Strings => _records.Values.OfType<string>();
    }
    internal static void Run()
    {
        const string key = "_af_world_diplomacy_v1";
        var state = new WorldDiplomacyStorage { PlayerDiplomacySchemaVersion = 1, PlayerLifecycleSchemaVersion = 1,
            PlayerLifecycleRecoveryRoundIds = new() { "legacy_held_after_earliest_case" } };
        state.ConcurrentRounds.Add(new() { RoundId = "save_handoff",SchedulingOwner = "player",OwnershipEpoch = 7,
            PlayerNegotiationWaits = new() { new() { SourceDocumentId = "incoming",NotifiedHour = 317,AnswerDocumentId = "reply_public" } },
            PlayerCourtWork = new() { new() { SourceDocumentId = "reply_public",KingdomId = "b",Status = "pending" } } });
        state.Jobs.Add(new() { JobId = "held_audit",RoundId = "save_handoff",Kind = "privacy_audit",OwnershipEpoch = 7,
            SchedulingOwner = "player",PrivacySourceMaterial = string.Concat(Enumerable.Repeat("秘密🙂é",15000)),
            PrivacyDraft = "{\"title\":\"draft\",\"body\":\"unpublished\"}",SuspendedReason = "privacy_rewrites_exhausted" });
        state.Documents.Add(new() { DocumentId = "incoming",RoundId = "save_handoff",ActualPlayerNoticeHour = 317,
            PlayerLifecycleClassified = true, PlayerReplyRoutingPending = true,
            Actions = new() { new() { ActionId = "action_1",DependencyActionIds = new() { "action_2" },ExecutionState = "deferred_conditions" } } });
        string json = JsonConvert.SerializeObject(state);var store = new Store();CampaignSaveChunkHelper.SaveChunkedStringStrict(store,key,json);
        Test.True(store.Strings.All(x => Encoding.UTF8.GetByteCount(x) <= 12000),"actual strict save helper bounds new owner/wait/privacy/condition state at 12000 UTF8 bytes per string");
        store.Saving = false;var loaded = CampaignSaveChunkHelper.LoadChunkedStringStrict(store,key);
        Test.True(loaded.Status == CampaignSaveChunkHelper.StrictReadStatus.Complete && loaded.Text == json,"large protected material and full handoff state survive strict chunk roundtrip without truncation");
        var copy = JsonConvert.DeserializeObject<WorldDiplomacyStorage>(loaded.Text)!;
        Test.True(copy.PlayerLifecycleSchemaVersion==1 && copy.PlayerLifecycleRecoveryRoundIds.Count==1
            && copy.Documents[0].PlayerLifecycleClassified && copy.Documents[0].PlayerReplyRoutingPending,
            "lifecycle migration queue and unfinished reply attribution survive the same strict chunk container");
        Test.True(copy.ConcurrentRounds[0].OwnershipEpoch == 7 && copy.Documents[0].ActualPlayerNoticeHour == 317
            && copy.Jobs[0].SuspendedReason == "privacy_rewrites_exhausted" && copy.Documents[0].Actions[0].DependencyActionIds.Count == 1,
            "real JSON fields recover ownership, notification, withheld draft and dependency receipt together");
        store.Remove(key + "__af_chunk_1");loaded = CampaignSaveChunkHelper.LoadChunkedStringStrict(store,key);
        Test.True(loaded.Status != CampaignSaveChunkHelper.StrictReadStatus.Complete && string.IsNullOrEmpty(loaded.Text),"missing chunk cannot become a successful empty or half-published event");
    }
}
