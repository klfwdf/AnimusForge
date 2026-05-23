using System;
using System.Runtime.CompilerServices;

namespace Iced.Intel.EncoderInternal
{
	// Token: 0x02000686 RID: 1670
	internal static class OpCodeHandlers
	{
		// Token: 0x060023DD RID: 9181 RVA: 0x00073C30 File Offset: 0x00071E30
		static OpCodeHandlers()
		{
			uint[] encFlags = EncoderData.EncFlags1;
			uint[] encFlags2 = EncoderData.EncFlags2;
			uint[] encFlags3Data = EncoderData.EncFlags3;
			OpCodeHandler[] handlers = new OpCodeHandler[4936];
			int i = 0;
			InvalidHandler invalidHandler = new InvalidHandler();
			while (i < encFlags.Length)
			{
				EncFlags3 encFlags3 = (EncFlags3)encFlags3Data[i];
				OpCodeHandler handler;
				switch (encFlags3 & EncFlags3.EncodingMask)
				{
				case EncFlags3.None:
				{
					Code code = (Code)i;
					if (code == Code.INVALID)
					{
						handler = invalidHandler;
					}
					else if (code <= Code.DeclareQword)
					{
						handler = new DeclareDataHandler(code);
					}
					else if (code == Code.Zero_bytes)
					{
						handler = new ZeroBytesHandler(code);
					}
					else
					{
						handler = new LegacyHandler((EncFlags1)encFlags[i], (EncFlags2)encFlags2[i], encFlags3);
					}
					break;
				}
				case (EncFlags3)1U:
					handler = new VexHandler((EncFlags1)encFlags[i], (EncFlags2)encFlags2[i], encFlags3);
					break;
				case (EncFlags3)2U:
					handler = new EvexHandler((EncFlags1)encFlags[i], (EncFlags2)encFlags2[i], encFlags3);
					break;
				case EncFlags3.OperandSizeShift:
					handler = new XopHandler((EncFlags1)encFlags[i], (EncFlags2)encFlags2[i], encFlags3);
					break;
				case (EncFlags3)4U:
					handler = new D3nowHandler((EncFlags2)encFlags2[i], encFlags3);
					break;
				case EncFlags3.AddressSizeShift:
					handler = invalidHandler;
					break;
				default:
					throw new InvalidOperationException();
				}
				handlers[i] = handler;
				i++;
			}
			if (i != handlers.Length)
			{
				throw new InvalidOperationException();
			}
			OpCodeHandlers.Handlers = handlers;
		}

		// Token: 0x04003517 RID: 13591
		[Nullable(1)]
		public static readonly OpCodeHandler[] Handlers;
	}
}
