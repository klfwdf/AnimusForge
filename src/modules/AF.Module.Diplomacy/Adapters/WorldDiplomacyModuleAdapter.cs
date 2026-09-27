using TaleWorlds.CampaignSystem;
using AnimusForge.Refactor.Contracts;

namespace AnimusForge;

internal sealed class WorldDiplomacyModuleAdapter : IWorldDiplomacyModulePort
{
    private sealed class RevisionSource : IWorldDiplomacyTimelineRevisionSource
    {
        public bool TryRead(out long revision) => WorldDiplomacyBehavior.TryGetTimelineRevisionSnapshot(out revision);
    }

    private sealed class TimelineStateSource : IWorldDiplomacyTimelineStateSource
    {
        public bool TryGetState(out WorldDiplomacyStorage storage) => WorldDiplomacyBehavior.TryGetTimelineState(out storage);
    }

    private static readonly IWorldDiplomacyTimelineRevisionSource TimelineRevisionSource = new RevisionSource();
    private static readonly IWorldDiplomacyTimelineStateSource TimelineSource = new TimelineStateSource();
    private static Hero ResolveHero(string id) => DiplomacyIdentityResolver.Hero(id);
    public bool CanDiscuss(string heroId) => WorldDiplomacyBehavior.CanDiscussWorldDiplomacyForExternal(ResolveHero(heroId));
    public bool TryBuildProactiveDiscussion(string heroId, out string key, out string fact, out float urgency) =>
        WorldDiplomacyBehavior.TryBuildProactiveDiscussionForExternal(ResolveHero(heroId), out key, out fact, out urgency);
    public WorldDiplomacyTimelineRevisionResult QueryTimelineRevision() => WorldDiplomacyTimelineRevisionApplication.Query(TimelineRevisionSource);
    public WorldDiplomacyTimelineDocumentsResult QueryTimelineDocuments(int maxCount) => WorldDiplomacyTimelineApplication.QueryDocuments(TimelineSource, maxCount);
    public bool TryMarkDocumentRead(string documentId, out bool ownerAvailable) => WorldDiplomacyTimelineApplication.MarkRead(TimelineSource, documentId, out ownerAvailable);
    public IWorldDiplomacyPresentationPort Presentation => WorldDiplomacyBehavior.ResolvePresentationPort();
    public void OnEngineTick() => WorldDiplomacyBehavior.Instance?.OnEngineTick();
}
