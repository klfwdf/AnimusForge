using TaleWorlds.CampaignSystem;
using AnimusForge.Refactor.Contracts;

namespace AnimusForge;

internal sealed class WorldDiplomacyModuleAdapter : IWorldDiplomacyModulePort
{
    public void ApplyExternalPrestigeDelta(string kingdomId, int delta, string reason)
    {
        var owner = Campaign.Current?.GetCampaignBehavior<WorldDiplomacyBehavior>();
        owner?.Orchestration.ApplyNationalPrestigeDelta(kingdomId, delta, null, reason ?? "");
    }
    public bool OpenComposeFromTerminal(System.Action onClose = null) => WorldDiplomacyPresentation.OpenComposeFromTerminal(onClose);
    public bool ShowRoyalAnnouncementArchive(System.Action onClose = null) => WorldDiplomacyPresentation.ShowRoyalAnnouncementArchive(onClose);
    public bool ComposePopupOpen => WorldDiplomacyComposePopup.IsOpen;
    public string Standing(string kingdomId)
    {
        try { return Presentation?.Standing(kingdomId) ?? ""; }
        catch { return ""; }
    }
    private sealed class MemorySource : IWorldDiplomacyMemorySource
    {
        public bool TryCapture(string heroId, string kingdomOverride, out WorldDiplomacyMemorySnapshot snapshot)
            => WorldDiplomacyBehavior.TryCaptureMemory(heroId, kingdomOverride, out snapshot);
        public string FormatDate(int day) => WorldDiplomacyBehavior.FormatDateForProactive(day);
    }
    private static readonly IWorldDiplomacyMemorySource Memory = new MemorySource();
    public string BuildMemory(string heroId, string kingdomOverride, string input, System.Collections.Generic.IReadOnlyList<string> ruleIds, bool proactive)
        => WorldDiplomacyMemoryApplication.Build(Memory, heroId, kingdomOverride, input, ruleIds, proactive);
    private sealed class RevisionSource : IWorldDiplomacyTimelineRevisionSource
    {
        public bool TryRead(out long revision) => WorldDiplomacyBehavior.TryGetTimelineRevisionSnapshot(out revision);
    }

    private sealed class TimelineStateSource : IWorldDiplomacyTimelineStateSource
    {
        public bool TryGetState(out WorldDiplomacyStorage storage) => WorldDiplomacyBehavior.TryGetTimelineState(out storage);
    }

    private sealed class DiscussionSource : IWorldDiplomacyDiscussionSource
    {
        public bool TryCaptureRepresentative(string heroId, out WorldDiplomacyDiscussionCandidate candidate, out string kingdomId) =>
            WorldDiplomacyBehavior.TryCaptureDiscussionCandidate(ResolveHero(heroId), out candidate, out kingdomId);
        public bool HasKnownDocument(string heroId, string kingdomId) =>
            WorldDiplomacyBehavior.HasKnownDocumentForDiscussion(ResolveHero(heroId), kingdomId);
    }

    private sealed class ProactiveSource : IWorldDiplomacyProactiveDiscussionSource
    {
        public bool TryCaptureSpeaker(string heroId, out WorldDiplomacyProactiveSpeakerCandidate candidate, out string playerKingdomId) =>
            WorldDiplomacyBehavior.TryCaptureProactiveSpeaker(ResolveHero(heroId), out candidate, out playerKingdomId);
        public bool TryCaptureDocuments(string heroId, string playerKingdomId,
            out System.Collections.Generic.IReadOnlyList<WorldDiplomacyDocument> documents,
            out System.Collections.Generic.HashSet<string> knownIds, out int currentDay) =>
            WorldDiplomacyBehavior.TryCaptureProactiveDocuments(ResolveHero(heroId), playerKingdomId,
                out documents, out knownIds, out currentDay);
        public string GetPlayerKingdomName(string playerKingdomId) => WorldDiplomacyBehavior.GetPlayerKingdomNameForProactive();
        public string FormatDate(int day) => WorldDiplomacyBehavior.FormatDateForProactive(day);
    }

    private static readonly IWorldDiplomacyTimelineRevisionSource TimelineRevisionSource = new RevisionSource();
    private static readonly IWorldDiplomacyTimelineStateSource TimelineSource = new TimelineStateSource();
    private static readonly IWorldDiplomacyDiscussionSource Discussion = new DiscussionSource();
    private static readonly IWorldDiplomacyProactiveDiscussionSource Proactive = new ProactiveSource();
    private static Hero ResolveHero(string id) => DiplomacyIdentityResolver.Hero(id);
    public bool CanDiscuss(string heroId) => WorldDiplomacyDiscussionApplication.CanDiscuss(Discussion, heroId);
    public bool TryBuildProactiveDiscussion(string heroId, out string key, out string fact, out float urgency) =>
        WorldDiplomacyProactiveDiscussionApplication.TryBuild(Proactive, heroId, out key, out fact, out urgency);
    public WorldDiplomacyTimelineRevisionResult QueryTimelineRevision() => WorldDiplomacyTimelineRevisionApplication.Query(TimelineRevisionSource);
    public WorldDiplomacyTimelineDocumentsResult QueryTimelineDocuments(int maxCount) => WorldDiplomacyTimelineApplication.QueryDocuments(TimelineSource, maxCount);
    public bool TryMarkDocumentRead(string documentId, out bool ownerAvailable) => WorldDiplomacyTimelineApplication.MarkRead(TimelineSource, documentId, out ownerAvailable);
    public WorldDiplomacyDocumentReadResult MarkTimelineRead(string documentId) => WorldDiplomacyTimelineApplication.MarkTimelineRead(TimelineSource, documentId);
    public IWorldDiplomacyPresentationPort Presentation => WorldDiplomacyBehavior.ResolvePresentationPort();
    public void OnLifecycle(WorldDiplomacyLifecycleEvent lifecycle)
    {
        var source = new WorldDiplomacyBehavior.LifecycleSource(WorldDiplomacyBehavior.Instance);
        WorldDiplomacyLifecycleApplication.Run(lifecycle, WorldDiplomacyBehavior.Instance?.Orchestration, ref source);
    }
    public void OnEngineTick()
    {
        var source = new WorldDiplomacyBehavior.TickSource(WorldDiplomacyBehavior.Instance);
        WorldDiplomacyTickApplication.Run(ref source, WorldDiplomacyBehavior.Instance?.Orchestration);
    }
    public void OnCampaignTick()
    {
        var source = new WorldDiplomacyBehavior.CampaignSource(WorldDiplomacyBehavior.Instance);
        WorldDiplomacyCampaignApplication.CampaignTick(ref source, WorldDiplomacyBehavior.Instance?.Orchestration);
    }
    public void OnDailyTick()
    {
        var source = new WorldDiplomacyBehavior.CampaignSource(WorldDiplomacyBehavior.Instance);
        WorldDiplomacyCampaignApplication.DailyTick(ref source, WorldDiplomacyBehavior.Instance?.Orchestration);
    }

}
