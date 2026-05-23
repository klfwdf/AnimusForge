using System;
using System.Runtime.CompilerServices;

namespace Iced.Intel.DecoderInternal
{
	// Token: 0x0200075A RID: 1882
	internal sealed class OpCodeHandler_Gv_M : OpCodeHandlerModRM
	{
		// Token: 0x0600259E RID: 9630 RVA: 0x0007F02F File Offset: 0x0007D22F
		public OpCodeHandler_Gv_M(Code code16, Code code32, Code code64)
		{
			this.codes = new Code3(code16, code32, code64);
		}

		// Token: 0x0600259F RID: 9631 RVA: 0x0007F048 File Offset: 0x0007D248
		[NullableContext(1)]
		public unsafe override void Decode(Decoder decoder, ref Instruction instruction)
		{
			UIntPtr operandSize = (UIntPtr)decoder.state.operandSize;
			instruction.InternalSetCodeNoCheck((Code)(*((ref this.codes.codes.FixedElementField) + (UIntPtr)((ulong)operandSize * 2UL))));
			instruction.Op0Register = ((int)operandSize << 4) + (int)(decoder.state.reg + decoder.state.zs.extraRegisterBase) + Register.AX;
			if (decoder.state.mod < 3U)
			{
				instruction.Op1Kind = OpKind.Memory;
				decoder.ReadOpMem(ref instruction);
				return;
			}
			decoder.SetInvalidInstruction();
		}

		// Token: 0x04003819 RID: 14361
		private readonly Code3 codes;
	}
}
