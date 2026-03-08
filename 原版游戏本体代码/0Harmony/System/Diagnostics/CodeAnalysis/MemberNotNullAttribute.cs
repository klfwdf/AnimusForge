using System;
using System.Runtime.CompilerServices;

namespace System.Diagnostics.CodeAnalysis
{
	// Token: 0x02000909 RID: 2313
	[NullableContext(1)]
	[Nullable(0)]
	[AttributeUsage(AttributeTargets.Method | AttributeTargets.Property, Inherited = false, AllowMultiple = true)]
	[ExcludeFromCodeCoverage]
	[DebuggerNonUserCode]
	internal sealed class MemberNotNullAttribute : Attribute
	{
		// Token: 0x1700086C RID: 2156
		// (get) Token: 0x0600308F RID: 12431 RVA: 0x000A7562 File Offset: 0x000A5762
		public string[] Members { get; }

		// Token: 0x06003090 RID: 12432 RVA: 0x000A756A File Offset: 0x000A576A
		public MemberNotNullAttribute(string member)
		{
			this.Members = new string[] { member };
		}

		// Token: 0x06003091 RID: 12433 RVA: 0x000A7582 File Offset: 0x000A5782
		public MemberNotNullAttribute(params string[] members)
		{
			this.Members = members;
		}
	}
}
