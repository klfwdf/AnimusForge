using System;

namespace System.Diagnostics.CodeAnalysis
{
	// Token: 0x020007F4 RID: 2036
	[AttributeUsage(AttributeTargets.Parameter, Inherited = false)]
	internal sealed class <b37590d4-39fb-478a-88de-d293f3364852>DoesNotReturnIfAttribute : Attribute
	{
		// Token: 0x06002700 RID: 9984 RVA: 0x0008743A File Offset: 0x0008563A
		public <b37590d4-39fb-478a-88de-d293f3364852>DoesNotReturnIfAttribute(bool parameterValue)
		{
			this.ParameterValue = parameterValue;
		}

		// Token: 0x17000803 RID: 2051
		// (get) Token: 0x06002701 RID: 9985 RVA: 0x00087449 File Offset: 0x00085649
		public bool ParameterValue { get; }
	}
}
