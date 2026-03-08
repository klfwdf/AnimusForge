using System;

namespace TaleWorlds.Core
{
	// Token: 0x02000073 RID: 115
	public abstract class ItemValueModel : MBGameModel<ItemValueModel>
	{
		// Token: 0x060007EA RID: 2026
		public abstract float GetEquipmentValueFromTier(float itemTierf);

		// Token: 0x060007EB RID: 2027
		public abstract float CalculateTier(ItemObject item);

		// Token: 0x060007EC RID: 2028
		public abstract int CalculateValue(ItemObject item);

		// Token: 0x060007ED RID: 2029
		public abstract bool GetIsTransferable(ItemObject item);
	}
}
