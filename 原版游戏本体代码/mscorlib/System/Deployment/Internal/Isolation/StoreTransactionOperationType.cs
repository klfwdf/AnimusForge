using System;

namespace System.Deployment.Internal.Isolation
{
	// Token: 0x020006AB RID: 1707
	internal enum StoreTransactionOperationType
	{
		// Token: 0x0400225B RID: 8795
		Invalid,
		// Token: 0x0400225C RID: 8796
		SetCanonicalizationContext = 14,
		// Token: 0x0400225D RID: 8797
		StageComponent = 20,
		// Token: 0x0400225E RID: 8798
		PinDeployment,
		// Token: 0x0400225F RID: 8799
		UnpinDeployment,
		// Token: 0x04002260 RID: 8800
		StageComponentFile,
		// Token: 0x04002261 RID: 8801
		InstallDeployment,
		// Token: 0x04002262 RID: 8802
		UninstallDeployment,
		// Token: 0x04002263 RID: 8803
		SetDeploymentMetadata,
		// Token: 0x04002264 RID: 8804
		Scavenge
	}
}
