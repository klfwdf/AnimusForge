using System;
using System.Runtime.CompilerServices;

namespace Iced.Intel.DecoderInternal
{
	// Token: 0x02000748 RID: 1864
	internal sealed class OpCodeHandler_Gv_Ev_Ib : OpCodeHandlerModRM
	{
		// Token: 0x0600257A RID: 9594 RVA: 0x0007E4EF File Offset: 0x0007C6EF
		public OpCodeHandler_Gv_Ev_Ib(Code code16, Code code32, Code code64)
		{
			this.codes = new Code3(code16, code32, code64);
		}

		// Token: 0x0600257B RID: 9595 RVA: 0x0007E508 File Offset: 0x0007C708
		[NullableContext(1)]
		public unsafe override void Decode(Decoder decoder, ref Instruction instruction)
		{
			UIntPtr operandSize = (UIntPtr)decoder.state.operandSize;
			instruction.InternalSetCodeNoCheck((Code)(*((ref this.codes.codes.FixedElementField) + (UIntPtr)((ulong)operandSize * 2UL))));
			instruction.Op0Register = ((int)operandSize << 4) + (int)(decoder.state.reg + decoder.state.zs.extraRegisterBase) + Register.AX;
			if (decoder.state.mod == 3U)
			{
				instruction.Op1Register = ((int)operandSize << 4) + (int)(decoder.state.rm + decoder.state.zs.extraBaseRegisterBase) + Register.AX;
			}
			else
			{
				instruction.Op1Kind = OpKind.Memory;
				decoder.ReadOpMem(ref instruction);
			}
			if ((uint)operandSize == 1U)
			{
				instruction.Op2Kind = OpKind.Immediate8to32;
				instruction.InternalImmediate8 = decoder.ReadByte();
				return;
			}
			if ((uint)operandSize == 2U)
			{
				instruction.Op2Kind = OpKind.Immediate8to64;
				instruction.InternalImmediate8 = decoder.ReadByte();
				return;
			}
			instruction.Op2Kind = OpKind.Immediate8to16;
			instruction.InternalImmediate8 = decoder.ReadByte();
		}

		// Token: 0x040037FE RID: 14334
		private readonly Code3 codes;
	}
}
