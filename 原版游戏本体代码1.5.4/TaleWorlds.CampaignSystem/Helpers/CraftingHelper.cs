using System.Collections.Generic;
using System.Linq;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.GameState;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.CampaignSystem.Roster;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.Core;
using TaleWorlds.Localization;

namespace Helpers;

public static class CraftingHelper
{
	public static IEnumerable<Hero> GetAvailableHeroesForCrafting()
	{
		return from t in PartyBase.MainParty.MemberRoster.GetTroopRoster()
			where t.Character.IsHero
			select t.Character.HeroObject;
	}

	public static void ChangeCurrentCraftingTemplate(CraftingTemplate craftingTemplate)
	{
		CraftingState oldState = Game.Current.GameStateManager.ActiveState as CraftingState;
		OpenCrafting(craftingTemplate, oldState);
	}

	public static void OpenCrafting(CraftingTemplate craftingTemplate, CraftingState oldState = null)
	{
		Settlement currentSettlement = Settlement.CurrentSettlement;
		TextObject textObject = new TextObject("{=uZhHh7pm}Crafted {CURR_TEMPLATE_NAME}");
		textObject.SetTextVariable("CURR_TEMPLATE_NAME", craftingTemplate.TemplateName);
		Crafting crafting = new Crafting(craftingTemplate, (currentSettlement != null) ? currentSettlement.Culture : new CultureObject(), textObject);
		crafting.Init();
		crafting.ReIndex();
		if (oldState == null)
		{
			CraftingState craftingState = Game.Current.GameStateManager.CreateState<CraftingState>();
			craftingState.InitializeLogic(crafting);
			Game.Current.GameStateManager.PushState(craftingState);
		}
		else
		{
			oldState.InitializeLogic(crafting, isReplacingWeaponClass: true);
		}
	}
}
