using System.Collections.Generic;
using TaleWorlds.CampaignSystem.GameMenus;

namespace TaleWorlds.CampaignSystem.ViewModelCollection.GameMenu.Overlay;

public static class GameMenuOverlayFactory
{
	private static List<IGameMenuOverlayProvider> _providers;

	static GameMenuOverlayFactory()
	{
		_providers = new List<IGameMenuOverlayProvider>();
	}

	public static void RegisterProvider(IGameMenuOverlayProvider provider)
	{
		_providers.Add(provider);
	}

	public static void UnregisterProvider(IGameMenuOverlayProvider provider)
	{
		_providers.Remove(provider);
	}

	public static GameMenuOverlay GetOverlay(TaleWorlds.CampaignSystem.GameMenus.GameMenu.MenuOverlayType menuOverlayType)
	{
		for (int num = _providers.Count - 1; num >= 0; num--)
		{
			GameMenuOverlay overlay = _providers[num].GetOverlay(menuOverlayType);
			if (overlay != null)
			{
				return overlay;
			}
		}
		return null;
	}
}
