using System;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.CampaignSystem.Settlements.Locations;
using TaleWorlds.Core;

namespace TaleWorlds.CampaignSystem.ComponentInterfaces
{
	// Token: 0x020001CD RID: 461
	public abstract class HeroAgentLocationModel : MBGameModel<HeroAgentLocationModel>
	{
		// Token: 0x06001E06 RID: 7686
		public abstract bool WillBeListedInOverlay(LocationCharacter locationCharacter);

		// Token: 0x06001E07 RID: 7687
		public abstract Location GetLocationForHero(Hero hero, Settlement settlement, out HeroAgentLocationModel.HeroLocationDetail heroSpawnDetail);

		// Token: 0x020005F7 RID: 1527
		public enum HeroLocationDetail
		{
			// Token: 0x040018AE RID: 6318
			None,
			// Token: 0x040018AF RID: 6319
			SettlementKingQueen,
			// Token: 0x040018B0 RID: 6320
			NobleBelongingToNoParty,
			// Token: 0x040018B1 RID: 6321
			Prisoner,
			// Token: 0x040018B2 RID: 6322
			PlayerClanMember,
			// Token: 0x040018B3 RID: 6323
			MainPartyCompanion,
			// Token: 0x040018B4 RID: 6324
			Notable,
			// Token: 0x040018B5 RID: 6325
			Wanderer,
			// Token: 0x040018B6 RID: 6326
			PartyLeader,
			// Token: 0x040018B7 RID: 6327
			PartylessHeroInsideVillage
		}
	}
}
