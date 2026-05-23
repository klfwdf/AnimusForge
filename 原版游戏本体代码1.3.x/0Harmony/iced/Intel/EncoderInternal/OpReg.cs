using System;
using System.Runtime.CompilerServices;

namespace Iced.Intel.EncoderInternal
{
	// Token: 0x0200068F RID: 1679
	internal sealed class OpReg : Op
	{
		// Token: 0x060023F1 RID: 9201 RVA: 0x00073F23 File Offset: 0x00072123
		public OpReg(Register register)
		{
			this.register = register;
		}

		// Token: 0x060023F2 RID: 9202 RVA: 0x00073F32 File Offset: 0x00072132
		[NullableContext(1)]
		public override void Encode(Encoder encoder, in Instruction instruction, int operand)
		{
			encoder.Verify(operand, OpKind.Register, instruction.GetOpKind(operand));
			encoder.Verify(operand, this.register, instruction.GetOpRegister(operand));
		}

		// Token: 0x04003525 RID: 13605
		private readonly Register register;
	}
}
