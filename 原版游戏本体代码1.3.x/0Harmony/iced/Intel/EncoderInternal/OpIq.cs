using System;
using System.Runtime.CompilerServices;

namespace Iced.Intel.EncoderInternal
{
	// Token: 0x02000695 RID: 1685
	internal sealed class OpIq : Op
	{
		// Token: 0x06002401 RID: 9217 RVA: 0x0007418C File Offset: 0x0007238C
		[NullableContext(1)]
		public override void Encode(Encoder encoder, in Instruction instruction, int operand)
		{
			if (!encoder.Verify(operand, OpKind.Immediate64, instruction.GetOpKind(operand)))
			{
				return;
			}
			encoder.ImmSize = ImmSize.Size8;
			ulong imm = instruction.Immediate64;
			encoder.Immediate = (uint)imm;
			encoder.ImmediateHi = (uint)(imm >> 32);
		}

		// Token: 0x06002402 RID: 9218 RVA: 0x000741CD File Offset: 0x000723CD
		public override OpKind GetImmediateOpKind()
		{
			return OpKind.Immediate64;
		}
	}
}
