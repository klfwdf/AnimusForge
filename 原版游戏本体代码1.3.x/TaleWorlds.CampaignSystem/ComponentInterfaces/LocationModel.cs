using System;
using TaleWorlds.CampaignSystem.Encounters;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.Core;

namespace TaleWorlds.CampaignSystem.ComponentInterfaces
{
	// Token: 0x02000188 RID: 392
	public abstract class LocationModel : MBGameModel<LocationModel>
	{
		// Token: 0x06001BD4 RID: 7124
		public abstract int GetSettlementUpgradeLevel(LocationEncounter locationEncounter);

		// Token: 0x06001BD5 RID: 7125
		public abstract string GetCivilianSceneLevel(Settlement settlement);

		// Token: 0x06001BD6 RID: 7126
		public abstract string GetCivilianUpgradeLevelTag(int upgradeLevel);

		// Token: 0x06001BD7 RID: 7127
		public abstract string GetUpgradeLevelTag(int upgradeLevel);
	}
}
