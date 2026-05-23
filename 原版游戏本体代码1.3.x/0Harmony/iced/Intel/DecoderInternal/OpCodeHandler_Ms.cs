using System;
using System.Runtime.CompilerServices;

namespace Iced.Intel.DecoderInternal
{
	// Token: 0x0200071F RID: 1823
	internal sealed class OpCodeHandler_Ms : OpCodeHandlerModRM
	{
		// Token: 0x06002525 RID: 9509 RVA: 0x0007C561 File Offset: 0x0007A761
		public OpCodeHandler_Ms(Code code16, Code code32, Code code64)
		{
			this.code16 = code16;
			this.code32 = code32;
			this.code64 = code64;
		}

		// Token: 0x06002526 RID: 9510 RVA: 0x0007C580 File Offset: 0x0007A780
		[NullableContext(1)]
		public override void Decode(Decoder decoder, ref Instruction instruction)
		{
			if (decoder.is64bMode)
			{
				instruction.InternalSetCodeNoCheck(this.code64);
			}
			else if (decoder.state.operandSize == OpSize.Size32)
			{
				instruction.InternalSetCodeNoCheck(this.code32);
			}
			else
			{
				instruction.InternalSetCodeNoCheck(this.code16);
			}
			instruction.Op0Kind = OpKind.Memory;
			decoder.ReadOpMem(ref instruction);
		}

		// Token: 0x040037A4 RID: 14244
		private readonly Code code16;

		// Token: 0x040037A5 RID: 14245
		private readonly Code code32;

		// Token: 0x040037A6 RID: 14246
		private readonly Code code64;
	}
}
