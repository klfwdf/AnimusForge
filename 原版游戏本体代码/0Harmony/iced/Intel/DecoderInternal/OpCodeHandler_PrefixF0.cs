using System;
using System.Runtime.CompilerServices;

namespace Iced.Intel.DecoderInternal
{
	// Token: 0x020006FF RID: 1791
	internal sealed class OpCodeHandler_PrefixF0 : OpCodeHandler
	{
		// Token: 0x060024E3 RID: 9443 RVA: 0x0007B28F File Offset: 0x0007948F
		[NullableContext(1)]
		public override void Decode(Decoder decoder, ref Instruction instruction)
		{
			instruction.InternalSetHasLockPrefix();
			decoder.state.zs.flags = decoder.state.zs.flags | StateFlags.Lock;
			decoder.ResetRexPrefixState();
			decoder.CallOpCodeHandlerXXTable(ref instruction);
		}
	}
}
