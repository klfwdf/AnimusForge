using System;
using System.Runtime.CompilerServices;

namespace Iced.Intel.DecoderInternal
{
	// Token: 0x020006B7 RID: 1719
	[NullableContext(1)]
	[Nullable(0)]
	internal sealed class OpCodeHandler_Bitness : OpCodeHandler
	{
		// Token: 0x06002448 RID: 9288 RVA: 0x000772AB File Offset: 0x000754AB
		public OpCodeHandler_Bitness(OpCodeHandler handler1632, OpCodeHandler handler64)
		{
			this.handler1632 = handler1632;
			this.handler64 = handler64;
		}

		// Token: 0x06002449 RID: 9289 RVA: 0x000772C4 File Offset: 0x000754C4
		public override void Decode(Decoder decoder, ref Instruction instruction)
		{
			OpCodeHandler handler;
			if (decoder.is64bMode)
			{
				handler = this.handler64;
			}
			else
			{
				handler = this.handler1632;
			}
			if (handler.HasModRM)
			{
				decoder.ReadModRM();
			}
			handler.Decode(decoder, ref instruction);
		}

		// Token: 0x0400367C RID: 13948
		private readonly OpCodeHandler handler1632;

		// Token: 0x0400367D RID: 13949
		private readonly OpCodeHandler handler64;
	}
}
