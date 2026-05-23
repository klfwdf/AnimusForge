using System;
using System.Runtime.CompilerServices;

namespace Iced.Intel.DecoderInternal
{
	// Token: 0x020007CA RID: 1994
	internal sealed class OpCodeHandler_VEX_Gv_Ev_Ib : OpCodeHandlerModRM
	{
		// Token: 0x0600268A RID: 9866 RVA: 0x00084078 File Offset: 0x00082278
		public OpCodeHandler_VEX_Gv_Ev_Ib(Code code32, Code code64)
		{
			this.code32 = code32;
			this.code64 = code64;
		}

		// Token: 0x0600268B RID: 9867 RVA: 0x00084090 File Offset: 0x00082290
		[NullableContext(1)]
		public override void Decode(Decoder decoder, ref Instruction instruction)
		{
			if ((decoder.state.vvvv_invalidCheck & decoder.invalidCheckMask) != 0U)
			{
				decoder.SetInvalidInstruction();
			}
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
			if (decoder.state.mod == 3U)
			{
				instruction.Op1Register = (int)(decoder.state.rm + decoder.state.zs.extraBaseRegisterBase) + gpr;
			}
			else
			{
				instruction.Op1Kind = OpKind.Memory;
				decoder.ReadOpMem(ref instruction);
			}
			instruction.Op2Kind = OpKind.Immediate8;
			instruction.InternalImmediate8 = decoder.ReadByte();
		}

		// Token: 0x040038EA RID: 14570
		private readonly Code code32;

		// Token: 0x040038EB RID: 14571
		private readonly Code code64;
	}
}
