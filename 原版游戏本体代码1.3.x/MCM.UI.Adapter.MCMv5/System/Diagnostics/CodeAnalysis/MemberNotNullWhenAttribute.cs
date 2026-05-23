using System;
using System.Runtime.CompilerServices;

namespace System.Diagnostics.CodeAnalysis
{
	// Token: 0x0200001E RID: 30
	[NullableContext(1)]
	[Nullable(0)]
	[AttributeUsage(AttributeTargets.Method | AttributeTargets.Property, Inherited = false, AllowMultiple = true)]
	[ExcludeFromCodeCoverage]
	[DebuggerNonUserCode]
	internal sealed class MemberNotNullWhenAttribute : Attribute
	{
		// Token: 0x1700000A RID: 10
		// (get) Token: 0x060000FE RID: 254 RVA: 0x00006779 File Offset: 0x00004979
		public bool ReturnValue { get; }

		// Token: 0x1700000B RID: 11
		// (get) Token: 0x060000FF RID: 255 RVA: 0x00006781 File Offset: 0x00004981
		public string[] Members { get; }

		// Token: 0x06000100 RID: 256 RVA: 0x00006789 File Offset: 0x00004989
		public MemberNotNullWhenAttribute(bool returnValue, string member)
		{
			this.ReturnValue = returnValue;
			this.Members = new string[] { member };
		}

		// Token: 0x06000101 RID: 257 RVA: 0x000067AA File Offset: 0x000049AA
		public MemberNotNullWhenAttribute(bool returnValue, params string[] members)
		{
			this.ReturnValue = returnValue;
			this.Members = members;
		}
	}
}
