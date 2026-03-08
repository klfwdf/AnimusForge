using System;
using System.Runtime.CompilerServices;

namespace Iced.Intel.DecoderInternal
{
	// Token: 0x02000717 RID: 1815
	internal sealed class OpCodeHandler_Rv : OpCodeHandlerModRM
	{
		// Token: 0x06002515 RID: 9493 RVA: 0x0007BFA4 File Offset: 0x0007A1A4
		public OpCodeHandler_Rv(Code code16, Code code32, Code code64)
		{
			this.codes = new Code3(code16, code32, code64);
		}

		// Token: 0x06002516 RID: 9494 RVA: 0x0007BFBC File Offset: 0x0007A1BC
		[NullableContext(1)]
		public unsafe override void Decode(Decoder decoder, ref Instruction instruction)
		{
			UIntPtr operandSize = (UIntPtr)decoder.state.operandSize;
			instruction.InternalSetCodeNoCheck((Code)(*((ref this.codes.codes.FixedElementField) + (UIntPtr)((ulong)operandSize * 2UL))));
			instruction.Op0Register = ((int)operandSize << 4) + (int)(decoder.state.rm + decoder.state.zs.extraBaseRegisterBase) + Register.AX;
		}

		// Token: 0x04003793 RID: 14227
		private readonly Code3 codes;
	}
}
