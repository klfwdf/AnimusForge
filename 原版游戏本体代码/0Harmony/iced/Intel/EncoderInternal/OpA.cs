using System;
using System.Runtime.CompilerServices;

namespace Iced.Intel.EncoderInternal
{
	// Token: 0x0200069D RID: 1693
	internal sealed class OpA : Op
	{
		// Token: 0x06002418 RID: 9240 RVA: 0x000745C7 File Offset: 0x000727C7
		public OpA(int size)
		{
			this.size = size;
		}

		// Token: 0x06002419 RID: 9241 RVA: 0x000745D6 File Offset: 0x000727D6
		[NullableContext(1)]
		public override void Encode(Encoder encoder, in Instruction instruction, int operand)
		{
			encoder.AddFarBranch(instruction, operand, this.size);
		}

		// Token: 0x0600241A RID: 9242 RVA: 0x000745E6 File Offset: 0x000727E6
		public override OpKind GetFarBranchOpKind()
		{
			if (this.size != 2)
			{
				return OpKind.FarBranch32;
			}
			return OpKind.FarBranch16;
		}

		// Token: 0x0400352C RID: 13612
		private readonly int size;
	}
}
