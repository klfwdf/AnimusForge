using System;
using System.Runtime.CompilerServices;

namespace Iced.Intel.DecoderInternal
{
	// Token: 0x020006B4 RID: 1716
	[NullableContext(1)]
	[Nullable(0)]
	internal sealed class OpCodeHandler_MandatoryPrefix2 : OpCodeHandlerModRM
	{
		// Token: 0x06002441 RID: 9281 RVA: 0x00077116 File Offset: 0x00075316
		public OpCodeHandler_MandatoryPrefix2(OpCodeHandler handler)
			: this(handler, OpCodeHandler_Invalid.Instance, OpCodeHandler_Invalid.Instance, OpCodeHandler_Invalid.Instance)
		{
		}

		// Token: 0x06002442 RID: 9282 RVA: 0x00077130 File Offset: 0x00075330
		public OpCodeHandler_MandatoryPrefix2(OpCodeHandler handler, OpCodeHandler handler66, OpCodeHandler handlerF3, OpCodeHandler handlerF2)
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

		// Token: 0x06002443 RID: 9283 RVA: 0x0007719C File Offset: 0x0007539C
		public override void Decode(Decoder decoder, ref Instruction instruction)
		{
			this.handlers[(int)decoder.state.zs.mandatoryPrefix].Decode(decoder, ref instruction);
		}

		// Token: 0x04003678 RID: 13944
		private readonly OpCodeHandler[] handlers;
	}
}
