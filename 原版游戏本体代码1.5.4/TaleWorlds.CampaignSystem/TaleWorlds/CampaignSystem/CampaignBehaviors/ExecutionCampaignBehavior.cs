using System.Collections.Generic;
using Helpers;
using TaleWorlds.CampaignSystem.Actions;
using TaleWorlds.CampaignSystem.CharacterDevelopment;
using TaleWorlds.CampaignSystem.Conversation;
using TaleWorlds.CampaignSystem.GameMenus;
using TaleWorlds.CampaignSystem.MapNotificationTypes;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.CampaignSystem.Roster;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.Core;
using TaleWorlds.Library;
using TaleWorlds.Localization;

namespace TaleWorlds.CampaignSystem.CampaignBehaviors;

public class ExecutionCampaignBehavior : CampaignBehaviorBase
{
	private Dictionary<Hero, (Clan CapturerClan, CampaignTime ExecutionDate)> _pendingClanMemberSettlementExecutions = new Dictionary<Hero, (Clan, CampaignTime)>();

	private List<Hero> _heroesPendingMapEventEndToBeExecuted = new List<Hero>();

	private bool _isMainHeroExecuted;

	private Clan _selectedBloodFeudClan;

	private const int PlayerExecutionRelationThreshold = -50;

	private const int BloodFeudEndRelationLevel = 0;

	private const int DaysTillBloodFeudExecutionInSettlement = 8;

	private float CalculatePlayerExecutionProbability(Hero executor)
	{
		float num = 0f;
		if (executor.Clan.HasBloodFeudWithPlayer)
		{
			return 1f;
		}
		int traitLevel = executor.GetTraitLevel(DefaultTraits.Mercy);
		float num2 = (float)traitLevel * 10f - 50f;
		float num3 = executor.Clan.GetRelationWithClan(Clan.PlayerClan);
		if (num3 >= num2)
		{
			return 0f;
		}
		return MathF.Max(((0f - num3) * 0.3f - (float)traitLevel * 5f) * 0.01f, 0f);
	}

	private float CalculatePlayerClanMemberExecutionProbability(Hero clanMember, Hero executor)
	{
		float num = 0f;
		int traitLevel = executor.GetTraitLevel(DefaultTraits.Mercy);
		float elapsedDaysUntilNow = clanMember.CaptivityStartTime.ElapsedDaysUntilNow;
		if (executor.Clan.HasBloodFeudWithPlayer)
		{
			float num2 = MBMath.ClampFloat(elapsedDaysUntilNow * 0.1f, 0f, 1f);
			return num2 * num2;
		}
		float num3 = executor.Clan.GetRelationWithClan(Clan.PlayerClan);
		if (num3 > -50f)
		{
			return 0f;
		}
		return MathF.Max(((0f - num3) * 0.3f - (float)traitLevel * 5f) * 0.01f, 0f);
	}

	public static int GetBloodFeudStartRelationPenaltyToOtherClan(Hero dyingHero, Clan otherClan)
	{
		if (otherClan.IsEliminated || otherClan.IsBanditFaction || otherClan.Leader == null)
		{
			return 0;
		}
		if (otherClan.GetRelationWithClan(dyingHero.Clan) >= Campaign.Current.Models.DiplomacyModel.MaxNeutralRelationLimit)
		{
			if (dyingHero.GetTraitLevel(DefaultTraits.Honor) >= 0 && !dyingHero.Clan.IsRebelClan && dyingHero.Clan.IsNoble)
			{
				return -45;
			}
			return -30;
		}
		if (otherClan.Kingdom == dyingHero.Clan.Kingdom)
		{
			if (dyingHero.GetTraitLevel(DefaultTraits.Honor) >= 0 && !dyingHero.Clan.IsRebelClan && dyingHero.Clan.IsNoble)
			{
				return -50;
			}
			return -25;
		}
		return 0;
	}

	private static int GetBloodMoneyForPlayerToPayAgainstClan(Clan clanWithFeud)
	{
		int num = 5000;
		if (clanWithFeud.BloodFeudExecutionsDoneCount < clanWithFeud.BloodFeudExecutionsReceivedCount)
		{
			num += 50000 * (clanWithFeud.BloodFeudExecutionsReceivedCount - clanWithFeud.BloodFeudExecutionsDoneCount);
		}
		return num;
	}

	public override void RegisterEvents()
	{
		CampaignEvents.HeroKilledEvent.AddNonSerializedListener(this, OnHeroKilled);
		CampaignEvents.HeroPrisonerTaken.AddNonSerializedListener(this, OnHeroTakenPrisoner);
		CampaignEvents.HeroPrisonerReleased.AddNonSerializedListener(this, OnHeroPrisonerReleased);
		CampaignEvents.OnSettlementOwnerChangedEvent.AddNonSerializedListener(this, OnSettlementOwnerChanged);
		CampaignEvents.DailyTickHeroEvent.AddNonSerializedListener(this, DailyTickHero);
		CampaignEvents.HeroRelationChanged.AddNonSerializedListener(this, OnHeroRelationChanged);
		CampaignEvents.GameMenuOpened.AddNonSerializedListener(this, OnGameMenuOpened);
		CampaignEvents.OnBloodFeudStateChangedEvent.AddNonSerializedListener(this, OnBloodFeudStateChanged);
		CampaignEvents.QuarterHourlyTickEvent.AddNonSerializedListener(this, QuarterHourlyTick);
		CampaignEvents.OnDeathMarkAddedEvent.AddNonSerializedListener(this, OnDeathMarkAdded);
		CampaignEvents.PrisonersChangeInSettlement.AddNonSerializedListener(this, OnPrisonersChangedInSettlement);
		CampaignEvents.OnSessionLaunchedEvent.AddNonSerializedListener(this, OnSessionLaunched);
		CampaignEvents.CanHeroBeReleasedEvent.AddNonSerializedListener(this, CanHeroBeReleased);
		CampaignEvents.OnClanDestroyedEvent.AddNonSerializedListener(this, OnClanDestroyed);
		CampaignEvents.OnPrisonerDonatedToSettlementEvent.AddNonSerializedListener(this, OnPrisonerDonatedToSettlement);
	}

	private void OnPrisonerDonatedToSettlement(MobileParty oldOwner, FlattenedTroopRoster prisonerRoster, Settlement toSettlement)
	{
		if (!oldOwner.ActualClan.HasBloodFeudWithPlayer)
		{
			return;
		}
		foreach (CharacterObject troop in prisonerRoster.Troops)
		{
			if (troop.IsHero && troop.HeroObject != Hero.MainHero && troop.HeroObject.Clan == Clan.PlayerClan && !_pendingClanMemberSettlementExecutions.ContainsKey(troop.HeroObject))
			{
				StartClanMemberExecutionAtSettlement(troop.HeroObject, oldOwner.ActualClan);
			}
		}
	}

	private void OnHeroKilled(Hero victim, Hero killer, KillCharacterAction.KillCharacterActionDetail detail, bool showNotification = true)
	{
		if (detail == KillCharacterAction.KillCharacterActionDetail.Executed && victim.Clan != null && killer.Clan != null)
		{
			if (killer == Hero.MainHero)
			{
				OnPlayerExecutedHero(victim);
			}
			else if (victim.Clan == Clan.PlayerClan)
			{
				OnPlayerClanMemberExecuted(victim, killer);
			}
			else if (killer.Clan == Clan.PlayerClan && victim.Clan.HasBloodFeudWithPlayer)
			{
				OnPlayerMemberExecutedAHero(victim, killer);
			}
			if (victim.Clan.HasBloodFeudWithPlayer)
			{
				victim.Clan.BloodFeudExecutionsReceivedCount++;
			}
			else if (killer.Clan.HasBloodFeudWithPlayer)
			{
				killer.Clan.BloodFeudExecutionsDoneCount++;
			}
		}
		if (_heroesPendingMapEventEndToBeExecuted.Contains(victim))
		{
			_heroesPendingMapEventEndToBeExecuted.Remove(victim);
		}
		if (_pendingClanMemberSettlementExecutions.ContainsKey(victim))
		{
			_pendingClanMemberSettlementExecutions.Remove(victim);
		}
	}

	private void OnPlayerExecutedHero(Hero victim)
	{
		if (!victim.Clan.HasBloodFeudWithPlayer)
		{
			ChangeBloodFeudStateAction.StartBloodFeudWithClanByPlayerExecutingAHero(victim.Clan, victim);
			TraitLevelingHelper.OnBloodFeudStarted(victim);
		}
	}

	private void OnPlayerClanMemberExecuted(Hero executedHero, Hero executor)
	{
		if (!executor.Clan.HasBloodFeudWithPlayer)
		{
			ChangeBloodFeudStateAction.StartBloodFeudWithClanByAIExecutingPlayerRelative(executor.Clan, executedHero);
		}
		TextObject textObject = new TextObject("{=xwMNKupG}The {EXECUTOR_CLAN} pursues their feud against you by executing your {RELATION} {CLAN_MEMBER.NAME}.");
		StringHelpers.SetCharacterProperties("CLAN_MEMBER", executedHero.CharacterObject, textObject);
		textObject.SetTextVariable("EXECUTOR_CLAN", executor.Clan.Name);
		textObject.SetTextVariable("RELATION", ConversationHelper.GetHeroRelationToHeroTextShort(executedHero, Hero.MainHero, uppercaseFirst: false));
		Campaign.Current.CampaignInformationManager.NewMapNoticeAdded(new BloodFeudClanMemberGotExecutedMapNotification(executedHero, executor, CampaignTime.Now, textObject));
	}

	private void OnPlayerMemberExecutedAHero(Hero heroToExecute, Hero clanMember)
	{
		TextObject textObject = new TextObject("{=9OTgBLHx}{CLAN_MEMBER} ordered the execution of {LORD}, continuing the blood feud between the {PLAYER_CLAN} and the {OTHER_CLAN}.");
		textObject.SetTextVariable("LORD", heroToExecute.Name);
		textObject.SetTextVariable("CLAN_MEMBER", clanMember.Name);
		textObject.SetTextVariable("PLAYER_CLAN", Clan.PlayerClan.Name);
		textObject.SetTextVariable("OTHER_CLAN", heroToExecute.Clan.Name);
		Campaign.Current.CampaignInformationManager.NewMapNoticeAdded(new BloodFeudClanMemberExecutedLordMapNotification(heroToExecute, clanMember, CampaignTime.Now, textObject));
	}

	private void OnHeroRelationChanged(Hero effectiveHero, Hero effectiveHeroGainedRelationWith, int relationChange, bool showNotification, ChangeRelationAction.ChangeRelationDetail detail, Hero originalHero, Hero originalGainedRelationWith)
	{
		if (effectiveHero.Clan != null && effectiveHeroGainedRelationWith.Clan != null && (effectiveHero.Clan == Clan.PlayerClan || effectiveHeroGainedRelationWith.Clan == Clan.PlayerClan) && effectiveHero.Clan.GetRelationWithClan(effectiveHeroGainedRelationWith.Clan) >= 0)
		{
			if (effectiveHero.Clan.HasBloodFeudWithPlayer)
			{
				ChangeBloodFeudStateAction.SettleBloodFeudByRelationIncrease(effectiveHero.Clan);
			}
			else if (effectiveHeroGainedRelationWith.Clan.HasBloodFeudWithPlayer)
			{
				ChangeBloodFeudStateAction.SettleBloodFeudByRelationIncrease(effectiveHeroGainedRelationWith.Clan);
			}
		}
	}

	private void OnBloodFeudStateChanged(Clan clanWithFeud, Hero executedHero, ChangeBloodFeudStateAction.ChangeBloodFeudActionDetail detail)
	{
		if (clanWithFeud.HasBloodFeudWithPlayer)
		{
			TextObject textObject;
			if (detail == ChangeBloodFeudStateAction.ChangeBloodFeudActionDetail.StartedByPlayerExecuteAHero)
			{
				textObject = new TextObject("{=3RvhG1HN}You have started a blood feud with the {CLAN}.");
			}
			else
			{
				textObject = new TextObject("{=99k8PGvq}The {CLAN} have started a blood feud with your clan, the {PLAYER_CLAN}.");
				textObject.SetTextVariable("PLAYER_CLAN", Clan.PlayerClan.Name);
			}
			textObject.SetTextVariable("CLAN", clanWithFeud.Name);
			Campaign.Current.CampaignInformationManager.NewMapNoticeAdded(new BloodFeudStartedMapNotification(textObject));
			ChangeRelationAction.SetRelationBetweenHeroes(Clan.PlayerClan.Leader, clanWithFeud.Leader, Campaign.Current.Models.DiplomacyModel.MinRelationLimit);
			int num = 0;
			foreach (Clan item in Clan.All)
			{
				if (item != Clan.PlayerClan)
				{
					int bloodFeudStartRelationPenaltyToOtherClan = GetBloodFeudStartRelationPenaltyToOtherClan(executedHero, item);
					if (bloodFeudStartRelationPenaltyToOtherClan != 0)
					{
						ChangeRelationAction.ApplyRelationChangeBetweenHeroes(Clan.PlayerClan.Leader, item.Leader, bloodFeudStartRelationPenaltyToOtherClan, showQuickNotification: false);
						num++;
					}
				}
			}
			if (num > 0)
			{
				TextObject textObject2 = new TextObject("{=oqO9kjeW}The execution has hurt your relations with {COUNT} {?IS_PLURAL}clans{?}clan{\\?}.");
				MBTextManager.SetTextVariable("IS_PLURAL", (num > 1) ? 1 : 0);
				textObject2.SetTextVariable("COUNT", num);
				MBInformationManager.AddQuickInformation(textObject2);
			}
		}
		else
		{
			TextObject textObject3 = new TextObject("{=Px3DDMvV}You have decided not to pursue your blood feud with the {CLAN}, and it has ended.");
			switch (detail)
			{
			case ChangeBloodFeudStateAction.ChangeBloodFeudActionDetail.SettledByRansomPayment:
				textObject3 = new TextObject("{=fsfeamMz}Your blood feud with the {CLAN} has ended, as you paid them money to settle it.");
				break;
			case ChangeBloodFeudStateAction.ChangeBloodFeudActionDetail.SettledByRelationIncrease:
				textObject3 = new TextObject("{=iNPcIqUj}Your blood feud with the {CLAN} has ended after relations between your clans improved.");
				break;
			}
			textObject3.SetTextVariable("CLAN", clanWithFeud.Name);
			Campaign.Current.CampaignInformationManager.NewMapNoticeAdded(new BloodFeudEndedMapNotification(textObject3));
		}
		UpdateAllPlayerClanMemberExecutionStates();
	}

	private void OnHeroTakenPrisoner(PartyBase capturer, Hero prisoner)
	{
		if (capturer == null || capturer.LeaderHero == null)
		{
			return;
		}
		if (prisoner == Hero.MainHero)
		{
			if (MBRandom.RandomFloat <= CalculatePlayerExecutionProbability(capturer.LeaderHero))
			{
				capturer.LeaderHero.SetHasMet();
				_isMainHeroExecuted = true;
			}
		}
		else if (capturer.LeaderHero.Clan == Clan.PlayerClan)
		{
			if (capturer.LeaderHero != Hero.MainHero && prisoner.Clan.HasBloodFeudWithPlayer)
			{
				_heroesPendingMapEventEndToBeExecuted.Add(prisoner);
			}
		}
		else if (prisoner.Clan == Clan.PlayerClan && capturer.LeaderHero.Clan.HasBloodFeudWithPlayer)
		{
			ShowClanMemberCapturedByFeudedClanNotification(prisoner, capturer.LeaderHero.Clan);
		}
	}

	private void ShowClanMemberCapturedByFeudedClanNotification(Hero clanMember, Clan feudedClan)
	{
		TextObject textObject = new TextObject("{=tOGIagn0}The {FEUDED_CLAN} has captured your {RELATION} {CLAN_MEMBER.NAME}. Unless {?CLAN_MEMBER.GENDER}she{?}he{\\?} is freed, they are likely to execute {?CLAN_MEMBER.GENDER}her{?}him{\\?} within a few days.");
		StringHelpers.SetCharacterProperties("CLAN_MEMBER", clanMember.CharacterObject, textObject);
		textObject.SetTextVariable("FEUDED_CLAN", feudedClan.Name);
		textObject.SetTextVariable("RELATION", ConversationHelper.GetHeroRelationToHeroTextShort(clanMember, Hero.MainHero, uppercaseFirst: false));
		Campaign.Current.CampaignInformationManager.NewMapNoticeAdded(new BloodFeudClanMemberCapturedMapNotification(clanMember, textObject));
	}

	private void OnHeroPrisonerReleased(Hero prisoner, PartyBase party, IFaction capturerFaction, EndCaptivityDetail detail, bool showNotification = true)
	{
		UpdateAllPlayerClanMemberExecutionStates();
	}

	private void OnSettlementOwnerChanged(Settlement settlement, bool openToClaim, Hero newOwner, Hero oldOwner, Hero capturerHero, ChangeOwnerOfSettlementAction.ChangeOwnerOfSettlementDetail detail)
	{
		UpdateAllPlayerClanMemberExecutionStates();
	}

	private void DailyTickHero(Hero hero)
	{
		if (_pendingClanMemberSettlementExecutions.ContainsKey(hero))
		{
			(Clan, CampaignTime) tuple = _pendingClanMemberSettlementExecutions[hero];
			if (tuple.Item2.IsPast)
			{
				Hero leader = tuple.Item1.Leader;
				KillCharacterAction.ApplyByExecution(hero, leader);
			}
		}
		else if (hero != Hero.MainHero && hero.Clan == Clan.PlayerClan && hero.PartyBelongedToAsPrisoner != null)
		{
			Hero hero2 = (hero.PartyBelongedToAsPrisoner.IsMobile ? hero.PartyBelongedToAsPrisoner.LeaderHero : hero.PartyBelongedToAsPrisoner.Settlement.OwnerClan.Leader);
			if (hero2 != null && MBRandom.RandomFloat <= CalculatePlayerClanMemberExecutionProbability(hero, hero2))
			{
				KillCharacterAction.ApplyByExecution(hero, hero2);
			}
		}
	}

	private void OnGameMenuOpened(MenuCallbackArgs args)
	{
		if (_isMainHeroExecuted && !Campaign.Current.ConversationManager.IsConversationInProgress)
		{
			CampaignMapConversation.OpenConversation(new ConversationCharacterData(CharacterObject.PlayerCharacter), new ConversationCharacterData(Hero.MainHero.PartyBelongedToAsPrisoner.LeaderHero.CharacterObject));
		}
	}

	private void OnDeathMarkAdded(Hero victim, Hero killer)
	{
		if (victim.DeathMark == KillCharacterAction.KillCharacterActionDetail.ExecutionAfterMapEvent)
		{
			_heroesPendingMapEventEndToBeExecuted.Add(victim);
			if (killer == Hero.MainHero)
			{
				OnPlayerExecutedHero(victim);
			}
		}
	}

	private void OnClanDestroyed(Clan destroyedClan)
	{
		UpdateAllPlayerClanMemberExecutionStates();
	}

	private void UpdateAllPlayerClanMemberExecutionStates()
	{
		foreach (Hero hero in Clan.PlayerClan.Heroes)
		{
			if (hero != Hero.MainHero)
			{
				UpdateClanMemberExecutionState(hero);
			}
		}
	}

	private void QuarterHourlyTick()
	{
		foreach (Hero item in _heroesPendingMapEventEndToBeExecuted)
		{
			if (item.DeathMark == KillCharacterAction.KillCharacterActionDetail.ExecutionAfterMapEvent && item.DeathMarkKillerHero != null && (item.PartyBelongedToAsPrisoner == null || item.PartyBelongedToAsPrisoner.MapEvent != null))
			{
				KillCharacterAction.ApplyByExecution(item, item.DeathMarkKillerHero);
				break;
			}
			if (item.IsPrisoner && item.PartyBelongedToAsPrisoner != null && item.PartyBelongedToAsPrisoner.LeaderHero != null && item.PartyBelongedToAsPrisoner.MapEvent == null)
			{
				Hero leaderHero = item.PartyBelongedToAsPrisoner.LeaderHero;
				if (leaderHero.Clan == Clan.PlayerClan && MBRandom.RandomFloat <= 0.125f)
				{
					KillCharacterAction.ApplyByExecution(item, leaderHero);
					break;
				}
			}
		}
	}

	private void StartClanMemberExecutionAtSettlement(Hero clanMember, Clan clanWithFeud)
	{
		CampaignTime campaignTime = CampaignTime.DaysFromNow(8f);
		_pendingClanMemberSettlementExecutions.Add(clanMember, (clanWithFeud, campaignTime));
		Settlement settlement = clanMember.PartyBelongedToAsPrisoner.Settlement;
		string heroRelationToHeroTextShort = ConversationHelper.GetHeroRelationToHeroTextShort(clanMember, Hero.MainHero, uppercaseFirst: false);
		TextObject textObject = new TextObject("{=8IP5qoyq}{CLAN_MEMBER.NAME}, your {RELATION}, is held prisoner by the {OTHER_CLAN} in {SETTLEMENT}. As your two clans have a blood feud, they plan to execute {?CLAN_MEMBER.GENDER}her{?}him{\\?} in {DAYS} {?DAYS > 1}days{?}day{\\?}.");
		StringHelpers.SetCharacterProperties("CLAN_MEMBER", clanMember.CharacterObject, textObject);
		textObject.SetTextVariable("SETTLEMENT", settlement.Name);
		textObject.SetTextVariable("DAYS", campaignTime.RemainingDaysFromNow);
		textObject.SetTextVariable("OTHER_CLAN", clanWithFeud.Name);
		textObject.SetTextVariable("RELATION", heroRelationToHeroTextShort);
		Campaign.Current.CampaignInformationManager.NewMapNoticeAdded(new BloodFeudClanMemberCapturedMapNotification(clanMember, campaignTime, settlement, textObject));
	}

	private void CancelClanMemberExecutionAtSettlement(Hero clanMember)
	{
		Clan item = _pendingClanMemberSettlementExecutions[clanMember].CapturerClan;
		_pendingClanMemberSettlementExecutions.Remove(clanMember);
		TextObject textObject = new TextObject("{=m3R1ubdZ}Your {RELATION} {CLAN_MEMBER.NAME} is no longer held prisoner by the {FEUDED_CLAN}, and is no longer at risk of being executed as part of your blood feud with them.");
		StringHelpers.SetCharacterProperties("CLAN_MEMBER", clanMember.CharacterObject, textObject);
		textObject.SetTextVariable("FEUDED_CLAN", item.Name);
		textObject.SetTextVariable("RELATION", ConversationHelper.GetHeroRelationToHeroTextShort(clanMember, Hero.MainHero, uppercaseFirst: false));
		Campaign.Current.CampaignInformationManager.NewMapNoticeAdded(new BloodFeudClanMemberExecuteCancelledMapNotification(clanMember, textObject));
	}

	public void UpdateClanMemberExecutionState(Hero clanMember)
	{
		Clan clan = null;
		if (_pendingClanMemberSettlementExecutions.TryGetValue(clanMember, out (Clan, CampaignTime) value))
		{
			(clan, _) = value;
		}
		if (GetClanMemberCurrentExecutionData(clanMember, out var currentExecutorClan))
		{
			if (clan != null)
			{
				if (clan != currentExecutorClan)
				{
					CancelClanMemberExecutionAtSettlement(clanMember);
					StartClanMemberExecutionAtSettlement(clanMember, currentExecutorClan);
				}
			}
			else
			{
				StartClanMemberExecutionAtSettlement(clanMember, currentExecutorClan);
			}
		}
		else if (clan != null)
		{
			CancelClanMemberExecutionAtSettlement(clanMember);
		}
	}

	public bool GetClanMemberCurrentExecutionData(Hero hero, out Clan currentExecutorClan)
	{
		if (_pendingClanMemberSettlementExecutions.TryGetValue(hero, out (Clan, CampaignTime) value) && value.Item1.HasBloodFeudWithPlayer && hero.PartyBelongedToAsPrisoner?.Settlement?.OwnerClan.MapFaction == value.Item1.MapFaction && !value.Item1.IsEliminated)
		{
			(currentExecutorClan, _) = value;
			return true;
		}
		if (hero.PartyBelongedToAsPrisoner != null && hero.PartyBelongedToAsPrisoner.Settlement != null)
		{
			currentExecutorClan = hero.PartyBelongedToAsPrisoner.Settlement.OwnerClan;
			if (currentExecutorClan.HasBloodFeudWithPlayer)
			{
				return true;
			}
		}
		currentExecutorClan = null;
		return false;
	}

	private void OnPrisonersChangedInSettlement(Settlement settlement, FlattenedTroopRoster prisonerRoster, Hero prisonerHero, bool takenFromDungeon)
	{
		UpdateAllPlayerClanMemberExecutionStates();
	}

	private void CanHeroBeReleased(Hero hero, ref bool result)
	{
		if (_pendingClanMemberSettlementExecutions.ContainsKey(hero))
		{
			result = false;
		}
		else if (_heroesPendingMapEventEndToBeExecuted.Contains(hero))
		{
			result = false;
		}
		else if (hero.Clan == Clan.PlayerClan && hero.IsPrisoner && hero.PartyBelongedToAsPrisoner.IsMobile && hero.PartyBelongedToAsPrisoner.LeaderHero != null && hero.PartyBelongedToAsPrisoner.MobileParty.ActualClan.HasBloodFeudWithPlayer)
		{
			result = false;
		}
	}

	private void OnSessionLaunched(CampaignGameStarter campaignGameStarter)
	{
		campaignGameStarter.AddDialogLine("lord_executes_player", "start", "close_window", "{=!}{EXECUTE_TEXT}", player_is_executed_option_condition, delegate
		{
			Campaign.Current.ConversationManager.ConversationEndOneShot += player_is_executed_option_consequence;
		});
		campaignGameStarter.AddPlayerLine("ransom_broker_talk_blood_feud_info", "ransom_broker_talk", "ransom_broker_blood_feud_info", "{=FmL3Jixd}As you deal with captives, do you know anything about blood feuds?", null, null);
		campaignGameStarter.AddDialogLine("ransom_broker_blood_feud_info_1", "ransom_broker_blood_feud_info", "ransom_broker_pretalk", "{=CaQqcqAU}Well, if one of the great families of this land kills a member of a different clan, the victim's kin may declare a feud. Both sides will then usually execute any prisoners from the other clan that they catch, without incurring the usual disapproval. Sometimes we can broker an end to such feuds, though it is costly.", null, null);
		campaignGameStarter.AddPlayerLine("ransom_broker_talk_end_blood_feud", "ransom_broker_talk", "ransom_broker_end_feud_ask", "{=bDKb7ACL}I’m involved in a blood feud, and I would like to end it.", conversation_ransom_broker_has_active_feuds_on_condition, null);
		campaignGameStarter.AddDialogLine("ransom_broker_end_feud_ask", "ransom_broker_end_feud_ask", "ransom_broker_end_feud_select_clan", "{=0oD6hGf4}Which clan do you want to end your feud with?", null, conversation_ransom_broker_collect_feud_clans_on_consequence);
		campaignGameStarter.AddRepeatablePlayerLine("ransom_broker_end_feud_clan", "ransom_broker_end_feud_select_clan", "ransom_broker_end_feud_confirm", "{=!}{CLAN}", "{=ijTpwdn1}I am thinking of a different clan", "ransom_broker_end_feud_ask", conversation_ransom_broker_feud_clan_option_on_condition, conversation_ransom_broker_feud_clan_selected_on_consequence);
		campaignGameStarter.AddPlayerLine("ransom_broker_end_feud_cancel", "ransom_broker_end_feud_select_clan", "ransom_broker_pretalk", "{=mdNRYlfS}Nevermind.", null, null);
		campaignGameStarter.AddDialogLine("ransom_broker_end_feud_confirm_free", "ransom_broker_end_feud_confirm", "ransom_broker_pretalk", "{=TSJHk5YJ}Consider it done.", conversation_ransom_broker_end_feud_no_cost_on_condition, conversation_ransom_broker_end_feud_on_consequence);
		campaignGameStarter.AddDialogLine("ransom_broker_end_feud_confirm_cost", "ransom_broker_end_feud_confirm", "ransom_broker_end_feud_pay", "{=!}{FEUD_END_TEXT}", conversation_ransom_broker_end_feud_cost_on_condition, null);
		campaignGameStarter.AddPlayerLine("ransom_broker_end_feud_pay_accept", "ransom_broker_end_feud_pay", "ransom_broker_end_feud_paid", "{=0rKfapmF}Yes. Here is the money. Let us end the killing.", null, conversation_ransom_broker_end_feud_on_consequence, 100, conversation_ransom_broker_end_feud_pay_clickable_condition);
		campaignGameStarter.AddPlayerLine("ransom_broker_end_feud_pay_decline", "ransom_broker_end_feud_pay", "ransom_broker_pretalk", "{=nykOrXhv}I cannot afford that right now.", null, null);
		campaignGameStarter.AddDialogLine("ransom_broker_end_feud_paid", "ransom_broker_end_feud_paid", "ransom_broker_pretalk", "{=TSJHk5YJ}Consider it done.", null, null);
	}

	private bool conversation_ransom_broker_has_active_feuds_on_condition()
	{
		foreach (Clan item in Clan.All)
		{
			if (item.HasBloodFeudWithPlayer && !item.IsEliminated)
			{
				return true;
			}
		}
		return false;
	}

	private void conversation_ransom_broker_collect_feud_clans_on_consequence()
	{
		List<Clan> list = new List<Clan>();
		foreach (Clan item in Clan.All)
		{
			if (item.HasBloodFeudWithPlayer && !item.IsEliminated)
			{
				list.Add(item);
			}
		}
		ConversationSentence.SetObjectsToRepeatOver(list);
	}

	private bool conversation_ransom_broker_feud_clan_option_on_condition()
	{
		if (ConversationSentence.CurrentProcessedRepeatObject is Clan clan)
		{
			ConversationSentence.SelectedRepeatLine.SetTextVariable("CLAN", clan.Name);
			return true;
		}
		return false;
	}

	private void conversation_ransom_broker_feud_clan_selected_on_consequence()
	{
		_selectedBloodFeudClan = ConversationSentence.SelectedRepeatObject as Clan;
	}

	private bool conversation_ransom_broker_end_feud_no_cost_on_condition()
	{
		if (_selectedBloodFeudClan != null)
		{
			return (float)GetBloodMoneyForPlayerToPayAgainstClan(_selectedBloodFeudClan) <= 0f;
		}
		return false;
	}

	private bool conversation_ransom_broker_end_feud_cost_on_condition()
	{
		if (_selectedBloodFeudClan == null)
		{
			return false;
		}
		TextObject empty = TextObject.GetEmpty();
		int bloodMoneyForPlayerToPayAgainstClan = GetBloodMoneyForPlayerToPayAgainstClan(_selectedBloodFeudClan);
		empty = ((_selectedBloodFeudClan.BloodFeudExecutionsDoneCount > _selectedBloodFeudClan.BloodFeudExecutionsReceivedCount) ? new TextObject("{=CwUg1jS5}Well, they've killed more of you than you have of them, so it probably will not cost you so much. I think I could settle things for {BLOOD_MONEY_COST}{GOLD_ICON} denars. Do you agree?") : ((_selectedBloodFeudClan.BloodFeudExecutionsDoneCount >= _selectedBloodFeudClan.BloodFeudExecutionsReceivedCount) ? new TextObject("{=eqaVrGK4}You both seem to have spilled the same amount of blood. Honor is satisfied. For {BLOOD_MONEY_COST}{GOLD_ICON}, I think I could arrange a settlement. Do you agree to pay?") : new TextObject("{=Wgfb1nSf}That can be arranged, but you've killed more of them then they of you, and a family like {CLAN} won't have it said that it reckons its blood cheaply. For {BLOOD_MONEY_COST}{GOLD_ICON}, I think I could arrange a settlement. Do you agree to pay?")));
		empty.SetTextVariable("BLOOD_MONEY_COST", bloodMoneyForPlayerToPayAgainstClan);
		empty.SetTextVariable("CLAN", _selectedBloodFeudClan.Name);
		MBTextManager.SetTextVariable("FEUD_END_TEXT", empty);
		return true;
	}

	private bool conversation_ransom_broker_end_feud_pay_clickable_condition(out TextObject explanation)
	{
		int bloodMoneyForPlayerToPayAgainstClan = GetBloodMoneyForPlayerToPayAgainstClan(_selectedBloodFeudClan);
		if (Hero.MainHero.Gold < bloodMoneyForPlayerToPayAgainstClan)
		{
			explanation = new TextObject("{=xVZVYNan}You don't have enough{GOLD_ICON}.");
			return false;
		}
		explanation = null;
		return true;
	}

	private void conversation_ransom_broker_end_feud_on_consequence()
	{
		if (_selectedBloodFeudClan != null && _selectedBloodFeudClan.HasBloodFeudWithPlayer)
		{
			int bloodMoneyForPlayerToPayAgainstClan = GetBloodMoneyForPlayerToPayAgainstClan(_selectedBloodFeudClan);
			if (bloodMoneyForPlayerToPayAgainstClan > 0)
			{
				GiveGoldAction.ApplyBetweenCharacters(Hero.MainHero, null, bloodMoneyForPlayerToPayAgainstClan);
			}
			ChangeBloodFeudStateAction.SettleBloodFeudByRansomPayment(_selectedBloodFeudClan);
			ChangeRelationAction.SetRelationBetweenHeroes(Clan.PlayerClan.Leader, _selectedBloodFeudClan.Leader, -49);
		}
	}

	private bool player_is_executed_option_condition()
	{
		if (Hero.OneToOneConversationHero != null && Hero.OneToOneConversationHero.IsLord && _isMainHeroExecuted)
		{
			TextObject textObject = new TextObject("{=y2dJ4jTd}Even if henceforth there is only blood between your kin and mine, I will show you no mercy. Off with your head.");
			if (Hero.OneToOneConversationHero.Clan.HasBloodFeudWithPlayer)
			{
				textObject = new TextObject("{=rMeHWobg}{PLAYER.NAME}. You owe us a debt of blood, and for that your life is forfeit.");
				if (Hero.MainHero.GetTraitLevel(DefaultTraits.Calculating) < 0 || Hero.MainHero.GetTraitLevel(DefaultTraits.Mercy) < 0 || Hero.MainHero.GetTraitLevel(DefaultTraits.Honor) < 0)
				{
					textObject = new TextObject("{=PF3Tao8I}Well, {PLAYER.NAME}. Are you expecting mercy? You shall receive the same kind of mercy that you have shown my kin. Off with your head!");
				}
				StringHelpers.SetCharacterProperties("PLAYER", Hero.MainHero.CharacterObject, textObject);
				MBTextManager.SetTextVariable("EXECUTE_TEXT", textObject);
			}
			return true;
		}
		return false;
	}

	private void player_is_executed_option_consequence()
	{
		_isMainHeroExecuted = false;
		KillCharacterAction.ApplyByExecution(Hero.MainHero, Hero.MainHero.PartyBelongedToAsPrisoner.LeaderHero);
	}

	public override void SyncData(IDataStore store)
	{
		store.SyncData("_pendingClanMemberSettlementExecutions", ref _pendingClanMemberSettlementExecutions);
		store.SyncData("_heroesPendingMapEventEndToBeExecuted", ref _heroesPendingMapEventEndToBeExecuted);
		store.SyncData("_isMainHeroExecuted", ref _isMainHeroExecuted);
	}
}
