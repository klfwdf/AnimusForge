using System;

namespace TaleWorlds.DotNet
{
	// Token: 0x02000031 RID: 49
	public class ScriptComponentParams : Attribute
	{
		// Token: 0x1700002F RID: 47
		// (get) Token: 0x0600013F RID: 319 RVA: 0x0000582E File Offset: 0x00003A2E
		// (set) Token: 0x06000140 RID: 320 RVA: 0x00005836 File Offset: 0x00003A36
		public string Tag { get; set; }

		// Token: 0x17000030 RID: 48
		// (get) Token: 0x06000141 RID: 321 RVA: 0x0000583F File Offset: 0x00003A3F
		// (set) Token: 0x06000142 RID: 322 RVA: 0x00005847 File Offset: 0x00003A47
		public string NameOverride { get; set; }

		// Token: 0x06000143 RID: 323 RVA: 0x00005850 File Offset: 0x00003A50
		public ScriptComponentParams(string tag = "", string nameOverride = "")
		{
			this.Tag = tag;
			this.NameOverride = nameOverride;
		}
	}
}
