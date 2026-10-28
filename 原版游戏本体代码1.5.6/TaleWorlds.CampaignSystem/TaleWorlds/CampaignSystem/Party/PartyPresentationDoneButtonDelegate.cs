using TaleWorlds.CampaignSystem.Roster;

namespace TaleWorlds.CampaignSystem.Party;

public delegate bool PartyPresentationDoneButtonDelegate(TroopRoster leftMemberRoster, TroopRoster leftPrisonRoster, TroopRoster rightMemberRoster, TroopRoster rightPrisonRoster, FlattenedTroopRoster takenPrisonerRoster, FlattenedTroopRoster releasedPrisonerRoster, bool isForced, PartyBase leftParty = null, PartyBase rightParty = null);
