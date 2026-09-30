using AnimusForge.Refactor.Contracts;
using TaleWorlds.CampaignSystem;

namespace HarmonyLib { internal sealed class Harmony { } }

// Recording boundaries only. Diplomatic rules/actions remain covered by the existing replay suites.
namespace TaleWorlds.CampaignSystem
{
    internal sealed class Hero { public string StringId { get; set; } }
    internal sealed class CharacterObject { public Hero HeroObject { get; set; } }
    internal sealed class Kingdom { public string StringId { get; set; } }
    internal sealed class Campaign
    {
        public static Campaign Current { get; set; }
        public ObjectManager CampaignObjectManager { get; set; } = new ObjectManager();
    }
    internal sealed class CampaignGameStarter { }
    internal sealed class ObjectManager
    {
        public Dictionary<string, object> Objects = new();
        public int Lookups;
        public bool Throw;
        public T Find<T>(string id) where T : class
        {
            Lookups++;
            if (Throw) throw new InvalidOperationException("lookup unavailable");
            return Objects.TryGetValue(id, out var value) ? value as T : null;
        }
    }
}
namespace AnimusForge
{
    internal sealed class WorldDiplomacyDocument { public string DocumentId; public bool IsRead; }
    internal sealed class WorldDiplomacyStorage { public List<WorldDiplomacyDocument> Documents = new(); }
    internal static class WorldDiplomacyPresentationQueries
    {
        public static WorldDiplomacyTimelineDocumentsResult Timeline(WorldDiplomacyStorage storage, int maxCount)
        { Recording.Call("documents",maxCount); return WorldDiplomacyTimelineDocumentsResult.Available(Array.Empty<WorldDiplomacyTimelineDocument>()); }
        public static bool MarkRead(WorldDiplomacyDocument document)
        { if (document == null) return false; document.IsRead = true; return true; }
    }
    internal readonly struct WorldDiplomacyProactiveSpeakerCandidate { }
    internal interface IWorldDiplomacyProactiveDiscussionSource
    {
        bool TryCaptureSpeaker(string heroId, out WorldDiplomacyProactiveSpeakerCandidate candidate, out string playerKingdomId);
        bool TryCaptureDocuments(string heroId, string playerKingdomId,
            out IReadOnlyList<WorldDiplomacyDocument> documents, out HashSet<string> knownIds, out int currentDay);
        string GetPlayerKingdomName(string playerKingdomId);
        string FormatDate(int day);
    }
    internal static class WorldDiplomacyProactiveDiscussionApplication
    {
        internal static bool TryBuild(IWorldDiplomacyProactiveDiscussionSource source, string heroId,
            out string key, out string fact, out float urgency)
        {
            if (!source.TryCaptureSpeaker(heroId, out _, out _))
            { key=""; fact=""; urgency=0f; return false; }
            key="key"; fact="fact"; urgency=0.75f; return Recording.Result;
        }
    }
    internal static class Recording
    {
        public static string Method;
        public static object[] Args;
        public static bool Result;
        public static int PeaceCaptures;
        public static Exception Failure;
        public static string LastTagLog;
        public static List<string> TagActions = new();
        public static void Call(string name, params object[] args)
        {
            Method = name; Args = args;
            if (Failure != null) throw Failure;
        }
    }
    internal static class DiplomacyBehavior
    {
        public static DiplomacyConversationEligibilitySnapshot CaptureEligibilitySnapshot(Hero h)
        {
            Recording.Call("eligibility",h);
            bool enabled=Recording.Result;
            return new DiplomacyConversationEligibilitySnapshot(enabled,false,false,enabled,false,enabled,
                enabled,false,enabled,enabled);
        }
        public static string BuildDiplomacyPostprocessContext(Hero h) { Recording.Call("context",h); return h?.StringId ?? ""; }
        public static void ProcessDiplomacyTagsDispatch(Hero h, ref string text) { Recording.Call("execute",h,text); if (h != null) text = "confirmed:"+text; }
    }
    internal readonly struct DiplomacyOralTagSource : IDiplomacyOralTagSource
    {
        private readonly Hero hero;
        internal DiplomacyOralTagSource(Hero h) => hero=h;
        public bool HasSpeaker => hero!=null;
        public bool IsAvailable => true;
        public string SpeakerHeroId => hero.StringId;
        public string DeclareWar(string payload) => Execute("war",payload);
        public string MakePeace(string payload) => Execute("peace",payload);
        public string IndependentClanPeace(string payload) => Execute("independent",payload);
        public string FormAlliance(string payload) => Execute("alliance",payload);
        public string BreakAlliance(string payload) => Execute("break-alliance",payload);
        public string MakeTrade(string payload) => Execute("trade",payload);
        public string CancelTrade(string payload) => Execute("cancel-trade",payload);
        private string Execute(string action,string payload)
        { Recording.Call("execute",hero,action,payload); Recording.TagActions.Add(action+":"+payload); return "confirmed"; }
        public void Log(string message) { Recording.LastTagLog=message; }
    }
    internal readonly struct DiplomacyTributePowerSource : IDiplomacyTributePowerSource
    {
        private readonly Kingdom payer;
        private readonly Kingdom receiver;
        public DiplomacyTributePowerSource(Kingdom p, Kingdom r) { payer=p;receiver=r; }
        public bool TryCapture(out DiplomacyTributePowerSnapshot snapshot)
        {
            Recording.Call("tribute",payer,receiver);
            snapshot=new DiplomacyTributePowerSnapshot(1,2,3,4,5,6,1000);
            return Recording.Result;
        }
    }
    internal struct DiplomacyPostprocessContextSource : IDiplomacyPostprocessContextSource
    {
        private readonly Hero hero;
        public DiplomacyPostprocessContextSource(Hero h) { hero=h; }
        public bool HasSpeaker => hero!=null;
        public bool TryCaptureIndependentPeace(out DiplomacyIndependentPeaceContextSnapshot snapshot)
        { snapshot=default; return false; }
        public DiplomacyConversationEligibilitySnapshot CaptureEligibility() =>
            new(true,false,false,true,false,true,true,false,true,true);
        public DiplomacyPostprocessKingdomSnapshot CaptureKingdoms()
        {
            Recording.Call("context",hero);
            return new DiplomacyPostprocessKingdomSnapshot(true,hero.StringId,hero.StringId,
                true,false,"player","Player",true,true,
                new[]{new DiplomacyKingdomSummary(hero.StringId,hero.StringId,false),
                      new DiplomacyKingdomSummary("old","Old",true)});
        }
        public string GetAnnexationHint() => "";
        public bool ArePlayerAndNpcAtWar() => false;
        public int CalculateDailyTribute(bool npcPays) => 0;
        public void LogFailure(string message) { Recording.Call("context-error",message); }
    }
    internal struct DiplomacyIndependentPeaceSource : IDiplomacyIndependentPeaceSource
    {
        private readonly Hero hero;
        public DiplomacyIndependentPeaceSource(Hero h) { hero=h; }
        public DiplomacyIndependentPeacePlayerSnapshot CapturePlayer()
        {
            Recording.PeaceCaptures++;
            Recording.Call("peace",hero);
            bool ok=Recording.Result;
            return new DiplomacyIndependentPeacePlayerSnapshot(ok,ok,ok,false,false,false,true);
        }
        public DiplomacyIndependentPeaceSpeakerSnapshot CaptureSpeaker()
        {
            Recording.PeaceCaptures++;
            Recording.Call("peace",hero);
            bool ok=Recording.Result;
            return new DiplomacyIndependentPeaceSpeakerSnapshot(ok,false,false,ok,false,false,false,false);
        }
        public DiplomacyIndependentPeaceTargetSnapshot CaptureTarget()
        {
            Recording.PeaceCaptures++;
            Recording.Call("peace",hero);
            bool ok=Recording.Result;
            return new DiplomacyIndependentPeaceTargetSnapshot(ok,ok);
        }
        public DiplomacyIndependentPeaceWarSnapshot CaptureWar()
        {
            Recording.PeaceCaptures++;
            Recording.Call("peace",hero);
            bool ok=Recording.Result;
            return new DiplomacyIndependentPeaceWarSnapshot(ok,false,false,ok,false);
        }
    }
    internal static class DiplomacyModuleComposition
    {
        internal static void Register(CampaignGameStarter starter) { }
        internal static void RegisterPatches(HarmonyLib.Harmony harmony) { }
    }
    // Generated-project orchestration surface: the union of members invoked by the
    // source-linked Tick and Campaign applications under test. The production
    // interface is wider; this stub is intentionally minimal and orchestration-agnostic.
    internal interface IWorldDiplomacyOrchestration
    {
        void ResetStorageForNewGame(bool initialPeacePending);
        void EnsureScheduleInitialized();
        void RecoverUnsettledAiInternationalReputation();
        void RecoverPlayerCourtReceiptsFromKnowledge();
        void ReconcileActiveDiplomacyAfterLoad();

        void HandleDisabledState();
        void ProcessCompletedJobs();
        void TryScheduleTokenCompression();
        void TryStartNextLlmJob();
        void PollNotifications();
        void TryApplyInitialNewGamePeace();
        void NormalizeStorage(bool allowWorldValidation);
        void ReconcileAllNationalPrestigeVassalRelations();
        void RetryDeferredCanonicalHistoryEntries();
        void RetryDiplomaticThreatDomesticPenalties();
        void RetryDiplomaticThreatComplianceConsequences();
        void RetryDiplomaticThreatHistoryResults();
        void RefreshRoundIntervalScheduleIfNeeded();
        void RecalculatePendingPropagationIfNeeded();
        void AnchorInternationalReputationNaturalChangeDays();
        void ProcessInternationalReputationNaturalChange();
        void RefreshPolicyDiplomacySignals();
        void RetryDeferredDocumentPropagation();
        void ProcessPropagationArrivals();
        void ProcessRelayArrivals();
        void RetryDeferredRoundProgress();
        void ProcessRoundLifecycle();
        void TrySchedulePolicyTriggeredRound();
        void TryScheduleNormalRound();
        void EnsureActiveWarLedgers();
        void TrimRecentBattleFacts();
        void DecayWarPressure();
    }
    internal sealed class NoopOrchestration : IWorldDiplomacyOrchestration
    {
        public void ResetStorageForNewGame(bool initialPeacePending) { }
        public void EnsureScheduleInitialized() { }
        public void RecoverUnsettledAiInternationalReputation() { }
        public void RecoverPlayerCourtReceiptsFromKnowledge() { }
        public void ReconcileActiveDiplomacyAfterLoad() { }

        public void HandleDisabledState() { }
        public void ProcessCompletedJobs() { }
        public void TryScheduleTokenCompression() { }
        public void TryStartNextLlmJob() { }
        public void PollNotifications() { }
        public void TryApplyInitialNewGamePeace() { }
        public void NormalizeStorage(bool allowWorldValidation) { }
        public void ReconcileAllNationalPrestigeVassalRelations() { }
        public void RetryDeferredCanonicalHistoryEntries() { }
        public void RetryDiplomaticThreatDomesticPenalties() { }
        public void RetryDiplomaticThreatComplianceConsequences() { }
        public void RetryDiplomaticThreatHistoryResults() { }
        public void RefreshRoundIntervalScheduleIfNeeded() { }
        public void RecalculatePendingPropagationIfNeeded() { }
        public void AnchorInternationalReputationNaturalChangeDays() { }
        public void ProcessInternationalReputationNaturalChange() { }
        public void RefreshPolicyDiplomacySignals() { }
        public void RetryDeferredDocumentPropagation() { }
        public void ProcessPropagationArrivals() { }
        public void ProcessRelayArrivals() { }
        public void RetryDeferredRoundProgress() { }
        public void ProcessRoundLifecycle() { }
        public void TrySchedulePolicyTriggeredRound() { }
        public void TryScheduleNormalRound() { }
        public void EnsureActiveWarLedgers() { }
        public void TrimRecentBattleFacts() { }
        public void DecayWarPressure() { }
    }
    internal sealed class WorldDiplomacyBehavior
    {
        internal static bool TryCaptureMemory(string heroId, string kingdom, out WorldDiplomacyMemorySnapshot snapshot)
        { snapshot = default; Recording.Call("memory", heroId, kingdom); return true; }

        public static WorldDiplomacyBehavior Instance;
        public int Ticks;
        public long Revision;
        public static bool Available;
        public static bool Applied;
        public static WorldDiplomacyStorage State = new();
        public static IWorldDiplomacyPresentationPort Port;
        public IWorldDiplomacyOrchestration Orchestration { get; internal set; } = new NoopOrchestration();
        internal static bool TryCaptureDiscussionCandidate(Hero h, out WorldDiplomacyDiscussionCandidate candidate, out string kingdomId)
        { Recording.Call("discuss",h); candidate = new WorldDiplomacyDiscussionCandidate(true,true,false,true,false); kingdomId="kingdom"; return true; }
        internal static bool HasKnownDocumentForDiscussion(Hero h, string kingdomId)
        { Recording.Call("known",h,kingdomId); return Recording.Result; }
        internal static bool TryCaptureProactiveSpeaker(Hero h,
            out WorldDiplomacyProactiveSpeakerCandidate candidate, out string playerKingdomId)
        { Recording.Call("proactive",h); candidate=default; playerKingdomId="player"; return true; }
        internal static bool TryCaptureProactiveDocuments(Hero h, string playerKingdomId,
            out IReadOnlyList<WorldDiplomacyDocument> documents, out HashSet<string> knownIds, out int currentDay)
        { documents=Array.Empty<WorldDiplomacyDocument>();knownIds=new();currentDay=0;return true; }
        internal static string GetPlayerKingdomNameForProactive() => "player";
        internal static string FormatDateForProactive(int day) => day.ToString();
        internal static bool TryGetTimelineRevisionSnapshot(out long revision)
        { revision = Instance?.Revision ?? 0L; return Instance != null; }
        internal static bool TryGetTimelineState(out WorldDiplomacyStorage storage)
        { storage = State; return Available; }
        public static IWorldDiplomacyPresentationPort ResolvePresentationPort() => Port;
        public void OnEngineTick() { Ticks++; }
        internal struct TickSource : IWorldDiplomacyTickSource
        {
            private readonly WorldDiplomacyBehavior owner;
            internal TickSource(WorldDiplomacyBehavior value) => owner=value;
            public bool HasOwner => owner!=null;
            public bool IsEnabled => true;
            public bool DisabledStateApplied => false;
            public void ProcessComposePopup() { owner.Ticks++; }
            public void ClearDisabledState() { }
        }
        internal readonly struct LifecycleSource : IWorldDiplomacyLifecycleSource
        {
            internal LifecycleSource(WorldDiplomacyBehavior owner) { }
            public bool StartAtPeace => true;
            public void ResetTransientRuntime(string reason) => Recording.Call("lifecycle-reset", reason);
        }
        internal struct CampaignSource : IWorldDiplomacyCampaignSource
        {
            private readonly WorldDiplomacyBehavior owner;
            internal CampaignSource(WorldDiplomacyBehavior value) => owner=value;
            public bool IsEnabled => owner != null;
            public int CurrentDay => 0;
            public bool DisabledStateApplied { get; set; }
            public bool NativeQueueSanitized { get; set; }
            public int LastSchedulerDay { get; set; }
            public void RemoveQueuedNativeDiplomacyDecisions() { }
            public void ClearDailyCaches() { }
            public void ResetDailyGenerationBudget() { }
        }
    }
    internal static class WorldDiplomacyPolicyContext
    {
        public static List<WorldDiplomacyPolicySignalSnapshot> Signals = new();
        public static IReadOnlyList<PublishedPolicyArtifactLedgerEntry> Artifacts = Array.Empty<PublishedPolicyArtifactLedgerEntry>();
        public static string BuildSnapshot(string id) { Recording.Call("snapshot",id);return "snapshot:"+id; }
        public static List<WorldDiplomacyPolicySignalSnapshot> GetForeignPolicySignals() { Recording.Call("signals");return new List<WorldDiplomacyPolicySignalSnapshot>(Signals); }
        public static bool IsForeignPolicySignalActive(string p,string o,string a) { Recording.Call("active",p,o,a);return Recording.Result; }
        public static string GetPublishedPolicyHistoryLedgerId() { Recording.Call("ledger");return "ledger"; }
        public static long GetPublishedPolicyHistoryCurrentSequence() { Recording.Call("sequence");return 17; }
        public static long GetPublishedPolicyHistoryCurrentRevision() { Recording.Call("revision");return 23; }
        public static IReadOnlyList<PublishedPolicyArtifactLedgerEntry> GetPublishedPolicyHistoryArtifacts(long after, int max) { Recording.Call("artifacts",after,max);return Artifacts; }
        public static bool TryAcknowledgePublishedPolicyHistoryThrough(long through) { Recording.Call("ack",through);return Recording.Result; }
        public static void Clear() { Recording.Call("clear"); }
    }
    internal sealed class Presentation : IWorldDiplomacyPresentationPort
    {
        public IReadOnlyList<WorldDiplomacyArchiveRecord> Archive() => Array.Empty<WorldDiplomacyArchiveRecord>();
        public string ArchiveSubtitle() => "subtitle";
        public string Standing(string id) => id;
        public WorldDiplomacyPlayerContext Player => null;
        public WorldDiplomacyDocumentDetail Detail(string id) => null;
        public string Submit(WorldDiplomacyPlayerDocumentCommand c) => c.Body;
        public bool MarkRead(string id) => true;
        public bool CanOpenReply(string id,string round,long generation) => false;
    }
}
namespace AnimusForge.Refactor.Domain
{
    internal static class WorldDiplomacyRoundLifecycleRules
    {
        internal static AnimusForge.WorldDiplomacyDocument ResolveDocument(
            List<AnimusForge.WorldDiplomacyDocument> documents, string documentId)
        {
            AnimusForge.Recording.Call("read", documentId);
            return AnimusForge.WorldDiplomacyBehavior.Applied
                ? new AnimusForge.WorldDiplomacyDocument { DocumentId = documentId }
                : null;
        }
    }
}

namespace AnimusForge
{
    // Wiring boundary only; actual memory selection is exercised by PromptMemoryReplay.
    internal readonly struct WorldDiplomacyMemorySnapshot { }
    internal interface IWorldDiplomacyMemorySource
    {
        bool TryCapture(string heroId, string kingdom, out WorldDiplomacyMemorySnapshot snapshot);
        string FormatDate(int day);
    }
    internal static class WorldDiplomacyMemoryApplication
    {
        internal static string Build(IWorldDiplomacyMemorySource source, string hero, string kingdom, string input, IReadOnlyList<string> ids, bool proactive)
        { source.TryCapture(hero, kingdom, out _); return "memory"; }
    }
    internal sealed class DiplomacyPromptSource : IDiplomacyPromptSource
    {
        private readonly Hero _hero;
        internal DiplomacyPromptSource(Hero hero) => _hero = hero;
        public DiplomacyConversationEligibilitySnapshot CaptureEligibility() => DiplomacyBehavior.CaptureEligibilitySnapshot(_hero);
        public DiplomacyPromptSnapshot Capture() => default;
        public IReadOnlyList<DiplomacyPromptWar> CaptureWars() => Array.Empty<DiplomacyPromptWar>();
        public bool TryCaptureIndependentPeace(out DiplomacyIndependentPeaceContextSnapshot snapshot) { snapshot = default; return false; }
        public string Template(string key, Dictionary<string, string> tokens) => key;
        public string AnnexationInstruction() => "";
        public void Log(string message) { }
    }
}
