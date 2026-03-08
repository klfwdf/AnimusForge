using System;
using System.Runtime.CompilerServices;

namespace Iced.Intel.EncoderInternal
{
	// Token: 0x02000693 RID: 1683
	internal sealed class OpIw : Op
	{
		// Token: 0x060023FB RID: 9211 RVA: 0x00074112 File Offset: 0x00072312
		[NullableContext(1)]
		public override void Encode(Encoder encoder, in Instruction instruction, int operand)
		{
			if (!encoder.Verify(operand, OpKind.Immediate16, instruction.GetOpKind(operand)))
			{
				return;
			}
			encoder.ImmSize = ImmSize.Size2;
			encoder.Immediate = (uint)instruction.Immediate16;
		}

		// Token: 0x060023FC RID: 9212 RVA: 0x0004350F File Offset: 0x0004170F
		public override OpKind GetImmediateOpKind()
		{
			return OpKind.Immediate16;
		}
	}
}
