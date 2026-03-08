using System;
using System.Runtime.CompilerServices;

namespace Iced.Intel.DecoderInternal
{
	// Token: 0x02000741 RID: 1857
	internal sealed class OpCodeHandler_Reg_Iz : OpCodeHandler
	{
		// Token: 0x0600256B RID: 9579 RVA: 0x0007DF6A File Offset: 0x0007C16A
		public OpCodeHandler_Reg_Iz(Code code16, Code code32, Code code64)
		{
			this.code16 = code16;
			this.code32 = code32;
			this.code64 = code64;
		}

		// Token: 0x0600256C RID: 9580 RVA: 0x0007DF88 File Offset: 0x0007C188
		[NullableContext(1)]
		public override void Decode(Decoder decoder, ref Instruction instruction)
		{
			if (decoder.state.operandSize == OpSize.Size32)
			{
				instruction.InternalSetCodeNoCheck(this.code32);
				instruction.Op0Register = Register.EAX;
				instruction.Op1Kind = OpKind.Immediate32;
				instruction.Immediate32 = decoder.ReadUInt32();
				return;
			}
			if (decoder.state.operandSize == OpSize.Size64)
			{
				instruction.InternalSetCodeNoCheck(this.code64);
				instruction.Op0Register = Register.RAX;
				instruction.Op1Kind = OpKind.Immediate32to64;
				instruction.Immediate32 = decoder.ReadUInt32();
				return;
			}
			instruction.InternalSetCodeNoCheck(this.code16);
			instruction.Op0Register = Register.AX;
			instruction.Op1Kind = OpKind.Immediate16;
			instruction.InternalImmediate16 = decoder.ReadUInt16();
		}

		// Token: 0x040037ED RID: 14317
		private readonly Code code16;

		// Token: 0x040037EE RID: 14318
		private readonly Code code32;

		// Token: 0x040037EF RID: 14319
		private readonly Code code64;
	}
}
