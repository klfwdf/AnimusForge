using System;
using System.Runtime.CompilerServices;

namespace Iced.Intel.DecoderInternal
{
	// Token: 0x020007CB RID: 1995
	internal sealed class OpCodeHandler_VEX_Gv_Ev_Id : OpCodeHandlerModRM
	{
		// Token: 0x0600268C RID: 9868 RVA: 0x0008416B File Offset: 0x0008236B
		public OpCodeHandler_VEX_Gv_Ev_Id(Code code32, Code code64)
		{
			this.code32 = code32;
			this.code64 = code64;
		}

		// Token: 0x0600268D RID: 9869 RVA: 0x00084184 File Offset: 0x00082384
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
			instruction.Op2Kind = OpKind.Immediate32;
			instruction.Immediate32 = decoder.ReadUInt32();
		}

		// Token: 0x040038EC RID: 14572
		private readonly Code code32;

		// Token: 0x040038ED RID: 14573
		private readonly Code code64;
	}
}
