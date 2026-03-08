using System;
using System.Runtime.CompilerServices;

namespace Iced.Intel
{
	// Token: 0x02000650 RID: 1616
	internal sealed class VARegisterValueProviderAdapter : IVATryGetRegisterValueProvider
	{
		// Token: 0x06002309 RID: 8969 RVA: 0x00071A44 File Offset: 0x0006FC44
		[NullableContext(1)]
		public VARegisterValueProviderAdapter(IVARegisterValueProvider provider)
		{
			this.provider = provider;
		}

		// Token: 0x0600230A RID: 8970 RVA: 0x00071A53 File Offset: 0x0006FC53
		public bool TryGetRegisterValue(Register register, int elementIndex, int elementSize, out ulong value)
		{
			value = this.provider.GetRegisterValue(register, elementIndex, elementSize);
			return true;
		}

		// Token: 0x04002AEE RID: 10990
		private readonly IVARegisterValueProvider provider;
	}
}
