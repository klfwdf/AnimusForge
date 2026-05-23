using System;
using System.Runtime.CompilerServices;

namespace Iced.Intel.DecoderInternal
{
	// Token: 0x02000715 RID: 1813
	internal sealed class OpCodeHandler_Ev_CL : OpCodeHandlerModRM
	{
		// Token: 0x06002510 RID: 9488 RVA: 0x0007BE30 File Offset: 0x0007A030
		public OpCodeHandler_Ev_CL(Code code16, Code code32, Code code64)
		{
			this.codes = new Code3(code16, code32, code64);
		}

		// Token: 0x06002511 RID: 9489 RVA: 0x0007BE48 File Offset: 0x0007A048
		[NullableContext(1)]
		public unsafe override void Decode(Decoder decoder, ref Instruction instruction)
		{
			UIntPtr operandSize = (UIntPtr)decoder.state.operandSize;
			instruction.InternalSetCodeNoCheck((Code)(*((ref this.codes.codes.FixedElementField) + (UIntPtr)((ulong)operandSize * 2UL))));
			instruction.Op1Register = Register.CL;
			if (decoder.state.mod == 3U)
			{
				instruction.Op0Register = ((int)operandSize << 4) + (int)(decoder.state.rm + decoder.state.zs.extraBaseRegisterBase) + Register.AX;
				return;
			}
			instruction.Op0Kind = OpKind.Memory;
			decoder.ReadOpMem(ref instruction);
		}

		// Token: 0x04003790 RID: 14224
		private readonly Code3 codes;
	}
}
