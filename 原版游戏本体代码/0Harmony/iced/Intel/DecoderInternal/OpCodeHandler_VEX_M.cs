using System;
using System.Runtime.CompilerServices;

namespace Iced.Intel.DecoderInternal
{
	// Token: 0x020007A8 RID: 1960
	internal sealed class OpCodeHandler_VEX_M : OpCodeHandlerModRM
	{
		// Token: 0x06002642 RID: 9794 RVA: 0x00082541 File Offset: 0x00080741
		public OpCodeHandler_VEX_M(Code code)
		{
			this.code = code;
		}

		// Token: 0x06002643 RID: 9795 RVA: 0x00082550 File Offset: 0x00080750
		[NullableContext(1)]
		public override void Decode(Decoder decoder, ref Instruction instruction)
		{
			if ((decoder.state.vvvv_invalidCheck & decoder.invalidCheckMask) != 0U)
			{
				decoder.SetInvalidInstruction();
			}
			instruction.InternalSetCodeNoCheck(this.code);
			if (decoder.state.mod == 3U)
			{
				decoder.SetInvalidInstruction();
				return;
			}
			instruction.Op0Kind = OpKind.Memory;
			decoder.ReadOpMem(ref instruction);
		}

		// Token: 0x0400389D RID: 14493
		private readonly Code code;
	}
}
