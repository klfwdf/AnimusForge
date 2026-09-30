using System.Collections.Generic;
using System.Linq;

namespace AnimusForge;

// JSON field names and defaults are the existing _npcMajorActions_v1/_npcRecentActions_v1 contract.
internal class NpcActionEntry
{
    public int Day;
    public int Order;
    public int Sequence;
    public string GameDate;
    public string Text;
    public string StableKey;
    public string ActionKind;
    public string ActorHeroId;
    public string ActorClanId;
    public string ActorKingdomId;
    public string TargetHeroId;
    public string TargetClanId;
    public string TargetKingdomId;
    public string SettlementId;
    public string SettlementName;
    public string SettlementOwnerHeroId;
    public string SettlementOwnerClanId;
    public string SettlementOwnerKingdomId;
    public string PreviousSettlementOwnerHeroId;
    public string PreviousSettlementOwnerClanId;
    public string PreviousSettlementOwnerKingdomId;
    public string LocationText;
    public bool? Won;
    public bool IsMajor;
    public List<string> RelatedHeroIds = new List<string>();
    public List<string> RelatedClanIds = new List<string>();
    public List<string> RelatedKingdomIds = new List<string>();

    internal NpcActionEntry CopyForSummary()
    {
        var copy = (NpcActionEntry)MemberwiseClone();
        copy.RelatedHeroIds = RelatedHeroIds?.ToList();
        copy.RelatedClanIds = RelatedClanIds?.ToList();
        copy.RelatedKingdomIds = RelatedKingdomIds?.ToList();
        return copy;
    }
}
