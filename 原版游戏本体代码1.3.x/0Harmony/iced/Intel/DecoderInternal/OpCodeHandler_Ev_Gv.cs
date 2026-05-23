using System;
using System.Runtime.CompilerServices;

namespace Iced.Intel.DecoderInternal
{
	// Token: 0x0200072F RID: 1839
	internal sealed class OpCodeHandler_Ev_Gv : OpCodeHandlerModRM
	{
		// Token: 0x06002545 RID: 9541 RVA: 0x0007D3FC File Offset: 0x0007B5FC
		public OpCodeHandler_Ev_Gv(Code code16, Code code32, Code code64)
		{
			this.codes = new Code3(code16, code32, code64);
		}

		// Token: 0x06002546 RID: 9542 RVA: 0x0007D412 File Offset: 0x0007B612
		public OpCodeHandler_Ev_Gv(Code code16, Code code32, Code code64, HandlerFlags flags)
		{
			this.codes = new Code3(code16, code32, code64);
			this.flags = flags;
		}

		// Token: 0x06002547 RID: 9543 RVA: 0x0007D430 File Offset: 0x0007B630
		[NullableContext(1)]
		public unsafe override void Decode(Decoder decoder, ref Instruction instruction)
		{
			UIntPtr operandSize = (UIntPtr)decoder.state.operandSize;
			instruction.InternalSetCodeNoCheck((Code)(*((ref this.codes.codes.FixedElementField) + (UIntPtr)((ulong)operandSize * 2UL))));
			instruction.Op1Register = ((int)operandSize << 4) + (int)(decoder.state.reg + decoder.state.zs.extraRegisterBase) + Register.AX;
			if (decoder.state.mod == 3U)
			{
				instruction.Op0Register = ((int)operandSize << 4) + (int)(decoder.state.rm + decoder.state.zs.extraBaseRegisterBase) + Register.AX;
				return;
			}
			decoder.state.zs.flags = decoder.state.zs.flags | (StateFlags)((this.flags & HandlerFlags.Lock) << 10);
			instruction.Op0Kind = OpKind.Memory;
			decoder.ReadOpMem(ref instruction);
		}

		// Token: 0x040037CC RID: 14284
		private readonly Code3 codes;

		// Token: 0x040037CD RID: 14285
		private readonly HandlerFlags flags;
	}
}
