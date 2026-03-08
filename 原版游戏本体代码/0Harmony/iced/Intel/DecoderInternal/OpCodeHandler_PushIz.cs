using System;
using System.Runtime.CompilerServices;

namespace Iced.Intel.DecoderInternal
{
	// Token: 0x02000745 RID: 1861
	internal sealed class OpCodeHandler_PushIz : OpCodeHandler
	{
		// Token: 0x06002574 RID: 9588 RVA: 0x0007E29F File Offset: 0x0007C49F
		public OpCodeHandler_PushIz(Code code16, Code code32, Code code64)
		{
			this.code16 = code16;
			this.code32 = code32;
			this.code64 = code64;
		}

		// Token: 0x06002575 RID: 9589 RVA: 0x0007E2BC File Offset: 0x0007C4BC
		[NullableContext(1)]
		public override void Decode(Decoder decoder, ref Instruction instruction)
		{
			if (decoder.is64bMode)
			{
				if (decoder.state.operandSize != OpSize.Size16)
				{
					instruction.InternalSetCodeNoCheck(this.code64);
					instruction.Op0Kind = OpKind.Immediate32to64;
					instruction.Immediate32 = decoder.ReadUInt32();
					return;
				}
				instruction.InternalSetCodeNoCheck(this.code16);
				instruction.Op0Kind = OpKind.Immediate16;
				instruction.InternalImmediate16 = decoder.ReadUInt16();
				return;
			}
			else
			{
				if (decoder.state.operandSize == OpSize.Size32)
				{
					instruction.InternalSetCodeNoCheck(this.code32);
					instruction.Op0Kind = OpKind.Immediate32;
					instruction.Immediate32 = decoder.ReadUInt32();
					return;
				}
				instruction.InternalSetCodeNoCheck(this.code16);
				instruction.Op0Kind = OpKind.Immediate16;
				instruction.InternalImmediate16 = decoder.ReadUInt16();
				return;
			}
		}

		// Token: 0x040037F7 RID: 14327
		private readonly Code code16;

		// Token: 0x040037F8 RID: 14328
		private readonly Code code32;

		// Token: 0x040037F9 RID: 14329
		private readonly Code code64;
	}
}
