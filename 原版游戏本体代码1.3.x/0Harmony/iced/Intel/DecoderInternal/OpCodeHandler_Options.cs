using System;
using System.Runtime.CompilerServices;

namespace Iced.Intel.DecoderInternal
{
	// Token: 0x020006BC RID: 1724
	[NullableContext(1)]
	[Nullable(0)]
	internal sealed class OpCodeHandler_Options : OpCodeHandler
	{
		// Token: 0x06002452 RID: 9298 RVA: 0x000774E7 File Offset: 0x000756E7
		public OpCodeHandler_Options(OpCodeHandler defaultHandler, OpCodeHandler handler1, DecoderOptions options1)
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

		// Token: 0x06002453 RID: 9299 RVA: 0x00077528 File Offset: 0x00075728
		public OpCodeHandler_Options(OpCodeHandler defaultHandler, OpCodeHandler handler1, DecoderOptions options1, OpCodeHandler handler2, DecoderOptions options2)
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

		// Token: 0x06002454 RID: 9300 RVA: 0x000775A4 File Offset: 0x000757A4
		public override void Decode(Decoder decoder, ref Instruction instruction)
		{
			OpCodeHandler handler = this.defaultHandler;
			DecoderOptions options = decoder.options;
			if ((decoder.options & this.infoOptions) != DecoderOptions.None)
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

		// Token: 0x04003687 RID: 13959
		private readonly OpCodeHandler defaultHandler;

		// Token: 0x04003688 RID: 13960
		private readonly HandlerOptions[] infos;

		// Token: 0x04003689 RID: 13961
		private readonly DecoderOptions infoOptions;
	}
}
