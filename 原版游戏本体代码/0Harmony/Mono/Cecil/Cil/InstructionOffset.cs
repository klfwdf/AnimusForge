using System;

namespace Mono.Cecil.Cil
{
	// Token: 0x0200030B RID: 779
	internal struct InstructionOffset
	{
		// Token: 0x17000520 RID: 1312
		// (get) Token: 0x0600144C RID: 5196 RVA: 0x00040D7C File Offset: 0x0003EF7C
		public int Offset
		{
			get
			{
				if (this.instruction != null)
				{
					return this.instruction.Offset;
				}
				if (this.offset != null)
				{
					return this.offset.Value;
				}
				throw new NotSupportedException();
			}
		}

		// Token: 0x17000521 RID: 1313
		// (get) Token: 0x0600144D RID: 5197 RVA: 0x00040DB0 File Offset: 0x0003EFB0
		public bool IsEndOfMethod
		{
			get
			{
				return this.instruction == null && this.offset == null;
			}
		}

		// Token: 0x17000522 RID: 1314
		// (get) Token: 0x0600144E RID: 5198 RVA: 0x00040DCA File Offset: 0x0003EFCA
		internal bool IsResolved
		{
			get
			{
				return this.instruction != null || this.offset == null;
			}
		}

		// Token: 0x17000523 RID: 1315
		// (get) Token: 0x0600144F RID: 5199 RVA: 0x00040DE4 File Offset: 0x0003EFE4
		internal Instruction ResolvedInstruction
		{
			get
			{
				return this.instruction;
			}
		}

		// Token: 0x06001450 RID: 5200 RVA: 0x00040DEC File Offset: 0x0003EFEC
		public InstructionOffset(Instruction instruction)
		{
			if (instruction == null)
			{
				throw new ArgumentNullException("instruction");
			}
			this.instruction = instruction;
			this.offset = null;
		}

		// Token: 0x06001451 RID: 5201 RVA: 0x00040E0F File Offset: 0x0003F00F
		public InstructionOffset(int offset)
		{
			this.instruction = null;
			this.offset = new int?(offset);
		}

		// Token: 0x04000A1E RID: 2590
		private readonly Instruction instruction;

		// Token: 0x04000A1F RID: 2591
		private readonly int? offset;
	}
}
