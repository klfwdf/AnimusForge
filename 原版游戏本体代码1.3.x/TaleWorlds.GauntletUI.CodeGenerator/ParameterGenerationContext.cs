using System;

namespace TaleWorlds.GauntletUI.CodeGenerator
{
	// Token: 0x02000009 RID: 9
	public class ParameterGenerationContext
	{
		// Token: 0x17000027 RID: 39
		// (get) Token: 0x06000068 RID: 104 RVA: 0x000035D9 File Offset: 0x000017D9
		// (set) Token: 0x06000069 RID: 105 RVA: 0x000035E1 File Offset: 0x000017E1
		public string Name { get; private set; }

		// Token: 0x17000028 RID: 40
		// (get) Token: 0x0600006A RID: 106 RVA: 0x000035EA File Offset: 0x000017EA
		// (set) Token: 0x0600006B RID: 107 RVA: 0x000035F2 File Offset: 0x000017F2
		public string Value { get; private set; }

		// Token: 0x0600006C RID: 108 RVA: 0x000035FB File Offset: 0x000017FB
		public ParameterGenerationContext(string name, string value)
		{
			this.Name = name;
			this.Value = value;
		}
	}
}
