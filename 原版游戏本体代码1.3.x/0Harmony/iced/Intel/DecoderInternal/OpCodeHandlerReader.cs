using System;
using System.Runtime.CompilerServices;

namespace Iced.Intel.DecoderInternal
{
	// Token: 0x020007D7 RID: 2007
	internal abstract class OpCodeHandlerReader
	{
		// Token: 0x060026A2 RID: 9890
		public abstract int ReadHandlers(ref TableDeserializer deserializer, [Nullable(new byte[] { 1, 2 })] OpCodeHandler[] result, int resultIndex);
	}
}
