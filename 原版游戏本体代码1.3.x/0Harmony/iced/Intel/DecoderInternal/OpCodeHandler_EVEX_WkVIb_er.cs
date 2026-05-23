using System;
using System.Runtime.CompilerServices;

namespace Iced.Intel.DecoderInternal
{
	// Token: 0x020006D3 RID: 1747
	internal sealed class OpCodeHandler_EVEX_WkVIb_er : OpCodeHandlerModRM
	{
		// Token: 0x06002488 RID: 9352 RVA: 0x00078D89 File Offset: 0x00076F89
		public OpCodeHandler_EVEX_WkVIb_er(Register baseReg1, Register baseReg2, Code code, TupleType tupleType)
		{
			this.baseReg1 = baseReg1;
			this.baseReg2 = baseReg2;
			this.code = code;
			this.tupleType = tupleType;
		}

		// Token: 0x06002489 RID: 9353 RVA: 0x00078DB0 File Offset: 0x00076FB0
		[NullableContext(1)]
		public override void Decode(Decoder decoder, ref Instruction instruction)
		{
			if ((decoder.state.vvvv_invalidCheck & decoder.invalidCheckMask) != 0U)
			{
				decoder.SetInvalidInstruction();
			}
			instruction.InternalSetCodeNoCheck(this.code);
			if (decoder.state.mod == 3U)
			{
				instruction.Op0Register = (int)(decoder.state.rm + decoder.state.extraBaseRegisterBaseEVEX) + this.baseReg1;
				if ((decoder.state.zs.flags & StateFlags.b) != (StateFlags)0U)
				{
					instruction.InternalSetSuppressAllExceptions();
				}
			}
			else
			{
				instruction.Op0Kind = OpKind.Memory;
				if ((decoder.state.zs.flags & (StateFlags.b | StateFlags.z) & (StateFlags)decoder.invalidCheckMask) != (StateFlags)0U)
				{
					decoder.SetInvalidInstruction();
				}
				decoder.ReadOpMem(ref instruction, this.tupleType);
			}
			instruction.Op1Register = (int)(decoder.state.reg + decoder.state.zs.extraRegisterBase + decoder.state.extraRegisterBaseEVEX) + this.baseReg2;
			instruction.Op2Kind = OpKind.Immediate8;
			instruction.InternalImmediate8 = decoder.ReadByte();
		}

		// Token: 0x040036EE RID: 14062
		private readonly Register baseReg1;

		// Token: 0x040036EF RID: 14063
		private readonly Register baseReg2;

		// Token: 0x040036F0 RID: 14064
		private readonly Code code;

		// Token: 0x040036F1 RID: 14065
		private readonly TupleType tupleType;
	}
}
