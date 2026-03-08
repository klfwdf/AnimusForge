using System;
using System.Runtime.CompilerServices;

namespace Iced.Intel.DecoderInternal
{
	// Token: 0x020006C3 RID: 1731
	[NullableContext(1)]
	[Nullable(0)]
	internal sealed class OpCodeHandler_VectorLength_EVEX : OpCodeHandlerModRM
	{
		// Token: 0x06002463 RID: 9315 RVA: 0x00077A30 File Offset: 0x00075C30
		public OpCodeHandler_VectorLength_EVEX(OpCodeHandler handler128, OpCodeHandler handler256, OpCodeHandler handler512)
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

		// Token: 0x06002464 RID: 9316 RVA: 0x00077A90 File Offset: 0x00075C90
		public override void Decode(Decoder decoder, ref Instruction instruction)
		{
			this.handlers[(int)decoder.state.vectorLength].Decode(decoder, ref instruction);
		}

		// Token: 0x040036AB RID: 13995
		private readonly OpCodeHandler[] handlers;
	}
}
