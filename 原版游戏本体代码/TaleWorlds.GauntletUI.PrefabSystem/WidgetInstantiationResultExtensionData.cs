using System;

namespace TaleWorlds.GauntletUI.PrefabSystem
{
	// Token: 0x0200001B RID: 27
	internal struct WidgetInstantiationResultExtensionData
	{
		// Token: 0x1700002F RID: 47
		// (get) Token: 0x060000C8 RID: 200 RVA: 0x00004038 File Offset: 0x00002238
		// (set) Token: 0x060000C9 RID: 201 RVA: 0x00004040 File Offset: 0x00002240
		public string Name { get; set; }

		// Token: 0x17000030 RID: 48
		// (get) Token: 0x060000CA RID: 202 RVA: 0x00004049 File Offset: 0x00002249
		// (set) Token: 0x060000CB RID: 203 RVA: 0x00004051 File Offset: 0x00002251
		public bool PassToChildWidgetCreation { get; set; }

		// Token: 0x17000031 RID: 49
		// (get) Token: 0x060000CC RID: 204 RVA: 0x0000405A File Offset: 0x0000225A
		// (set) Token: 0x060000CD RID: 205 RVA: 0x00004062 File Offset: 0x00002262
		public object Data { get; set; }
	}
}
