using System;
using System.Runtime.CompilerServices;

namespace Iced.Intel.DecoderInternal
{
	// Token: 0x02000710 RID: 1808
	[NullableContext(1)]
	[Nullable(0)]
	internal sealed class OpCodeHandler_Reservednop : OpCodeHandlerModRM
	{
		// Token: 0x06002503 RID: 9475 RVA: 0x0007BA0D File Offset: 0x00079C0D
		public OpCodeHandler_Reservednop(OpCodeHandler reservedNopHandler, OpCodeHandler otherHandler)
		{
			if (reservedNopHandler == null)
			{
				throw new ArgumentNullException("reservedNopHandler");
			}
			this.reservedNopHandler = reservedNopHandler;
			if (otherHandler == null)
			{
				throw new ArgumentNullException("otherHandler");
			}
			this.otherHandler = otherHandler;
		}

		// Token: 0x06002504 RID: 9476 RVA: 0x0007BA41 File Offset: 0x00079C41
		public override void Decode(Decoder decoder, ref Instruction instruction)
		{
			(((decoder.options & DecoderOptions.ForceReservedNop) != DecoderOptions.None) ? this.reservedNopHandler : this.otherHandler).Decode(decoder, ref instruction);
		}

		// Token: 0x04003787 RID: 14215
		private readonly OpCodeHandler reservedNopHandler;

		// Token: 0x04003788 RID: 14216
		private readonly OpCodeHandler otherHandler;
	}
}
