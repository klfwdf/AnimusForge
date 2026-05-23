using System;
using System.Runtime.CompilerServices;

namespace System.Diagnostics.CodeAnalysis
{
	// Token: 0x0200004D RID: 77
	[NullableContext(1)]
	[Nullable(0)]
	[AttributeUsage(AttributeTargets.Method | AttributeTargets.Property, Inherited = false, AllowMultiple = true)]
	[ExcludeFromCodeCoverage]
	[DebuggerNonUserCode]
	internal sealed class MemberNotNullAttribute : Attribute
	{
		// Token: 0x1700003C RID: 60
		// (get) Token: 0x060002AC RID: 684 RVA: 0x0000CBE0 File Offset: 0x0000ADE0
		public string[] Members { get; }

		// Token: 0x060002AD RID: 685 RVA: 0x0000CBE8 File Offset: 0x0000ADE8
		public MemberNotNullAttribute(string member)
		{
			this.Members = new string[] { member };
		}

		// Token: 0x060002AE RID: 686 RVA: 0x0000CC00 File Offset: 0x0000AE00
		public MemberNotNullAttribute(params string[] members)
		{
			this.Members = members;
		}
	}
}
