using System.Collections.Generic;
using System.Linq;
using Helpers;
using TaleWorlds.CampaignSystem.Actions;
using TaleWorlds.CampaignSystem.CharacterDevelopment;
using TaleWorlds.CampaignSystem.Conversation;
using TaleWorlds.CampaignSystem.Extensions;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.CampaignSystem.Roster;
using TaleWorlds.Core;
using TaleWorlds.Library;
using TaleWorlds.Localization;

namespace TaleWorlds.CampaignSystem.CampaignBehaviors;

public class PerkResetCampaignBehavior : CampaignBehaviorBase
{
	private Hero _heroForPerkReset;

	private CharacterAttribute _attributeForPerkReset;

	private SkillObject _selectedSkillForReset;

	private CampaignTime _warningTime;

	public int PerkResetCost
	{
		get
		{
			if (_selectedSkillForReset == null)
			{
				return 0;
			}
			return _heroForPerkReset.GetSkillValue(_selectedSkillForReset) * 40;
		}
	}

	public bool HasEnoughSkillValueForReset
	{
		get
		{
			if (_selectedSkillForReset != null)
			{
				return _heroForPerkReset.GetSkillValue(_selectedSkillForReset) >= 25;
			}
			return false;
		}
	}

	public override void RegisterEvents()
	{
		CampaignEvents.OnSessionLaunchedEvent.AddNonSerializedListener(this, OnSessionLaunched);
		CampaignEvents.DailyTickEvent.AddNonSerializedListener(this, DailyTick);
		CampaignEvents.PerkResetEvent.AddNonSerializedListener(this, OnPerkReset);
	}

	public override void SyncData(IDataStore dataStore)
	{
		dataStore.SyncData("_warningTime", ref _warningTime);
	}

	public void OnSessionLaunched(CampaignGameStarter campaignGameStarter)
	{
		AddDialogs(campaignGameStarter);
	}

	protected void AddDialogs(CampaignGameStarter campaignGameStarter)
	{
		campaignGameStarter.AddDialogLine("arena_intro_7", "arena_intro_perk_reset", "arena_intro_4", "{=ocIutUyu}Also, here at the arena, we think a lot about the arts of war - and many other related skills as well. Often you pick up certain habits while learning your skills. If you need to change those, to practice one of your skills in a certain way, we can help you.", null, null);
		campaignGameStarter.AddPlayerLine("arena_master_ask_player_perk_reset", "arena_master_talk", "arena_master_ask_retrain", "{=Y7tz9D28}These teachers who help people hone their skills and learn new habits... Can you help me find one?", null, null);
		campaignGameStarter.AddDialogLine("arena_master_ask_retrain", "arena_master_ask_retrain", "arena_master_choose_hero", "{=NyWXSHH2}Of course. Was this for you, or someone else?", null, null);
		campaignGameStarter.AddPlayerLine("arena_master_ask_player_perk_reset_2", "arena_master_choose_hero", "arena_master_reset_attribute", "{=3VxA6HaZ}This is for me.", null, conversation_arena_player_select_player_for_perk_reset_on_consequence);
		campaignGameStarter.AddPlayerLine("arena_master_ask_clan_member_perk_reset", "arena_master_choose_hero", "arena_master_reset_attribute", "{=1OKEl18y}This is for {COMPANION.NAME}", conversation_player_has_single_clan_member_on_condition, conversation_player_has_single_clan_member_on_consequence);
		campaignGameStarter.AddPlayerLine("arena_master_ask_clan_member_perk_reset_2", "arena_master_choose_hero", "arena_master_retrain_ask_clan_members", "{=GvcotJmH}I would like you to help hone the skills of a member of my clan.", conversation_player_has_multiple_clan_members_on_condition, conversation_arena_list_clan_members_on_condition);
		campaignGameStarter.AddDialogLine("arena_master_retrain_ask_clan_member", "arena_master_retrain_ask_clan_members", "arena_master_select_clan_member", "{=WRwA0VVS}Which one of your clan members did you wish me to retrain?", null, null);
		campaignGameStarter.AddRepeatablePlayerLine("arena_master_select_clan_member", "arena_master_select_clan_member", "arena_master_reset_attribute", "{=!}{CLAN_MEMBER.NAME}", "{=ElG1LnCA}I am thinking of someone else.", "arena_master_retrain_ask_clan_members", conversation_arena_player_select_clan_member_multiple_on_condition, conversation_arena_player_select_clan_member_for_perk_reset_on_consequence);
		campaignGameStarter.AddPlayerLine("arena_master_select_clan_member_cancel", "arena_master_select_clan_member", "arena_master_pre_talk", "{=D33fIGQe}Never mind.", null, null);
		campaignGameStarter.AddDialogLine("arena_master_reset_attribute", "arena_master_reset_attribute", "arena_master_select_attribute", "{=95jXfam8}What kind of skill is this, speaking broadly? What trait would you say it reflects?", null, null);
		campaignGameStarter.AddRepeatablePlayerLine("arena_master_select_attribute", "arena_master_select_attribute", "arena_master_reset_perks", "{=!}{ATTRIBUTE_NAME}", "{=0G8Q3AZv}I am thinking of a different attribute.", "arena_master_reset_attribute", conversation_arena_player_select_attribute_on_condition, conversation_arena_player_select_attribute_on_consequence);
		campaignGameStarter.AddPlayerLine("arena_master_select_attribute_cancel", "arena_master_select_attribute", "arena_master_pre_talk", "{=g0JOQQl0}I don't want to do this right now.", null, null);
		campaignGameStarter.AddDialogLine("arena_master_reset_perks", "arena_master_reset_perks", "arena_master_select_skill", "{=pGyO41lb}Yes, I can do that. What skill exactly do you have in mind?", null, conversation_arena_set_skills_for_reset_on_consequence);
		campaignGameStarter.AddRepeatablePlayerLine("arena_master_select_skill", "arena_master_select_skill", "arena_master_pay_for_reset", "{=8PV1oB9W}I wish to focus on {SKILL_NAME}.", "{=Z9pq58h4}I am thinking of a different skill.", "arena_master_reset_perks", conversation_arena_player_select_skill_on_condition, conversation_arena_player_select_skill_on_consequence);
		campaignGameStarter.AddPlayerLine("arena_master_select_skill_cancel", "arena_master_select_skill", "arena_master_reset_attribute", "{=CH7b5LaX}I have changed my mind.", null, conversation_arena_list_perks_on_condition);
		campaignGameStarter.AddDialogLine("arena_master_pay_for_reset", "arena_master_pay_for_reset", "arena_master_accept_perk_reset", "{=q3J9Wb8N}If you can afford to pay {GOLD_AMOUNT} {GOLD_ICON} for it, I can teach you right now. Are you sure you want to go through with it?", conversation_arena_ask_price_on_condition, null);
		campaignGameStarter.AddDialogLine("arena_master_selected_skill_invalid", "arena_master_pay_for_reset", "arena_master_reset_attribute", "{=!}{NOT_ENOUGH_SKILL_TEXT}", conversation_arena_skill_not_developed_enough_on_condition, conversation_arena_skill_not_developed_enough_on_consequence);
		campaignGameStarter.AddPlayerLine("arena_master_accept_perk_reset1", "arena_master_accept_perk_reset", "arena_master_perk_reset_closure", "{=Q0UjYw7V}Yes I am sure.", null, conversation_arena_player_accept_perk_reset_on_consequence, 100, conversation_arena_player_accept_price);
		campaignGameStarter.AddPlayerLine("arena_master_reject_perk_reset2", "arena_master_accept_perk_reset", "arena_master_pre_talk", "{=UEbesbKZ}Actually, I have changed my mind.", null, null);
		campaignGameStarter.AddDialogLine("arena_master_perk_reset_closure", "arena_master_perk_reset_closure", "arena_master_perk_reset_final", "{=IsBVxopm}Excellent! Is there anything else I can help you with?", null, null);
		campaignGameStarter.AddPlayerLine("arena_master_perk_reset_final1", "arena_master_perk_reset_final", "arena_master_reset_attribute", "{=aCGgBilx}I would like help fine-tuning another skill.", null, conversation_arena_train_another_skill_on_condition);
		campaignGameStarter.AddPlayerLine("arena_master_perk_reset_final2", "arena_master_perk_reset_final", "arena_master_retrain_ask_clan_members", "{=c4tfVgqb}I would like you to help another member of my clan hone their skills.", conversation_player_has_multiple_clan_members_on_condition, conversation_arena_train_another_clan_member_on_condition);
		campaignGameStarter.AddPlayerLine("arena_master_perk_reset_final3", "arena_master_perk_reset_final", "arena_master_pre_talk", "{=Dz7E79QP}You have already helped enough. Thank you.", null, conversation_arena_finish_perk_reset_dialogs_on_consequence);
	}

	private void OnPerkReset(Hero hero, PerkObject perk)
	{
		if (perk.PrimaryRole == PartyRole.Captain)
		{
			hero.UpdatePowerModifier();
		}
	}

	private void conversation_player_has_single_clan_member_on_consequence()
	{
		_heroForPerkReset = GetClanMembersInParty()[0];
		SetAttributesForDialog();
	}

	private void conversation_arena_skill_not_developed_enough_on_consequence()
	{
		SetAttributesForDialog();
	}

	private bool conversation_arena_skill_not_developed_enough_on_condition()
	{
		TextObject textObject;
		if (_heroForPerkReset == Hero.MainHero)
		{
			textObject = new TextObject("{=FN3xNnd1}You really don't have much experience in this skill, I can't help you much. Maybe we can work on something else?");
		}
		else
		{
			textObject = new TextObject("{=wGAmNQGE}{CHARACTER.NAME} does not have much experience in this skill, I can't help {?CHARACTER.GENDER}her{?}him{\\?} much. Maybe we can work on something else?");
			textObject.SetCharacterProperties("CHARACTER", _heroForPerkReset.CharacterObject);
		}
		MBTextManager.SetTextVariable("NOT_ENOUGH_SKILL_TEXT", textObject);
		return !HasEnoughSkillValueForReset;
	}

	private void conversation_arena_finish_perk_reset_dialogs_on_consequence()
	{
		_heroForPerkReset = null;
		_attributeForPerkReset = null;
		_selectedSkillForReset = null;
	}

	private void conversation_arena_train_another_skill_on_condition()
	{
		SetAttributesForDialog();
	}

	private void conversation_arena_train_another_clan_member_on_condition()
	{
		SetHeroesForDialog();
	}

	private void conversation_arena_player_accept_perk_reset_on_consequence()
	{
		GiveGoldAction.ApplyBetweenCharacters(Hero.MainHero, null, PerkResetCost);
		ResetPerkTreeForHero(_heroForPerkReset, _selectedSkillForReset);
	}

	private bool conversation_arena_player_accept_price(out TextObject explanation)
	{
		if (Hero.MainHero.Gold < PerkResetCost)
		{
			explanation = new TextObject("{=QOWyEJrm}You don't have enough denars.");
			return false;
		}
		explanation = new TextObject("{=ePmSvu1s}{AMOUNT}{GOLD_ICON}");
		explanation.SetTextVariable("AMOUNT", PerkResetCost);
		return true;
	}

	private void conversation_arena_player_select_skill_on_consequence()
	{
		_selectedSkillForReset = ConversationSentence.SelectedRepeatObject as SkillObject;
	}

	private bool conversation_arena_ask_price_on_condition()
	{
		if (HasEnoughSkillValueForReset)
		{
			MBTextManager.SetTextVariable("GOLD_AMOUNT", PerkResetCost);
			MBTextManager.SetTextVariable("GOLD_ICON", "{=!}<img src=\"General\\Icons\\Coin@2x\" extend=\"6\">");
			return true;
		}
		return false;
	}

	private bool conversation_arena_player_select_skill_on_condition()
	{
		if (ConversationSentence.CurrentProcessedRepeatObject is SkillObject skillObject)
		{
			ConversationSentence.SelectedRepeatLine.SetTextVariable("SKILL_NAME", skillObject.Name);
			return true;
		}
		return false;
	}

	private void conversation_arena_set_skills_for_reset_on_consequence()
	{
		SetSkillsForDialog();
	}

	private void conversation_arena_list_perks_on_condition()
	{
		SetAttributesForDialog();
	}

	private void conversation_arena_player_select_attribute_on_consequence()
	{
		_attributeForPerkReset = ConversationSentence.SelectedRepeatObject as CharacterAttribute;
		SetSkillsForDialog();
	}

	private bool conversation_arena_player_select_attribute_on_condition()
	{
		if (ConversationSentence.CurrentProcessedRepeatObject is CharacterAttribute characterAttribute)
		{
			ConversationSentence.SelectedRepeatLine.SetTextVariable("ATTRIBUTE_NAME", characterAttribute.Name);
			return true;
		}
		return false;
	}

	private void conversation_arena_player_select_clan_member_for_perk_reset_on_consequence()
	{
		_heroForPerkReset = ConversationSentence.SelectedRepeatObject as Hero;
		SetAttributesForDialog();
	}

	private void conversation_arena_player_select_player_for_perk_reset_on_consequence()
	{
		_heroForPerkReset = Hero.MainHero;
		SetAttributesForDialog();
	}

	private void conversation_arena_list_clan_members_on_condition()
	{
		SetHeroesForDialog();
	}

	private bool conversation_arena_player_select_clan_member_multiple_on_condition()
	{
		if (ConversationSentence.CurrentProcessedRepeatObject is Hero hero)
		{
			ConversationSentence.SelectedRepeatLine.SetCharacterProperties("CLAN_MEMBER", hero.CharacterObject);
			return true;
		}
		return false;
	}

	private bool conversation_player_has_multiple_clan_members_on_condition()
	{
		return GetClanMembersInParty().Count > 1;
	}

	private bool conversation_player_has_single_clan_member_on_condition()
	{
		List<Hero> clanMembersInParty = GetClanMembersInParty();
		if (clanMembersInParty.Count == 1)
		{
			StringHelpers.SetCharacterProperties("COMPANION", clanMembersInParty[0].CharacterObject);
			return true;
		}
		return false;
	}

	private void DailyTick()
	{
		if (Clan.PlayerClan.Companions.Count > Clan.PlayerClan.CompanionLimit)
		{
			if (_warningTime != CampaignTime.Zero)
			{
				if (_warningTime.ElapsedDaysUntilNow > 6f)
				{
					RemoveACompanionFromPlayerParty();
				}
			}
			else
			{
				WarnPlayerAboutCompanionLimit();
			}
		}
		else
		{
			_warningTime = CampaignTime.Zero;
		}
	}

	private void SetHeroesForDialog()
	{
		ConversationSentence.SetObjectsToRepeatOver(GetClanMembersInParty());
	}

	private void SetAttributesForDialog()
	{
		ConversationSentence.SetObjectsToRepeatOver(Attributes.All.ToList());
	}

	private void SetSkillsForDialog()
	{
		ConversationSentence.SetObjectsToRepeatOver(Skills.All.Where((SkillObject s) => s.Attributes.Contains(_attributeForPerkReset)).ToList());
	}

	private void ResetPerkTreeForHero(Hero hero, SkillObject skill)
	{
		PerkHelper.ClearPerksForSkill(hero, skill);
	}

	private void ClearPermanentBonusesIfExists(Hero hero, PerkObject perk)
	{
		if (hero.GetPerkValue(perk))
		{
			if (perk == DefaultPerks.Crafting.VigorousSmith)
			{
				hero.HeroDeveloper.RemoveAttribute(DefaultCharacterAttributes.Vigor, 1);
			}
			else if (perk == DefaultPerks.Crafting.StrongSmith)
			{
				hero.HeroDeveloper.RemoveAttribute(DefaultCharacterAttributes.Control, 1);
			}
			else if (perk == DefaultPerks.Crafting.EnduringSmith)
			{
				hero.HeroDeveloper.RemoveAttribute(DefaultCharacterAttributes.Endurance, 1);
			}
			else if (perk == DefaultPerks.Crafting.WeaponMasterSmith)
			{
				hero.HeroDeveloper.RemoveFocus(DefaultSkills.OneHanded, 1);
				hero.HeroDeveloper.RemoveFocus(DefaultSkills.TwoHanded, 1);
			}
			else if (perk == DefaultPerks.Athletics.Durable)
			{
				hero.HeroDeveloper.RemoveAttribute(DefaultCharacterAttributes.Endurance, 1);
			}
			else if (perk == DefaultPerks.Athletics.Steady)
			{
				hero.HeroDeveloper.RemoveAttribute(DefaultCharacterAttributes.Control, 1);
			}
			else if (perk == DefaultPerks.Athletics.Strong)
			{
				hero.HeroDeveloper.RemoveAttribute(DefaultCharacterAttributes.Vigor, 1);
			}
		}
	}

	private void ClearPerksForSkill(Hero hero, SkillObject skill)
	{
		foreach (PerkObject item in PerkObject.All)
		{
			if (item.Skill == skill)
			{
				ClearPermanentBonusesIfExists(hero, item);
				hero.SetPerkValueInternal(item, value: false);
			}
		}
		PartyBase.MainParty.MemberRoster.UpdateVersion();
		hero.HitPoints = MathF.Min(hero.HitPoints, hero.MaxHitPoints);
	}

	private void RemoveACompanionFromPlayerParty()
	{
		int count = Clan.PlayerClan.Companions.Count;
		int num = MBRandom.RandomInt(count);
		for (int i = 0; i < count; i++)
		{
			int index = (i + num) % count;
			Hero hero = Clan.PlayerClan.Companions[index];
			if (hero.PartyBelongedTo?.MapEvent == null && hero.CurrentSettlement?.Party.MapEvent == null && !Campaign.Current.IssueManager.IssueSolvingCompanionList.Contains(hero))
			{
				KillCharacterAction.ApplyByRemove(hero, showNotification: true);
				break;
			}
		}
	}

	private void WarnPlayerAboutCompanionLimit()
	{
		MBInformationManager.AddQuickInformation(new TextObject("{=xDikJxbO}Your party is above your companion limits. Due to that some of the companions might leave soon."), 0, null, null, "event:/ui/notification/relation");
		_warningTime = CampaignTime.Now;
	}

	private List<Hero> GetClanMembersInParty()
	{
		return (from t in PartyBase.MainParty.MemberRoster.GetTroopRoster()
			where t.Character.IsHero && t.Character.HeroObject.Clan == Clan.PlayerClan && !t.Character.HeroObject.IsHumanPlayerCharacter
			select t.Character.HeroObject).ToList();
	}
}
