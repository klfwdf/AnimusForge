using System;
using System.Runtime.CompilerServices;

namespace Iced.Intel.DecoderInternal
{
	// Token: 0x02000709 RID: 1801
	internal sealed class OpCodeHandler_Ib3 : OpCodeHandlerModRM
	{
		// Token: 0x060024F6 RID: 9462 RVA: 0x0007B576 File Offset: 0x00079776
		public OpCodeHandler_Ib3(Code code)
		{
			this.code = code;
		}

		// Token: 0x060024F7 RID: 9463 RVA: 0x0007B585 File Offset: 0x00079785
		[NullableContext(1)]
		public override void Decode(Decoder decoder, ref Instruction instruction)
		{
			instruction.InternalSetCodeNoCheck(this.code);
			instruction.Op0Kind = OpKind.Immediate8;
			instruction.InternalImmediate8 = decoder.ReadByte();
		}

		// Token: 0x0400377A RID: 14202
		private readonly Code code;
	}
}
