using System;
using System.Runtime.CompilerServices;

namespace Iced.Intel.DecoderInternal
{
	// Token: 0x020006CE RID: 1742
	internal sealed class OpCodeHandler_EVEX_VkM : OpCodeHandlerModRM
	{
		// Token: 0x0600247E RID: 9342 RVA: 0x0007884B File Offset: 0x00076A4B
		public OpCodeHandler_EVEX_VkM(Register baseReg, Code code, TupleType tupleType)
		{
			this.baseReg = baseReg;
			this.code = code;
			this.tupleType = tupleType;
		}

		// Token: 0x0600247F RID: 9343 RVA: 0x00078868 File Offset: 0x00076A68
		[NullableContext(1)]
		public override void Decode(Decoder decoder, ref Instruction instruction)
		{
			if ((((decoder.state.zs.flags & StateFlags.b) | (StateFlags)decoder.state.vvvv_invalidCheck) & (StateFlags)decoder.invalidCheckMask) != (StateFlags)0U)
			{
				decoder.SetInvalidInstruction();
			}
			instruction.InternalSetCodeNoCheck(this.code);
			instruction.Op0Register = (int)(decoder.state.reg + decoder.state.zs.extraRegisterBase + decoder.state.extraRegisterBaseEVEX) + this.baseReg;
			if (decoder.state.mod == 3U)
			{
				decoder.SetInvalidInstruction();
				return;
			}
			instruction.Op1Kind = OpKind.Memory;
			decoder.ReadOpMem(ref instruction, this.tupleType);
		}

		// Token: 0x040036D9 RID: 14041
		private readonly Register baseReg;

		// Token: 0x040036DA RID: 14042
		private readonly Code code;

		// Token: 0x040036DB RID: 14043
		private readonly TupleType tupleType;
	}
}
