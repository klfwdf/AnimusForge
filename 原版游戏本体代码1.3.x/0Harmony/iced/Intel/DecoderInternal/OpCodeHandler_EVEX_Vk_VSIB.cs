using System;
using System.Runtime.CompilerServices;

namespace Iced.Intel.DecoderInternal
{
	// Token: 0x020006EC RID: 1772
	internal sealed class OpCodeHandler_EVEX_Vk_VSIB : OpCodeHandlerModRM
	{
		// Token: 0x060024BE RID: 9406 RVA: 0x0007A9B2 File Offset: 0x00078BB2
		public OpCodeHandler_EVEX_Vk_VSIB(Register baseReg, Register vsibBase, Code code, TupleType tupleType)
		{
			this.baseReg = baseReg;
			this.vsibBase = vsibBase;
			this.code = code;
			this.tupleType = tupleType;
		}

		// Token: 0x060024BF RID: 9407 RVA: 0x0007A9D8 File Offset: 0x00078BD8
		[NullableContext(1)]
		public override void Decode(Decoder decoder, ref Instruction instruction)
		{
			if (decoder.invalidCheckMask != 0U && (((decoder.state.zs.flags & (StateFlags.b | StateFlags.z)) | (StateFlags)(decoder.state.vvvv_invalidCheck & 15U)) != (StateFlags)0U || decoder.state.aaa == 0U))
			{
				decoder.SetInvalidInstruction();
			}
			instruction.InternalSetCodeNoCheck(this.code);
			int regNum = (int)(decoder.state.reg + decoder.state.zs.extraRegisterBase + decoder.state.extraRegisterBaseEVEX);
			instruction.Op0Register = regNum + this.baseReg;
			if (decoder.state.mod == 3U)
			{
				decoder.SetInvalidInstruction();
				return;
			}
			instruction.Op1Kind = OpKind.Memory;
			decoder.ReadOpMem_VSIB(ref instruction, this.vsibBase, this.tupleType);
			if (decoder.invalidCheckMask != 0U && regNum == (instruction.MemoryIndex - Register.XMM0) % 32)
			{
				decoder.SetInvalidInstruction();
			}
		}

		// Token: 0x0400374E RID: 14158
		private readonly Register baseReg;

		// Token: 0x0400374F RID: 14159
		private readonly Register vsibBase;

		// Token: 0x04003750 RID: 14160
		private readonly Code code;

		// Token: 0x04003751 RID: 14161
		private readonly TupleType tupleType;
	}
}
