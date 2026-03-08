using System;

namespace Mono.Cecil.Cil
{
	// Token: 0x0200030D RID: 781
	internal struct VariableIndex
	{
		// Token: 0x17000524 RID: 1316
		// (get) Token: 0x06001452 RID: 5202 RVA: 0x00040E24 File Offset: 0x0003F024
		public int Index
		{
			get
			{
				if (this.variable != null)
				{
					return this.variable.Index;
				}
				if (this.index != null)
				{
					return this.index.Value;
				}
				throw new NotSupportedException();
			}
		}

		// Token: 0x17000525 RID: 1317
		// (get) Token: 0x06001453 RID: 5203 RVA: 0x00040E58 File Offset: 0x0003F058
		internal bool IsResolved
		{
			get
			{
				return this.variable != null;
			}
		}

		// Token: 0x17000526 RID: 1318
		// (get) Token: 0x06001454 RID: 5204 RVA: 0x00040E63 File Offset: 0x0003F063
		internal VariableDefinition ResolvedVariable
		{
			get
			{
				return this.variable;
			}
		}

		// Token: 0x06001455 RID: 5205 RVA: 0x00040E6B File Offset: 0x0003F06B
		public VariableIndex(VariableDefinition variable)
		{
			if (variable == null)
			{
				throw new ArgumentNullException("variable");
			}
			this.variable = variable;
			this.index = null;
		}

		// Token: 0x06001456 RID: 5206 RVA: 0x00040E8E File Offset: 0x0003F08E
		public VariableIndex(int index)
		{
			this.variable = null;
			this.index = new int?(index);
		}

		// Token: 0x04000A23 RID: 2595
		private readonly VariableDefinition variable;

		// Token: 0x04000A24 RID: 2596
		private readonly int? index;
	}
}
