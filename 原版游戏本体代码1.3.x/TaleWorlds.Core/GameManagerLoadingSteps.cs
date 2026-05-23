using System;

namespace TaleWorlds.Core
{
	// Token: 0x0200006B RID: 107
	public enum GameManagerLoadingSteps
	{
		// Token: 0x040003FE RID: 1022
		None = -1,
		// Token: 0x040003FF RID: 1023
		PreInitializeZerothStep,
		// Token: 0x04000400 RID: 1024
		FirstInitializeFirstStep,
		// Token: 0x04000401 RID: 1025
		WaitSecondStep,
		// Token: 0x04000402 RID: 1026
		SecondInitializeThirdState,
		// Token: 0x04000403 RID: 1027
		PostInitializeFourthState,
		// Token: 0x04000404 RID: 1028
		FinishLoadingFifthStep,
		// Token: 0x04000405 RID: 1029
		LoadingIsOver
	}
}
