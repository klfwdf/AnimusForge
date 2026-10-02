using System.Collections.Generic;
using AnimusForge.Refactor.Contracts;

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
