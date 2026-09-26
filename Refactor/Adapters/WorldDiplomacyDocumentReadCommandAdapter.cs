using System;
using AnimusForge.Refactor.Contracts;

namespace AnimusForge.Refactor.Adapters;

internal sealed class WorldDiplomacyDocumentReadCommandAdapter : IWorldDiplomacyDocumentReadCommand
{
    private const string TimelinePrefix = "diplomacy:";

    public WorldDiplomacyDocumentReadResult MarkRead(string documentId)
    {
        string cleanId = (documentId ?? string.Empty).Trim();
        if (cleanId.StartsWith(TimelinePrefix, StringComparison.OrdinalIgnoreCase))
        {
            cleanId = cleanId.Substring(TimelinePrefix.Length);
        }
        if (string.IsNullOrEmpty(cleanId))
        {
            return WorldDiplomacyDocumentReadResult.InvalidDocumentId();
        }

        bool applied = DiplomacyModuleServices.World.TryMarkDocumentRead(
            cleanId,
            out bool ownerAvailable);
        if (!ownerAvailable)
        {
            return WorldDiplomacyDocumentReadResult.Unavailable();
        }

        return applied
            ? WorldDiplomacyDocumentReadResult.Applied()
            : WorldDiplomacyDocumentReadResult.NotFound();
    }
}
