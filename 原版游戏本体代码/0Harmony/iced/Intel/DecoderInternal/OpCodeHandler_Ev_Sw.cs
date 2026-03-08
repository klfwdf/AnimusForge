using System;
using System.Runtime.CompilerServices;

namespace Iced.Intel.DecoderInternal
{
	// Token: 0x02000758 RID: 1880
	internal sealed class OpCodeHandler_Ev_Sw : OpCodeHandlerModRM
	{
		// Token: 0x0600259A RID: 9626 RVA: 0x0007EF3E File Offset: 0x0007D13E
		public OpCodeHandler_Ev_Sw(Code code16, Code code32, Code code64)
		{
			this.codes = new Code3(code16, code32, code64);
		}

		// Token: 0x0600259B RID: 9627 RVA: 0x0007EF54 File Offset: 0x0007D154
		[NullableContext(1)]
		public unsafe override void Decode(Decoder decoder, ref Instruction instruction)
		{
			instruction.Op1Register = decoder.ReadOpSegReg();
			UIntPtr operandSize = (UIntPtr)decoder.state.operandSize;
			instruction.InternalSetCodeNoCheck((Code)(*((ref this.codes.codes.FixedElementField) + (UIntPtr)((ulong)operandSize * 2UL))));
			if (decoder.state.mod == 3U)
			{
				instruction.Op0Register = ((int)operandSize << 4) + (int)(decoder.state.rm + decoder.state.zs.extraBaseRegisterBase) + Register.AX;
				return;
			}
			instruction.Op0Kind = OpKind.Memory;
			decoder.ReadOpMem(ref instruction);
		}

		// Token: 0x04003817 RID: 14359
		private readonly Code3 codes;
	}
}
