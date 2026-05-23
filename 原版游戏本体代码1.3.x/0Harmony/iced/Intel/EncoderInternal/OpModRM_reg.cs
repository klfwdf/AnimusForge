using System;
using System.Runtime.CompilerServices;

namespace Iced.Intel.EncoderInternal
{
	// Token: 0x0200068C RID: 1676
	internal sealed class OpModRM_reg : Op
	{
		// Token: 0x060023EB RID: 9195 RVA: 0x00073E20 File Offset: 0x00072020
		public OpModRM_reg(Register regLo, Register regHi)
		{
			this.regLo = regLo;
			this.regHi = regHi;
		}

		// Token: 0x060023EC RID: 9196 RVA: 0x00073E36 File Offset: 0x00072036
		[NullableContext(1)]
		public override void Encode(Encoder encoder, in Instruction instruction, int operand)
		{
			encoder.AddModRMRegister(instruction, operand, this.regLo, this.regHi);
		}

		// Token: 0x0400351F RID: 13599
		private readonly Register regLo;

		// Token: 0x04003520 RID: 13600
		private readonly Register regHi;
	}
}
