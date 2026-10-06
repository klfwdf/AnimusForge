using TaleWorlds.CampaignSystem.Roster;

namespace TaleWorlds.CampaignSystem.Party;

public delegate void PartyScreenClosedDelegate(PartyBase leftOwnerParty, TroopRoster leftMemberRoster, TroopRoster leftPrisonRoster, PartyBase rightOwnerParty, TroopRoster rightMemberRoster, TroopRoster rightPrisonRoster, bool fromCancel);
