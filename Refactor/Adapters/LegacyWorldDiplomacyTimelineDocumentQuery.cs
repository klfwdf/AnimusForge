using System.Collections.Generic;
using System.Linq;
using AnimusForge.Refactor.Contracts;

namespace AnimusForge.Refactor.Adapters;

/// <summary>
/// Maps the existing deep-cloned recent-document query to the immutable timeline contract.
/// </summary>
internal sealed class LegacyWorldDiplomacyTimelineDocumentQuery : IWorldDiplomacyTimelineDocumentQuery
{
    public WorldDiplomacyTimelineDocumentsResult Query(int maxCount)
    {
        if (!WorldDiplomacyBehavior.TryGetRecentDocumentsForTimelineQuery(maxCount, out List<WorldDiplomacyDocument> documents))
        {
            return WorldDiplomacyTimelineDocumentsResult.Unavailable();
        }

        return WorldDiplomacyTimelineDocumentsResult.Available(
            documents
                .Where(document => document != null
                    && (document.IsPlayerAuthored || document.IsReadyForPublication))
                .Select(MapDocument));
    }

    private static WorldDiplomacyTimelineDocument MapDocument(WorldDiplomacyDocument document)
    {
        return new WorldDiplomacyTimelineDocument(
            document.DocumentId,
            document.AuthorKingdomId,
            document.AuthorKingdomName,
            document.TargetKingdomId,
            document.TargetKingdomName,
            document.Title,
            document.Body,
            document.GameDate,
            document.Day,
            document.CreatedUtcTicks,
            document.IsResponse,
            document.RequiresResponse,
            document.ChangedDiplomaticState,
            document.IsRead,
            WorldDiplomacyBehavior.BuildDiplomaticStandingImpactTextForExternal(document),
            (document.Actions ?? new List<WorldDiplomacyDocumentAction>())
                .Where(action => action != null)
                .Select(action => new WorldDiplomacyTimelineCountryReference(
                    action.TargetKingdomId,
                    action.TargetKingdomName)));
    }
}
