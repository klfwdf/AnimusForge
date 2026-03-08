using System;
using System.Runtime.CompilerServices;

namespace Iced.Intel.DecoderInternal
{
	// Token: 0x020006AE RID: 1710
	internal sealed class OpCodeHandler_Simple : OpCodeHandler
	{
		// Token: 0x06002435 RID: 9269 RVA: 0x00076F55 File Offset: 0x00075155
		public OpCodeHandler_Simple(Code code)
		{
			this.code = code;
		}

		// Token: 0x06002436 RID: 9270 RVA: 0x00076F64 File Offset: 0x00075164
		[NullableContext(1)]
		public override void Decode(Decoder decoder, ref Instruction instruction)
		{
			instruction.InternalSetCodeNoCheck(this.code);
		}

		// Token: 0x04003670 RID: 13936
		private readonly Code code;
	}
}
