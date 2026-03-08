using System;
using System.Runtime.CompilerServices;

namespace Iced.Intel.DecoderInternal
{
	// Token: 0x0200079F RID: 1951
	internal sealed class OpCodeHandler_VEX_Simple : OpCodeHandler
	{
		// Token: 0x0600262F RID: 9775 RVA: 0x00081E94 File Offset: 0x00080094
		public OpCodeHandler_VEX_Simple(Code code)
		{
			this.code = code;
		}

		// Token: 0x06002630 RID: 9776 RVA: 0x00081EA3 File Offset: 0x000800A3
		[NullableContext(1)]
		public override void Decode(Decoder decoder, ref Instruction instruction)
		{
			if ((decoder.state.vvvv_invalidCheck & decoder.invalidCheckMask) != 0U)
			{
				decoder.SetInvalidInstruction();
			}
			instruction.InternalSetCodeNoCheck(this.code);
		}

		// Token: 0x04003888 RID: 14472
		private readonly Code code;
	}
}
