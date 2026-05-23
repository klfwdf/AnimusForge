using System;

namespace TaleWorlds.DotNet
{
	// Token: 0x02000013 RID: 19
	public class EngineStruct : Attribute
	{
		// Token: 0x17000018 RID: 24
		// (get) Token: 0x06000054 RID: 84 RVA: 0x00002EFF File Offset: 0x000010FF
		// (set) Token: 0x06000055 RID: 85 RVA: 0x00002F07 File Offset: 0x00001107
		public string EngineType { get; set; }

		// Token: 0x17000019 RID: 25
		// (get) Token: 0x06000056 RID: 86 RVA: 0x00002F10 File Offset: 0x00001110
		// (set) Token: 0x06000057 RID: 87 RVA: 0x00002F18 File Offset: 0x00001118
		public string AlternateDotNetType { get; set; }

		// Token: 0x1700001A RID: 26
		// (get) Token: 0x06000058 RID: 88 RVA: 0x00002F21 File Offset: 0x00001121
		// (set) Token: 0x06000059 RID: 89 RVA: 0x00002F29 File Offset: 0x00001129
		public string EngineEnumPrefix { get; set; }

		// Token: 0x1700001B RID: 27
		// (get) Token: 0x0600005A RID: 90 RVA: 0x00002F32 File Offset: 0x00001132
		// (set) Token: 0x0600005B RID: 91 RVA: 0x00002F3A File Offset: 0x0000113A
		public bool IgnoreMemberOffsetTest { get; set; }

		// Token: 0x1700001C RID: 28
		// (get) Token: 0x0600005C RID: 92 RVA: 0x00002F43 File Offset: 0x00001143
		// (set) Token: 0x0600005D RID: 93 RVA: 0x00002F4B File Offset: 0x0000114B
		public string[] Conditionals { get; set; }

		// Token: 0x1700001D RID: 29
		// (get) Token: 0x0600005E RID: 94 RVA: 0x00002F54 File Offset: 0x00001154
		// (set) Token: 0x0600005F RID: 95 RVA: 0x00002F5C File Offset: 0x0000115C
		public bool FirstCharacterUppercase { get; set; } = true;

		// Token: 0x06000060 RID: 96 RVA: 0x00002F65 File Offset: 0x00001165
		public EngineStruct(string engineType, bool ignoreMemberOffsetTest = false, string[] conditionals = null)
		{
			this.EngineType = engineType;
			this.AlternateDotNetType = null;
			this.EngineEnumPrefix = null;
			this.IgnoreMemberOffsetTest = ignoreMemberOffsetTest;
			this.Conditionals = conditionals;
		}

		// Token: 0x06000061 RID: 97 RVA: 0x00002F97 File Offset: 0x00001197
		public EngineStruct(string engineType, string alternateDotNetType, bool ignoreMemberOffsetTest = false, string[] conditionals = null)
		{
			this.EngineType = engineType;
			this.AlternateDotNetType = alternateDotNetType;
			this.EngineEnumPrefix = null;
			this.IgnoreMemberOffsetTest = ignoreMemberOffsetTest;
			this.Conditionals = conditionals;
		}

		// Token: 0x06000062 RID: 98 RVA: 0x00002FCA File Offset: 0x000011CA
		public EngineStruct(string engineType, bool isEnum, string engineEnumPrefix, bool ignoreMemberOffsetTest = false)
		{
			this.EngineType = engineType;
			this.AlternateDotNetType = null;
			this.EngineEnumPrefix = engineEnumPrefix;
			this.IgnoreMemberOffsetTest = ignoreMemberOffsetTest;
		}
	}
}
