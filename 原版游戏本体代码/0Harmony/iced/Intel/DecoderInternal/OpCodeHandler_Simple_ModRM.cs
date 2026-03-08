using System;
using System.Runtime.CompilerServices;

namespace Iced.Intel.DecoderInternal
{
	// Token: 0x020006AF RID: 1711
	internal sealed class OpCodeHandler_Simple_ModRM : OpCodeHandlerModRM
	{
		// Token: 0x06002437 RID: 9271 RVA: 0x00076F72 File Offset: 0x00075172
		public OpCodeHandler_Simple_ModRM(Code code)
		{
			this.code = code;
		}

		// Token: 0x06002438 RID: 9272 RVA: 0x00076F81 File Offset: 0x00075181
		[NullableContext(1)]
		public override void Decode(Decoder decoder, ref Instruction instruction)
		{
			instruction.InternalSetCodeNoCheck(this.code);
		}

		// Token: 0x04003671 RID: 13937
		private readonly Code code;
	}
}
