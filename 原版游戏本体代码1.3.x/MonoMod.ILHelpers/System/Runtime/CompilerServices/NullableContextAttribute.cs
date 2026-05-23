using System;
using Microsoft.CodeAnalysis;

namespace System.Runtime.CompilerServices
{
	// Token: 0x02000007 RID: 7
	[AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct | AttributeTargets.Method | AttributeTargets.Interface | AttributeTargets.Delegate, AllowMultiple = false, Inherited = false)]
	[CompilerGenerated]
	[Embedded]
	internal sealed class NullableContextAttribute : Attribute
	{
		// Token: 0x06000008 RID: 8 RVA: 0x000020A8 File Offset: 0x000002A8
		public NullableContextAttribute(byte A_0)
		{
			this.Flag = A_0;
		}

		// Token: 0x04000003 RID: 3
		public readonly byte Flag;
	}
}
