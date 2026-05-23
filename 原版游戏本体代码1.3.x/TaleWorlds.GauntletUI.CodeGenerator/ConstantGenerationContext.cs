using System;
using TaleWorlds.GauntletUI.PrefabSystem;

namespace TaleWorlds.GauntletUI.CodeGenerator
{
	// Token: 0x02000007 RID: 7
	public class ConstantGenerationContext
	{
		// Token: 0x1700001B RID: 27
		// (get) Token: 0x06000046 RID: 70 RVA: 0x00002A84 File Offset: 0x00000C84
		// (set) Token: 0x06000047 RID: 71 RVA: 0x00002A8C File Offset: 0x00000C8C
		public ConstantDefinition ConstantDefinition { get; private set; }

		// Token: 0x06000048 RID: 72 RVA: 0x00002A95 File Offset: 0x00000C95
		public ConstantGenerationContext(ConstantDefinition constantDefinition)
		{
			this.ConstantDefinition = constantDefinition;
		}
	}
}
