using System;
using TaleWorlds.CampaignSystem.GameMenus;
using TaleWorlds.CampaignSystem.ViewModelCollection.GameMenu.Overlay;
using TaleWorlds.Library;

namespace SandBox.View.Overlay
{
	// Token: 0x0200000D RID: 13
	public class DefaultGameMenuOverlayProvider : IGameMenuOverlayProvider
	{
		// Token: 0x06000066 RID: 102 RVA: 0x00004150 File Offset: 0x00002350
		public GameMenuOverlay GetOverlay(GameMenu.MenuOverlayType menuOverlayType)
		{
			if (menuOverlayType == GameMenu.MenuOverlayType.Encounter)
			{
				return new EncounterMenuOverlayVM();
			}
			if (menuOverlayType == GameMenu.MenuOverlayType.SettlementWithParties || menuOverlayType == GameMenu.MenuOverlayType.SettlementWithCharacters || menuOverlayType == GameMenu.MenuOverlayType.SettlementWithBoth)
			{
				return new SettlementMenuOverlayVM(menuOverlayType);
			}
			Debug.FailedAssert("Game menu overlay: " + menuOverlayType.ToString() + " could not be found", "C:\\BuildAgent\\work\\mb3\\Source\\Bannerlord\\SandBox.View\\Overlay\\DefaultGameMenuOverlayProvider.cs", "GetOverlay", 22);
			return null;
		}
	}
}
