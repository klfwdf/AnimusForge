using System;
using System.Collections.Generic;
using System.Linq;
using Helpers;
using TaleWorlds.CampaignSystem.Actions;
using TaleWorlds.CampaignSystem.CharacterDevelopment;
using TaleWorlds.CampaignSystem.Conversation;
using TaleWorlds.CampaignSystem.Extensions;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.CampaignSystem.Roster;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.Core;
using TaleWorlds.Library;
using TaleWorlds.Localization;

namespace TaleWorlds.CampaignSystem.CampaignBehaviors
{
	// Token: 0x0200042C RID: 1068
	public class PerkResetCampaignBehavior : CampaignBehaviorBase
	{
		// Token: 0x17000E24 RID: 3620
		// (get) Token: 0x06004373 RID: 17267 RVA: 0x00146ECA File Offset: 0x001450CA
		public int PerkResetCost
		{
			get
			{
				if (this._selectedSkillForReset == null)
				{
					return 0;
				}
				return this._heroForPerkReset.GetSkillValue(this._selectedSkillForReset) * 40;
			}
		}

		// Token: 0x17000E25 RID: 3621
		// (get) Token: 0x06004374 RID: 17268 RVA: 0x00146EEA File Offset: 0x001450EA
		public bool HasEnoughSkillValueForReset
		{
			get
			{
				return this._selectedSkillForReset != null && this._heroForPerkReset.GetSkillValue(this._selectedSkillForReset) >= 25;
			}
		}

		// Token: 0x06004375 RID: 17269 RVA: 0x00146F10 File Offset: 0x00145110
		public override void RegisterEvents()
		{
			CampaignEvents.OnSessionLaunchedEvent.AddNonSerializedListener(this, new Action<CampaignGameStarter>(this.OnSessionLaunched));
			CampaignEvents.DailyTickEvent.AddNonSerializedListener(this, new Action(this.DailyTick));
			CampaignEvents.PerkResetEvent.AddNonSerializedListener(this, new Action<Hero, PerkObject>(this.OnPerkReset));
		}

		// Token: 0x06004376 RID: 17270 RVA: 0x00146F62 File Offset: 0x00145162
		public override void SyncData(IDataStore dataStore)
		{
			dataStore.SyncData<CampaignTime>("_warningTime", ref this._warningTime);
		}

		// Token: 0x06004377 RID: 17271 RVA: 0x00146F76 File Offset: 0x00145176
		public void OnSessionLaunched(CampaignGameStarter campaignGameStarter)
		{
			this.AddDialogs(campaignGameStarter);
		}

		// Token: 0x06004378 RID: 17272 RVA: 0x00146F80 File Offset: 0x00145180
		protected void AddDialogs(CampaignGameStarter campaignGameStarter)
		{
			campaignGameStarter.AddDialogLine("arena_intro_7", "arena_intro_perk_reset", "arena_intro_4", "{=ocIutUyu}Also, here at the arena, we think a lot about the arts of war - and many other related skills as well. Often you pick up certain habits while learning your skills. If you need to change those, to practice one of your skills in a certain way, we can help you.", null, null, 100, null);
			campaignGameStarter.AddPlayerLine("arena_master_ask_player_perk_reset", "arena_master_talk", "arena_master_ask_retrain", "{=Y7tz9D28}These teachers who help people hone their skills and learn new habits... Can you help me find one?", null, null, 100, null, null);
			campaignGameStarter.AddDialogLine("arena_master_ask_retrain", "arena_master_ask_retrain", "arena_master_choose_hero", "{=NyWXSHH2}Of course. Was this for you, or someone else?", null, null, 100, null);
			campaignGameStarter.AddPlayerLine("arena_master_ask_player_perk_reset_2", "arena_master_choose_hero", "arena_master_reset_attribute", "{=3VxA6HaZ}This is for me.", null, new ConversationSentence.OnConsequenceDelegate(this.conversation_arena_player_select_player_for_perk_reset_on_consequence), 100, null, null);
			campaignGameStarter.AddPlayerLine("arena_master_ask_clan_member_perk_reset", "arena_master_choose_hero", "arena_master_reset_attribute", "{=1OKEl18y}This is for {COMPANION.NAME}", new ConversationSentence.OnConditionDelegate(this.conversation_player_has_single_clan_member_on_condition), new ConversationSentence.OnConsequenceDelegate(this.conversation_player_has_single_clan_member_on_consequence), 100, null, null);
			campaignGameStarter.AddPlayerLine("arena_master_ask_clan_member_perk_reset_2", "arena_master_choose_hero", "arena_master_retrain_ask_clan_members", "{=GvcotJmH}I would like you to help hone the skills of a member of my clan.", new ConversationSentence.OnConditionDelegate(this.conversation_player_has_multiple_clan_members_on_condition), new ConversationSentence.OnConsequenceDelegate(this.conversation_arena_list_clan_members_on_condition), 100, null, null);
			campaignGameStarter.AddDialogLine("arena_master_retrain_ask_clan_member", "arena_master_retrain_ask_clan_members", "arena_master_select_clan_member", "{=WRwA0VVS}Which one of your clan members did you wish me to retrain?", null, null, 100, null);
			campaignGameStarter.AddRepeatablePlayerLine("arena_master_select_clan_member", "arena_master_select_clan_member", "arena_master_reset_attribute", "{=!}{CLAN_MEMBER.NAME}", "{=ElG1LnCA}I am thinking of someone else.", "arena_master_retrain_ask_clan_members", new ConversationSentence.OnConditionDelegate(this.conversation_arena_player_select_clan_member_multiple_on_condition), new ConversationSentence.OnConsequenceDelegate(this.conversation_arena_player_select_clan_member_for_perk_reset_on_consequence), 100, null);
			campaignGameStarter.AddPlayerLine("arena_master_select_clan_member_cancel", "arena_master_select_clan_member", "arena_master_pre_talk", "{=D33fIGQe}Never mind.", null, null, 100, null, null);
			campaignGameStarter.AddDialogLine("arena_master_reset_attribute", "arena_master_reset_attribute", "arena_master_select_attribute", "{=95jXfam8}What kind of skill is this, speaking broadly? What trait would you say it reflects?", null, null, 100, null);
			campaignGameStarter.AddRepeatablePlayerLine("arena_master_select_attribute", "arena_master_select_attribute", "arena_master_reset_perks", "{=!}{ATTRIBUTE_NAME}", "{=0G8Q3AZv}I am thinking of a different attribute.", "arena_master_reset_attribute", new ConversationSentence.OnConditionDelegate(this.conversation_arena_player_select_attribute_on_condition), new ConversationSentence.OnConsequenceDelegate(this.conversation_arena_player_select_attribute_on_consequence), 100, null);
			campaignGameStarter.AddPlayerLine("arena_master_select_attribute_cancel", "arena_master_select_attribute", "arena_master_pre_talk", "{=g0JOQQl0}I don't want to do this right now.", null, null, 100, null, null);
			campaignGameStarter.AddDialogLine("arena_master_reset_perks", "arena_master_reset_perks", "arena_master_select_skill", "{=pGyO41lb}Yes, I can do that. What skill exactly do you have in mind?", null, new ConversationSentence.OnConsequenceDelegate(this.conversation_arena_set_skills_for_reset_on_consequence), 100, null);
			campaignGameStarter.AddRepeatablePlayerLine("arena_master_select_skill", "arena_master_select_skill", "arena_master_pay_for_reset", "{=8PV1oB9W}I wish to focus on {SKILL_NAME}.", "{=Z9pq58h4}I am thinking of a different skill.", "arena_master_reset_perks", new ConversationSentence.OnConditionDelegate(this.conversation_arena_player_select_skill_on_condition), new ConversationSentence.OnConsequenceDelegate(this.conversation_arena_player_select_skill_on_consequence), 100, null);
			campaignGameStarter.AddPlayerLine("arena_master_select_skill_cancel", "arena_master_select_skill", "arena_master_reset_attribute", "{=CH7b5LaX}I have changed my mind.", null, new ConversationSentence.OnConsequenceDelegate(this.conversation_arena_list_perks_on_condition), 100, null, null);
			campaignGameStarter.AddDialogLine("arena_master_pay_for_reset", "arena_master_pay_for_reset", "arena_master_accept_perk_reset", "{=q3J9Wb8N}If you can afford to pay {GOLD_AMOUNT} {GOLD_ICON} for it, I can teach you right now. Are you sure you want to go through with it?", new ConversationSentence.OnConditionDelegate(this.conversation_arena_ask_price_on_condition), null, 100, null);
			campaignGameStarter.AddDialogLine("arena_master_selected_skill_invalid", "arena_master_pay_for_reset", "arena_master_reset_attribute", "{=!}{NOT_ENOUGH_SKILL_TEXT}", new ConversationSentence.OnConditionDelegate(this.conversation_arena_skill_not_developed_enough_on_condition), new ConversationSentence.OnConsequenceDelegate(this.conversation_arena_skill_not_developed_enough_on_consequence), 100, null);
			campaignGameStarter.AddPlayerLine("arena_master_accept_perk_reset1", "arena_master_accept_perk_reset", "arena_master_perk_reset_closure", "{=Q0UjYw7V}Yes I am sure.", null, new ConversationSentence.OnConsequenceDelegate(this.conversation_arena_player_accept_perk_reset_on_consequence), 100, new ConversationSentence.OnClickableConditionDelegate(this.conversation_arena_player_accept_price), null);
			campaignGameStarter.AddPlayerLine("arena_master_reject_perk_reset2", "arena_master_accept_perk_reset", "arena_master_pre_talk", "{=UEbesbKZ}Actually, I have changed my mind.", null, null, 100, null, null);
			campaignGameStarter.AddDialogLine("arena_master_perk_reset_closure", "arena_master_perk_reset_closure", "arena_master_perk_reset_final", "{=IsBVxopm}Excellent! Is there anything else I can help you with?", null, null, 100, null);
			campaignGameStarter.AddPlayerLine("arena_master_perk_reset_final1", "arena_master_perk_reset_final", "arena_master_reset_attribute", "{=aCGgBilx}I would like help fine-tuning another skill.", null, new ConversationSentence.OnConsequenceDelegate(this.conversation_arena_train_another_skill_on_condition), 100, null, null);
			campaignGameStarter.AddPlayerLine("arena_master_perk_reset_final2", "arena_master_perk_reset_final", "arena_master_retrain_ask_clan_members", "{=c4tfVgqb}I would like you to help another member of my clan hone their skills.", new ConversationSentence.OnConditionDelegate(this.conversation_player_has_multiple_clan_members_on_condition), new ConversationSentence.OnConsequenceDelegate(this.conversation_arena_train_another_clan_member_on_condition), 100, null, null);
			campaignGameStarter.AddPlayerLine("arena_master_perk_reset_final3", "arena_master_perk_reset_final", "arena_master_pre_talk", "{=Dz7E79QP}You have already helped enough. Thank you.", null, new ConversationSentence.OnConsequenceDelegate(this.conversation_arena_finish_perk_reset_dialogs_on_consequence), 100, null, null);
		}

		// Token: 0x06004379 RID: 17273 RVA: 0x00147389 File Offset: 0x00145589
		private void OnPerkReset(Hero hero, PerkObject perk)
		{
			if (perk.PrimaryRole == PartyRole.Captain)
			{
				hero.UpdatePowerModifier();
			}
		}

		// Token: 0x0600437A RID: 17274 RVA: 0x0014739B File Offset: 0x0014559B
		private void conversation_player_has_single_clan_member_on_consequence()
		{
			this._heroForPerkReset = this.GetClanMembersInParty()[0];
			this.SetAttributesForDialog();
		}

		// Token: 0x0600437B RID: 17275 RVA: 0x001473B5 File Offset: 0x001455B5
		private void conversation_arena_skill_not_developed_enough_on_consequence()
		{
			this.SetAttributesForDialog();
		}

		// Token: 0x0600437C RID: 17276 RVA: 0x001473C0 File Offset: 0x001455C0
		private bool conversation_arena_skill_not_developed_enough_on_condition()
		{
			TextObject textObject;
			if (this._heroForPerkReset == Hero.MainHero)
			{
				textObject = new TextObject("{=FN3xNnd1}You really don't have much experience in this skill, I can't help you much. Maybe we can work on something else?", null);
			}
			else
			{
				textObject = new TextObject("{=wGAmNQGE}{CHARACTER.NAME} does not have much experience in this skill, I can't help {?CHARACTER.GENDER}her{?}him{\\?} much. Maybe we can work on something else?", null);
				textObject.SetCharacterProperties("CHARACTER", this._heroForPerkReset.CharacterObject, false);
			}
			MBTextManager.SetTextVariable("NOT_ENOUGH_SKILL_TEXT", textObject, false);
			return !this.HasEnoughSkillValueForReset;
		}

		// Token: 0x0600437D RID: 17277 RVA: 0x00147420 File Offset: 0x00145620
		private void conversation_arena_finish_perk_reset_dialogs_on_consequence()
		{
			this._heroForPerkReset = null;
			this._attributeForPerkReset = null;
			this._selectedSkillForReset = null;
		}

		// Token: 0x0600437E RID: 17278 RVA: 0x00147437 File Offset: 0x00145637
		private void conversation_arena_train_another_skill_on_condition()
		{
			this.SetAttributesForDialog();
		}

		// Token: 0x0600437F RID: 17279 RVA: 0x0014743F File Offset: 0x0014563F
		private void conversation_arena_train_another_clan_member_on_condition()
		{
			this.SetHeroesForDialog();
		}

		// Token: 0x06004380 RID: 17280 RVA: 0x00147447 File Offset: 0x00145647
		private void conversation_arena_player_accept_perk_reset_on_consequence()
		{
			GiveGoldAction.ApplyBetweenCharacters(Hero.MainHero, null, this.PerkResetCost, false);
			this.ResetPerkTreeForHero(this._heroForPerkReset, this._selectedSkillForReset);
		}

		// Token: 0x06004381 RID: 17281 RVA: 0x00147470 File Offset: 0x00145670
		private bool conversation_arena_player_accept_price(out TextObject explanation)
		{
			if (Hero.MainHero.Gold < this.PerkResetCost)
			{
				explanation = new TextObject("{=QOWyEJrm}You don't have enough denars.", null);
				return false;
			}
			explanation = new TextObject("{=ePmSvu1s}{AMOUNT}{GOLD_ICON}", null);
			explanation.SetTextVariable("AMOUNT", this.PerkResetCost);
			return true;
		}

		// Token: 0x06004382 RID: 17282 RVA: 0x001474BF File Offset: 0x001456BF
		private void conversation_arena_player_select_skill_on_consequence()
		{
			this._selectedSkillForReset = ConversationSentence.SelectedRepeatObject as SkillObject;
		}

		// Token: 0x06004383 RID: 17283 RVA: 0x001474D1 File Offset: 0x001456D1
		private bool conversation_arena_ask_price_on_condition()
		{
			if (this.HasEnoughSkillValueForReset)
			{
				MBTextManager.SetTextVariable("GOLD_AMOUNT", this.PerkResetCost);
				MBTextManager.SetTextVariable("GOLD_ICON", "{=!}<img src=\"General\\Icons\\Coin@2x\" extend=\"8\">", false);
				return true;
			}
			return false;
		}

		// Token: 0x06004384 RID: 17284 RVA: 0x00147500 File Offset: 0x00145700
		private bool conversation_arena_player_select_skill_on_condition()
		{
			SkillObject skillObject = ConversationSentence.CurrentProcessedRepeatObject as SkillObject;
			if (skillObject != null)
			{
				ConversationSentence.SelectedRepeatLine.SetTextVariable("SKILL_NAME", skillObject.Name);
				return true;
			}
			return false;
		}

		// Token: 0x06004385 RID: 17285 RVA: 0x00147534 File Offset: 0x00145734
		private void conversation_arena_set_skills_for_reset_on_consequence()
		{
			this.SetSkillsForDialog();
		}

		// Token: 0x06004386 RID: 17286 RVA: 0x0014753C File Offset: 0x0014573C
		private void conversation_arena_list_perks_on_condition()
		{
			this.SetAttributesForDialog();
		}

		// Token: 0x06004387 RID: 17287 RVA: 0x00147544 File Offset: 0x00145744
		private void conversation_arena_player_select_attribute_on_consequence()
		{
			this._attributeForPerkReset = ConversationSentence.SelectedRepeatObject as CharacterAttribute;
			this.SetSkillsForDialog();
		}

		// Token: 0x06004388 RID: 17288 RVA: 0x0014755C File Offset: 0x0014575C
		private bool conversation_arena_player_select_attribute_on_condition()
		{
			CharacterAttribute characterAttribute = ConversationSentence.CurrentProcessedRepeatObject as CharacterAttribute;
			if (characterAttribute != null)
			{
				ConversationSentence.SelectedRepeatLine.SetTextVariable("ATTRIBUTE_NAME", characterAttribute.Name);
				return true;
			}
			return false;
		}

		// Token: 0x06004389 RID: 17289 RVA: 0x00147590 File Offset: 0x00145790
		private void conversation_arena_player_select_clan_member_for_perk_reset_on_consequence()
		{
			this._heroForPerkReset = ConversationSentence.SelectedRepeatObject as Hero;
			this.SetAttributesForDialog();
		}

		// Token: 0x0600438A RID: 17290 RVA: 0x001475A8 File Offset: 0x001457A8
		private void conversation_arena_player_select_player_for_perk_reset_on_consequence()
		{
			this._heroForPerkReset = Hero.MainHero;
			this.SetAttributesForDialog();
		}

		// Token: 0x0600438B RID: 17291 RVA: 0x001475BB File Offset: 0x001457BB
		private void conversation_arena_list_clan_members_on_condition()
		{
			this.SetHeroesForDialog();
		}

		// Token: 0x0600438C RID: 17292 RVA: 0x001475C4 File Offset: 0x001457C4
		private bool conversation_arena_player_select_clan_member_multiple_on_condition()
		{
			Hero hero = ConversationSentence.CurrentProcessedRepeatObject as Hero;
			if (hero != null)
			{
				ConversationSentence.SelectedRepeatLine.SetCharacterProperties("CLAN_MEMBER", hero.CharacterObject, false);
				return true;
			}
			return false;
		}

		// Token: 0x0600438D RID: 17293 RVA: 0x001475F8 File Offset: 0x001457F8
		private bool conversation_player_has_multiple_clan_members_on_condition()
		{
			return this.GetClanMembersInParty().Count > 1;
		}

		// Token: 0x0600438E RID: 17294 RVA: 0x00147608 File Offset: 0x00145808
		private bool conversation_player_has_single_clan_member_on_condition()
		{
			List<Hero> clanMembersInParty = this.GetClanMembersInParty();
			if (clanMembersInParty.Count == 1)
			{
				StringHelpers.SetCharacterProperties("COMPANION", clanMembersInParty[0].CharacterObject, null, false);
				return true;
			}
			return false;
		}

		// Token: 0x0600438F RID: 17295 RVA: 0x00147644 File Offset: 0x00145844
		private void DailyTick()
		{
			if (Clan.PlayerClan.Companions.Count > Clan.PlayerClan.CompanionLimit)
			{
				if (!(this._warningTime != CampaignTime.Zero))
				{
					this.WarnPlayerAboutCompanionLimit();
					return;
				}
				if (this._warningTime.ElapsedDaysUntilNow > 6f)
				{
					this.RemoveACompanionFromPlayerParty();
					return;
				}
			}
			else
			{
				this._warningTime = CampaignTime.Zero;
			}
		}

		// Token: 0x06004390 RID: 17296 RVA: 0x001476A9 File Offset: 0x001458A9
		private void SetHeroesForDialog()
		{
			ConversationSentence.SetObjectsToRepeatOver(this.GetClanMembersInParty(), 5);
		}

		// Token: 0x06004391 RID: 17297 RVA: 0x001476B7 File Offset: 0x001458B7
		private void SetAttributesForDialog()
		{
			ConversationSentence.SetObjectsToRepeatOver(Attributes.All.ToList<CharacterAttribute>(), 5);
		}

		// Token: 0x06004392 RID: 17298 RVA: 0x001476C9 File Offset: 0x001458C9
		private void SetSkillsForDialog()
		{
			ConversationSentence.SetObjectsToRepeatOver((from s in Skills.All
				where s.Attributes.Contains(this._attributeForPerkReset)
				select s).ToList<SkillObject>(), 5);
		}

		// Token: 0x06004393 RID: 17299 RVA: 0x001476EC File Offset: 0x001458EC
		private void ResetPerkTreeForHero(Hero hero, SkillObject skill)
		{
			this.ClearPerksForSkill(hero, skill);
		}

		// Token: 0x06004394 RID: 17300 RVA: 0x001476F8 File Offset: 0x001458F8
		private void ClearPermanentBonusesIfExists(Hero hero, PerkObject perk)
		{
			if (!hero.GetPerkValue(perk))
			{
				return;
			}
			if (perk == DefaultPerks.Crafting.VigorousSmith)
			{
				hero.HeroDeveloper.RemoveAttribute(DefaultCharacterAttributes.Vigor, 1);
				return;
			}
			if (perk == DefaultPerks.Crafting.StrongSmith)
			{
				hero.HeroDeveloper.RemoveAttribute(DefaultCharacterAttributes.Control, 1);
				return;
			}
			if (perk == DefaultPerks.Crafting.EnduringSmith)
			{
				hero.HeroDeveloper.RemoveAttribute(DefaultCharacterAttributes.Endurance, 1);
				return;
			}
			if (perk == DefaultPerks.Crafting.WeaponMasterSmith)
			{
				hero.HeroDeveloper.RemoveFocus(DefaultSkills.OneHanded, 1);
				hero.HeroDeveloper.RemoveFocus(DefaultSkills.TwoHanded, 1);
				return;
			}
			if (perk == DefaultPerks.Athletics.Durable)
			{
				hero.HeroDeveloper.RemoveAttribute(DefaultCharacterAttributes.Endurance, 1);
				return;
			}
			if (perk == DefaultPerks.Athletics.Steady)
			{
				hero.HeroDeveloper.RemoveAttribute(DefaultCharacterAttributes.Control, 1);
				return;
			}
			if (perk == DefaultPerks.Athletics.Strong)
			{
				hero.HeroDeveloper.RemoveAttribute(DefaultCharacterAttributes.Vigor, 1);
			}
		}

		// Token: 0x06004395 RID: 17301 RVA: 0x001477D8 File Offset: 0x001459D8
		private void ClearPerksForSkill(Hero hero, SkillObject skill)
		{
			foreach (PerkObject perkObject in PerkObject.All)
			{
				if (perkObject.Skill == skill)
				{
					this.ClearPermanentBonusesIfExists(hero, perkObject);
					hero.SetPerkValueInternal(perkObject, false);
				}
			}
			PartyBase.MainParty.MemberRoster.UpdateVersion();
			hero.HitPoints = MathF.Min(hero.HitPoints, hero.MaxHitPoints);
		}

		// Token: 0x06004396 RID: 17302 RVA: 0x00147864 File Offset: 0x00145A64
		private void RemoveACompanionFromPlayerParty()
		{
			int count = Clan.PlayerClan.Companions.Count;
			int num = MBRandom.RandomInt(count);
			for (int i = 0; i < count; i++)
			{
				int index = (i + num) % count;
				Hero hero = Clan.PlayerClan.Companions[index];
				MobileParty partyBelongedTo = hero.PartyBelongedTo;
				if (((partyBelongedTo != null) ? partyBelongedTo.MapEvent : null) == null)
				{
					Settlement currentSettlement = hero.CurrentSettlement;
					if (((currentSettlement != null) ? currentSettlement.Party.MapEvent : null) == null && !Campaign.Current.IssueManager.IssueSolvingCompanionList.Contains(hero))
					{
						KillCharacterAction.ApplyByRemove(hero, true, true);
						return;
					}
				}
			}
		}

		// Token: 0x06004397 RID: 17303 RVA: 0x001478FD File Offset: 0x00145AFD
		private void WarnPlayerAboutCompanionLimit()
		{
			MBInformationManager.AddQuickInformation(new TextObject("{=xDikJxbO}Your party is above your companion limits. Due to that some of the companions might leave soon.", null), 0, null, null, "event:/ui/notification/relation");
			this._warningTime = CampaignTime.Now;
		}

		// Token: 0x06004398 RID: 17304 RVA: 0x00147924 File Offset: 0x00145B24
		private List<Hero> GetClanMembersInParty()
		{
			return (from m in PartyBase.MainParty.MemberRoster.GetTroopRoster()
				where m.Character.IsHero && m.Character.HeroObject.Clan == Clan.PlayerClan && !m.Character.HeroObject.IsHumanPlayerCharacter
				select m into t
				select t.Character.HeroObject).ToList<Hero>();
		}

		// Token: 0x0400132B RID: 4907
		private Hero _heroForPerkReset;

		// Token: 0x0400132C RID: 4908
		private CharacterAttribute _attributeForPerkReset;

		// Token: 0x0400132D RID: 4909
		private SkillObject _selectedSkillForReset;

		// Token: 0x0400132E RID: 4910
		private CampaignTime _warningTime;
	}
}
