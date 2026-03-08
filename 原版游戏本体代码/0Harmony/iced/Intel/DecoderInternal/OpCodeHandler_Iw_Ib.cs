using System;
using System.Runtime.CompilerServices;

namespace Iced.Intel.DecoderInternal
{
	// Token: 0x02000764 RID: 1892
	internal sealed class OpCodeHandler_Iw_Ib : OpCodeHandler
	{
		// Token: 0x060025B2 RID: 9650 RVA: 0x0007F701 File Offset: 0x0007D901
		public OpCodeHandler_Iw_Ib(Code code16, Code code32, Code code64)
		{
			this.code16 = code16;
			this.code32 = code32;
			this.code64 = code64;
		}

		// Token: 0x060025B3 RID: 9651 RVA: 0x0007F720 File Offset: 0x0007D920
		[NullableContext(1)]
		public override void Decode(Decoder decoder, ref Instruction instruction)
		{
			instruction.Op0Kind = OpKind.Immediate16;
			instruction.InternalImmediate16 = decoder.ReadUInt16();
			instruction.Op1Kind = OpKind.Immediate8_2nd;
			instruction.InternalImmediate8_2nd = decoder.ReadByte();
			if (decoder.is64bMode)
			{
				if (decoder.state.operandSize != OpSize.Size16)
				{
					instruction.InternalSetCodeNoCheck(this.code64);
					return;
				}
				instruction.InternalSetCodeNoCheck(this.code16);
				return;
			}
			else
			{
				if (decoder.state.operandSize == OpSize.Size32)
				{
					instruction.InternalSetCodeNoCheck(this.code32);
					return;
				}
				instruction.InternalSetCodeNoCheck(this.code16);
				return;
			}
		}

		// Token: 0x0400382A RID: 14378
		private readonly Code code16;

		// Token: 0x0400382B RID: 14379
		private readonly Code code32;

		// Token: 0x0400382C RID: 14380
		private readonly Code code64;
	}
}
