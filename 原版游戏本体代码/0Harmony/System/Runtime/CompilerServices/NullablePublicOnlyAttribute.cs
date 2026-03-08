using System;
using Microsoft.CodeAnalysis;

namespace System.Runtime.CompilerServices
{
	// Token: 0x02000625 RID: 1573
	[CompilerGenerated]
	[Embedded]
	[AttributeUsage(AttributeTargets.Module, AllowMultiple = false, Inherited = false)]
	internal sealed class NullablePublicOnlyAttribute : Attribute
	{
		// Token: 0x060020ED RID: 8429 RVA: 0x000684CE File Offset: 0x000666CE
		public NullablePublicOnlyAttribute(bool A_1)
		{
			this.IncludesInternals = A_1;
		}

		// Token: 0x04001677 RID: 5751
		public readonly bool IncludesInternals;
	}
}
