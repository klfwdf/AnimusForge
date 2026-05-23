using System;
using System.Collections.Generic;
using System.Linq;
using Helpers;
using TaleWorlds.CampaignSystem.CharacterDevelopment;
using TaleWorlds.CampaignSystem.ComponentInterfaces;
using TaleWorlds.CampaignSystem.Encounters;
using TaleWorlds.CampaignSystem.MapEvents;
using TaleWorlds.CampaignSystem.Naval;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.CampaignSystem.Roster;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.Core;
using TaleWorlds.Library;

namespace TaleWorlds.CampaignSystem.GameComponents;

public class DefaultBattleRewardModel : BattleRewardModel
{
	private static readonly int[] _indices = new int[12];

	private const float DestroyHideoutBannerLootChance = 0.1f;

	private const float CaptureSettlementBannerLootChance = 0.5f;

	private const float DefeatRegularHeroBannerLootChance = 0.5f;

	private const float DefeatClanLeaderBannerLootChance = 0.25f;

	private const float DefeatKingdomRulerBannerLootChance = 0.1f;

	private const float MainPartyMemberScatterChance = 0.1f;

	public override int GetPlayerGainedRelationAmount(MapEvent mapEvent, Hero hero)
	{
		float playerBattleContributionRate = mapEvent.GetPlayerBattleContributionRate();
		float num = (mapEvent.StrengthOfSide[(int)PartyBase.MainParty.Side] - PlayerEncounter.Current.PlayerPartyInitialStrength) / (mapEvent.StrengthOfSide[(int)PartyBase.MainParty.OpponentSide] + 1f);
		float num2 = ((num < 1f) ? (1f + (1f - num)) : ((num < 3f) ? (0.5f * (3f - num)) : 0f));
		float renownValue = (mapEvent.AttackerSide.IsMainPartyAmongParties() ? mapEvent.AttackerSide : mapEvent.DefenderSide).RenownValue;
		ExplainedNumber explainedNumber = new ExplainedNumber(0.75f + TaleWorlds.Library.MathF.Pow(playerBattleContributionRate * 1.3f * (num2 + renownValue), 0.67f));
		if (Hero.MainHero.GetPerkValue(DefaultPerks.Charm.Camaraderie))
		{
			explainedNumber.AddFactor(DefaultPerks.Charm.Camaraderie.PrimaryBonus, DefaultPerks.Charm.Camaraderie.Name);
		}
		return (int)explainedNumber.ResultNumber;
	}

	public override ExplainedNumber CalculateRenownGain(PartyBase winnerParty, float renownValueOfBattleForWinnerSide, float contributionShareOfWinnerParty, float renownMultiplierForWinnerSide, bool includeDescriptions)
	{
		ExplainedNumber stat = new ExplainedNumber(contributionShareOfWinnerParty * renownValueOfBattleForWinnerSide * renownMultiplierForWinnerSide, includeDescriptions);
		if (winnerParty.IsMobile)
		{
			if (winnerParty.MobileParty.HasPerk(DefaultPerks.Throwing.LongReach, checkSecondaryRole: true))
			{
				PerkHelper.AddPerkBonusForParty(DefaultPerks.Throwing.LongReach, winnerParty.MobileParty, isPrimaryBonus: false, ref stat);
			}
			if (winnerParty.MobileParty.HasPerk(DefaultPerks.Charm.PublicSpeaker))
			{
				stat.AddFactor(DefaultPerks.Charm.PublicSpeaker.PrimaryBonus, DefaultPerks.Charm.PublicSpeaker.Name);
			}
			if (winnerParty.LeaderHero != null)
			{
				PerkHelper.AddPerkBonusForCharacter(DefaultPerks.Leadership.FamousCommander, winnerParty.LeaderHero.CharacterObject, isPrimaryBonus: true, ref stat, winnerParty.MobileParty.IsCurrentlyAtSea);
			}
			if (PartyBaseHelper.HasFeat(winnerParty, DefaultCulturalFeats.VlandianRenownMercenaryFeat))
			{
				stat.AddFactor(DefaultCulturalFeats.VlandianRenownMercenaryFeat.EffectBonus, GameTexts.FindText("str_culture"));
			}
		}
		return stat;
	}

	public override ExplainedNumber CalculateInfluenceGain(PartyBase winnerParty, float influenceValueOfBattleForWinnerSide, float contributionShareOfWinnerParty, float influenceMultiplierForWinnerSide, bool includeDescriptions)
	{
		ExplainedNumber bonuses = new ExplainedNumber(0f, includeDescriptions: false, null);
		if (winnerParty.MapFaction.IsKingdomFaction)
		{
			bonuses = new ExplainedNumber(influenceValueOfBattleForWinnerSide * contributionShareOfWinnerParty * influenceMultiplierForWinnerSide, includeDescriptions);
			if (winnerParty.LeaderHero != null)
			{
				PerkHelper.AddPerkBonusForCharacter(DefaultPerks.Charm.Warlord, winnerParty.LeaderHero.CharacterObject, isPrimaryBonus: true, ref bonuses, winnerParty.MobileParty.IsCurrentlyAtSea);
			}
		}
		return bonuses;
	}

	public override ExplainedNumber CalculateMoraleGainVictory(PartyBase winnerParty, float renownValueOfBattleForWinnerSide, float contributionShareOfWinnerParty, bool includeDescriptions)
	{
		ExplainedNumber stat = new ExplainedNumber(0.5f + renownValueOfBattleForWinnerSide * contributionShareOfWinnerParty * 0.5f, includeDescriptions);
		if (winnerParty.IsMobile)
		{
			if (winnerParty.MobileParty.HasPerk(DefaultPerks.Throwing.LongReach, checkSecondaryRole: true))
			{
				PerkHelper.AddPerkBonusForParty(DefaultPerks.Throwing.LongReach, winnerParty.MobileParty, isPrimaryBonus: false, ref stat);
			}
			if (winnerParty.MobileParty.HasPerk(DefaultPerks.Leadership.CitizenMilitia, checkSecondaryRole: true))
			{
				PerkHelper.AddPerkBonusForParty(DefaultPerks.Leadership.CitizenMilitia, winnerParty.MobileParty, isPrimaryBonus: false, ref stat, winnerParty.MobileParty.IsCurrentlyAtSea);
			}
		}
		return stat;
	}

	public override int CalculateGoldLossAfterDefeat(Hero partyLeaderHero)
	{
		return (int)Math.Min((float)partyLeaderHero.Gold * 0.05f, 10000f);
	}

	public override EquipmentElement GetLootedItemFromTroop(CharacterObject character, float targetValue)
	{
		bool num = MobileParty.MainParty.HasPerk(DefaultPerks.Engineering.Metallurgy);
		EquipmentElement result = GetRandomItem(character.BattleEquipments.GetRandomElementInefficiently(), targetValue);
		if (num && result.ItemModifier != null && result.ItemModifier.PriceMultiplier < 1f && MBRandom.RandomFloat < DefaultPerks.Engineering.Metallurgy.PrimaryBonus)
		{
			result = new EquipmentElement(result.Item);
		}
		return result;
	}

	private static EquipmentElement GetRandomItem(Equipment equipment, float targetValue = 0f)
	{
		int num = 0;
		for (int i = 0; i < 12; i++)
		{
			if (equipment[i].Item != null && !equipment[i].Item.NotMerchandise)
			{
				_indices[num] = i;
				num++;
			}
		}
		for (int j = 0; j < num - 1; j++)
		{
			int num2 = j;
			int value = equipment[_indices[j]].Item.Value;
			for (int k = j + 1; k < num; k++)
			{
				if (equipment[_indices[k]].Item.Value > value)
				{
					num2 = k;
					value = equipment[_indices[k]].Item.Value;
				}
			}
			int num3 = _indices[j];
			_indices[j] = _indices[num2];
			_indices[num2] = num3;
		}
		if (num > 0)
		{
			for (int l = 0; l < num; l++)
			{
				int index = _indices[l];
				EquipmentElement result = equipment[index];
				if (result.Item == null || equipment[index].Item.NotMerchandise)
				{
					continue;
				}
				float b = (float)result.Item.Value + 0.1f;
				float num4 = 0.325f * (targetValue / (TaleWorlds.Library.MathF.Max(targetValue, b) * (float)(num - l)));
				if (MBRandom.RandomFloat < num4)
				{
					ItemModifier itemModifier = result.Item.ItemComponent?.ItemModifierGroup?.GetRandomItemModifierLootScoreBased();
					if (itemModifier != null)
					{
						result = new EquipmentElement(result.Item, itemModifier);
					}
					return result;
				}
			}
		}
		return default(EquipmentElement);
	}

	public override float GetExpectedLootedItemValueFromCasualty(Hero winnerPartyLeaderHero, CharacterObject casualtyCharacter)
	{
		float num = 7.25f * (float)(casualtyCharacter.Level * casualtyCharacter.Level);
		if (winnerPartyLeaderHero == Hero.MainHero)
		{
			if (PlayerEncounter.Current != null && PlayerEncounter.Current.ForceHideoutSendTroops)
			{
				return 0f;
			}
			return num * MBRandom.RandomFloatRanged(0.85f, 1.15f);
		}
		return num;
	}

	public override float GetAITradePenalty()
	{
		return 1f / 55f;
	}

	public override float GetMainPartyMemberScatterChance()
	{
		return 0.1f;
	}

	public override int CalculatePlunderedGoldAmountFromDefeatedParty(PartyBase defeatedParty)
	{
		int result = 0;
		if (defeatedParty.LeaderHero != null)
		{
			result = Campaign.Current.Models.BattleRewardModel.CalculateGoldLossAfterDefeat(defeatedParty.LeaderHero);
		}
		else if (defeatedParty.IsMobile && defeatedParty.MobileParty.IsPartyTradeActive)
		{
			MobileParty mobileParty = defeatedParty.MobileParty;
			result = (int)((float)mobileParty.PartyTradeGold * (mobileParty.IsBandit ? 0.5f : 0.1f));
		}
		return result;
	}

	public override MBReadOnlyList<KeyValuePair<MapEventParty, float>> GetLootGoldChances(MBReadOnlyList<MapEventParty> winnerParties)
	{
		MBList<KeyValuePair<MapEventParty, float>> mBList = new MBList<KeyValuePair<MapEventParty, float>>();
		float num = 0f;
		foreach (MapEventParty winnerParty in winnerParties)
		{
			if (winnerParty.ContributionToBattle > 0 && (!winnerParty.Party.IsMobile || !winnerParty.Party.MobileParty.IsPatrolParty))
			{
				mBList.Add(new KeyValuePair<MapEventParty, float>(winnerParty, winnerParty.ContributionToBattle));
				num += (float)winnerParty.ContributionToBattle;
			}
		}
		for (int i = 0; i < mBList.Count; i++)
		{
			mBList[i] = new KeyValuePair<MapEventParty, float>(mBList[i].Key, mBList[i].Value / num);
		}
		return mBList;
	}

	public override void GetCaptureMemberChancesForWinnerParties(MapEvent endedMapEvent, MBReadOnlyList<MapEventParty> winnerParties, out MBList<KeyValuePair<MapEventParty, float>> woundedMemberChances, out MBList<KeyValuePair<MapEventParty, float>> healthyMemberChances)
	{
		woundedMemberChances = new MBList<KeyValuePair<MapEventParty, float>>();
		healthyMemberChances = new MBList<KeyValuePair<MapEventParty, float>>();
		float num = 0f;
		float num2 = 0.25f;
		if (endedMapEvent.GetMapEventSide(endedMapEvent.DefeatedSide).IsSurrendered)
		{
			num2 = 1f;
		}
		foreach (MapEventParty winnerParty in winnerParties)
		{
			MobileParty mobileParty = winnerParty.Party.MobileParty;
			if (winnerParty.ContributionToBattle > 0 && winnerParty.Party.MemberRoster.Count > 0 && (mobileParty == null || (!mobileParty.IsVillager && !mobileParty.IsCaravan && !mobileParty.IsPatrolParty && ((!mobileParty.IsGarrison && !mobileParty.IsMilitia) || !mobileParty.CurrentSettlement.IsVillage))))
			{
				healthyMemberChances.Add(new KeyValuePair<MapEventParty, float>(winnerParty, winnerParty.ContributionToBattle));
				num += (float)winnerParty.ContributionToBattle;
			}
		}
		for (int i = 0; i < healthyMemberChances.Count; i++)
		{
			woundedMemberChances.Add(new KeyValuePair<MapEventParty, float>(healthyMemberChances[i].Key, healthyMemberChances[i].Value / num * 1f));
			healthyMemberChances[i] = new KeyValuePair<MapEventParty, float>(healthyMemberChances[i].Key, healthyMemberChances[i].Value / num * num2);
		}
	}

	public override MBReadOnlyList<KeyValuePair<MapEventParty, float>> GetLootPrisonerChances(MBReadOnlyList<MapEventParty> winnerParties, TroopRosterElement prisonerElement)
	{
		MBList<KeyValuePair<MapEventParty, float>> mBList = new MBList<KeyValuePair<MapEventParty, float>>();
		CharacterObject character = prisonerElement.Character;
		if (character.HeroObject == null || !character.HeroObject.IsReleased)
		{
			float num = 0f;
			Occupation occupation = character.Occupation;
			foreach (MapEventParty winnerParty in winnerParties)
			{
				MobileParty mobileParty = winnerParty.Party.MobileParty;
				if (winnerParty.ContributionToBattle > 0 && winnerParty.Party.MemberRoster.Count > 0 && ((mobileParty == null && occupation != Occupation.Bandit) || (mobileParty != null && !mobileParty.IsVillager && !mobileParty.IsCaravan && !mobileParty.IsMilitia && !mobileParty.IsPatrolParty && (!mobileParty.IsBandit || occupation == Occupation.Bandit) && (!mobileParty.IsGarrison || occupation != Occupation.Bandit))))
				{
					mBList.Add(new KeyValuePair<MapEventParty, float>(winnerParty, winnerParty.ContributionToBattle));
					num += (float)winnerParty.ContributionToBattle;
				}
			}
			for (int i = 0; i < mBList.Count; i++)
			{
				mBList[i] = new KeyValuePair<MapEventParty, float>(mBList[i].Key, mBList[i].Value / num * 1f);
			}
		}
		return mBList;
	}

	public override MBList<KeyValuePair<MapEventParty, float>> GetLootItemChancesForWinnerParties(MBReadOnlyList<MapEventParty> winnerParties, PartyBase defeatedParty)
	{
		MBList<KeyValuePair<MapEventParty, float>> mBList = new MBList<KeyValuePair<MapEventParty, float>>();
		if (PlayerEncounter.Current != null && PlayerEncounter.Current.ForceHideoutSendTroops && winnerParties.Any((MapEventParty x) => x.Party == PartyBase.MainParty))
		{
			return mBList;
		}
		if (!defeatedParty.IsSettlement)
		{
			MBList<KeyValuePair<MapEventParty, float>> mBList2 = new MBList<KeyValuePair<MapEventParty, float>>();
			float num = 0f;
			foreach (MapEventParty winnerParty in winnerParties)
			{
				MobileParty mobileParty = winnerParty.Party.MobileParty;
				PartyBase party = winnerParty.Party;
				if (winnerParty.ContributionToBattle > 0 && winnerParty.Party.MemberRoster.Count > 0 && (mobileParty == null || (!mobileParty.IsGarrison && !mobileParty.IsMilitia)))
				{
					mBList2.Add(new KeyValuePair<MapEventParty, float>(winnerParty, winnerParty.ContributionToBattle));
					num += (float)winnerParty.ContributionToBattle;
					ExplainedNumber explainedNumber = new ExplainedNumber(1f);
					SkillHelper.AddSkillBonusForParty(DefaultSkillEffects.RogueryLootBonus, party.MobileParty, ref explainedNumber);
					if (party.LeaderHero != null && party.LeaderHero.GetPerkValue(DefaultPerks.Roguery.RogueExtraordinaire))
					{
						PerkHelper.AddEpicPerkBonusForCharacter(DefaultPerks.Roguery.RogueExtraordinaire, party.LeaderHero.CharacterObject, DefaultSkills.Roguery, applyPrimaryBonus: true, ref explainedNumber, Campaign.Current.Models.CharacterDevelopmentModel.MinSkillRequiredForEpicPerkBonus);
					}
					float num2 = explainedNumber.ResultNumber;
					if (party.MobileParty.HasPerk(DefaultPerks.Roguery.KnowHow) && (defeatedParty.MobileParty.IsCaravan || defeatedParty.MobileParty.IsVillager))
					{
						num2 *= 1f + DefaultPerks.Roguery.KnowHow.PrimaryBonus;
					}
					mBList.Add(new KeyValuePair<MapEventParty, float>(winnerParty, num2));
				}
			}
			for (int num3 = 0; num3 < mBList2.Count; num3++)
			{
				mBList[num3] = new KeyValuePair<MapEventParty, float>(mBList2[num3].Key, mBList2[num3].Value / num * mBList[num3].Value * 0.5f);
			}
		}
		return mBList;
	}

	public override MBReadOnlyList<KeyValuePair<MapEventParty, float>> GetLootCasualtyChances(MBReadOnlyList<MapEventParty> winnerParties, PartyBase defeatedParty)
	{
		MBList<KeyValuePair<MapEventParty, float>> mBList = new MBList<KeyValuePair<MapEventParty, float>>();
		if (!defeatedParty.IsSettlement || !defeatedParty.Settlement.IsTown)
		{
			float num = 0f;
			foreach (MapEventParty winnerParty in winnerParties)
			{
				MobileParty mobileParty = winnerParty.Party.MobileParty;
				if (winnerParty.ContributionToBattle > 0 && winnerParty.Party.MemberRoster.Count > 0 && (mobileParty == null || (!mobileParty.IsGarrison && !mobileParty.IsMilitia)))
				{
					mBList.Add(new KeyValuePair<MapEventParty, float>(winnerParty, winnerParty.ContributionToBattle));
					num += (float)winnerParty.ContributionToBattle;
				}
			}
			for (int i = 0; i < mBList.Count; i++)
			{
				mBList[i] = new KeyValuePair<MapEventParty, float>(mBList[i].Key, mBList[i].Value / num * 1f);
			}
		}
		return mBList;
	}

	public override float CalculateShipDamageAfterDefeat(Ship ship)
	{
		return 0f;
	}

	public override MBReadOnlyList<KeyValuePair<Ship, MapEventParty>> DistributeDefeatedPartyShipsAmongWinners(MapEvent mapEvent, MBReadOnlyList<Ship> shipsToLoot, MBReadOnlyList<MapEventParty> winnerParties)
	{
		return new MBReadOnlyList<KeyValuePair<Ship, MapEventParty>>();
	}

	public override float GetBannerLootChanceFromDefeatedHero(Hero defeatedHero)
	{
		if (defeatedHero.Clan?.Kingdom?.RulingClan.Leader == defeatedHero)
		{
			return 0.1f;
		}
		if (defeatedHero.Clan?.Leader == defeatedHero)
		{
			return 0.25f;
		}
		return 0.5f;
	}

	public override ItemObject GetBannerRewardForWinningMapEvent(MapEvent mapEvent)
	{
		if (mapEvent.IsHideoutBattle || (mapEvent.AttackerSide.MissionSide == mapEvent.PlayerSide && mapEvent.IsSiegeAssault))
		{
			bool isHideoutBattle = mapEvent.IsHideoutBattle;
			Settlement mapEventSettlement = mapEvent.MapEventSettlement;
			float num = (isHideoutBattle ? 0.1f : 0.5f);
			if (MBRandom.RandomFloat <= num)
			{
				MBList<ItemObject> mBList = Campaign.Current.Models.BannerItemModel.GetPossibleRewardBannerItems().ToMBList();
				if (mBList.Count > 0)
				{
					mBList.Shuffle();
					int num2 = (isHideoutBattle ? 1 : mapEventSettlement.Town.GetWallLevel());
					foreach (ItemObject item in mBList)
					{
						if (((BannerComponent)item.ItemComponent).BannerLevel == num2 && (item.Culture == null || item.Culture.StringId == "neutral_culture" || (!isHideoutBattle && item.Culture == mapEventSettlement.Culture)))
						{
							return item;
						}
					}
				}
			}
		}
		return null;
	}

	public override float GetSunkenShipMoraleEffect(PartyBase shipOwner, Ship ship)
	{
		return 0f;
	}

	public override float CalculateMoraleChangeOnRoundVictory(PartyBase party, MapEventSide partySide, BattleSideEnum roundWinner)
	{
		float result = 0f;
		if (partySide.MissionSide != roundWinner && roundWinner != BattleSideEnum.None)
		{
			result = ((partySide.MapEvent.RetreatingSide == BattleSideEnum.None) ? (-3f) : (-1f));
		}
		return result;
	}

	public override float GetShipSiegeEngineHitMoraleEffect(Ship ship, SiegeEngineType siegeEngineType)
	{
		return 0f;
	}

	public override Figurehead GetFigureheadLoot(MBReadOnlyList<MapEventParty> defeatedParties, PartyBase defeatedSideLeaderParty)
	{
		return null;
	}

	public override MBReadOnlyList<MapEventParty> GetWinnerPartiesThatCanPlunderGoldFromShips(MBReadOnlyList<MapEventParty> winnerParties)
	{
		return new MBReadOnlyList<MapEventParty>();
	}

	public override bool CanTroopBeTakenPrisoner(CharacterObject troop)
	{
		return true;
	}
}
