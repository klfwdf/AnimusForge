using System;
using System.Runtime.CompilerServices;

namespace Iced.Intel.DecoderInternal
{
	// Token: 0x020006BD RID: 1725
	[NullableContext(1)]
	[Nullable(0)]
	internal sealed class OpCodeHandler_Options_DontReadModRM : OpCodeHandlerModRM
	{
		// Token: 0x06002455 RID: 9301 RVA: 0x00077617 File Offset: 0x00075817
		public OpCodeHandler_Options_DontReadModRM(OpCodeHandler defaultHandler, OpCodeHandler handler1, DecoderOptions options1)
		{
			if (defaultHandler == null)
			{
				throw new ArgumentNullException("defaultHandler");
			}
			this.defaultHandler = defaultHandler;
			this.infos = new HandlerOptions[]
			{
				new HandlerOptions(handler1, options1)
			};
		}

		// Token: 0x06002456 RID: 9302 RVA: 0x00077650 File Offset: 0x00075850
		public override void Decode(Decoder decoder, ref Instruction instruction)
		{
			OpCodeHandler handler = this.defaultHandler;
			DecoderOptions options = decoder.options;
			foreach (HandlerOptions info in this.infos)
			{
				if ((options & info.options) != DecoderOptions.None)
				{
					handler = info.handler;
					break;
				}
			}
			handler.Decode(decoder, ref instruction);
		}

		// Token: 0x0400368A RID: 13962
		private readonly OpCodeHandler defaultHandler;

		// Token: 0x0400368B RID: 13963
		private readonly HandlerOptions[] infos;
	}
}
