using System;

namespace System.Diagnostics.CodeAnalysis
{
	// Token: 0x0200090D RID: 2317
	[AttributeUsage(AttributeTargets.Parameter, Inherited = false)]
	[ExcludeFromCodeCoverage]
	[DebuggerNonUserCode]
	internal sealed class NotNullWhenAttribute : Attribute
	{
		// Token: 0x17000870 RID: 2160
		// (get) Token: 0x06003099 RID: 12441 RVA: 0x000A75ED File Offset: 0x000A57ED
		public bool ReturnValue { get; }

		// Token: 0x0600309A RID: 12442 RVA: 0x000A75F5 File Offset: 0x000A57F5
		public NotNullWhenAttribute(bool returnValue)
		{
			this.ReturnValue = returnValue;
		}
	}
}
