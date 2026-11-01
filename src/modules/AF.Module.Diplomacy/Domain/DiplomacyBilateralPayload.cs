using System;
using System.Globalization;
using AnimusForge.DiplomacyDialogue;

namespace AnimusForge;

internal sealed class DiplomacyBilateralPayload
{
    internal string Tag, Payload, SourceDocument = "", SourceAction = "";
    internal string[] Parts;
    internal DialogueDiplomaticAction Action;
    internal WorldDiplomacyDialogueTerms Terms = new();

    // Only syntax is normalized here. Current rulers, source offers, conditions,
    // idempotency and effects remain owned by the existing application paths.
    internal static bool TryLobbyistCommit(string tag, string payload, string npc, string playerAuthority,
        out string commitment, out string error)
    {
        commitment = null; error = "";
        if (!DiplomacyBilateralRouting.TryAction(tag, out var action)) return false;
        if (string.IsNullOrWhiteSpace(payload) || payload.Length > 1800)
        { error = "invalid_payload"; return true; }
        string[] pair = payload.Split('|')[0].Split(':');
        if (pair.Length >= 2 && DiplomacyBilateralRouting.IsPair(pair[0].Trim(), pair[1].Trim(), playerAuthority, npc))
            return false;
        if (pair.Length < 2 || !SafeAtom(pair[0].Trim()) || !SafeAtom(pair[1].Trim())
            || string.Equals(pair[0].Trim(), pair[1].Trim(), StringComparison.OrdinalIgnoreCase))
        { error = "invalid_participants"; return true; }
        bool firstNpc = string.Equals(pair[0].Trim(), npc, StringComparison.OrdinalIgnoreCase);
        bool secondNpc = string.Equals(pair[1].Trim(), npc, StringComparison.OrdinalIgnoreCase);
        bool unilateral = action is DialogueDiplomaticAction.DeclareWar or DialogueDiplomaticAction.BreakAlliance or DialogueDiplomaticAction.CancelTrade;
        if ((!firstNpc && !secondNpc) || unilateral && !firstNpc)
        { error = "speaker_is_not_actor"; return true; }
        string target = (firstNpc ? pair[1] : pair[0]).Trim();
        if (!TryParse(tag, payload, target, npc, out var request)
            || !SafeAtom(request.SourceDocument, true) || !SafeAtom(request.SourceAction, true)
            || !SafeAtom(request.Terms.CessionSettlementId, true))
        { error = "invalid_or_incomplete_terms"; return true; }
        string Num(int value) => value.ToString(CultureInfo.InvariantCulture);
        var t = request.Terms;
        commitment = "action=" + action + ";move=" + (request.SourceDocument == "" ? "NewMatter" : "AcceptProposal") + ";target=" + target;
        if (action == DialogueDiplomaticAction.Peace)
        {
            commitment += ";tribute=" + Num(t.DailyTribute) + ";days=" + Num(t.DurationDays);
            if (t.DailyTribute > 0) commitment += ";payer=" + t.TributePayerKingdomId + ";receiver=" + t.TributeReceiverKingdomId;
            if (!string.IsNullOrEmpty(t.CessionSettlementId)) commitment += ";cession_from=" + t.CessionFromKingdomId
                + ";cession_to=" + t.CessionToKingdomId + ";settlement=" + t.CessionSettlementId;
        }
        else if (t.DurationDays > 0) commitment += ";days=" + Num(t.DurationDays);
        if (request.SourceDocument != "") commitment += ";source_document=" + request.SourceDocument + ";source_action=" + request.SourceAction;
        if (!DialogueTagPayload.TryParse(commitment, out _))
        { commitment = null; error = "unrepresentable_terms"; }
        return true;
    }

    internal static bool TryFromCommit(DialogueTagPayload parsed, string player, string npc, out DiplomacyBilateralPayload value)
    {
        value = null;
        if (!DiplomacyBilateralRouting.OwnsMatter(parsed, npc, player)
            || !string.IsNullOrEmpty(parsed.Condition) && parsed.Condition != "none"
            || parsed.SponsorGold != 0 || !string.IsNullOrEmpty(parsed.Continuation)
            || !string.IsNullOrEmpty(parsed.EventRoundId) || !string.IsNullOrEmpty(parsed.SupersedesArrangementId)) return false;
        var t = parsed.Terms;
        if (!string.IsNullOrEmpty(t.ReceivingKingdomId) || !string.IsNullOrEmpty(t.JoiningKingdomId)) return false;
        string pair = npc + ":" + player, tag, payload;
        string Num(int number) => number.ToString(CultureInfo.InvariantCulture);
        bool cession = !string.IsNullOrEmpty(t.CessionFromKingdomId) || !string.IsNullOrEmpty(t.CessionToKingdomId) || !string.IsNullOrEmpty(t.CessionSettlementId);
        bool tribute = t.DailyTribute != 0 || !string.IsNullOrEmpty(t.TributePayerKingdomId) || !string.IsNullOrEmpty(t.TributeReceiverKingdomId);
        if (parsed.Action != DialogueDiplomaticAction.Peace && (cession || tribute)) return false;
        switch (parsed.Action)
        {
            case DialogueDiplomaticAction.Peace:
                tag = "MAKE_PEACE";
                if (t.DailyTribute == 0 && !tribute) payload = pair + ":0:" + Num(t.DurationDays);
                else payload = t.TributePayerKingdomId + ":" + t.TributeReceiverKingdomId + ":" + Num(t.DailyTribute) + ":" + Num(t.DurationDays);
                if (cession) payload += ":" + t.CessionFromKingdomId + ":" + t.CessionToKingdomId + ":" + t.CessionSettlementId;
                break;
            case DialogueDiplomaticAction.Alliance:
                if (t.DurationDays != 0) return false;
                tag = "FORM_ALLIANCE"; payload = pair + ":default"; break;
            case DialogueDiplomaticAction.Trade:
                tag = "MAKE_TRADE"; payload = pair + ":" + (t.DurationDays == 0 ? "default" : Num(t.DurationDays)); break;
            default:
                if (t.DurationDays != 0) return false;
                tag = parsed.Action == DialogueDiplomaticAction.DeclareWar ? "DECLARE_WAR"
                    : parsed.Action == DialogueDiplomaticAction.BreakAlliance ? "BREAK_ALLIANCE" : "CANCEL_TRADE";
                payload = pair; break;
        }
        if (parsed.Move == DialogueDiplomaticMove.AcceptProposal)
        {
            if (!SafeAtom(parsed.SourceDocumentId) || !SafeAtom(parsed.SourceActionId, true)) return false;
            payload += "|document=" + parsed.SourceDocumentId + ";action=" + parsed.SourceActionId;
        }
        return TryParse(tag, payload, player, npc, out value);
    }

    private static bool SafeAtom(string value, bool allowEmpty = false)
    {
        if (string.IsNullOrEmpty(value)) return allowEmpty;
        foreach (char c in value) if (char.IsWhiteSpace(c) || ":;=|[]".IndexOf(c) >= 0) return false;
        return true;
    }

    internal static bool TryParse(string tag, string payload, string player, string npc, out DiplomacyBilateralPayload value)
    {
        value = null;
        if (!DiplomacyBilateralRouting.TryAction(tag, out var action) || string.IsNullOrWhiteSpace(payload) || payload.Length > 1800) return false;
        string[] binding = payload.Split('|');
        if (binding.Length > 2) return false;
        string[] p = binding[0].Split(':');
        for (int i=0;i<p.Length;i++) p[i]=p[i].Trim();
        if (p.Length < 2 || !DiplomacyBilateralRouting.IsPair(p[0],p[1],player,npc)) return false;
        // Resolve model spelling to the current authoritative identities before
        // the native rules, effect ports and persisted evidence consume it.
        p[0]=string.Equals(p[0],player,StringComparison.OrdinalIgnoreCase)?player:npc;
        p[1]=string.Equals(p[1],player,StringComparison.OrdinalIgnoreCase)?player:npc;
        var v = new DiplomacyBilateralPayload { Tag=tag.ToUpperInvariant(), Action=action, Payload=string.Join(":",p),Parts=p };
        if (binding.Length == 2)
        {
            string[] source=binding[1].Split(';');
            if(source.Length!=2 || !source[0].StartsWith("document=",StringComparison.Ordinal)
                || !source[1].StartsWith("action=",StringComparison.Ordinal))return false;
            v.SourceDocument=source[0].Substring(9).Trim();v.SourceAction=source[1].Substring(7).Trim();
            if(string.IsNullOrWhiteSpace(v.SourceDocument) || v.SourceDocument.Contains("=") || v.SourceAction.Contains("="))return false;
        }
        switch(v.Tag)
        {
            case "MAKE_PEACE":
                if(p.Length!=4 && p.Length!=7 || !int.TryParse(p[2],out int amount) || !int.TryParse(p[3],out int days)
                    || amount<0 || amount==0 && days!=0 || amount>0 && (days<1 || days>252))return false;
                v.Terms.TributePayerKingdomId=p[0];v.Terms.TributeReceiverKingdomId=p[1];v.Terms.DailyTribute=amount;v.Terms.DurationDays=days;
                if(p.Length==7)
                {
                    if(!DiplomacyBilateralRouting.IsPair(p[4],p[5],player,npc) || string.IsNullOrWhiteSpace(p[6]))return false;
                    p[4]=string.Equals(p[4],player,StringComparison.OrdinalIgnoreCase)?player:npc;
                    p[5]=string.Equals(p[5],player,StringComparison.OrdinalIgnoreCase)?player:npc;
                    v.Terms.CessionFromKingdomId=p[4];v.Terms.CessionToKingdomId=p[5];v.Terms.CessionSettlementId=p[6];
                }
                break;
            case "FORM_ALLIANCE":
                if(p.Length!=2 && (p.Length!=3 || !p[2].Equals("default",StringComparison.OrdinalIgnoreCase)))return false;
                break;
            case "MAKE_TRADE":
                if(p.Length!=2 && (p.Length!=3 || !p[2].Equals("default",StringComparison.OrdinalIgnoreCase)
                    && (!int.TryParse(p[2],out int duration) || duration<1 || duration>252)))return false;
                if(p.Length==3 && int.TryParse(p[2],out int tradeDays))v.Terms.DurationDays=tradeDays;
                break;
            default: if(p.Length!=2)return false;break;
        }
        v.Payload=string.Join(":",p);value=v;return true;
    }
}
