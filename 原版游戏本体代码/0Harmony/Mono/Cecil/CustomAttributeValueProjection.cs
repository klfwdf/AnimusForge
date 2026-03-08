using System;

namespace Mono.Cecil
{
	// Token: 0x020002B7 RID: 695
	internal sealed class CustomAttributeValueProjection
	{
		// Token: 0x060011A7 RID: 4519 RVA: 0x00035254 File Offset: 0x00033454
		public CustomAttributeValueProjection(AttributeTargets targets, CustomAttributeValueTreatment treatment)
		{
			this.Targets = targets;
			this.Treatment = treatment;
		}

		// Token: 0x0400066F RID: 1647
		public readonly AttributeTargets Targets;

		// Token: 0x04000670 RID: 1648
		public readonly CustomAttributeValueTreatment Treatment;
	}
}
