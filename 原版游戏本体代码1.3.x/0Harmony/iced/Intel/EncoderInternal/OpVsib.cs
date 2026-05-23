using System;
using System.Runtime.CompilerServices;

namespace Iced.Intel.EncoderInternal
{
	// Token: 0x020006A1 RID: 1697
	internal sealed class OpVsib : Op
	{
		// Token: 0x06002422 RID: 9250 RVA: 0x00074712 File Offset: 0x00072912
		public OpVsib(Register regLo, Register regHi)
		{
			this.vsibIndexRegLo = regLo;
			this.vsibIndexRegHi = regHi;
		}

		// Token: 0x06002423 RID: 9251 RVA: 0x00074728 File Offset: 0x00072928
		[NullableContext(1)]
		public override void Encode(Encoder encoder, in Instruction instruction, int operand)
		{
			encoder.EncoderFlags |= EncoderFlags.MustUseSib;
			encoder.AddRegOrMem(instruction, operand, Register.None, Register.None, this.vsibIndexRegLo, this.vsibIndexRegHi, true, false);
		}

		// Token: 0x04003530 RID: 13616
		private readonly Register vsibIndexRegLo;

		// Token: 0x04003531 RID: 13617
		private readonly Register vsibIndexRegHi;
	}
}
