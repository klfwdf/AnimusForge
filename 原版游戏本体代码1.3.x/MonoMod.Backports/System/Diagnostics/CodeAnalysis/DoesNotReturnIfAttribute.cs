using System;

namespace System.Diagnostics.CodeAnalysis
{
	// Token: 0x0200004A RID: 74
	[AttributeUsage(AttributeTargets.Parameter, Inherited = false)]
	[ExcludeFromCodeCoverage]
	[DebuggerNonUserCode]
	internal sealed class DoesNotReturnIfAttribute : Attribute
	{
		// Token: 0x1700003A RID: 58
		// (get) Token: 0x060002A7 RID: 679 RVA: 0x0000CBAA File Offset: 0x0000ADAA
		public bool ParameterValue { get; }

		// Token: 0x060002A8 RID: 680 RVA: 0x0000CBB2 File Offset: 0x0000ADB2
		public DoesNotReturnIfAttribute(bool parameterValue)
		{
			this.ParameterValue = parameterValue;
		}
	}
}
