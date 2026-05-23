using System;

namespace System.Diagnostics.CodeAnalysis
{
	// Token: 0x02000051 RID: 81
	[AttributeUsage(AttributeTargets.Parameter, Inherited = false)]
	[ExcludeFromCodeCoverage]
	[DebuggerNonUserCode]
	internal sealed class NotNullWhenAttribute : Attribute
	{
		// Token: 0x17000040 RID: 64
		// (get) Token: 0x060002B6 RID: 694 RVA: 0x0000CC73 File Offset: 0x0000AE73
		public bool ReturnValue { get; }

		// Token: 0x060002B7 RID: 695 RVA: 0x0000CC7B File Offset: 0x0000AE7B
		public NotNullWhenAttribute(bool returnValue)
		{
			this.ReturnValue = returnValue;
		}
	}
}
