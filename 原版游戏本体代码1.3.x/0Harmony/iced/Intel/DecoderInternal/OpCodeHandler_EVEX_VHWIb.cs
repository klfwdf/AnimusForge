using System;
using System.Runtime.CompilerServices;

namespace Iced.Intel.DecoderInternal
{
	// Token: 0x020006E3 RID: 1763
	internal sealed class OpCodeHandler_EVEX_VHWIb : OpCodeHandlerModRM
	{
		// Token: 0x060024AA RID: 9386 RVA: 0x00079F94 File Offset: 0x00078194
		public OpCodeHandler_EVEX_VHWIb(Register baseReg, Code code, TupleType tupleType)
		{
			this.baseReg = baseReg;
			this.code = code;
			this.tupleType = tupleType;
		}

		// Token: 0x060024AB RID: 9387 RVA: 0x00079FB4 File Offset: 0x000781B4
		[NullableContext(1)]
		public override void Decode(Decoder decoder, ref Instruction instruction)
		{
			if ((((decoder.state.zs.flags & (StateFlags.b | StateFlags.z)) | (StateFlags)decoder.state.aaa) & (StateFlags)decoder.invalidCheckMask) != (StateFlags)0U)
			{
				decoder.SetInvalidInstruction();
			}
			instruction.InternalSetCodeNoCheck(this.code);
			instruction.Op0Register = (int)(decoder.state.reg + decoder.state.zs.extraRegisterBase + decoder.state.extraRegisterBaseEVEX) + this.baseReg;
			instruction.Op1Register = (int)decoder.state.vvvv + this.baseReg;
			if (decoder.state.mod == 3U)
			{
				instruction.Op2Register = (int)(decoder.state.rm + decoder.state.extraBaseRegisterBaseEVEX) + this.baseReg;
			}
			else
			{
				instruction.Op2Kind = OpKind.Memory;
				decoder.ReadOpMem(ref instruction, this.tupleType);
			}
			instruction.Op3Kind = OpKind.Immediate8;
			instruction.InternalImmediate8 = decoder.ReadByte();
		}

		// Token: 0x0400372C RID: 14124
		private readonly Register baseReg;

		// Token: 0x0400372D RID: 14125
		private readonly Code code;

		// Token: 0x0400372E RID: 14126
		private readonly TupleType tupleType;
	}
}
