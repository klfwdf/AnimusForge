using System;

namespace TaleWorlds.Engine.Options
{
	// Token: 0x020000A4 RID: 164
	public interface INumericOptionData : IOptionData
	{
		// Token: 0x06000F28 RID: 3880
		float GetMinValue();

		// Token: 0x06000F29 RID: 3881
		float GetMaxValue();

		// Token: 0x06000F2A RID: 3882
		bool GetIsDiscrete();

		// Token: 0x06000F2B RID: 3883
		int GetDiscreteIncrementInterval();

		// Token: 0x06000F2C RID: 3884
		bool GetShouldUpdateContinuously();
	}
}
