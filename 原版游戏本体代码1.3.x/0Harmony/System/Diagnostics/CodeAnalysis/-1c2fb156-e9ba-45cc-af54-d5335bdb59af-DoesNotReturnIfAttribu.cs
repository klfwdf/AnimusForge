using System;

namespace System.Diagnostics.CodeAnalysis
{
	// Token: 0x020004BF RID: 1215
	[AttributeUsage(AttributeTargets.Parameter, Inherited = false)]
	[ExcludeFromCodeCoverage]
	[DebuggerNonUserCode]
	internal sealed class <1c2fb156-e9ba-45cc-af54-d5335bdb59af>DoesNotReturnIfAttribute : Attribute
	{
		// Token: 0x170005E5 RID: 1509
		// (get) Token: 0x06001B32 RID: 6962 RVA: 0x0005843E File Offset: 0x0005663E
		public bool ParameterValue { get; }

		// Token: 0x06001B33 RID: 6963 RVA: 0x00058446 File Offset: 0x00056646
		public <1c2fb156-e9ba-45cc-af54-d5335bdb59af>DoesNotReturnIfAttribute(bool parameterValue)
		{
			this.ParameterValue = parameterValue;
		}
	}
}
