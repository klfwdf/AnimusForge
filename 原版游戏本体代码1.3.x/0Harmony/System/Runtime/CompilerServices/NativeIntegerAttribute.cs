using System;
using Microsoft.CodeAnalysis;

namespace System.Runtime.CompilerServices
{
	// Token: 0x02000462 RID: 1122
	[CompilerGenerated]
	[Embedded]
	[AttributeUsage(AttributeTargets.Class | AttributeTargets.Property | AttributeTargets.Field | AttributeTargets.Event | AttributeTargets.Parameter | AttributeTargets.ReturnValue | AttributeTargets.GenericParameter, AllowMultiple = false, Inherited = false)]
	internal sealed class NativeIntegerAttribute : Attribute
	{
		// Token: 0x0600184E RID: 6222 RVA: 0x0004D03E File Offset: 0x0004B23E
		public NativeIntegerAttribute()
		{
			this.TransformFlags = new bool[] { true };
		}

		// Token: 0x0600184F RID: 6223 RVA: 0x0004D056 File Offset: 0x0004B256
		public NativeIntegerAttribute(bool[] A_1)
		{
			this.TransformFlags = A_1;
		}

		// Token: 0x04001073 RID: 4211
		public readonly bool[] TransformFlags;
	}
}
