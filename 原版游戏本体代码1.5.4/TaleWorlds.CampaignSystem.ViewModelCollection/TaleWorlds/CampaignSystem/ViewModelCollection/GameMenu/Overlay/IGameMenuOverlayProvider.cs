using TaleWorlds.CampaignSystem.GameMenus;

namespace TaleWorlds.CampaignSystem.ViewModelCollection.GameMenu.Overlay;

public interface IGameMenuOverlayProvider
{
	GameMenuOverlay GetOverlay(TaleWorlds.CampaignSystem.GameMenus.GameMenu.MenuOverlayType menuOverlayType);
}
