using System;
using System.Runtime.CompilerServices;

namespace Iced.Intel.DecoderInternal
{
	// Token: 0x02000700 RID: 1792
	internal sealed class OpCodeHandler_PrefixF2 : OpCodeHandler
	{
		// Token: 0x060024E5 RID: 9445 RVA: 0x0007B2BD File Offset: 0x000794BD
		[NullableContext(1)]
		public override void Decode(Decoder decoder, ref Instruction instruction)
		{
			instruction.InternalSetHasRepnePrefix();
			decoder.state.zs.mandatoryPrefix = MandatoryPrefixByte.PF2;
			decoder.ResetRexPrefixState();
			decoder.CallOpCodeHandlerXXTable(ref instruction);
		}
	}
}
