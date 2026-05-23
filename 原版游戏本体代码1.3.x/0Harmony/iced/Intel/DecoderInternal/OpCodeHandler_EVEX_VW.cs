using System;
using System.Runtime.CompilerServices;

namespace Iced.Intel.DecoderInternal
{
	// Token: 0x020006D5 RID: 1749
	internal sealed class OpCodeHandler_EVEX_VW : OpCodeHandlerModRM
	{
		// Token: 0x0600248C RID: 9356 RVA: 0x00078FE5 File Offset: 0x000771E5
		public OpCodeHandler_EVEX_VW(Register baseReg, Code code, TupleType tupleType)
		{
			this.baseReg1 = baseReg;
			this.baseReg2 = baseReg;
			this.code = code;
			this.tupleType = tupleType;
		}

		// Token: 0x0600248D RID: 9357 RVA: 0x0007900C File Offset: 0x0007720C
		[NullableContext(1)]
		public override void Decode(Decoder decoder, ref Instruction instruction)
		{
			if ((((decoder.state.zs.flags & (StateFlags.b | StateFlags.z)) | (StateFlags)decoder.state.vvvv_invalidCheck | (StateFlags)decoder.state.aaa) & (StateFlags)decoder.invalidCheckMask) != (StateFlags)0U)
			{
				decoder.SetInvalidInstruction();
			}
			instruction.InternalSetCodeNoCheck(this.code);
			instruction.Op0Register = (int)(decoder.state.reg + decoder.state.zs.extraRegisterBase + decoder.state.extraRegisterBaseEVEX) + this.baseReg1;
			if (decoder.state.mod == 3U)
			{
				instruction.Op1Register = (int)(decoder.state.rm + decoder.state.extraBaseRegisterBaseEVEX) + this.baseReg2;
				if ((decoder.state.zs.flags & StateFlags.b & (StateFlags)decoder.invalidCheckMask) != (StateFlags)0U)
				{
					decoder.SetInvalidInstruction();
					return;
				}
			}
			else
			{
				instruction.Op1Kind = OpKind.Memory;
				decoder.ReadOpMem(ref instruction, this.tupleType);
			}
		}

		// Token: 0x040036F6 RID: 14070
		private readonly Register baseReg1;

		// Token: 0x040036F7 RID: 14071
		private readonly Register baseReg2;

		// Token: 0x040036F8 RID: 14072
		private readonly Code code;

		// Token: 0x040036F9 RID: 14073
		private readonly TupleType tupleType;
	}
}
