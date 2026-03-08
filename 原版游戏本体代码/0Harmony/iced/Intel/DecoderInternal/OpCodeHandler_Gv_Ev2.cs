using System;
using System.Runtime.CompilerServices;

namespace Iced.Intel.DecoderInternal
{
	// Token: 0x02000725 RID: 1829
	internal sealed class OpCodeHandler_Gv_Ev2 : OpCodeHandlerModRM
	{
		// Token: 0x06002531 RID: 9521 RVA: 0x0007C973 File Offset: 0x0007AB73
		public OpCodeHandler_Gv_Ev2(Code code16, Code code32, Code code64)
		{
			this.codes = new Code3(code16, code32, code64);
		}

		// Token: 0x06002532 RID: 9522 RVA: 0x0007C98C File Offset: 0x0007AB8C
		[NullableContext(1)]
		public unsafe override void Decode(Decoder decoder, ref Instruction instruction)
		{
			UIntPtr operandSize = (UIntPtr)decoder.state.operandSize;
			instruction.InternalSetCodeNoCheck((Code)(*((ref this.codes.codes.FixedElementField) + (UIntPtr)((ulong)operandSize * 2UL))));
			instruction.Op0Register = ((int)operandSize << 4) + (int)(decoder.state.reg + decoder.state.zs.extraRegisterBase) + Register.AX;
			if (decoder.state.mod != 3U)
			{
				instruction.Op1Kind = OpKind.Memory;
				decoder.ReadOpMem(ref instruction);
				return;
			}
			uint index = decoder.state.rm + decoder.state.zs.extraBaseRegisterBase;
			if (decoder.state.operandSize != OpSize.Size16)
			{
				instruction.Op1Register = (int)index + Register.EAX;
				return;
			}
			instruction.Op1Register = (int)index + Register.AX;
		}

		// Token: 0x040037AC RID: 14252
		private readonly Code3 codes;
	}
}
