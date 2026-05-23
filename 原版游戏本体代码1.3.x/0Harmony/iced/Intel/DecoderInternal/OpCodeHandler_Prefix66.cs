using System;
using System.Runtime.CompilerServices;

namespace Iced.Intel.DecoderInternal
{
	// Token: 0x020006FD RID: 1789
	internal sealed class OpCodeHandler_Prefix66 : OpCodeHandler
	{
		// Token: 0x060024DF RID: 9439 RVA: 0x0007B208 File Offset: 0x00079408
		[NullableContext(1)]
		public override void Decode(Decoder decoder, ref Instruction instruction)
		{
			decoder.state.zs.flags = decoder.state.zs.flags | StateFlags.Has66;
			decoder.state.operandSize = decoder.defaultInvertedOperandSize;
			if (decoder.state.zs.mandatoryPrefix == MandatoryPrefixByte.None)
			{
				decoder.state.zs.mandatoryPrefix = MandatoryPrefixByte.P66;
			}
			decoder.ResetRexPrefixState();
			decoder.CallOpCodeHandlerXXTable(ref instruction);
		}
	}
}
