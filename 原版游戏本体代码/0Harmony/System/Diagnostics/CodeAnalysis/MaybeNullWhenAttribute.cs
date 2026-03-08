using System;

namespace System.Diagnostics.CodeAnalysis
{
	// Token: 0x02000908 RID: 2312
	[AttributeUsage(AttributeTargets.Parameter, Inherited = false)]
	[ExcludeFromCodeCoverage]
	[DebuggerNonUserCode]
	internal sealed class MaybeNullWhenAttribute : Attribute
	{
		// Token: 0x1700086B RID: 2155
		// (get) Token: 0x0600308D RID: 12429 RVA: 0x000A754B File Offset: 0x000A574B
		public bool ReturnValue { get; }

		// Token: 0x0600308E RID: 12430 RVA: 0x000A7553 File Offset: 0x000A5753
		public MaybeNullWhenAttribute(bool returnValue)
		{
			this.ReturnValue = returnValue;
		}
	}
}
