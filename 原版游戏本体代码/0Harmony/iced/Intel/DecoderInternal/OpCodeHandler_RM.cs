using System;
using System.Runtime.CompilerServices;

namespace Iced.Intel.DecoderInternal
{
	// Token: 0x020006B9 RID: 1721
	[NullableContext(1)]
	[Nullable(0)]
	internal sealed class OpCodeHandler_RM : OpCodeHandlerModRM
	{
		// Token: 0x0600244C RID: 9292 RVA: 0x00077345 File Offset: 0x00075545
		public OpCodeHandler_RM(OpCodeHandler reg, OpCodeHandler mem)
		{
			if (reg == null)
			{
				throw new ArgumentNullException("reg");
			}
			this.reg = reg;
			if (mem == null)
			{
				throw new ArgumentNullException("mem");
			}
			this.mem = mem;
		}

		// Token: 0x0600244D RID: 9293 RVA: 0x00077379 File Offset: 0x00075579
		public override void Decode(Decoder decoder, ref Instruction instruction)
		{
			((decoder.state.mod == 3U) ? this.reg : this.mem).Decode(decoder, ref instruction);
		}

		// Token: 0x04003680 RID: 13952
		private readonly OpCodeHandler reg;

		// Token: 0x04003681 RID: 13953
		private readonly OpCodeHandler mem;
	}
}
