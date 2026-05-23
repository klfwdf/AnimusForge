using System;
using System.Runtime.CompilerServices;

namespace Iced.Intel.DecoderInternal
{
	// Token: 0x0200079E RID: 1950
	[NullableContext(1)]
	[Nullable(0)]
	internal sealed class OpCodeHandler_VectorLength_NoModRM_VEX : OpCodeHandler
	{
		// Token: 0x0600262D RID: 9773 RVA: 0x00081E24 File Offset: 0x00080024
		public OpCodeHandler_VectorLength_NoModRM_VEX(OpCodeHandler handler128, OpCodeHandler handler256)
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
			array[2] = OpCodeHandler_Invalid.Instance;
			array[3] = OpCodeHandler_Invalid.Instance;
			this.handlers = array;
		}

		// Token: 0x0600262E RID: 9774 RVA: 0x00081E79 File Offset: 0x00080079
		public override void Decode(Decoder decoder, ref Instruction instruction)
		{
			this.handlers[(int)decoder.state.vectorLength].Decode(decoder, ref instruction);
		}

		// Token: 0x04003887 RID: 14471
		private readonly OpCodeHandler[] handlers;
	}
}
