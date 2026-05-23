using System;
using System.Runtime.CompilerServices;

namespace Iced.Intel.EncoderInternal
{
	// Token: 0x0200069A RID: 1690
	internal sealed class OpJ : Op
	{
		// Token: 0x0600240F RID: 9231 RVA: 0x0007453F File Offset: 0x0007273F
		public OpJ(OpKind opKind, int immSize)
		{
			this.opKind = opKind;
			this.immSize = immSize;
		}

		// Token: 0x06002410 RID: 9232 RVA: 0x00074555 File Offset: 0x00072755
		[NullableContext(1)]
		public override void Encode(Encoder encoder, in Instruction instruction, int operand)
		{
			encoder.AddBranch(this.opKind, this.immSize, instruction, operand);
		}

		// Token: 0x06002411 RID: 9233 RVA: 0x0007456B File Offset: 0x0007276B
		public override OpKind GetNearBranchOpKind()
		{
			return this.opKind;
		}

		// Token: 0x04003528 RID: 13608
		private readonly OpKind opKind;

		// Token: 0x04003529 RID: 13609
		private readonly int immSize;
	}
}
