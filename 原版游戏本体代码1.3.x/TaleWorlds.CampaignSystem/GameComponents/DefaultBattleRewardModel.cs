using System;
using System.Collections.Generic;
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

namespace TaleWorlds.CampaignSystem.GameComponents
{
	// Token: 0x020000F5 RID: 245
	public class DefaultBattleRewardModel : BattleRewardModel
	{
		// Token: 0x06001648 RID: 5704 RVA: 0x00065B44 File Offset: 0x00063D44
		public override int GetPlayerGainedRelationAmount(MapEvent mapEvent, Hero hero)
		{
			MapEventSide mapEventSide = (mapEvent.AttackerSide.IsMainPartyAmongParties() ? mapEvent.AttackerSide : mapEvent.DefenderSide);
			float playerPartyContributionRate = mapEventSide.GetPlayerPartyContributionRate();
			float num = (mapEvent.StrengthOfSide[(int)PartyBase.MainParty.Side] - PlayerEncounter.Current.PlayerPartyInitialStrength) / (mapEvent.StrengthOfSide[(int)PartyBase.MainParty.OpponentSide] + 1f);
			float num2 = ((num < 1f) ? (1f + (1f - num)) : ((num < 3f) ? (0.5f * (3f - num)) : 0f));
			float renownValue = mapEvent.GetRenownValue((mapEventSide == mapEvent.AttackerSide) ? BattleSideEnum.Attacker : BattleSideEnum.Defender);
			ExplainedNumber explainedNumber = new ExplainedNumber(0.75f + MathF.Pow(playerPartyContributionRate * 1.3f * (num2 + renownValue), 0.67f), false, null);
			if (Hero.MainHero.GetPerkValue(DefaultPerks.Charm.Camaraderie))
			{
				explainedNumber.AddFactor(DefaultPerks.Charm.Camaraderie.PrimaryBonus, DefaultPerks.Charm.Camaraderie.Name);
			}
			return (int)explainedNumber.ResultNumber;
		}

		// Token: 0x06001649 RID: 5705 RVA: 0x00065C4C File Offset: 0x00063E4C
		public override ExplainedNumber CalculateRenownGain(PartyBase party, float renownValueOfBattle, float contributionShare)
		{
			ExplainedNumber result = new ExplainedNumber(renownValueOfBattle * contributionShare, true, null);
			if (party.IsMobile)
			{
				if (party.MobileParty.HasPerk(DefaultPerks.Throwing.LongReach, true))
				{
					PerkHelper.AddPerkBonusForParty(DefaultPerks.Throwing.LongReach, party.MobileParty, false, ref result, false);
				}
				if (party.MobileParty.HasPerk(DefaultPerks.Charm.PublicSpeaker, false))
				{
					result.AddFactor(DefaultPerks.Charm.PublicSpeaker.PrimaryBonus, DefaultPerks.Charm.PublicSpeaker.Name);
				}
				if (party.LeaderHero != null)
				{
					PerkHelper.AddPerkBonusForCharacter(DefaultPerks.Leadership.FamousCommander, party.LeaderHero.CharacterObject, true, ref result, party.MobileParty.IsCurrentlyAtSea);
				}
				if (PartyBaseHelper.HasFeat(party, DefaultCulturalFeats.VlandianRenownMercenaryFeat))
				{
					result.AddFactor(DefaultCulturalFeats.VlandianRenownMercenaryFeat.EffectBonus, GameTexts.FindText("str_culture", null));
				}
			}
			return result;
		}

		// Token: 0x0600164A RID: 5706 RVA: 0x00065D1C File Offset: 0x00063F1C
		public override ExplainedNumber CalculateInfluenceGain(PartyBase party, float influenceValueOfBattle, float contributionShare)
		{
			ExplainedNumber result = new ExplainedNumber(party.MapFaction.IsKingdomFaction ? (influenceValueOfBattle * contributionShare) : 0f, true, null);
			if (party.LeaderHero != null)
			{
				PerkHelper.AddPerkBonusForCharacter(DefaultPerks.Charm.Warlord, party.LeaderHero.CharacterObject, true, ref result, party.MobileParty.IsCurrentlyAtSea);
			}
			return result;
		}

		// Token: 0x0600164B RID: 5707 RVA: 0x00065D78 File Offset: 0x00063F78
		public override ExplainedNumber CalculateMoraleGainVictory(PartyBase party, float renownValueOfBattle, float contributionShare, MapEvent battle)
		{
			ExplainedNumber result = new ExplainedNumber(0.5f + renownValueOfBattle * contributionShare * 0.5f, true, null);
			if (party.IsMobile && party.MobileParty.HasPerk(DefaultPerks.Throwing.LongReach, true))
			{
				PerkHelper.AddPerkBonusForParty(DefaultPerks.Throwing.LongReach, party.MobileParty, false, ref result, false);
			}
			if (party.IsMobile && party.MobileParty.HasPerk(DefaultPerks.Leadership.CitizenMilitia, true))
			{
				PerkHelper.AddPerkBonusForParty(DefaultPerks.Leadership.CitizenMilitia, party.MobileParty, false, ref result, party.MobileParty.IsCurrentlyAtSea);
			}
			return result;
		}

		// Token: 0x0600164C RID: 5708 RVA: 0x00065E06 File Offset: 0x00064006
		public override int CalculateGoldLossAfterDefeat(Hero partyLeaderHero)
		{
			return (int)Math.Min((float)partyLeaderHero.Gold * 0.05f, 10000f);
		}

		// Token: 0x0600164D RID: 5709 RVA: 0x00065E20 File Offset: 0x00064020
		public override EquipmentElement GetLootedItemFromTroop(CharacterObject character, float targetValue)
		{
			bool flag = MobileParty.MainParty.HasPerk(DefaultPerks.Engineering.Metallurgy, false);
			EquipmentElement randomItem = DefaultBattleRewardModel.GetRandomItem(character.BattleEquipments.GetRandomElementInefficiently<Equipment>(), targetValue);
			if (flag && randomItem.ItemModifier != null && randomItem.ItemModifier.PriceMultiplier < 1f && MBRandom.RandomFloat < DefaultPerks.Engineering.Metallurgy.PrimaryBonus)
			{
				randomItem = new EquipmentElement(randomItem.Item, null, null, false);
			}
			return randomItem;
		}

		// Token: 0x0600164E RID: 5710 RVA: 0x00065E90 File Offset: 0x00064090
		private static EquipmentElement GetRandomItem(Equipment equipment, float targetValue = 0f)
		{
			int num = 0;
			for (int i = 0; i < 12; i++)
			{
				if (equipment[i].Item != null && !equipment[i].Item.NotMerchandise)
				{
					DefaultBattleRewardModel._indices[num] = i;
					num++;
				}
			}
			for (int j = 0; j < num - 1; j++)
			{
				int num2 = j;
				int value = equipment[DefaultBattleRewardModel._indices[j]].Item.Value;
				for (int k = j + 1; k < num; k++)
				{
					if (equipment[DefaultBattleRewardModel._indices[k]].Item.Value > value)
					{
						num2 = k;
						value = equipment[DefaultBattleRewardModel._indices[k]].Item.Value;
					}
				}
				int num3 = DefaultBattleRewardModel._indices[j];
				DefaultBattleRewardModel._indices[j] = DefaultBattleRewardModel._indices[num2];
				DefaultBattleRewardModel._indices[num2] = num3;
			}
			if (num > 0)
			{
				for (int l = 0; l < num; l++)
				{
					int index = DefaultBattleRewardModel._indices[l];
					EquipmentElement result = equipment[index];
					if (result.Item != null && !equipment[index].Item.NotMerchandise)
					{
						float b = (float)result.Item.Value + 0.1f;
						float num4 = 0.325f * (targetValue / (MathF.Max(targetValue, b) * (float)(num - l)));
						if (MBRandom.RandomFloat < num4)
						{
							ItemComponent itemComponent = result.Item.ItemComponent;
							ItemModifier itemModifier;
							if (itemComponent == null)
							{
								itemModifier = null;
							}
							else
							{
								ItemModifierGroup itemModifierGroup = itemComponent.ItemModifierGroup;
								itemModifier = ((itemModifierGroup != null) ? itemModifierGroup.GetRandomItemModifierLootScoreBased() : null);
							}
							ItemModifier itemModifier2 = itemModifier;
							if (itemModifier2 != null)
							{
								result = new EquipmentElement(result.Item, itemModifier2, null, false);
							}
							return result;
						}
					}
				}
			}
			return default(EquipmentElement);
		}

		// Token: 0x0600164F RID: 5711 RVA: 0x00066058 File Offset: 0x00064258
		public override float GetExpectedLootedItemValueFromCasualty(Hero winnerPartyLeaderHero, CharacterObject casualtyCharacter)
		{
			float num = 7.25f * (float)(casualtyCharacter.Level * casualtyCharacter.Level);
			if (winnerPartyLeaderHero == Hero.MainHero)
			{
				return num * MBRandom.RandomFloatRanged(0.85f, 1.15f);
			}
			return num;
		}

		// Token: 0x06001650 RID: 5712 RVA: 0x00066095 File Offset: 0x00064295
		public override float GetAITradePenalty()
		{
			return 0.018181818f;
		}

		// Token: 0x06001651 RID: 5713 RVA: 0x0006609C File Offset: 0x0006429C
		public override float GetMainPartyMemberScatterChance()
		{
			return 0.1f;
		}

		// Token: 0x06001652 RID: 5714 RVA: 0x000660A4 File Offset: 0x000642A4
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

		// Token: 0x06001653 RID: 5715 RVA: 0x00066114 File Offset: 0x00064314
		public override MBReadOnlyList<KeyValuePair<MapEventParty, float>> GetLootGoldChances(MBReadOnlyList<MapEventParty> winnerParties)
		{
			MBList<KeyValuePair<MapEventParty, float>> mblist = new MBList<KeyValuePair<MapEventParty, float>>();
			float num = 0f;
			foreach (MapEventParty mapEventParty in winnerParties)
			{
				if (mapEventParty.ContributionToBattle > 0 && (!mapEventParty.Party.IsMobile || !mapEventParty.Party.MobileParty.IsPatrolParty))
				{
					mblist.Add(new KeyValuePair<MapEventParty, float>(mapEventParty, (float)mapEventParty.ContributionToBattle));
					num += (float)mapEventParty.ContributionToBattle;
				}
			}
			for (int i = 0; i < mblist.Count; i++)
			{
				mblist[i] = new KeyValuePair<MapEventParty, float>(mblist[i].Key, mblist[i].Value / num);
			}
			return mblist;
		}

		// Token: 0x06001654 RID: 5716 RVA: 0x000661F4 File Offset: 0x000643F4
		public override MBReadOnlyList<KeyValuePair<MapEventParty, float>> GetLootMemberChancesForWinnerParties(MBReadOnlyList<MapEventParty> winnerParties)
		{
			MBList<KeyValuePair<MapEventParty, float>> mblist = new MBList<KeyValuePair<MapEventParty, float>>();
			float num = 0f;
			foreach (MapEventParty mapEventParty in winnerParties)
			{
				MobileParty mobileParty = mapEventParty.Party.MobileParty;
				if (mapEventParty.ContributionToBattle > 0 && mapEventParty.Party.MemberRoster.Count > 0 && (mobileParty == null || (!mobileParty.IsVillager && !mobileParty.IsCaravan && !mobileParty.IsPatrolParty && ((!mobileParty.IsGarrison && !mobileParty.IsMilitia) || !mobileParty.CurrentSettlement.IsVillage))))
				{
					mblist.Add(new KeyValuePair<MapEventParty, float>(mapEventParty, (float)mapEventParty.ContributionToBattle));
					num += (float)mapEventParty.ContributionToBattle;
				}
			}
			for (int i = 0; i < mblist.Count; i++)
			{
				mblist[i] = new KeyValuePair<MapEventParty, float>(mblist[i].Key, mblist[i].Value / num * 0.75f);
			}
			return mblist;
		}

		// Token: 0x06001655 RID: 5717 RVA: 0x00066320 File Offset: 0x00064520
		public override MBReadOnlyList<KeyValuePair<MapEventParty, float>> GetLootPrisonerChances(MBReadOnlyList<MapEventParty> winnerParties, TroopRosterElement prisonerElement)
		{
			MBList<KeyValuePair<MapEventParty, float>> mblist = new MBList<KeyValuePair<MapEventParty, float>>();
			CharacterObject character = prisonerElement.Character;
			if (character.HeroObject == null || !character.HeroObject.IsReleased)
			{
				float num = 0f;
				Occupation occupation = character.Occupation;
				foreach (MapEventParty mapEventParty in winnerParties)
				{
					MobileParty mobileParty = mapEventParty.Party.MobileParty;
					if (mapEventParty.ContributionToBattle > 0 && mapEventParty.Party.MemberRoster.Count > 0 && ((mobileParty == null && occupation != Occupation.Bandit) || (mobileParty != null && !mobileParty.IsVillager && !mobileParty.IsCaravan && !mobileParty.IsMilitia && !mobileParty.IsPatrolParty && (!mobileParty.IsBandit || occupation == Occupation.Bandit) && (!mobileParty.IsGarrison || occupation != Occupation.Bandit))))
					{
						mblist.Add(new KeyValuePair<MapEventParty, float>(mapEventParty, (float)mapEventParty.ContributionToBattle));
						num += (float)mapEventParty.ContributionToBattle;
					}
				}
				for (int i = 0; i < mblist.Count; i++)
				{
					mblist[i] = new KeyValuePair<MapEventParty, float>(mblist[i].Key, mblist[i].Value / num * 0.55f);
				}
			}
			return mblist;
		}

		// Token: 0x06001656 RID: 5718 RVA: 0x0006648C File Offset: 0x0006468C
		public override MBList<KeyValuePair<MapEventParty, float>> GetLootItemChancesForWinnerParties(MBReadOnlyList<MapEventParty> winnerParties, PartyBase defeatedParty)
		{
			MBList<KeyValuePair<MapEventParty, float>> mblist = new MBList<KeyValuePair<MapEventParty, float>>();
			if (!defeatedParty.IsSettlement)
			{
				MBList<KeyValuePair<MapEventParty, float>> mblist2 = new MBList<KeyValuePair<MapEventParty, float>>();
				float num = 0f;
				foreach (MapEventParty mapEventParty in winnerParties)
				{
					MobileParty mobileParty = mapEventParty.Party.MobileParty;
					PartyBase party = mapEventParty.Party;
					if (mapEventParty.ContributionToBattle > 0 && mapEventParty.Party.MemberRoster.Count > 0 && (mobileParty == null || (!mobileParty.IsGarrison && !mobileParty.IsMilitia)))
					{
						mblist2.Add(new KeyValuePair<MapEventParty, float>(mapEventParty, (float)mapEventParty.ContributionToBattle));
						num += (float)mapEventParty.ContributionToBattle;
						ExplainedNumber explainedNumber = new ExplainedNumber(1f, false, null);
						SkillHelper.AddSkillBonusForParty(DefaultSkillEffects.RogueryLootBonus, party.MobileParty, ref explainedNumber);
						if (party.LeaderHero != null && party.LeaderHero.GetPerkValue(DefaultPerks.Roguery.RogueExtraordinaire))
						{
							PerkHelper.AddEpicPerkBonusForCharacter(DefaultPerks.Roguery.RogueExtraordinaire, party.LeaderHero.CharacterObject, DefaultSkills.Roguery, true, ref explainedNumber, Campaign.Current.Models.CharacterDevelopmentModel.MinSkillRequiredForEpicPerkBonus, false);
						}
						float num2 = explainedNumber.ResultNumber;
						if (party.MobileParty.HasPerk(DefaultPerks.Roguery.KnowHow, false) && (defeatedParty.MobileParty.IsCaravan || defeatedParty.MobileParty.IsVillager))
						{
							num2 *= 1f + DefaultPerks.Roguery.KnowHow.PrimaryBonus;
						}
						mblist.Add(new KeyValuePair<MapEventParty, float>(mapEventParty, num2));
					}
				}
				for (int i = 0; i < mblist2.Count; i++)
				{
					mblist[i] = new KeyValuePair<MapEventParty, float>(mblist2[i].Key, mblist2[i].Value / num * mblist[i].Value * 0.5f);
				}
			}
			return mblist;
		}

		// Token: 0x06001657 RID: 5719 RVA: 0x000666A4 File Offset: 0x000648A4
		public override MBReadOnlyList<KeyValuePair<MapEventParty, float>> GetLootCasualtyChances(MBReadOnlyList<MapEventParty> winnerParties, PartyBase defeatedParty)
		{
			MBList<KeyValuePair<MapEventParty, float>> mblist = new MBList<KeyValuePair<MapEventParty, float>>();
			if (!defeatedParty.IsSettlement || !defeatedParty.Settlement.IsTown)
			{
				float num = 0f;
				foreach (MapEventParty mapEventParty in winnerParties)
				{
					MobileParty mobileParty = mapEventParty.Party.MobileParty;
					if (mapEventParty.ContributionToBattle > 0 && mapEventParty.Party.MemberRoster.Count > 0 && (mobileParty == null || (!mobileParty.IsGarrison && !mobileParty.IsMilitia)))
					{
						mblist.Add(new KeyValuePair<MapEventParty, float>(mapEventParty, (float)mapEventParty.ContributionToBattle));
						num += (float)mapEventParty.ContributionToBattle;
					}
				}
				for (int i = 0; i < mblist.Count; i++)
				{
					mblist[i] = new KeyValuePair<MapEventParty, float>(mblist[i].Key, mblist[i].Value / num * 1f);
				}
			}
			return mblist;
		}

		// Token: 0x06001658 RID: 5720 RVA: 0x000667B8 File Offset: 0x000649B8
		public override float CalculateShipDamageAfterDefeat(Ship ship)
		{
			return 0f;
		}

		// Token: 0x06001659 RID: 5721 RVA: 0x000667BF File Offset: 0x000649BF
		public override MBReadOnlyList<KeyValuePair<Ship, MapEventParty>> DistributeDefeatedPartyShipsAmongWinners(MapEvent mapEvent, MBReadOnlyList<Ship> shipsToLoot, MBReadOnlyList<MapEventParty> winnerParties)
		{
			return new MBReadOnlyList<KeyValuePair<Ship, MapEventParty>>();
		}

		// Token: 0x0600165A RID: 5722 RVA: 0x000667C8 File Offset: 0x000649C8
		public override float GetBannerLootChanceFromDefeatedHero(Hero defeatedHero)
		{
			Clan clan = defeatedHero.Clan;
			Hero hero;
			if (clan == null)
			{
				hero = null;
			}
			else
			{
				Kingdom kingdom = clan.Kingdom;
				hero = ((kingdom != null) ? kingdom.RulingClan.Leader : null);
			}
			if (hero == defeatedHero)
			{
				return 0.1f;
			}
			Clan clan2 = defeatedHero.Clan;
			if (((clan2 != null) ? clan2.Leader : null) == defeatedHero)
			{
				return 0.25f;
			}
			return 0.5f;
		}

		// Token: 0x0600165B RID: 5723 RVA: 0x00066824 File Offset: 0x00064A24
		public override ItemObject GetBannerRewardForWinningMapEvent(MapEvent mapEvent)
		{
			if (mapEvent.IsHideoutBattle || (mapEvent.AttackerSide.MissionSide == mapEvent.PlayerSide && mapEvent.IsSiegeAssault))
			{
				bool isHideoutBattle = mapEvent.IsHideoutBattle;
				Settlement mapEventSettlement = mapEvent.MapEventSettlement;
				float num = (isHideoutBattle ? 0.1f : 0.5f);
				if (MBRandom.RandomFloat <= num)
				{
					MBList<ItemObject> mblist = Campaign.Current.Models.BannerItemModel.GetPossibleRewardBannerItems().ToMBList<ItemObject>();
					if (mblist.Count > 0)
					{
						mblist.Shuffle<ItemObject>();
						int num2 = (isHideoutBattle ? 1 : mapEventSettlement.Town.GetWallLevel());
						foreach (ItemObject itemObject in mblist)
						{
							if (((BannerComponent)itemObject.ItemComponent).BannerLevel == num2 && (itemObject.Culture == null || itemObject.Culture.StringId == "neutral_culture" || (!isHideoutBattle && itemObject.Culture == mapEventSettlement.Culture)))
							{
								return itemObject;
							}
						}
					}
				}
			}
			return null;
		}

		// Token: 0x0600165C RID: 5724 RVA: 0x00066950 File Offset: 0x00064B50
		public override float GetSunkenShipMoraleEffect(PartyBase shipOwner, Ship ship)
		{
			return 0f;
		}

		// Token: 0x0600165D RID: 5725 RVA: 0x00066958 File Offset: 0x00064B58
		public override ExplainedNumber CalculateMoraleChangeOnRoundVictory(PartyBase party, MapEventSide partySide, BattleSideEnum roundWinner)
		{
			int num = 0;
			if (partySide.MissionSide != roundWinner && roundWinner != BattleSideEnum.None)
			{
				if (partySide.MapEvent.RetreatingSide != BattleSideEnum.None)
				{
					num = -1;
				}
				else
				{
					num = -3;
				}
			}
			return new ExplainedNumber((float)num, false, null);
		}

		// Token: 0x0600165E RID: 5726 RVA: 0x00066992 File Offset: 0x00064B92
		public override float GetShipSiegeEngineHitMoraleEffect(Ship ship, SiegeEngineType siegeEngineType)
		{
			return 0f;
		}

		// Token: 0x0600165F RID: 5727 RVA: 0x00066999 File Offset: 0x00064B99
		public override Figurehead GetFigureheadLoot(MBReadOnlyList<MapEventParty> defeatedParties, PartyBase defeatedSideLeaderParty)
		{
			return null;
		}

		// Token: 0x06001660 RID: 5728 RVA: 0x0006699C File Offset: 0x00064B9C
		public override MBReadOnlyList<MapEventParty> GetWinnerPartiesThatCanPlunderGoldFromShips(MBReadOnlyList<MapEventParty> winnerParties)
		{
			return new MBReadOnlyList<MapEventParty>();
		}

		// Token: 0x04000761 RID: 1889
		private static readonly int[] _indices = new int[12];

		// Token: 0x04000762 RID: 1890
		private const float DestroyHideoutBannerLootChance = 0.1f;

		// Token: 0x04000763 RID: 1891
		private const float CaptureSettlementBannerLootChance = 0.5f;

		// Token: 0x04000764 RID: 1892
		private const float DefeatRegularHeroBannerLootChance = 0.5f;

		// Token: 0x04000765 RID: 1893
		private const float DefeatClanLeaderBannerLootChance = 0.25f;

		// Token: 0x04000766 RID: 1894
		private const float DefeatKingdomRulerBannerLootChance = 0.1f;

		// Token: 0x04000767 RID: 1895
		private const float MainPartyMemberScatterChance = 0.1f;
	}
}
