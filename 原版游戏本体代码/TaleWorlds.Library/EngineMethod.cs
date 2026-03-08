using System;

namespace TaleWorlds.Library
{
	// Token: 0x0200002C RID: 44
	public class EngineMethod : Attribute
	{
		// Token: 0x06000160 RID: 352 RVA: 0x00005DFD File Offset: 0x00003FFD
		public EngineMethod(string engineMethodName, bool activateTelemetryProfiling = false, string[] conditionals = null, bool isMonoInline = false)
		{
			this.EngineMethodName = engineMethodName;
			this.ActivateTelemetryProfiling = activateTelemetryProfiling;
			this.Conditionals = conditionals;
			this.IsMonoInline = isMonoInline;
		}

		// Token: 0x1700001D RID: 29
		// (get) Token: 0x06000161 RID: 353 RVA: 0x00005E22 File Offset: 0x00004022
		// (set) Token: 0x06000162 RID: 354 RVA: 0x00005E2A File Offset: 0x0000402A
		public string EngineMethodName { get; private set; }

		// Token: 0x1700001E RID: 30
		// (get) Token: 0x06000163 RID: 355 RVA: 0x00005E33 File Offset: 0x00004033
		// (set) Token: 0x06000164 RID: 356 RVA: 0x00005E3B File Offset: 0x0000403B
		public bool ActivateTelemetryProfiling { get; private set; }

		// Token: 0x1700001F RID: 31
		// (get) Token: 0x06000165 RID: 357 RVA: 0x00005E44 File Offset: 0x00004044
		// (set) Token: 0x06000166 RID: 358 RVA: 0x00005E4C File Offset: 0x0000404C
		public string[] Conditionals { get; private set; }

		// Token: 0x17000020 RID: 32
		// (get) Token: 0x06000167 RID: 359 RVA: 0x00005E55 File Offset: 0x00004055
		// (set) Token: 0x06000168 RID: 360 RVA: 0x00005E5D File Offset: 0x0000405D
		public bool IsMonoInline { get; private set; }
	}
}
