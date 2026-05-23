using System;
using System.Runtime.CompilerServices;

namespace Iced.Intel.DecoderInternal
{
	// Token: 0x020006CD RID: 1741
	internal sealed class OpCodeHandler_EVEX_WkV : OpCodeHandlerModRM
	{
		// Token: 0x0600247A RID: 9338 RVA: 0x000786B1 File Offset: 0x000768B1
		public OpCodeHandler_EVEX_WkV(Register baseReg, Code code, TupleType tupleType)
		{
			this.baseReg1 = baseReg;
			this.baseReg2 = baseReg;
			this.code = code;
			this.tupleType = tupleType;
			this.disallowZeroingMasking = 0U;
		}

		// Token: 0x0600247B RID: 9339 RVA: 0x000786DC File Offset: 0x000768DC
		public OpCodeHandler_EVEX_WkV(Register baseReg, Code code, TupleType tupleType, bool allowZeroingMasking)
		{
			this.baseReg1 = baseReg;
			this.baseReg2 = baseReg;
			this.code = code;
			this.tupleType = tupleType;
			this.disallowZeroingMasking = (allowZeroingMasking ? 0U : uint.MaxValue);
		}

		// Token: 0x0600247C RID: 9340 RVA: 0x0007870E File Offset: 0x0007690E
		public OpCodeHandler_EVEX_WkV(Register baseReg1, Register baseReg2, Code code, TupleType tupleType)
		{
			this.baseReg1 = baseReg1;
			this.baseReg2 = baseReg2;
			this.code = code;
			this.tupleType = tupleType;
			this.disallowZeroingMasking = 0U;
		}

		// Token: 0x0600247D RID: 9341 RVA: 0x0007873C File Offset: 0x0007693C
		[NullableContext(1)]
		public override void Decode(Decoder decoder, ref Instruction instruction)
		{
			if ((((decoder.state.zs.flags & StateFlags.b) | (StateFlags)decoder.state.vvvv_invalidCheck) & (StateFlags)decoder.invalidCheckMask) != (StateFlags)0U)
			{
				decoder.SetInvalidInstruction();
			}
			instruction.InternalSetCodeNoCheck(this.code);
			instruction.Op1Register = (int)(decoder.state.reg + decoder.state.zs.extraRegisterBase + decoder.state.extraRegisterBaseEVEX) + this.baseReg2;
			if ((decoder.state.zs.flags & StateFlags.z & (StateFlags)this.disallowZeroingMasking & (StateFlags)decoder.invalidCheckMask) != (StateFlags)0U)
			{
				decoder.SetInvalidInstruction();
			}
			if (decoder.state.mod == 3U)
			{
				instruction.Op0Register = (int)(decoder.state.rm + decoder.state.extraBaseRegisterBaseEVEX) + this.baseReg1;
				return;
			}
			instruction.Op0Kind = OpKind.Memory;
			if ((decoder.state.zs.flags & StateFlags.z & (StateFlags)decoder.invalidCheckMask) != (StateFlags)0U)
			{
				decoder.SetInvalidInstruction();
			}
			decoder.ReadOpMem(ref instruction, this.tupleType);
		}

		// Token: 0x040036D4 RID: 14036
		private readonly Register baseReg1;

		// Token: 0x040036D5 RID: 14037
		private readonly Register baseReg2;

		// Token: 0x040036D6 RID: 14038
		private readonly Code code;

		// Token: 0x040036D7 RID: 14039
		private readonly TupleType tupleType;

		// Token: 0x040036D8 RID: 14040
		private readonly uint disallowZeroingMasking;
	}
}
