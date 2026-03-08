using System;

namespace Mono.Cecil
{
	// Token: 0x02000244 RID: 580
	[Flags]
	internal enum GenericParameterAttributes : ushort
	{
		// Token: 0x040003EC RID: 1004
		VarianceMask = 3,
		// Token: 0x040003ED RID: 1005
		NonVariant = 0,
		// Token: 0x040003EE RID: 1006
		Covariant = 1,
		// Token: 0x040003EF RID: 1007
		Contravariant = 2,
		// Token: 0x040003F0 RID: 1008
		SpecialConstraintMask = 28,
		// Token: 0x040003F1 RID: 1009
		ReferenceTypeConstraint = 4,
		// Token: 0x040003F2 RID: 1010
		NotNullableValueTypeConstraint = 8,
		// Token: 0x040003F3 RID: 1011
		DefaultConstructorConstraint = 16,
		// Token: 0x040003F4 RID: 1012
		AllowByRefLikeConstraint = 32
	}
}
