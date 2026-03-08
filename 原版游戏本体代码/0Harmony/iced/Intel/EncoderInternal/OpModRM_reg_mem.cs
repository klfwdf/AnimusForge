using System;
using System.Runtime.CompilerServices;

namespace Iced.Intel.EncoderInternal
{
	// Token: 0x0200068D RID: 1677
	internal sealed class OpModRM_reg_mem : Op
	{
		// Token: 0x060023ED RID: 9197 RVA: 0x00073E4C File Offset: 0x0007204C
		public OpModRM_reg_mem(Register regLo, Register regHi)
		{
			this.regLo = regLo;
			this.regHi = regHi;
		}

		// Token: 0x060023EE RID: 9198 RVA: 0x00073E62 File Offset: 0x00072062
		[NullableContext(1)]
		public override void Encode(Encoder encoder, in Instruction instruction, int operand)
		{
			encoder.AddModRMRegister(instruction, operand, this.regLo, this.regHi);
			encoder.EncoderFlags |= EncoderFlags.RegIsMemory;
		}

		// Token: 0x04003521 RID: 13601
		private readonly Register regLo;

		// Token: 0x04003522 RID: 13602
		private readonly Register regHi;
	}
}
