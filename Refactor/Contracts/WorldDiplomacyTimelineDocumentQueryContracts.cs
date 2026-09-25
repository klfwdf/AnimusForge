using System;
using System.Collections.Generic;
using System.Linq;

namespace AnimusForge.Refactor.Contracts;

public enum WorldDiplomacyTimelineDocumentsStatus
{
    Unavailable,
    Available,
    Failed
}

public sealed class WorldDiplomacyTimelineCountryReference
{
    public WorldDiplomacyTimelineCountryReference(string countryId, string countryName)
    {
        CountryId = countryId ?? string.Empty;
        CountryName = countryName ?? string.Empty;
    }

    public string CountryId { get; }
    public string CountryName { get; }
}

/// <summary>
/// Immutable timeline projection. It deliberately excludes persisted diplomacy DTOs and live game objects.
/// </summary>
public sealed class WorldDiplomacyTimelineDocument
{
    public WorldDiplomacyTimelineDocument(
        string documentId,
        string authorKingdomId,
        string authorKingdomName,
        string targetKingdomId,
        string targetKingdomName,
        string title,
        string body,
        string gameDate,
        int day,
        long createdUtcTicks,
        bool isResponse,
        bool requiresResponse,
        bool changedDiplomaticState,
        bool isRead,
        string impactText,
        IEnumerable<WorldDiplomacyTimelineCountryReference> actionTargets)
    {
        DocumentId = documentId ?? string.Empty;
        AuthorKingdomId = authorKingdomId ?? string.Empty;
        AuthorKingdomName = authorKingdomName ?? string.Empty;
        TargetKingdomId = targetKingdomId ?? string.Empty;
        TargetKingdomName = targetKingdomName ?? string.Empty;
        Title = title ?? string.Empty;
        Body = body ?? string.Empty;
        GameDate = gameDate ?? string.Empty;
        Day = day;
        CreatedUtcTicks = createdUtcTicks;
        IsResponse = isResponse;
        RequiresResponse = requiresResponse;
        ChangedDiplomaticState = changedDiplomaticState;
        IsRead = isRead;
        ImpactText = impactText ?? string.Empty;
        ActionTargets = (actionTargets ?? Enumerable.Empty<WorldDiplomacyTimelineCountryReference>())
            .Where(target => target != null)
            .ToList()
            .AsReadOnly();
    }

    public string DocumentId { get; }
    public string AuthorKingdomId { get; }
    public string AuthorKingdomName { get; }
    public string TargetKingdomId { get; }
    public string TargetKingdomName { get; }
    public string Title { get; }
    public string Body { get; }
    public string GameDate { get; }
    public int Day { get; }
    public long CreatedUtcTicks { get; }
    public bool IsResponse { get; }
    public bool RequiresResponse { get; }
    public bool ChangedDiplomaticState { get; }
    public bool IsRead { get; }
    public string ImpactText { get; }
    public IReadOnlyList<WorldDiplomacyTimelineCountryReference> ActionTargets { get; }
}

public readonly struct WorldDiplomacyTimelineDocumentsResult
{
    private readonly IReadOnlyList<WorldDiplomacyTimelineDocument> _documents;

    private WorldDiplomacyTimelineDocumentsResult(
        WorldDiplomacyTimelineDocumentsStatus status,
        IEnumerable<WorldDiplomacyTimelineDocument> documents)
    {
        Status = status;
        _documents = status == WorldDiplomacyTimelineDocumentsStatus.Available
            ? (documents ?? Enumerable.Empty<WorldDiplomacyTimelineDocument>())
                .Where(document => document != null)
                .ToList()
                .AsReadOnly()
            : Array.Empty<WorldDiplomacyTimelineDocument>();
    }

    public WorldDiplomacyTimelineDocumentsStatus Status { get; }
    public bool IsAvailable => Status == WorldDiplomacyTimelineDocumentsStatus.Available;
    public IReadOnlyList<WorldDiplomacyTimelineDocument> Documents =>
        _documents ?? Array.Empty<WorldDiplomacyTimelineDocument>();

    public static WorldDiplomacyTimelineDocumentsResult Available(
        IEnumerable<WorldDiplomacyTimelineDocument> documents)
    {
        return new WorldDiplomacyTimelineDocumentsResult(
            WorldDiplomacyTimelineDocumentsStatus.Available,
            documents);
    }

    public static WorldDiplomacyTimelineDocumentsResult Unavailable()
    {
        return new WorldDiplomacyTimelineDocumentsResult(
            WorldDiplomacyTimelineDocumentsStatus.Unavailable,
            Array.Empty<WorldDiplomacyTimelineDocument>());
    }

    public static WorldDiplomacyTimelineDocumentsResult Failed()
    {
        return new WorldDiplomacyTimelineDocumentsResult(
            WorldDiplomacyTimelineDocumentsStatus.Failed,
            Array.Empty<WorldDiplomacyTimelineDocument>());
    }
}

public interface IWorldDiplomacyTimelineDocumentQuery
{
    WorldDiplomacyTimelineDocumentsResult Query(int maxCount);
}
