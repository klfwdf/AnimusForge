using System;
using System.Runtime.CompilerServices;

namespace Iced.Intel.EncoderInternal
{
	// Token: 0x02000690 RID: 1680
	internal sealed class OpRegSTi : Op
	{
		// Token: 0x060023F3 RID: 9203 RVA: 0x00073F5C File Offset: 0x0007215C
		[NullableContext(1)]
		public override void Encode(Encoder encoder, in Instruction instruction, int operand)
		{
			if (!encoder.Verify(operand, OpKind.Register, instruction.GetOpKind(operand)))
			{
				return;
			}
			Register reg = instruction.GetOpRegister(operand);
			if (!encoder.Verify(operand, reg, Register.ST0, Register.ST7))
			{
				return;
			}
			encoder.OpCode |= (uint)(reg - Register.ST0);
		}
	}
}
