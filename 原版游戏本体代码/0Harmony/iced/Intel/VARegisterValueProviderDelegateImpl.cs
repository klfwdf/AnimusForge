using System;
using System.Runtime.CompilerServices;

namespace Iced.Intel
{
	// Token: 0x0200064F RID: 1615
	internal sealed class VARegisterValueProviderDelegateImpl : IVATryGetRegisterValueProvider
	{
		// Token: 0x06002307 RID: 8967 RVA: 0x00071A12 File Offset: 0x0006FC12
		[NullableContext(1)]
		public VARegisterValueProviderDelegateImpl(VAGetRegisterValue getRegisterValue)
		{
			if (getRegisterValue == null)
			{
				throw new ArgumentNullException("getRegisterValue");
			}
			this.getRegisterValue = getRegisterValue;
		}

		// Token: 0x06002308 RID: 8968 RVA: 0x00071A30 File Offset: 0x0006FC30
		public bool TryGetRegisterValue(Register register, int elementIndex, int elementSize, out ulong value)
		{
			value = this.getRegisterValue(register, elementIndex, elementSize);
			return true;
		}

		// Token: 0x04002AED RID: 10989
		private readonly VAGetRegisterValue getRegisterValue;
	}
}
