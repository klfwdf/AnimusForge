using System;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.CampaignSystem.Settlements.Buildings;
using TaleWorlds.Core;

namespace TaleWorlds.CampaignSystem.ComponentInterfaces
{
	// Token: 0x020001FB RID: 507
	public abstract class BuildingModel : MBGameModel<BuildingModel>
	{
		// Token: 0x06001F46 RID: 8006
		public abstract bool CanAddBuildingTypeToTown(BuildingType buildingType, Town town);
	}
}
