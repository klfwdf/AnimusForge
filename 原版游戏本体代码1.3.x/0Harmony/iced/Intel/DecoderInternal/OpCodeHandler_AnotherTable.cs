using System;
using System.Runtime.CompilerServices;

namespace Iced.Intel.DecoderInternal
{
	// Token: 0x020006B3 RID: 1715
	[NullableContext(1)]
	[Nullable(0)]
	internal sealed class OpCodeHandler_AnotherTable : OpCodeHandler
	{
		// Token: 0x0600243F RID: 9279 RVA: 0x000770E9 File Offset: 0x000752E9
		public OpCodeHandler_AnotherTable(OpCodeHandler[] otherTable)
		{
			if (otherTable == null)
			{
				throw new ArgumentNullException("otherTable");
			}
			this.otherTable = otherTable;
		}

		// Token: 0x06002440 RID: 9280 RVA: 0x00077107 File Offset: 0x00075307
		public override void Decode(Decoder decoder, ref Instruction instruction)
		{
			decoder.DecodeTable(this.otherTable, ref instruction);
		}

		// Token: 0x04003677 RID: 13943
		private readonly OpCodeHandler[] otherTable;
	}
}
