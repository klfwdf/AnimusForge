using System;
using System.Runtime.CompilerServices;

namespace Iced.Intel.DecoderInternal
{
	// Token: 0x02000746 RID: 1862
	internal sealed class OpCodeHandler_Gv_Ma : OpCodeHandlerModRM
	{
		// Token: 0x06002576 RID: 9590 RVA: 0x0007E36D File Offset: 0x0007C56D
		public OpCodeHandler_Gv_Ma(Code code16, Code code32)
		{
			this.code16 = code16;
			this.code32 = code32;
		}

		// Token: 0x06002577 RID: 9591 RVA: 0x0007E384 File Offset: 0x0007C584
		[NullableContext(1)]
		public override void Decode(Decoder decoder, ref Instruction instruction)
		{
			if (decoder.state.operandSize != OpSize.Size16)
			{
				instruction.InternalSetCodeNoCheck(this.code32);
				instruction.Op0Register = (int)(decoder.state.reg + decoder.state.zs.extraRegisterBase) + Register.EAX;
			}
			else
			{
				instruction.InternalSetCodeNoCheck(this.code16);
				instruction.Op0Register = (int)(decoder.state.reg + decoder.state.zs.extraRegisterBase) + Register.AX;
			}
			instruction.Op1Kind = OpKind.Memory;
			decoder.ReadOpMem(ref instruction);
		}

		// Token: 0x040037FA RID: 14330
		private readonly Code code16;

		// Token: 0x040037FB RID: 14331
		private readonly Code code32;
	}
}
