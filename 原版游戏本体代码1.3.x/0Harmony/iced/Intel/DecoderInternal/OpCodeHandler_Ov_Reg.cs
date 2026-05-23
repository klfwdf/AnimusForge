using System;
using System.Runtime.CompilerServices;

namespace Iced.Intel.DecoderInternal
{
	// Token: 0x02000761 RID: 1889
	internal sealed class OpCodeHandler_Ov_Reg : OpCodeHandler
	{
		// Token: 0x060025AC RID: 9644 RVA: 0x0007F4E9 File Offset: 0x0007D6E9
		public OpCodeHandler_Ov_Reg(Code code16, Code code32, Code code64)
		{
			this.codes = new Code3(code16, code32, code64);
		}

		// Token: 0x060025AD RID: 9645 RVA: 0x0007F500 File Offset: 0x0007D700
		[NullableContext(1)]
		public unsafe override void Decode(Decoder decoder, ref Instruction instruction)
		{
			decoder.displIndex = decoder.state.zs.instructionLength;
			instruction.Op0Kind = OpKind.Memory;
			UIntPtr operandSize = (UIntPtr)decoder.state.operandSize;
			instruction.InternalSetCodeNoCheck((Code)(*((ref this.codes.codes.FixedElementField) + (UIntPtr)((ulong)operandSize * 2UL))));
			instruction.Op1Register = ((int)operandSize << 4) + Register.AX;
			if (decoder.state.addressSize == OpSize.Size64)
			{
				instruction.InternalSetMemoryDisplSize(4U);
				decoder.state.zs.flags = decoder.state.zs.flags | StateFlags.Addr64;
				instruction.MemoryDisplacement64 = decoder.ReadUInt64();
				return;
			}
			if (decoder.state.addressSize == OpSize.Size32)
			{
				instruction.InternalSetMemoryDisplSize(3U);
				instruction.MemoryDisplacement64 = (ulong)decoder.ReadUInt32();
				return;
			}
			instruction.InternalSetMemoryDisplSize(2U);
			instruction.MemoryDisplacement64 = (ulong)decoder.ReadUInt16();
		}

		// Token: 0x04003823 RID: 14371
		private readonly Code3 codes;
	}
}
