using System;
using AnimusForge.Refactor.Contracts;

namespace AnimusForge.Refactor.Modules;

internal sealed class WorldDiplomacyDocumentReadCommandFacade
{
    private readonly IWorldDiplomacyDocumentReadCommand _command;

    public WorldDiplomacyDocumentReadCommandFacade(IWorldDiplomacyDocumentReadCommand command)
    {
        _command = command ?? throw new ArgumentNullException(nameof(command));
    }

    public WorldDiplomacyDocumentReadResult MarkRead(string documentId)
    {
        try
        {
            return _command.MarkRead(documentId);
        }
        catch
        {
            return WorldDiplomacyDocumentReadResult.Failed();
        }
    }

    public bool MarkReadOrFalse(string documentId)
    {
        return MarkRead(documentId).IsApplied;
    }
}
