using System;
using System.Runtime.CompilerServices;

namespace Iced.Intel.EncoderInternal
{
	// Token: 0x020006A0 RID: 1696
	internal sealed class OpHx : Op
	{
		// Token: 0x06002420 RID: 9248 RVA: 0x000746A5 File Offset: 0x000728A5
		public OpHx(Register regLo, Register regHi)
		{
			this.regLo = regLo;
			this.regHi = regHi;
		}

		// Token: 0x06002421 RID: 9249 RVA: 0x000746BC File Offset: 0x000728BC
		[NullableContext(1)]
		public override void Encode(Encoder encoder, in Instruction instruction, int operand)
		{
			if (!encoder.Verify(operand, OpKind.Register, instruction.GetOpKind(operand)))
			{
				return;
			}
			Register reg = instruction.GetOpRegister(operand);
			if (!encoder.Verify(operand, reg, this.regLo, this.regHi))
			{
				return;
			}
			encoder.EncoderFlags |= (EncoderFlags)(reg - this.regLo << 27);
		}

		// Token: 0x0400352E RID: 13614
		private readonly Register regLo;

		// Token: 0x0400352F RID: 13615
		private readonly Register regHi;
	}
}
