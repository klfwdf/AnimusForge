using System;

namespace System.Diagnostics.CodeAnalysis
{
	// Token: 0x02000071 RID: 113
	[AttributeUsage(AttributeTargets.Parameter, Inherited = false)]
	[ExcludeFromCodeCoverage]
	[DebuggerNonUserCode]
	internal sealed class DoesNotReturnIfAttribute : Attribute
	{
		// Token: 0x17000114 RID: 276
		// (get) Token: 0x06000496 RID: 1174 RVA: 0x0001140B File Offset: 0x0000F60B
		public bool ParameterValue { get; }

		// Token: 0x06000497 RID: 1175 RVA: 0x00011413 File Offset: 0x0000F613
		public DoesNotReturnIfAttribute(bool parameterValue)
		{
			this.ParameterValue = parameterValue;
		}
	}
}
