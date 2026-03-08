using System;
using System.Runtime.CompilerServices;

namespace System.Diagnostics.CodeAnalysis
{
	// Token: 0x0200001D RID: 29
	[NullableContext(1)]
	[Nullable(0)]
	[AttributeUsage(AttributeTargets.Method | AttributeTargets.Property, Inherited = false, AllowMultiple = true)]
	[ExcludeFromCodeCoverage]
	[DebuggerNonUserCode]
	internal sealed class MemberNotNullAttribute : Attribute
	{
		// Token: 0x17000009 RID: 9
		// (get) Token: 0x060000FB RID: 251 RVA: 0x00006746 File Offset: 0x00004946
		public string[] Members { get; }

		// Token: 0x060000FC RID: 252 RVA: 0x0000674E File Offset: 0x0000494E
		public MemberNotNullAttribute(string member)
		{
			this.Members = new string[] { member };
		}

		// Token: 0x060000FD RID: 253 RVA: 0x00006768 File Offset: 0x00004968
		public MemberNotNullAttribute(params string[] members)
		{
			this.Members = members;
		}
	}
}
