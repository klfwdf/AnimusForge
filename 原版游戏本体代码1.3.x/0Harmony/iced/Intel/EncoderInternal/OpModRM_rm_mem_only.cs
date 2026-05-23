using System;
using System.Runtime.CompilerServices;

namespace Iced.Intel.EncoderInternal
{
	// Token: 0x02000688 RID: 1672
	internal sealed class OpModRM_rm_mem_only : Op
	{
		// Token: 0x060023E3 RID: 9187 RVA: 0x00073D61 File Offset: 0x00071F61
		public OpModRM_rm_mem_only(bool mustUseSib)
		{
			this.mustUseSib = mustUseSib;
		}

		// Token: 0x060023E4 RID: 9188 RVA: 0x00073D70 File Offset: 0x00071F70
		[NullableContext(1)]
		public override void Encode(Encoder encoder, in Instruction instruction, int operand)
		{
			if (this.mustUseSib)
			{
				encoder.EncoderFlags |= EncoderFlags.MustUseSib;
			}
			encoder.AddRegOrMem(instruction, operand, Register.None, Register.None, true, false);
		}

		// Token: 0x04003518 RID: 13592
		private readonly bool mustUseSib;
	}
}
