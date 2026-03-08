using System;
using System.Runtime.CompilerServices;

namespace Iced.Intel.DecoderInternal
{
	// Token: 0x020006AA RID: 1706
	internal abstract class OpCodeHandler
	{
		// Token: 0x0600242B RID: 9259 RVA: 0x00002B15 File Offset: 0x00000D15
		protected OpCodeHandler()
		{
		}

		// Token: 0x0600242C RID: 9260 RVA: 0x00076F0D File Offset: 0x0007510D
		protected OpCodeHandler(bool hasModRM)
		{
			this.HasModRM = hasModRM;
		}

		// Token: 0x0600242D RID: 9261
		[NullableContext(1)]
		public abstract void Decode(Decoder decoder, ref Instruction instruction);

		// Token: 0x0400366D RID: 13933
		public readonly bool HasModRM;
	}
}
