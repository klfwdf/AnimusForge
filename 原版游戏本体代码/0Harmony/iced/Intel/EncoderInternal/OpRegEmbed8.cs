using System;
using System.Runtime.CompilerServices;

namespace Iced.Intel.EncoderInternal
{
	// Token: 0x0200068A RID: 1674
	internal sealed class OpRegEmbed8 : Op
	{
		// Token: 0x060023E7 RID: 9191 RVA: 0x00073DC6 File Offset: 0x00071FC6
		public OpRegEmbed8(Register regLo, Register regHi)
		{
			this.regLo = regLo;
			this.regHi = regHi;
		}

		// Token: 0x060023E8 RID: 9192 RVA: 0x00073DDC File Offset: 0x00071FDC
		[NullableContext(1)]
		public override void Encode(Encoder encoder, in Instruction instruction, int operand)
		{
			encoder.AddReg(instruction, operand, this.regLo, this.regHi);
		}

		// Token: 0x0400351B RID: 13595
		private readonly Register regLo;

		// Token: 0x0400351C RID: 13596
		private readonly Register regHi;
	}
}
