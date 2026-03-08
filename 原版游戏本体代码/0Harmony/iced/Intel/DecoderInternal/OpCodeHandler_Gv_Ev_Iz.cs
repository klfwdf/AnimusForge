using System;
using System.Runtime.CompilerServices;

namespace Iced.Intel.DecoderInternal
{
	// Token: 0x0200074B RID: 1867
	internal sealed class OpCodeHandler_Gv_Ev_Iz : OpCodeHandlerModRM
	{
		// Token: 0x06002580 RID: 9600 RVA: 0x0007E7CE File Offset: 0x0007C9CE
		public OpCodeHandler_Gv_Ev_Iz(Code code16, Code code32, Code code64)
		{
			this.codes = new Code3(code16, code32, code64);
		}

		// Token: 0x06002581 RID: 9601 RVA: 0x0007E7E4 File Offset: 0x0007C9E4
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
				instruction.Op2Kind = OpKind.Immediate32;
				instruction.Immediate32 = decoder.ReadUInt32();
				return;
			}
			if ((uint)operandSize == 2U)
			{
				instruction.Op2Kind = OpKind.Immediate32to64;
				instruction.Immediate32 = decoder.ReadUInt32();
				return;
			}
			instruction.Op2Kind = OpKind.Immediate16;
			instruction.InternalImmediate16 = decoder.ReadUInt16();
		}

		// Token: 0x04003805 RID: 14341
		private readonly Code3 codes;
	}
}
