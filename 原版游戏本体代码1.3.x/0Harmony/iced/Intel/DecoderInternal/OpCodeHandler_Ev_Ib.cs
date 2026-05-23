using System;
using System.Runtime.CompilerServices;

namespace Iced.Intel.DecoderInternal
{
	// Token: 0x02000712 RID: 1810
	internal sealed class OpCodeHandler_Ev_Ib : OpCodeHandlerModRM
	{
		// Token: 0x06002508 RID: 9480 RVA: 0x0007BB80 File Offset: 0x00079D80
		public OpCodeHandler_Ev_Ib(Code code16, Code code32, Code code64)
		{
			this.codes = new Code3(code16, code32, code64);
		}

		// Token: 0x06002509 RID: 9481 RVA: 0x0007BB96 File Offset: 0x00079D96
		public OpCodeHandler_Ev_Ib(Code code16, Code code32, Code code64, HandlerFlags flags)
		{
			this.codes = new Code3(code16, code32, code64);
			this.flags = flags;
		}

		// Token: 0x0600250A RID: 9482 RVA: 0x0007BBB4 File Offset: 0x00079DB4
		[NullableContext(1)]
		public unsafe override void Decode(Decoder decoder, ref Instruction instruction)
		{
			UIntPtr operandSize = (UIntPtr)decoder.state.operandSize;
			instruction.InternalSetCodeNoCheck((Code)(*((ref this.codes.codes.FixedElementField) + (UIntPtr)((ulong)operandSize * 2UL))));
			if (decoder.state.mod == 3U)
			{
				instruction.Op0Register = ((int)operandSize << 4) + (int)(decoder.state.rm + decoder.state.zs.extraBaseRegisterBase) + Register.AX;
			}
			else
			{
				decoder.state.zs.flags = decoder.state.zs.flags | (StateFlags)((this.flags & HandlerFlags.Lock) << 10);
				instruction.Op0Kind = OpKind.Memory;
				decoder.ReadOpMem(ref instruction);
			}
			if ((uint)operandSize == 1U)
			{
				instruction.Op1Kind = OpKind.Immediate8to32;
			}
			else if ((uint)operandSize == 2U)
			{
				instruction.Op1Kind = OpKind.Immediate8to64;
			}
			else
			{
				instruction.Op1Kind = OpKind.Immediate8to16;
			}
			instruction.InternalImmediate8 = decoder.ReadByte();
		}

		// Token: 0x0400378B RID: 14219
		private readonly Code3 codes;

		// Token: 0x0400378C RID: 14220
		private readonly HandlerFlags flags;
	}
}
