using System;

namespace TaleWorlds.CampaignSystem.ViewModelCollection.GameMenu.Overlay
{
	// Token: 0x020000BC RID: 188
	public class MenuOverlay : Attribute
	{
		// Token: 0x06001272 RID: 4722 RVA: 0x0004A51B File Offset: 0x0004871B
		public MenuOverlay(string typeId)
		{
			this.TypeId = typeId;
		}

		// Token: 0x0400086C RID: 2156
		public new string TypeId;
	}
}
