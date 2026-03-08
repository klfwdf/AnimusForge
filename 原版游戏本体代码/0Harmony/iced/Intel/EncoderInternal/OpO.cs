using System;
using System.Runtime.CompilerServices;

namespace Iced.Intel.EncoderInternal
{
	// Token: 0x0200069E RID: 1694
	internal sealed class OpO : Op
	{
		// Token: 0x0600241B RID: 9243 RVA: 0x000745F4 File Offset: 0x000727F4
		[NullableContext(1)]
		public override void Encode(Encoder encoder, in Instruction instruction, int operand)
		{
			encoder.AddAbsMem(instruction, operand);
		}
	}
}
