using System;
using Microsoft.CodeAnalysis;

namespace System.Runtime.CompilerServices
{
	// Token: 0x02000007 RID: 7
	[CompilerGenerated]
	[Embedded]
	[AttributeUsage(AttributeTargets.Class | AttributeTargets.Property | AttributeTargets.Field | AttributeTargets.Event | AttributeTargets.Parameter | AttributeTargets.ReturnValue | AttributeTargets.GenericParameter, AllowMultiple = false, Inherited = false)]
	internal sealed class NativeIntegerAttribute : Attribute
	{
		// Token: 0x06000007 RID: 7 RVA: 0x0000209E File Offset: 0x0000029E
		public NativeIntegerAttribute()
		{
			this.TransformFlags = new bool[] { true };
		}

		// Token: 0x06000008 RID: 8 RVA: 0x000020B6 File Offset: 0x000002B6
		public NativeIntegerAttribute(bool[] A_1)
		{
			this.TransformFlags = A_1;
		}

		// Token: 0x04000003 RID: 3
		public readonly bool[] TransformFlags;
	}
}
