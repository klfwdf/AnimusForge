using System;
using System.Runtime.CompilerServices;

namespace Iced.Intel.DecoderInternal
{
	// Token: 0x02000711 RID: 1809
	internal sealed class OpCodeHandler_Ev_Iz : OpCodeHandlerModRM
	{
		// Token: 0x06002505 RID: 9477 RVA: 0x0007BA62 File Offset: 0x00079C62
		public OpCodeHandler_Ev_Iz(Code code16, Code code32, Code code64)
		{
			this.codes = new Code3(code16, code32, code64);
		}

		// Token: 0x06002506 RID: 9478 RVA: 0x0007BA78 File Offset: 0x00079C78
		public OpCodeHandler_Ev_Iz(Code code16, Code code32, Code code64, HandlerFlags flags)
		{
			this.codes = new Code3(code16, code32, code64);
			this.flags = flags;
		}

		// Token: 0x06002507 RID: 9479 RVA: 0x0007BA98 File Offset: 0x00079C98
		[NullableContext(1)]
		public unsafe override void Decode(Decoder decoder, ref Instruction instruction)
		{
			UIntPtr operandSize = (UIntPtr)decoder.state.operandSize;
			instruction.InternalSetCodeNoCheck((Code)(*((ref this.codes.codes.FixedElementField) + (UIntPtr)((ulong)operandSize * 2UL))));
			if (decoder.state.mod < 3U)
			{
				decoder.state.zs.flags = decoder.state.zs.flags | (StateFlags)((this.flags & HandlerFlags.Lock) << 10);
				instruction.Op0Kind = OpKind.Memory;
				decoder.ReadOpMem(ref instruction);
			}
			else
			{
				instruction.Op0Register = ((int)operandSize << 4) + (int)(decoder.state.rm + decoder.state.zs.extraBaseRegisterBase) + Register.AX;
			}
			if ((uint)operandSize == 1U)
			{
				instruction.Op1Kind = OpKind.Immediate32;
				instruction.Immediate32 = decoder.ReadUInt32();
				return;
			}
			if ((uint)operandSize == 2U)
			{
				instruction.Op1Kind = OpKind.Immediate32to64;
				instruction.Immediate32 = decoder.ReadUInt32();
				return;
			}
			instruction.Op1Kind = OpKind.Immediate16;
			instruction.InternalImmediate16 = decoder.ReadUInt16();
		}

		// Token: 0x04003789 RID: 14217
		private readonly Code3 codes;

		// Token: 0x0400378A RID: 14218
		private readonly HandlerFlags flags;
	}
}
