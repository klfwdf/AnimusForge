using System;
using System.Collections.Generic;

namespace AnimusForge.DiplomacyDialogue;

/// <summary>One format shared by messenger, free conversation and scene shout.
/// IDs come from the runtime list; unknown fields and malformed numbers fail closed.</summary>
public sealed class DialogueTagPayload
{
    public DialogueDiplomaticAction Action { get; private set; }
    public DialogueDiplomaticMove Move { get; private set; }
    public string TargetKingdomId { get; private set; }
    public string SourceDocumentId { get; private set; }
    public string SourceActionId { get; private set; }
    public string EventRoundId { get; private set; }
    public DialogueDiplomaticTerms Terms { get; private set; }
    public string SupersedesArrangementId { get; private set; }
    public int SupersedesVersion { get; private set; }
    public string RevisionReason { get; private set; }
    public string Condition { get; private set; }
    public int SponsorGold { get; private set; }
    public string SponsorReceipt { get; private set; }
    public string Continuation { get; private set; }
    public bool HasLobbyistFields { get; private set; }
    public static bool TryParse(string payload, out DialogueTagPayload result)
    {
        result = null;
        if (string.IsNullOrWhiteSpace(payload) || payload.Length > 1800) return false;
        var fields = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        var allowed = new HashSet<string>(new[] { "action", "move", "target", "source_document", "source_action", "receiving", "joining",
            "payer", "receiver", "tribute", "days", "cession_from", "cession_to", "settlement", "supersedes", "version", "reason", "event",
            "condition", "sponsor_gold", "sponsor_receipt", "continuation" }, StringComparer.OrdinalIgnoreCase);
        foreach (string part in payload.Split(';'))
        {
            int separator = part.IndexOf('=');
            if (separator <= 0) return false;
            string key = part.Substring(0, separator).Trim(), value = part.Substring(separator + 1).Trim();
            if (!allowed.Contains(key) || fields.ContainsKey(key) || value.IndexOf('=') >= 0) return false;
            fields.Add(key, value);
        }
        string Get(string key) => fields.TryGetValue(key, out string value) ? value : "";
        if (!Enum.TryParse(Get("action"), true, out DialogueDiplomaticAction action) || !Enum.IsDefined(typeof(DialogueDiplomaticAction), action)
            || !Enum.TryParse(Get("move"), true, out DialogueDiplomaticMove move) || !Enum.IsDefined(typeof(DialogueDiplomaticMove), move)
            || string.IsNullOrWhiteSpace(Get("target"))) return false;
        int tribute = 0, days = 0;
        int sponsor = 0;
        string condition = Get("condition");
        if (condition != "" && condition != "none" && condition != "player_gold" && condition != "clarify") return false;
        if (fields.ContainsKey("sponsor_gold") && (!int.TryParse(Get("sponsor_gold"), out sponsor) || sponsor <= 0)) return false;
        if ((condition == "player_gold") != (sponsor > 0)
            || (condition != "player_gold" && fields.ContainsKey("sponsor_receipt"))) return false;
        if ((fields.ContainsKey("tribute") && (!int.TryParse(Get("tribute"), out tribute) || tribute < 0))
            || (fields.ContainsKey("days") && (!int.TryParse(Get("days"), out days) || days < 0 || days > 252))) return false;
        if (move == DialogueDiplomaticMove.NewMatter && (!string.IsNullOrEmpty(Get("source_document")) || !string.IsNullOrEmpty(Get("source_action")))) return false;
        int version = 0;
        bool revision = fields.ContainsKey("supersedes") || fields.ContainsKey("version") || fields.ContainsKey("reason");
        if (revision && (move != DialogueDiplomaticMove.NewMatter || string.IsNullOrWhiteSpace(Get("supersedes"))
            || !int.TryParse(Get("version"), out version) || version <= 0 || version == int.MaxValue
            || string.IsNullOrWhiteSpace(Get("reason")) || Get("reason").Length > 180)) return false;
        result = new DialogueTagPayload { Action = action, Move = move, TargetKingdomId = Get("target"),
            Condition = condition, SponsorGold = sponsor, SponsorReceipt = Get("sponsor_receipt"), Continuation = Get("continuation"),
            HasLobbyistFields = fields.ContainsKey("condition") || fields.ContainsKey("sponsor_gold")
                || fields.ContainsKey("sponsor_receipt") || fields.ContainsKey("continuation"),
            EventRoundId = Get("event"),
            SupersedesArrangementId = Get("supersedes"), SupersedesVersion = version, RevisionReason = Get("reason"),
            SourceDocumentId = Get("source_document"), SourceActionId = Get("source_action"),
            Terms = new DialogueDiplomaticTerms(Get("receiving"), Get("joining"), Get("payer"), Get("receiver"), tribute, days,
                Get("cession_from"), Get("cession_to"), Get("settlement")) };
        return true;
    }
}
