using System;

namespace Iced.Intel
{
	// Token: 0x0200064C RID: 1612
	internal interface IVARegisterValueProvider
	{
		// Token: 0x06002301 RID: 8961
		ulong GetRegisterValue(Register register, int elementIndex, int elementSize);
	}
}
