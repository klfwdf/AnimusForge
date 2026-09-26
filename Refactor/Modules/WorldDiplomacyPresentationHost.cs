using AnimusForge.Refactor.Contracts;
using System;
using System.Collections.Generic;

namespace AnimusForge;

// Composition boundary for detail/archive/player UI; timeline keeps its existing facade.
// Resolve the current campaign owner on every call, never retain it in a popup callback.
internal static class WorldDiplomacyPresentationHost
{
    internal static bool IsAvailable => DiplomacyModuleServices.World.Presentation != null;
    internal static IReadOnlyList<WorldDiplomacyArchiveRecord> Archive() =>
        DiplomacyModuleServices.World.Presentation?.Archive() ?? Array.Empty<WorldDiplomacyArchiveRecord>();
    internal static string ArchiveSubtitle() => DiplomacyModuleServices.World.Presentation?.ArchiveSubtitle() ?? "";
    internal static string Standing(string kingdomId)
    {
        try { return DiplomacyModuleServices.World.Presentation?.Standing(kingdomId) ?? ""; }
        catch { return ""; }
    }
    internal static WorldDiplomacyPlayerContext Player() => DiplomacyModuleServices.World.Presentation?.Player;
    internal static WorldDiplomacyDocumentDetail Detail(string id) => DiplomacyModuleServices.World.Presentation?.Detail(id);
    internal static string Submit(WorldDiplomacyPlayerDocumentCommand command) => DiplomacyModuleServices.World.Presentation?.Submit(command) ?? "";
    internal static bool MarkRead(string id) => DiplomacyModuleServices.World.Presentation?.MarkRead(id) == true;
    internal static bool CanOpenReply(string documentId, string roundId, long generation) =>
        DiplomacyModuleServices.World.Presentation?.CanOpenReply(documentId, roundId, generation) == true;
}
