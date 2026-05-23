using System;
using System.Runtime.CompilerServices;

namespace Iced.Intel.DecoderInternal
{
	// Token: 0x0200076D RID: 1901
	internal sealed class OpCodeHandler_Eb_Gb : OpCodeHandlerModRM
	{
		// Token: 0x060025C6 RID: 9670 RVA: 0x0007FC01 File Offset: 0x0007DE01
		public OpCodeHandler_Eb_Gb(Code code)
		{
			this.code = code;
		}

		// Token: 0x060025C7 RID: 9671 RVA: 0x0007FC10 File Offset: 0x0007DE10
		public OpCodeHandler_Eb_Gb(Code code, HandlerFlags flags)
		{
			this.code = code;
			this.flags = flags;
		}

		// Token: 0x060025C8 RID: 9672 RVA: 0x0007FC28 File Offset: 0x0007DE28
		[NullableContext(1)]
		public override void Decode(Decoder decoder, ref Instruction instruction)
		{
			instruction.InternalSetCodeNoCheck(this.code);
			uint index = decoder.state.reg + decoder.state.zs.extraRegisterBase;
			if ((decoder.state.zs.flags & StateFlags.HasRex) != (StateFlags)0U && index >= 4U)
			{
				index += 4U;
			}
			instruction.Op1Register = (int)index + Register.AL;
			if (decoder.state.mod == 3U)
			{
				index = decoder.state.rm + decoder.state.zs.extraBaseRegisterBase;
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

		// Token: 0x0400383B RID: 14395
		private readonly Code code;

		// Token: 0x0400383C RID: 14396
		private readonly HandlerFlags flags;
	}
}
