using System;
using System.Runtime.CompilerServices;

namespace Iced.Intel.DecoderInternal
{
	// Token: 0x02000730 RID: 1840
	internal sealed class OpCodeHandler_Ev_Gv_32_64 : OpCodeHandlerModRM
	{
		// Token: 0x06002548 RID: 9544 RVA: 0x0007D4FA File Offset: 0x0007B6FA
		public OpCodeHandler_Ev_Gv_32_64(Code code32, Code code64)
		{
			this.code32 = code32;
			this.code64 = code64;
		}

		// Token: 0x06002549 RID: 9545 RVA: 0x0007D510 File Offset: 0x0007B710
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
			instruction.Op1Register = (int)(decoder.state.reg + decoder.state.zs.extraRegisterBase) + baseReg;
			if (decoder.state.mod == 3U)
			{
				instruction.Op0Register = (int)(decoder.state.rm + decoder.state.zs.extraBaseRegisterBase) + baseReg;
				return;
			}
			instruction.Op0Kind = OpKind.Memory;
			decoder.ReadOpMem(ref instruction);
		}

		// Token: 0x040037CE RID: 14286
		private readonly Code code32;

		// Token: 0x040037CF RID: 14287
		private readonly Code code64;
	}
}
