using System;
using System.Runtime.CompilerServices;

namespace Iced.Intel.DecoderInternal
{
	// Token: 0x020006B1 RID: 1713
	[NullableContext(1)]
	[Nullable(0)]
	internal sealed class OpCodeHandler_Group8x64 : OpCodeHandlerModRM
	{
		// Token: 0x0600243B RID: 9275 RVA: 0x00077013 File Offset: 0x00075213
		public OpCodeHandler_Group8x64(OpCodeHandler[] tableLow, [Nullable(new byte[] { 1, 2 })] OpCodeHandler[] tableHigh)
		{
			if (tableLow.Length != 8)
			{
				throw new ArgumentOutOfRangeException("tableLow");
			}
			if (tableHigh.Length != 64)
			{
				throw new ArgumentOutOfRangeException("tableHigh");
			}
			this.tableLow = tableLow;
			this.tableHigh = tableHigh;
		}

		// Token: 0x0600243C RID: 9276 RVA: 0x0007704C File Offset: 0x0007524C
		public override void Decode(Decoder decoder, ref Instruction instruction)
		{
			OpCodeHandler handler;
			if (decoder.state.mod == 3U)
			{
				handler = this.tableHigh[(int)(decoder.state.modrm & 63U)] ?? this.tableLow[(int)decoder.state.reg];
			}
			else
			{
				handler = this.tableLow[(int)decoder.state.reg];
			}
			handler.Decode(decoder, ref instruction);
		}

		// Token: 0x04003674 RID: 13940
		private readonly OpCodeHandler[] tableLow;

		// Token: 0x04003675 RID: 13941
		private readonly OpCodeHandler[] tableHigh;
	}
}
