using System;
using System.Runtime.CompilerServices;

namespace Iced.Intel.DecoderInternal
{
	// Token: 0x0200074E RID: 1870
	internal sealed class OpCodeHandler_Yv_Reg2 : OpCodeHandler
	{
		// Token: 0x06002586 RID: 9606 RVA: 0x0007E9DF File Offset: 0x0007CBDF
		public OpCodeHandler_Yv_Reg2(Code code16, Code code32)
		{
			this.code16 = code16;
			this.code32 = code32;
		}

		// Token: 0x06002587 RID: 9607 RVA: 0x0007E9F8 File Offset: 0x0007CBF8
		[NullableContext(1)]
		public override void Decode(Decoder decoder, ref Instruction instruction)
		{
			if (decoder.state.operandSize != OpSize.Size16)
			{
				instruction.InternalSetCodeNoCheck(this.code32);
				instruction.Op1Register = Register.DX;
			}
			else
			{
				instruction.InternalSetCodeNoCheck(this.code16);
				instruction.Op1Register = Register.DX;
			}
			if (decoder.state.addressSize == OpSize.Size64)
			{
				instruction.Op0Kind = OpKind.MemoryESRDI;
				return;
			}
			if (decoder.state.addressSize == OpSize.Size32)
			{
				instruction.Op0Kind = OpKind.MemoryESEDI;
				return;
			}
			instruction.Op0Kind = OpKind.MemoryESDI;
		}

		// Token: 0x04003809 RID: 14345
		private readonly Code code16;

		// Token: 0x0400380A RID: 14346
		private readonly Code code32;
	}
}
