using System;
using System.Runtime.CompilerServices;

namespace System.Diagnostics.CodeAnalysis
{
	// Token: 0x02000020 RID: 32
	[NullableContext(1)]
	[Nullable(0)]
	[AttributeUsage(AttributeTargets.Property | AttributeTargets.Parameter | AttributeTargets.ReturnValue, AllowMultiple = true, Inherited = false)]
	[ExcludeFromCodeCoverage]
	[DebuggerNonUserCode]
	internal sealed class NotNullIfNotNullAttribute : Attribute
	{
		// Token: 0x1700000C RID: 12
		// (get) Token: 0x06000103 RID: 259 RVA: 0x000067CC File Offset: 0x000049CC
		public string ParameterName { get; }

		// Token: 0x06000104 RID: 260 RVA: 0x000067D4 File Offset: 0x000049D4
		public NotNullIfNotNullAttribute(string parameterName)
		{
			this.ParameterName = parameterName;
		}
	}
}
