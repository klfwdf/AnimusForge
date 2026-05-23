using System;
using System.Runtime.CompilerServices;

namespace Iced.Intel.DecoderInternal
{
	// Token: 0x0200071E RID: 1822
	internal sealed class OpCodeHandler_Ew : OpCodeHandlerModRM
	{
		// Token: 0x06002523 RID: 9507 RVA: 0x0007C4C9 File Offset: 0x0007A6C9
		public OpCodeHandler_Ew(Code code16, Code code32, Code code64)
		{
			this.codes = new Code3(code16, code32, code64);
		}

		// Token: 0x06002524 RID: 9508 RVA: 0x0007C4E0 File Offset: 0x0007A6E0
		[NullableContext(1)]
		public unsafe override void Decode(Decoder decoder, ref Instruction instruction)
		{
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

		// Token: 0x040037A3 RID: 14243
		private readonly Code3 codes;
	}
}
