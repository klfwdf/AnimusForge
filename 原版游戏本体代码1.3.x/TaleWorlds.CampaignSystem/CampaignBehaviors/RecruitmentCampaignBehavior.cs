using System;
using System.Collections.Generic;
using Helpers;
using TaleWorlds.CampaignSystem.Actions;
using TaleWorlds.CampaignSystem.CharacterDevelopment;
using TaleWorlds.CampaignSystem.Conversation;
using TaleWorlds.CampaignSystem.Encounters;
using TaleWorlds.CampaignSystem.GameMenus;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.Core;
using TaleWorlds.Library;
using TaleWorlds.LinQuick;
using TaleWorlds.Localization;
using TaleWorlds.SaveSystem;

namespace TaleWorlds.CampaignSystem.CampaignBehaviors
{
	// Token: 0x02000439 RID: 1081
	public class RecruitmentCampaignBehavior : CampaignBehaviorBase
	{
		// Token: 0x060044AA RID: 17578 RVA: 0x001505DC File Offset: 0x0014E7DC
		public override void RegisterEvents()
		{
			CampaignEvents.BeforeSettlementEnteredEvent.AddNonSerializedListener(this, new Action<MobileParty, Settlement, Hero>(this.OnBeforeSettlementEntered));
			CampaignEvents.OnSessionLaunchedEvent.AddNonSerializedListener(this, new Action<CampaignGameStarter>(this.OnSessionLaunched));
			CampaignEvents.DailyTickTownEvent.AddNonSerializedListener(this, new Action<Town>(this.DailyTickTown));
			CampaignEvents.DailyTickSettlementEvent.AddNonSerializedListener(this, new Action<Settlement>(this.DailyTickSettlement));
			CampaignEvents.HourlyTickPartyEvent.AddNonSerializedListener(this, new Action<MobileParty>(this.HourlyTickParty));
			CampaignEvents.OnNewGameCreatedPartialFollowUpEndEvent.AddNonSerializedListener(this, new Action<CampaignGameStarter>(this.OnNewGameCreatedPartialFollowUpEnd));
			CampaignEvents.OnUnitRecruitedEvent.AddNonSerializedListener(this, new Action<CharacterObject, int>(this.OnUnitRecruited));
			CampaignEvents.OnTroopRecruitedEvent.AddNonSerializedListener(this, new Action<Hero, Settlement, Hero, CharacterObject, int>(this.OnTroopRecruited));
		}

		// Token: 0x060044AB RID: 17579 RVA: 0x001506A1 File Offset: 0x0014E8A1
		private void DailyTickSettlement(Settlement settlement)
		{
			this.UpdateVolunteersOfNotablesInSettlement(settlement);
		}

		// Token: 0x060044AC RID: 17580 RVA: 0x001506AA File Offset: 0x0014E8AA
		public override void SyncData(IDataStore dataStore)
		{
			dataStore.SyncData<CharacterObject>("_selectedTroop", ref this._selectedTroop);
			dataStore.SyncData<Dictionary<Town, RecruitmentCampaignBehavior.TownMercenaryData>>("_townMercenaryData", ref this._townMercenaryData);
		}

		// Token: 0x060044AD RID: 17581 RVA: 0x001506D0 File Offset: 0x0014E8D0
		public RecruitmentCampaignBehavior.TownMercenaryData GetMercenaryData(Town town)
		{
			RecruitmentCampaignBehavior.TownMercenaryData townMercenaryData;
			if (!this._townMercenaryData.TryGetValue(town, out townMercenaryData))
			{
				townMercenaryData = new RecruitmentCampaignBehavior.TownMercenaryData(town);
				this._townMercenaryData.Add(town, townMercenaryData);
			}
			return townMercenaryData;
		}

		// Token: 0x060044AE RID: 17582 RVA: 0x00150704 File Offset: 0x0014E904
		private void OnNewGameCreatedPartialFollowUpEnd(CampaignGameStarter starter)
		{
			foreach (Town town in Town.AllTowns)
			{
				this.UpdateCurrentMercenaryTroopAndCount(town, true);
			}
			foreach (Settlement settlement in Settlement.All)
			{
				this.UpdateVolunteersOfNotablesInSettlement(settlement);
			}
		}

		// Token: 0x060044AF RID: 17583 RVA: 0x00150798 File Offset: 0x0014E998
		private void OnTroopRecruited(Hero recruiter, Settlement settlement, Hero recruitmentSource, CharacterObject troop, int count)
		{
			if (recruiter != null && recruiter.PartyBelongedTo != null && recruiter.GetPerkValue(DefaultPerks.Leadership.FamousCommander))
			{
				recruiter.PartyBelongedTo.MemberRoster.AddXpToTroop(troop, (int)DefaultPerks.Leadership.FamousCommander.SecondaryBonus * count);
			}
			SkillLevelingManager.OnTroopRecruited(recruiter, count, troop.Tier);
			if (recruiter != null && recruiter.PartyBelongedTo != null && troop.Occupation == Occupation.Bandit)
			{
				SkillLevelingManager.OnBanditsRecruited(recruiter.PartyBelongedTo, troop, count);
			}
		}

		// Token: 0x060044B0 RID: 17584 RVA: 0x00150814 File Offset: 0x0014EA14
		private void OnUnitRecruited(CharacterObject troop, int count)
		{
			if (Hero.MainHero.GetPerkValue(DefaultPerks.Leadership.FamousCommander))
			{
				MobileParty.MainParty.MemberRoster.AddXpToTroop(troop, (int)DefaultPerks.Leadership.FamousCommander.SecondaryBonus * count);
			}
			SkillLevelingManager.OnTroopRecruited(Hero.MainHero, count, troop.Tier);
			if (troop.Occupation == Occupation.Bandit)
			{
				SkillLevelingManager.OnBanditsRecruited(MobileParty.MainParty, troop, count);
			}
		}

		// Token: 0x060044B1 RID: 17585 RVA: 0x00150878 File Offset: 0x0014EA78
		private void DailyTickTown(Town town)
		{
			this.UpdateCurrentMercenaryTroopAndCount(town, (int)CampaignTime.Now.ToDays % 2 == 0);
		}

		// Token: 0x060044B2 RID: 17586 RVA: 0x0015089F File Offset: 0x0014EA9F
		private void OnSessionLaunched(CampaignGameStarter campaignGameStarter)
		{
			this.AddGameMenus(campaignGameStarter);
			this.AddDialogs(campaignGameStarter);
		}

		// Token: 0x060044B3 RID: 17587 RVA: 0x001508B0 File Offset: 0x0014EAB0
		private void UpdateVolunteersOfNotablesInSettlement(Settlement settlement)
		{
			if ((settlement.IsTown && !settlement.Town.InRebelliousState) || (settlement.IsVillage && !settlement.Village.Bound.Town.InRebelliousState))
			{
				foreach (Hero hero in settlement.Notables)
				{
					if (hero.CanHaveRecruits && hero.IsAlive)
					{
						bool flag = false;
						CharacterObject basicVolunteer = Campaign.Current.Models.VolunteerModel.GetBasicVolunteer(hero);
						for (int i = 0; i < 6; i++)
						{
							if (MBRandom.RandomFloat < Campaign.Current.Models.VolunteerModel.GetDailyVolunteerProductionProbability(hero, i, settlement))
							{
								CharacterObject characterObject = hero.VolunteerTypes[i];
								if (characterObject == null)
								{
									hero.VolunteerTypes[i] = basicVolunteer;
									flag = true;
								}
								else if (characterObject.UpgradeTargets.Length != 0 && characterObject.Tier < Campaign.Current.Models.VolunteerModel.MaxVolunteerTier)
								{
									float num = MathF.Log(hero.Power / (float)characterObject.Tier, 2f) * 0.01f;
									if (MBRandom.RandomFloat < num)
									{
										hero.VolunteerTypes[i] = characterObject.UpgradeTargets[MBRandom.RandomInt(characterObject.UpgradeTargets.Length)];
										flag = true;
									}
								}
							}
						}
						if (flag)
						{
							CharacterObject[] volunteerTypes = hero.VolunteerTypes;
							for (int j = 1; j < 6; j++)
							{
								CharacterObject characterObject2 = volunteerTypes[j];
								if (characterObject2 != null)
								{
									int num2 = 0;
									int num3 = j - 1;
									CharacterObject characterObject3 = volunteerTypes[num3];
									while (num3 >= 0 && (characterObject3 == null || (float)characterObject2.Level + (characterObject2.IsMounted ? 0.5f : 0f) < (float)characterObject3.Level + (characterObject3.IsMounted ? 0.5f : 0f)))
									{
										if (characterObject3 == null)
										{
											num3--;
											num2++;
											if (num3 >= 0)
											{
												characterObject3 = volunteerTypes[num3];
											}
										}
										else
										{
											volunteerTypes[num3 + 1 + num2] = characterObject3;
											num3--;
											num2 = 0;
											if (num3 >= 0)
											{
												characterObject3 = volunteerTypes[num3];
											}
										}
									}
									volunteerTypes[num3 + 1 + num2] = characterObject2;
								}
							}
						}
					}
				}
			}
		}

		// Token: 0x060044B4 RID: 17588 RVA: 0x00150B18 File Offset: 0x0014ED18
		public void HourlyTickParty(MobileParty mobileParty)
		{
			if ((mobileParty.IsCaravan || mobileParty.IsLordParty) && mobileParty.MapEvent == null && mobileParty != MobileParty.MainParty)
			{
				Settlement currentSettlementOfMobilePartyForAICalculation = MobilePartyHelper.GetCurrentSettlementOfMobilePartyForAICalculation(mobileParty);
				if (currentSettlementOfMobilePartyForAICalculation != null)
				{
					if ((currentSettlementOfMobilePartyForAICalculation.IsVillage && !currentSettlementOfMobilePartyForAICalculation.IsRaided && !currentSettlementOfMobilePartyForAICalculation.IsUnderRaid) || (currentSettlementOfMobilePartyForAICalculation.IsTown && !currentSettlementOfMobilePartyForAICalculation.IsUnderSiege))
					{
						this.CheckRecruiting(mobileParty, currentSettlementOfMobilePartyForAICalculation);
						return;
					}
				}
				else if (MBRandom.RandomFloat < 0.05f && mobileParty.LeaderHero != null && mobileParty.ActualClan != Clan.PlayerClan && !mobileParty.IsCaravan)
				{
					IFaction mapFaction = mobileParty.MapFaction;
					if (mapFaction != null && mapFaction.IsMinorFaction && MobileParty.MainParty.Position.DistanceSquared(mobileParty.Position) > (MobileParty.MainParty.SeeingRange + 5f) * (MobileParty.MainParty.SeeingRange + 5f))
					{
						int partySizeLimit = mobileParty.Party.PartySizeLimit;
						float num = (float)mobileParty.Party.NumberOfAllMembers / (float)partySizeLimit;
						float num2 = (((double)num < 0.2) ? 1000f : (((double)num < 0.3) ? 2000f : (((double)num < 0.4) ? 3000f : (((double)num < 0.55) ? 4000f : (((double)num < 0.7) ? 5000f : 7000f)))));
						float num3 = (((float)mobileParty.PartyTradeGold > num2) ? 1f : MathF.Sqrt((float)mobileParty.PartyTradeGold / num2));
						if (MBRandom.RandomFloat < (1f - num) * num3)
						{
							CharacterObject basicTroop = mobileParty.ActualClan.BasicTroop;
							int num4 = MBRandom.RandomInt(3, 8);
							if (num4 + mobileParty.Party.NumberOfAllMembers > partySizeLimit)
							{
								num4 = partySizeLimit - mobileParty.Party.NumberOfAllMembers;
							}
							int roundedResultNumber = Campaign.Current.Models.PartyWageModel.GetTroopRecruitmentCost(basicTroop, mobileParty.LeaderHero, false).RoundedResultNumber;
							if (num4 * roundedResultNumber > mobileParty.PartyTradeGold)
							{
								num4 = mobileParty.PartyTradeGold / roundedResultNumber;
							}
							if (num4 > 0)
							{
								this.GetRecruitVolunteerFromMap(mobileParty, basicTroop, num4);
							}
						}
					}
				}
			}
		}

		// Token: 0x060044B5 RID: 17589 RVA: 0x00150D60 File Offset: 0x0014EF60
		private void UpdateCurrentMercenaryTroopAndCount(Town town, bool forceUpdate = false)
		{
			RecruitmentCampaignBehavior.TownMercenaryData mercenaryData = this.GetMercenaryData(town);
			if (!forceUpdate && mercenaryData.HasAvailableMercenary(Occupation.NotAssigned))
			{
				int difference = this.FindNumberOfMercenariesWillBeAdded(mercenaryData.TroopType, true);
				mercenaryData.ChangeMercenaryCount(difference);
				return;
			}
			if (MBRandom.RandomFloat < Campaign.Current.Models.TavernMercenaryTroopsModel.RegularMercenariesSpawnChance)
			{
				CharacterObject randomElementInefficiently = town.Culture.BasicMercenaryTroops.GetRandomElementInefficiently<CharacterObject>();
				this._selectedTroop = null;
				float num = this.FindTotalMercenaryProbability(randomElementInefficiently, 1f);
				float randomValueRemaining = MBRandom.RandomFloat * num;
				this.FindRandomMercenaryTroop(randomElementInefficiently, 1f, randomValueRemaining);
				int number = this.FindNumberOfMercenariesWillBeAdded(this._selectedTroop, false);
				mercenaryData.ChangeMercenaryType(this._selectedTroop, number);
				return;
			}
			CharacterObject caravanGuard = town.Culture.CaravanGuard;
			if (caravanGuard != null)
			{
				this._selectedTroop = null;
				float num2 = this.FindTotalMercenaryProbability(caravanGuard, 1f);
				float randomValueRemaining2 = MBRandom.RandomFloat * num2;
				this.FindRandomMercenaryTroop(caravanGuard, 1f, randomValueRemaining2);
				int number2 = this.FindNumberOfMercenariesWillBeAdded(this._selectedTroop, false);
				mercenaryData.ChangeMercenaryType(this._selectedTroop, number2);
			}
		}

		// Token: 0x060044B6 RID: 17590 RVA: 0x00150E6C File Offset: 0x0014F06C
		private float FindTotalMercenaryProbability(CharacterObject mercenaryTroop, float probabilityOfTroop)
		{
			float num = probabilityOfTroop;
			foreach (CharacterObject mercenaryTroop2 in mercenaryTroop.UpgradeTargets)
			{
				num += this.FindTotalMercenaryProbability(mercenaryTroop2, probabilityOfTroop / 1.5f);
			}
			return num;
		}

		// Token: 0x060044B7 RID: 17591 RVA: 0x00150EA8 File Offset: 0x0014F0A8
		private float FindRandomMercenaryTroop(CharacterObject mercenaryTroop, float probabilityOfTroop, float randomValueRemaining)
		{
			randomValueRemaining -= probabilityOfTroop;
			if (randomValueRemaining <= 1E-05f && this._selectedTroop == null)
			{
				this._selectedTroop = mercenaryTroop;
				return 1f;
			}
			float num = probabilityOfTroop;
			foreach (CharacterObject mercenaryTroop2 in mercenaryTroop.UpgradeTargets)
			{
				float num2 = this.FindRandomMercenaryTroop(mercenaryTroop2, probabilityOfTroop / 1.5f, randomValueRemaining);
				randomValueRemaining -= num2;
				num += num2;
			}
			return num;
		}

		// Token: 0x060044B8 RID: 17592 RVA: 0x00150F10 File Offset: 0x0014F110
		private int FindNumberOfMercenariesWillBeAdded(CharacterObject character, bool dailyUpdate = false)
		{
			int tier = Campaign.Current.Models.CharacterStatsModel.GetTier(character);
			int maxCharacterTier = Campaign.Current.Models.CharacterStatsModel.MaxCharacterTier;
			int num = (maxCharacterTier - tier) * 2;
			int num2 = (maxCharacterTier - tier) * 5;
			float randomFloat = MBRandom.RandomFloat;
			float randomFloat2 = MBRandom.RandomFloat;
			return MBRandom.RoundRandomized(MBMath.ClampFloat((randomFloat * randomFloat2 * (float)(num2 - num) + (float)num) * (dailyUpdate ? 0.1f : 1f), 1f, (float)num2));
		}

		// Token: 0x060044B9 RID: 17593 RVA: 0x00150F88 File Offset: 0x0014F188
		private void CheckRecruiting(MobileParty mobileParty, Settlement settlement)
		{
			if (settlement.IsTown && mobileParty.IsCaravan)
			{
				RecruitmentCampaignBehavior.TownMercenaryData mercenaryData = this.GetMercenaryData(settlement.Town);
				if (mercenaryData.HasAvailableMercenary(Occupation.CaravanGuard) || mercenaryData.HasAvailableMercenary(Occupation.Mercenary))
				{
					int partySizeLimit = mobileParty.Party.PartySizeLimit;
					if (mobileParty.Party.NumberOfAllMembers < partySizeLimit)
					{
						CharacterObject troopType = mercenaryData.TroopType;
						int roundedResultNumber = Campaign.Current.Models.PartyWageModel.GetTroopRecruitmentCost(troopType, mobileParty.LeaderHero, false).RoundedResultNumber;
						int num = (mobileParty.IsCaravan ? 2000 : 0);
						if (mobileParty.PartyTradeGold > roundedResultNumber + num)
						{
							bool flag = true;
							double num2 = 0.0;
							for (int i = 0; i < mercenaryData.Number; i++)
							{
								if (flag)
								{
									int num3 = mobileParty.PartyTradeGold - (roundedResultNumber + num);
									double num4 = (double)MathF.Min(1f, MathF.Sqrt((float)num3 / (100f * (float)roundedResultNumber)));
									float num5 = (float)mobileParty.Party.NumberOfAllMembers / (float)partySizeLimit;
									float num6 = (MathF.Min(10f, 1f / num5) * MathF.Min(10f, 1f / num5) - 1f) * ((mobileParty.IsCaravan && mobileParty.Party.Owner == Hero.MainHero) ? 0.4f : 0.1f);
									num2 = num4 * (double)num6;
								}
								if ((double)MBRandom.RandomFloat < num2)
								{
									this.ApplyRecruitMercenary(mobileParty, settlement, troopType, 1);
									flag = true;
								}
								else
								{
									flag = false;
								}
							}
							return;
						}
					}
				}
			}
			else if (mobileParty.IsLordParty && !mobileParty.IsDisbanding && mobileParty.LeaderHero != null && !mobileParty.Party.IsStarving && mobileParty.Party.LeaderHero.IsAlive && (float)mobileParty.PartyTradeGold > HeroHelper.StartRecruitingMoneyLimit(mobileParty.LeaderHero) && (mobileParty.LeaderHero == mobileParty.LeaderHero.Clan.Leader || (float)mobileParty.LeaderHero.Clan.Gold > HeroHelper.StartRecruitingMoneyLimitForClanLeader(mobileParty.LeaderHero)) && ((float)mobileParty.Party.NumberOfAllMembers + 0.5f) / (float)mobileParty.Party.PartySizeLimit <= 1f)
			{
				if (settlement.IsTown && this.GetMercenaryData(settlement.Town).HasAvailableMercenary(Occupation.Mercenary))
				{
					float num7 = (float)mobileParty.Party.NumberOfAllMembers / (float)mobileParty.Party.PartySizeLimit;
					CharacterObject troopType2 = this.GetMercenaryData(settlement.Town).TroopType;
					if (troopType2 != null)
					{
						int roundedResultNumber2 = Campaign.Current.Models.PartyWageModel.GetTroopRecruitmentCost(troopType2, mobileParty.LeaderHero, false).RoundedResultNumber;
						if (roundedResultNumber2 < 5000)
						{
							float num8 = MathF.Min(1f, (float)mobileParty.PartyTradeGold / ((roundedResultNumber2 <= 100) ? 100000f : ((float)((roundedResultNumber2 <= 200) ? 125000 : ((roundedResultNumber2 <= 400) ? 150000 : ((roundedResultNumber2 <= 700) ? 175000 : ((roundedResultNumber2 <= 1100) ? 200000 : ((roundedResultNumber2 <= 1600) ? 250000 : ((roundedResultNumber2 <= 2200) ? 300000 : 400000)))))))));
							float num9 = num8 * num8;
							float num10 = MathF.Max(1f, MathF.Min(10f, 1f / num7)) - 1f;
							float num11 = num9 * num10 * 0.25f;
							int number = this.GetMercenaryData(settlement.Town).Number;
							int num12 = 0;
							int characterWage = Campaign.Current.Models.PartyWageModel.GetCharacterWage(troopType2);
							for (int j = 0; j < number; j++)
							{
								if (MBRandom.RandomFloat < num11)
								{
									num12++;
								}
							}
							num12 = MathF.Min(num12, mobileParty.Party.PartySizeLimit - mobileParty.Party.NumberOfAllMembers);
							num12 = (((double)roundedResultNumber2 <= 0.1) ? num12 : MathF.Min(mobileParty.PartyTradeGold / roundedResultNumber2, num12));
							num12 = MathF.Min(num12, mobileParty.GetAvailableWageBudget() / characterWage);
							if (num12 > 0)
							{
								this.ApplyRecruitMercenary(mobileParty, settlement, troopType2, num12);
							}
						}
					}
				}
				if (mobileParty.Party.NumberOfAllMembers < mobileParty.Party.PartySizeLimit && !mobileParty.IsWageLimitExceeded())
				{
					this.RecruitVolunteersFromNotable(mobileParty, settlement);
				}
			}
		}

		// Token: 0x060044BA RID: 17594 RVA: 0x001513F8 File Offset: 0x0014F5F8
		private void RecruitVolunteersFromNotable(MobileParty mobileParty, Settlement settlement)
		{
			if (((float)mobileParty.Party.NumberOfAllMembers + 0.5f) / (float)mobileParty.Party.PartySizeLimit <= 1f)
			{
				foreach (Hero hero in settlement.Notables)
				{
					if (hero.IsAlive)
					{
						int num = hero.VolunteerTypes.FindIndexQ((CharacterObject x) => x != null);
						if (num >= 0)
						{
							int num2 = MBRandom.RandomInt(6);
							int num3 = Campaign.Current.Models.VolunteerModel.MaximumIndexHeroCanRecruitFromHero(mobileParty.IsGarrison ? mobileParty.Party.Owner : mobileParty.LeaderHero, hero, -101);
							if (num <= num3)
							{
								for (int i = num2; i < num2 + 6; i++)
								{
									int num4 = i % 6;
									if (num4 >= num3)
									{
										break;
									}
									int num5 = ((mobileParty.LeaderHero != null) ? ((int)MathF.Sqrt((float)mobileParty.PartyTradeGold / 10000f)) : 0);
									float num6 = MBRandom.RandomFloat;
									for (int j = 0; j < num5; j++)
									{
										float randomFloat = MBRandom.RandomFloat;
										if (randomFloat > num6)
										{
											num6 = randomFloat;
										}
									}
									if (mobileParty.Army != null)
									{
										float y = ((mobileParty.Army.LeaderParty == mobileParty) ? 0.5f : 0.67f);
										num6 = MathF.Pow(num6, y);
									}
									float num7 = (float)mobileParty.Party.NumberOfAllMembers / (float)mobileParty.Party.PartySizeLimit;
									if (num6 > num7 - 0.1f)
									{
										CharacterObject characterObject = hero.VolunteerTypes[num4];
										if (characterObject != null && mobileParty.PartyTradeGold > Campaign.Current.Models.PartyWageModel.GetTroopRecruitmentCost(characterObject, mobileParty.LeaderHero, false).RoundedResultNumber && mobileParty.GetAvailableWageBudget() >= Campaign.Current.Models.PartyWageModel.GetCharacterWage(characterObject))
										{
											this.GetRecruitVolunteerFromIndividual(mobileParty, characterObject, hero, num4);
											break;
										}
									}
								}
							}
						}
					}
				}
			}
		}

		// Token: 0x060044BB RID: 17595 RVA: 0x00151630 File Offset: 0x0014F830
		public void OnBeforeSettlementEntered(MobileParty mobileParty, Settlement settlement, Hero hero)
		{
			if (mobileParty != null && mobileParty.MapEvent == null)
			{
				if (!settlement.IsVillage)
				{
					IFaction mapFaction = settlement.MapFaction;
					if (mapFaction == null || mapFaction.IsAtWarWith(mobileParty.MapFaction))
					{
						return;
					}
				}
				if (!settlement.IsRaided && !settlement.IsUnderRaid)
				{
					int num = (mobileParty.IsCaravan ? 1 : ((mobileParty.Army != null && mobileParty.Army == MobileParty.MainParty.Army) ? ((MobileParty.MainParty.PartySizeRatio < 0.6f) ? 1 : ((MobileParty.MainParty.PartySizeRatio < 0.9f) ? 2 : 3)) : 7));
					List<MobileParty> list = new List<MobileParty>();
					if (mobileParty.Army != null && mobileParty.Army.LeaderParty == mobileParty)
					{
						using (List<MobileParty>.Enumerator enumerator = mobileParty.Army.Parties.GetEnumerator())
						{
							while (enumerator.MoveNext())
							{
								MobileParty mobileParty2 = enumerator.Current;
								if ((mobileParty2 == mobileParty.Army.LeaderParty || mobileParty2.AttachedTo == mobileParty.Army.LeaderParty) && mobileParty2 != MobileParty.MainParty)
								{
									list.Add(mobileParty2);
								}
							}
							goto IL_138;
						}
					}
					if (mobileParty.AttachedTo == null && mobileParty != MobileParty.MainParty)
					{
						list.Add(mobileParty);
					}
					IL_138:
					for (int i = 0; i < num; i++)
					{
						foreach (MobileParty mobileParty3 in list)
						{
							this.CheckRecruiting(mobileParty3, settlement);
						}
					}
				}
			}
		}

		// Token: 0x060044BC RID: 17596 RVA: 0x001517D8 File Offset: 0x0014F9D8
		private void ApplyInternal(MobileParty side1Party, Settlement settlement, Hero individual, CharacterObject troop, int number, int bitCode, RecruitmentCampaignBehavior.RecruitingDetail detail)
		{
			int roundedResultNumber = Campaign.Current.Models.PartyWageModel.GetTroopRecruitmentCost(troop, side1Party.LeaderHero, false).RoundedResultNumber;
			if (detail == RecruitmentCampaignBehavior.RecruitingDetail.MercenaryFromTavern)
			{
				if (side1Party.IsCaravan)
				{
					side1Party.PartyTradeGold -= number * roundedResultNumber;
					this.GetMercenaryData(settlement.Town).ChangeMercenaryCount(-number);
				}
				else
				{
					GiveGoldAction.ApplyBetweenCharacters(side1Party.LeaderHero, null, number * roundedResultNumber, true);
					this.GetMercenaryData(settlement.Town).ChangeMercenaryCount(-number);
				}
				side1Party.AddElementToMemberRoster(troop, number, false);
			}
			else if (detail == RecruitmentCampaignBehavior.RecruitingDetail.VolunteerFromIndividual)
			{
				GiveGoldAction.ApplyBetweenCharacters(side1Party.LeaderHero, null, roundedResultNumber, true);
				individual.VolunteerTypes[bitCode] = null;
				side1Party.AddElementToMemberRoster(troop, 1, false);
			}
			else if (detail == RecruitmentCampaignBehavior.RecruitingDetail.VolunteerFromMap)
			{
				GiveGoldAction.ApplyBetweenCharacters(side1Party.LeaderHero, null, number * roundedResultNumber, true);
				side1Party.AddElementToMemberRoster(troop, number, false);
			}
			else if (detail == RecruitmentCampaignBehavior.RecruitingDetail.VolunteerFromIndividualToGarrison)
			{
				individual.VolunteerTypes[bitCode] = null;
				side1Party.AddElementToMemberRoster(troop, 1, false);
			}
			CampaignEventDispatcher.Instance.OnTroopRecruited(side1Party.LeaderHero, settlement, individual, troop, number);
		}

		// Token: 0x060044BD RID: 17597 RVA: 0x001518EF File Offset: 0x0014FAEF
		private void ApplyRecruitMercenary(MobileParty side1Party, Settlement side2Party, CharacterObject subject, int number)
		{
			this.ApplyInternal(side1Party, side2Party, null, subject, number, -1, RecruitmentCampaignBehavior.RecruitingDetail.MercenaryFromTavern);
		}

		// Token: 0x060044BE RID: 17598 RVA: 0x001518FF File Offset: 0x0014FAFF
		private void GetRecruitVolunteerFromMap(MobileParty side1Party, CharacterObject subject, int number)
		{
			this.ApplyInternal(side1Party, null, null, subject, number, -1, RecruitmentCampaignBehavior.RecruitingDetail.VolunteerFromMap);
		}

		// Token: 0x060044BF RID: 17599 RVA: 0x0015190E File Offset: 0x0014FB0E
		private void GetRecruitVolunteerFromIndividual(MobileParty side1Party, CharacterObject subject, Hero individual, int bitCode)
		{
			this.ApplyInternal(side1Party, individual.CurrentSettlement, individual, subject, 1, bitCode, RecruitmentCampaignBehavior.RecruitingDetail.VolunteerFromIndividual);
		}

		// Token: 0x060044C0 RID: 17600 RVA: 0x00151924 File Offset: 0x0014FB24
		protected void AddGameMenus(CampaignGameStarter campaignGameSystemStarter)
		{
			campaignGameSystemStarter.AddGameMenuOption("town_backstreet", "recruit_mercenaries", "{=NwO0CVzn}Recruit {MEN_COUNT} {MERCENARY_NAME} ({TOTAL_AMOUNT}{GOLD_ICON})", new GameMenuOption.OnConditionDelegate(this.buy_mercenaries_condition), delegate(MenuCallbackArgs x)
			{
				this.buy_mercenaries_on_consequence();
			}, false, 2, false, null);
		}

		// Token: 0x060044C1 RID: 17601 RVA: 0x00151964 File Offset: 0x0014FB64
		protected void AddDialogs(CampaignGameStarter campaignGameStarter)
		{
			campaignGameStarter.AddDialogLine("mercenary_recruit_start", "start", "mercenary_tavern_talk", "{=I0StkXlK}Do you have a need for fighters, {?PLAYER.GENDER}madam{?}sir{\\?}? Me and {?PLURAL}{MERCENARY_COUNT} of my mates{?}one of my mates{\\?} are looking for a master. You might call us mercenaries, like. We'll join you for {GOLD_AMOUNT}{GOLD_ICON}", new ConversationSentence.OnConditionDelegate(this.conversation_mercenary_recruit_plural_start_on_condition), null, 100, null);
			campaignGameStarter.AddDialogLine("mercenary_recruit_start_single", "start", "mercenary_tavern_talk", "{=rJwExPKb}Do you have a need for fighters, {?PLAYER.GENDER}madam{?}sir{\\?}? I am looking for a master. I'll join you for {GOLD_AMOUNT}{GOLD_ICON}", new ConversationSentence.OnConditionDelegate(this.conversation_mercenary_recruit_single_start_on_condition), null, 100, null);
			campaignGameStarter.AddPlayerLine("mercenary_recruit_accept", "mercenary_tavern_talk", "mercenary_tavern_talk_hire", "{=PDLDvUfH}All right. I will hire {?PLURAL}all of you{?}you{\\?}. Here is {GOLD_AMOUNT}{GOLD_ICON}", new ConversationSentence.OnConditionDelegate(this.conversation_mercenary_recruit_accept_on_condition), new ConversationSentence.OnConsequenceDelegate(this.conversation_mercenary_recruit_accept_on_consequence), 100, null, null);
			campaignGameStarter.AddPlayerLine("mercenary_recruit_accept_some", "mercenary_tavern_talk", "mercenary_tavern_talk_hire", "{=aTPc7AkY}All right. But I can only hire {MERCENARY_COUNT} of you. Here is {GOLD_AMOUNT}{GOLD_ICON}", new ConversationSentence.OnConditionDelegate(this.conversation_mercenary_recruit_accept_some_on_condition), new ConversationSentence.OnConsequenceDelegate(this.conversation_mercenary_recruit_accept_some_on_consequence), 100, null, null);
			campaignGameStarter.AddPlayerLine("mercenary_recruit_reject_gold", "mercenary_tavern_talk", "close_window", "{=n5BGNLrc}That sounds good. But I can't afford any more men right now.", new ConversationSentence.OnConditionDelegate(this.conversation_mercenary_recruit_reject_gold_on_condition), null, 100, null, null);
			campaignGameStarter.AddPlayerLine("mercenary_recruit_reject", "mercenary_tavern_talk", "close_window", "{=ZSWrAC7V}Sorry, I can't take on any more troops right now.", new ConversationSentence.OnConditionDelegate(this.conversation_mercenary_recruit_dont_need_men_on_condition), null, 100, null, null);
			campaignGameStarter.AddDialogLine("mercenary_recruit_end", "mercenary_tavern_talk_hire", "close_window", "{=vbxQoyN3}{RANDOM_HIRE_SENTENCE}", new ConversationSentence.OnConditionDelegate(this.conversation_mercenary_recruit_end_on_condition), null, 100, null);
			campaignGameStarter.AddDialogLine("mercenary_recruit_start_2", "start", "close_window", "{=Jhj437BV}Don't worry, I'll be ready. Just having a last drink for the road.", new ConversationSentence.OnConditionDelegate(this.conversation_mercenary_recruited_on_condition), null, 100, null);
		}

		// Token: 0x060044C2 RID: 17602 RVA: 0x00151AE4 File Offset: 0x0014FCE4
		private bool buy_mercenaries_condition(MenuCallbackArgs args)
		{
			if (MobileParty.MainParty.CurrentSettlement != null && MobileParty.MainParty.CurrentSettlement.IsTown && this.GetMercenaryData(MobileParty.MainParty.CurrentSettlement.Town).Number > 0)
			{
				RecruitmentCampaignBehavior.TownMercenaryData mercenaryData = this.GetMercenaryData(MobileParty.MainParty.CurrentSettlement.Town);
				int roundedResultNumber = Campaign.Current.Models.PartyWageModel.GetTroopRecruitmentCost(mercenaryData.TroopType, Hero.MainHero, false).RoundedResultNumber;
				if (Hero.MainHero.Gold >= roundedResultNumber)
				{
					int num = MathF.Min(mercenaryData.Number, Hero.MainHero.Gold / roundedResultNumber);
					MBTextManager.SetTextVariable("MEN_COUNT", num);
					MBTextManager.SetTextVariable("MERCENARY_NAME", mercenaryData.TroopType.Name, false);
					MBTextManager.SetTextVariable("TOTAL_AMOUNT", num * roundedResultNumber);
				}
				else
				{
					args.Tooltip = GameTexts.FindText("str_decision_not_enough_gold", null);
					args.IsEnabled = false;
					int number = mercenaryData.Number;
					MBTextManager.SetTextVariable("MEN_COUNT", number);
					MBTextManager.SetTextVariable("MERCENARY_NAME", mercenaryData.TroopType.Name, false);
					MBTextManager.SetTextVariable("TOTAL_AMOUNT", number * roundedResultNumber);
				}
				args.optionLeaveType = GameMenuOption.LeaveType.Bribe;
				return true;
			}
			return false;
		}

		// Token: 0x060044C3 RID: 17603 RVA: 0x00151C24 File Offset: 0x0014FE24
		private void buy_mercenaries_on_consequence()
		{
			if (MobileParty.MainParty.CurrentSettlement != null && MobileParty.MainParty.CurrentSettlement.IsTown && this.GetMercenaryData(MobileParty.MainParty.CurrentSettlement.Town).Number > 0)
			{
				RecruitmentCampaignBehavior.TownMercenaryData mercenaryData = this.GetMercenaryData(MobileParty.MainParty.CurrentSettlement.Town);
				int roundedResultNumber = Campaign.Current.Models.PartyWageModel.GetTroopRecruitmentCost(mercenaryData.TroopType, Hero.MainHero, false).RoundedResultNumber;
				if (Hero.MainHero.Gold >= roundedResultNumber)
				{
					int num = MathF.Min(mercenaryData.Number, Hero.MainHero.Gold / roundedResultNumber);
					MobileParty.MainParty.MemberRoster.AddToCounts(mercenaryData.TroopType, num, false, 0, 0, true, -1);
					GiveGoldAction.ApplyBetweenCharacters(null, Hero.MainHero, -(num * roundedResultNumber), false);
					mercenaryData.ChangeMercenaryCount(-num);
					GameMenu.SwitchToMenu("town_backstreet");
				}
			}
		}

		// Token: 0x060044C4 RID: 17604 RVA: 0x00151D18 File Offset: 0x0014FF18
		private bool conversation_mercenary_recruit_plural_start_on_condition()
		{
			if (PlayerEncounter.EncounterSettlement == null || !PlayerEncounter.EncounterSettlement.IsTown)
			{
				return false;
			}
			RecruitmentCampaignBehavior.TownMercenaryData mercenaryData = this.GetMercenaryData(PlayerEncounter.EncounterSettlement.Town);
			bool flag = (CharacterObject.OneToOneConversationCharacter.Occupation == Occupation.Mercenary || CharacterObject.OneToOneConversationCharacter.Occupation == Occupation.CaravanGuard || CharacterObject.OneToOneConversationCharacter.Occupation == Occupation.Gangster) && PlayerEncounter.EncounterSettlement != null && PlayerEncounter.EncounterSettlement.IsTown && mercenaryData.Number > 1;
			if (flag)
			{
				int roundedResultNumber = Campaign.Current.Models.PartyWageModel.GetTroopRecruitmentCost(mercenaryData.TroopType, Hero.MainHero, false).RoundedResultNumber;
				MBTextManager.SetTextVariable("PLURAL", (mercenaryData.Number - 1 > 1) ? 1 : 0);
				MBTextManager.SetTextVariable("MERCENARY_COUNT", mercenaryData.Number - 1);
				MBTextManager.SetTextVariable("GOLD_AMOUNT", roundedResultNumber * mercenaryData.Number);
			}
			return flag;
		}

		// Token: 0x060044C5 RID: 17605 RVA: 0x00151DFC File Offset: 0x0014FFFC
		private bool conversation_mercenary_recruit_single_start_on_condition()
		{
			if (PlayerEncounter.EncounterSettlement == null || !PlayerEncounter.EncounterSettlement.IsTown)
			{
				return false;
			}
			RecruitmentCampaignBehavior.TownMercenaryData mercenaryData = this.GetMercenaryData(PlayerEncounter.EncounterSettlement.Town);
			bool flag = (CharacterObject.OneToOneConversationCharacter.Occupation == Occupation.Mercenary || CharacterObject.OneToOneConversationCharacter.Occupation == Occupation.CaravanGuard || CharacterObject.OneToOneConversationCharacter.Occupation == Occupation.Gangster) && PlayerEncounter.EncounterSettlement != null && PlayerEncounter.EncounterSettlement.IsTown && mercenaryData.Number == 1;
			if (flag)
			{
				int roundedResultNumber = Campaign.Current.Models.PartyWageModel.GetTroopRecruitmentCost(mercenaryData.TroopType, Hero.MainHero, false).RoundedResultNumber;
				MBTextManager.SetTextVariable("GOLD_AMOUNT", mercenaryData.Number * roundedResultNumber);
			}
			return flag;
		}

		// Token: 0x060044C6 RID: 17606 RVA: 0x00151EB8 File Offset: 0x001500B8
		private bool conversation_mercenary_recruit_accept_on_condition()
		{
			RecruitmentCampaignBehavior.TownMercenaryData mercenaryData = this.GetMercenaryData(PlayerEncounter.EncounterSettlement.Town);
			int roundedResultNumber = Campaign.Current.Models.PartyWageModel.GetTroopRecruitmentCost(mercenaryData.TroopType, Hero.MainHero, false).RoundedResultNumber;
			MBTextManager.SetTextVariable("PLURAL", (mercenaryData.Number > 1) ? 1 : 0);
			return Hero.MainHero.Gold >= mercenaryData.Number * roundedResultNumber;
		}

		// Token: 0x060044C7 RID: 17607 RVA: 0x00151F2D File Offset: 0x0015012D
		private bool conversation_mercenary_recruited_on_condition()
		{
			return (CharacterObject.OneToOneConversationCharacter.Occupation == Occupation.Mercenary || CharacterObject.OneToOneConversationCharacter.Occupation == Occupation.CaravanGuard || CharacterObject.OneToOneConversationCharacter.Occupation == Occupation.Gangster) && PlayerEncounter.EncounterSettlement != null;
		}

		// Token: 0x060044C8 RID: 17608 RVA: 0x00151F64 File Offset: 0x00150164
		private void BuyMercenaries()
		{
			this.GetMercenaryData(PlayerEncounter.EncounterSettlement.Town).ChangeMercenaryCount(-this._selectedMercenaryCount);
			int roundedResultNumber = Campaign.Current.Models.PartyWageModel.GetTroopRecruitmentCost(this.GetMercenaryData(PlayerEncounter.EncounterSettlement.Town).TroopType, Hero.MainHero, false).RoundedResultNumber;
			MobileParty.MainParty.AddElementToMemberRoster(CharacterObject.OneToOneConversationCharacter, this._selectedMercenaryCount, false);
			int amount = this._selectedMercenaryCount * roundedResultNumber;
			GiveGoldAction.ApplyBetweenCharacters(Hero.MainHero, null, amount, false);
			CampaignEventDispatcher.Instance.OnUnitRecruited(CharacterObject.OneToOneConversationCharacter, this._selectedMercenaryCount);
		}

		// Token: 0x060044C9 RID: 17609 RVA: 0x00152007 File Offset: 0x00150207
		private void conversation_mercenary_recruit_accept_on_consequence()
		{
			this._selectedMercenaryCount = this.GetMercenaryData(PlayerEncounter.EncounterSettlement.Town).Number;
			this.BuyMercenaries();
		}

		// Token: 0x060044CA RID: 17610 RVA: 0x0015202C File Offset: 0x0015022C
		private bool conversation_mercenary_recruit_accept_some_on_condition()
		{
			int roundedResultNumber = Campaign.Current.Models.PartyWageModel.GetTroopRecruitmentCost(this.GetMercenaryData(PlayerEncounter.EncounterSettlement.Town).TroopType, Hero.MainHero, false).RoundedResultNumber;
			if (Hero.MainHero.Gold >= roundedResultNumber && Hero.MainHero.Gold < this.GetMercenaryData(PlayerEncounter.EncounterSettlement.Town).Number * roundedResultNumber)
			{
				this._selectedMercenaryCount = 0;
				while (Hero.MainHero.Gold >= roundedResultNumber * (this._selectedMercenaryCount + 1))
				{
					this._selectedMercenaryCount++;
				}
				MBTextManager.SetTextVariable("MERCENARY_COUNT", this._selectedMercenaryCount);
				MBTextManager.SetTextVariable("GOLD_AMOUNT", roundedResultNumber * this._selectedMercenaryCount);
				return true;
			}
			return false;
		}

		// Token: 0x060044CB RID: 17611 RVA: 0x001520F3 File Offset: 0x001502F3
		private void conversation_mercenary_recruit_accept_some_on_consequence()
		{
			this.BuyMercenaries();
		}

		// Token: 0x060044CC RID: 17612 RVA: 0x001520FC File Offset: 0x001502FC
		private bool conversation_mercenary_recruit_reject_gold_on_condition()
		{
			int roundedResultNumber = Campaign.Current.Models.PartyWageModel.GetTroopRecruitmentCost(this.GetMercenaryData(PlayerEncounter.EncounterSettlement.Town).TroopType, Hero.MainHero, false).RoundedResultNumber;
			return Hero.MainHero.Gold < roundedResultNumber;
		}

		// Token: 0x060044CD RID: 17613 RVA: 0x00152150 File Offset: 0x00150350
		private bool conversation_mercenary_recruit_dont_need_men_on_condition()
		{
			int roundedResultNumber = Campaign.Current.Models.PartyWageModel.GetTroopRecruitmentCost(this.GetMercenaryData(PlayerEncounter.EncounterSettlement.Town).TroopType, Hero.MainHero, false).RoundedResultNumber;
			return Hero.MainHero.Gold >= roundedResultNumber;
		}

		// Token: 0x060044CE RID: 17614 RVA: 0x001521A8 File Offset: 0x001503A8
		private bool conversation_mercenary_recruit_end_on_condition()
		{
			MBTextManager.SetTextVariable("RANDOM_HIRE_SENTENCE", GameTexts.FindText("str_mercenary_tavern_talk_hire", MBRandom.RandomInt(4).ToString()), false);
			return true;
		}

		// Token: 0x0400135C RID: 4956
		private Dictionary<Town, RecruitmentCampaignBehavior.TownMercenaryData> _townMercenaryData = new Dictionary<Town, RecruitmentCampaignBehavior.TownMercenaryData>();

		// Token: 0x0400135D RID: 4957
		private int _selectedMercenaryCount;

		// Token: 0x0400135E RID: 4958
		private CharacterObject _selectedTroop;

		// Token: 0x02000832 RID: 2098
		public class RecruitmentCampaignBehaviorTypeDefiner : SaveableTypeDefiner
		{
			// Token: 0x060066AA RID: 26282 RVA: 0x001C2B87 File Offset: 0x001C0D87
			public RecruitmentCampaignBehaviorTypeDefiner()
				: base(881200)
			{
			}

			// Token: 0x060066AB RID: 26283 RVA: 0x001C2B94 File Offset: 0x001C0D94
			protected override void DefineClassTypes()
			{
				base.AddClassDefinition(typeof(RecruitmentCampaignBehavior.TownMercenaryData), 1, null);
			}

			// Token: 0x060066AC RID: 26284 RVA: 0x001C2BA8 File Offset: 0x001C0DA8
			protected override void DefineContainerDefinitions()
			{
				base.ConstructContainerDefinition(typeof(Dictionary<Town, RecruitmentCampaignBehavior.TownMercenaryData>));
			}
		}

		// Token: 0x02000833 RID: 2099
		public class TownMercenaryData
		{
			// Token: 0x17001521 RID: 5409
			// (get) Token: 0x060066AD RID: 26285 RVA: 0x001C2BBA File Offset: 0x001C0DBA
			// (set) Token: 0x060066AE RID: 26286 RVA: 0x001C2BC2 File Offset: 0x001C0DC2
			[SaveableProperty(202)]
			public CharacterObject TroopType { get; private set; }

			// Token: 0x17001522 RID: 5410
			// (get) Token: 0x060066AF RID: 26287 RVA: 0x001C2BCB File Offset: 0x001C0DCB
			// (set) Token: 0x060066B0 RID: 26288 RVA: 0x001C2BD3 File Offset: 0x001C0DD3
			[SaveableProperty(203)]
			public int Number { get; private set; }

			// Token: 0x060066B1 RID: 26289 RVA: 0x001C2BDC File Offset: 0x001C0DDC
			public TownMercenaryData(Town currentTown)
			{
				this._currentTown = currentTown;
			}

			// Token: 0x060066B2 RID: 26290 RVA: 0x001C2BEC File Offset: 0x001C0DEC
			public void ChangeMercenaryType(CharacterObject troopType, int number)
			{
				if (troopType != this.TroopType)
				{
					CharacterObject troopType2 = this.TroopType;
					this.TroopType = troopType;
					this.Number = number;
					CampaignEventDispatcher.Instance.OnMercenaryTroopChangedInTown(this._currentTown, troopType2, this.TroopType);
					return;
				}
				if (this.Number != number)
				{
					int difference = number - this.Number;
					this.ChangeMercenaryCount(difference);
				}
			}

			// Token: 0x060066B3 RID: 26291 RVA: 0x001C2C48 File Offset: 0x001C0E48
			public void ChangeMercenaryCount(int difference)
			{
				if (difference != 0)
				{
					int number = this.Number;
					this.Number += difference;
					CampaignEventDispatcher.Instance.OnMercenaryNumberChangedInTown(this._currentTown, number, this.Number);
				}
			}

			// Token: 0x060066B4 RID: 26292 RVA: 0x001C2C84 File Offset: 0x001C0E84
			public bool HasAvailableMercenary(Occupation occupation = Occupation.NotAssigned)
			{
				return this.TroopType != null && this.Number > 0 && (occupation == Occupation.NotAssigned || this.TroopType.Occupation == occupation);
			}

			// Token: 0x060066B5 RID: 26293 RVA: 0x001C2CAC File Offset: 0x001C0EAC
			internal static void AutoGeneratedStaticCollectObjectsTownMercenaryData(object o, List<object> collectedObjects)
			{
				((RecruitmentCampaignBehavior.TownMercenaryData)o).AutoGeneratedInstanceCollectObjects(collectedObjects);
			}

			// Token: 0x060066B6 RID: 26294 RVA: 0x001C2CBA File Offset: 0x001C0EBA
			protected virtual void AutoGeneratedInstanceCollectObjects(List<object> collectedObjects)
			{
				collectedObjects.Add(this._currentTown);
				collectedObjects.Add(this.TroopType);
			}

			// Token: 0x060066B7 RID: 26295 RVA: 0x001C2CD4 File Offset: 0x001C0ED4
			internal static object AutoGeneratedGetMemberValueTroopType(object o)
			{
				return ((RecruitmentCampaignBehavior.TownMercenaryData)o).TroopType;
			}

			// Token: 0x060066B8 RID: 26296 RVA: 0x001C2CE1 File Offset: 0x001C0EE1
			internal static object AutoGeneratedGetMemberValueNumber(object o)
			{
				return ((RecruitmentCampaignBehavior.TownMercenaryData)o).Number;
			}

			// Token: 0x060066B9 RID: 26297 RVA: 0x001C2CF3 File Offset: 0x001C0EF3
			internal static object AutoGeneratedGetMemberValue_currentTown(object o)
			{
				return ((RecruitmentCampaignBehavior.TownMercenaryData)o)._currentTown;
			}

			// Token: 0x040022F4 RID: 8948
			[SaveableField(204)]
			private readonly Town _currentTown;
		}

		// Token: 0x02000834 RID: 2100
		public enum RecruitingDetail
		{
			// Token: 0x040022F6 RID: 8950
			MercenaryFromTavern,
			// Token: 0x040022F7 RID: 8951
			VolunteerFromIndividual,
			// Token: 0x040022F8 RID: 8952
			VolunteerFromIndividualToGarrison,
			// Token: 0x040022F9 RID: 8953
			VolunteerFromMap
		}
	}
}
