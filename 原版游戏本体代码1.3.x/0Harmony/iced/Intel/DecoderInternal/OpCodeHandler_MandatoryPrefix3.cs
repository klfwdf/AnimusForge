using System;
using System.Runtime.CompilerServices;

namespace Iced.Intel.DecoderInternal
{
	// Token: 0x0200070B RID: 1803
	[NullableContext(1)]
	[Nullable(0)]
	internal sealed class OpCodeHandler_MandatoryPrefix3 : OpCodeHandlerModRM
	{
		// Token: 0x060024FA RID: 9466 RVA: 0x0007B63C File Offset: 0x0007983C
		public OpCodeHandler_MandatoryPrefix3(OpCodeHandler handler_reg, OpCodeHandler handler_mem, OpCodeHandler handler66_reg, OpCodeHandler handler66_mem, OpCodeHandler handlerF3_reg, OpCodeHandler handlerF3_mem, OpCodeHandler handlerF2_reg, OpCodeHandler handlerF2_mem, LegacyHandlerFlags flags)
		{
			OpCodeHandler_MandatoryPrefix3.Info[] array = new OpCodeHandler_MandatoryPrefix3.Info[4];
			int num = 0;
			if (handler_reg == null)
			{
				throw new ArgumentNullException("handler_reg");
			}
			array[num] = new OpCodeHandler_MandatoryPrefix3.Info(handler_reg, (flags & LegacyHandlerFlags.HandlerReg) == (LegacyHandlerFlags)0U);
			int num2 = 1;
			if (handler66_reg == null)
			{
				throw new ArgumentNullException("handler66_reg");
			}
			array[num2] = new OpCodeHandler_MandatoryPrefix3.Info(handler66_reg, (flags & LegacyHandlerFlags.Handler66Reg) == (LegacyHandlerFlags)0U);
			int num3 = 2;
			if (handlerF3_reg == null)
			{
				throw new ArgumentNullException("handlerF3_reg");
			}
			array[num3] = new OpCodeHandler_MandatoryPrefix3.Info(handlerF3_reg, (flags & LegacyHandlerFlags.HandlerF3Reg) == (LegacyHandlerFlags)0U);
			int num4 = 3;
			if (handlerF2_reg == null)
			{
				throw new ArgumentNullException("handlerF2_reg");
			}
			array[num4] = new OpCodeHandler_MandatoryPrefix3.Info(handlerF2_reg, (flags & LegacyHandlerFlags.HandlerF2Reg) == (LegacyHandlerFlags)0U);
			this.handlers_reg = array;
			OpCodeHandler_MandatoryPrefix3.Info[] array2 = new OpCodeHandler_MandatoryPrefix3.Info[4];
			int num5 = 0;
			if (handler_mem == null)
			{
				throw new ArgumentNullException("handler_mem");
			}
			array2[num5] = new OpCodeHandler_MandatoryPrefix3.Info(handler_mem, (flags & LegacyHandlerFlags.HandlerMem) == (LegacyHandlerFlags)0U);
			int num6 = 1;
			if (handler66_mem == null)
			{
				throw new ArgumentNullException("handler66_mem");
			}
			array2[num6] = new OpCodeHandler_MandatoryPrefix3.Info(handler66_mem, (flags & LegacyHandlerFlags.Handler66Mem) == (LegacyHandlerFlags)0U);
			int num7 = 2;
			if (handlerF3_mem == null)
			{
				throw new ArgumentNullException("handlerF3_mem");
			}
			array2[num7] = new OpCodeHandler_MandatoryPrefix3.Info(handlerF3_mem, (flags & LegacyHandlerFlags.HandlerF3Mem) == (LegacyHandlerFlags)0U);
			int num8 = 3;
			if (handlerF2_mem == null)
			{
				throw new ArgumentNullException("handlerF2_mem");
			}
			array2[num8] = new OpCodeHandler_MandatoryPrefix3.Info(handlerF2_mem, (flags & LegacyHandlerFlags.HandlerF2Mem) == (LegacyHandlerFlags)0U);
			this.handlers_mem = array2;
		}

		// Token: 0x060024FB RID: 9467 RVA: 0x0007B78C File Offset: 0x0007998C
		public override void Decode(Decoder decoder, ref Instruction instruction)
		{
			OpCodeHandler_MandatoryPrefix3.Info info = ((decoder.state.mod == 3U) ? this.handlers_reg : this.handlers_mem)[(int)decoder.state.zs.mandatoryPrefix];
			if (info.mandatoryPrefix)
			{
				decoder.ClearMandatoryPrefix(ref instruction);
			}
			info.handler.Decode(decoder, ref instruction);
		}

		// Token: 0x0400377C RID: 14204
		private readonly OpCodeHandler_MandatoryPrefix3.Info[] handlers_reg;

		// Token: 0x0400377D RID: 14205
		private readonly OpCodeHandler_MandatoryPrefix3.Info[] handlers_mem;

		// Token: 0x0200070C RID: 1804
		private readonly struct Info
		{
			// Token: 0x060024FC RID: 9468 RVA: 0x0007B7E5 File Offset: 0x000799E5
			public Info(OpCodeHandler handler, bool mandatoryPrefix)
			{
				this.handler = handler;
				this.mandatoryPrefix = mandatoryPrefix;
			}

			// Token: 0x0400377E RID: 14206
			public readonly OpCodeHandler handler;

			// Token: 0x0400377F RID: 14207
			public readonly bool mandatoryPrefix;
		}
	}
}
