using System;
using System.Runtime.CompilerServices;

namespace Iced.Intel.DecoderInternal
{
	// Token: 0x02000718 RID: 1816
	internal sealed class OpCodeHandler_Rv_32_64 : OpCodeHandlerModRM
	{
		// Token: 0x06002517 RID: 9495 RVA: 0x0007C01E File Offset: 0x0007A21E
		public OpCodeHandler_Rv_32_64(Code code32, Code code64)
		{
			this.code32 = code32;
			this.code64 = code64;
		}

		// Token: 0x06002518 RID: 9496 RVA: 0x0007C034 File Offset: 0x0007A234
		[NullableContext(1)]
		public override void Decode(Decoder decoder, ref Instruction instruction)
		{
			Register baseReg;
			if (decoder.is64bMode)
			{
				instruction.InternalSetCodeNoCheck(this.code64);
				baseReg = Register.RAX;
			}
			else
			{
				instruction.InternalSetCodeNoCheck(this.code32);
				baseReg = Register.EAX;
			}
			instruction.Op0Register = (int)(decoder.state.rm + decoder.state.zs.extraBaseRegisterBase) + baseReg;
		}

		// Token: 0x04003794 RID: 14228
		private readonly Code code32;

		// Token: 0x04003795 RID: 14229
		private readonly Code code64;
	}
}
