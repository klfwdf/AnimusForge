using System;
using System.Runtime.CompilerServices;

namespace Iced.Intel.DecoderInternal
{
	// Token: 0x02000713 RID: 1811
	internal sealed class OpCodeHandler_Ev_Ib2 : OpCodeHandlerModRM
	{
		// Token: 0x0600250B RID: 9483 RVA: 0x0007BC87 File Offset: 0x00079E87
		public OpCodeHandler_Ev_Ib2(Code code16, Code code32, Code code64)
		{
			this.codes = new Code3(code16, code32, code64);
		}

		// Token: 0x0600250C RID: 9484 RVA: 0x0007BC9D File Offset: 0x00079E9D
		public OpCodeHandler_Ev_Ib2(Code code16, Code code32, Code code64, HandlerFlags flags)
		{
			this.codes = new Code3(code16, code32, code64);
			this.flags = flags;
		}

		// Token: 0x0600250D RID: 9485 RVA: 0x0007BCBC File Offset: 0x00079EBC
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
			instruction.Op1Kind = OpKind.Immediate8;
			instruction.InternalImmediate8 = decoder.ReadByte();
		}

		// Token: 0x0400378D RID: 14221
		private readonly Code3 codes;

		// Token: 0x0400378E RID: 14222
		private readonly HandlerFlags flags;
	}
}
