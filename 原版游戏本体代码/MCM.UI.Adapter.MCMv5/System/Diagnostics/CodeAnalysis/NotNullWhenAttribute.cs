using System;

namespace System.Diagnostics.CodeAnalysis
{
	// Token: 0x02000021 RID: 33
	[AttributeUsage(AttributeTargets.Parameter, Inherited = false)]
	[ExcludeFromCodeCoverage]
	[DebuggerNonUserCode]
	internal sealed class NotNullWhenAttribute : Attribute
	{
		// Token: 0x1700000D RID: 13
		// (get) Token: 0x06000105 RID: 261 RVA: 0x000067E5 File Offset: 0x000049E5
		public bool ReturnValue { get; }

		// Token: 0x06000106 RID: 262 RVA: 0x000067ED File Offset: 0x000049ED
		public NotNullWhenAttribute(bool returnValue)
		{
			this.ReturnValue = returnValue;
		}
	}
}
