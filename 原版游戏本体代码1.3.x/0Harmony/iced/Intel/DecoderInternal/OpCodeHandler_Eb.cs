using System;
using System.Runtime.CompilerServices;

namespace Iced.Intel.DecoderInternal
{
	// Token: 0x0200076C RID: 1900
	internal sealed class OpCodeHandler_Eb : OpCodeHandlerModRM
	{
		// Token: 0x060025C3 RID: 9667 RVA: 0x0007FB41 File Offset: 0x0007DD41
		public OpCodeHandler_Eb(Code code)
		{
			this.code = code;
		}

		// Token: 0x060025C4 RID: 9668 RVA: 0x0007FB50 File Offset: 0x0007DD50
		public OpCodeHandler_Eb(Code code, HandlerFlags flags)
		{
			this.code = code;
			this.flags = flags;
		}

		// Token: 0x060025C5 RID: 9669 RVA: 0x0007FB68 File Offset: 0x0007DD68
		[NullableContext(1)]
		public override void Decode(Decoder decoder, ref Instruction instruction)
		{
			instruction.InternalSetCodeNoCheck(this.code);
			if (decoder.state.mod == 3U)
			{
				uint index = decoder.state.rm + decoder.state.zs.extraBaseRegisterBase;
				if ((decoder.state.zs.flags & StateFlags.HasRex) != (StateFlags)0U && index >= 4U)
				{
					index += 4U;
				}
				instruction.Op0Register = (int)index + Register.AL;
				return;
			}
			decoder.state.zs.flags = decoder.state.zs.flags | (StateFlags)((this.flags & HandlerFlags.Lock) << 10);
			instruction.Op0Kind = OpKind.Memory;
			decoder.ReadOpMem(ref instruction);
		}

		// Token: 0x04003839 RID: 14393
		private readonly Code code;

		// Token: 0x0400383A RID: 14394
		private readonly HandlerFlags flags;
	}
}
