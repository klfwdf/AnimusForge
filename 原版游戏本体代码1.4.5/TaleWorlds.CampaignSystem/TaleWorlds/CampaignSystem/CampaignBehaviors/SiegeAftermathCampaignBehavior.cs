using System.Collections.Generic;
using System.Linq;
using Helpers;
using TaleWorlds.CampaignSystem.Actions;
using TaleWorlds.CampaignSystem.CharacterDevelopment;
using TaleWorlds.CampaignSystem.Encounters;
using TaleWorlds.CampaignSystem.GameMenus;
using TaleWorlds.CampaignSystem.MapEvents;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.CampaignSystem.Settlements.Buildings;
using TaleWorlds.Core;
using TaleWorlds.Library;
using TaleWorlds.LinQuick;
using TaleWorlds.Localization;

namespace TaleWorlds.CampaignSystem.CampaignBehaviors;

public class SiegeAftermathCampaignBehavior : CampaignBehaviorBase
{
	private MobileParty _besiegerParty;

	private Clan _prevSettlementOwnerClan;

	private SiegeAftermathAction.SiegeAftermath _playerEncounterAftermath;

	private Dictionary<MobileParty, float> _siegeEventPartyContributions = new Dictionary<MobileParty, float>();

	private Dictionary<Building, int> _playerEncounterAftermathDamagedBuildings = new Dictionary<Building, int>();

	private bool _wasPlayerArmyMember;

	private float _settlementProsperityCache = -1f;

	public override void RegisterEvents()
	{
		CampaignEvents.OnSessionLaunchedEvent.AddNonSerializedListener(this, OnSessionLaunched);
		CampaignEvents.OnSiegeAftermathAppliedEvent.AddNonSerializedListener(this, OnSiegeAftermathApplied);
		CampaignEvents.MapEventEnded.AddNonSerializedListener(this, OnMapEventEnded);
		CampaignEvents.OnBuildingLevelChangedEvent.AddNonSerializedListener(this, OnBuildingLevelChanged);
		CampaignEvents.OnSettlementOwnerChangedEvent.AddNonSerializedListener(this, OnSettlementOwnerChanged);
	}

	private void OnSessionLaunched(CampaignGameStarter campaignGameStarter)
	{
		AddGameMenus(campaignGameStarter);
	}

	private void AddGameMenus(CampaignGameStarter gameSystemInitializer)
	{
		gameSystemInitializer.AddGameMenu("menu_settlement_taken", "", menu_settlement_taken_on_init);
		gameSystemInitializer.AddGameMenu("menu_settlement_taken_player_leader", "{=!}{SETTLEMENT_TAKEN_TEXT}", menu_settlement_taken_player_leader_on_init);
		gameSystemInitializer.AddGameMenuOption("menu_settlement_taken_player_leader", "menu_settlement_taken_devastate", "{=v0mZi3Zd}Devastate {DEVASTATE_INFLUENCE_COST_TEXT}", menu_settlement_taken_devastate_on_condition, menu_settlement_taken_devastate_on_consequence);
		gameSystemInitializer.AddGameMenuOption("menu_settlement_taken_player_leader", "menu_settlement_taken_pillage", "{=tZCLAkGZ}Pillage", menu_settlement_taken_pillage_on_condition, menu_settlement_taken_pillage_on_consequence);
		gameSystemInitializer.AddGameMenuOption("menu_settlement_taken_player_leader", "menu_settlement_taken_show_mercy", "{=EuwtMZGZ}Show Mercy {SHOW_MERCY_INFLUENCE_COST_TEXT}", menu_settlement_taken_show_mercy_on_condition, menu_settlement_taken_show_mercy_on_consequence);
		gameSystemInitializer.AddGameMenu("menu_settlement_taken_player_army_member", "{=!}{LEADER_DECISION_TEXT}", menu_settlement_taken_player_army_member_on_init);
		gameSystemInitializer.AddGameMenuOption("menu_settlement_taken_player_army_member", "menu_settlement_taken_continue", "{=veWOovVv}Continue...", continue_on_condition, menu_settlement_taken_continue_on_consequence);
		gameSystemInitializer.AddGameMenu("menu_settlement_taken_player_participant", "{=!}{PLAYER_PARTICIPANT_TEXT}", menu_settlement_taken_player_participant_on_init);
		gameSystemInitializer.AddGameMenuOption("menu_settlement_taken_player_participant", "menu_settlement_taken_continue", "{=veWOovVv}Continue...", continue_on_condition, menu_settlement_taken_continue_on_consequence);
		gameSystemInitializer.AddGameMenu("siege_aftermath_contextual_summary", "{=!}{START_OF_EXPLANATION}{newline} {newline}{CONTEXTUAL_SUMMARY_TEXT}{newline} {newline}{END_OF_EXPLANATION}", siege_aftermath_contextual_summary_on_init);
		gameSystemInitializer.AddGameMenuOption("siege_aftermath_contextual_summary", "menu_settlement_taken_continue", "{=veWOovVv}Continue...", continue_on_condition, menu_settlement_taken_continue_on_consequence);
	}

	private MBReadOnlyList<(PartyBase, float)> GetLootPercentagesOfPartiesOnSideForSiegeAftermath(MapEvent mapEvent, BattleSideEnum side)
	{
		MBReadOnlyList<MapEventParty> mBReadOnlyList = mapEvent.PartiesOnSide(side);
		MBList<(PartyBase, float)> mBList = new MBList<(PartyBase, float)>(mBReadOnlyList.Count);
		int num = mapEvent.GetMapEventSide(side).CalculateTotalContribution();
		foreach (MapEventParty item in mBReadOnlyList)
		{
			mBList.Add((item.Party, (int)(100f * ((float)item.ContributionToBattle / (float)num))));
		}
		return mBList;
	}

	private void OnMapEventEnded(MapEvent mapEvent)
	{
		BattleSideEnum battleSideEnum = ((!mapEvent.IsSallyOut && !mapEvent.IsBlockadeSallyOut) ? BattleSideEnum.Attacker : BattleSideEnum.Defender);
		if ((!mapEvent.IsSiegeAssault && !mapEvent.IsSiegeOutside && !mapEvent.IsSallyOut && !mapEvent.IsBlockadeSallyOut) || mapEvent.WinningSide != battleSideEnum || mapEvent.MapEventSettlement == null)
		{
			return;
		}
		_siegeEventPartyContributions.Clear();
		foreach (var item in GetLootPercentagesOfPartiesOnSideForSiegeAftermath(mapEvent, battleSideEnum))
		{
			if (item.Item1.IsMobile && !_siegeEventPartyContributions.ContainsKey(item.Item1.MobileParty))
			{
				_siegeEventPartyContributions.Add(item.Item1.MobileParty, item.Item2);
			}
		}
		if (mapEvent.GetMapEventSide(battleSideEnum).IsMainPartyAmongParties())
		{
			_playerEncounterAftermathDamagedBuildings.Clear();
			_wasPlayerArmyMember = false;
			_besiegerParty = mapEvent.GetMapEventSide(battleSideEnum).LeaderParty.MobileParty;
			_prevSettlementOwnerClan = mapEvent.MapEventSettlement.OwnerClan;
			if (_besiegerParty != MobileParty.MainParty)
			{
				if (_besiegerParty.Army != null && _besiegerParty.Army.Parties.Contains(MobileParty.MainParty))
				{
					_wasPlayerArmyMember = true;
				}
				else if (MobileParty.MainParty.Army != null && MobileParty.MainParty.Army.LeaderParty != MobileParty.MainParty)
				{
					_wasPlayerArmyMember = true;
				}
				_playerEncounterAftermath = DetermineAISiegeAftermath(_besiegerParty, mapEvent.MapEventSettlement);
				SiegeAftermathAction.ApplyAftermath(_besiegerParty, mapEvent.MapEventSettlement, _playerEncounterAftermath, _prevSettlementOwnerClan, _siegeEventPartyContributions);
			}
		}
		else if (mapEvent.MapEventSettlement.SiegeEvent != null)
		{
			MobileParty leaderParty = mapEvent.MapEventSettlement.SiegeEvent.BesiegerCamp.LeaderParty;
			SiegeAftermathAction.SiegeAftermath aftermathType = DetermineAISiegeAftermath(leaderParty, mapEvent.MapEventSettlement);
			SiegeAftermathAction.ApplyAftermath(leaderParty, mapEvent.MapEventSettlement, aftermathType, mapEvent.MapEventSettlement.OwnerClan, _siegeEventPartyContributions);
		}
		else
		{
			Debug.FailedAssert("Siege event is null in siege aftermath", "C:\\BuildAgent\\work\\mb3\\Source\\Bannerlord\\TaleWorlds.CampaignSystem\\CampaignBehaviors\\SiegeAftermathCampaignBehavior.cs", "OnMapEventEnded", 143);
		}
	}

	private void OnSiegeAftermathApplied(MobileParty attackerParty, Settlement settlement, SiegeAftermathAction.SiegeAftermath aftermathType, Clan previousSettlementOwner, Dictionary<MobileParty, float> partyContributions)
	{
		float siegeAftermathInfluenceCost = GetSiegeAftermathInfluenceCost(attackerParty, settlement, aftermathType);
		if (siegeAftermathInfluenceCost > 0f)
		{
			ChangeClanInfluenceAction.Apply(attackerParty.ActualClan, 0f - siegeAftermathInfluenceCost);
		}
		_settlementProsperityCache = settlement.Town.Prosperity;
		settlement.Town.Prosperity += GetSiegeAftermathProsperityPenalty(attackerParty, settlement, aftermathType);
		if (aftermathType != SiegeAftermathAction.SiegeAftermath.ShowMercy)
		{
			int siegeAftermathProjectsLoss = GetSiegeAftermathProjectsLoss(attackerParty, aftermathType);
			for (int i = 0; i < siegeAftermathProjectsLoss; i++)
			{
				settlement.Town.Buildings.GetRandomElementWithPredicate((Building x) => !x.BuildingType.IsDailyProject).LevelDown();
			}
			settlement.Town.Loyalty += GetSiegeAftermathLoyaltyPenalty(aftermathType);
			if (settlement.IsTown)
			{
				foreach (Hero notable in settlement.Notables)
				{
					notable.AddPower(notable.Power * GetSiegeAftermathNotablePowerModifierForAftermath(aftermathType));
				}
			}
			if (previousSettlementOwner.Leader == null)
			{
				Debug.Print($"{previousSettlementOwner.StringId}: {previousSettlementOwner} leader was null");
				Debug.FailedAssert($"{previousSettlementOwner.StringId}: {previousSettlementOwner} leader was null", "C:\\BuildAgent\\work\\mb3\\Source\\Bannerlord\\TaleWorlds.CampaignSystem\\CampaignBehaviors\\SiegeAftermathCampaignBehavior.cs", "OnSiegeAftermathApplied", 185);
			}
			if (attackerParty.LeaderHero != null)
			{
				ChangeRelationAction.ApplyRelationChangeBetweenHeroes(attackerParty.LeaderHero, previousSettlementOwner.Leader, GetSiegeAftermathRelationPenaltyWithSettlementOwner(aftermathType));
			}
		}
		float totalArmyGoldGain = GetSiegeAftermathArmyGoldGain(attackerParty, settlement, aftermathType);
		foreach (KeyValuePair<MobileParty, float> partyContribution in partyContributions)
		{
			MobileParty key = partyContribution.Key;
			int siegeAftermathPartyGoldGain = GetSiegeAftermathPartyGoldGain(totalArmyGoldGain, partyContribution.Value);
			if (key.LeaderHero != null)
			{
				GiveGoldAction.ApplyForPartyToCharacter(null, key.LeaderHero, siegeAftermathPartyGoldGain);
			}
			else
			{
				GiveGoldAction.ApplyForCharacterToParty(null, key.Party, siegeAftermathPartyGoldGain);
			}
			key.RecentEventsMorale += GetSiegeAftermathPartyMoraleBonus(attackerParty, settlement, aftermathType);
			if (attackerParty == MobileParty.MainParty && key != attackerParty && key.LeaderHero != null && aftermathType != SiegeAftermathAction.SiegeAftermath.Pillage && attackerParty.MapFaction.Culture != settlement.Culture)
			{
				int siegeAftermathRelationChangeWithLord = GetSiegeAftermathRelationChangeWithLord(key.LeaderHero, aftermathType);
				if (siegeAftermathRelationChangeWithLord != 0)
				{
					ChangeRelationAction.ApplyPlayerRelation(key.LeaderHero, siegeAftermathRelationChangeWithLord);
				}
			}
		}
		if (attackerParty == MobileParty.MainParty && aftermathType != SiegeAftermathAction.SiegeAftermath.Pillage)
		{
			TraitLevelingHelper.OnSiegeAftermathApplied(settlement, aftermathType, new TraitObject[1] { DefaultTraits.Mercy });
		}
	}

	private SiegeAftermathAction.SiegeAftermath DetermineSiegeAftermathOnEncounterLeaderDeath(MobileParty attackerParty, Settlement settlement)
	{
		if (attackerParty.MapFaction?.Culture != settlement.Culture)
		{
			return SiegeAftermathAction.SiegeAftermath.Devastate;
		}
		return SiegeAftermathAction.SiegeAftermath.ShowMercy;
	}

	private bool IsMobilePartyLeaderAliveForSiegeAftermath(MobileParty attackerParty)
	{
		if (attackerParty.LeaderHero != null && attackerParty.LeaderHero.IsAlive && attackerParty.LeaderHero.DeathMark != KillCharacterAction.KillCharacterActionDetail.DiedInBattle)
		{
			return attackerParty.LeaderHero.DeathMark != KillCharacterAction.KillCharacterActionDetail.WoundedInBattle;
		}
		return false;
	}

	private SiegeAftermathAction.SiegeAftermath DetermineAISiegeAftermath(MobileParty attackerParty, Settlement settlement)
	{
		if (!IsMobilePartyLeaderAliveForSiegeAftermath(attackerParty))
		{
			return DetermineSiegeAftermathOnEncounterLeaderDeath(attackerParty, settlement);
		}
		bool flag = false;
		bool flag2 = false;
		bool flag3 = false;
		float num = ((attackerParty.Army != null) ? attackerParty.Army.Morale : attackerParty.Morale);
		if (attackerParty.MapFaction?.Culture == settlement.Culture || (attackerParty.ActualClan?.Influence > 2f * GetSiegeAftermathInfluenceCost(attackerParty, settlement, SiegeAftermathAction.SiegeAftermath.ShowMercy) && num > 60f))
		{
			flag = true;
		}
		if (attackerParty.MapFaction?.Culture != settlement.Culture)
		{
			flag2 = true;
		}
		if (attackerParty.MapFaction?.Culture != settlement.Culture && attackerParty.ActualClan?.Influence > 2f * GetSiegeAftermathInfluenceCost(attackerParty, settlement, SiegeAftermathAction.SiegeAftermath.Devastate) && num < 90f)
		{
			flag3 = true;
		}
		float num2 = 0f;
		float num3 = 0f;
		float num4 = 0f;
		int num5 = attackerParty.LeaderHero?.GetTraitLevel(DefaultTraits.Mercy) ?? 0;
		if (num5 > 0)
		{
			num2 = 0.4f + 0.2f * (float)num5;
			num3 = 1f - num2;
			num4 = 0f;
		}
		else if (num5 < 0)
		{
			num4 = 0.4f + 0.2f * (float)MathF.Abs(num5);
			num3 = 1f - num4;
			num2 = 0f;
		}
		else
		{
			num2 = 0.2f;
			num3 = 0.6f;
			num4 = 0.2f;
		}
		if (!flag)
		{
			num3 += num2;
			num2 = 0f;
		}
		if (!flag3)
		{
			num3 += num4;
			num4 = 0f;
		}
		if (!flag2)
		{
			num2 += num3;
			num3 = 0f;
		}
		return MBRandom.ChooseWeighted(new List<(SiegeAftermathAction.SiegeAftermath, float)>
		{
			(SiegeAftermathAction.SiegeAftermath.ShowMercy, num2),
			(SiegeAftermathAction.SiegeAftermath.Pillage, num3),
			(SiegeAftermathAction.SiegeAftermath.Devastate, num4)
		});
	}

	private void OnBuildingLevelChanged(Town town, Building building, int level)
	{
		if (town.Settlement == PlayerEncounter.EncounterSettlement && level < 0)
		{
			if (!_playerEncounterAftermathDamagedBuildings.ContainsKey(building))
			{
				_playerEncounterAftermathDamagedBuildings.Add(building, 0);
			}
			_playerEncounterAftermathDamagedBuildings[building] += -1;
		}
	}

	private void HandlePlayerDeathDuringSiegeAftermath()
	{
		Settlement currentSettlement = Settlement.CurrentSettlement;
		SiegeAftermathAction.SiegeAftermath aftermathType = DetermineSiegeAftermathOnEncounterLeaderDeath(MobileParty.MainParty, currentSettlement);
		SiegeAftermathAction.ApplyAftermath(MobileParty.MainParty, currentSettlement, aftermathType, _prevSettlementOwnerClan, _siegeEventPartyContributions);
		GameMenu.SwitchToMenu("siege_aftermath_contextual_summary");
	}

	private void menu_settlement_taken_on_init(MenuCallbackArgs args)
	{
		MobileParty besiegerParty = _besiegerParty;
		if (besiegerParty == MobileParty.MainParty)
		{
			if (IsMobilePartyLeaderAliveForSiegeAftermath(besiegerParty))
			{
				GameMenu.SwitchToMenu("menu_settlement_taken_player_leader");
			}
			else
			{
				HandlePlayerDeathDuringSiegeAftermath();
			}
		}
		else if (_wasPlayerArmyMember)
		{
			GameMenu.SwitchToMenu("menu_settlement_taken_player_army_member");
		}
		else
		{
			GameMenu.SwitchToMenu("menu_settlement_taken_player_participant");
		}
	}

	private void menu_settlement_taken_player_leader_on_init(MenuCallbackArgs args)
	{
		args.MenuContext.GameMenu.GetText().SetTextVariable("SETTLEMENT_TAKEN_TEXT", new TextObject("{=QvyFYn1b}The defenders are routed, and it's clear that {TOWN_NAME} is yours. It's time for you to determine the fate of the {?IS_CITY}city{?}fortress{\\?}."));
		MBTextManager.SetTextVariable("TOWN_NAME", Settlement.CurrentSettlement.Name);
		MBTextManager.SetTextVariable("IS_CITY", Settlement.CurrentSettlement.IsTown ? 1 : 0);
		args.MenuContext.SetBackgroundMeshName("encounter_win");
	}

	private void menu_settlement_taken_player_army_member_on_init(MenuCallbackArgs args)
	{
		bool flag = _besiegerParty.Army != null && _besiegerParty.Army.Parties.Contains(MobileParty.MainParty);
		Settlement currentSettlement = _besiegerParty.CurrentSettlement;
		TextObject textObject;
		if (_playerEncounterAftermath == SiegeAftermathAction.SiegeAftermath.Devastate)
		{
			textObject = (flag ? new TextObject("{=peHCARhM}{DEFAULT_TEXT}{ARMY_LEADER.LINK} has ordered that {SETTLEMENT} to be laid waste. {?ARMY_LEADER.GENDER}Her{?}His{\\?} troops sweep through the {?IS_CITY}city{?}fortress{\\?} taking whatever loot they like and setting fire to the rest.") : ((!_wasPlayerArmyMember) ? TextObject.GetEmpty() : new TextObject("{=qeRRWMfU}{DEFAULT_TEXT}{ARMY_LEADER.LINK} fell during the fighting. {?ARMY_LEADER.GENDER}Her{?}His{\\?} vengeful troops sweep through the {?IS_CITY}city{?}fortress{\\?} taking whatever loot they like and setting fire to the rest.")));
			textObject.SetTextVariable("IS_CITY", currentSettlement.IsTown ? 1 : 0);
		}
		else if (_playerEncounterAftermath == SiegeAftermathAction.SiegeAftermath.Pillage)
		{
			if (flag)
			{
				textObject = new TextObject("{=BXw5MwX7}{DEFAULT_TEXT}{ARMY_LEADER.LINK} grants {?ARMY_LEADER.GENDER}her{?}his{\\?} men their customary right of pillage after a successful siege. {?ARMY_LEADER.GENDER}She{?}He{\\?} tells them they may take property but must spare the townsfolk's lives.");
			}
			else if (_wasPlayerArmyMember)
			{
				Debug.FailedAssert("_wasPlayerArmyMember", "C:\\BuildAgent\\work\\mb3\\Source\\Bannerlord\\TaleWorlds.CampaignSystem\\CampaignBehaviors\\SiegeAftermathCampaignBehavior.cs", "menu_settlement_taken_player_army_member_on_init", 433);
				textObject = new TextObject("{=99v8GTTe}{DEFAULT_TEXT}Before {?ARMY_LEADER.GENDER}she{?}he{\\?} fell, {ARMY_LEADER.LINK} granted {?ARMY_LEADER.GENDER}her{?}his{\\?} men their customary right of pillage after a successful siege. They may take property but must spare the townsfolk's lives.");
			}
			else
			{
				textObject = TextObject.GetEmpty();
			}
		}
		else if (_besiegerParty.MapFaction?.Culture == _besiegerParty.CurrentSettlement.Culture)
		{
			if (flag)
			{
				textObject = new TextObject("{=Wmq47pvL}{DEFAULT_TEXT}{ARMY_LEADER.LINK} had to show mercy to the people of {SETTLEMENT} who were originally descendants of {FACTION}.");
				textObject.SetTextVariable("FACTION", currentSettlement.Culture.GetName());
			}
			else if (_wasPlayerArmyMember)
			{
				textObject = new TextObject("{=F5Xc0m5O}{DEFAULT_TEXT}{ARMY_LEADER.LINK} fell during the fighting. {?ARMY_LEADER.GENDER}Her{?}His{\\?} troops, reluctant to harm their {CULTURE_ADJ} kinfolk, forego their traditional right of pillage.");
				textObject.SetTextVariable("CULTURE_ADJ", FactionHelper.GetAdjectiveForFaction(_besiegerParty.MapFaction));
			}
			else
			{
				textObject = TextObject.GetEmpty();
			}
		}
		else if (flag)
		{
			textObject = new TextObject("{=Bp0ZQbfp}{DEFAULT_TEXT}{ARMY_LEADER.LINK} has decided to show mercy to the people of {SETTLEMENT}. You can hear disgruntled murmuring among the troops, who have been denied their customary right of pillage.");
		}
		else if (_wasPlayerArmyMember)
		{
			Debug.FailedAssert("_wasPlayerArmyMember", "C:\\BuildAgent\\work\\mb3\\Source\\Bannerlord\\TaleWorlds.CampaignSystem\\CampaignBehaviors\\SiegeAftermathCampaignBehavior.cs", "menu_settlement_taken_player_army_member_on_init", 468);
			textObject = new TextObject("{=ULtzLvXi}{DEFAULT_TEXT}Before {?ARMY_LEADER.GENDER}she{?}he{\\?} fell, {ARMY_LEADER.LINK} gave orders that mercy should be shown to the people of {SETTLEMENT}.");
		}
		else
		{
			textObject = TextObject.GetEmpty();
		}
		TextObject text = args.MenuContext.GameMenu.GetText();
		TextObject textObject2 = new TextObject("{=hvQUqRSb}{SETTLEMENT} has been taken by an army of which you are a member. ");
		textObject2.SetTextVariable("SETTLEMENT", currentSettlement.GetName());
		textObject.SetTextVariable("DEFAULT_TEXT", textObject2);
		StringHelpers.SetCharacterProperties("ARMY_LEADER", _besiegerParty.LordPartyComponent?.Owner?.CharacterObject ?? CharacterObject.PlayerCharacter, textObject);
		textObject.SetTextVariable("SETTLEMENT", currentSettlement.GetName());
		text.SetTextVariable("LEADER_DECISION_TEXT", textObject);
		text.SetTextVariable("SETTLEMENT", currentSettlement.GetName());
		args.MenuContext.SetBackgroundMeshName("encounter_win");
	}

	private void menu_settlement_taken_player_participant_on_init(MenuCallbackArgs args)
	{
		Settlement currentSettlement = _besiegerParty.CurrentSettlement;
		TextObject textObject = new TextObject("{=C2KeQd0a}{ENCOUNTER_LEADER.LINK} thanks you for helping in the siege of {SETTLEMENT}. You were able to loot your fallen foes, but you do not participate in the sack of the {?IS_TOWN}town{?}castle{\\?} as you are not part of the army that took it.");
		StringHelpers.SetCharacterProperties("ENCOUNTER_LEADER", _besiegerParty.LordPartyComponent?.Owner?.CharacterObject ?? CharacterObject.PlayerCharacter, textObject);
		textObject.SetTextVariable("SETTLEMENT", currentSettlement.GetName());
		textObject.SetTextVariable("IS_TOWN", currentSettlement.IsTown ? 1 : 0);
		args.MenuContext.GameMenu.GetText().SetTextVariable("PLAYER_PARTICIPANT_TEXT", textObject);
		args.MenuContext.SetBackgroundMeshName("encounter_win");
	}

	private void menu_settlement_taken_continue_on_consequence(MenuCallbackArgs args)
	{
		GameMenu.ExitToLast();
	}

	private bool menu_settlement_taken_devastate_on_condition(MenuCallbackArgs args)
	{
		Settlement currentSettlement = Settlement.CurrentSettlement;
		MobileParty attackerParty = _besiegerParty;
		MBList<MobileParty> mBList = attackerParty.Army?.Parties.WhereQ((MobileParty t) => t != attackerParty && t.LeaderHero != null && t.LeaderHero.GetTraitLevel(DefaultTraits.Mercy) > 0).ToMBList() ?? new MBList<MobileParty>();
		int num = (int)GetSiegeAftermathInfluenceCost(attackerParty, currentSettlement, SiegeAftermathAction.SiegeAftermath.Devastate);
		bool flag = (float)num > 0f;
		bool flag2 = currentSettlement.Culture == Hero.MainHero.MapFaction?.Culture;
		TextObject textObject = new TextObject("{=FPPb7ur6}({INFLUENCE_AMOUNT}{INFLUENCE_ICON})");
		textObject.SetTextVariable("INFLUENCE_AMOUNT", num);
		textObject.SetTextVariable("INFLUENCE_ICON", "{=!}<img src=\"General\\Icons\\Influence@2x\" extend=\"5\">");
		MBTextManager.SetTextVariable("DEVASTATE_INFLUENCE_COST_TEXT", flag ? textObject : TextObject.GetEmpty());
		TextObject textObject2 = new TextObject("{=0FxtNPvV}You cannot devastate a settlement that has your faction culture.{newline}");
		TextObject textObject3 = new TextObject("{=Q9RXyDBz}{newline} • {HERO.NAME} must use {INFLUENCE_AMOUNT} influence to convince {MERCIFUL_LORD_COUNT} merciful leaders of this action:{newline} {MERCIFUL_LORDS}");
		StringHelpers.SetCharacterProperties("HERO", attackerParty.LeaderHero?.CharacterObject, textObject3);
		textObject3.SetTextVariable("INFLUENCE_AMOUNT", num);
		textObject3.SetTextVariable("MERCIFUL_LORD_COUNT", mBList.Count);
		List<TextObject> list = new List<TextObject>();
		foreach (MobileParty item in mBList)
		{
			list.Add(item.LeaderHero.Name);
		}
		textObject3.SetTextVariable("MERCIFUL_LORDS", GameTexts.GameTextHelper.MergeTextObjectsWithSymbol(list, new TextObject("{=!}{newline}")));
		textObject3.SetTextVariable("INFLUENCE_AMOUNT", num);
		TextObject textObject4 = new TextObject("{=!}{CULTURE_CONDITION_TEXT}{STATIC_CONDITIONS_TEXT}{INFLUENCE_CONDITION_TEXT}");
		textObject4.SetTextVariable("CULTURE_CONDITION_TEXT", flag2 ? textObject2 : TextObject.GetEmpty());
		textObject4.SetTextVariable("STATIC_CONDITIONS_TEXT", GetSiegeAftermathConsequencesText(attackerParty, currentSettlement, SiegeAftermathAction.SiegeAftermath.Devastate, isTooltip: true));
		textObject4.SetTextVariable("INFLUENCE_CONDITION_TEXT", flag ? textObject3 : TextObject.GetEmpty());
		args.IsEnabled = IsSiegeAftermathPossible(attackerParty, currentSettlement, SiegeAftermathAction.SiegeAftermath.Devastate);
		args.Tooltip = textObject4;
		args.optionLeaveType = GameMenuOption.LeaveType.Devastate;
		return true;
	}

	private void menu_settlement_taken_devastate_on_consequence(MenuCallbackArgs args)
	{
		Settlement currentSettlement = Settlement.CurrentSettlement;
		MobileParty besiegerParty = _besiegerParty;
		_playerEncounterAftermath = SiegeAftermathAction.SiegeAftermath.Devastate;
		SiegeAftermathAction.ApplyAftermath(besiegerParty, currentSettlement, _playerEncounterAftermath, _prevSettlementOwnerClan, _siegeEventPartyContributions);
		GameMenu.SwitchToMenu("siege_aftermath_contextual_summary");
	}

	private bool continue_on_condition(MenuCallbackArgs args)
	{
		args.optionLeaveType = GameMenuOption.LeaveType.Continue;
		return true;
	}

	private bool menu_settlement_taken_pillage_on_condition(MenuCallbackArgs args)
	{
		Settlement currentSettlement = Settlement.CurrentSettlement;
		MobileParty besiegerParty = _besiegerParty;
		bool flag = currentSettlement.Culture == Hero.MainHero.MapFaction?.Culture;
		TextObject textObject = new TextObject("{=!}{CULTURE_CONDITION_TEXT}{STATIC_CONDITIONS_TEXT}");
		TextObject textObject2 = new TextObject("{=uwmHjy7z}You cannot pillage a settlement that has your faction culture.{newline}");
		textObject.SetTextVariable("CULTURE_CONDITION_TEXT", flag ? textObject2 : TextObject.GetEmpty());
		textObject.SetTextVariable("STATIC_CONDITIONS_TEXT", GetSiegeAftermathConsequencesText(besiegerParty, currentSettlement, SiegeAftermathAction.SiegeAftermath.Pillage, isTooltip: true));
		args.IsEnabled = IsSiegeAftermathPossible(besiegerParty, currentSettlement, SiegeAftermathAction.SiegeAftermath.Pillage);
		args.Tooltip = textObject;
		args.optionLeaveType = GameMenuOption.LeaveType.Pillage;
		return true;
	}

	private void menu_settlement_taken_pillage_on_consequence(MenuCallbackArgs args)
	{
		Settlement currentSettlement = Settlement.CurrentSettlement;
		MobileParty besiegerParty = _besiegerParty;
		_playerEncounterAftermath = SiegeAftermathAction.SiegeAftermath.Pillage;
		SiegeAftermathAction.ApplyAftermath(besiegerParty, currentSettlement, _playerEncounterAftermath, _prevSettlementOwnerClan, _siegeEventPartyContributions);
		GameMenu.SwitchToMenu("siege_aftermath_contextual_summary");
	}

	private bool menu_settlement_taken_show_mercy_on_condition(MenuCallbackArgs args)
	{
		Settlement currentSettlement = Settlement.CurrentSettlement;
		MobileParty attackerParty = _besiegerParty;
		MBList<MobileParty> mBList = attackerParty.Army?.Parties.WhereQ((MobileParty t) => t != attackerParty && t.LeaderHero != null && t.LeaderHero.GetTraitLevel(DefaultTraits.Mercy) < 0).ToMBList() ?? new MBList<MobileParty>();
		int num = (int)GetSiegeAftermathInfluenceCost(attackerParty, currentSettlement, SiegeAftermathAction.SiegeAftermath.ShowMercy);
		bool flag = currentSettlement.Culture == attackerParty.MapFaction?.Culture;
		bool flag2 = (float)num > 0f;
		TextObject textObject = new TextObject("{=FPPb7ur6}({INFLUENCE_AMOUNT}{INFLUENCE_ICON})");
		textObject.SetTextVariable("INFLUENCE_AMOUNT", num);
		textObject.SetTextVariable("INFLUENCE_ICON", "{=!}<img src=\"General\\Icons\\Influence@2x\" extend=\"5\">");
		MBTextManager.SetTextVariable("SHOW_MERCY_INFLUENCE_COST_TEXT", flag2 ? textObject : TextObject.GetEmpty());
		TextObject textObject2 = new TextObject("{=aXFYyBEQ}Showing mercy to a settlement that shares your faction's culture requires no influence.{newline}");
		TextObject textObject3 = new TextObject("{=bn5fpYx3}{newline} • {HERO.NAME} must use {INFLUENCE_AMOUNT} influence to convince {CRUEL_LORD_COUNT} non-merciful leaders of this action:{newline} {CRUEL_LORDS}");
		StringHelpers.SetCharacterProperties("HERO", attackerParty.LeaderHero?.CharacterObject, textObject3);
		textObject3.SetTextVariable("INFLUENCE_AMOUNT", num);
		textObject3.SetTextVariable("CRUEL_LORD_COUNT", mBList.Count);
		List<TextObject> list = new List<TextObject>();
		foreach (MobileParty item in mBList)
		{
			list.Add(item.LeaderHero.Name);
		}
		textObject3.SetTextVariable("CRUEL_LORDS", GameTexts.GameTextHelper.MergeTextObjectsWithSymbol(list, new TextObject("{=!}{newline}")));
		TextObject textObject4 = new TextObject("{=!}{CULTURE_CONDITION_TEXT}{STATIC_CONDITIONS_TEXT}{INFLUENCE_CONDITION_TEXT}");
		textObject4.SetTextVariable("CULTURE_CONDITION_TEXT", flag ? textObject2 : TextObject.GetEmpty());
		textObject4.SetTextVariable("STATIC_CONDITIONS_TEXT", GetSiegeAftermathConsequencesText(attackerParty, currentSettlement, SiegeAftermathAction.SiegeAftermath.ShowMercy, isTooltip: true));
		textObject4.SetTextVariable("INFLUENCE_CONDITION_TEXT", flag2 ? textObject3 : TextObject.GetEmpty());
		args.IsEnabled = IsSiegeAftermathPossible(attackerParty, currentSettlement, SiegeAftermathAction.SiegeAftermath.ShowMercy);
		args.Tooltip = textObject4;
		args.optionLeaveType = GameMenuOption.LeaveType.ShowMercy;
		return true;
	}

	private void menu_settlement_taken_show_mercy_on_consequence(MenuCallbackArgs args)
	{
		Settlement currentSettlement = Settlement.CurrentSettlement;
		MobileParty besiegerParty = _besiegerParty;
		_playerEncounterAftermath = SiegeAftermathAction.SiegeAftermath.ShowMercy;
		SiegeAftermathAction.ApplyAftermath(besiegerParty, currentSettlement, _playerEncounterAftermath, _prevSettlementOwnerClan, _siegeEventPartyContributions);
		GameMenu.SwitchToMenu("siege_aftermath_contextual_summary");
	}

	private TextObject GetSiegeAftermathConsequencesText(MobileParty attackerParty, Settlement settlement, SiegeAftermathAction.SiegeAftermath aftermath, bool isTooltip)
	{
		TextObject textObject = new TextObject("{=!}{PROSPERITY_TEXT}{TOWN_PROJECTS_TEXT}{LOYALTY_TEXT}{NOTABLE_POWER_TEXT}{PARTY_MORALE_TEXT}{ARMY_GOLD_TEXT}{PARTY_GOLD_TEXT}{OWNER_RELATION_TEXT}");
		TextObject textObject2 = new TextObject("{=ERh2DVEa} • Prosperity Lost: {PROSPERITY_LOST_AMOUNT}");
		textObject2.SetTextVariable("PROSPERITY_LOST_AMOUNT", -1 * (int)GetSiegeAftermathProsperityPenalty(attackerParty, settlement, aftermath));
		textObject.SetTextVariable("PROSPERITY_TEXT", textObject2);
		TextObject textObject3 = new TextObject("{=HtHcEv7N}{newline} • Party Morale Change : {MORALE_CHANGE}");
		textObject3.SetTextVariable("MORALE_CHANGE", GetSiegeAftermathPartyMoraleBonus(attackerParty, settlement, aftermath));
		textObject.SetTextVariable("PARTY_MORALE_TEXT", textObject3);
		if (aftermath != SiegeAftermathAction.SiegeAftermath.ShowMercy)
		{
			TextObject empty = TextObject.GetEmpty();
			if (isTooltip)
			{
				empty = new TextObject("{=tF1G5YLe}{newline} • Building Levels Reduced: {LEVELS_LOST}");
				empty.SetTextVariable("LEVELS_LOST", GetSiegeAftermathProjectsLoss(attackerParty, aftermath));
				textObject.SetTextVariable("TOWN_PROJECTS_TEXT", empty);
			}
			else
			{
				empty = new TextObject("{=WDRTZ8se}{newline} • Town Projects Razed: {LEVELS_LOST}{PROJECTS_DESTROYED}");
				TextObject textObject4 = new TextObject("{=W1KJEvit}{newline}    Levels Lost: {BUILDINGS_LOST_LEVEL}");
				TextObject textObject5 = new TextObject("{=n1bQHmCk}{newline}    Projects Destroyed: {BUILDINGS_DESTROYED}");
				TextObject textObject6 = new TextObject("{=HDNedIxl}{newline}        {BUILDING_NAME}: {LEVEL_LOST}");
				TextObject textObject7 = new TextObject("{=jZmBbA5M}{newline}        {BUILDING_NAME}");
				List<KeyValuePair<Building, int>> list = new List<KeyValuePair<Building, int>>(_playerEncounterAftermathDamagedBuildings.Where((KeyValuePair<Building, int> t) => t.Key.CurrentLevel > 0));
				List<Building> list2 = new List<Building>(from t in _playerEncounterAftermathDamagedBuildings
					where t.Key.CurrentLevel <= 0
					select t.Key);
				string text = "";
				foreach (KeyValuePair<Building, int> item in list)
				{
					TextObject textObject8 = textObject6.CopyTextObject();
					textObject8.SetTextVariable("BUILDING_NAME", item.Key.Name);
					textObject8.SetTextVariable("LEVEL_LOST", item.Value);
					text += textObject8.ToString();
				}
				string text2 = "";
				foreach (Building item2 in list2)
				{
					TextObject textObject9 = textObject7.CopyTextObject();
					textObject9.SetTextVariable("BUILDING_NAME", item2.Name);
					text2 += textObject9.ToString();
				}
				textObject4.SetTextVariable("BUILDINGS_LOST_LEVEL", text);
				textObject5.SetTextVariable("BUILDINGS_DESTROYED", text2);
				empty.SetTextVariable("LEVELS_LOST", (!list.IsEmpty()) ? textObject4.ToString() : "");
				empty.SetTextVariable("PROJECTS_DESTROYED", (!list2.IsEmpty()) ? textObject5.ToString() : "");
				textObject.SetTextVariable("TOWN_PROJECTS_TEXT", (!list2.IsEmpty() || !list.IsEmpty()) ? empty.ToString() : "");
			}
			TextObject textObject10 = new TextObject("{=EVxxKXmW}{newline} • Loyalty in {SETTLEMENT} : {LOYALTY_LOST_AMOUNT}");
			textObject10.SetTextVariable("LOYALTY_LOST_AMOUNT", GetSiegeAftermathLoyaltyPenalty(aftermath));
			textObject10.SetTextVariable("SETTLEMENT", settlement.GetName());
			textObject.SetTextVariable("LOYALTY_TEXT", textObject10);
			if (settlement.Notables.Count > 0)
			{
				TextObject textObject11 = new TextObject("{=38dcXWzq}{newline} • Notable Powers: {NOTABLE_POWER_LOST_AMOUNT}%");
				textObject11.SetTextVariable("NOTABLE_POWER_LOST_AMOUNT", GetSiegeAftermathNotablePowerModifierForAftermath(aftermath) * 100f);
				textObject.SetTextVariable("NOTABLE_POWER_TEXT", textObject11);
			}
			TextObject textObject12 = new TextObject("{=RO3Zv0K4}{newline} • Relation with Settlement Owner : {OWNER.LINK} : {RELATION_CHANGE}");
			textObject12.SetTextVariable("RELATION_CHANGE", GetSiegeAftermathRelationPenaltyWithSettlementOwner(aftermath));
			StringHelpers.SetCharacterProperties("OWNER", _prevSettlementOwnerClan.Leader.CharacterObject, textObject12);
			textObject.SetTextVariable("OWNER_RELATION_TEXT", textObject12);
			int siegeAftermathArmyGoldGain = GetSiegeAftermathArmyGoldGain(attackerParty, settlement, aftermath);
			if (attackerParty.Army != null)
			{
				TextObject textObject13 = new TextObject("{=2wwAyZdL}{newline} • Army Gold Gain : {ARMY_GOLD_GAIN}");
				textObject13.SetTextVariable("ARMY_GOLD_GAIN", siegeAftermathArmyGoldGain);
				textObject.SetTextVariable("ARMY_GOLD_TEXT", textObject13);
			}
			else
			{
				textObject.SetTextVariable("ARMY_GOLD_TEXT", "");
			}
			TextObject textObject14 = new TextObject("{=RmW8Wf83}{newline} • Party Gold Gain : {PARTY_GOLD_GAIN}");
			textObject14.SetTextVariable("PARTY_GOLD_GAIN", GetSiegeAftermathPartyGoldGain(siegeAftermathArmyGoldGain, _siegeEventPartyContributions.TryGetValue(attackerParty, out var value) ? value : 0f));
			textObject.SetTextVariable("PARTY_GOLD_TEXT", textObject14);
		}
		else
		{
			textObject.SetTextVariable("TOWN_PROJECTS_TEXT", "");
			textObject.SetTextVariable("LOYALTY_TEXT", "");
			textObject.SetTextVariable("NOTABLE_POWER_TEXT", "");
			textObject.SetTextVariable("OWNER_RELATION_TEXT", "");
			textObject.SetTextVariable("ARMY_GOLD_TEXT", "");
			textObject.SetTextVariable("PARTY_GOLD_TEXT", "");
		}
		return textObject;
	}

	private void siege_aftermath_contextual_summary_on_init(MenuCallbackArgs args)
	{
		Settlement currentSettlement = Settlement.CurrentSettlement;
		MobileParty besiegerParty = _besiegerParty;
		if (_playerEncounterAftermath == SiegeAftermathAction.SiegeAftermath.Devastate)
		{
			TextObject textObject = new TextObject("{=VFVqjZwY}Your troops sweep through the {?IS_CITY}city{?}fortress{\\?}, taking whatever loot they like and setting fire to the rest.");
			textObject.SetTextVariable("IS_CITY", Settlement.CurrentSettlement.IsTown ? 1 : 0);
			MBTextManager.SetTextVariable("START_OF_EXPLANATION", textObject);
		}
		else if (_playerEncounterAftermath == SiegeAftermathAction.SiegeAftermath.Pillage)
		{
			MBTextManager.SetTextVariable("START_OF_EXPLANATION", new TextObject("{=oJUxWEwp}You grant your men their customary right of pillage after a successful siege. You tell them they may take property but must spare the townsfolk's lives."));
		}
		else if (currentSettlement.Culture != besiegerParty.MapFaction?.Culture)
		{
			TextObject textObject2 = new TextObject("{=x2dvXNQ0}You have decided to show mercy to the people of {SETTLEMENT_NAME}.{newline}You can hear disgruntled murmuring among the troops, who have been denied their customary right of pillage.");
			textObject2.SetTextVariable("SETTLEMENT_NAME", currentSettlement.Name);
			MBTextManager.SetTextVariable("START_OF_EXPLANATION", textObject2);
		}
		else
		{
			TextObject textObject3 = new TextObject("{=bXN2fbcv}Your men treat the residents of {SETTLEMENT_NAME} as wayward subjects of the {SETTLEMENT_CULTURE_NAME} rather than foes, and treat them relatively well.");
			textObject3.SetTextVariable("SETTLEMENT_NAME", currentSettlement.Name);
			textObject3.SetTextVariable("SETTLEMENT_CULTURE_NAME", FactionHelper.GetFormalNameForFactionCulture(currentSettlement.Culture));
			MBTextManager.SetTextVariable("START_OF_EXPLANATION", textObject3);
		}
		MBTextManager.SetTextVariable("CONTEXTUAL_SUMMARY_TEXT", GetSiegeAftermathConsequencesText(besiegerParty, currentSettlement, _playerEncounterAftermath, isTooltip: false));
		TextObject text = new TextObject("{=I0ZG4tIj}{TOWN_NAME} has fallen to your troops. You may station a garrison here to defend it against enemies who may try to recapture it.");
		MBTextManager.SetTextVariable("TOWN_NAME", Settlement.CurrentSettlement.Name);
		MBTextManager.SetTextVariable("END_OF_EXPLANATION", text);
		args.MenuContext.SetBackgroundMeshName("encounter_win");
	}

	private float GetSiegeAftermathProsperityPenalty(MobileParty attackerParty, Settlement settlement, SiegeAftermathAction.SiegeAftermath aftermathType)
	{
		int num = attackerParty.MemberRoster?.TotalHealthyCount ?? 0;
		if (attackerParty.Army != null)
		{
			num = attackerParty.Army.TotalHealthyMembers;
		}
		float num2 = -1f * ((MathF.Log((float)num * 0.04f + 2f, 2f) * 2.5f + 2.5f) * 0.01f * ((_settlementProsperityCache < 0f) ? settlement.Town.Prosperity : _settlementProsperityCache));
		float result = num2;
		switch (aftermathType)
		{
		case SiegeAftermathAction.SiegeAftermath.Devastate:
			result = 1.5f * num2;
			break;
		case SiegeAftermathAction.SiegeAftermath.ShowMercy:
			result = 0.5f * num2;
			break;
		}
		return result;
	}

	private int GetSiegeAftermathProjectsLoss(MobileParty attackerParty, SiegeAftermathAction.SiegeAftermath aftermathType)
	{
		int num = attackerParty.MemberRoster?.TotalHealthyCount ?? 0;
		if (attackerParty.Army != null)
		{
			num = attackerParty.Army.TotalHealthyMembers;
		}
		int num2 = MathF.Floor(MathF.Log((float)num * 0.02f + 2f, 2f));
		int result = 0;
		switch (aftermathType)
		{
		case SiegeAftermathAction.SiegeAftermath.Devastate:
			result = MathF.Ceiling(1.5f * (float)num2);
			break;
		case SiegeAftermathAction.SiegeAftermath.Pillage:
			result = num2;
			break;
		}
		return result;
	}

	private float GetSiegeAftermathLoyaltyPenalty(SiegeAftermathAction.SiegeAftermath aftermathType)
	{
		float result = 0f;
		switch (aftermathType)
		{
		case SiegeAftermathAction.SiegeAftermath.Devastate:
			result = -30f;
			break;
		case SiegeAftermathAction.SiegeAftermath.Pillage:
			result = -15f;
			break;
		}
		return result;
	}

	private float GetSiegeAftermathNotablePowerModifierForAftermath(SiegeAftermathAction.SiegeAftermath aftermathType)
	{
		float result = 0f;
		switch (aftermathType)
		{
		case SiegeAftermathAction.SiegeAftermath.Devastate:
			result = -0.5f;
			break;
		case SiegeAftermathAction.SiegeAftermath.Pillage:
			result = -0.25f;
			break;
		}
		return result;
	}

	private float GetSiegeAftermathPartyMoraleBonus(MobileParty attackerParty, Settlement settlement, SiegeAftermathAction.SiegeAftermath aftermathType)
	{
		int num = 0;
		switch (aftermathType)
		{
		case SiegeAftermathAction.SiegeAftermath.Devastate:
			num = 20;
			break;
		case SiegeAftermathAction.SiegeAftermath.Pillage:
			num = 10;
			break;
		case SiegeAftermathAction.SiegeAftermath.ShowMercy:
			if (attackerParty.MapFaction?.Culture != settlement.Culture)
			{
				num = -15;
			}
			break;
		}
		return num;
	}

	private int GetSiegeAftermathArmyGoldGain(MobileParty attackerParty, Settlement settlement, SiegeAftermathAction.SiegeAftermath aftermathType)
	{
		float num = -1f * GetSiegeAftermathProsperityPenalty(attackerParty, settlement, aftermathType);
		float f = 0f;
		if (aftermathType == SiegeAftermathAction.SiegeAftermath.Devastate || aftermathType == SiegeAftermathAction.SiegeAftermath.Pillage)
		{
			f = num * 15f;
		}
		return MathF.Floor(f);
	}

	private int GetSiegeAftermathPartyGoldGain(float totalArmyGoldGain, float partyContributionPercentage)
	{
		return MathF.Floor(totalArmyGoldGain * partyContributionPercentage / 100f);
	}

	private int GetSiegeAftermathRelationChangeWithLord(Hero hero, SiegeAftermathAction.SiegeAftermath aftermathType)
	{
		if (hero.GetTraitLevel(DefaultTraits.Mercy) > 0)
		{
			return GetSiegeAftermathRelationChangeWithMercifulLord(aftermathType);
		}
		if (hero.GetTraitLevel(DefaultTraits.Mercy) < 0)
		{
			return -1 * GetSiegeAftermathRelationChangeWithMercifulLord(aftermathType);
		}
		return 0;
	}

	private int GetSiegeAftermathRelationChangeWithMercifulLord(SiegeAftermathAction.SiegeAftermath aftermathType)
	{
		int result = 0;
		switch (aftermathType)
		{
		case SiegeAftermathAction.SiegeAftermath.Devastate:
			result = -10;
			break;
		case SiegeAftermathAction.SiegeAftermath.ShowMercy:
			result = 10;
			break;
		}
		return result;
	}

	private int GetSiegeAftermathRelationPenaltyWithSettlementOwner(SiegeAftermathAction.SiegeAftermath aftermathType)
	{
		int result = 0;
		switch (aftermathType)
		{
		case SiegeAftermathAction.SiegeAftermath.Devastate:
			result = -30;
			break;
		case SiegeAftermathAction.SiegeAftermath.Pillage:
			result = -15;
			break;
		}
		return result;
	}

	private float GetSiegeAftermathInfluenceCost(MobileParty attackerParty, Settlement settlement, SiegeAftermathAction.SiegeAftermath aftermathType)
	{
		float result = 0f;
		if (attackerParty.Army != null && aftermathType != SiegeAftermathAction.SiegeAftermath.Pillage)
		{
			int num = attackerParty.Army.Parties.Count((MobileParty t) => t != attackerParty && t.LeaderHero != null && t.LeaderHero.GetTraitLevel(DefaultTraits.Mercy) > 0);
			int num2 = attackerParty.Army.Parties.Count((MobileParty t) => t != attackerParty && t.LeaderHero != null && t.LeaderHero.GetTraitLevel(DefaultTraits.Mercy) < 0);
			switch (aftermathType)
			{
			case SiegeAftermathAction.SiegeAftermath.Devastate:
				result = (3000f + settlement.Town.Prosperity) / 400f * (float)num;
				break;
			case SiegeAftermathAction.SiegeAftermath.ShowMercy:
				if (attackerParty.MapFaction?.Culture != settlement.Culture)
				{
					result = (3000f + settlement.Town.Prosperity) / 400f * (float)num2;
				}
				break;
			}
		}
		return result;
	}

	private bool IsSiegeAftermathPossible(MobileParty attackerParty, Settlement settlement, SiegeAftermathAction.SiegeAftermath aftermathType)
	{
		bool flag = false;
		float siegeAftermathInfluenceCost = GetSiegeAftermathInfluenceCost(attackerParty, settlement, aftermathType);
		Clan actualClan = attackerParty.ActualClan;
		bool flag2 = actualClan != null && actualClan.Influence >= siegeAftermathInfluenceCost;
		bool flag3 = settlement.Culture == Hero.MainHero.MapFaction?.Culture;
		return aftermathType switch
		{
			SiegeAftermathAction.SiegeAftermath.Devastate => flag2 && !flag3, 
			SiegeAftermathAction.SiegeAftermath.Pillage => !flag3, 
			_ => flag2 || flag3, 
		};
	}

	private void OnSettlementOwnerChanged(Settlement settlement, bool openToClaim, Hero newOwner, Hero oldOwner, Hero capturerHero, ChangeOwnerOfSettlementAction.ChangeOwnerOfSettlementDetail detail)
	{
		if (settlement.IsFortification && detail == ChangeOwnerOfSettlementAction.ChangeOwnerOfSettlementDetail.BySiege && capturerHero != null && settlement.OwnerClan != null && settlement.OwnerClan != Clan.PlayerClan && !oldOwner.IsDead)
		{
			ChangeRelationAction.ApplyRelationChangeBetweenHeroes(oldOwner, capturerHero, -10, capturerHero == Hero.MainHero);
			if (capturerHero.MapFaction.Leader != capturerHero && settlement.OwnerClan.Leader != capturerHero.MapFaction.Leader)
			{
				ChangeRelationAction.ApplyRelationChangeBetweenHeroes(oldOwner, capturerHero.MapFaction.Leader, -6, capturerHero.MapFaction.Leader == Hero.MainHero);
			}
		}
	}

	public override void SyncData(IDataStore dataStore)
	{
		dataStore.SyncData("_besiegerParty", ref _besiegerParty);
		dataStore.SyncData("_prevSettlementOwnerClan", ref _prevSettlementOwnerClan);
		dataStore.SyncData("_playerEncounterAftermath", ref _playerEncounterAftermath);
		dataStore.SyncData("_siegeEventPartyContributions", ref _siegeEventPartyContributions);
		dataStore.SyncData("_wasPlayerArmyMember", ref _wasPlayerArmyMember);
		dataStore.SyncData("_playerEncounterAftermathDamagedBuildings", ref _playerEncounterAftermathDamagedBuildings);
	}
}
