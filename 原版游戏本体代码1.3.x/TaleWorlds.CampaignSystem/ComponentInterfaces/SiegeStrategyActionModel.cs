using System;
using TaleWorlds.CampaignSystem.Siege;
using TaleWorlds.Core;

namespace TaleWorlds.CampaignSystem.ComponentInterfaces
{
	// Token: 0x020001E2 RID: 482
	public abstract class SiegeStrategyActionModel : MBGameModel<SiegeStrategyActionModel>
	{
		// Token: 0x06001E8C RID: 7820
		public abstract void GetLogicalActionForStrategy(ISiegeEventSide side, out SiegeStrategyActionModel.SiegeAction siegeAction, out SiegeEngineType siegeEngineType, out int deploymentIndex, out int reserveIndex);

		// Token: 0x020005F8 RID: 1528
		public enum SiegeAction
		{
			// Token: 0x040018B9 RID: 6329
			ConstructNewSiegeEngine,
			// Token: 0x040018BA RID: 6330
			DeploySiegeEngineFromReserve,
			// Token: 0x040018BB RID: 6331
			MoveSiegeEngineToReserve,
			// Token: 0x040018BC RID: 6332
			RemoveDeployedSiegeEngine,
			// Token: 0x040018BD RID: 6333
			Hold
		}
	}
}
