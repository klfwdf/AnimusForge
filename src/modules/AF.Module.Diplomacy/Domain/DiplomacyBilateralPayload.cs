using System;
using AnimusForge.DiplomacyDialogue;

namespace AnimusForge;

internal sealed class DiplomacyBilateralPayload
{
    internal string Tag, Payload, SourceDocument = "", SourceAction = "";
    internal string[] Parts;
    internal DialogueDiplomaticAction Action;
    internal WorldDiplomacyDialogueTerms Terms = new();

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
