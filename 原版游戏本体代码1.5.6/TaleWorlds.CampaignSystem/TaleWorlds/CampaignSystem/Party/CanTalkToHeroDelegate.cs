using TaleWorlds.Localization;

namespace TaleWorlds.CampaignSystem.Party;

public delegate bool CanTalkToHeroDelegate(Hero hero, PartyScreenLogic.TroopType type, PartyScreenLogic.PartyRosterSide side, PartyBase LeftOwnerParty, out TextObject cantTalkReason);
