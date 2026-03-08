using System;
using System.Runtime.CompilerServices;

namespace Iced.Intel.DecoderInternal
{
	// Token: 0x020006FB RID: 1787
	internal sealed class OpCodeHandler_PrefixEsCsSsDs : OpCodeHandler
	{
		// Token: 0x060024DA RID: 9434 RVA: 0x0007B185 File Offset: 0x00079385
		public OpCodeHandler_PrefixEsCsSsDs(Register seg)
		{
			this.seg = seg;
		}

		// Token: 0x060024DB RID: 9435 RVA: 0x0007B194 File Offset: 0x00079394
		[NullableContext(1)]
		public override void Decode(Decoder decoder, ref Instruction instruction)
		{
			if (!decoder.is64bMode || decoder.state.zs.segmentPrio <= 0)
			{
				instruction.SegmentPrefix = this.seg;
			}
			decoder.ResetRexPrefixState();
			decoder.CallOpCodeHandlerXXTable(ref instruction);
		}

		// Token: 0x0400376D RID: 14189
		private readonly Register seg;
	}
}
