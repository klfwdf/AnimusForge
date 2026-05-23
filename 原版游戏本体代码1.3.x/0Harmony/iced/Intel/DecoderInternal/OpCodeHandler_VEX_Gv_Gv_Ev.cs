using System;
using System.Runtime.CompilerServices;

namespace Iced.Intel.DecoderInternal
{
	// Token: 0x020007C4 RID: 1988
	internal sealed class OpCodeHandler_VEX_Gv_Gv_Ev : OpCodeHandlerModRM
	{
		// Token: 0x0600267E RID: 9854 RVA: 0x00083B66 File Offset: 0x00081D66
		public OpCodeHandler_VEX_Gv_Gv_Ev(Code code32, Code code64)
		{
			this.code32 = code32;
			this.code64 = code64;
		}

		// Token: 0x0600267F RID: 9855 RVA: 0x00083B7C File Offset: 0x00081D7C
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
			instruction.Op1Register = (int)decoder.state.vvvv + gpr;
			if (decoder.state.mod == 3U)
			{
				instruction.Op2Register = (int)(decoder.state.rm + decoder.state.zs.extraBaseRegisterBase) + gpr;
				return;
			}
			instruction.Op2Kind = OpKind.Memory;
			decoder.ReadOpMem(ref instruction);
		}

		// Token: 0x040038DD RID: 14557
		private readonly Code code32;

		// Token: 0x040038DE RID: 14558
		private readonly Code code64;
	}
}
