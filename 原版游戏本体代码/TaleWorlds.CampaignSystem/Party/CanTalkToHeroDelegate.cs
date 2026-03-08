using System;
using TaleWorlds.Localization;

namespace TaleWorlds.CampaignSystem.Party
{
	// Token: 0x020002FB RID: 763
	// (Invoke) Token: 0x06002C05 RID: 11269
	public delegate bool CanTalkToHeroDelegate(Hero hero, PartyScreenLogic.TroopType type, PartyScreenLogic.PartyRosterSide side, PartyBase LeftOwnerParty, out TextObject cantTalkReason);
}
