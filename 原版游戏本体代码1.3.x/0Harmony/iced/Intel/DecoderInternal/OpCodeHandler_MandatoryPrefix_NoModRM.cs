using System;
using System.Runtime.CompilerServices;

namespace Iced.Intel.DecoderInternal
{
	// Token: 0x0200070E RID: 1806
	[NullableContext(1)]
	[Nullable(0)]
	internal sealed class OpCodeHandler_MandatoryPrefix_NoModRM : OpCodeHandler
	{
		// Token: 0x060024FF RID: 9471 RVA: 0x0007B910 File Offset: 0x00079B10
		public OpCodeHandler_MandatoryPrefix_NoModRM(OpCodeHandler handler, OpCodeHandler handler66, OpCodeHandler handlerF3, OpCodeHandler handlerF2)
		{
			OpCodeHandler[] array = new OpCodeHandler[4];
			int num = 0;
			if (handler == null)
			{
				throw new ArgumentNullException("handler");
			}
			array[num] = handler;
			int num2 = 1;
			if (handler66 == null)
			{
				throw new ArgumentNullException("handler66");
			}
			array[num2] = handler66;
			int num3 = 2;
			if (handlerF3 == null)
			{
				throw new ArgumentNullException("handlerF3");
			}
			array[num3] = handlerF3;
			int num4 = 3;
			if (handlerF2 == null)
			{
				throw new ArgumentNullException("handlerF2");
			}
			array[num4] = handlerF2;
			this.handlers = array;
		}

		// Token: 0x06002500 RID: 9472 RVA: 0x0007B97C File Offset: 0x00079B7C
		public override void Decode(Decoder decoder, ref Instruction instruction)
		{
			decoder.ClearMandatoryPrefix(ref instruction);
			this.handlers[(int)decoder.state.zs.mandatoryPrefix].Decode(decoder, ref instruction);
		}

		// Token: 0x04003785 RID: 14213
		private readonly OpCodeHandler[] handlers;
	}
}
