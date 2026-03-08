using System;

namespace Mono.Cecil
{
	// Token: 0x020002B5 RID: 693
	internal sealed class MethodDefinitionProjection
	{
		// Token: 0x060011A5 RID: 4517 RVA: 0x00035206 File Offset: 0x00033406
		public MethodDefinitionProjection(MethodDefinition method, MethodDefinitionTreatment treatment)
		{
			this.Attributes = method.Attributes;
			this.ImplAttributes = method.ImplAttributes;
			this.Name = method.Name;
			this.Treatment = treatment;
		}

		// Token: 0x04000669 RID: 1641
		public readonly MethodAttributes Attributes;

		// Token: 0x0400066A RID: 1642
		public readonly MethodImplAttributes ImplAttributes;

		// Token: 0x0400066B RID: 1643
		public readonly string Name;

		// Token: 0x0400066C RID: 1644
		public readonly MethodDefinitionTreatment Treatment;
	}
}
