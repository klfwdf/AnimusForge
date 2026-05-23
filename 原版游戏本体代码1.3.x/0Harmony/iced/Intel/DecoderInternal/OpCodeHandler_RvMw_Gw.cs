using System;
using System.Runtime.CompilerServices;

namespace Iced.Intel.DecoderInternal
{
	// Token: 0x02000747 RID: 1863
	internal sealed class OpCodeHandler_RvMw_Gw : OpCodeHandlerModRM
	{
		// Token: 0x06002578 RID: 9592 RVA: 0x0007E412 File Offset: 0x0007C612
		public OpCodeHandler_RvMw_Gw(Code code16, Code code32)
		{
			this.code16 = code16;
			this.code32 = code32;
		}

		// Token: 0x06002579 RID: 9593 RVA: 0x0007E428 File Offset: 0x0007C628
		[NullableContext(1)]
		public override void Decode(Decoder decoder, ref Instruction instruction)
		{
			Register baseReg;
			if (decoder.state.operandSize != OpSize.Size16)
			{
				instruction.InternalSetCodeNoCheck(this.code32);
				instruction.Op1Register = (int)(decoder.state.reg + decoder.state.zs.extraRegisterBase) + Register.EAX;
				baseReg = Register.EAX;
			}
			else
			{
				instruction.InternalSetCodeNoCheck(this.code16);
				instruction.Op1Register = (int)(decoder.state.reg + decoder.state.zs.extraRegisterBase) + Register.AX;
				baseReg = Register.AX;
			}
			if (decoder.state.mod == 3U)
			{
				instruction.Op0Register = (int)(decoder.state.rm + decoder.state.zs.extraBaseRegisterBase) + baseReg;
				return;
			}
			instruction.Op0Kind = OpKind.Memory;
			decoder.ReadOpMem(ref instruction);
		}

		// Token: 0x040037FC RID: 14332
		private readonly Code code16;

		// Token: 0x040037FD RID: 14333
		private readonly Code code32;
	}
}
