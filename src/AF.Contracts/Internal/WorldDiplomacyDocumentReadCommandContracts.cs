namespace AnimusForge.Refactor.Contracts;

public enum WorldDiplomacyDocumentReadStatus
{
    InvalidDocumentId,
    Applied,
    NotFound,
    Unavailable,
    Failed
}

public readonly struct WorldDiplomacyDocumentReadResult
{
    private WorldDiplomacyDocumentReadResult(WorldDiplomacyDocumentReadStatus status)
    {
        Status = status;
    }

    public WorldDiplomacyDocumentReadStatus Status { get; }
    public bool IsApplied => Status == WorldDiplomacyDocumentReadStatus.Applied;

    public static WorldDiplomacyDocumentReadResult Applied()
    {
        return new WorldDiplomacyDocumentReadResult(WorldDiplomacyDocumentReadStatus.Applied);
    }

    public static WorldDiplomacyDocumentReadResult InvalidDocumentId()
    {
        return new WorldDiplomacyDocumentReadResult(WorldDiplomacyDocumentReadStatus.InvalidDocumentId);
    }

    public static WorldDiplomacyDocumentReadResult NotFound()
    {
        return new WorldDiplomacyDocumentReadResult(WorldDiplomacyDocumentReadStatus.NotFound);
    }

    public static WorldDiplomacyDocumentReadResult Unavailable()
    {
        return new WorldDiplomacyDocumentReadResult(WorldDiplomacyDocumentReadStatus.Unavailable);
    }

    public static WorldDiplomacyDocumentReadResult Failed()
    {
        return new WorldDiplomacyDocumentReadResult(WorldDiplomacyDocumentReadStatus.Failed);
    }
}

public interface IWorldDiplomacyDocumentReadCommand
{
    WorldDiplomacyDocumentReadResult MarkRead(string documentId);
}
