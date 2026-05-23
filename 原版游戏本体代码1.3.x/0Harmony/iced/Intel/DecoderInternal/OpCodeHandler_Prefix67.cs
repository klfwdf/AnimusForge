using System;
using System.Runtime.CompilerServices;

namespace Iced.Intel.DecoderInternal
{
	// Token: 0x020006FE RID: 1790
	internal sealed class OpCodeHandler_Prefix67 : OpCodeHandler
	{
		// Token: 0x060024E1 RID: 9441 RVA: 0x0007B26F File Offset: 0x0007946F
		[NullableContext(1)]
		public override void Decode(Decoder decoder, ref Instruction instruction)
		{
			decoder.state.addressSize = decoder.defaultInvertedAddressSize;
			decoder.ResetRexPrefixState();
			decoder.CallOpCodeHandlerXXTable(ref instruction);
		}
	}
}
