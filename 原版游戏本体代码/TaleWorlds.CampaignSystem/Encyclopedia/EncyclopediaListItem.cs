using System;

namespace TaleWorlds.CampaignSystem.Encyclopedia
{
	// Token: 0x02000173 RID: 371
	public struct EncyclopediaListItem
	{
		// Token: 0x06001B15 RID: 6933 RVA: 0x0008B111 File Offset: 0x00089311
		public EncyclopediaListItem(object obj, string name, string description, string id, string typeName, bool playerCanSeeValues, Action onShowTooltip = null)
		{
			this.Object = obj;
			this.Name = name;
			this.Description = description;
			this.Id = id;
			this.TypeName = typeName;
			this.PlayerCanSeeValues = playerCanSeeValues;
			this.OnShowTooltip = onShowTooltip;
		}

		// Token: 0x04000909 RID: 2313
		public readonly object Object;

		// Token: 0x0400090A RID: 2314
		public readonly string Name;

		// Token: 0x0400090B RID: 2315
		public readonly string Description;

		// Token: 0x0400090C RID: 2316
		public readonly string Id;

		// Token: 0x0400090D RID: 2317
		public readonly string TypeName;

		// Token: 0x0400090E RID: 2318
		public readonly bool PlayerCanSeeValues;

		// Token: 0x0400090F RID: 2319
		public readonly Action OnShowTooltip;
	}
}
