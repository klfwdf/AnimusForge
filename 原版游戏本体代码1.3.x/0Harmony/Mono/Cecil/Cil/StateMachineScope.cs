using System;

namespace Mono.Cecil.Cil
{
	// Token: 0x02000319 RID: 793
	internal sealed class StateMachineScope
	{
		// Token: 0x17000544 RID: 1348
		// (get) Token: 0x0600148F RID: 5263 RVA: 0x00041204 File Offset: 0x0003F404
		// (set) Token: 0x06001490 RID: 5264 RVA: 0x0004120C File Offset: 0x0003F40C
		public InstructionOffset Start
		{
			get
			{
				return this.start;
			}
			set
			{
				this.start = value;
			}
		}

		// Token: 0x17000545 RID: 1349
		// (get) Token: 0x06001491 RID: 5265 RVA: 0x00041215 File Offset: 0x0003F415
		// (set) Token: 0x06001492 RID: 5266 RVA: 0x0004121D File Offset: 0x0003F41D
		public InstructionOffset End
		{
			get
			{
				return this.end;
			}
			set
			{
				this.end = value;
			}
		}

		// Token: 0x06001493 RID: 5267 RVA: 0x00041226 File Offset: 0x0003F426
		internal StateMachineScope(int start, int end)
		{
			this.start = new InstructionOffset(start);
			this.end = new InstructionOffset(end);
		}

		// Token: 0x06001494 RID: 5268 RVA: 0x00041248 File Offset: 0x0003F448
		public StateMachineScope(Instruction start, Instruction end)
		{
			this.start = new InstructionOffset(start);
			this.end = ((end != null) ? new InstructionOffset(end) : default(InstructionOffset));
		}

		// Token: 0x04000A4D RID: 2637
		internal InstructionOffset start;

		// Token: 0x04000A4E RID: 2638
		internal InstructionOffset end;
	}
}
