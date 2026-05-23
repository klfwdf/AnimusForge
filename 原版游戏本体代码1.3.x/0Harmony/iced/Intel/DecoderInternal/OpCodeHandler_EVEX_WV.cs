using System;
using System.Runtime.CompilerServices;

namespace Iced.Intel.DecoderInternal
{
	// Token: 0x020006D6 RID: 1750
	internal sealed class OpCodeHandler_EVEX_WV : OpCodeHandlerModRM
	{
		// Token: 0x0600248E RID: 9358 RVA: 0x000790FE File Offset: 0x000772FE
		public OpCodeHandler_EVEX_WV(Register baseReg, Code code, TupleType tupleType)
		{
			this.baseReg1 = baseReg;
			this.baseReg2 = baseReg;
			this.code = code;
			this.tupleType = tupleType;
		}

		// Token: 0x0600248F RID: 9359 RVA: 0x00079124 File Offset: 0x00077324
		[NullableContext(1)]
		public override void Decode(Decoder decoder, ref Instruction instruction)
		{
			if ((((decoder.state.zs.flags & (StateFlags.b | StateFlags.z)) | (StateFlags)decoder.state.vvvv_invalidCheck | (StateFlags)decoder.state.aaa) & (StateFlags)decoder.invalidCheckMask) != (StateFlags)0U)
			{
				decoder.SetInvalidInstruction();
			}
			instruction.InternalSetCodeNoCheck(this.code);
			instruction.Op1Register = (int)(decoder.state.reg + decoder.state.zs.extraRegisterBase + decoder.state.extraRegisterBaseEVEX) + this.baseReg1;
			if (decoder.state.mod == 3U)
			{
				instruction.Op0Register = (int)(decoder.state.rm + decoder.state.extraBaseRegisterBaseEVEX) + this.baseReg2;
				if ((decoder.state.zs.flags & StateFlags.b & (StateFlags)decoder.invalidCheckMask) != (StateFlags)0U)
				{
					decoder.SetInvalidInstruction();
					return;
				}
			}
			else
			{
				instruction.Op0Kind = OpKind.Memory;
				decoder.ReadOpMem(ref instruction, this.tupleType);
			}
		}

		// Token: 0x040036FA RID: 14074
		private readonly Register baseReg1;

		// Token: 0x040036FB RID: 14075
		private readonly Register baseReg2;

		// Token: 0x040036FC RID: 14076
		private readonly Code code;

		// Token: 0x040036FD RID: 14077
		private readonly TupleType tupleType;
	}
}
