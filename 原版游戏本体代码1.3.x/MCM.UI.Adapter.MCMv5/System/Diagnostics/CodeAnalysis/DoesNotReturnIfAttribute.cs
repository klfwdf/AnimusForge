using System;

namespace System.Diagnostics.CodeAnalysis
{
	// Token: 0x0200001A RID: 26
	[AttributeUsage(AttributeTargets.Parameter, Inherited = false)]
	[ExcludeFromCodeCoverage]
	[DebuggerNonUserCode]
	internal sealed class DoesNotReturnIfAttribute : Attribute
	{
		// Token: 0x17000007 RID: 7
		// (get) Token: 0x060000F6 RID: 246 RVA: 0x0000670A File Offset: 0x0000490A
		public bool ParameterValue { get; }

		// Token: 0x060000F7 RID: 247 RVA: 0x00006712 File Offset: 0x00004912
		public DoesNotReturnIfAttribute(bool parameterValue)
		{
			this.ParameterValue = parameterValue;
		}
	}
}
