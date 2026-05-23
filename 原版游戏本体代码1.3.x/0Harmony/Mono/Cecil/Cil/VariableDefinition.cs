using System;

namespace Mono.Cecil.Cil
{
	// Token: 0x02000329 RID: 809
	internal sealed class VariableDefinition : VariableReference
	{
		// Token: 0x17000554 RID: 1364
		// (get) Token: 0x060014DB RID: 5339 RVA: 0x00041CE9 File Offset: 0x0003FEE9
		public bool IsPinned
		{
			get
			{
				return this.variable_type.IsPinned;
			}
		}

		// Token: 0x060014DC RID: 5340 RVA: 0x00041CF6 File Offset: 0x0003FEF6
		public VariableDefinition(TypeReference variableType)
			: base(variableType)
		{
		}

		// Token: 0x060014DD RID: 5341 RVA: 0x0001B6A2 File Offset: 0x000198A2
		public override VariableDefinition Resolve()
		{
			return this;
		}
	}
}
