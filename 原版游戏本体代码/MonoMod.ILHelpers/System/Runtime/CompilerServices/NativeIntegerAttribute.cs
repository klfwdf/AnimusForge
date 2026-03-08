using System;
using Microsoft.CodeAnalysis;

namespace System.Runtime.CompilerServices
{
	// Token: 0x02000005 RID: 5
	[Embedded]
	[CompilerGenerated]
	[AttributeUsage(AttributeTargets.Class | AttributeTargets.Property | AttributeTargets.Field | AttributeTargets.Event | AttributeTargets.Parameter | AttributeTargets.ReturnValue | AttributeTargets.GenericParameter, AllowMultiple = false, Inherited = false)]
	internal sealed class NativeIntegerAttribute : Attribute
	{
		// Token: 0x06000004 RID: 4 RVA: 0x00002058 File Offset: 0x00000258
		public NativeIntegerAttribute()
		{
			this.TransformFlags = new bool[] { true };
		}

		// Token: 0x06000005 RID: 5 RVA: 0x00002070 File Offset: 0x00000270
		public NativeIntegerAttribute(bool[] A_0)
		{
			this.TransformFlags = A_0;
		}

		// Token: 0x04000001 RID: 1
		public readonly bool[] TransformFlags;
	}
}
