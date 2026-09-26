using AnimusForge.Refactor.Contracts;
using System;
using System.Collections.Generic;

namespace AnimusForge;

internal interface IWorldDiplomacyPresentationPort
{
    IReadOnlyList<WorldDiplomacyArchiveRecord> Archive();
    string ArchiveSubtitle();
    string Standing(string kingdomId);
    WorldDiplomacyPlayerContext Player { get; }
    WorldDiplomacyDocumentDetail Detail(string id);
    string Submit(WorldDiplomacyPlayerDocumentCommand command);
    bool MarkRead(string id);
    bool CanOpenReply(string documentId, string roundId, long generation);
}

// Composition boundary for detail/archive/player UI; timeline keeps its existing facade.
// Resolve the current campaign owner on every call, never retain it in a popup callback.
internal static class WorldDiplomacyPresentationHost
{
    internal static bool IsAvailable => WorldDiplomacyBehavior.ResolvePresentationPort() != null;
    internal static IReadOnlyList<WorldDiplomacyArchiveRecord> Archive() =>
        WorldDiplomacyBehavior.ResolvePresentationPort()?.Archive() ?? Array.Empty<WorldDiplomacyArchiveRecord>();
    internal static string ArchiveSubtitle() => WorldDiplomacyBehavior.ResolvePresentationPort()?.ArchiveSubtitle() ?? "";
    internal static string Standing(string kingdomId)
    {
        try { return WorldDiplomacyBehavior.ResolvePresentationPort()?.Standing(kingdomId) ?? ""; }
        catch { return ""; }
    }
    internal static WorldDiplomacyPlayerContext Player() => WorldDiplomacyBehavior.ResolvePresentationPort()?.Player;
    internal static WorldDiplomacyDocumentDetail Detail(string id) => WorldDiplomacyBehavior.ResolvePresentationPort()?.Detail(id);
    internal static string Submit(WorldDiplomacyPlayerDocumentCommand command) => WorldDiplomacyBehavior.ResolvePresentationPort()?.Submit(command) ?? "";
    internal static bool MarkRead(string id) => WorldDiplomacyBehavior.ResolvePresentationPort()?.MarkRead(id) == true;
    internal static bool CanOpenReply(string documentId, string roundId, long generation) =>
        WorldDiplomacyBehavior.ResolvePresentationPort()?.CanOpenReply(documentId, roundId, generation) == true;
}
