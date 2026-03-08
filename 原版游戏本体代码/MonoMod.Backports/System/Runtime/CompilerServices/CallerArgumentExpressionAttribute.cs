using System;

namespace System.Runtime.CompilerServices
{
	// Token: 0x0200003D RID: 61
	[NullableContext(1)]
	[Nullable(0)]
	[AttributeUsage(AttributeTargets.Parameter, AllowMultiple = false, Inherited = false)]
	public sealed class CallerArgumentExpressionAttribute : Attribute
	{
		// Token: 0x0600026F RID: 623 RVA: 0x0000C066 File Offset: 0x0000A266
		public CallerArgumentExpressionAttribute(string parameterName)
		{
			this.ParameterName = parameterName;
		}

		// Token: 0x17000034 RID: 52
		// (get) Token: 0x06000270 RID: 624 RVA: 0x0000C075 File Offset: 0x0000A275
		public string ParameterName { get; }
	}
}
