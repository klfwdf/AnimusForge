using System;
using System.Runtime.CompilerServices;

namespace Iced.Intel.DecoderInternal
{
	// Token: 0x0200070D RID: 1805
	[NullableContext(1)]
	[Nullable(0)]
	internal sealed class OpCodeHandler_MandatoryPrefix4 : OpCodeHandler
	{
		// Token: 0x060024FD RID: 9469 RVA: 0x0007B7F8 File Offset: 0x000799F8
		public OpCodeHandler_MandatoryPrefix4(OpCodeHandler handlerNP, OpCodeHandler handler66, OpCodeHandler handlerF3, OpCodeHandler handlerF2, uint flags)
		{
			if (handlerNP == null)
			{
				throw new ArgumentNullException("handlerNP");
			}
			this.handlerNP = handlerNP;
			if (handler66 == null)
			{
				throw new ArgumentNullException("handler66");
			}
			this.handler66 = handler66;
			if (handlerF3 == null)
			{
				throw new ArgumentNullException("handlerF3");
			}
			this.handlerF3 = handlerF3;
			if (handlerF2 == null)
			{
				throw new ArgumentNullException("handlerF2");
			}
			this.handlerF2 = handlerF2;
			this.flags = flags;
		}

		// Token: 0x060024FE RID: 9470 RVA: 0x0007B86C File Offset: 0x00079A6C
		public override void Decode(Decoder decoder, ref Instruction instruction)
		{
			OpCodeHandler handler;
			switch (decoder.state.zs.mandatoryPrefix)
			{
			case MandatoryPrefixByte.None:
				handler = this.handlerNP;
				break;
			case MandatoryPrefixByte.P66:
				handler = this.handler66;
				break;
			case MandatoryPrefixByte.PF3:
				if ((this.flags & 4U) != 0U)
				{
					decoder.ClearMandatoryPrefixF3(ref instruction);
				}
				handler = this.handlerF3;
				break;
			case MandatoryPrefixByte.PF2:
				if ((this.flags & 8U) != 0U)
				{
					decoder.ClearMandatoryPrefixF2(ref instruction);
				}
				handler = this.handlerF2;
				break;
			default:
				throw new InvalidOperationException();
			}
			if (handler.HasModRM && (this.flags & 16U) != 0U)
			{
				decoder.ReadModRM();
			}
			handler.Decode(decoder, ref instruction);
		}

		// Token: 0x04003780 RID: 14208
		private readonly OpCodeHandler handlerNP;

		// Token: 0x04003781 RID: 14209
		private readonly OpCodeHandler handler66;

		// Token: 0x04003782 RID: 14210
		private readonly OpCodeHandler handlerF3;

		// Token: 0x04003783 RID: 14211
		private readonly OpCodeHandler handlerF2;

		// Token: 0x04003784 RID: 14212
		private readonly uint flags;
	}
}
