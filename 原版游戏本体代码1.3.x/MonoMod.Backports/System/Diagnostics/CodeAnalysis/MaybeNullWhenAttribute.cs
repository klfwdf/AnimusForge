using System;

namespace System.Diagnostics.CodeAnalysis
{
	// Token: 0x0200004C RID: 76
	[AttributeUsage(AttributeTargets.Parameter, Inherited = false)]
	[ExcludeFromCodeCoverage]
	[DebuggerNonUserCode]
	internal sealed class MaybeNullWhenAttribute : Attribute
	{
		// Token: 0x1700003B RID: 59
		// (get) Token: 0x060002AA RID: 682 RVA: 0x0000CBC9 File Offset: 0x0000ADC9
		public bool ReturnValue { get; }

		// Token: 0x060002AB RID: 683 RVA: 0x0000CBD1 File Offset: 0x0000ADD1
		public MaybeNullWhenAttribute(bool returnValue)
		{
			this.ReturnValue = returnValue;
		}
	}
}
