using System;
using System.Runtime.CompilerServices;

namespace Iced.Intel.DecoderInternal
{
	// Token: 0x0200073D RID: 1853
	internal sealed class OpCodeHandler_Simple4 : OpCodeHandler
	{
		// Token: 0x06002562 RID: 9570 RVA: 0x0007DCDE File Offset: 0x0007BEDE
		public OpCodeHandler_Simple4(Code code32, Code code64)
		{
			this.code32 = code32;
			this.code64 = code64;
		}

		// Token: 0x06002563 RID: 9571 RVA: 0x0007DCF4 File Offset: 0x0007BEF4
		[NullableContext(1)]
		public override void Decode(Decoder decoder, ref Instruction instruction)
		{
			if ((decoder.state.zs.flags & StateFlags.W) != (StateFlags)0U)
			{
				instruction.InternalSetCodeNoCheck(this.code64);
				return;
			}
			instruction.InternalSetCodeNoCheck(this.code32);
		}

		// Token: 0x040037E2 RID: 14306
		private readonly Code code32;

		// Token: 0x040037E3 RID: 14307
		private readonly Code code64;
	}
}
