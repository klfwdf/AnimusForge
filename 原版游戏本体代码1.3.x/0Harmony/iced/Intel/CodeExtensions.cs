using System;

namespace Iced.Intel
{
	// Token: 0x02000633 RID: 1587
	internal static class CodeExtensions
	{
		// Token: 0x0600210C RID: 8460 RVA: 0x00068D01 File Offset: 0x00066F01
		internal static bool IgnoresSegment(this Code code)
		{
			return code - Code.Lea_r16_m <= 2 || code - Code.Bndcl_bnd_rm32 <= 3 || code - Code.Bndmk_bnd_m32 <= 3;
		}

		// Token: 0x0600210D RID: 8461 RVA: 0x00068D24 File Offset: 0x00066F24
		internal static bool IgnoresIndex(this Code code)
		{
			return code == Code.Bndldx_bnd_mib || code == Code.Bndstx_mib_bnd;
		}

		// Token: 0x0600210E RID: 8462 RVA: 0x00068D39 File Offset: 0x00066F39
		internal static bool IsTileStrideIndex(this Code code)
		{
			return code - Code.VEX_Tileloaddt1_tmm_sibmem <= 2;
		}
	}
}
