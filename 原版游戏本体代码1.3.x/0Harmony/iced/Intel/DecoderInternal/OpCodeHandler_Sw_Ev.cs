using System;
using System.Runtime.CompilerServices;

namespace Iced.Intel.DecoderInternal
{
	// Token: 0x0200075B RID: 1883
	internal sealed class OpCodeHandler_Sw_Ev : OpCodeHandlerModRM
	{
		// Token: 0x060025A0 RID: 9632 RVA: 0x0007F0CF File Offset: 0x0007D2CF
		public OpCodeHandler_Sw_Ev(Code code16, Code code32, Code code64)
		{
			this.codes = new Code3(code16, code32, code64);
		}

		// Token: 0x060025A1 RID: 9633 RVA: 0x0007F0E8 File Offset: 0x0007D2E8
		[NullableContext(1)]
		public unsafe override void Decode(Decoder decoder, ref Instruction instruction)
		{
			UIntPtr operandSize = (UIntPtr)decoder.state.operandSize;
			instruction.InternalSetCodeNoCheck((Code)(*((ref this.codes.codes.FixedElementField) + (UIntPtr)((ulong)operandSize * 2UL))));
			Register sreg = decoder.ReadOpSegReg();
			if (decoder.invalidCheckMask != 0U && sreg == Register.CS)
			{
				decoder.SetInvalidInstruction();
			}
			instruction.Op0Register = sreg;
			if (decoder.state.mod == 3U)
			{
				instruction.Op1Register = ((int)operandSize << 4) + (int)(decoder.state.rm + decoder.state.zs.extraBaseRegisterBase) + Register.AX;
				return;
			}
			instruction.Op1Kind = OpKind.Memory;
			decoder.ReadOpMem(ref instruction);
		}

		// Token: 0x0400381A RID: 14362
		private readonly Code3 codes;
	}
}
