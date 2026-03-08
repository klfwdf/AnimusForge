using System;
using System.Runtime.CompilerServices;

namespace Iced.Intel.EncoderInternal
{
	// Token: 0x02000694 RID: 1684
	internal sealed class OpId : Op
	{
		// Token: 0x060023FE RID: 9214 RVA: 0x00074139 File Offset: 0x00072339
		public OpId(OpKind opKind)
		{
			this.opKind = opKind;
		}

		// Token: 0x060023FF RID: 9215 RVA: 0x00074148 File Offset: 0x00072348
		[NullableContext(1)]
		public override void Encode(Encoder encoder, in Instruction instruction, int operand)
		{
			OpKind opImmKind = instruction.GetOpKind(operand);
			if (!encoder.Verify(operand, this.opKind, opImmKind))
			{
				return;
			}
			encoder.ImmSize = ImmSize.Size4;
			encoder.Immediate = instruction.Immediate32;
		}

		// Token: 0x06002400 RID: 9216 RVA: 0x00074181 File Offset: 0x00072381
		public override OpKind GetImmediateOpKind()
		{
			return this.opKind;
		}

		// Token: 0x04003527 RID: 13607
		private readonly OpKind opKind;
	}
}
