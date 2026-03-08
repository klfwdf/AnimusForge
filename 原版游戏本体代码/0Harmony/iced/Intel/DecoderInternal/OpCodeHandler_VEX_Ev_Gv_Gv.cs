using System;
using System.Runtime.CompilerServices;

namespace Iced.Intel.DecoderInternal
{
	// Token: 0x020007C6 RID: 1990
	internal sealed class OpCodeHandler_VEX_Ev_Gv_Gv : OpCodeHandlerModRM
	{
		// Token: 0x06002682 RID: 9858 RVA: 0x00083D14 File Offset: 0x00081F14
		public OpCodeHandler_VEX_Ev_Gv_Gv(Code code32, Code code64)
		{
			this.code32 = code32;
			this.code64 = code64;
		}

		// Token: 0x06002683 RID: 9859 RVA: 0x00083D2C File Offset: 0x00081F2C
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
			instruction.Op1Register = (int)(decoder.state.reg + decoder.state.zs.extraRegisterBase) + gpr;
			instruction.Op2Register = (int)decoder.state.vvvv + gpr;
			if (decoder.state.mod == 3U)
			{
				instruction.Op0Register = (int)(decoder.state.rm + decoder.state.zs.extraBaseRegisterBase) + gpr;
				return;
			}
			instruction.Op0Kind = OpKind.Memory;
			decoder.ReadOpMem(ref instruction);
		}

		// Token: 0x040038E1 RID: 14561
		private readonly Code code32;

		// Token: 0x040038E2 RID: 14562
		private readonly Code code64;
	}
}
