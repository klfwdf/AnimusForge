using System;
using System.Runtime.CompilerServices;

namespace Iced.Intel.DecoderInternal
{
	// Token: 0x020006B2 RID: 1714
	[NullableContext(1)]
	[Nullable(0)]
	internal sealed class OpCodeHandler_Group : OpCodeHandlerModRM
	{
		// Token: 0x0600243D RID: 9277 RVA: 0x000770B0 File Offset: 0x000752B0
		public OpCodeHandler_Group(OpCodeHandler[] groupHandlers)
		{
			if (groupHandlers == null)
			{
				throw new ArgumentNullException("groupHandlers");
			}
			this.groupHandlers = groupHandlers;
		}

		// Token: 0x0600243E RID: 9278 RVA: 0x000770CE File Offset: 0x000752CE
		public override void Decode(Decoder decoder, ref Instruction instruction)
		{
			this.groupHandlers[(int)decoder.state.reg].Decode(decoder, ref instruction);
		}

		// Token: 0x04003676 RID: 13942
		private readonly OpCodeHandler[] groupHandlers;
	}
}
