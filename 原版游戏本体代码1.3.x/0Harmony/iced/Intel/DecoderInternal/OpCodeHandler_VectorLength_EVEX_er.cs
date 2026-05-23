using System;
using System.Runtime.CompilerServices;

namespace Iced.Intel.DecoderInternal
{
	// Token: 0x020006C4 RID: 1732
	[NullableContext(1)]
	[Nullable(0)]
	internal sealed class OpCodeHandler_VectorLength_EVEX_er : OpCodeHandlerModRM
	{
		// Token: 0x06002465 RID: 9317 RVA: 0x00077AAC File Offset: 0x00075CAC
		public OpCodeHandler_VectorLength_EVEX_er(OpCodeHandler handler128, OpCodeHandler handler256, OpCodeHandler handler512)
		{
			OpCodeHandler[] array = new OpCodeHandler[4];
			int num = 0;
			if (handler128 == null)
			{
				throw new ArgumentNullException("handler128");
			}
			array[num] = handler128;
			int num2 = 1;
			if (handler256 == null)
			{
				throw new ArgumentNullException("handler256");
			}
			array[num2] = handler256;
			int num3 = 2;
			if (handler512 == null)
			{
				throw new ArgumentNullException("handler512");
			}
			array[num3] = handler512;
			array[3] = OpCodeHandler_Invalid.Instance;
			this.handlers = array;
		}

		// Token: 0x06002466 RID: 9318 RVA: 0x00077B0C File Offset: 0x00075D0C
		public override void Decode(Decoder decoder, ref Instruction instruction)
		{
			int index = (int)decoder.state.vectorLength;
			if (decoder.state.mod == 3U && (decoder.state.zs.flags & StateFlags.b) != (StateFlags)0U)
			{
				index = 2;
			}
			this.handlers[index].Decode(decoder, ref instruction);
		}

		// Token: 0x040036AC RID: 13996
		private readonly OpCodeHandler[] handlers;
	}
}
