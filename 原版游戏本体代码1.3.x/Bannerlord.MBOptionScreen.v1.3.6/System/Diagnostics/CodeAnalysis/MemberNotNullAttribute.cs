using System;
using System.Runtime.CompilerServices;

namespace System.Diagnostics.CodeAnalysis
{
	// Token: 0x02000074 RID: 116
	[NullableContext(1)]
	[Nullable(0)]
	[AttributeUsage(AttributeTargets.Method | AttributeTargets.Property, Inherited = false, AllowMultiple = true)]
	[ExcludeFromCodeCoverage]
	[DebuggerNonUserCode]
	internal sealed class MemberNotNullAttribute : Attribute
	{
		// Token: 0x17000116 RID: 278
		// (get) Token: 0x0600049B RID: 1179 RVA: 0x00011441 File Offset: 0x0000F641
		public string[] Members { get; }

		// Token: 0x0600049C RID: 1180 RVA: 0x00011449 File Offset: 0x0000F649
		public MemberNotNullAttribute(string member)
		{
			this.Members = new string[] { member };
		}

		// Token: 0x0600049D RID: 1181 RVA: 0x00011461 File Offset: 0x0000F661
		public MemberNotNullAttribute(params string[] members)
		{
			this.Members = members;
		}
	}
}
