using System;
using System.Runtime.CompilerServices;

namespace System.Diagnostics.CodeAnalysis
{
	// Token: 0x02000613 RID: 1555
	[NullableContext(1)]
	[Nullable(0)]
	[AttributeUsage(AttributeTargets.Property | AttributeTargets.Parameter | AttributeTargets.ReturnValue, AllowMultiple = true, Inherited = false)]
	[ExcludeFromCodeCoverage]
	[DebuggerNonUserCode]
	internal sealed class <24b3ba8a-00b7-40fc-a603-2711fa115297>NotNullIfNotNullAttribute : Attribute
	{
		// Token: 0x1700075D RID: 1885
		// (get) Token: 0x060020E9 RID: 8425 RVA: 0x000684A0 File Offset: 0x000666A0
		public string ParameterName { get; }

		// Token: 0x060020EA RID: 8426 RVA: 0x000684A8 File Offset: 0x000666A8
		public <24b3ba8a-00b7-40fc-a603-2711fa115297>NotNullIfNotNullAttribute(string parameterName)
		{
			this.ParameterName = parameterName;
		}
	}
}
