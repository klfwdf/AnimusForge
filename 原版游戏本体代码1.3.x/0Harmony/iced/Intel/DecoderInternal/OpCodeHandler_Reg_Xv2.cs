using System;
using System.Runtime.CompilerServices;

namespace Iced.Intel.DecoderInternal
{
	// Token: 0x02000751 RID: 1873
	internal sealed class OpCodeHandler_Reg_Xv2 : OpCodeHandler
	{
		// Token: 0x0600258C RID: 9612 RVA: 0x0007EB77 File Offset: 0x0007CD77
		public OpCodeHandler_Reg_Xv2(Code code16, Code code32)
		{
			this.code16 = code16;
			this.code32 = code32;
		}

		// Token: 0x0600258D RID: 9613 RVA: 0x0007EB90 File Offset: 0x0007CD90
		[NullableContext(1)]
		public override void Decode(Decoder decoder, ref Instruction instruction)
		{
			if (decoder.state.operandSize != OpSize.Size16)
			{
				instruction.InternalSetCodeNoCheck(this.code32);
				instruction.Op0Register = Register.DX;
			}
			else
			{
				instruction.InternalSetCodeNoCheck(this.code16);
				instruction.Op0Register = Register.DX;
			}
			if (decoder.state.addressSize == OpSize.Size64)
			{
				instruction.Op1Kind = OpKind.MemorySegRSI;
				return;
			}
			if (decoder.state.addressSize == OpSize.Size32)
			{
				instruction.Op1Kind = OpKind.MemorySegESI;
				return;
			}
			instruction.Op1Kind = OpKind.MemorySegSI;
		}

		// Token: 0x0400380E RID: 14350
		private readonly Code code16;

		// Token: 0x0400380F RID: 14351
		private readonly Code code32;
	}
}
