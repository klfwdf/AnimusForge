using System;
using System.Runtime.CompilerServices;

namespace Iced.Intel.DecoderInternal
{
	// Token: 0x020006D0 RID: 1744
	internal sealed class OpCodeHandler_EVEX_WkVIb : OpCodeHandlerModRM
	{
		// Token: 0x06002482 RID: 9346 RVA: 0x00078A55 File Offset: 0x00076C55
		public OpCodeHandler_EVEX_WkVIb(Register baseReg1, Register baseReg2, Code code, TupleType tupleType)
		{
			this.baseReg1 = baseReg1;
			this.baseReg2 = baseReg2;
			this.code = code;
			this.tupleType = tupleType;
		}

		// Token: 0x06002483 RID: 9347 RVA: 0x00078A7C File Offset: 0x00076C7C
		[NullableContext(1)]
		public override void Decode(Decoder decoder, ref Instruction instruction)
		{
			if ((((decoder.state.zs.flags & StateFlags.b) | (StateFlags)decoder.state.vvvv_invalidCheck) & (StateFlags)decoder.invalidCheckMask) != (StateFlags)0U)
			{
				decoder.SetInvalidInstruction();
			}
			instruction.InternalSetCodeNoCheck(this.code);
			if (decoder.state.mod == 3U)
			{
				instruction.Op0Register = (int)(decoder.state.rm + decoder.state.extraBaseRegisterBaseEVEX) + this.baseReg1;
			}
			else
			{
				instruction.Op0Kind = OpKind.Memory;
				if ((decoder.state.zs.flags & StateFlags.z & (StateFlags)decoder.invalidCheckMask) != (StateFlags)0U)
				{
					decoder.SetInvalidInstruction();
				}
				decoder.ReadOpMem(ref instruction, this.tupleType);
			}
			instruction.Op1Register = (int)(decoder.state.reg + decoder.state.zs.extraRegisterBase + decoder.state.extraRegisterBaseEVEX) + this.baseReg2;
			instruction.Op2Kind = OpKind.Immediate8;
			instruction.InternalImmediate8 = decoder.ReadByte();
		}

		// Token: 0x040036E1 RID: 14049
		private readonly Register baseReg1;

		// Token: 0x040036E2 RID: 14050
		private readonly Register baseReg2;

		// Token: 0x040036E3 RID: 14051
		private readonly Code code;

		// Token: 0x040036E4 RID: 14052
		private readonly TupleType tupleType;
	}
}
