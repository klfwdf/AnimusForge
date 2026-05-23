using System;

namespace Iced.Intel.EncoderInternal
{
	// Token: 0x02000682 RID: 1666
	[Flags]
	internal enum EncFlags2 : uint
	{
		// Token: 0x040034DD RID: 13533
		None = 0U,
		// Token: 0x040034DE RID: 13534
		OpCodeShift = 0U,
		// Token: 0x040034DF RID: 13535
		OpCodeIs2Bytes = 65536U,
		// Token: 0x040034E0 RID: 13536
		TableShift = 17U,
		// Token: 0x040034E1 RID: 13537
		TableMask = 7U,
		// Token: 0x040034E2 RID: 13538
		MandatoryPrefixShift = 20U,
		// Token: 0x040034E3 RID: 13539
		MandatoryPrefixMask = 3U,
		// Token: 0x040034E4 RID: 13540
		WBitShift = 22U,
		// Token: 0x040034E5 RID: 13541
		WBitMask = 3U,
		// Token: 0x040034E6 RID: 13542
		LBitShift = 24U,
		// Token: 0x040034E7 RID: 13543
		LBitMask = 7U,
		// Token: 0x040034E8 RID: 13544
		GroupIndexShift = 27U,
		// Token: 0x040034E9 RID: 13545
		GroupIndexMask = 7U,
		// Token: 0x040034EA RID: 13546
		HasMandatoryPrefix = 1073741824U,
		// Token: 0x040034EB RID: 13547
		HasGroupIndex = 2147483648U
	}
}
