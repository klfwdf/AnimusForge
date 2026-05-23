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

namespace TaleWorlds.CampaignSystem.CampaignBehaviors;

public class GarrisonTroopsCampaignBehavior : CampaignBehaviorBase
{
	private struct ArmyGarrisonTransferDataArgs
	{
		public Settlement Settlement;

		public List<(MobileParty, int)> ArmyPartiesIdealPartySizes;

		public int TotalIdealPartySize;

		public int TotalMenCount;

		public int SettlementFinalMenCount;

		public int SettlementCurrentMenCount;

		public bool IsLeavingTroopsToGarrison;

		public List<(MobileParty, int)> GetTroopsToLeaveDataForArmy()
		{
			List<(MobileParty, int)> list = new List<(MobileParty, int)>();
			for (int i = 0; i < ArmyPartiesIdealPartySizes.Count; i++)
			{
				(MobileParty, int) tuple = ArmyPartiesIdealPartySizes[i];
				MobileParty item = tuple.Item1;
				int item2 = tuple.Item2;
				float num = (float)item2 / (float)TotalIdealPartySize;
				int val = MBMath.ClampInt(MBRandom.RoundRandomized((float)TotalMenCount * num), 30, item2);
				int numberOfRegularMembers = item.Party.NumberOfRegularMembers;
				val = Math.Min(val, numberOfRegularMembers);
				int num2 = numberOfRegularMembers - val;
				if (num2 > 0)
				{
					list.Add((item, num2));
				}
			}
			int num3 = list.Sum(((MobileParty, int) s) => s.Item2);
			int num4 = Math.Max(SettlementFinalMenCount - SettlementCurrentMenCount, 0);
			if (num3 > num4)
			{
				float num5 = (float)num4 / (float)num3;
				for (int num6 = list.Count - 1; num6 >= 0; num6--)
				{
					list[num6] = (list[num6].Item1, MBRandom.RoundRandomized((float)list[num6].Item2 * num5));
					if (list[num6].Item2 == 0)
					{
						list.RemoveAt(num6);
					}
				}
			}
			return list;
		}

		public List<(MobileParty, int)> GetTroopsToTakeDataForArmy()
		{
			List<(MobileParty, int)> list = new List<(MobileParty, int)>();
			if (SettlementFinalMenCount < SettlementCurrentMenCount)
			{
				for (int i = 0; i < ArmyPartiesIdealPartySizes.Count; i++)
				{
					(MobileParty, int) tuple = ArmyPartiesIdealPartySizes[i];
					var (mobileParty, _) = tuple;
					if (mobileParty.LeaderHero.Clan == Settlement.OwnerClan && !mobileParty.IsWageLimitExceeded())
					{
						int item = tuple.Item2;
						float num = (float)item / (float)TotalIdealPartySize;
						int val = MBMath.ClampInt(MBRandom.RoundRandomized((float)TotalMenCount * num), 30, item);
						int numberOfRegularMembers = mobileParty.Party.NumberOfRegularMembers;
						int val2 = Math.Max(val, numberOfRegularMembers) - numberOfRegularMembers;
						int val3 = Math.Max(item - numberOfRegularMembers, 0);
						val2 = Math.Min(val2, val3);
						if (val2 > 0)
						{
							list.Add((mobileParty, val2));
						}
					}
				}
				int num2 = list.Sum(((MobileParty, int) s) => s.Item2);
				int num3 = Math.Max(SettlementCurrentMenCount - SettlementFinalMenCount, 0);
				if (num2 > num3)
				{
					float num4 = (float)num3 / (float)num2;
					for (int num5 = list.Count - 1; num5 >= 0; num5--)
					{
						list[num5] = (list[num5].Item1, MBRandom.RoundRandomized((float)list[num5].Item2 * num4));
						if (list[num5].Item2 == 0)
						{
							list.RemoveAt(num5);
						}
					}
				}
			}
			return list;
		}
	}

	private struct PartyGarrisonTransferDataArgs
	{
		public Settlement Settlement;

		public MobileParty MobileParty;

		public int PartyIdealPartySize;

		public int SettlementIdealPartySize;

		public int TotalIdealPartySize;

		public int TotalMenCount;

		public int PartyCurrentMenCount;

		public int SettlementFinalMenCount;

		public int SettlementCurrentMenCount;

		public bool IsLeavingTroopsToGarrison;

		public int GetNumberOfTroopsToLeaveForParty()
		{
			int result = 0;
			if (SettlementFinalMenCount > SettlementCurrentMenCount)
			{
				float num = (float)PartyIdealPartySize / (float)TotalIdealPartySize;
				int val = MBMath.ClampInt(MBRandom.RoundRandomized((float)TotalMenCount * num), 30, PartyIdealPartySize);
				int partyCurrentMenCount = PartyCurrentMenCount;
				val = Math.Min(val, partyCurrentMenCount);
				result = partyCurrentMenCount - val;
				int val2 = Math.Max(SettlementIdealPartySize - SettlementCurrentMenCount, 0);
				result = Math.Min(result, val2);
			}
			return result;
		}

		public int GetNumberOfTroopsToTakeForParty()
		{
			int result = 0;
			if (MobileParty.LeaderHero.Clan == Settlement.OwnerClan && !MobileParty.IsWageLimitExceeded() && SettlementFinalMenCount < SettlementCurrentMenCount)
			{
				float num = (float)PartyIdealPartySize / (float)TotalIdealPartySize;
				int val = MBMath.ClampInt(MBRandom.RoundRandomized((float)TotalMenCount * num), 30, PartyIdealPartySize);
				int partyCurrentMenCount = PartyCurrentMenCount;
				result = Math.Max(val, partyCurrentMenCount) - partyCurrentMenCount;
				int val2 = Math.Max(PartyIdealPartySize - partyCurrentMenCount, 0);
				result = Math.Min(result, val2);
				int val3 = SettlementCurrentMenCount - SettlementFinalMenCount;
				result = Math.Min(result, val3);
			}
			return result;
		}
	}

	private const int PartyMinMenNumberAfterDonation = 30;

	private const int MinGarrisonNumberForTown = 125;

	private const int MinGarrisonNumberForCastle = 75;

	private const int MaxGarrisonNumberForTown = 750;

	private const int MaxGarrisonNumberForCastle = 500;

	private Settlement _newlyConqueredFortification;

	public override void RegisterEvents()
	{
		CampaignEvents.SettlementEntered.AddNonSerializedListener(this, OnSettlementEntered);
		CampaignEvents.OnNewGameCreatedPartialFollowUpEvent.AddNonSerializedListener(this, OnNewGameCreatedPartialFollowUpEvent);
		CampaignEvents.OnSettlementOwnerChangedEvent.AddNonSerializedListener(this, OnSettlementOwnerChanged);
	}

	public override void SyncData(IDataStore dataStore)
	{
	}

	private void OnNewGameCreatedPartialFollowUpEvent(CampaignGameStarter starter, int i)
	{
		List<Settlement> list = Campaign.Current.Settlements.WhereQ((Settlement x) => x.IsFortification).ToList();
		int count = list.Count;
		int num = count / 100 + ((count % 100 > i) ? 1 : 0);
		int num2 = count / 100 * i;
		for (int num3 = 0; num3 < i; num3++)
		{
			num2 += ((count % 100 > num3) ? 1 : 0);
		}
		for (int num4 = 0; num4 < num; num4++)
		{
			Settlement settlement = list[num2 + num4];
			settlement.AddGarrisonParty();
			FillGarrisonPartyOnNewGame(settlement.Town);
		}
	}

	private void FillGarrisonPartyOnNewGame(Town fortification)
	{
		PartyTemplateObject defaultPartyTemplate = fortification.Culture.DefaultPartyTemplate;
		float num = 1f + fortification.Prosperity / 1300f;
		int num2 = TaleWorlds.Library.MathF.Round(70f * num);
		for (int i = 0; i < num2; i++)
		{
			int index = 0;
			float num3 = 0f;
			for (int j = 0; j < defaultPartyTemplate.Stacks.Count; j++)
			{
				num3 += (defaultPartyTemplate.Stacks[j].Character.IsRanged ? 6f : ((!defaultPartyTemplate.Stacks[j].Character.IsMounted) ? 2f : 1f)) * ((float)(defaultPartyTemplate.Stacks[j].MaxValue + defaultPartyTemplate.Stacks[j].MinValue) / 2f);
			}
			float num4 = MBRandom.RandomFloat * num3;
			for (int k = 0; k < defaultPartyTemplate.Stacks.Count; k++)
			{
				num4 -= (defaultPartyTemplate.Stacks[k].Character.IsRanged ? 6f : ((!defaultPartyTemplate.Stacks[k].Character.IsMounted) ? 2f : 1f)) * ((float)(defaultPartyTemplate.Stacks[k].MaxValue + defaultPartyTemplate.Stacks[k].MinValue) / 2f);
				if (num4 < 0f)
				{
					index = k;
					break;
				}
			}
			CharacterObject character = defaultPartyTemplate.Stacks[index].Character;
			fortification.GarrisonParty.AddElementToMemberRoster(character, 1);
		}
	}

	private void OnSettlementOwnerChanged(Settlement settlement, bool openToClaim, Hero newOwner, Hero oldOwner, Hero capturerHero, ChangeOwnerOfSettlementAction.ChangeOwnerOfSettlementDetail detail)
	{
		if (openToClaim && detail == ChangeOwnerOfSettlementAction.ChangeOwnerOfSettlementDetail.BySiege && settlement != null)
		{
			_newlyConqueredFortification = settlement;
		}
	}

	private void OnSettlementEntered(MobileParty mobileParty, Settlement settlement, Hero hero)
	{
		if (!Campaign.Current.GameStarted || mobileParty == null || !mobileParty.IsLordParty || mobileParty.IsDisbanding || mobileParty.LeaderHero == null || !settlement.IsFortification || !DiplomacyHelper.IsSameFactionAndNotEliminated(mobileParty.MapFaction, settlement.MapFaction) || (settlement.OwnerClan == Clan.PlayerClan && settlement != _newlyConqueredFortification))
		{
			return;
		}
		if (mobileParty.Army != null)
		{
			if (mobileParty.Army.LeaderParty == mobileParty)
			{
				ManageGarrisonForArmy(mobileParty, settlement);
			}
		}
		else if (!mobileParty.IsMainParty)
		{
			ManageGarrisonForParty(mobileParty, settlement);
		}
	}

	private void ManageGarrisonForArmy(MobileParty armyLeaderParty, Settlement settlement)
	{
		CollectArmyGarrisonTransferDataArgs(armyLeaderParty, settlement, out var armyGarrionTransferDataArgs);
		if (armyGarrionTransferDataArgs.IsLeavingTroopsToGarrison)
		{
			TryToLeaveTroopsToGarrisonForArmy(in armyGarrionTransferDataArgs);
		}
		else
		{
			TryToTakeTroopsFromGarrisonForArmy(in armyGarrionTransferDataArgs);
		}
		_newlyConqueredFortification = null;
	}

	private void CollectArmyGarrisonTransferDataArgs(MobileParty armyLeaderParty, Settlement settlement, out ArmyGarrisonTransferDataArgs armyGarrionTransferDataArgs)
	{
		armyGarrionTransferDataArgs = default(ArmyGarrisonTransferDataArgs);
		int val = CalculateSettlementGarrisonPartySizeLimitWithFoodAndWage(settlement);
		int num = CalculateSettlementIdealPartySizeWithEffects(settlement);
		List<(MobileParty, int)> list = CalculateMobilePartiesIdealPartySizes(armyLeaderParty);
		int num2 = settlement.Town.GarrisonParty?.Party.NumberOfRegularMembers ?? 0;
		int num3 = num2;
		int num4 = num;
		foreach (var item in list)
		{
			num3 += item.Item1.Party.NumberOfRegularMembers;
			num4 += item.Item2;
		}
		float num5 = (float)num / (float)num4;
		int val2 = MBRandom.RoundRandomized((float)num3 * num5);
		int minValue = (settlement.IsTown ? 125 : 75);
		int maxValue = (settlement.IsTown ? 750 : 500);
		val2 = Math.Min(val2, val);
		val2 = MBMath.ClampInt(val2, minValue, maxValue);
		armyGarrionTransferDataArgs.Settlement = settlement;
		armyGarrionTransferDataArgs.ArmyPartiesIdealPartySizes = list;
		armyGarrionTransferDataArgs.TotalIdealPartySize = num4;
		armyGarrionTransferDataArgs.TotalMenCount = num3;
		armyGarrionTransferDataArgs.SettlementCurrentMenCount = num2;
		armyGarrionTransferDataArgs.SettlementFinalMenCount = val2;
		armyGarrionTransferDataArgs.IsLeavingTroopsToGarrison = val2 > num2;
		if (settlement.Town.GarrisonParty != null && settlement.Town.GarrisonParty.IsWageLimitExceeded())
		{
			armyGarrionTransferDataArgs.IsLeavingTroopsToGarrison = false;
		}
	}

	private void TryToLeaveTroopsToGarrisonForArmy(in ArmyGarrisonTransferDataArgs armyGarrisonTransferDataArgs)
	{
		foreach (var (mobileParty, numberOfTroopsToLeave) in armyGarrisonTransferDataArgs.GetTroopsToLeaveDataForArmy())
		{
			LeaveTroopsToGarrison(mobileParty, armyGarrisonTransferDataArgs.Settlement, numberOfTroopsToLeave, archersAreHighPriority: true);
		}
	}

	private void TryToTakeTroopsFromGarrisonForArmy(in ArmyGarrisonTransferDataArgs armyGarrisonTransferDataArgs)
	{
		foreach (var (mobileParty, numberOfTroopsToTake) in armyGarrisonTransferDataArgs.GetTroopsToTakeDataForArmy())
		{
			TakeTroopsFromGarrison(mobileParty, armyGarrisonTransferDataArgs.Settlement, numberOfTroopsToTake, archersAreHighPriority: false);
		}
	}

	private int CalculateSettlementIdealPartySizeWithEffects(Settlement settlement)
	{
		int num = CalculateSettlementGarrisonPartySizeLimitWithFoodAndWage(settlement);
		float num2 = (settlement.IsTown ? GetProsperityEffectForTown(settlement.Town) : 1f);
		float num3 = 1f;
		if (_newlyConqueredFortification != null)
		{
			num3 = (settlement.IsTown ? 1.75f : 1.33f);
		}
		float num4 = num2 * num3;
		return MBRandom.RoundRandomized((float)num * num4);
	}

	private void ManageGarrisonForParty(MobileParty mobileParty, Settlement settlement)
	{
		CollectPartyGarrisonTransferData(mobileParty, settlement, out var partyGarrisonTransferDataArgs);
		if (partyGarrisonTransferDataArgs.IsLeavingTroopsToGarrison)
		{
			TryToLeaveTroopsToGarrisonForParty(in partyGarrisonTransferDataArgs);
		}
		else
		{
			TryToTakeTroopsFromGarrisonForParty(in partyGarrisonTransferDataArgs);
		}
		_newlyConqueredFortification = null;
	}

	private void CollectPartyGarrisonTransferData(MobileParty mobileParty, Settlement settlement, out PartyGarrisonTransferDataArgs partyGarrisonTransferDataArgs)
	{
		partyGarrisonTransferDataArgs = default(PartyGarrisonTransferDataArgs);
		int num = CalculateSettlementGarrisonPartySizeLimitWithFoodAndWage(settlement);
		int num2 = CalculateSettlementIdealPartySizeWithEffects(settlement);
		int num3 = CalculateMobilePartySizeLimitWithFoodAndWage(mobileParty);
		int num4 = settlement.Town.GarrisonParty?.Party.NumberOfRegularMembers ?? 0;
		int num5 = mobileParty.Party.NumberOfRegularMembers + num4;
		int num6 = num2 + num3;
		float num7 = (float)num2 / (float)num6;
		int val = MBRandom.RoundRandomized((float)num5 * num7);
		int minValue = (settlement.IsTown ? 125 : 75);
		int maxValue = (settlement.IsTown ? 750 : 500);
		val = Math.Min(val, num);
		val = MBMath.ClampInt(val, minValue, maxValue);
		partyGarrisonTransferDataArgs.Settlement = settlement;
		partyGarrisonTransferDataArgs.MobileParty = mobileParty;
		partyGarrisonTransferDataArgs.PartyIdealPartySize = num3;
		partyGarrisonTransferDataArgs.SettlementIdealPartySize = num;
		partyGarrisonTransferDataArgs.TotalIdealPartySize = num6;
		partyGarrisonTransferDataArgs.PartyCurrentMenCount = mobileParty.Party.NumberOfRegularMembers;
		partyGarrisonTransferDataArgs.SettlementCurrentMenCount = num4;
		partyGarrisonTransferDataArgs.TotalMenCount = num5;
		partyGarrisonTransferDataArgs.SettlementFinalMenCount = val;
		partyGarrisonTransferDataArgs.IsLeavingTroopsToGarrison = val > num4;
		if ((settlement.Town.GarrisonParty != null && settlement.Town.GarrisonParty.IsWageLimitExceeded()) || (mobileParty.LeaderHero.Clan == Clan.PlayerClan && _newlyConqueredFortification == null))
		{
			partyGarrisonTransferDataArgs.IsLeavingTroopsToGarrison = false;
		}
	}

	private void TryToLeaveTroopsToGarrisonForParty(in PartyGarrisonTransferDataArgs partyGarrisonTransferDataArgs)
	{
		int numberOfTroopsToLeaveForParty = partyGarrisonTransferDataArgs.GetNumberOfTroopsToLeaveForParty();
		if (numberOfTroopsToLeaveForParty > 0)
		{
			LeaveTroopsToGarrison(partyGarrisonTransferDataArgs.MobileParty, partyGarrisonTransferDataArgs.Settlement, numberOfTroopsToLeaveForParty, archersAreHighPriority: true);
		}
	}

	private void TryToTakeTroopsFromGarrisonForParty(in PartyGarrisonTransferDataArgs partyGarrisonTransferDataArgs)
	{
		int numberOfTroopsToTakeForParty = partyGarrisonTransferDataArgs.GetNumberOfTroopsToTakeForParty();
		if (numberOfTroopsToTakeForParty > 0)
		{
			TakeTroopsFromGarrison(partyGarrisonTransferDataArgs.MobileParty, partyGarrisonTransferDataArgs.Settlement, numberOfTroopsToTakeForParty, archersAreHighPriority: false);
		}
	}

	private List<(MobileParty, int)> CalculateMobilePartiesIdealPartySizes(MobileParty armyLeaderParty)
	{
		List<(MobileParty, int)> list = new List<(MobileParty, int)>();
		List<MobileParty> list2 = new List<MobileParty>();
		if (armyLeaderParty != MobileParty.MainParty && (armyLeaderParty.LeaderHero.Clan != Clan.PlayerClan || _newlyConqueredFortification != null))
		{
			list2.Add(armyLeaderParty);
		}
		foreach (MobileParty attachedParty in armyLeaderParty.AttachedParties)
		{
			if (attachedParty != MobileParty.MainParty && attachedParty.LeaderHero != null && (attachedParty.LeaderHero.Clan != Clan.PlayerClan || _newlyConqueredFortification != null))
			{
				list2.Add(attachedParty);
			}
		}
		foreach (MobileParty item2 in list2)
		{
			int item = CalculateMobilePartySizeLimitWithFoodAndWage(item2);
			list.Add((item2, item));
		}
		return list;
	}

	private int CalculateMobilePartySizeLimitWithFoodAndWage(MobileParty mobileParty)
	{
		int partySizeLimit = mobileParty.Party.PartySizeLimit;
		int a = TaleWorlds.Library.MathF.Round((float)mobileParty.PaymentLimit / Campaign.Current.AverageWage);
		int num = 2;
		int numberOfMenOnMapToEatOneFood = Campaign.Current.Models.MobilePartyFoodConsumptionModel.NumberOfMenOnMapToEatOneFood;
		float num2 = ((mobileParty.Army != null) ? mobileParty.Army.Parties.Sum((MobileParty s) => s.Food) : mobileParty.Food);
		int a2 = TaleWorlds.Library.MathF.Round((float)numberOfMenOnMapToEatOneFood * num2 / (float)num);
		return TaleWorlds.Library.MathF.Min(b: TaleWorlds.Library.MathF.Max(a2, 30), a: TaleWorlds.Library.MathF.Min(a, partySizeLimit));
	}

	private int CalculateMaxGarrisonSizeTownCanFeed(Town town, bool includeMarketStocks = true)
	{
		SettlementFoodModel settlementFoodModel = Campaign.Current.Models.SettlementFoodModel;
		if (settlementFoodModel == null)
		{
			return 0;
		}
		float resultNumber = settlementFoodModel.CalculateTownFoodStocksChange(town, includeMarketStocks).ResultNumber;
		int num = town.GarrisonParty?.Party.NumberOfRegularMembers ?? 0;
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
		int num4 = 0;
		float num5 = (0f - town.Prosperity) / (float)settlementFoodModel.NumberOfProsperityToEatOneFood;
		float num6 = 1f;
		float num7 = num5 * num6;
		if (resultNumber < num7)
		{
			if (_newlyConqueredFortification != null)
			{
				return (int)MBMath.Map(town.Prosperity, 0f, 8000f, 150f, 300f);
			}
			int num8 = TaleWorlds.Library.MathF.Round(TaleWorlds.Library.MathF.Abs(resultNumber - num7) * (float)settlementFoodModel.NumberOfMenOnGarrisonToEatOneFood / (1f + num2 + num3));
			return Math.Max(num - num8, 0);
		}
		int num9 = TaleWorlds.Library.MathF.Round((TaleWorlds.Library.MathF.Abs(num7) + resultNumber) * (float)settlementFoodModel.NumberOfMenOnGarrisonToEatOneFood / (1f + num2 + num3));
		return num + num9;
	}

	private int CalculateSettlementGarrisonPartySizeLimitWithFoodAndWage(Settlement settlement)
	{
		int a = (int)Campaign.Current.Models.PartySizeLimitModel.CalculateGarrisonPartySizeLimit(settlement).ResultNumber;
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
		int b = TaleWorlds.Library.MathF.Round(num5 * num * num6);
		int b2 = CalculateMaxGarrisonSizeTownCanFeed(settlement.Town);
		return TaleWorlds.Library.MathF.Min(TaleWorlds.Library.MathF.Min(a, b2), b);
	}

	private float GetProsperityEffectForTown(Town town)
	{
		return MBMath.Map(town.Prosperity, 0f, 8000f, 1f, 1.35f);
	}

	private CharacterObject GetASuitableCharacterFromPartyRosterByWeight(TroopRoster troopRoster, bool archersAreHighPriority)
	{
		List<(CharacterObject, float)> list = new List<(CharacterObject, float)>();
		for (int i = 0; i < troopRoster.Count; i++)
		{
			TroopRosterElement elementCopyAtIndex = troopRoster.GetElementCopyAtIndex(i);
			if (!elementCopyAtIndex.Character.IsHero)
			{
				if (archersAreHighPriority && elementCopyAtIndex.Character.IsRanged)
				{
					list.Add((elementCopyAtIndex.Character, elementCopyAtIndex.Number * 4));
				}
				else
				{
					list.Add((elementCopyAtIndex.Character, elementCopyAtIndex.Number));
				}
			}
		}
		if (!list.IsEmpty())
		{
			return MBRandom.ChooseWeighted(list);
		}
		return null;
	}

	private void LeaveTroopsToGarrison(MobileParty mobileParty, Settlement settlement, int numberOfTroopsToLeave, bool archersAreHighPriority)
	{
		TroopRoster troopRoster = TroopRoster.CreateDummyTroopRoster();
		for (int i = 0; i < numberOfTroopsToLeave; i++)
		{
			CharacterObject aSuitableCharacterFromPartyRosterByWeight = GetASuitableCharacterFromPartyRosterByWeight(mobileParty.MemberRoster, archersAreHighPriority);
			if (aSuitableCharacterFromPartyRosterByWeight == null)
			{
				break;
			}
			foreach (TroopRosterElement item in mobileParty.MemberRoster.GetTroopRoster())
			{
				if (item.Character == aSuitableCharacterFromPartyRosterByWeight)
				{
					if (settlement.Town.GarrisonParty == null)
					{
						settlement.AddGarrisonParty();
					}
					if (item.WoundedNumber > 0)
					{
						settlement.Town.GarrisonParty.MemberRoster.AddToCounts(aSuitableCharacterFromPartyRosterByWeight, 1, insertAtFront: false, 1);
						troopRoster.AddToCounts(aSuitableCharacterFromPartyRosterByWeight, 1, insertAtFront: false, 1);
						mobileParty.MemberRoster.AddToCounts(aSuitableCharacterFromPartyRosterByWeight, -1, insertAtFront: false, -1);
					}
					else
					{
						settlement.Town.GarrisonParty.MemberRoster.AddToCounts(aSuitableCharacterFromPartyRosterByWeight, 1);
						troopRoster.AddToCounts(aSuitableCharacterFromPartyRosterByWeight, 1);
						mobileParty.MemberRoster.AddToCounts(aSuitableCharacterFromPartyRosterByWeight, -1);
					}
					break;
				}
			}
		}
		if (troopRoster.Count > 0)
		{
			CampaignEventDispatcher.Instance.OnTroopGivenToSettlement(mobileParty.LeaderHero, settlement, troopRoster);
			ApplyKingdomInfluenceBonusForLeavingTroopToGarrison(mobileParty, settlement, troopRoster);
		}
	}

	private void TakeTroopsFromGarrison(MobileParty mobileParty, Settlement settlement, int numberOfTroopsToTake, bool archersAreHighPriority)
	{
		for (int i = 0; i < numberOfTroopsToTake; i++)
		{
			CharacterObject aSuitableCharacterFromPartyRosterByWeight = GetASuitableCharacterFromPartyRosterByWeight(settlement.Town.GarrisonParty.MemberRoster, archersAreHighPriority);
			if (aSuitableCharacterFromPartyRosterByWeight == null)
			{
				break;
			}
			foreach (TroopRosterElement item in settlement.Town.GarrisonParty.MemberRoster.GetTroopRoster())
			{
				if (item.Character == aSuitableCharacterFromPartyRosterByWeight)
				{
					if (item.Number - item.WoundedNumber > 0)
					{
						mobileParty.MemberRoster.AddToCounts(aSuitableCharacterFromPartyRosterByWeight, 1);
						settlement.Town.GarrisonParty.MemberRoster.AddToCounts(aSuitableCharacterFromPartyRosterByWeight, -1);
					}
					else
					{
						mobileParty.MemberRoster.AddToCounts(aSuitableCharacterFromPartyRosterByWeight, 1, insertAtFront: false, 1);
						settlement.Town.GarrisonParty.MemberRoster.AddToCounts(aSuitableCharacterFromPartyRosterByWeight, -1, insertAtFront: false, -1);
					}
					break;
				}
			}
		}
	}

	private void ApplyKingdomInfluenceBonusForLeavingTroopToGarrison(MobileParty mobileParty, Settlement settlement, TroopRoster troopsToBeTransferred)
	{
		if (mobileParty.LeaderHero == null || settlement.OwnerClan == mobileParty.LeaderHero.Clan)
		{
			return;
		}
		float num = 0f;
		foreach (TroopRosterElement item in troopsToBeTransferred.GetTroopRoster())
		{
			float troopPower = Campaign.Current.Models.MilitaryPowerModel.GetTroopPower(item.Character, BattleSideEnum.Defender, MapEvent.PowerCalculationContext.Siege, 0f);
			num += troopPower * (float)item.Number;
		}
		GainKingdomInfluenceAction.ApplyForLeavingTroopToGarrison(mobileParty.LeaderHero, num / 3f);
	}
}
