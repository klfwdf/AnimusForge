using System;
using System.Runtime.CompilerServices;

namespace Iced.Intel.DecoderInternal
{
	// Token: 0x02000762 RID: 1890
	internal sealed class OpCodeHandler_BranchIw : OpCodeHandler
	{
		// Token: 0x060025AE RID: 9646 RVA: 0x0007F5D5 File Offset: 0x0007D7D5
		public OpCodeHandler_BranchIw(Code code16, Code code32, Code code64)
		{
			this.code16 = code16;
			this.code32 = code32;
			this.code64 = code64;
		}

		// Token: 0x060025AF RID: 9647 RVA: 0x0007F5F4 File Offset: 0x0007D7F4
		[NullableContext(1)]
		public override void Decode(Decoder decoder, ref Instruction instruction)
		{
			if (decoder.is64bMode)
			{
				if ((decoder.options & DecoderOptions.AMD) == DecoderOptions.None || decoder.state.operandSize != OpSize.Size16)
				{
					instruction.InternalSetCodeNoCheck(this.code64);
				}
				else
				{
					instruction.InternalSetCodeNoCheck(this.code16);
				}
			}
			else if (decoder.state.operandSize == OpSize.Size32)
			{
				instruction.InternalSetCodeNoCheck(this.code32);
			}
			else
			{
				instruction.InternalSetCodeNoCheck(this.code16);
			}
			instruction.Op0Kind = OpKind.Immediate16;
			instruction.InternalImmediate16 = decoder.ReadUInt16();
		}

		// Token: 0x04003824 RID: 14372
		private readonly Code code16;

		// Token: 0x04003825 RID: 14373
		private readonly Code code32;

		// Token: 0x04003826 RID: 14374
		private readonly Code code64;
	}
}
