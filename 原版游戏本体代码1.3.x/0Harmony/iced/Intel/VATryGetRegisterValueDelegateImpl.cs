using System;
using System.Runtime.CompilerServices;

namespace Iced.Intel
{
	// Token: 0x02000651 RID: 1617
	internal sealed class VATryGetRegisterValueDelegateImpl : IVATryGetRegisterValueProvider
	{
		// Token: 0x0600230B RID: 8971 RVA: 0x00071A67 File Offset: 0x0006FC67
		[NullableContext(1)]
		public VATryGetRegisterValueDelegateImpl(VATryGetRegisterValue getRegisterValue)
		{
			this.getRegisterValue = getRegisterValue;
		}

		// Token: 0x0600230C RID: 8972 RVA: 0x00071A76 File Offset: 0x0006FC76
		public bool TryGetRegisterValue(Register register, int elementIndex, int elementSize, out ulong value)
		{
			return this.getRegisterValue(register, elementIndex, elementSize, out value);
		}

		// Token: 0x04002AEF RID: 10991
		private readonly VATryGetRegisterValue getRegisterValue;
	}
}
