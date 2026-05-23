using System;

namespace System.Diagnostics.CodeAnalysis
{
	// Token: 0x0200060D RID: 1549
	[AttributeUsage(AttributeTargets.Parameter, Inherited = false)]
	[ExcludeFromCodeCoverage]
	[DebuggerNonUserCode]
	internal sealed class <24b3ba8a-00b7-40fc-a603-2711fa115297>DoesNotReturnIfAttribute : Attribute
	{
		// Token: 0x17000758 RID: 1880
		// (get) Token: 0x060020DC RID: 8412 RVA: 0x000683FE File Offset: 0x000665FE
		public bool ParameterValue { get; }

		// Token: 0x060020DD RID: 8413 RVA: 0x00068406 File Offset: 0x00066606
		public <24b3ba8a-00b7-40fc-a603-2711fa115297>DoesNotReturnIfAttribute(bool parameterValue)
		{
			this.ParameterValue = parameterValue;
		}
	}
}
