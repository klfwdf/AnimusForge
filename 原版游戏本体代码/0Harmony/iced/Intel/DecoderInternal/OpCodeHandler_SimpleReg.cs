using System;
using System.Runtime.CompilerServices;

namespace Iced.Intel.DecoderInternal
{
	// Token: 0x0200073F RID: 1855
	internal sealed class OpCodeHandler_SimpleReg : OpCodeHandler
	{
		// Token: 0x06002566 RID: 9574 RVA: 0x0007DE2F File Offset: 0x0007C02F
		public OpCodeHandler_SimpleReg(Code code, int index)
		{
			this.code = code;
			this.index = index;
		}

		// Token: 0x06002567 RID: 9575 RVA: 0x0007DE48 File Offset: 0x0007C048
		[NullableContext(1)]
		public override void Decode(Decoder decoder, ref Instruction instruction)
		{
			int sizeIndex = (int)decoder.state.operandSize;
			instruction.InternalSetCodeNoCheck(sizeIndex + this.code);
			instruction.Op0Register = sizeIndex * 16 + this.index + (int)decoder.state.zs.extraBaseRegisterBase + Register.AX;
		}

		// Token: 0x040037E8 RID: 14312
		private readonly Code code;

		// Token: 0x040037E9 RID: 14313
		private readonly int index;
	}
}
