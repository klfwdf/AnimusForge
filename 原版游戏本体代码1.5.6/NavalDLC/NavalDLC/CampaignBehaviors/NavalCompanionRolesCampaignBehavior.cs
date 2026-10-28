using Helpers;
using SandBox.CampaignBehaviors;
using TaleWorlds.CampaignSystem;
using TaleWorlds.Library;
using TaleWorlds.Localization;

namespace NavalDLC.CampaignBehaviors;

public class NavalCompanionRolesCampaignBehavior : CampaignBehaviorBase
{
	public override void RegisterEvents()
	{
		CampaignEvents.OnSessionLaunchedEvent.AddNonSerializedListener(this, OnSessionLaunched);
	}

	public override void SyncData(IDataStore dataStore)
	{
	}

	public void OnSessionLaunched(CampaignGameStarter campaignGameStarter)
	{
		AddDialogs(campaignGameStarter);
	}

	private void AddDialogs(CampaignGameStarter campaignGameStarter)
	{
		campaignGameStarter.AddPlayerLine("companion_becomes_first_mate", "companion_roles", "companion_okay", "{=FRTvNn9Q}I no longer need you as First Mate.", companion_fire_first_mate_on_condition, remove_first_mate_role_on_consequence);
		campaignGameStarter.AddPlayerLine("companion_becomes_navigator", "companion_roles", "companion_okay", "{=1dO4mgZI}I no longer need you as Navigator.", companion_fire_navigator_on_condition, remove_navigator_role_on_consequence);
		campaignGameStarter.AddPlayerLine("companion_becomes_first_mate_2", "companion_roles", "give_companion_roles", "{=fqva0OdY}First Mate {CURRENTLY_HELD_FIRST_MATE}", companion_becomes_first_mate_on_condition, companion_becomes_first_mate_on_consequence, 100, companion_becomes_first_mate_clickable_condition);
		campaignGameStarter.AddPlayerLine("companion_becomes_navigator_2", "companion_roles", "give_companion_roles", "{=jjISJIcf}Navigator {CURRENTLY_HELD_NAVIGATOR}", companion_becomes_navigator_on_condition, companion_becomes_navigator_on_consequence, 100, companion_becomes_navigator_clickable_condition);
		campaignGameStarter.AddPlayerLine("companion_becomes_first_mate_3", "too_many_roles_responses", "companion_okay_to_role_selection", "{=FRTvNn9Q}I no longer need you as First Mate.", companion_fire_first_mate_on_condition, remove_first_mate_role_on_consequence);
		campaignGameStarter.AddPlayerLine("companion_becomes_navigator_3", "too_many_roles_responses", "companion_okay_to_role_selection", "{=1dO4mgZI}I no longer need you as Navigator.", companion_fire_navigator_on_condition, remove_navigator_role_on_consequence);
		campaignGameStarter.AddPlayerLine("tavernkeeper_companion_info_player_select_first_mate", "tavernkeeper_list_companion_types", "player_selected_companion_type", "{=bdMwsaY6}I need a first mate who can enforce discipline and keep the ship battle-ready.", null, tavernkeeper_companion_info_player_select_first_mate_on_consequence, 100, companion_type_select_clickable_condition);
		campaignGameStarter.AddPlayerLine("tavernkeeper_companion_info_player_select_navigator", "tavernkeeper_list_companion_types", "player_selected_companion_type", "{=bzoUl6DI}I need a navigator who knows winds, currents and coasts, and can help me sail swiftly.", null, tavernkeeper_companion_info_player_select_navigator_on_consequence, 100, companion_type_select_clickable_condition);
	}

	private bool companion_becomes_first_mate_clickable_condition(out TextObject explanation)
	{
		return party_role_assignment_clickable_condition(PartyRole.FirstMate, out explanation);
	}

	private bool companion_becomes_first_mate_on_condition()
	{
		Hero oneToOneConversationHero = Hero.OneToOneConversationHero;
		Hero roleHolder = oneToOneConversationHero.PartyBelongedTo.GetRoleHolder(PartyRole.FirstMate);
		if (roleHolder != null)
		{
			TextObject textObject = new TextObject("{=QEp8t8u0}(Currently held by {COMPANION.LINK})");
			StringHelpers.SetCharacterProperties("COMPANION", roleHolder.CharacterObject, textObject);
			MBTextManager.SetTextVariable("CURRENTLY_HELD_FIRST_MATE", textObject);
		}
		else
		{
			MBTextManager.SetTextVariable("CURRENTLY_HELD_FIRST_MATE", "{=kNQMkh3j}(Currently unassigned)");
		}
		return roleHolder != oneToOneConversationHero;
	}

	private void companion_becomes_first_mate_on_consequence()
	{
		Hero.OneToOneConversationHero.PartyBelongedTo.SetPartyFirstMate(Hero.OneToOneConversationHero);
	}

	private bool companion_becomes_navigator_clickable_condition(out TextObject explanation)
	{
		return party_role_assignment_clickable_condition(PartyRole.Navigator, out explanation);
	}

	private bool companion_becomes_navigator_on_condition()
	{
		Hero oneToOneConversationHero = Hero.OneToOneConversationHero;
		Hero roleHolder = oneToOneConversationHero.PartyBelongedTo.GetRoleHolder(PartyRole.Navigator);
		if (roleHolder != null)
		{
			TextObject textObject = new TextObject("{=QEp8t8u0}(Currently held by {COMPANION.LINK})");
			StringHelpers.SetCharacterProperties("COMPANION", roleHolder.CharacterObject, textObject);
			MBTextManager.SetTextVariable("CURRENTLY_HELD_NAVIGATOR", textObject);
		}
		else
		{
			MBTextManager.SetTextVariable("CURRENTLY_HELD_NAVIGATOR", "{=kNQMkh3j}(Currently unassigned)");
		}
		return roleHolder != oneToOneConversationHero;
	}

	private void companion_becomes_navigator_on_consequence()
	{
		Hero.OneToOneConversationHero.PartyBelongedTo.SetPartyNavigator(Hero.OneToOneConversationHero);
	}

	private bool companion_fire_first_mate_on_condition()
	{
		return CanFireHeroFromRole(PartyRole.FirstMate, Hero.OneToOneConversationHero);
	}

	private bool companion_fire_navigator_on_condition()
	{
		return CanFireHeroFromRole(PartyRole.Navigator, Hero.OneToOneConversationHero);
	}

	private void remove_first_mate_role_on_consequence()
	{
		Hero.OneToOneConversationHero.PartyBelongedTo.RemovePartyRoleOfHero(Hero.OneToOneConversationHero, PartyRole.FirstMate);
	}

	private void remove_navigator_role_on_consequence()
	{
		Hero.OneToOneConversationHero.PartyBelongedTo.RemovePartyRoleOfHero(Hero.OneToOneConversationHero, PartyRole.Navigator);
	}

	private bool party_role_assignment_clickable_condition(PartyRole role, out TextObject explanation)
	{
		bool num = Campaign.Current.Models.ClanMemberPartyRoleModel.IsHeroAssignableForPartyRoleInParty(role, Hero.OneToOneConversationHero, Hero.OneToOneConversationHero.PartyBelongedTo);
		if (!num)
		{
			explanation = new TextObject("{=zcTOL3gI}Not eligible for the role.");
			return num;
		}
		explanation = TextObject.GetEmpty();
		return num;
	}

	private bool CanFireHeroFromRole(PartyRole role, Hero hero)
	{
		if (hero.PartyBelongedTo.GetRoleHolder(role) == hero)
		{
			return hero != hero.PartyBelongedTo.LeaderHero;
		}
		return false;
	}

	private void tavernkeeper_companion_info_player_select_first_mate_on_consequence()
	{
		TavernEmployeesCampaignBehavior behavior = Campaign.Current.CampaignBehaviorManager.GetBehavior<TavernEmployeesCampaignBehavior>();
		if (behavior != null)
		{
			behavior.FindCompanionWithType(PartyRole.FirstMate);
		}
		else
		{
			Debug.FailedAssert("TavernEmployeesCampaignBehavior does not exist!", "C:\\BuildAgent\\work\\mb3\\Source\\Bannerlord\\NavalDLC\\CampaignBehaviors\\NavalCompanionRolesCampaignBehavior.cs", "tavernkeeper_companion_info_player_select_first_mate_on_consequence", 159);
		}
	}

	private void tavernkeeper_companion_info_player_select_navigator_on_consequence()
	{
		TavernEmployeesCampaignBehavior behavior = Campaign.Current.CampaignBehaviorManager.GetBehavior<TavernEmployeesCampaignBehavior>();
		if (behavior != null)
		{
			behavior.FindCompanionWithType(PartyRole.Navigator);
		}
		else
		{
			Debug.FailedAssert("TavernEmployeesCampaignBehavior does not exist!", "C:\\BuildAgent\\work\\mb3\\Source\\Bannerlord\\NavalDLC\\CampaignBehaviors\\NavalCompanionRolesCampaignBehavior.cs", "tavernkeeper_companion_info_player_select_navigator_on_consequence", 172);
		}
	}

	private static bool companion_type_select_clickable_condition(out TextObject explanation)
	{
		explanation = new TextObject("{=!}{COMPANION_INQUIRY_COST}{GOLD_ICON}.");
		MBTextManager.SetTextVariable("COMPANION_INQUIRY_COST", 2);
		if (Hero.MainHero.Gold < 2)
		{
			explanation = new TextObject("{=xVZVYNan}You don't have enough{GOLD_ICON}.");
			return false;
		}
		return true;
	}
}
