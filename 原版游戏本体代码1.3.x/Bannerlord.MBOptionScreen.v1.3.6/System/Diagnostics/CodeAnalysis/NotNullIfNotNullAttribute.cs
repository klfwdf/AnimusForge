using System;
using System.Runtime.CompilerServices;

namespace System.Diagnostics.CodeAnalysis
{
	// Token: 0x02000077 RID: 119
	[NullableContext(1)]
	[Nullable(0)]
	[AttributeUsage(AttributeTargets.Property | AttributeTargets.Parameter | AttributeTargets.ReturnValue, AllowMultiple = true, Inherited = false)]
	[ExcludeFromCodeCoverage]
	[DebuggerNonUserCode]
	internal sealed class NotNullIfNotNullAttribute : Attribute
	{
		// Token: 0x17000119 RID: 281
		// (get) Token: 0x060004A3 RID: 1187 RVA: 0x000114BD File Offset: 0x0000F6BD
		public string ParameterName { get; }

		// Token: 0x060004A4 RID: 1188 RVA: 0x000114C5 File Offset: 0x0000F6C5
		public NotNullIfNotNullAttribute(string parameterName)
		{
			this.ParameterName = parameterName;
		}
	}
}
