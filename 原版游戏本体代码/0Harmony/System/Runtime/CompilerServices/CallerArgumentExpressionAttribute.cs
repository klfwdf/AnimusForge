using System;

namespace System.Runtime.CompilerServices
{
	// Token: 0x020004AE RID: 1198
	[NullableContext(1)]
	[Nullable(0)]
	[AttributeUsage(AttributeTargets.Parameter, AllowMultiple = false, Inherited = false)]
	internal sealed class CallerArgumentExpressionAttribute : Attribute
	{
		// Token: 0x06001AEC RID: 6892 RVA: 0x0005778F File Offset: 0x0005598F
		public CallerArgumentExpressionAttribute(string parameterName)
		{
			this.ParameterName = parameterName;
		}

		// Token: 0x170005DD RID: 1501
		// (get) Token: 0x06001AED RID: 6893 RVA: 0x0005779E File Offset: 0x0005599E
		public string ParameterName { get; }
	}
}
