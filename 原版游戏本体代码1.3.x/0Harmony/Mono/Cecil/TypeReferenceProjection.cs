using System;

namespace Mono.Cecil
{
	// Token: 0x020002B4 RID: 692
	internal sealed class TypeReferenceProjection
	{
		// Token: 0x060011A4 RID: 4516 RVA: 0x000351D3 File Offset: 0x000333D3
		public TypeReferenceProjection(TypeReference type, TypeReferenceTreatment treatment)
		{
			this.Name = type.Name;
			this.Namespace = type.Namespace;
			this.Scope = type.Scope;
			this.Treatment = treatment;
		}

		// Token: 0x04000665 RID: 1637
		public readonly string Name;

		// Token: 0x04000666 RID: 1638
		public readonly string Namespace;

		// Token: 0x04000667 RID: 1639
		public readonly IMetadataScope Scope;

		// Token: 0x04000668 RID: 1640
		public readonly TypeReferenceTreatment Treatment;
	}
}
