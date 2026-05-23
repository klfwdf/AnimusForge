using System;
using System.Runtime.CompilerServices;

namespace Iced.Intel.DecoderInternal
{
	// Token: 0x02000744 RID: 1860
	internal sealed class OpCodeHandler_PushIb2 : OpCodeHandler
	{
		// Token: 0x06002572 RID: 9586 RVA: 0x0007E1CF File Offset: 0x0007C3CF
		public OpCodeHandler_PushIb2(Code code16, Code code32, Code code64)
		{
			this.code16 = code16;
			this.code32 = code32;
			this.code64 = code64;
		}

		// Token: 0x06002573 RID: 9587 RVA: 0x0007E1EC File Offset: 0x0007C3EC
		[NullableContext(1)]
		public override void Decode(Decoder decoder, ref Instruction instruction)
		{
			if (decoder.is64bMode)
			{
				if (decoder.state.operandSize != OpSize.Size16)
				{
					instruction.InternalSetCodeNoCheck(this.code64);
					instruction.Op0Kind = OpKind.Immediate8to64;
					instruction.InternalImmediate8 = decoder.ReadByte();
					return;
				}
				instruction.InternalSetCodeNoCheck(this.code16);
				instruction.Op0Kind = OpKind.Immediate8to16;
				instruction.InternalImmediate8 = decoder.ReadByte();
				return;
			}
			else
			{
				if (decoder.state.operandSize == OpSize.Size32)
				{
					instruction.InternalSetCodeNoCheck(this.code32);
					instruction.Op0Kind = OpKind.Immediate8to32;
					instruction.InternalImmediate8 = decoder.ReadByte();
					return;
				}
				instruction.InternalSetCodeNoCheck(this.code16);
				instruction.Op0Kind = OpKind.Immediate8to16;
				instruction.InternalImmediate8 = decoder.ReadByte();
				return;
			}
		}

		// Token: 0x040037F4 RID: 14324
		private readonly Code code16;

		// Token: 0x040037F5 RID: 14325
		private readonly Code code32;

		// Token: 0x040037F6 RID: 14326
		private readonly Code code64;
	}
}
