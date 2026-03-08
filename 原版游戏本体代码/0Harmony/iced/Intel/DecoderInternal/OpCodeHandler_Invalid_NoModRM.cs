using System;
using System.Runtime.CompilerServices;

namespace Iced.Intel.DecoderInternal
{
	// Token: 0x020006AD RID: 1709
	[NullableContext(1)]
	[Nullable(0)]
	internal sealed class OpCodeHandler_Invalid_NoModRM : OpCodeHandler
	{
		// Token: 0x06002432 RID: 9266 RVA: 0x00076F41 File Offset: 0x00075141
		private OpCodeHandler_Invalid_NoModRM()
		{
		}

		// Token: 0x06002433 RID: 9267 RVA: 0x00076F2D File Offset: 0x0007512D
		public override void Decode(Decoder decoder, ref Instruction instruction)
		{
			decoder.SetInvalidInstruction();
		}

		// Token: 0x0400366F RID: 13935
		public static readonly OpCodeHandler_Invalid_NoModRM Instance = new OpCodeHandler_Invalid_NoModRM();
	}
}
