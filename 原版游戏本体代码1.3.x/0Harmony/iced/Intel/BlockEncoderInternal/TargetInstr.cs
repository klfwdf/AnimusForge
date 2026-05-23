using System;
using System.Runtime.CompilerServices;

namespace Iced.Intel.BlockEncoderInternal
{
	// Token: 0x020007EA RID: 2026
	[NullableContext(1)]
	[Nullable(0)]
	internal readonly struct TargetInstr
	{
		// Token: 0x060026F0 RID: 9968 RVA: 0x00087179 File Offset: 0x00085379
		public TargetInstr(Instr instruction)
		{
			this.instruction = instruction;
			this.address = 0UL;
		}

		// Token: 0x060026F1 RID: 9969 RVA: 0x0008718A File Offset: 0x0008538A
		public TargetInstr(ulong address)
		{
			this.instruction = null;
			this.address = address;
		}

		// Token: 0x060026F2 RID: 9970 RVA: 0x0008719A File Offset: 0x0008539A
		public bool IsInBlock(Block block)
		{
			Instr instr = this.instruction;
			return ((instr != null) ? instr.Block : null) == block;
		}

		// Token: 0x060026F3 RID: 9971 RVA: 0x000871B4 File Offset: 0x000853B4
		public ulong GetAddress()
		{
			Instr instruction = this.instruction;
			if (instruction == null)
			{
				return this.address;
			}
			return instruction.IP;
		}

		// Token: 0x040039A6 RID: 14758
		private readonly Instr instruction;

		// Token: 0x040039A7 RID: 14759
		private readonly ulong address;
	}
}
