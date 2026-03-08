using System;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.Core;
using TaleWorlds.Localization;

namespace TaleWorlds.CampaignSystem.ComponentInterfaces
{
	// Token: 0x020001BC RID: 444
	public abstract class InventoryCapacityModel : MBGameModel<InventoryCapacityModel>
	{
		// Token: 0x06001D83 RID: 7555
		public abstract ExplainedNumber CalculateInventoryCapacity(MobileParty mobileParty, bool isCurrentlyAtSea, bool includeDescriptions = false, int additionalManOnFoot = 0, int additionalSpareMounts = 0, int additionalPackAnimals = 0, bool includeFollowers = false);

		// Token: 0x06001D84 RID: 7556
		public abstract int GetItemAverageWeight();

		// Token: 0x06001D85 RID: 7557
		public abstract float GetItemEffectiveWeight(EquipmentElement equipmentElement, MobileParty mobileParty, bool isCurrentlyAtSea, out TextObject description);

		// Token: 0x06001D86 RID: 7558
		public abstract ExplainedNumber CalculateTotalWeightCarried(MobileParty mobileParty, bool isCurrentlyAtSea, bool includeDescriptions = false);
	}
}
