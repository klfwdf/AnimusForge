using System;
using System.Collections.Generic;
using System.Linq;
using Helpers;
using TaleWorlds.CampaignSystem.Actions;
using TaleWorlds.CampaignSystem.Encounters;
using TaleWorlds.CampaignSystem.Extensions;
using TaleWorlds.CampaignSystem.Map;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.CampaignSystem.Party.PartyComponents;
using TaleWorlds.CampaignSystem.Roster;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.CampaignSystem.Settlements.Buildings;
using TaleWorlds.Core;
using TaleWorlds.Library;
using TaleWorlds.Localization;

namespace TaleWorlds.CampaignSystem.CampaignBehaviors;

public class PatrolPartiesCampaignBehavior : CampaignBehaviorBase, IPatrolPartiesCampaignBehavior
{
	private const float BasePatrolScore = 1.25f;

	private const float VisitHomeSettlementPartySizeRatioThreshold = 5f;

	private const float VisitHomeSettlementBaseScore = 0.15f;

	private const float ConsiderReplenishPartySizeRatioThreshold = 0.15f;

	private const float BaseReplenishmentChance = 0.005f;

	private Dictionary<Settlement, CampaignTime> _partyGenerationQueue = new Dictionary<Settlement, CampaignTime>();

	private Dictionary<Settlement, CampaignTime> _lastHomeSettlementVisitTimes = new Dictionary<Settlement, CampaignTime>();

	private Dictionary<Settlement, CampaignTime> _lastHomeSettlementVisitTimesCoastal = new Dictionary<Settlement, CampaignTime>();

	private Dictionary<Settlement, CampaignTime> _interactedPatrolParties = new Dictionary<Settlement, CampaignTime>();

	public override void RegisterEvents()
	{
		CampaignEvents.DailyTickSettlementEvent.AddNonSerializedListener(this, DailyTickSettlement);
		CampaignEvents.OnSettlementOwnerChangedEvent.AddNonSerializedListener(this, OnSettlementOwnerChangedEvent);
		CampaignEvents.OnNewGameCreatedPartialFollowUpEvent.AddNonSerializedListener(this, OnNewGameCreated);
		CampaignEvents.AiHourlyTickEvent.AddNonSerializedListener(this, AiHourlyTick);
		CampaignEvents.SettlementEntered.AddNonSerializedListener(this, SettlementEntered);
		CampaignEvents.OnSettlementLeftEvent.AddNonSerializedListener(this, OnSettlementLeft);
		CampaignEvents.MobilePartyDestroyed.AddNonSerializedListener(this, MobilePartyDestroyed);
		CampaignEvents.MobilePartyCreated.AddNonSerializedListener(this, MobilePartyCreated);
		CampaignEvents.HourlyTickPartyEvent.AddNonSerializedListener(this, HourlyTickParty);
		CampaignEvents.OnBuildingLevelChangedEvent.AddNonSerializedListener(this, OnBuildingLevelChanged);
		CampaignEvents.OnSessionLaunchedEvent.AddNonSerializedListener(this, OnSessionLaunched);
	}

	private void OnBuildingLevelChanged(Town town, Building building, int levelChange)
	{
		if (building.BuildingType == DefaultBuildingTypes.SettlementGuardHouse)
		{
			if (building.CurrentLevel == 1 && levelChange > 0 && CanSettlementSpawnNewPartyCurrently(town.Settlement, includeReason: false, out var _))
			{
				UpdateSettlementQueue(town.Settlement, CampaignTime.Zero);
			}
			if (town.Settlement.PatrolParty != null)
			{
				town.Settlement.PatrolParty.Party.MemberRoster.UpdateVersion();
			}
		}
	}

	private void DailyTickSettlement(Settlement settlement)
	{
		if (CanSettlementSpawnNewPartyCurrently(settlement, includeReason: false, out var _))
		{
			if (!_partyGenerationQueue.TryGetValue(settlement, out var value))
			{
				UpdateSettlementQueue(settlement, CampaignTime.Now + Campaign.Current.Models.SettlementPatrolModel.GetPatrolPartySpawnDuration(settlement, naval: false));
			}
			else if (value.IsPast)
			{
				SpawnPatrolParty(settlement);
			}
		}
		else
		{
			UpdateSettlementParties(settlement);
		}
	}

	private void HourlyTickParty(MobileParty mobileParty)
	{
		if (!mobileParty.IsPatrolParty)
		{
			return;
		}
		if (mobileParty.CurrentSettlement == mobileParty.HomeSettlement && !mobileParty.CurrentSettlement.IsUnderSiege && mobileParty.HomeSettlement.Party.MapEvent == null)
		{
			GetLastHomeSettlementVisitTime(mobileParty, out var campaignTime);
			float elapsedHoursUntilNow = campaignTime.ElapsedHoursUntilNow;
			if (MBRandom.RandomFloat < elapsedHoursUntilNow * elapsedHoursUntilNow * 0.005f && mobileParty.PartySizeRatio < 1f)
			{
				ReplenishParty(mobileParty);
			}
		}
		if (mobileParty.CurrentSettlement == null && mobileParty.TargetSettlement == mobileParty.HomeSettlement && mobileParty.DefaultBehavior == AiBehavior.GoToSettlement && !mobileParty.TargetSettlement.IsUnderSiege)
		{
			mobileParty.Ai.SetInitiative(0.1f, 1f, 1f);
		}
	}

	private void ReplenishParty(MobileParty party)
	{
		PartyTemplateObject partyTemplateForPatrolParty = Campaign.Current.Models.SettlementPatrolModel.GetPartyTemplateForPatrolParty(party.CurrentSettlement, party.PatrolPartyComponent.IsNaval);
		TroopRoster troopRoster = Campaign.Current.Models.PartySizeLimitModel.FindAppropriateInitialRosterForMobileParty(party, partyTemplateForPatrolParty);
		party.MemberRoster.Clear();
		party.MemberRoster.Add(troopRoster);
		SortRoster(party);
	}

	private void SortRoster(MobileParty mobileParty)
	{
		mobileParty.PatrolPartyComponent.SortRoster();
	}

	private void OnSessionLaunched(CampaignGameStarter starter)
	{
		AddDialogs(starter);
	}

	private void OnNewGameCreated(CampaignGameStarter starter, int index)
	{
		if (index != 50)
		{
			return;
		}
		foreach (Town allFief in Town.AllFiefs)
		{
			if (CanSettlementSpawnNewPartyCurrently(allFief.Settlement, includeReason: false, out var _))
			{
				SpawnPatrolParty(allFief.Settlement);
			}
		}
	}

	private void AddDialogs(CampaignGameStarter starter)
	{
		starter.AddDialogLine("patrol_talk_start_patrol_party", "start", "patrol_talk_start_1", "{=!}{PATROL_PARTY_GREETING}", patrol_talk_on_condition, patrol_talk_on_consequence);
		starter.AddPlayerLine("patrol_talk_start_enemy_1", "patrol_talk_start_1", "patrol_talk_start_attack", "{=!}{PLAYER_ATTACK_TEXT}", patrol_talk_on_condition_player_is_attacker, null);
		starter.AddPlayerLine("patrol_talk_start_enemy_2", "patrol_talk_start_1", "close_window", "{=5KGuQb5C}We'll see who slays whom here.", patrol_talk_on_condition_player_is_not_attacker, null);
		starter.AddDialogLine("patrol_talk_start_enemy_attack", "patrol_talk_start_attack", "patrol_talk_start_attack_final", "{=dQfha0al}You'll answer to the {?OWNER.GENDER}lady{?}lord{\\?} of {SETTLEMENT_LINK}, then!", patrol_talk_on_condition_enemy_attack, null);
		starter.AddDialogLine("patrol_talk_start_neutral_attack", "patrol_talk_start_attack", "patrol_talk_start_attack_final", "{=dQfha0al}You'll answer to the {?OWNER.GENDER}lady{?}lord{\\?} of {SETTLEMENT_LINK}, then!", patrol_talk_on_condition_non_enemy_attack, null);
		starter.AddPlayerLine("patrol_talk_start_attack_final", "patrol_talk_start_attack_final", "patrol_talk_start_attack_final_response", "{=z7fFBuqt}I'll take that chance. Men, attack!", null, patrol_attack_on_consequence);
		starter.AddPlayerLine("patrol_talk_start_attack_final_decline", "patrol_talk_start_attack_final", "close_window", "{=uJuOTHnb}On second thought, be on your way.", patrol_talk_on_condition_dont_attack, patrol_talk_start_enemy_leave_on_consequence);
		starter.AddDialogLine("patrol_talk_start_attack_final_response", "patrol_talk_start_attack_final_response", "close_window", "{=4VfGEtuS}Curse you!", null, null);
		starter.AddPlayerLine("patrol_talk_start_ask_hideout", "patrol_talk_start_1", "patrol_talk_start_hideout", "{=y6aBcAFF}You might know if any bandits have made their lairs around here, then?", patrol_talk_on_condition_hideout, null);
		starter.AddDialogLine("patrol_talk_hideout", "patrol_talk_start_hideout", "start", "{=!}{HIDEOUT_TEXT}", patrol_talk_hideout_on_condition, null);
		starter.AddPlayerLine("patrol_talk_start_ask_security", "patrol_talk_start_1", "patrol_talk_start_security", "{=!}{SECURITY_QUESTION}", patrol_talk_on_condition_security_start, null);
		starter.AddDialogLine("patrol_talk_security", "patrol_talk_start_security", "start", "{=!}{SECURITY_TEXT}", patrol_talk_on_condition_security, null);
		starter.AddPlayerLine("patrol_talk_start_leave", "patrol_talk_start_1", "close_window", "{=tqh6ydEW}Carry on, then.", patrol_talk_start_enemy_leave_on_condition, patrol_talk_start_enemy_leave_on_consequence);
	}

	private bool patrol_talk_on_condition_dont_attack()
	{
		if (MobileParty.ConversationParty != null && MobileParty.ConversationParty.IsPatrolParty)
		{
			return PlayerEncounter.PlayerIsAttacker;
		}
		return false;
	}

	private bool patrol_talk_start_enemy_leave_on_condition()
	{
		if (MobileParty.ConversationParty != null && MobileParty.ConversationParty.IsPatrolParty)
		{
			if (MobileParty.ConversationParty.MapFaction.IsAtWarWith(MobileParty.MainParty.MapFaction))
			{
				return PlayerEncounter.PlayerIsAttacker;
			}
			return true;
		}
		return false;
	}

	private void patrol_attack_on_consequence()
	{
		BeHostileAction.ApplyEncounterHostileAction(PartyBase.MainParty, MobileParty.ConversationParty.Party);
	}

	private bool patrol_talk_hideout_on_condition()
	{
		TextObject textObject = null;
		textObject = (MobileParty.ConversationParty.MapFaction.IsAtWarWith(MobileParty.MainParty.MapFaction) ? new TextObject("{=IjcuJggo}Listen... We don't want to fight you, but that doesn't mean we should sit here bandying words and passing the time of day.") : (IsThereHideoutNearSettlement(MobileParty.ConversationParty.HomeSettlement) ? new TextObject("{=H0DiZ3fk}We've heard talk that some have holed up a short distance of here. If you could find them and roust them out, you'd be doing us all a service.") : ((!(MobileParty.ConversationParty.HomeSettlement.OwnerClan.Culture.StringId == "nord") && !(MobileParty.ConversationParty.HomeSettlement.OwnerClan.Culture.StringId == "battania")) ? new TextObject("{=PI6kC7Mp}We've heard of none near here, Heaven be praised.") : new TextObject("{=wWYyOARQ}We've heard of none near here, the gods be praised."))));
		MBTextManager.SetTextVariable("HIDEOUT_TEXT", textObject);
		return true;
	}

	private bool patrol_talk_on_condition_security_start()
	{
		if (MobileParty.ConversationParty != null && MobileParty.ConversationParty.IsPatrolParty && PlayerEncounter.PlayerIsAttacker)
		{
			TextObject empty = TextObject.GetEmpty();
			empty = ((!MobileParty.ConversationParty.IsCurrentlyAtSea) ? new TextObject("{=EVprpuUB}How are things around {SETTLEMENT}, then. Do the people feel safe?") : new TextObject("{=5xOINADa}What news out of {SETTLEMENT}, then? Are these waters safe?"));
			empty.SetTextVariable("SETTLEMENT", MobileParty.ConversationParty.HomeSettlement.Name);
			MBTextManager.SetTextVariable("SECURITY_QUESTION", empty);
			return true;
		}
		return false;
	}

	private bool patrol_talk_on_condition_security()
	{
		if (MobileParty.ConversationParty != null && MobileParty.ConversationParty.IsPatrolParty)
		{
			TextObject textObject = null;
			if (MobileParty.ConversationParty.MapFaction.IsAtWarWith(MobileParty.MainParty.MapFaction))
			{
				textObject = new TextObject("{=IjcuJggo}Listen... We don't want to fight you, but that doesn't mean we should sit here bandying words and passing the time of day.");
			}
			else
			{
				Settlement homeSettlement = MobileParty.ConversationParty.HomeSettlement;
				textObject = ((homeSettlement.Town.Security <= 20f) ? ((!MobileParty.ConversationParty.IsCurrentlyAtSea) ? new TextObject("{=Tudsduxi}Safe? What safety is there, in these cursed times? We do what we can, but these bandits are a plague, sent upon us for our sins. Track down one band, and a dozen more take their place.") : new TextObject("{=iTNOzgah}Safe? What safety is there, in this dark age? There are so many pirates about that you can’t earn an honest living afloat, which is probably why so many turn pirate to begin with.")) : ((homeSettlement.Town.Security >= 70f) ? new TextObject("{=pWSgSp4l}Aye, safe enough, given the times we're in.") : ((!MobileParty.ConversationParty.IsCurrentlyAtSea) ? new TextObject("{=sYcqstOy}Things could be better. People still go about their business, but you never know when some miscreant will set on you and take you for all you've got.") : new TextObject("{=AE81KA7d}Things could be better. Fishermen and merchants come and go, but you never know when some cursed vessel full of cutthroats will set on you and take you for all you've got."))));
			}
			MBTextManager.SetTextVariable("SECURITY_TEXT", textObject);
			MBTextManager.SetTextVariable("SETTLEMENT_LINK", MobileParty.ConversationParty.HomeSettlement.EncyclopediaLinkWithName);
		}
		return true;
	}

	private bool patrol_talk_on_condition_hideout()
	{
		if (MobileParty.ConversationParty != null && MobileParty.ConversationParty.IsPatrolParty && PlayerEncounter.PlayerIsAttacker)
		{
			return !MobileParty.ConversationParty.PatrolPartyComponent.IsNaval;
		}
		return false;
	}

	private bool IsThereHideoutNearSettlement(Settlement settlement)
	{
		Hideout hideout = SettlementHelper.FindNearestHideoutToSettlement(settlement, MobileParty.NavigationType.Default);
		if (hideout != null && hideout.IsInfested && DistanceHelper.FindClosestDistanceFromSettlementToSettlement(hideout.Settlement, settlement, MobileParty.NavigationType.Default, out var _) <= Campaign.Current.GetAverageDistanceBetweenClosestTwoTownsWithNavigationType(MobileParty.NavigationType.Default) * 0.6f)
		{
			MBTextManager.SetTextVariable("SETTLEMENT_LINK", settlement.EncyclopediaLinkWithName);
			return true;
		}
		foreach (Village boundVillage in settlement.BoundVillages)
		{
			if (IsThereHideoutNearSettlement(boundVillage.Settlement))
			{
				return true;
			}
		}
		return false;
	}

	private void patrol_talk_start_enemy_leave_on_consequence()
	{
		if (PlayerEncounter.Current != null)
		{
			PlayerEncounter.LeaveEncounter = true;
		}
	}

	private bool patrol_talk_common_condition_enemy()
	{
		if (MobileParty.ConversationParty.IsPatrolParty)
		{
			return MobileParty.ConversationParty.MapFaction != Hero.MainHero.MapFaction;
		}
		return false;
	}

	private bool patrol_talk_on_condition_player_is_attacker()
	{
		if (patrol_talk_common_condition_enemy() && PlayerEncounter.PlayerIsAttacker)
		{
			TextObject textObject = null;
			textObject = (FactionManager.IsAtWarAgainstFaction(MobileParty.ConversationParty.MapFaction, Hero.MainHero.MapFaction) ? ((!MobileParty.ConversationParty.IsCurrentlyAtSea) ? new TextObject("{=bVVh2LR9}You are a foe in arms. Yield or die!") : new TextObject("{=SNeZ6ETU}You are a lawful enemy. Yield or die!")) : ((!MobileParty.ConversationParty.IsCurrentlyAtSea) ? new TextObject("{=blwPe1jQ}I'm afraid I just don't like the look of you. Yield or die!") : new TextObject("{=92m8r4lH}I'm afraid I just don't like the cut of your jib. Yield or die!")));
			MBTextManager.SetTextVariable("PLAYER_ATTACK_TEXT", textObject);
			return true;
		}
		return false;
	}

	private bool patrol_talk_on_condition_player_is_not_attacker()
	{
		if (patrol_talk_common_condition_enemy() && !PlayerEncounter.PlayerIsAttacker)
		{
			MBTextManager.SetTextVariable("SETTLEMENT_LINK", MobileParty.ConversationParty.HomeSettlement.EncyclopediaLinkWithName);
			StringHelpers.SetCharacterProperties("OWNER", MobileParty.ConversationParty.HomeSettlement.Owner.CharacterObject);
			return true;
		}
		return false;
	}

	private bool patrol_talk_on_condition_non_enemy_attack()
	{
		MBTextManager.SetTextVariable("SETTLEMENT_LINK", MobileParty.ConversationParty.HomeSettlement.EncyclopediaLinkWithName);
		StringHelpers.SetCharacterProperties("OWNER", MobileParty.ConversationParty.HomeSettlement.Owner.CharacterObject);
		return !FactionManager.IsAtWarAgainstFaction(MobileParty.ConversationParty.MapFaction, Hero.MainHero.MapFaction);
	}

	private bool patrol_talk_on_condition_enemy_attack()
	{
		MBTextManager.SetTextVariable("SETTLEMENT_LINK", MobileParty.ConversationParty.HomeSettlement.EncyclopediaLinkWithName);
		StringHelpers.SetCharacterProperties("OWNER", MobileParty.ConversationParty.HomeSettlement.Owner.CharacterObject);
		return FactionManager.IsAtWarAgainstFaction(MobileParty.ConversationParty.MapFaction, Hero.MainHero.MapFaction);
	}

	private void patrol_talk_on_consequence()
	{
		_interactedPatrolParties[MobileParty.ConversationParty.HomeSettlement] = CampaignTime.Now;
	}

	private bool patrol_talk_on_condition()
	{
		if (MobileParty.ConversationParty != null && MobileParty.ConversationParty.IsPatrolParty)
		{
			TextObject empty = TextObject.GetEmpty();
			int num = int.MaxValue;
			if (_interactedPatrolParties.TryGetValue(MobileParty.ConversationParty.HomeSettlement, out var value))
			{
				num = (int)value.ElapsedHoursUntilNow;
			}
			if (MobileParty.ConversationParty.MapFaction.IsAtWarWith(MobileParty.MainParty.MapFaction))
			{
				if (PlayerEncounter.PlayerIsAttacker)
				{
					empty = ((!MobileParty.ConversationParty.IsCurrentlyAtSea) ? new TextObject("{=SJ3KYtzM}We're not looking for trouble with you. We're here to chase bandits and brigands, the common enemy of all law-abiding folk.") : new TextObject("{=Y8tKOnlq}We're not looking for trouble with you. We're here to chase pirates, the common enemy of all law-abiding sailors."));
				}
				else
				{
					empty = ((!MobileParty.ConversationParty.IsCurrentlyAtSea) ? new TextObject("{=uwtzkbsX}You are an enemy of the {?OWNER.GENDER}lady{?}lord{\\?} and people of {SETTLEMENT_LINK}. Surrender, or be slain!") : new TextObject("{=yX0bOiCI}You are an enemy of the {?OWNER.GENDER}lady{?}lord{\\?} and people of {SETTLEMENT_LINK}. Give up your ship, or be slain!"));
					empty.SetCharacterProperties("OWNER", MobileParty.ConversationParty.HomeSettlement.OwnerClan.Leader.CharacterObject);
				}
			}
			else if (num <= CampaignTime.HoursInDay)
			{
				empty = ((!MobileParty.ConversationParty.IsCurrentlyAtSea) ? new TextObject("{=7PgV69zl}Hope you're keeping safe. You never know who's lurking about.") : new TextObject("{=wXjmwOZK}Keep a watchful eye. You never know who’s lurking just below the horizon."));
			}
			else
			{
				TextObject introText = GetIntroText();
				TextObject statusText = GetStatusText();
				empty = new TextObject("{=!}{INTRO} {STATUS}");
				empty.SetTextVariable("INTRO", introText);
				empty.SetTextVariable("STATUS", statusText);
			}
			empty.SetTextVariable("SETTLEMENT_LINK", MobileParty.ConversationParty.HomeSettlement.EncyclopediaLinkWithName);
			MBTextManager.SetTextVariable("PATROL_PARTY_GREETING", empty);
			return true;
		}
		return false;
	}

	private TextObject GetStatusText()
	{
		int num = 0;
		LocatableSearchData<MobileParty> data = MobileParty.StartFindingLocatablesAroundPosition(MobileParty.ConversationParty.Position.ToVec2(), MobileParty.ConversationParty.Speed * (float)CampaignTime.HoursInDay * 0.5f);
		for (MobileParty mobileParty = MobileParty.FindNextLocatable(ref data); mobileParty != null; mobileParty = MobileParty.FindNextLocatable(ref data))
		{
			if (mobileParty.IsBandit && mobileParty.IsCurrentlyAtSea == MobileParty.MainParty.IsCurrentlyAtSea)
			{
				num++;
			}
			if (num >= 5)
			{
				break;
			}
		}
		TextObject textObject = null;
		if (num >= 5)
		{
			if (MobileParty.ConversationParty.IsCurrentlyAtSea)
			{
				return new TextObject("{=H3nSfb9e}These waters are thick with pirates, like sharks amid chum.");
			}
			return new TextObject("{=FbnCZqHa}This place is thick with bandits and troublemakers.");
		}
		if (num >= 3)
		{
			if (MobileParty.ConversationParty.IsCurrentlyAtSea)
			{
				return new TextObject("{=DMWbQBHg}There are a few pirates around here who've been giving us some trouble.");
			}
			return new TextObject("{=yNWFgFl1}There are a few brigands around here who've been giving us some trouble.");
		}
		if (MobileParty.ConversationParty.IsCurrentlyAtSea)
		{
			return new TextObject("{=eF47hqVx}Haven't heard any reports of piracy around here, but you never know when things will change.");
		}
		return new TextObject("{=Gbav1PEd}Haven't heard any reports of bandits around here, but you never know when things will change.");
	}

	private TextObject GetIntroText()
	{
		TextObject textObject = null;
		if (MobileParty.ConversationParty.HomeSettlement.Owner != Hero.MainHero)
		{
			textObject = ((!MobileParty.ConversationParty.IsCurrentlyAtSea) ? new TextObject("{=GC4coq47}Greetings. We're here to keep the lands around {SETTLEMENT_LINK} safe.") : new TextObject("{=i2a7b9az}Greetings. We're here to keep the waters around {SETTLEMENT_LINK} safe."));
		}
		else
		{
			textObject = ((!MobileParty.ConversationParty.IsCurrentlyAtSea) ? new TextObject("{=T4sfyeNq}Greetings, your {?OWNER.GENDER}ladyship{?}lordship{\\?}. We're doing our best to keep your lands and the people of {SETTLEMENT_LINK} safe.") : new TextObject("{=GXOkfamn}Greetings, your {?OWNER.GENDER}ladyship{?}lordship{\\?}. We're doing our best to protect shipping out of {SETTLEMENT_LINK}."));
			textObject.SetCharacterProperties("OWNER", CharacterObject.PlayerCharacter);
		}
		textObject.SetTextVariable("SETTLEMENT_LINK", MobileParty.ConversationParty.HomeSettlement.EncyclopediaLinkWithName);
		return textObject;
	}

	private void OnSettlementOwnerChangedEvent(Settlement settlement, bool openToClaim, Hero newOwner, Hero oldOwner, Hero capturerHero, ChangeOwnerOfSettlementAction.ChangeOwnerOfSettlementDetail detail)
	{
		if (settlement.PatrolParty != null)
		{
			RemoveSettlementParties(settlement);
		}
	}

	private void AiHourlyTick(MobileParty mobileParty, PartyThinkParams p)
	{
		if (!mobileParty.IsPatrolParty || mobileParty.IsDisbanding || (mobileParty.CurrentSettlement != null && !CanVisitSettlement(mobileParty, mobileParty.CurrentSettlement)) || mobileParty.CurrentSettlement?.SiegeEvent != null || (mobileParty.CurrentSettlement != null && mobileParty.PartySizeRatio < 0.9f))
		{
			return;
		}
		if (mobileParty.PatrolPartyComponent.HomeSettlement.HasPort && mobileParty.PatrolPartyComponent.IsNaval)
		{
			CalculatePatrollingScoreForSettlement(mobileParty.PatrolPartyComponent.HomeSettlement, p, isNavalPatrolling: true);
		}
		else
		{
			CalculatePatrollingScoreForSettlement(mobileParty.PatrolPartyComponent.HomeSettlement, p, isNavalPatrolling: false);
			foreach (Village boundVillage in mobileParty.PatrolPartyComponent.HomeSettlement.BoundVillages)
			{
				CalculatePatrollingScoreForSettlement(boundVillage.Settlement, p, isNavalPatrolling: false);
			}
		}
		CalculateVisitHomeSettlementScore(mobileParty, p);
	}

	private void CalculateVisitHomeSettlementScore(MobileParty mobileParty, PartyThinkParams p)
	{
		float partySizeRatio = mobileParty.PartySizeRatio;
		float num = 0f;
		if (!CanVisitSettlement(mobileParty, mobileParty.HomeSettlement))
		{
			return;
		}
		num = 0.15f;
		float num2 = 0f;
		if (GetLastHomeSettlementVisitTime(mobileParty, out var campaignTime))
		{
			num2 = campaignTime.ElapsedDaysUntilNow;
		}
		num += (num2 - 5f) * 0.025f;
		num += 1.1f / TaleWorlds.Library.MathF.Max(partySizeRatio, 0.01f);
		if (num > 0.15f)
		{
			AiHelper.GetBestNavigationTypeAndAdjustedDistanceOfSettlementForMobileParty(mobileParty, mobileParty.HomeSettlement, mobileParty.HomeSettlement.HasPort && mobileParty.IsCurrentlyAtSea, out var bestNavigationType, out var _, out var isFromPort);
			AIBehaviorData aiBehaviorData = new AIBehaviorData(mobileParty.HomeSettlement, AiBehavior.GoToSettlement, bestNavigationType, willGatherArmy: false, isFromPort, mobileParty.HomeSettlement.HasPort && mobileParty.IsCurrentlyAtSea);
			if (p.TryGetBehaviorScore(in aiBehaviorData, out var score))
			{
				p.SetBehaviorScore(in aiBehaviorData, num + score);
			}
			else
			{
				p.AddBehaviorScore((aiBehaviorData, num));
			}
		}
	}

	private bool CanVisitSettlement(MobileParty mobileParty, Settlement settlement)
	{
		if (!mobileParty.HasLandNavigationCapability || settlement.IsUnderSiege || settlement.Party.MapEvent != null)
		{
			if (!mobileParty.HasLandNavigationCapability)
			{
				if (settlement.SiegeEvent != null)
				{
					return !settlement.SiegeEvent.IsBlockadeActive;
				}
				return true;
			}
			return false;
		}
		return true;
	}

	private float GetSettlementScoreAdjustment(Settlement settlement, bool isNavalPatrolling)
	{
		if (!isNavalPatrolling)
		{
			if (!settlement.IsFortification)
			{
				if (!settlement.IsVillage)
				{
					return 1f;
				}
				if (!settlement.IsUnderRaid)
				{
					return 1.2f;
				}
				return 1.5f;
			}
			return 0.9f;
		}
		return 0.4f;
	}

	private void CalculatePatrollingScoreForSettlement(Settlement settlement, PartyThinkParams p, bool isNavalPatrolling)
	{
		MobileParty mobilePartyOf = p.MobilePartyOf;
		AiHelper.GetBestNavigationTypeAndAdjustedDistanceOfSettlementForMobileParty(mobilePartyOf, settlement, isNavalPatrolling, out var bestNavigationType, out var _, out var isFromPort);
		if (bestNavigationType == MobileParty.NavigationType.None)
		{
			return;
		}
		AIBehaviorData item = new AIBehaviorData(settlement, AiBehavior.PatrolAroundPoint, bestNavigationType, willGatherArmy: false, isFromPort, isNavalPatrolling);
		float num = Campaign.Current.Models.TargetScoreCalculatingModel.CalculateDefensivePatrollingScoreForSettlement(settlement, isNavalPatrolling, mobilePartyOf);
		num *= GetSettlementScoreAdjustment(settlement, bestNavigationType == MobileParty.NavigationType.Naval);
		num = TaleWorlds.Library.MathF.Max(num, 0.01f);
		if (1.25f + num > 0f)
		{
			if (!mobilePartyOf.IsCurrentlyAtSea)
			{
				_ = 2;
			}
			p.AddBehaviorScore((item, 1.25f + num));
		}
	}

	private void MobilePartyDestroyed(MobileParty party, PartyBase destroyerParty)
	{
		if (party.IsPatrolParty)
		{
			RemoveLastVisitEntry(party);
			if (!party.PatrolPartyComponent.IsNaval)
			{
				_interactedPatrolParties.Remove(party.HomeSettlement);
			}
		}
	}

	private void OnSettlementLeft(MobileParty party, Settlement settlement)
	{
		if (party != null && party.IsPatrolParty && settlement == party.HomeSettlement)
		{
			SetLastHomeSettlementVisitTime(party, CampaignTime.Now);
		}
	}

	private void SettlementEntered(MobileParty party, Settlement settlement, Hero hero)
	{
		if (party == null || !party.IsPatrolParty || settlement != party.HomeSettlement)
		{
			return;
		}
		SetLastHomeSettlementVisitTime(party, CampaignTime.Now);
		foreach (TroopRosterElement item in party.PrisonRoster.GetTroopRoster())
		{
			if (item.Character.HeroObject != null)
			{
				TransferPrisonerAction.Apply(item.Character, party.Party, settlement.Party);
			}
		}
		if (party.PrisonRoster.Count > 0)
		{
			SellPrisonersAction.ApplyForAllPrisoners(party.Party, settlement.Party);
		}
	}

	private bool CanSettlementSpawnNewPartyCurrently(Settlement settlement, bool includeReason, out TextObject reason)
	{
		if (!Campaign.Current.Models.SettlementPatrolModel.CanSettlementHavePatrolParties(settlement, naval: false))
		{
			reason = (includeReason ? new TextObject("{=RosQSZWa}No Guard House") : null);
			return false;
		}
		if (settlement.InRebelliousState)
		{
			reason = (includeReason ? new TextObject("{=UHDv0qer}Rebellious") : null);
			return false;
		}
		if (settlement.Town.IsUnderSiege || settlement.Party.MapEvent != null)
		{
			reason = (includeReason ? new TextObject("{=BhiOmgst}Under Siege") : null);
			return false;
		}
		reason = (includeReason ? TextObject.GetEmpty() : null);
		return settlement.PatrolParty == null;
	}

	private void MobilePartyCreated(MobileParty party)
	{
		if (party.IsPatrolParty)
		{
			SetLastHomeSettlementVisitTime(party, CampaignTime.Now);
		}
	}

	public override void SyncData(IDataStore dataStore)
	{
		dataStore.SyncData("_partyGenerationQueue", ref _partyGenerationQueue);
		dataStore.SyncData("_lastHomeSettlementVisitTimes", ref _lastHomeSettlementVisitTimes);
		dataStore.SyncData("_lastHomeSettlementVisitTimesCoastal", ref _lastHomeSettlementVisitTimesCoastal);
		dataStore.SyncData("_interactedPatrolParties", ref _interactedPatrolParties);
	}

	private bool GetLastHomeSettlementVisitTime(MobileParty mobileParty, out CampaignTime campaignTime)
	{
		return (mobileParty.PatrolPartyComponent.IsNaval ? _lastHomeSettlementVisitTimesCoastal : _lastHomeSettlementVisitTimes).TryGetValue(mobileParty.HomeSettlement, out campaignTime);
	}

	private void SetLastHomeSettlementVisitTime(MobileParty mobileParty, CampaignTime time)
	{
		(mobileParty.PatrolPartyComponent.IsNaval ? _lastHomeSettlementVisitTimesCoastal : _lastHomeSettlementVisitTimes)[mobileParty.HomeSettlement] = time;
	}

	private void RemoveLastVisitEntry(MobileParty mobileParty)
	{
		(mobileParty.PatrolPartyComponent.IsNaval ? _lastHomeSettlementVisitTimesCoastal : _lastHomeSettlementVisitTimes).Remove(mobileParty.HomeSettlement);
	}

	private void UpdateSettlementParties(Settlement settlement)
	{
		if (!Campaign.Current.Models.SettlementPatrolModel.CanSettlementHavePatrolParties(settlement, naval: false) && settlement.PatrolParty != null)
		{
			RemoveSettlementParties(settlement);
		}
	}

	private void RemoveSettlementParties(Settlement settlement)
	{
		_partyGenerationQueue.Remove(settlement);
		settlement.PatrolParty.MobileParty.MapEventSide = null;
		if (settlement.PatrolParty.MobileParty.IsActive)
		{
			DestroyPartyAction.Apply(null, settlement.PatrolParty.MobileParty);
		}
	}

	private void UpdateSettlementQueue(Settlement settlement, CampaignTime time)
	{
		_partyGenerationQueue[settlement] = time;
	}

	private void SpawnPatrolParty(Settlement settlement)
	{
		_partyGenerationQueue.Remove(settlement);
		PartyTemplateObject partyTemplateForPatrolParty = Campaign.Current.Models.SettlementPatrolModel.GetPartyTemplateForPatrolParty(settlement, naval: false);
		CampaignVec2 position = ((partyTemplateForPatrolParty.ShipHulls.Any() && settlement.HasPort && MBRandom.RandomFloat < 0.25f) ? settlement.PortPosition : settlement.GatePosition);
		PatrolPartyComponent.CreatePatrolParty("patrol_party_1", position, 8f * Campaign.Current.EstimatedAverageBanditPartySpeed, settlement, partyTemplateForPatrolParty);
	}

	public TextObject GetSettlementPatrolStatus(Settlement settlement)
	{
		TextObject empty = TextObject.GetEmpty();
		TextObject reason;
		CampaignTime value;
		if (settlement.PatrolParty != null)
		{
			empty = new TextObject("{=sUb6FHIE}{REMAINING_TROOP_COUNT}/{TOTAL_TROOP_COUNT}");
			empty.SetTextVariable("REMAINING_TROOP_COUNT", settlement.PatrolParty.MobileParty.MemberRoster.TotalManCount);
			empty.SetTextVariable("TOTAL_TROOP_COUNT", settlement.PatrolParty.MobileParty.Party.PartySizeLimit);
		}
		else if (!CanSettlementSpawnNewPartyCurrently(settlement, includeReason: true, out reason))
		{
			empty = reason;
		}
		else if (_partyGenerationQueue.TryGetValue(settlement, out value))
		{
			int variable = ((value == CampaignTime.Zero) ? 1 : Math.Max((int)Math.Ceiling(value.RemainingDaysFromNow), 1));
			empty = new TextObject("{=LvwUsZ9p}Ready in {DAYS} {?DAYS > 1}days{?}day{\\?}");
			empty.SetTextVariable("DAYS", variable);
		}
		else
		{
			empty = new TextObject("{=trainingPatrolParties}Training");
		}
		return empty;
	}
}
