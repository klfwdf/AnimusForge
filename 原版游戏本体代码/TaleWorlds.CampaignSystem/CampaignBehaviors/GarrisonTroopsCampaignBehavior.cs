using System;
using System.Collections.Generic;
using System.Linq;
using Helpers;
using TaleWorlds.CampaignSystem.Actions;
using TaleWorlds.CampaignSystem.CharacterDevelopment;
using TaleWorlds.CampaignSystem.ComponentInterfaces;
using TaleWorlds.CampaignSystem.MapEvents;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.CampaignSystem.Roster;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.Core;
using TaleWorlds.Library;
using TaleWorlds.LinQuick;

namespace TaleWorlds.CampaignSystem.CampaignBehaviors
{
	// Token: 0x020003F1 RID: 1009
	public class GarrisonTroopsCampaignBehavior : CampaignBehaviorBase
	{
		// Token: 0x06003ED6 RID: 16086 RVA: 0x0011A6D4 File Offset: 0x001188D4
		public override void RegisterEvents()
		{
			CampaignEvents.SettlementEntered.AddNonSerializedListener(this, new Action<MobileParty, Settlement, Hero>(this.OnSettlementEntered));
			CampaignEvents.OnNewGameCreatedPartialFollowUpEvent.AddNonSerializedListener(this, new Action<CampaignGameStarter, int>(this.OnNewGameCreatedPartialFollowUpEvent));
			CampaignEvents.OnSettlementOwnerChangedEvent.AddNonSerializedListener(this, new Action<Settlement, bool, Hero, Hero, Hero, ChangeOwnerOfSettlementAction.ChangeOwnerOfSettlementDetail>(this.OnSettlementOwnerChanged));
		}

		// Token: 0x06003ED7 RID: 16087 RVA: 0x0011A726 File Offset: 0x00118926
		public override void SyncData(IDataStore dataStore)
		{
		}

		// Token: 0x06003ED8 RID: 16088 RVA: 0x0011A728 File Offset: 0x00118928
		private void OnNewGameCreatedPartialFollowUpEvent(CampaignGameStarter starter, int i)
		{
			List<Settlement> list = Campaign.Current.Settlements.WhereQ((Settlement x) => x.IsFortification).ToList<Settlement>();
			int count = list.Count;
			int num = count / 100 + ((count % 100 > i) ? 1 : 0);
			int num2 = count / 100 * i;
			for (int j = 0; j < i; j++)
			{
				num2 += ((count % 100 > j) ? 1 : 0);
			}
			for (int k = 0; k < num; k++)
			{
				Settlement settlement = list[num2 + k];
				settlement.AddGarrisonParty();
				this.FillGarrisonPartyOnNewGame(settlement.Town);
			}
		}

		// Token: 0x06003ED9 RID: 16089 RVA: 0x0011A7D8 File Offset: 0x001189D8
		private void FillGarrisonPartyOnNewGame(Town fortification)
		{
			PartyTemplateObject defaultPartyTemplate = fortification.Culture.DefaultPartyTemplate;
			float num = (float)70;
			float num2 = 1f + fortification.Prosperity / 1300f;
			int num3 = MathF.Round(num * num2);
			for (int i = 0; i < num3; i++)
			{
				int index = 0;
				float num4 = 0f;
				for (int j = 0; j < defaultPartyTemplate.Stacks.Count; j++)
				{
					num4 += (defaultPartyTemplate.Stacks[j].Character.IsRanged ? 6f : ((!defaultPartyTemplate.Stacks[j].Character.IsMounted) ? 2f : 1f)) * ((float)(defaultPartyTemplate.Stacks[j].MaxValue + defaultPartyTemplate.Stacks[j].MinValue) / 2f);
				}
				float num5 = MBRandom.RandomFloat * num4;
				for (int k = 0; k < defaultPartyTemplate.Stacks.Count; k++)
				{
					num5 -= (defaultPartyTemplate.Stacks[k].Character.IsRanged ? 6f : ((!defaultPartyTemplate.Stacks[k].Character.IsMounted) ? 2f : 1f)) * ((float)(defaultPartyTemplate.Stacks[k].MaxValue + defaultPartyTemplate.Stacks[k].MinValue) / 2f);
					if (num5 < 0f)
					{
						index = k;
						break;
					}
				}
				CharacterObject character = defaultPartyTemplate.Stacks[index].Character;
				fortification.GarrisonParty.AddElementToMemberRoster(character, 1, false);
			}
		}

		// Token: 0x06003EDA RID: 16090 RVA: 0x0011A993 File Offset: 0x00118B93
		private void OnSettlementOwnerChanged(Settlement settlement, bool openToClaim, Hero newOwner, Hero oldOwner, Hero capturerHero, ChangeOwnerOfSettlementAction.ChangeOwnerOfSettlementDetail detail)
		{
			if (openToClaim && detail == ChangeOwnerOfSettlementAction.ChangeOwnerOfSettlementDetail.BySiege && settlement != null)
			{
				this._newlyConqueredFortification = settlement;
			}
		}

		// Token: 0x06003EDB RID: 16091 RVA: 0x0011A9A8 File Offset: 0x00118BA8
		private void OnSettlementEntered(MobileParty mobileParty, Settlement settlement, Hero hero)
		{
			if (!Campaign.Current.GameStarted)
			{
				return;
			}
			if (mobileParty != null && mobileParty.IsLordParty && !mobileParty.IsDisbanding && mobileParty.LeaderHero != null && settlement.IsFortification && DiplomacyHelper.IsSameFactionAndNotEliminated(mobileParty.MapFaction, settlement.MapFaction) && (settlement.OwnerClan != Clan.PlayerClan || settlement == this._newlyConqueredFortification))
			{
				if (mobileParty.Army != null)
				{
					if (mobileParty.Army.LeaderParty == mobileParty)
					{
						this.ManageGarrisonForArmy(mobileParty, settlement);
						return;
					}
				}
				else if (!mobileParty.IsMainParty)
				{
					this.ManageGarrisonForParty(mobileParty, settlement);
				}
			}
		}

		// Token: 0x06003EDC RID: 16092 RVA: 0x0011AA40 File Offset: 0x00118C40
		private void ManageGarrisonForArmy(MobileParty armyLeaderParty, Settlement settlement)
		{
			GarrisonTroopsCampaignBehavior.ArmyGarrisonTransferDataArgs armyGarrisonTransferDataArgs;
			this.CollectArmyGarrisonTransferDataArgs(armyLeaderParty, settlement, out armyGarrisonTransferDataArgs);
			if (armyGarrisonTransferDataArgs.IsLeavingTroopsToGarrison)
			{
				this.TryToLeaveTroopsToGarrisonForArmy(armyGarrisonTransferDataArgs);
				return;
			}
			this.TryToTakeTroopsFromGarrisonForArmy(armyGarrisonTransferDataArgs);
		}

		// Token: 0x06003EDD RID: 16093 RVA: 0x0011AA70 File Offset: 0x00118C70
		private void CollectArmyGarrisonTransferDataArgs(MobileParty armyLeaderParty, Settlement settlement, out GarrisonTroopsCampaignBehavior.ArmyGarrisonTransferDataArgs armyGarrionTransferDataArgs)
		{
			armyGarrionTransferDataArgs = default(GarrisonTroopsCampaignBehavior.ArmyGarrisonTransferDataArgs);
			int val = this.CalculateSettlementGarrisonPartySizeLimitWithFoodAndWage(settlement);
			int num = this.CalculateSettlementIdealPartySizeWithEffects(settlement);
			List<ValueTuple<MobileParty, int>> list = this.CalculateMobilePartiesIdealPartySizes(armyLeaderParty);
			MobileParty garrisonParty = settlement.Town.GarrisonParty;
			int num2 = ((garrisonParty != null) ? garrisonParty.Party.NumberOfRegularMembers : 0);
			int num3 = num2;
			int num4 = num;
			foreach (ValueTuple<MobileParty, int> valueTuple in list)
			{
				num3 += valueTuple.Item1.Party.NumberOfRegularMembers;
				num4 += valueTuple.Item2;
			}
			float num5 = (float)num / (float)num4;
			int num6 = MBRandom.RoundRandomized((float)num3 * num5);
			int minValue = (settlement.IsTown ? 125 : 75);
			int maxValue = (settlement.IsTown ? 750 : 500);
			num6 = Math.Min(num6, val);
			num6 = MBMath.ClampInt(num6, minValue, maxValue);
			armyGarrionTransferDataArgs.Settlement = settlement;
			armyGarrionTransferDataArgs.ArmyPartiesIdealPartySizes = list;
			armyGarrionTransferDataArgs.TotalIdealPartySize = num4;
			armyGarrionTransferDataArgs.TotalMenCount = num3;
			armyGarrionTransferDataArgs.SettlementCurrentMenCount = num2;
			armyGarrionTransferDataArgs.SettlementFinalMenCount = num6;
			armyGarrionTransferDataArgs.IsLeavingTroopsToGarrison = num6 > num2;
			if (settlement.Town.GarrisonParty != null && settlement.Town.GarrisonParty.IsWageLimitExceeded())
			{
				armyGarrionTransferDataArgs.IsLeavingTroopsToGarrison = false;
			}
			this._newlyConqueredFortification = null;
		}

		// Token: 0x06003EDE RID: 16094 RVA: 0x0011ABD4 File Offset: 0x00118DD4
		private void TryToLeaveTroopsToGarrisonForArmy(in GarrisonTroopsCampaignBehavior.ArmyGarrisonTransferDataArgs armyGarrisonTransferDataArgs)
		{
			GarrisonTroopsCampaignBehavior.ArmyGarrisonTransferDataArgs armyGarrisonTransferDataArgs2 = armyGarrisonTransferDataArgs;
			foreach (ValueTuple<MobileParty, int> valueTuple in armyGarrisonTransferDataArgs2.GetTroopsToLeaveDataForArmy())
			{
				MobileParty item = valueTuple.Item1;
				int item2 = valueTuple.Item2;
				this.LeaveTroopsToGarrison(item, armyGarrisonTransferDataArgs.Settlement, item2, true);
			}
		}

		// Token: 0x06003EDF RID: 16095 RVA: 0x0011AC44 File Offset: 0x00118E44
		private void TryToTakeTroopsFromGarrisonForArmy(in GarrisonTroopsCampaignBehavior.ArmyGarrisonTransferDataArgs armyGarrisonTransferDataArgs)
		{
			GarrisonTroopsCampaignBehavior.ArmyGarrisonTransferDataArgs armyGarrisonTransferDataArgs2 = armyGarrisonTransferDataArgs;
			foreach (ValueTuple<MobileParty, int> valueTuple in armyGarrisonTransferDataArgs2.GetTroopsToTakeDataForArmy())
			{
				MobileParty item = valueTuple.Item1;
				int item2 = valueTuple.Item2;
				this.TakeTroopsFromGarrison(item, armyGarrisonTransferDataArgs.Settlement, item2, false);
			}
		}

		// Token: 0x06003EE0 RID: 16096 RVA: 0x0011ACB4 File Offset: 0x00118EB4
		private int CalculateSettlementIdealPartySizeWithEffects(Settlement settlement)
		{
			float num = (float)this.CalculateSettlementGarrisonPartySizeLimitWithFoodAndWage(settlement);
			float num2 = (settlement.IsTown ? this.GetProsperityEffectForTown(settlement.Town) : 1f);
			float num3 = 1f;
			if (this._newlyConqueredFortification != null)
			{
				num3 = (settlement.IsTown ? 1.75f : 1.33f);
			}
			float num4 = num2 * num3;
			return MBRandom.RoundRandomized(num * num4);
		}

		// Token: 0x06003EE1 RID: 16097 RVA: 0x0011AD14 File Offset: 0x00118F14
		private void ManageGarrisonForParty(MobileParty mobileParty, Settlement settlement)
		{
			GarrisonTroopsCampaignBehavior.PartyGarrisonTransferDataArgs partyGarrisonTransferDataArgs;
			this.CollectPartyGarrisonTransferData(mobileParty, settlement, out partyGarrisonTransferDataArgs);
			if (partyGarrisonTransferDataArgs.IsLeavingTroopsToGarrison)
			{
				this.TryToLeaveTroopsToGarrisonForParty(partyGarrisonTransferDataArgs);
				return;
			}
			this.TryToTakeTroopsFromGarrisonForParty(partyGarrisonTransferDataArgs);
		}

		// Token: 0x06003EE2 RID: 16098 RVA: 0x0011AD44 File Offset: 0x00118F44
		private void CollectPartyGarrisonTransferData(MobileParty mobileParty, Settlement settlement, out GarrisonTroopsCampaignBehavior.PartyGarrisonTransferDataArgs partyGarrisonTransferDataArgs)
		{
			partyGarrisonTransferDataArgs = default(GarrisonTroopsCampaignBehavior.PartyGarrisonTransferDataArgs);
			int num = this.CalculateSettlementGarrisonPartySizeLimitWithFoodAndWage(settlement);
			int num2 = this.CalculateSettlementIdealPartySizeWithEffects(settlement);
			int num3 = this.CalculateMobilePartySizeLimitWithFoodAndWage(mobileParty);
			MobileParty garrisonParty = settlement.Town.GarrisonParty;
			int num4 = ((garrisonParty != null) ? garrisonParty.Party.NumberOfRegularMembers : 0);
			int num5 = mobileParty.Party.NumberOfRegularMembers + num4;
			int num6 = num2 + num3;
			float num7 = (float)num2 / (float)num6;
			int num8 = MBRandom.RoundRandomized((float)num5 * num7);
			int minValue = (settlement.IsTown ? 125 : 75);
			int maxValue = (settlement.IsTown ? 750 : 500);
			num8 = Math.Min(num8, num);
			num8 = MBMath.ClampInt(num8, minValue, maxValue);
			partyGarrisonTransferDataArgs.Settlement = settlement;
			partyGarrisonTransferDataArgs.MobileParty = mobileParty;
			partyGarrisonTransferDataArgs.PartyIdealPartySize = num3;
			partyGarrisonTransferDataArgs.SettlementIdealPartySize = num;
			partyGarrisonTransferDataArgs.TotalIdealPartySize = num6;
			partyGarrisonTransferDataArgs.PartyCurrentMenCount = mobileParty.Party.NumberOfRegularMembers;
			partyGarrisonTransferDataArgs.SettlementCurrentMenCount = num4;
			partyGarrisonTransferDataArgs.TotalMenCount = num5;
			partyGarrisonTransferDataArgs.SettlementFinalMenCount = num8;
			partyGarrisonTransferDataArgs.IsLeavingTroopsToGarrison = num8 > num4;
			if (settlement.Town.GarrisonParty != null && settlement.Town.GarrisonParty.IsWageLimitExceeded())
			{
				partyGarrisonTransferDataArgs.IsLeavingTroopsToGarrison = false;
			}
			this._newlyConqueredFortification = null;
		}

		// Token: 0x06003EE3 RID: 16099 RVA: 0x0011AE74 File Offset: 0x00119074
		private void TryToLeaveTroopsToGarrisonForParty(in GarrisonTroopsCampaignBehavior.PartyGarrisonTransferDataArgs partyGarrisonTransferDataArgs)
		{
			GarrisonTroopsCampaignBehavior.PartyGarrisonTransferDataArgs partyGarrisonTransferDataArgs2 = partyGarrisonTransferDataArgs;
			int numberOfTroopsToLeaveForParty = partyGarrisonTransferDataArgs2.GetNumberOfTroopsToLeaveForParty();
			if (numberOfTroopsToLeaveForParty > 0)
			{
				this.LeaveTroopsToGarrison(partyGarrisonTransferDataArgs.MobileParty, partyGarrisonTransferDataArgs.Settlement, numberOfTroopsToLeaveForParty, true);
			}
		}

		// Token: 0x06003EE4 RID: 16100 RVA: 0x0011AEA8 File Offset: 0x001190A8
		private void TryToTakeTroopsFromGarrisonForParty(in GarrisonTroopsCampaignBehavior.PartyGarrisonTransferDataArgs partyGarrisonTransferDataArgs)
		{
			GarrisonTroopsCampaignBehavior.PartyGarrisonTransferDataArgs partyGarrisonTransferDataArgs2 = partyGarrisonTransferDataArgs;
			int numberOfTroopsToTakeForParty = partyGarrisonTransferDataArgs2.GetNumberOfTroopsToTakeForParty();
			if (numberOfTroopsToTakeForParty > 0)
			{
				this.TakeTroopsFromGarrison(partyGarrisonTransferDataArgs.MobileParty, partyGarrisonTransferDataArgs.Settlement, numberOfTroopsToTakeForParty, false);
			}
		}

		// Token: 0x06003EE5 RID: 16101 RVA: 0x0011AEDC File Offset: 0x001190DC
		private List<ValueTuple<MobileParty, int>> CalculateMobilePartiesIdealPartySizes(MobileParty armyLeaderParty)
		{
			List<ValueTuple<MobileParty, int>> list = new List<ValueTuple<MobileParty, int>>();
			List<MobileParty> list2 = new List<MobileParty>();
			if (armyLeaderParty != MobileParty.MainParty)
			{
				list2.Add(armyLeaderParty);
			}
			foreach (MobileParty mobileParty in armyLeaderParty.AttachedParties)
			{
				if (mobileParty != MobileParty.MainParty && mobileParty.LeaderHero != null)
				{
					list2.Add(mobileParty);
				}
			}
			foreach (MobileParty mobileParty2 in list2)
			{
				int item = this.CalculateMobilePartySizeLimitWithFoodAndWage(mobileParty2);
				list.Add(new ValueTuple<MobileParty, int>(mobileParty2, item));
			}
			return list;
		}

		// Token: 0x06003EE6 RID: 16102 RVA: 0x0011AFAC File Offset: 0x001191AC
		private int CalculateMobilePartySizeLimitWithFoodAndWage(MobileParty mobileParty)
		{
			int partySizeLimit = mobileParty.Party.PartySizeLimit;
			int a = MathF.Round((float)mobileParty.PaymentLimit / Campaign.Current.AverageWage);
			int num = 2;
			float numberOfMenOnMapToEatOneFood = (float)Campaign.Current.Models.MobilePartyFoodConsumptionModel.NumberOfMenOnMapToEatOneFood;
			float num2;
			if (mobileParty.Army == null)
			{
				num2 = mobileParty.Food;
			}
			else
			{
				num2 = mobileParty.Army.Parties.Sum((MobileParty s) => s.Food);
			}
			float num3 = num2;
			int num4 = MathF.Round(numberOfMenOnMapToEatOneFood * num3 / (float)num);
			num4 = MathF.Max(num4, 30);
			return MathF.Min(MathF.Min(a, partySizeLimit), num4);
		}

		// Token: 0x06003EE7 RID: 16103 RVA: 0x0011B054 File Offset: 0x00119254
		private int CalculateMaxGarrisonSizeTownCanFeed(Town town, bool includeMarketStocks = true)
		{
			SettlementFoodModel settlementFoodModel = Campaign.Current.Models.SettlementFoodModel;
			if (settlementFoodModel == null)
			{
				return 0;
			}
			float resultNumber = settlementFoodModel.CalculateTownFoodStocksChange(town, includeMarketStocks, false).ResultNumber;
			MobileParty garrisonParty = town.GarrisonParty;
			int num = ((garrisonParty != null) ? garrisonParty.Party.NumberOfRegularMembers : 0);
			float num2 = 0f;
			float num3 = 0f;
			if (town.Governor != null)
			{
				if (town.IsUnderSiege)
				{
					if (town.Governor.GetPerkValue(DefaultPerks.Steward.Gourmet))
					{
						num3 += DefaultPerks.Steward.Gourmet.SecondaryBonus;
					}
					if (town.Governor.GetPerkValue(DefaultPerks.Medicine.TriageTent))
					{
						num2 += DefaultPerks.Medicine.TriageTent.SecondaryBonus;
					}
				}
				if (town.Governor.GetPerkValue(DefaultPerks.Steward.MasterOfWarcraft))
				{
					num2 += DefaultPerks.Steward.MasterOfWarcraft.SecondaryBonus;
				}
			}
			float num4 = -town.Prosperity / (float)settlementFoodModel.NumberOfProsperityToEatOneFood;
			float num5 = 1f;
			float num6 = num4 * num5;
			int result;
			if (resultNumber < num6)
			{
				if (this._newlyConqueredFortification != null)
				{
					result = (int)MBMath.Map(town.Prosperity, 0f, 8000f, 150f, 300f);
				}
				else
				{
					int num7 = MathF.Round(MathF.Abs(resultNumber - num6) * (float)settlementFoodModel.NumberOfMenOnGarrisonToEatOneFood / (1f + num2 + num3));
					result = Math.Max(num - num7, 0);
				}
			}
			else
			{
				int num8 = MathF.Round((MathF.Abs(num6) + resultNumber) * (float)settlementFoodModel.NumberOfMenOnGarrisonToEatOneFood / (1f + num2 + num3));
				result = num + num8;
			}
			return result;
		}

		// Token: 0x06003EE8 RID: 16104 RVA: 0x0011B1CC File Offset: 0x001193CC
		private int CalculateSettlementGarrisonPartySizeLimitWithFoodAndWage(Settlement settlement)
		{
			int a = (int)Campaign.Current.Models.PartySizeLimitModel.CalculateGarrisonPartySizeLimit(settlement, false).ResultNumber;
			float num = FactionHelper.FindIdealGarrisonStrengthPerWalledCenter(settlement.OwnerClan.MapFaction as Kingdom, settlement.OwnerClan);
			List<float> list = new List<float>();
			float num2;
			if (settlement.OwnerClan.Kingdom != null)
			{
				foreach (Clan clan in settlement.OwnerClan.Kingdom.Clans)
				{
					list.Add(FactionHelper.OwnerClanEconomyEffectOnGarrisonSizeConstant(clan));
				}
				num2 = list.Average();
			}
			else
			{
				num2 = FactionHelper.OwnerClanEconomyEffectOnGarrisonSizeConstant(settlement.OwnerClan);
			}
			float num3 = FactionHelper.SettlementProsperityEffectOnGarrisonSizeConstant(settlement.Town);
			float num4 = FactionHelper.SettlementFoodPotentialEffectOnGarrisonSizeConstant(settlement);
			float num5 = num2 * num3 * num4;
			float num6 = 1.5f;
			int b = MathF.Round(num5 * num * num6);
			int b2 = this.CalculateMaxGarrisonSizeTownCanFeed(settlement.Town, true);
			return MathF.Min(MathF.Min(a, b2), b);
		}

		// Token: 0x06003EE9 RID: 16105 RVA: 0x0011B2E4 File Offset: 0x001194E4
		private float GetProsperityEffectForTown(Town town)
		{
			return MBMath.Map(town.Prosperity, 0f, 8000f, 1f, 1.35f);
		}

		// Token: 0x06003EEA RID: 16106 RVA: 0x0011B308 File Offset: 0x00119508
		private CharacterObject GetASuitableCharacterFromPartyRosterByWeight(TroopRoster troopRoster, bool archersAreHighPriority)
		{
			List<ValueTuple<CharacterObject, float>> list = new List<ValueTuple<CharacterObject, float>>();
			for (int i = 0; i < troopRoster.Count; i++)
			{
				TroopRosterElement elementCopyAtIndex = troopRoster.GetElementCopyAtIndex(i);
				if (!elementCopyAtIndex.Character.IsHero)
				{
					if (archersAreHighPriority && elementCopyAtIndex.Character.IsRanged)
					{
						list.Add(new ValueTuple<CharacterObject, float>(elementCopyAtIndex.Character, (float)(elementCopyAtIndex.Number * 4)));
					}
					else
					{
						list.Add(new ValueTuple<CharacterObject, float>(elementCopyAtIndex.Character, (float)elementCopyAtIndex.Number));
					}
				}
			}
			if (!list.IsEmpty<ValueTuple<CharacterObject, float>>())
			{
				return MBRandom.ChooseWeighted<CharacterObject>(list);
			}
			return null;
		}

		// Token: 0x06003EEB RID: 16107 RVA: 0x0011B398 File Offset: 0x00119598
		private void LeaveTroopsToGarrison(MobileParty mobileParty, Settlement settlement, int numberOfTroopsToLeave, bool archersAreHighPriority)
		{
			TroopRoster troopRoster = TroopRoster.CreateDummyTroopRoster();
			for (int i = 0; i < numberOfTroopsToLeave; i++)
			{
				CharacterObject asuitableCharacterFromPartyRosterByWeight = this.GetASuitableCharacterFromPartyRosterByWeight(mobileParty.MemberRoster, archersAreHighPriority);
				if (asuitableCharacterFromPartyRosterByWeight == null)
				{
					break;
				}
				foreach (TroopRosterElement troopRosterElement in mobileParty.MemberRoster.GetTroopRoster())
				{
					if (troopRosterElement.Character == asuitableCharacterFromPartyRosterByWeight)
					{
						if (settlement.Town.GarrisonParty == null)
						{
							settlement.AddGarrisonParty();
						}
						if (troopRosterElement.WoundedNumber > 0)
						{
							settlement.Town.GarrisonParty.MemberRoster.AddToCounts(asuitableCharacterFromPartyRosterByWeight, 1, false, 1, 0, true, -1);
							troopRoster.AddToCounts(asuitableCharacterFromPartyRosterByWeight, 1, false, 1, 0, true, -1);
							mobileParty.MemberRoster.AddToCounts(asuitableCharacterFromPartyRosterByWeight, -1, false, -1, 0, true, -1);
							break;
						}
						settlement.Town.GarrisonParty.MemberRoster.AddToCounts(asuitableCharacterFromPartyRosterByWeight, 1, false, 0, 0, true, -1);
						troopRoster.AddToCounts(asuitableCharacterFromPartyRosterByWeight, 1, false, 0, 0, true, -1);
						mobileParty.MemberRoster.AddToCounts(asuitableCharacterFromPartyRosterByWeight, -1, false, 0, 0, true, -1);
						break;
					}
				}
			}
			if (troopRoster.Count > 0)
			{
				CampaignEventDispatcher.Instance.OnTroopGivenToSettlement(mobileParty.LeaderHero, settlement, troopRoster);
				this.ApplyKingdomInfluenceBonusForLeavingTroopToGarrison(mobileParty, settlement, troopRoster);
			}
		}

		// Token: 0x06003EEC RID: 16108 RVA: 0x0011B4EC File Offset: 0x001196EC
		private void TakeTroopsFromGarrison(MobileParty mobileParty, Settlement settlement, int numberOfTroopsToTake, bool archersAreHighPriority)
		{
			for (int i = 0; i < numberOfTroopsToTake; i++)
			{
				CharacterObject asuitableCharacterFromPartyRosterByWeight = this.GetASuitableCharacterFromPartyRosterByWeight(settlement.Town.GarrisonParty.MemberRoster, archersAreHighPriority);
				if (asuitableCharacterFromPartyRosterByWeight == null)
				{
					break;
				}
				foreach (TroopRosterElement troopRosterElement in settlement.Town.GarrisonParty.MemberRoster.GetTroopRoster())
				{
					if (troopRosterElement.Character == asuitableCharacterFromPartyRosterByWeight)
					{
						if (troopRosterElement.Number - troopRosterElement.WoundedNumber > 0)
						{
							mobileParty.MemberRoster.AddToCounts(asuitableCharacterFromPartyRosterByWeight, 1, false, 0, 0, true, -1);
							settlement.Town.GarrisonParty.MemberRoster.AddToCounts(asuitableCharacterFromPartyRosterByWeight, -1, false, 0, 0, true, -1);
							break;
						}
						mobileParty.MemberRoster.AddToCounts(asuitableCharacterFromPartyRosterByWeight, 1, false, 1, 0, true, -1);
						settlement.Town.GarrisonParty.MemberRoster.AddToCounts(asuitableCharacterFromPartyRosterByWeight, -1, false, -1, 0, true, -1);
						break;
					}
				}
			}
		}

		// Token: 0x06003EED RID: 16109 RVA: 0x0011B600 File Offset: 0x00119800
		private void ApplyKingdomInfluenceBonusForLeavingTroopToGarrison(MobileParty mobileParty, Settlement settlement, TroopRoster troopsToBeTransferred)
		{
			if (mobileParty.LeaderHero != null && settlement.OwnerClan != mobileParty.LeaderHero.Clan)
			{
				float num = 0f;
				foreach (TroopRosterElement troopRosterElement in troopsToBeTransferred.GetTroopRoster())
				{
					float troopPower = Campaign.Current.Models.MilitaryPowerModel.GetTroopPower(troopRosterElement.Character, BattleSideEnum.Defender, MapEvent.PowerCalculationContext.Siege, 0f);
					num += troopPower * (float)troopRosterElement.Number;
				}
				GainKingdomInfluenceAction.ApplyForLeavingTroopToGarrison(mobileParty.LeaderHero, num / 3f);
			}
		}

		// Token: 0x040012B5 RID: 4789
		private const int PartyMinMenNumberAfterDonation = 30;

		// Token: 0x040012B6 RID: 4790
		private const int MinGarrisonNumberForTown = 125;

		// Token: 0x040012B7 RID: 4791
		private const int MinGarrisonNumberForCastle = 75;

		// Token: 0x040012B8 RID: 4792
		private const int MaxGarrisonNumberForTown = 750;

		// Token: 0x040012B9 RID: 4793
		private const int MaxGarrisonNumberForCastle = 500;

		// Token: 0x040012BA RID: 4794
		private Settlement _newlyConqueredFortification;

		// Token: 0x020007F6 RID: 2038
		private struct ArmyGarrisonTransferDataArgs
		{
			// Token: 0x06006368 RID: 25448 RVA: 0x001BD47C File Offset: 0x001BB67C
			public List<ValueTuple<MobileParty, int>> GetTroopsToLeaveDataForArmy()
			{
				List<ValueTuple<MobileParty, int>> list = new List<ValueTuple<MobileParty, int>>();
				for (int i = 0; i < this.ArmyPartiesIdealPartySizes.Count; i++)
				{
					ValueTuple<MobileParty, int> valueTuple = this.ArmyPartiesIdealPartySizes[i];
					MobileParty item = valueTuple.Item1;
					int item2 = valueTuple.Item2;
					float num = (float)item2 / (float)this.TotalIdealPartySize;
					int num2 = MBMath.ClampInt(MBRandom.RoundRandomized((float)this.TotalMenCount * num), 30, item2);
					int numberOfRegularMembers = item.Party.NumberOfRegularMembers;
					num2 = Math.Min(num2, numberOfRegularMembers);
					int num3 = numberOfRegularMembers - num2;
					if (num3 > 0)
					{
						list.Add(new ValueTuple<MobileParty, int>(item, num3));
					}
				}
				int num4 = list.Sum((ValueTuple<MobileParty, int> s) => s.Item2);
				int num5 = Math.Max(this.SettlementFinalMenCount - this.SettlementCurrentMenCount, 0);
				if (num4 > num5)
				{
					float num6 = (float)num5 / (float)num4;
					for (int j = list.Count - 1; j >= 0; j--)
					{
						list[j] = new ValueTuple<MobileParty, int>(list[j].Item1, MBRandom.RoundRandomized((float)list[j].Item2 * num6));
						if (list[j].Item2 == 0)
						{
							list.RemoveAt(j);
						}
					}
				}
				return list;
			}

			// Token: 0x06006369 RID: 25449 RVA: 0x001BD5C4 File Offset: 0x001BB7C4
			public List<ValueTuple<MobileParty, int>> GetTroopsToTakeDataForArmy()
			{
				List<ValueTuple<MobileParty, int>> list = new List<ValueTuple<MobileParty, int>>();
				if (this.SettlementFinalMenCount < this.SettlementCurrentMenCount)
				{
					for (int i = 0; i < this.ArmyPartiesIdealPartySizes.Count; i++)
					{
						ValueTuple<MobileParty, int> valueTuple = this.ArmyPartiesIdealPartySizes[i];
						MobileParty item = valueTuple.Item1;
						if (item.LeaderHero.Clan == this.Settlement.OwnerClan && !item.IsWageLimitExceeded())
						{
							int item2 = valueTuple.Item2;
							float num = (float)item2 / (float)this.TotalIdealPartySize;
							int val = MBMath.ClampInt(MBRandom.RoundRandomized((float)this.TotalMenCount * num), 30, item2);
							int numberOfRegularMembers = item.Party.NumberOfRegularMembers;
							int num2 = Math.Max(val, numberOfRegularMembers) - numberOfRegularMembers;
							int val2 = Math.Max(item2 - numberOfRegularMembers, 0);
							num2 = Math.Min(num2, val2);
							if (num2 > 0)
							{
								list.Add(new ValueTuple<MobileParty, int>(item, num2));
							}
						}
					}
					int num3 = list.Sum((ValueTuple<MobileParty, int> s) => s.Item2);
					int num4 = Math.Max(this.SettlementCurrentMenCount - this.SettlementFinalMenCount, 0);
					if (num3 > num4)
					{
						float num5 = (float)num4 / (float)num3;
						for (int j = list.Count - 1; j >= 0; j--)
						{
							list[j] = new ValueTuple<MobileParty, int>(list[j].Item1, MBRandom.RoundRandomized((float)list[j].Item2 * num5));
							if (list[j].Item2 == 0)
							{
								list.RemoveAt(j);
							}
						}
					}
				}
				return list;
			}

			// Token: 0x04001FCD RID: 8141
			public Settlement Settlement;

			// Token: 0x04001FCE RID: 8142
			public List<ValueTuple<MobileParty, int>> ArmyPartiesIdealPartySizes;

			// Token: 0x04001FCF RID: 8143
			public int TotalIdealPartySize;

			// Token: 0x04001FD0 RID: 8144
			public int TotalMenCount;

			// Token: 0x04001FD1 RID: 8145
			public int SettlementFinalMenCount;

			// Token: 0x04001FD2 RID: 8146
			public int SettlementCurrentMenCount;

			// Token: 0x04001FD3 RID: 8147
			public bool IsLeavingTroopsToGarrison;
		}

		// Token: 0x020007F7 RID: 2039
		private struct PartyGarrisonTransferDataArgs
		{
			// Token: 0x0600636A RID: 25450 RVA: 0x001BD758 File Offset: 0x001BB958
			public int GetNumberOfTroopsToLeaveForParty()
			{
				int num = 0;
				if (this.SettlementFinalMenCount > this.SettlementCurrentMenCount)
				{
					float num2 = (float)this.PartyIdealPartySize / (float)this.TotalIdealPartySize;
					int num3 = MBMath.ClampInt(MBRandom.RoundRandomized((float)this.TotalMenCount * num2), 30, this.PartyIdealPartySize);
					int partyCurrentMenCount = this.PartyCurrentMenCount;
					num3 = Math.Min(num3, partyCurrentMenCount);
					num = partyCurrentMenCount - num3;
					int val = Math.Max(this.SettlementIdealPartySize - this.SettlementCurrentMenCount, 0);
					num = Math.Min(num, val);
				}
				return num;
			}

			// Token: 0x0600636B RID: 25451 RVA: 0x001BD7D4 File Offset: 0x001BB9D4
			public int GetNumberOfTroopsToTakeForParty()
			{
				int num = 0;
				if (this.MobileParty.LeaderHero.Clan == this.Settlement.OwnerClan && !this.MobileParty.IsWageLimitExceeded() && this.SettlementFinalMenCount < this.SettlementCurrentMenCount)
				{
					float num2 = (float)this.PartyIdealPartySize / (float)this.TotalIdealPartySize;
					int val = MBMath.ClampInt(MBRandom.RoundRandomized((float)this.TotalMenCount * num2), 30, this.PartyIdealPartySize);
					int partyCurrentMenCount = this.PartyCurrentMenCount;
					num = Math.Max(val, partyCurrentMenCount) - partyCurrentMenCount;
					int val2 = Math.Max(this.PartyIdealPartySize - partyCurrentMenCount, 0);
					num = Math.Min(num, val2);
					int val3 = this.SettlementCurrentMenCount - this.SettlementFinalMenCount;
					num = Math.Min(num, val3);
				}
				return num;
			}

			// Token: 0x04001FD4 RID: 8148
			public Settlement Settlement;

			// Token: 0x04001FD5 RID: 8149
			public MobileParty MobileParty;

			// Token: 0x04001FD6 RID: 8150
			public int PartyIdealPartySize;

			// Token: 0x04001FD7 RID: 8151
			public int SettlementIdealPartySize;

			// Token: 0x04001FD8 RID: 8152
			public int TotalIdealPartySize;

			// Token: 0x04001FD9 RID: 8153
			public int TotalMenCount;

			// Token: 0x04001FDA RID: 8154
			public int PartyCurrentMenCount;

			// Token: 0x04001FDB RID: 8155
			public int SettlementFinalMenCount;

			// Token: 0x04001FDC RID: 8156
			public int SettlementCurrentMenCount;

			// Token: 0x04001FDD RID: 8157
			public bool IsLeavingTroopsToGarrison;
		}
	}
}
