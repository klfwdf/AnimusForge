using System;

namespace System.Diagnostics.CodeAnalysis
{
	// Token: 0x02000906 RID: 2310
	[AttributeUsage(AttributeTargets.Parameter, Inherited = false)]
	[ExcludeFromCodeCoverage]
	[DebuggerNonUserCode]
	internal sealed class DoesNotReturnIfAttribute : Attribute
	{
		// Token: 0x1700086A RID: 2154
		// (get) Token: 0x0600308A RID: 12426 RVA: 0x000A7534 File Offset: 0x000A5734
		public bool ParameterValue { get; }

		// Token: 0x0600308B RID: 12427 RVA: 0x000A753C File Offset: 0x000A573C
		public DoesNotReturnIfAttribute(bool parameterValue)
		{
			this.ParameterValue = parameterValue;
		}
	}
}
