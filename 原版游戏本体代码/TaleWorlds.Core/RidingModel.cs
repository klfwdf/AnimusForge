using System;

namespace TaleWorlds.Core
{
	// Token: 0x02000071 RID: 113
	public abstract class RidingModel : MBGameModel<RidingModel>
	{
		// Token: 0x060007E6 RID: 2022
		public abstract float CalculateAcceleration(in EquipmentElement mountElement, in EquipmentElement harnessElement, int ridingSkill);
	}
}
