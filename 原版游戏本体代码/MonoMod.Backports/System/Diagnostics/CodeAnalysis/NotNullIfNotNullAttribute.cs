using System;
using System.Runtime.CompilerServices;

namespace System.Diagnostics.CodeAnalysis
{
	// Token: 0x02000050 RID: 80
	[NullableContext(1)]
	[Nullable(0)]
	[AttributeUsage(AttributeTargets.Property | AttributeTargets.Parameter | AttributeTargets.ReturnValue, AllowMultiple = true, Inherited = false)]
	[ExcludeFromCodeCoverage]
	[DebuggerNonUserCode]
	internal sealed class NotNullIfNotNullAttribute : Attribute
	{
		// Token: 0x1700003F RID: 63
		// (get) Token: 0x060002B4 RID: 692 RVA: 0x0000CC5C File Offset: 0x0000AE5C
		public string ParameterName { get; }

		// Token: 0x060002B5 RID: 693 RVA: 0x0000CC64 File Offset: 0x0000AE64
		public NotNullIfNotNullAttribute(string parameterName)
		{
			this.ParameterName = parameterName;
		}
	}
}
