using System;
using Microsoft.CodeAnalysis;

namespace System.Runtime.CompilerServices
{
	// Token: 0x02000006 RID: 6
	[AttributeUsage(AttributeTargets.Class | AttributeTargets.Property | AttributeTargets.Field | AttributeTargets.Event | AttributeTargets.Parameter | AttributeTargets.ReturnValue | AttributeTargets.GenericParameter, AllowMultiple = false, Inherited = false)]
	[Embedded]
	[CompilerGenerated]
	internal sealed class NullableAttribute : Attribute
	{
		// Token: 0x06000006 RID: 6 RVA: 0x0000207F File Offset: 0x0000027F
		public NullableAttribute(byte A_0)
		{
			this.NullableFlags = new byte[] { A_0 };
		}

		// Token: 0x06000007 RID: 7 RVA: 0x00002098 File Offset: 0x00000298
		public NullableAttribute(byte[] A_0)
		{
			this.NullableFlags = A_0;
		}

		// Token: 0x04000002 RID: 2
		public readonly byte[] NullableFlags;
	}
}
