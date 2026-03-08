using System;
using System.Runtime.CompilerServices;

namespace Iced.Intel.EncoderInternal
{
	// Token: 0x0200069C RID: 1692
	internal sealed class OpJdisp : Op
	{
		// Token: 0x06002415 RID: 9237 RVA: 0x0007459A File Offset: 0x0007279A
		public OpJdisp(int displSize)
		{
			this.displSize = displSize;
		}

		// Token: 0x06002416 RID: 9238 RVA: 0x000745A9 File Offset: 0x000727A9
		[NullableContext(1)]
		public override void Encode(Encoder encoder, in Instruction instruction, int operand)
		{
			encoder.AddBranchDisp(this.displSize, instruction, operand);
		}

		// Token: 0x06002417 RID: 9239 RVA: 0x000745B9 File Offset: 0x000727B9
		public override OpKind GetNearBranchOpKind()
		{
			if (this.displSize != 2)
			{
				return OpKind.NearBranch32;
			}
			return OpKind.NearBranch16;
		}

		// Token: 0x0400352B RID: 13611
		private readonly int displSize;
	}
}
