using System;
using System.Runtime.CompilerServices;

namespace System.Diagnostics.CodeAnalysis
{
	// Token: 0x0200090C RID: 2316
	[NullableContext(1)]
	[Nullable(0)]
	[AttributeUsage(AttributeTargets.Property | AttributeTargets.Parameter | AttributeTargets.ReturnValue, AllowMultiple = true, Inherited = false)]
	[ExcludeFromCodeCoverage]
	[DebuggerNonUserCode]
	internal sealed class NotNullIfNotNullAttribute : Attribute
	{
		// Token: 0x1700086F RID: 2159
		// (get) Token: 0x06003097 RID: 12439 RVA: 0x000A75D6 File Offset: 0x000A57D6
		public string ParameterName { get; }

		// Token: 0x06003098 RID: 12440 RVA: 0x000A75DE File Offset: 0x000A57DE
		public NotNullIfNotNullAttribute(string parameterName)
		{
			this.ParameterName = parameterName;
		}
	}
}
