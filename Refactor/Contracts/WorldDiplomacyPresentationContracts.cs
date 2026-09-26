using System;
using System.Collections.Generic;

namespace AnimusForge.Refactor.Contracts;

internal sealed class WorldDiplomacyNotice
{
    internal WorldDiplomacyNotice(string documentId, string title, string description)
    { DocumentId = documentId; Title = title; Description = description; }
    public string DocumentId { get; }
    public string Title { get; }
    public string Description { get; }
}

internal sealed class WorldDiplomacyPlayerContext
{
    internal WorldDiplomacyPlayerContext(long generation, string kingdomId, bool isRuler, bool independent, string representativeName)
    { Generation = generation; KingdomId = kingdomId; IsRuler = isRuler; Independent = independent; RepresentativeName = representativeName; }
    public long Generation { get; }
    public string KingdomId { get; }
    public bool IsRuler { get; }
    public bool Independent { get; }
    public string RepresentativeName { get; }
}

internal sealed class WorldDiplomacyPlayerDocumentCommand
{
    internal WorldDiplomacyPlayerDocumentCommand(string body, long generation, string sourceDocumentId = null, string roundId = null)
    { Body = body; Generation = generation; SourceDocumentId = sourceDocumentId; RoundId = roundId; }
    public string Body { get; }
    public long Generation { get; }
    public string SourceDocumentId { get; }
    public string RoundId { get; }
    public bool IsReply => SourceDocumentId != null;
}

internal sealed class WorldDiplomacyDocumentDetail
{
    internal WorldDiplomacyDocumentDetail(string documentId, string roundId, long generation,
        string title, string subtitle, string body, string impact, bool canReply)
    { DocumentId = documentId; RoundId = roundId; Generation = generation; Title = title;
      Subtitle = subtitle; Body = body; Impact = impact; CanReply = canReply; }
    public string DocumentId { get; }
    public string RoundId { get; }
    public long Generation { get; }
    public string Title { get; }
    public string Subtitle { get; }
    public string Body { get; }
    public string Impact { get; }
    public bool CanReply { get; }
}

internal sealed class WorldDiplomacyArchiveRecord
{
    internal WorldDiplomacyArchiveRecord(string KingdomId, string KingdomName,
        string EventId = "",
        string KindLabel = "",
        string HeaderRightText = "",
        string DateText = "",
        string TitleText = "",
        string IndexTitleText = "",
        string MetaText = "",
        string PolicyNameText = "",
        string BodyText = "",
        string BodySectionTitleText = "",
        string ImpactSectionTitleText = "",
        string ImpactText = "",
        string IndexMetaText = "",
        string UnreadMarkerText = "",
        bool IsUnread = false,
        bool HasPolicyName = false,
        bool HasImpact = false)
    {
        this.KingdomId = KingdomId;
        this.KingdomName = KingdomName;
        this.EventId = EventId;
        this.KindLabel = KindLabel;
        this.HeaderRightText = HeaderRightText;
        this.DateText = DateText;
        this.TitleText = TitleText;
        this.IndexTitleText = IndexTitleText;
        this.MetaText = MetaText;
        this.PolicyNameText = PolicyNameText;
        this.BodyText = BodyText;
        this.BodySectionTitleText = BodySectionTitleText;
        this.ImpactSectionTitleText = ImpactSectionTitleText;
        this.ImpactText = ImpactText;
        this.IndexMetaText = IndexMetaText;
        this.UnreadMarkerText = UnreadMarkerText;
        this.IsUnread = IsUnread;
        this.HasPolicyName = HasPolicyName;
        this.HasImpact = HasImpact;
    }
    public string KingdomId { get; }
    public string KingdomName { get; }
    public string EventId { get; }
    public string KindLabel { get; }
    public string HeaderRightText { get; }
    public string DateText { get; }
    public string TitleText { get; }
    public string IndexTitleText { get; }
    public string MetaText { get; }
    public string PolicyNameText { get; }
    public string BodyText { get; }
    public string BodySectionTitleText { get; }
    public string ImpactSectionTitleText { get; }
    public string ImpactText { get; }
    public string IndexMetaText { get; }
    public string UnreadMarkerText { get; }
    public bool IsUnread { get; }
    public bool HasPolicyName { get; }
    public bool HasImpact { get; }
}
