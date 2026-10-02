using System.Collections.Generic;
using AnimusForge.Refactor.Adapters;
using AnimusForge.Refactor.Contracts;

namespace AnimusForge.Refactor.Modules;

/// <summary>
/// Process-lifetime composition root for the scalar diplomacy timeline query.
/// The facade and adapter are allocated once; ordinary UI polling only returns a struct and a long.
/// </summary>
internal static class WorldDiplomacyTimelineQueryHost
{
    private static readonly WorldDiplomacyTimelineRevisionFacade RevisionFacade =
        new WorldDiplomacyTimelineRevisionFacade(new WorldDiplomacyTimelineRevisionQueryAdapter());
    private static readonly WorldDiplomacyTimelineDocumentQueryFacade DocumentFacade =
        new WorldDiplomacyTimelineDocumentQueryFacade(new WorldDiplomacyTimelineDocumentQueryAdapter());
    private static readonly WorldDiplomacyDocumentReadCommandFacade DocumentReadFacade =
        new WorldDiplomacyDocumentReadCommandFacade(new WorldDiplomacyDocumentReadCommandAdapter());

    internal static long GetRevisionOrZero()
    {
        return RevisionFacade.GetRevisionOrZero();
    }

    internal static IReadOnlyList<WorldDiplomacyTimelineDocument> GetRecentDocumentsOrEmpty(int maxCount)
    {
        return DocumentFacade.Query(maxCount).Documents;
    }

    internal static bool MarkDocumentRead(string documentId)
    {
        return DocumentReadFacade.MarkReadOrFalse(documentId);
    }
}
