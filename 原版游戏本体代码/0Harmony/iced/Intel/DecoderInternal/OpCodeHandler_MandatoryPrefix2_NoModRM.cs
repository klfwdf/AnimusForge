using System;
using System.Runtime.CompilerServices;

namespace Iced.Intel.DecoderInternal
{
	// Token: 0x020006B5 RID: 1717
	[NullableContext(1)]
	[Nullable(0)]
	internal sealed class OpCodeHandler_MandatoryPrefix2_NoModRM : OpCodeHandler
	{
		// Token: 0x06002444 RID: 9284 RVA: 0x000771BC File Offset: 0x000753BC
		public OpCodeHandler_MandatoryPrefix2_NoModRM(OpCodeHandler handler, OpCodeHandler handler66, OpCodeHandler handlerF3, OpCodeHandler handlerF2)
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

		// Token: 0x06002445 RID: 9285 RVA: 0x00077228 File Offset: 0x00075428
		public override void Decode(Decoder decoder, ref Instruction instruction)
		{
			this.handlers[(int)decoder.state.zs.mandatoryPrefix].Decode(decoder, ref instruction);
		}

		// Token: 0x04003679 RID: 13945
		private readonly OpCodeHandler[] handlers;
	}
}
