using System;

namespace Iced.Intel
{
	// Token: 0x0200064E RID: 1614
	internal interface IVATryGetRegisterValueProvider
	{
		// Token: 0x06002306 RID: 8966
		bool TryGetRegisterValue(Register register, int elementIndex, int elementSize, out ulong value);
	}
}
