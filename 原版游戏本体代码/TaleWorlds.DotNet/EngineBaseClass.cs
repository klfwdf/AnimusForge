using System;

namespace TaleWorlds.DotNet
{
	// Token: 0x0200000E RID: 14
	public abstract class EngineBaseClass : Attribute
	{
		// Token: 0x17000014 RID: 20
		// (get) Token: 0x06000046 RID: 70 RVA: 0x00002E44 File Offset: 0x00001044
		// (set) Token: 0x06000047 RID: 71 RVA: 0x00002E4C File Offset: 0x0000104C
		public string EngineType { get; set; }

		// Token: 0x06000048 RID: 72 RVA: 0x00002E55 File Offset: 0x00001055
		protected EngineBaseClass(string engineType)
		{
			this.EngineType = engineType;
		}
	}
}
