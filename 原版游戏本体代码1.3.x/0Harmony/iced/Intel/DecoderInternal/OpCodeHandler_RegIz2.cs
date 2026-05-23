using System;
using System.Runtime.CompilerServices;

namespace Iced.Intel.DecoderInternal
{
	// Token: 0x02000743 RID: 1859
	internal sealed class OpCodeHandler_RegIz2 : OpCodeHandler
	{
		// Token: 0x06002570 RID: 9584 RVA: 0x0007E0CD File Offset: 0x0007C2CD
		public OpCodeHandler_RegIz2(int index)
		{
			this.index = index;
		}

		// Token: 0x06002571 RID: 9585 RVA: 0x0007E0DC File Offset: 0x0007C2DC
		[NullableContext(1)]
		public override void Decode(Decoder decoder, ref Instruction instruction)
		{
			if (decoder.state.operandSize == OpSize.Size32)
			{
				instruction.InternalSetCodeNoCheck(Code.Mov_r32_imm32);
				instruction.Op0Register = this.index + (int)decoder.state.zs.extraBaseRegisterBase + Register.EAX;
				instruction.Op1Kind = OpKind.Immediate32;
				instruction.Immediate32 = decoder.ReadUInt32();
				return;
			}
			if (decoder.state.operandSize == OpSize.Size64)
			{
				instruction.InternalSetCodeNoCheck(Code.Mov_r64_imm64);
				instruction.Op0Register = this.index + (int)decoder.state.zs.extraBaseRegisterBase + Register.RAX;
				instruction.Op1Kind = OpKind.Immediate64;
				instruction.InternalImmediate64_lo = decoder.ReadUInt32();
				instruction.InternalImmediate64_hi = decoder.ReadUInt32();
				return;
			}
			instruction.InternalSetCodeNoCheck(Code.Mov_r16_imm16);
			instruction.Op0Register = this.index + (int)decoder.state.zs.extraBaseRegisterBase + Register.AX;
			instruction.Op1Kind = OpKind.Immediate16;
			instruction.InternalImmediate16 = decoder.ReadUInt16();
		}

		// Token: 0x040037F3 RID: 14323
		private readonly int index;
	}
}
