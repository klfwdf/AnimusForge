using System;
using System.Runtime.CompilerServices;

namespace Iced.Intel
{
	// Token: 0x0200065F RID: 1631
	internal static class MnemonicUtils
	{
		// Token: 0x0600237D RID: 9085 RVA: 0x00072B61 File Offset: 0x00070D61
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static Mnemonic Mnemonic(this Code code)
		{
			return (Mnemonic)MnemonicUtilsData.toMnemonic[(int)code];
		}
	}
}
