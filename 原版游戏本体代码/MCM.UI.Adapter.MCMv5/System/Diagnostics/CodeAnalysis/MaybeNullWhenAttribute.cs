using System;

namespace System.Diagnostics.CodeAnalysis
{
	// Token: 0x0200001C RID: 28
	[AttributeUsage(AttributeTargets.Parameter, Inherited = false)]
	[ExcludeFromCodeCoverage]
	[DebuggerNonUserCode]
	internal sealed class MaybeNullWhenAttribute : Attribute
	{
		// Token: 0x17000008 RID: 8
		// (get) Token: 0x060000F9 RID: 249 RVA: 0x0000672D File Offset: 0x0000492D
		public bool ReturnValue { get; }

		// Token: 0x060000FA RID: 250 RVA: 0x00006735 File Offset: 0x00004935
		public MaybeNullWhenAttribute(bool returnValue)
		{
			this.ReturnValue = returnValue;
		}
	}
}
