using System;
using System.Runtime.CompilerServices;

namespace Iced.Intel.DecoderInternal
{
	// Token: 0x02000701 RID: 1793
	internal sealed class OpCodeHandler_PrefixF3 : OpCodeHandler
	{
		// Token: 0x060024E7 RID: 9447 RVA: 0x0007B2E3 File Offset: 0x000794E3
		[NullableContext(1)]
		public override void Decode(Decoder decoder, ref Instruction instruction)
		{
			instruction.InternalSetHasRepePrefix();
			decoder.state.zs.mandatoryPrefix = MandatoryPrefixByte.PF3;
			decoder.ResetRexPrefixState();
			decoder.CallOpCodeHandlerXXTable(ref instruction);
		}
	}
}
