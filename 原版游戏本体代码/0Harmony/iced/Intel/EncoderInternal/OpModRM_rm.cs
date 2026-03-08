using System;
using System.Runtime.CompilerServices;

namespace Iced.Intel.EncoderInternal
{
	// Token: 0x02000689 RID: 1673
	internal sealed class OpModRM_rm : Op
	{
		// Token: 0x060023E5 RID: 9189 RVA: 0x00073D98 File Offset: 0x00071F98
		public OpModRM_rm(Register regLo, Register regHi)
		{
			this.regLo = regLo;
			this.regHi = regHi;
		}

		// Token: 0x060023E6 RID: 9190 RVA: 0x00073DAE File Offset: 0x00071FAE
		[NullableContext(1)]
		public override void Encode(Encoder encoder, in Instruction instruction, int operand)
		{
			encoder.AddRegOrMem(instruction, operand, this.regLo, this.regHi, true, true);
		}

		// Token: 0x04003519 RID: 13593
		private readonly Register regLo;

		// Token: 0x0400351A RID: 13594
		private readonly Register regHi;
	}
}
