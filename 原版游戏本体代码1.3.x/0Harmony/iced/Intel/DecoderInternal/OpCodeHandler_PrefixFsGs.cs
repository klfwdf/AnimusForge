using System;
using System.Runtime.CompilerServices;

namespace Iced.Intel.DecoderInternal
{
	// Token: 0x020006FC RID: 1788
	internal sealed class OpCodeHandler_PrefixFsGs : OpCodeHandler
	{
		// Token: 0x060024DC RID: 9436 RVA: 0x0007B1CA File Offset: 0x000793CA
		public OpCodeHandler_PrefixFsGs(Register seg)
		{
			this.seg = seg;
		}

		// Token: 0x060024DD RID: 9437 RVA: 0x0007B1D9 File Offset: 0x000793D9
		[NullableContext(1)]
		public override void Decode(Decoder decoder, ref Instruction instruction)
		{
			instruction.SegmentPrefix = this.seg;
			decoder.state.zs.segmentPrio = 1;
			decoder.ResetRexPrefixState();
			decoder.CallOpCodeHandlerXXTable(ref instruction);
		}

		// Token: 0x0400376E RID: 14190
		private readonly Register seg;
	}
}
