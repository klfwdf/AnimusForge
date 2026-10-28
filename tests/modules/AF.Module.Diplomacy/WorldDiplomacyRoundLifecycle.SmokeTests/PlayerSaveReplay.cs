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
            TechnicalHoldStartedHour = 321,
            EventWait = new() { Kind = "offer", SourceId = "incoming", SourceActionId = "specific-offer", UntilDay = 15 },
            PlayerNegotiationWaits = new() { new() { SourceDocumentId = "incoming",NotifiedHour = 317,AnswerDocumentId = "reply_public" } },
            PlayerCourtWork = new() { new() { SourceDocumentId = "reply_public",KingdomId = "b",Status = "pending" } } });
        state.Jobs.Add(new() { JobId = "held_audit",RoundId = "save_handoff",Kind = "privacy_audit",OwnershipEpoch = 7,
            SchedulingOwner = "player",PrivacySourceMaterial = string.Concat(Enumerable.Repeat("秘密🙂é",15000)),
            EventMaterialSnapshot = JsonConvert.SerializeObject(new[] { new { key = "lore:saved", known_by = "b", text = new string('界', 36000) } }),
            EventTriggerMaterial = new string('因', 16000), TechnicalRecoveryAttempts = 3,
            RecoveryConfiguration = "configuration-fingerprint", RecoveryServiceEpoch = 9,
            PrivacyDraft = "{\"title\":\"draft\",\"body\":\"unpublished\"}",SuspendedReason = "privacy_rewrites_exhausted",
            PrivacyAuditRetries = 2, PrivacyRepairFeedback = "{\"validation_error\":\"agenda_quote_not_in_public_draft_body\"}" });
        state.Documents.Add(new() { DocumentId = "incoming",RoundId = "save_handoff",ActualPlayerNoticeHour = 317,
            PlayerLifecycleClassified = true, PlayerReplyRoutingPending = true,
            Actions = new() { new() { ActionId = "action_1",DependencyActionIds = new() { "action_2" },ExecutionState = "deferred_conditions" } } });
        state.EventAnnouncements.Add(new() { Id = "archived:started", EventId = "archived", State = "public",
            ProgressEvidenceIds = new() { "published-apology" },
            MaterialKeys = new() { "lore:old_case" }, ConceptFingerprint = "stable-story-identity" });
        var admission=DiplomacyCountryAdmission.Calculate(new(){Direct=new(){"a","b"},Pairs=new(){new(){Country="c",Origin="a",Border=true,War=true,OriginRuler="ruler_a",Relations=new(){["ruler_a"]=-65}}}});
        admission.Seed=JsonConvert.SerializeObject(new {source=new string('证',14000),core="ruler_a"});
        state.Jobs[0].CountryAdmissionSeed=admission.Seed;state.Jobs[0].CountryAdmission=JsonConvert.SerializeObject(admission);
        state.Jobs[0].AdmissionSupplementedCountries=new(){"a"};state.Jobs[0].AdmissionRequiredSourceKeys=new(){"persona:a"};
        state.ConcurrentRounds[0].CountryAdmissionVersion=1;state.ConcurrentRounds[0].CountryAdmission=state.Jobs[0].CountryAdmission;
        string json = JsonConvert.SerializeObject(state);var store = new Store();CampaignSaveChunkHelper.SaveChunkedStringStrict(store,key,json);
        Test.True(store.Strings.All(x => Encoding.UTF8.GetByteCount(x) <= 12000),"actual strict save helper bounds new owner/wait/privacy/condition state at 12000 UTF8 bytes per string");
        store.Saving = false;var loaded = CampaignSaveChunkHelper.LoadChunkedStringStrict(store,key);
        Test.True(loaded.Status == CampaignSaveChunkHelper.StrictReadStatus.Complete && loaded.Text == json,"large protected material and full handoff state survive strict chunk roundtrip without truncation");
        var copy = JsonConvert.DeserializeObject<WorldDiplomacyStorage>(loaded.Text)!;
        Test.True(copy.Jobs[0].CountryAdmission==state.Jobs[0].CountryAdmission&&copy.Jobs[0].CountryAdmissionSeed==admission.Seed
            &&copy.ConcurrentRounds[0].CountryAdmission==state.ConcurrentRounds[0].CountryAdmission&&copy.ConcurrentRounds[0].CountryAdmissionVersion==1
            &&copy.Jobs[0].AdmissionSupplementedCountries.SequenceEqual(new[]{"a"})&&copy.Jobs[0].AdmissionRequiredSourceKeys.SequenceEqual(new[]{"persona:a"}),
            "frozen signed score, large seed, adopted cast and partial supplementation survive the existing strict save container");
        Test.True(copy.PlayerLifecycleSchemaVersion==1 && copy.PlayerLifecycleRecoveryRoundIds.Count==1
            && copy.Documents[0].PlayerLifecycleClassified && copy.Documents[0].PlayerReplyRoutingPending,
            "lifecycle migration queue and unfinished reply attribution survive the same strict chunk container");
        Test.True(copy.ConcurrentRounds[0].OwnershipEpoch == 7 && copy.Documents[0].ActualPlayerNoticeHour == 317
            && copy.Jobs[0].SuspendedReason == "privacy_rewrites_exhausted" && copy.Documents[0].Actions[0].DependencyActionIds.Count == 1,
            "real JSON fields recover ownership, notification, withheld draft and dependency receipt together");
        Test.True(copy.Jobs[0].EventMaterialSnapshot == state.Jobs[0].EventMaterialSnapshot,
            "frozen attributed event evidence survives actual strict UTF8 chunking without losing its source or text");
        Test.True(copy.Jobs[0].PrivacyAuditRetries == 2 && copy.Jobs[0].PrivacyRepairFeedback == state.Jobs[0].PrivacyRepairFeedback,
            "audit retry bound and precise diagnostic survive strict chunked persistence without new save keys");
        Test.True(copy.Jobs[0].EventTriggerMaterial == state.Jobs[0].EventTriggerMaterial && copy.Jobs[0].TechnicalRecoveryAttempts == 3
            && copy.Jobs[0].RecoveryServiceEpoch == 9 && copy.ConcurrentRounds[0].TechnicalHoldStartedHour == 321
            && copy.EventAnnouncements[0].ProgressEvidenceIds.Single() == "published-apology",
            "new retry budget, paused clock, trigger originals and progress evidence survive the existing strict save chunks");
        Test.True(copy.ConcurrentRounds[0].EventWait.SourceActionId == "specific-offer", "specific wait action identity survives the original save container");
        Test.True(copy.EventAnnouncements.Single().MaterialKeys.Single() == "lore:old_case"
            && copy.EventAnnouncements.Single().ConceptFingerprint == "stable-story-identity",
            "long-term story identity survives the same strict chunk save container without a new save key");
        store.Remove(key + "__af_chunk_1");loaded = CampaignSaveChunkHelper.LoadChunkedStringStrict(store,key);
        Test.True(loaded.Status != CampaignSaveChunkHelper.StrictReadStatus.Complete && string.IsNullOrEmpty(loaded.Text),"missing chunk cannot become a successful empty or half-published event");
    }
}
