using System;

namespace Iced.Intel
{
	// Token: 0x02000640 RID: 1600
	[Flags]
	internal enum DecoderOptions : uint
	{
		// Token: 0x04002A45 RID: 10821
		None = 0U,
		// Token: 0x04002A46 RID: 10822
		NoInvalidCheck = 1U,
		// Token: 0x04002A47 RID: 10823
		AMD = 2U,
		// Token: 0x04002A48 RID: 10824
		ForceReservedNop = 4U,
		// Token: 0x04002A49 RID: 10825
		Umov = 8U,
		// Token: 0x04002A4A RID: 10826
		Xbts = 16U,
		// Token: 0x04002A4B RID: 10827
		Cmpxchg486A = 32U,
		// Token: 0x04002A4C RID: 10828
		OldFpu = 64U,
		// Token: 0x04002A4D RID: 10829
		Pcommit = 128U,
		// Token: 0x04002A4E RID: 10830
		Loadall286 = 256U,
		// Token: 0x04002A4F RID: 10831
		Loadall386 = 512U,
		// Token: 0x04002A50 RID: 10832
		Cl1invmb = 1024U,
		// Token: 0x04002A51 RID: 10833
		MovTr = 2048U,
		// Token: 0x04002A52 RID: 10834
		Jmpe = 4096U,
		// Token: 0x04002A53 RID: 10835
		NoPause = 8192U,
		// Token: 0x04002A54 RID: 10836
		NoWbnoinvd = 16384U,
		// Token: 0x04002A55 RID: 10837
		Udbg = 32768U,
		// Token: 0x04002A56 RID: 10838
		NoMPFX_0FBC = 65536U,
		// Token: 0x04002A57 RID: 10839
		NoMPFX_0FBD = 131072U,
		// Token: 0x04002A58 RID: 10840
		NoLahfSahf64 = 262144U,
		// Token: 0x04002A59 RID: 10841
		MPX = 524288U,
		// Token: 0x04002A5A RID: 10842
		Cyrix = 1048576U,
		// Token: 0x04002A5B RID: 10843
		Cyrix_SMINT_0F7E = 2097152U,
		// Token: 0x04002A5C RID: 10844
		Cyrix_DMI = 4194304U,
		// Token: 0x04002A5D RID: 10845
		ALTINST = 8388608U,
		// Token: 0x04002A5E RID: 10846
		KNC = 16777216U
	}
}
