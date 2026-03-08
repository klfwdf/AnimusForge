using System;
using System.Collections.Generic;
using TaleWorlds.GauntletUI.PrefabSystem;

namespace TaleWorlds.GauntletUI.Data
{
	// Token: 0x0200000B RID: 11
	public class ItemTemplateUsageWithData
	{
		// Token: 0x1700001E RID: 30
		// (get) Token: 0x0600007D RID: 125 RVA: 0x00003B24 File Offset: 0x00001D24
		// (set) Token: 0x0600007E RID: 126 RVA: 0x00003B2C File Offset: 0x00001D2C
		public Dictionary<string, WidgetAttributeTemplate> GivenParameters { get; private set; }

		// Token: 0x1700001F RID: 31
		// (get) Token: 0x0600007F RID: 127 RVA: 0x00003B35 File Offset: 0x00001D35
		// (set) Token: 0x06000080 RID: 128 RVA: 0x00003B3D File Offset: 0x00001D3D
		public ItemTemplateUsage ItemTemplateUsage { get; private set; }

		// Token: 0x17000020 RID: 32
		// (get) Token: 0x06000081 RID: 129 RVA: 0x00003B46 File Offset: 0x00001D46
		public WidgetTemplate DefaultItemTemplate
		{
			get
			{
				return this.ItemTemplateUsage.DefaultItemTemplate;
			}
		}

		// Token: 0x17000021 RID: 33
		// (get) Token: 0x06000082 RID: 130 RVA: 0x00003B53 File Offset: 0x00001D53
		public WidgetTemplate FirstItemTemplate
		{
			get
			{
				return this.ItemTemplateUsage.FirstItemTemplate;
			}
		}

		// Token: 0x17000022 RID: 34
		// (get) Token: 0x06000083 RID: 131 RVA: 0x00003B60 File Offset: 0x00001D60
		public WidgetTemplate LastItemTemplate
		{
			get
			{
				return this.ItemTemplateUsage.LastItemTemplate;
			}
		}

		// Token: 0x06000084 RID: 132 RVA: 0x00003B6D File Offset: 0x00001D6D
		public ItemTemplateUsageWithData(ItemTemplateUsage itemTemplateUsage)
		{
			this.GivenParameters = new Dictionary<string, WidgetAttributeTemplate>();
			this.ItemTemplateUsage = itemTemplateUsage;
		}
	}
}
