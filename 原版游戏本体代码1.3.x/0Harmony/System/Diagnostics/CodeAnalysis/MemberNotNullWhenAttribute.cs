using System;
using System.Runtime.CompilerServices;

namespace System.Diagnostics.CodeAnalysis
{
	// Token: 0x0200090A RID: 2314
	[NullableContext(1)]
	[Nullable(0)]
	[AttributeUsage(AttributeTargets.Method | AttributeTargets.Property, Inherited = false, AllowMultiple = true)]
	[ExcludeFromCodeCoverage]
	[DebuggerNonUserCode]
	internal sealed class MemberNotNullWhenAttribute : Attribute
	{
		// Token: 0x1700086D RID: 2157
		// (get) Token: 0x06003092 RID: 12434 RVA: 0x000A7591 File Offset: 0x000A5791
		public bool ReturnValue { get; }

		// Token: 0x1700086E RID: 2158
		// (get) Token: 0x06003093 RID: 12435 RVA: 0x000A7599 File Offset: 0x000A5799
		public string[] Members { get; }

		// Token: 0x06003094 RID: 12436 RVA: 0x000A75A1 File Offset: 0x000A57A1
		public MemberNotNullWhenAttribute(bool returnValue, string member)
		{
			this.ReturnValue = returnValue;
			this.Members = new string[] { member };
		}

		// Token: 0x06003095 RID: 12437 RVA: 0x000A75C0 File Offset: 0x000A57C0
		public MemberNotNullWhenAttribute(bool returnValue, params string[] members)
		{
			this.ReturnValue = returnValue;
			this.Members = members;
		}
	}
}
