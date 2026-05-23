using System;
using System.Runtime.CompilerServices;

namespace Iced.Intel.DecoderInternal
{
	// Token: 0x020006BB RID: 1723
	[NullableContext(1)]
	[Nullable(0)]
	internal sealed class OpCodeHandler_Options1632 : OpCodeHandler
	{
		// Token: 0x0600244F RID: 9295 RVA: 0x000773AE File Offset: 0x000755AE
		public OpCodeHandler_Options1632(OpCodeHandler defaultHandler, OpCodeHandler handler1, DecoderOptions options1)
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
			this.infoOptions = options1;
		}

		// Token: 0x06002450 RID: 9296 RVA: 0x000773F0 File Offset: 0x000755F0
		public OpCodeHandler_Options1632(OpCodeHandler defaultHandler, OpCodeHandler handler1, DecoderOptions options1, OpCodeHandler handler2, DecoderOptions options2)
		{
			if (defaultHandler == null)
			{
				throw new ArgumentNullException("defaultHandler");
			}
			this.defaultHandler = defaultHandler;
			HandlerOptions[] array = new HandlerOptions[2];
			int num = 0;
			if (handler1 == null)
			{
				throw new ArgumentNullException("handler1");
			}
			array[num] = new HandlerOptions(handler1, options1);
			int num2 = 1;
			if (handler2 == null)
			{
				throw new ArgumentNullException("handler2");
			}
			array[num2] = new HandlerOptions(handler2, options2);
			this.infos = array;
			this.infoOptions = options1 | options2;
		}

		// Token: 0x06002451 RID: 9297 RVA: 0x0007746C File Offset: 0x0007566C
		public override void Decode(Decoder decoder, ref Instruction instruction)
		{
			OpCodeHandler handler = this.defaultHandler;
			DecoderOptions options = decoder.options;
			if (!decoder.is64bMode && (decoder.options & this.infoOptions) != DecoderOptions.None)
			{
				foreach (HandlerOptions info in this.infos)
				{
					if ((options & info.options) != DecoderOptions.None)
					{
						handler = info.handler;
						break;
					}
				}
			}
			if (handler.HasModRM)
			{
				decoder.ReadModRM();
			}
			handler.Decode(decoder, ref instruction);
		}

		// Token: 0x04003684 RID: 13956
		private readonly OpCodeHandler defaultHandler;

		// Token: 0x04003685 RID: 13957
		private readonly HandlerOptions[] infos;

		// Token: 0x04003686 RID: 13958
		private readonly DecoderOptions infoOptions;
	}
}
