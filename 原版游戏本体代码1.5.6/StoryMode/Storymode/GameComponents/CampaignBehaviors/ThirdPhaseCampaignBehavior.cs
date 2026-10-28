using System;
using System.Collections.Generic;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Actions;
using TaleWorlds.CampaignSystem.GameMenus;
using TaleWorlds.Library;

namespace StoryMode.GameComponents.CampaignBehaviors;

public class ThirdPhaseCampaignBehavior : CampaignBehaviorBase
{
	private List<Tuple<Kingdom, Kingdom>> _warsToEnforcePeaceNextWeek = new List<Tuple<Kingdom, Kingdom>>();

	public override void RegisterEvents()
	{
		CampaignEvents.WarDeclared.AddNonSerializedListener(this, OnWarDeclared);
		CampaignEvents.WeeklyTickEvent.AddNonSerializedListener(this, WeeklyTick);
		CampaignEvents.CanKingdomBeDiscontinuedEvent.AddNonSerializedListener(this, CanKingdomBeDiscontinued);
		CampaignEvents.OnSessionLaunchedEvent.AddNonSerializedListener(this, OnSessionLaunched);
	}

	private void OnSessionLaunched(CampaignGameStarter starter)
	{
		AddGameMenus(starter);
	}

	private void AddGameMenus(CampaignGameStarter starter)
	{
		starter.AddGameMenu("siege_ended_by_last_conspiracy_kingdom_defeat", "{=3pEDvftb} The conspiracy has collapsed. The defenders of their final stronghold send out a delegation under flag of truce and agree to surrender. Your men take possession of their fortress.", game_menu_last_conspiracy_kingdom_defeated_when_player_besiege_menu_on_init);
		starter.AddGameMenuOption("siege_ended_by_last_conspiracy_kingdom_defeat", "leave_from_besieged_last_conspiracy_settlement", "{=WVkc4UgX}Continue.", siege_ended_by_last_conspiracy_kingdom_defeat_condition, siege_ended_by_last_conspiracy_kingdom_defeat_consequence, isLeave: true);
	}

	private void OnWarDeclared(IFaction faction1, IFaction faction2, DeclareWarAction.DeclareWarDetail detail)
	{
		if (faction1 is Kingdom kingdom && faction2 is Kingdom kingdom2 && StoryModeManager.Current.MainStoryLine.ThirdPhase != null)
		{
			MBReadOnlyList<Kingdom> oppositionKingdoms = StoryModeManager.Current.MainStoryLine.ThirdPhase.OppositionKingdoms;
			MBReadOnlyList<Kingdom> allyKingdoms = StoryModeManager.Current.MainStoryLine.ThirdPhase.AllyKingdoms;
			if ((oppositionKingdoms.IndexOf(kingdom) >= 0 && oppositionKingdoms.IndexOf(kingdom2) >= 0) || (allyKingdoms.IndexOf(kingdom) >= 0 && allyKingdoms.IndexOf(kingdom2) >= 0))
			{
				_warsToEnforcePeaceNextWeek.Add(new Tuple<Kingdom, Kingdom>(kingdom, kingdom2));
			}
		}
	}

	private void WeeklyTick()
	{
		foreach (Tuple<Kingdom, Kingdom> item in new List<Tuple<Kingdom, Kingdom>>(_warsToEnforcePeaceNextWeek))
		{
			MakePeaceAction.Apply(item.Item1, item.Item2);
		}
	}

	private void CanKingdomBeDiscontinued(Kingdom kingdom, ref bool result)
	{
		if (StoryModeManager.Current.MainStoryLine.ThirdPhase != null && StoryModeManager.Current.MainStoryLine.ThirdPhase.OppositionKingdoms.Contains(kingdom))
		{
			result = false;
		}
	}

	public override void SyncData(IDataStore dataStore)
	{
		dataStore.SyncData("_warsToEnforcePeaceNextWeek", ref _warsToEnforcePeaceNextWeek);
	}

	private void siege_ended_by_last_conspiracy_kingdom_defeat_consequence(MenuCallbackArgs args)
	{
		GameMenu.ExitToLast();
	}

	private bool siege_ended_by_last_conspiracy_kingdom_defeat_condition(MenuCallbackArgs args)
	{
		args.optionLeaveType = GameMenuOption.LeaveType.Leave;
		return true;
	}

	private void game_menu_last_conspiracy_kingdom_defeated_when_player_besiege_menu_on_init(MenuCallbackArgs args)
	{
		Debug.Print("Game loaded when the player siege is left on last conspiracy kingdom is defeated by some other reasons");
	}
}
