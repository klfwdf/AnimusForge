using AnimusForge.Refactor.Contracts;
namespace AnimusForge.Refactor.Adapters;
internal sealed class WorldDiplomacyDocumentReadCommandAdapter : IWorldDiplomacyDocumentReadCommand
{
    public WorldDiplomacyDocumentReadResult MarkRead(string documentId) => DiplomacyModuleServices.World.MarkTimelineRead(documentId);
}
