using System;
using System.Runtime.CompilerServices;

namespace Iced.Intel.DecoderInternal
{
	// Token: 0x020006CB RID: 1739
	internal sealed class OpCodeHandler_EVEX_VkWIb_er : OpCodeHandlerModRM
	{
		// Token: 0x06002475 RID: 9333 RVA: 0x00078432 File Offset: 0x00076632
		public OpCodeHandler_EVEX_VkWIb_er(Register baseReg, Code code, TupleType tupleType)
		{
			this.baseReg1 = baseReg;
			this.baseReg2 = baseReg;
			this.code = code;
			this.tupleType = tupleType;
		}

		// Token: 0x06002476 RID: 9334 RVA: 0x00078458 File Offset: 0x00076658
		[NullableContext(1)]
		public override void Decode(Decoder decoder, ref Instruction instruction)
		{
			if ((decoder.state.vvvv_invalidCheck & decoder.invalidCheckMask) != 0U)
			{
				decoder.SetInvalidInstruction();
			}
			instruction.InternalSetCodeNoCheck(this.code);
			instruction.Op0Register = (int)(decoder.state.reg + decoder.state.zs.extraRegisterBase + decoder.state.extraRegisterBaseEVEX) + this.baseReg1;
			if (decoder.state.mod == 3U)
			{
				instruction.Op1Register = (int)(decoder.state.rm + decoder.state.extraBaseRegisterBaseEVEX) + this.baseReg2;
				if ((decoder.state.zs.flags & StateFlags.b) != (StateFlags)0U)
				{
					instruction.InternalSetSuppressAllExceptions();
				}
			}
			else
			{
				instruction.Op1Kind = OpKind.Memory;
				if ((decoder.state.zs.flags & StateFlags.b) != (StateFlags)0U)
				{
					instruction.InternalSetIsBroadcast();
				}
				decoder.ReadOpMem(ref instruction, this.tupleType);
			}
			instruction.Op2Kind = OpKind.Immediate8;
			instruction.InternalImmediate8 = decoder.ReadByte();
		}

		// Token: 0x040036CB RID: 14027
		private readonly Register baseReg1;

		// Token: 0x040036CC RID: 14028
		private readonly Register baseReg2;

		// Token: 0x040036CD RID: 14029
		private readonly Code code;

		// Token: 0x040036CE RID: 14030
		private readonly TupleType tupleType;
	}
}
