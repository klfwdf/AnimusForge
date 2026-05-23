using System;
using TaleWorlds.CampaignSystem.GameMenus;

namespace TaleWorlds.CampaignSystem.ViewModelCollection.GameMenu.Overlay
{
	// Token: 0x020000BB RID: 187
	public interface IGameMenuOverlayProvider
	{
		// Token: 0x06001271 RID: 4721
		GameMenuOverlay GetOverlay(GameMenu.MenuOverlayType menuOverlayType);
	}
}
