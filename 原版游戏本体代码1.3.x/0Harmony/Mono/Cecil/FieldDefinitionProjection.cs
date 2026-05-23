using System;

namespace Mono.Cecil
{
	// Token: 0x020002B6 RID: 694
	internal sealed class FieldDefinitionProjection
	{
		// Token: 0x060011A6 RID: 4518 RVA: 0x00035239 File Offset: 0x00033439
		public FieldDefinitionProjection(FieldDefinition field, FieldDefinitionTreatment treatment)
		{
			this.Attributes = field.Attributes;
			this.Treatment = treatment;
		}

		// Token: 0x0400066D RID: 1645
		public readonly FieldAttributes Attributes;

		// Token: 0x0400066E RID: 1646
		public readonly FieldDefinitionTreatment Treatment;
	}
}
