using System;
using System.Runtime.CompilerServices;

namespace Iced.Intel.EncoderInternal
{
	// Token: 0x020006A2 RID: 1698
	internal sealed class OpIsX : Op
	{
		// Token: 0x06002424 RID: 9252 RVA: 0x0007475F File Offset: 0x0007295F
		public OpIsX(Register regLo, Register regHi)
		{
			this.regLo = regLo;
			this.regHi = regHi;
		}

		// Token: 0x06002425 RID: 9253 RVA: 0x00074778 File Offset: 0x00072978
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
			encoder.ImmSize = ImmSize.SizeIbReg;
			encoder.Immediate = (uint)((uint)(reg - this.regLo) << 4);
		}

		// Token: 0x04003532 RID: 13618
		private readonly Register regLo;

		// Token: 0x04003533 RID: 13619
		private readonly Register regHi;
	}
}
