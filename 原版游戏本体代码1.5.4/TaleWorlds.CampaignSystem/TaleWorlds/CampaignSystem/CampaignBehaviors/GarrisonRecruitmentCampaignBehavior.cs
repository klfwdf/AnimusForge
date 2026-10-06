using System;
using System.Collections.Generic;
using System.Linq;
using TaleWorlds.CampaignSystem.Roster;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.Library;
using TaleWorlds.Localization;

namespace TaleWorlds.CampaignSystem.CampaignBehaviors;

public class GarrisonRecruitmentCampaignBehavior : CampaignBehaviorBase, IGarrisonRecruitmentBehavior
{
	private struct VolunteerTroop : IComparable
	{
		public Hero OwnerNotable;

		public int NotableVolunteerArrayIndex;

		public int Wage;

		public VolunteerTroop(Hero ownerNotable, int notableVolunteerArrayIndex)
		{
			OwnerNotable = ownerNotable;
			NotableVolunteerArrayIndex = notableVolunteerArrayIndex;
			Wage = Campaign.Current.Models.PartyWageModel.GetCharacterWage(ownerNotable.VolunteerTypes[notableVolunteerArrayIndex]);
		}

		public int CompareTo(object obj)
		{
			VolunteerTroop volunteerTroop = (VolunteerTroop)obj;
			int num = Wage.CompareTo(volunteerTroop.Wage);
			if (num == 0)
			{
				num = volunteerTroop.NotableVolunteerArrayIndex.CompareTo(NotableVolunteerArrayIndex);
			}
			if (num == 0)
			{
				num = volunteerTroop.OwnerNotable.Id.CompareTo(OwnerNotable.Id);
			}
			return num;
		}
	}

	private readonly SortedSet<VolunteerTroop> _volunteerListCache = new SortedSet<VolunteerTroop>();

	public override void SyncData(IDataStore dataStore)
	{
	}

	public override void RegisterEvents()
	{
		CampaignEvents.DailyTickSettlementEvent.AddNonSerializedListener(this, OnDailySettlementTick);
	}

	private static CharacterObject GetBasicTroopForTown(Town town)
	{
		return town.MapFaction.BasicTroop;
	}

	private void OnDailySettlementTick(Settlement settlement)
	{
		if (!settlement.IsFortification)
		{
			return;
		}
		Town town = settlement.Town;
		if (settlement.Party.MapEvent == null && settlement.Party.SiegeEvent == null)
		{
			TickGarrisonChangeForTown(town);
			if (CanSettlementAutoRecruit(settlement))
			{
				TickAutoRecruitmentGarrisonChange(town);
			}
		}
		if (town.GarrisonParty != null)
		{
			HandleGarrisonXpChange(town);
		}
	}

	private void TickAutoRecruitmentGarrisonChange(Town town)
	{
		float resultNumber = GetAutoRecruitmentGarrisonChangeExplainedNumber(town, includeDescriptions: false).ResultNumber;
		if (resultNumber > 0f)
		{
			if (town.GarrisonParty == null)
			{
				town.Owner.Settlement.AddGarrisonParty();
			}
			for (int i = 0; (float)i < resultNumber; i++)
			{
				VolunteerTroop volunteerTroop = _volunteerListCache.ElementAt(i);
				Hero ownerNotable = volunteerTroop.OwnerNotable;
				int notableVolunteerArrayIndex = volunteerTroop.NotableVolunteerArrayIndex;
				town.GarrisonParty.MemberRoster.AddToCounts(ownerNotable.VolunteerTypes[notableVolunteerArrayIndex], 1);
				town.Settlement.OwnerClan.AutoRecruitmentExpenses += Campaign.Current.Models.PartyWageModel.GetTroopRecruitmentCost(ownerNotable.VolunteerTypes[notableVolunteerArrayIndex], town.Settlement.OwnerClan.Leader).RoundedResultNumber;
				ownerNotable.VolunteerTypes[notableVolunteerArrayIndex] = null;
			}
		}
	}

	private void TickGarrisonChangeForTown(Town town)
	{
		int num = (int)GetBaseGarrisonChangeExplainedNumber(town, includeDescriptions: false).ResultNumber;
		if (num > 0)
		{
			if (town.GarrisonParty == null)
			{
				town.Owner.Settlement.AddGarrisonParty();
			}
			town.GarrisonParty.MemberRoster.AddToCounts(GetBasicTroopForTown(town), num);
		}
	}

	private void HandleGarrisonXpChange(Town town)
	{
		int num = Campaign.Current.Models.DailyTroopXpBonusModel.CalculateDailyTroopXpBonus(town);
		if (num <= 0)
		{
			return;
		}
		float num2 = Campaign.Current.Models.DailyTroopXpBonusModel.CalculateGarrisonXpBonusMultiplier(town);
		foreach (TroopRosterElement item in town.GarrisonParty.MemberRoster.GetTroopRoster())
		{
			town.GarrisonParty.MemberRoster.AddXpToTroop(item.Character, TaleWorlds.Library.MathF.Round((float)num * num2 * (float)item.Number));
		}
	}

	private void RepopulateVolunteerListCache(Town town)
	{
		_volunteerListCache.Clear();
		List<Hero> list = new List<Hero>();
		foreach (Hero notable in town.Settlement.Notables)
		{
			if (notable.IsAlive)
			{
				list.Add(notable);
			}
		}
		foreach (Village boundVillage in town.Settlement.BoundVillages)
		{
			if (boundVillage.VillageState != Village.VillageStates.Normal)
			{
				continue;
			}
			foreach (Hero notable2 in boundVillage.Settlement.Notables)
			{
				if (notable2.IsAlive)
				{
					list.Add(notable2);
				}
			}
		}
		foreach (Hero item2 in list)
		{
			int num = Campaign.Current.Models.VolunteerModel.MaximumIndexGarrisonCanRecruitFromHero(town.Settlement, item2);
			for (int i = 0; i < num; i++)
			{
				if (item2.VolunteerTypes[i] != null)
				{
					VolunteerTroop item = new VolunteerTroop(item2, i);
					_volunteerListCache.Add(item);
				}
			}
		}
	}

	private ExplainedNumber GetAutoRecruitmentGarrisonChangeExplainedNumber(Town town, bool includeDescriptions)
	{
		ExplainedNumber maximumDailyAutoRecruitmentCount = Campaign.Current.Models.SettlementGarrisonModel.GetMaximumDailyAutoRecruitmentCount(town, includeDescriptions);
		RepopulateVolunteerListCache(town);
		if ((float)_volunteerListCache.Count < maximumDailyAutoRecruitmentCount.LimitMaxValue)
		{
			maximumDailyAutoRecruitmentCount.LimitMax(_volunteerListCache.Count, new TextObject("{=H1hi1kfF}Max Available Units"));
		}
		int num = town.GarrisonParty?.GetAvailableWageBudget() ?? town.Settlement.GarrisonWagePaymentLimit;
		int num2 = 0;
		int num3 = 0;
		foreach (VolunteerTroop item in _volunteerListCache)
		{
			num2 += item.Wage;
			if (num2 >= num)
			{
				break;
			}
			num3++;
		}
		if ((float)num3 < maximumDailyAutoRecruitmentCount.LimitMaxValue)
		{
			maximumDailyAutoRecruitmentCount.LimitMax(num3, new TextObject("{=7GJOWuUO}Wage Limit"));
		}
		int num4 = ((town.GarrisonParty == null) ? ((int)Campaign.Current.Models.PartySizeLimitModel.CalculateGarrisonPartySizeLimit(town.Settlement).ResultNumber) : (town.GarrisonParty.Party.PartySizeLimit - town.GarrisonParty.Party.NumberOfAllMembers));
		if ((float)num4 < maximumDailyAutoRecruitmentCount.LimitMaxValue)
		{
			maximumDailyAutoRecruitmentCount.LimitMax(num4, new TextObject("{=mp68RYnD}Party Size Limit"));
		}
		return maximumDailyAutoRecruitmentCount;
	}

	private ExplainedNumber GetBaseGarrisonChangeExplainedNumber(Town town, bool includeDescriptions)
	{
		ExplainedNumber result = Campaign.Current.Models.SettlementGarrisonModel.CalculateBaseGarrisonChange(town.Settlement, includeDescriptions);
		int num = ((town.GarrisonParty == null) ? ((int)Campaign.Current.Models.PartySizeLimitModel.CalculateGarrisonPartySizeLimit(town.Settlement).ResultNumber) : (town.GarrisonParty.Party.PartySizeLimit - town.GarrisonParty.Party.NumberOfAllMembers));
		if (result.LimitMaxValue > (float)num)
		{
			result.LimitMax(num, new TextObject("{=mp68RYnD}Party Size Limit"));
		}
		int characterWage = Campaign.Current.Models.PartyWageModel.GetCharacterWage(GetBasicTroopForTown(town));
		int num2 = (town.GarrisonParty?.GetAvailableWageBudget() ?? town.Settlement.GarrisonWagePaymentLimit) / characterWage;
		if (result.LimitMaxValue > (float)num2)
		{
			result.LimitMax(num2, new TextObject("{=7GJOWuUO}Wage Limit"));
		}
		return result;
	}

	public ExplainedNumber GetGarrisonChangeExplainedNumber(Town town)
	{
		ExplainedNumber result = new ExplainedNumber(0f, includeDescriptions: true);
		ExplainedNumber baseGarrisonChangeExplainedNumber = GetBaseGarrisonChangeExplainedNumber(town, includeDescriptions: true);
		result.AddFromExplainedNumber(baseGarrisonChangeExplainedNumber, new TextObject("{=basevalue}Base"));
		if (CanSettlementAutoRecruit(town.Settlement))
		{
			ExplainedNumber autoRecruitmentGarrisonChangeExplainedNumber = GetAutoRecruitmentGarrisonChangeExplainedNumber(town, includeDescriptions: true);
			result.AddFromExplainedNumber(autoRecruitmentGarrisonChangeExplainedNumber, new TextObject("{=Uzsnek6O}Auto Recruitment"));
		}
		return result;
	}

	private bool CanSettlementAutoRecruit(Settlement settlement)
	{
		if (settlement.Town.GarrisonAutoRecruitmentIsEnabled)
		{
			return settlement.Town.FoodChange > 0f;
		}
		return false;
	}
}
