using TaleWorlds.CampaignSystem.GameMenus;
using TaleWorlds.CampaignSystem.ViewModelCollection.GameMenu.Overlay;
using TaleWorlds.Library;

namespace SandBox.View.Overlay;

public class DefaultGameMenuOverlayProvider : IGameMenuOverlayProvider
{
	public GameMenuOverlay GetOverlay(GameMenu.MenuOverlayType menuOverlayType)
	{
		switch (menuOverlayType)
		{
		case GameMenu.MenuOverlayType.Encounter:
			return new EncounterMenuOverlayVM();
		case GameMenu.MenuOverlayType.SettlementWithParties:
		case GameMenu.MenuOverlayType.SettlementWithCharacters:
		case GameMenu.MenuOverlayType.SettlementWithBoth:
			return new SettlementMenuOverlayVM(menuOverlayType);
		default:
			Debug.FailedAssert("Game menu overlay: " + menuOverlayType.ToString() + " could not be found", "C:\\BuildAgent\\work\\mb3\\Source\\Bannerlord\\SandBox.View\\Overlay\\DefaultGameMenuOverlayProvider.cs", "GetOverlay", 22);
			return null;
		}
	}
}
