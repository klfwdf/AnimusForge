using System;
using System.Runtime.CompilerServices;

namespace Iced.Intel.DecoderInternal
{
	// Token: 0x020007C7 RID: 1991
	internal sealed class OpCodeHandler_VEX_Hv_Ev : OpCodeHandlerModRM
	{
		// Token: 0x06002684 RID: 9860 RVA: 0x00083DEC File Offset: 0x00081FEC
		public OpCodeHandler_VEX_Hv_Ev(Code code32, Code code64)
		{
			this.code32 = code32;
			this.code64 = code64;
		}

		// Token: 0x06002685 RID: 9861 RVA: 0x00083E04 File Offset: 0x00082004
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
			instruction.Op0Register = (int)decoder.state.vvvv + gpr;
			if (decoder.state.mod == 3U)
			{
				instruction.Op1Register = (int)(decoder.state.rm + decoder.state.zs.extraBaseRegisterBase) + gpr;
				return;
			}
			instruction.Op1Kind = OpKind.Memory;
			decoder.ReadOpMem(ref instruction);
		}

		// Token: 0x040038E3 RID: 14563
		private readonly Code code32;

		// Token: 0x040038E4 RID: 14564
		private readonly Code code64;
	}
}
