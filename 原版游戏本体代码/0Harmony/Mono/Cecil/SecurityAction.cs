using System;

namespace Mono.Cecil
{
	// Token: 0x02000294 RID: 660
	internal enum SecurityAction : ushort
	{
		// Token: 0x04000577 RID: 1399
		Request = 1,
		// Token: 0x04000578 RID: 1400
		Demand,
		// Token: 0x04000579 RID: 1401
		Assert,
		// Token: 0x0400057A RID: 1402
		Deny,
		// Token: 0x0400057B RID: 1403
		PermitOnly,
		// Token: 0x0400057C RID: 1404
		LinkDemand,
		// Token: 0x0400057D RID: 1405
		InheritDemand,
		// Token: 0x0400057E RID: 1406
		RequestMinimum,
		// Token: 0x0400057F RID: 1407
		RequestOptional,
		// Token: 0x04000580 RID: 1408
		RequestRefuse,
		// Token: 0x04000581 RID: 1409
		PreJitGrant,
		// Token: 0x04000582 RID: 1410
		PreJitDeny,
		// Token: 0x04000583 RID: 1411
		NonCasDemand,
		// Token: 0x04000584 RID: 1412
		NonCasLinkDemand,
		// Token: 0x04000585 RID: 1413
		NonCasInheritance
	}
}
