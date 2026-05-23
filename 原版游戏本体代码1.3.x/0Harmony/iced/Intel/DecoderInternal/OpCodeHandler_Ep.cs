using System;
using System.Runtime.CompilerServices;

namespace Iced.Intel.DecoderInternal
{
	// Token: 0x0200071C RID: 1820
	internal sealed class OpCodeHandler_Ep : OpCodeHandlerModRM
	{
		// Token: 0x0600251F RID: 9503 RVA: 0x0007C391 File Offset: 0x0007A591
		public OpCodeHandler_Ep(Code code16, Code code32, Code code64)
		{
			this.code16 = code16;
			this.code32 = code32;
			this.code64 = code64;
		}

		// Token: 0x06002520 RID: 9504 RVA: 0x0007C3B0 File Offset: 0x0007A5B0
		[NullableContext(1)]
		public override void Decode(Decoder decoder, ref Instruction instruction)
		{
			if (decoder.state.operandSize == OpSize.Size64 && (decoder.options & DecoderOptions.AMD) == DecoderOptions.None)
			{
				instruction.InternalSetCodeNoCheck(this.code64);
			}
			else if (decoder.state.operandSize == OpSize.Size16)
			{
				instruction.InternalSetCodeNoCheck(this.code16);
			}
			else
			{
				instruction.InternalSetCodeNoCheck(this.code32);
			}
			if (decoder.state.mod == 3U)
			{
				decoder.SetInvalidInstruction();
				return;
			}
			instruction.Op0Kind = OpKind.Memory;
			decoder.ReadOpMem(ref instruction);
		}

		// Token: 0x0400379F RID: 14239
		private readonly Code code16;

		// Token: 0x040037A0 RID: 14240
		private readonly Code code32;

		// Token: 0x040037A1 RID: 14241
		private readonly Code code64;
	}
}
