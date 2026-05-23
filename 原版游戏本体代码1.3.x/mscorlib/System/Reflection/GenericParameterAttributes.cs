using System;

namespace System.Reflection
{
	// Token: 0x020005E9 RID: 1513
	[Flags]
	[__DynamicallyInvokable]
	public enum GenericParameterAttributes
	{
		// Token: 0x04001CC7 RID: 7367
		[__DynamicallyInvokable]
		None = 0,
		// Token: 0x04001CC8 RID: 7368
		[__DynamicallyInvokable]
		VarianceMask = 3,
		// Token: 0x04001CC9 RID: 7369
		[__DynamicallyInvokable]
		Covariant = 1,
		// Token: 0x04001CCA RID: 7370
		[__DynamicallyInvokable]
		Contravariant = 2,
		// Token: 0x04001CCB RID: 7371
		[__DynamicallyInvokable]
		SpecialConstraintMask = 28,
		// Token: 0x04001CCC RID: 7372
		[__DynamicallyInvokable]
		ReferenceTypeConstraint = 4,
		// Token: 0x04001CCD RID: 7373
		[__DynamicallyInvokable]
		NotNullableValueTypeConstraint = 8,
		// Token: 0x04001CCE RID: 7374
		[__DynamicallyInvokable]
		DefaultConstructorConstraint = 16
	}
}
