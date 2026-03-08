using System;

namespace System.Diagnostics.CodeAnalysis
{
	// Token: 0x02000078 RID: 120
	[AttributeUsage(AttributeTargets.Parameter, Inherited = false)]
	[ExcludeFromCodeCoverage]
	[DebuggerNonUserCode]
	internal sealed class NotNullWhenAttribute : Attribute
	{
		// Token: 0x1700011A RID: 282
		// (get) Token: 0x060004A5 RID: 1189 RVA: 0x000114D4 File Offset: 0x0000F6D4
		public bool ReturnValue { get; }

		// Token: 0x060004A6 RID: 1190 RVA: 0x000114DC File Offset: 0x0000F6DC
		public NotNullWhenAttribute(bool returnValue)
		{
			this.ReturnValue = returnValue;
		}
	}
}
