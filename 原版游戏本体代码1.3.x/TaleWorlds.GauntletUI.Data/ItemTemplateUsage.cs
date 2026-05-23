using System;
using TaleWorlds.GauntletUI.PrefabSystem;

namespace TaleWorlds.GauntletUI.Data
{
	// Token: 0x0200000A RID: 10
	public class ItemTemplateUsage
	{
		// Token: 0x1700001B RID: 27
		// (get) Token: 0x06000076 RID: 118 RVA: 0x00003AE2 File Offset: 0x00001CE2
		// (set) Token: 0x06000077 RID: 119 RVA: 0x00003AEA File Offset: 0x00001CEA
		public WidgetTemplate DefaultItemTemplate { get; private set; }

		// Token: 0x1700001C RID: 28
		// (get) Token: 0x06000078 RID: 120 RVA: 0x00003AF3 File Offset: 0x00001CF3
		// (set) Token: 0x06000079 RID: 121 RVA: 0x00003AFB File Offset: 0x00001CFB
		public WidgetTemplate FirstItemTemplate { get; set; }

		// Token: 0x1700001D RID: 29
		// (get) Token: 0x0600007A RID: 122 RVA: 0x00003B04 File Offset: 0x00001D04
		// (set) Token: 0x0600007B RID: 123 RVA: 0x00003B0C File Offset: 0x00001D0C
		public WidgetTemplate LastItemTemplate { get; set; }

		// Token: 0x0600007C RID: 124 RVA: 0x00003B15 File Offset: 0x00001D15
		public ItemTemplateUsage(WidgetTemplate defaultItemTemplate)
		{
			this.DefaultItemTemplate = defaultItemTemplate;
		}
	}
}
