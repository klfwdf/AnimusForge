using System;
using System.Runtime.CompilerServices;

namespace Iced.Intel.DecoderInternal
{
	// Token: 0x020006B0 RID: 1712
	[NullableContext(1)]
	[Nullable(0)]
	internal sealed class OpCodeHandler_Group8x8 : OpCodeHandlerModRM
	{
		// Token: 0x06002439 RID: 9273 RVA: 0x00076F8F File Offset: 0x0007518F
		public OpCodeHandler_Group8x8(OpCodeHandler[] tableLow, OpCodeHandler[] tableHigh)
		{
			if (tableLow.Length != 8)
			{
				throw new ArgumentOutOfRangeException("tableLow");
			}
			if (tableHigh.Length != 8)
			{
				throw new ArgumentOutOfRangeException("tableHigh");
			}
			this.tableLow = tableLow;
			this.tableHigh = tableHigh;
		}

		// Token: 0x0600243A RID: 9274 RVA: 0x00076FC8 File Offset: 0x000751C8
		public override void Decode(Decoder decoder, ref Instruction instruction)
		{
			OpCodeHandler handler;
			if (decoder.state.mod == 3U)
			{
				handler = this.tableHigh[(int)decoder.state.reg];
			}
			else
			{
				handler = this.tableLow[(int)decoder.state.reg];
			}
			handler.Decode(decoder, ref instruction);
		}

		// Token: 0x04003672 RID: 13938
		private readonly OpCodeHandler[] tableLow;

		// Token: 0x04003673 RID: 13939
		private readonly OpCodeHandler[] tableHigh;
	}
}
