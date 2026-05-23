using System;
using System.Runtime.CompilerServices;

namespace Iced.Intel.EncoderInternal
{
	// Token: 0x0200068E RID: 1678
	internal sealed class OpModRM_regF0 : Op
	{
		// Token: 0x060023EF RID: 9199 RVA: 0x00073E8A File Offset: 0x0007208A
		public OpModRM_regF0(Register regLo, Register regHi)
		{
			this.regLo = regLo;
			this.regHi = regHi;
		}

		// Token: 0x060023F0 RID: 9200 RVA: 0x00073EA0 File Offset: 0x000720A0
		[NullableContext(1)]
		public override void Encode(Encoder encoder, in Instruction instruction, int operand)
		{
			if (encoder.Bitness != 64 && instruction.GetOpKind(operand) == OpKind.Register && instruction.GetOpRegister(operand) >= this.regLo + 8 && instruction.GetOpRegister(operand) <= this.regLo + 15)
			{
				encoder.EncoderFlags |= EncoderFlags.PF0;
				encoder.AddModRMRegister(instruction, operand, this.regLo + 8, this.regLo + 15);
				return;
			}
			encoder.AddModRMRegister(instruction, operand, this.regLo, this.regHi);
		}

		// Token: 0x04003523 RID: 13603
		private readonly Register regLo;

		// Token: 0x04003524 RID: 13604
		private readonly Register regHi;
	}
}
