using System;
using System.Runtime.CompilerServices;

namespace System.Diagnostics.CodeAnalysis
{
	// Token: 0x0200004E RID: 78
	[NullableContext(1)]
	[Nullable(0)]
	[AttributeUsage(AttributeTargets.Method | AttributeTargets.Property, Inherited = false, AllowMultiple = true)]
	[ExcludeFromCodeCoverage]
	[DebuggerNonUserCode]
	internal sealed class MemberNotNullWhenAttribute : Attribute
	{
		// Token: 0x1700003D RID: 61
		// (get) Token: 0x060002AF RID: 687 RVA: 0x0000CC0F File Offset: 0x0000AE0F
		public bool ReturnValue { get; }

		// Token: 0x1700003E RID: 62
		// (get) Token: 0x060002B0 RID: 688 RVA: 0x0000CC17 File Offset: 0x0000AE17
		public string[] Members { get; }

		// Token: 0x060002B1 RID: 689 RVA: 0x0000CC1F File Offset: 0x0000AE1F
		public MemberNotNullWhenAttribute(bool returnValue, string member)
		{
			this.ReturnValue = returnValue;
			this.Members = new string[] { member };
		}

		// Token: 0x060002B2 RID: 690 RVA: 0x0000CC3E File Offset: 0x0000AE3E
		public MemberNotNullWhenAttribute(bool returnValue, params string[] members)
		{
			this.ReturnValue = returnValue;
			this.Members = members;
		}
	}
}
