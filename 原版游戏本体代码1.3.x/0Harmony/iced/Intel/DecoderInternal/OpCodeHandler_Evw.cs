using System;
using System.Runtime.CompilerServices;

namespace Iced.Intel.DecoderInternal
{
	// Token: 0x0200071D RID: 1821
	internal sealed class OpCodeHandler_Evw : OpCodeHandlerModRM
	{
		// Token: 0x06002521 RID: 9505 RVA: 0x0007C42F File Offset: 0x0007A62F
		public OpCodeHandler_Evw(Code code16, Code code32, Code code64)
		{
			this.codes = new Code3(code16, code32, code64);
		}

		// Token: 0x06002522 RID: 9506 RVA: 0x0007C448 File Offset: 0x0007A648
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

		// Token: 0x040037A2 RID: 14242
		private readonly Code3 codes;
	}
}
