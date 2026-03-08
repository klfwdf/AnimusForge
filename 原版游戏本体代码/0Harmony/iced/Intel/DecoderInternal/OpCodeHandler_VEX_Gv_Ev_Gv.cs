using System;
using System.Runtime.CompilerServices;

namespace Iced.Intel.DecoderInternal
{
	// Token: 0x020007C5 RID: 1989
	internal sealed class OpCodeHandler_VEX_Gv_Ev_Gv : OpCodeHandlerModRM
	{
		// Token: 0x06002680 RID: 9856 RVA: 0x00083C3C File Offset: 0x00081E3C
		public OpCodeHandler_VEX_Gv_Ev_Gv(Code code32, Code code64)
		{
			this.code32 = code32;
			this.code64 = code64;
		}

		// Token: 0x06002681 RID: 9857 RVA: 0x00083C54 File Offset: 0x00081E54
		[NullableContext(1)]
		public override void Decode(Decoder decoder, ref Instruction instruction)
		{
			Register gpr;
			if ((decoder.state.zs.flags & (StateFlags)decoder.is64bMode_and_W) != (StateFlags)0U)
			{
				instruction.InternalSetCodeNoCheck(this.code64);
				gpr = Register.RAX;
			}
			else
			{
				instruction.InternalSetCodeNoCheck(this.code32);
				gpr = Register.EAX;
			}
			instruction.Op0Register = (int)(decoder.state.reg + decoder.state.zs.extraRegisterBase) + gpr;
			instruction.Op2Register = (int)decoder.state.vvvv + gpr;
			if (decoder.state.mod == 3U)
			{
				instruction.Op1Register = (int)(decoder.state.rm + decoder.state.zs.extraBaseRegisterBase) + gpr;
				return;
			}
			instruction.Op1Kind = OpKind.Memory;
			decoder.ReadOpMem(ref instruction);
		}

		// Token: 0x040038DF RID: 14559
		private readonly Code code32;

		// Token: 0x040038E0 RID: 14560
		private readonly Code code64;
	}
}
