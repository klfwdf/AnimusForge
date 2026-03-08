using System;
using TaleWorlds.CampaignSystem.MapEvents;
using TaleWorlds.CampaignSystem.Naval;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.CampaignSystem.Siege;
using TaleWorlds.Core;
using TaleWorlds.Library;
using TaleWorlds.Localization;

namespace TaleWorlds.CampaignSystem.ComponentInterfaces
{
	// Token: 0x020001DC RID: 476
	public abstract class TroopSacrificeModel : MBGameModel<TroopSacrificeModel>
	{
		// Token: 0x17000797 RID: 1943
		// (get) Token: 0x06001E5D RID: 7773
		public abstract int BreakOutArmyLeaderRelationPenalty { get; }

		// Token: 0x17000798 RID: 1944
		// (get) Token: 0x06001E5E RID: 7774
		public abstract int BreakOutArmyMemberRelationPenalty { get; }

		// Token: 0x06001E5F RID: 7775
		public abstract ExplainedNumber GetLostTroopCountForBreakingInBesiegedSettlement(MobileParty party, SiegeEvent siegeEvent);

		// Token: 0x06001E60 RID: 7776
		public abstract ExplainedNumber GetLostTroopCountForBreakingOutOfBesiegedSettlement(MobileParty party, SiegeEvent siegeEvent, bool isBreakingOutFromPort);

		// Token: 0x06001E61 RID: 7777
		public abstract int GetNumberOfTroopsSacrificedForTryingToGetAway(BattleSideEnum playerBattleSide, MapEvent mapEvent);

		// Token: 0x06001E62 RID: 7778
		public abstract void GetShipsToSacrificeForTryingToGetAway(BattleSideEnum playerBattleSide, MapEvent mapEvent, out MBList<Ship> shipsToCapture, out Ship shipToTakeDamage, out float damageToApplyForLastShip);

		// Token: 0x06001E63 RID: 7779
		public abstract bool CanPlayerGetAwayFromEncounter(out TextObject explanation);
	}
}
