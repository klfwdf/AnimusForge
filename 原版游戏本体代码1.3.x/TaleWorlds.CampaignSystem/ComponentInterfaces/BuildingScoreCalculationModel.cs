using System;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.CampaignSystem.Settlements.Buildings;
using TaleWorlds.Core;

namespace TaleWorlds.CampaignSystem.ComponentInterfaces
{
	// Token: 0x020001E7 RID: 487
	public abstract class BuildingScoreCalculationModel : MBGameModel<BuildingScoreCalculationModel>
	{
		// Token: 0x06001EB3 RID: 7859
		public abstract Building GetNextBuilding(Town town);

		// Token: 0x06001EB4 RID: 7860
		public abstract Building GetNextDailyBuilding(Town town);
	}
}
