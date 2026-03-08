using System;
using System.Collections.Generic;
using TaleWorlds.CampaignSystem.GameMenus;

namespace TaleWorlds.CampaignSystem.ViewModelCollection.GameMenu.Overlay
{
	// Token: 0x020000B9 RID: 185
	public static class GameMenuOverlayFactory
	{
		// Token: 0x06001228 RID: 4648 RVA: 0x000491E5 File Offset: 0x000473E5
		public static void RegisterProvider(IGameMenuOverlayProvider provider)
		{
			GameMenuOverlayFactory._providers.Add(provider);
		}

		// Token: 0x06001229 RID: 4649 RVA: 0x000491F2 File Offset: 0x000473F2
		public static void UnregisterProvider(IGameMenuOverlayProvider provider)
		{
			GameMenuOverlayFactory._providers.Remove(provider);
		}

		// Token: 0x0600122A RID: 4650 RVA: 0x00049200 File Offset: 0x00047400
		public static GameMenuOverlay GetOverlay(GameMenu.MenuOverlayType menuOverlayType)
		{
			for (int i = GameMenuOverlayFactory._providers.Count - 1; i >= 0; i--)
			{
				GameMenuOverlay overlay = GameMenuOverlayFactory._providers[i].GetOverlay(menuOverlayType);
				if (overlay != null)
				{
					return overlay;
				}
			}
			return null;
		}

		// Token: 0x0400084C RID: 2124
		private static List<IGameMenuOverlayProvider> _providers = new List<IGameMenuOverlayProvider>();
	}
}
