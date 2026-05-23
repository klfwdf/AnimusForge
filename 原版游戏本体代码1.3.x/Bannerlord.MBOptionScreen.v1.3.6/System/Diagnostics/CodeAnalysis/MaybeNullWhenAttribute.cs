using System;

namespace System.Diagnostics.CodeAnalysis
{
	// Token: 0x02000073 RID: 115
	[AttributeUsage(AttributeTargets.Parameter, Inherited = false)]
	[ExcludeFromCodeCoverage]
	[DebuggerNonUserCode]
	internal sealed class MaybeNullWhenAttribute : Attribute
	{
		// Token: 0x17000115 RID: 277
		// (get) Token: 0x06000499 RID: 1177 RVA: 0x0001142A File Offset: 0x0000F62A
		public bool ReturnValue { get; }

		// Token: 0x0600049A RID: 1178 RVA: 0x00011432 File Offset: 0x0000F632
		public MaybeNullWhenAttribute(bool returnValue)
		{
			this.ReturnValue = returnValue;
		}
	}
}
