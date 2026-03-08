using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;

namespace Iced.Intel
{
	// Token: 0x0200062B RID: 1579
	[NullableContext(1)]
	[Nullable(0)]
	internal readonly struct InstructionBlock
	{
		// Token: 0x060020F0 RID: 8432 RVA: 0x000684ED File Offset: 0x000666ED
		public InstructionBlock(CodeWriter codeWriter, IList<Instruction> instructions, ulong rip)
		{
			if (codeWriter == null)
			{
				throw new ArgumentNullException("codeWriter");
			}
			this.CodeWriter = codeWriter;
			if (instructions == null)
			{
				throw new ArgumentNullException("instructions");
			}
			this.Instructions = instructions;
			this.RIP = rip;
		}

		// Token: 0x04001680 RID: 5760
		public readonly CodeWriter CodeWriter;

		// Token: 0x04001681 RID: 5761
		public readonly IList<Instruction> Instructions;

		// Token: 0x04001682 RID: 5762
		public readonly ulong RIP;
	}
}
