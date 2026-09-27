using TaleWorlds.CampaignSystem;
using AnimusForge.Refactor.Contracts;

namespace AnimusForge;

internal sealed class WorldDiplomacyModuleAdapter : IWorldDiplomacyModulePort
{
    private sealed class RevisionSource : IWorldDiplomacyTimelineRevisionSource
    {
        public bool TryRead(out long revision) => WorldDiplomacyBehavior.TryGetTimelineRevisionSnapshot(out revision);
    }

    private static readonly IWorldDiplomacyTimelineRevisionSource TimelineRevisionSource = new RevisionSource();
    private static Hero ResolveHero(string id) => DiplomacyIdentityResolver.Hero(id);
    public bool CanDiscuss(string heroId) => WorldDiplomacyBehavior.CanDiscussWorldDiplomacyForExternal(ResolveHero(heroId));
    public bool TryBuildProactiveDiscussion(string heroId, out string key, out string fact, out float urgency) =>
        WorldDiplomacyBehavior.TryBuildProactiveDiscussionForExternal(ResolveHero(heroId), out key, out fact, out urgency);
    public WorldDiplomacyTimelineRevisionResult QueryTimelineRevision() => WorldDiplomacyTimelineRevisionApplication.Query(TimelineRevisionSource);
    public WorldDiplomacyTimelineDocumentsResult QueryTimelineDocuments(int maxCount) => WorldDiplomacyBehavior.QueryTimelineDocuments(maxCount);
    public bool TryMarkDocumentRead(string documentId, out bool ownerAvailable) => WorldDiplomacyBehavior.TryMarkDocumentReadForCommand(documentId, out ownerAvailable);
    public IWorldDiplomacyPresentationPort Presentation => WorldDiplomacyBehavior.ResolvePresentationPort();
    public void OnEngineTick() => WorldDiplomacyBehavior.Instance?.OnEngineTick();
}
