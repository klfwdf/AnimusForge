using System;
using System.Collections.Generic;
using System.Linq;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.GameState;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.CampaignSystem.Roster;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.Core;
using TaleWorlds.Localization;

namespace Helpers
{
	// Token: 0x02000021 RID: 33
	public static class CraftingHelper
	{
		// Token: 0x06000110 RID: 272 RVA: 0x0000D5C8 File Offset: 0x0000B7C8
		public static IEnumerable<Hero> GetAvailableHeroesForCrafting()
		{
			return from m in PartyBase.MainParty.MemberRoster.GetTroopRoster()
				where m.Character.IsHero
				select m into t
				select t.Character.HeroObject;
		}

		// Token: 0x06000111 RID: 273 RVA: 0x0000D62C File Offset: 0x0000B82C
		public static void ChangeCurrentCraftingTemplate(CraftingTemplate craftingTemplate)
		{
			CraftingState oldState = Game.Current.GameStateManager.ActiveState as CraftingState;
			CraftingHelper.OpenCrafting(craftingTemplate, oldState);
		}

		// Token: 0x06000112 RID: 274 RVA: 0x0000D658 File Offset: 0x0000B858
		public static void OpenCrafting(CraftingTemplate craftingTemplate, CraftingState oldState = null)
		{
			Settlement currentSettlement = Settlement.CurrentSettlement;
			TextObject textObject = new TextObject("{=uZhHh7pm}Crafted {CURR_TEMPLATE_NAME}", null);
			textObject.SetTextVariable("CURR_TEMPLATE_NAME", craftingTemplate.TemplateName);
			Crafting crafting = new Crafting(craftingTemplate, (currentSettlement != null) ? currentSettlement.Culture : new CultureObject(), textObject);
			crafting.Init();
			crafting.ReIndex(false);
			if (oldState == null)
			{
				CraftingState craftingState = Game.Current.GameStateManager.CreateState<CraftingState>();
				craftingState.InitializeLogic(crafting, false);
				Game.Current.GameStateManager.PushState(craftingState, 0);
				return;
			}
			oldState.InitializeLogic(crafting, true);
		}
	}
}
