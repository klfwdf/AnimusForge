using System;

namespace TaleWorlds.GauntletUI.PrefabSystem
{
	// Token: 0x02000012 RID: 18
	public class WidgetAttributeTemplate
	{
		// Token: 0x1700001D RID: 29
		// (get) Token: 0x06000070 RID: 112 RVA: 0x00002EB8 File Offset: 0x000010B8
		// (set) Token: 0x06000071 RID: 113 RVA: 0x00002EC0 File Offset: 0x000010C0
		public WidgetAttributeKeyType KeyType { get; set; }

		// Token: 0x1700001E RID: 30
		// (get) Token: 0x06000072 RID: 114 RVA: 0x00002EC9 File Offset: 0x000010C9
		// (set) Token: 0x06000073 RID: 115 RVA: 0x00002ED1 File Offset: 0x000010D1
		public WidgetAttributeValueType ValueType { get; set; }

		// Token: 0x1700001F RID: 31
		// (get) Token: 0x06000074 RID: 116 RVA: 0x00002EDA File Offset: 0x000010DA
		// (set) Token: 0x06000075 RID: 117 RVA: 0x00002EE2 File Offset: 0x000010E2
		public string Key { get; set; }

		// Token: 0x17000020 RID: 32
		// (get) Token: 0x06000076 RID: 118 RVA: 0x00002EEB File Offset: 0x000010EB
		// (set) Token: 0x06000077 RID: 119 RVA: 0x00002EF3 File Offset: 0x000010F3
		public string Value { get; set; }
	}
}
