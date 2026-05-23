using System;
using System.Runtime.CompilerServices;

namespace Iced.Intel.EncoderInternal
{
	// Token: 0x0200068B RID: 1675
	internal sealed class OpModRM_rm_reg_only : Op
	{
		// Token: 0x060023E9 RID: 9193 RVA: 0x00073DF2 File Offset: 0x00071FF2
		public OpModRM_rm_reg_only(Register regLo, Register regHi)
		{
			this.regLo = regLo;
			this.regHi = regHi;
		}

		// Token: 0x060023EA RID: 9194 RVA: 0x00073E08 File Offset: 0x00072008
		[NullableContext(1)]
		public override void Encode(Encoder encoder, in Instruction instruction, int operand)
		{
			encoder.AddRegOrMem(instruction, operand, this.regLo, this.regHi, false, true);
		}

		// Token: 0x0400351D RID: 13597
		private readonly Register regLo;

		// Token: 0x0400351E RID: 13598
		private readonly Register regHi;
	}
}
