using System;
using System.Runtime.CompilerServices;

namespace Iced.Intel.EncoderInternal
{
	// Token: 0x0200069B RID: 1691
	internal sealed class OpJx : Op
	{
		// Token: 0x06002412 RID: 9234 RVA: 0x00074573 File Offset: 0x00072773
		public OpJx(int immSize)
		{
			this.immSize = immSize;
		}

		// Token: 0x06002413 RID: 9235 RVA: 0x00074582 File Offset: 0x00072782
		[NullableContext(1)]
		public override void Encode(Encoder encoder, in Instruction instruction, int operand)
		{
			encoder.AddBranchX(this.immSize, instruction, operand);
		}

		// Token: 0x06002414 RID: 9236 RVA: 0x00074592 File Offset: 0x00072792
		public override OpKind GetNearBranchOpKind()
		{
			return base.GetNearBranchOpKind();
		}

		// Token: 0x0400352A RID: 13610
		private readonly int immSize;
	}
}
