using System;
using System.Runtime.CompilerServices;

namespace Iced.Intel.DecoderInternal
{
	// Token: 0x020006D7 RID: 1751
	internal sealed class OpCodeHandler_EVEX_VM : OpCodeHandlerModRM
	{
		// Token: 0x06002490 RID: 9360 RVA: 0x00079216 File Offset: 0x00077416
		public OpCodeHandler_EVEX_VM(Register baseReg, Code code, TupleType tupleType)
		{
			this.baseReg = baseReg;
			this.code = code;
			this.tupleType = tupleType;
		}

		// Token: 0x06002491 RID: 9361 RVA: 0x00079234 File Offset: 0x00077434
		[NullableContext(1)]
		public override void Decode(Decoder decoder, ref Instruction instruction)
		{
			if ((((decoder.state.zs.flags & (StateFlags.b | StateFlags.z)) | (StateFlags)decoder.state.vvvv_invalidCheck | (StateFlags)decoder.state.aaa) & (StateFlags)decoder.invalidCheckMask) != (StateFlags)0U)
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

		// Token: 0x040036FE RID: 14078
		private readonly Register baseReg;

		// Token: 0x040036FF RID: 14079
		private readonly Code code;

		// Token: 0x04003700 RID: 14080
		private readonly TupleType tupleType;
	}
}
