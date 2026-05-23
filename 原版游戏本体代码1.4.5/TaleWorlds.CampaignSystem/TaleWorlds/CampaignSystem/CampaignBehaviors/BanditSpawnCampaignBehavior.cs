using System;
using System.Collections.Generic;
using System.Linq;
using Helpers;
using TaleWorlds.CampaignSystem.Actions;
using TaleWorlds.CampaignSystem.Extensions;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.CampaignSystem.Party.PartyComponents;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.Core;
using TaleWorlds.Library;

namespace TaleWorlds.CampaignSystem.CampaignBehaviors;

public class BanditSpawnCampaignBehavior : CampaignBehaviorBase
{
	private const float BanditStartGoldPerBandit = 10f;

	private const float BanditLongTermGoldPerBandit = 50f;

	private const float HideoutInfestCooldownAfterFightInDays = 1.5f;

	private Dictionary<CultureObject, List<Hideout>> _hideouts = new Dictionary<CultureObject, List<Hideout>>();

	private Dictionary<Settlement, int> _banditCountsPerHideout = new Dictionary<Settlement, int>();

	private float BanditSpawnRadiusAsDays => 0.5f * Campaign.Current.EstimatedAverageBanditPartySpeed * (float)CampaignTime.HoursInDay;

	private float _radiusAroundPlayerPartySquared => MobileParty.MainParty.SeeingRange * MobileParty.MainParty.SeeingRange;

	private float _numberOfMinimumBanditPartiesInAHideoutToInfestIt => Campaign.Current.Models.BanditDensityModel.NumberOfMinimumBanditPartiesInAHideoutToInfestIt;

	private int _numberOfMaxBanditPartiesAroundEachHideout => Campaign.Current.Models.BanditDensityModel.NumberOfMaximumBanditPartiesAroundEachHideout;

	private int _numberOfMaxHideoutsAtEachBanditFaction => Campaign.Current.Models.BanditDensityModel.NumberOfMaximumHideoutsAtEachBanditFaction;

	private int _numberOfInitialHideoutsAtEachBanditFaction => Campaign.Current.Models.BanditDensityModel.NumberOfInitialHideoutsAtEachBanditFaction;

	private int _numberOfMaximumBanditPartiesInEachHideout => Campaign.Current.Models.BanditDensityModel.NumberOfMaximumBanditPartiesInEachHideout;

	private int _numberOfMaxBanditCountPerClanHideout => _numberOfMaxBanditPartiesAroundEachHideout + _numberOfMaximumBanditPartiesInEachHideout;

	public override void RegisterEvents()
	{
		CampaignEvents.MobilePartyCreated.AddNonSerializedListener(this, MobilePartyCreated);
		CampaignEvents.MobilePartyDestroyed.AddNonSerializedListener(this, MobilePartyDestroyed);
		CampaignEvents.SettlementEntered.AddNonSerializedListener(this, OnSettlementEntered);
		CampaignEvents.DailyTickEvent.AddNonSerializedListener(this, DailyTick);
		CampaignEvents.HourlyTickClanEvent.AddNonSerializedListener(this, HourlyTickClan);
		CampaignEvents.OnGameLoadedEvent.AddNonSerializedListener(this, OnGameLoaded);
		CampaignEvents.OnHomeHideoutChangedEvent.AddNonSerializedListener(this, OnHomeHideoutChanged);
		CampaignEvents.OnNewGameCreatedPartialFollowUpEvent.AddNonSerializedListener(this, OnNewGameCreatedPartialFollowUp);
	}

	private void MobilePartyDestroyed(MobileParty party, PartyBase destroyerParty)
	{
		if (party.IsBandit && party.ActualClan != null && (IsBanditFaction(party.ActualClan) || IsLooterFaction(party.ActualClan)))
		{
			int value = 0;
			_banditCountsPerHideout.TryGetValue(party.HomeSettlement, out value);
			_banditCountsPerHideout[party.HomeSettlement] = value - 1;
		}
	}

	private void MobilePartyCreated(MobileParty party)
	{
		if (party.IsBandit && party.ActualClan != null && (IsBanditFaction(party.ActualClan) || IsLooterFaction(party.ActualClan)))
		{
			int value = 0;
			_banditCountsPerHideout.TryGetValue(party.HomeSettlement, out value);
			_banditCountsPerHideout[party.HomeSettlement] = value + 1;
		}
	}

	private void OnGameLoaded(CampaignGameStarter starter)
	{
		CacheHideouts();
		CacheBanditCounts();
	}

	public override void SyncData(IDataStore dataStore)
	{
	}

	private void OnNewGameCreatedPartialFollowUp(CampaignGameStarter starter, int i)
	{
		switch (i)
		{
		case 10:
			CacheHideouts();
			if (_numberOfInitialHideoutsAtEachBanditFaction > 0)
			{
				InitializeInitialHideouts();
			}
			break;
		case 11:
			SpawnBanditsAroundHideoutAtNewGame();
			SpawnLootersAtNewGame();
			CacheBanditCounts();
			break;
		}
	}

	private void CacheHideouts()
	{
		foreach (Hideout item in Hideout.All)
		{
			if (!_hideouts.TryGetValue(item.Settlement.Culture, out var _))
			{
				_hideouts[item.Settlement.Culture] = new List<Hideout>();
			}
			_hideouts[item.Settlement.Culture].Add(item);
		}
	}

	private void CacheBanditCounts()
	{
		_banditCountsPerHideout = new Dictionary<Settlement, int>();
		foreach (MobileParty allBanditParty in MobileParty.AllBanditParties)
		{
			if (IsBanditFaction(allBanditParty.ActualClan) || IsLooterFaction(allBanditParty.ActualClan))
			{
				int value = 0;
				_banditCountsPerHideout.TryGetValue(allBanditParty.HomeSettlement, out value);
				_banditCountsPerHideout[allBanditParty.HomeSettlement] = value + 1;
			}
		}
	}

	public void InitializeInitialHideouts()
	{
		foreach (Clan banditFaction in Clan.BanditFactions)
		{
			if (IsBanditFaction(banditFaction))
			{
				SpawnHideoutsAndBanditsPartiallyOnNewGame(banditFaction);
			}
		}
	}

	private void SpawnHideoutsAndBanditsPartiallyOnNewGame(Clan banditClan)
	{
		for (int i = 0; i < _numberOfInitialHideoutsAtEachBanditFaction; i++)
		{
			FillANewHideoutWithBandits(banditClan);
		}
	}

	public void OnSettlementEntered(MobileParty mobileParty, Settlement settlement, Hero hero)
	{
		CheckForSpawningBanditBoss(settlement, mobileParty);
		if (!Campaign.Current.GameStarted || mobileParty == null || !mobileParty.IsBandit || !settlement.IsHideout)
		{
			return;
		}
		if (!settlement.Hideout.IsSpotted && settlement.Hideout.IsInfested && mobileParty.IsVisible)
		{
			settlement.Hideout.IsSpotted = true;
			settlement.Party.UpdateVisibilityAndInspected(MobileParty.MainParty.Position);
			CampaignEventDispatcher.Instance.OnHideoutSpotted(MobileParty.MainParty.Party, settlement.Party);
		}
		int num = 0;
		foreach (ItemRosterElement item in mobileParty.ItemRoster)
		{
			int num2 = (item.EquipmentElement.Item.IsFood ? MBRandom.RoundRandomized((float)mobileParty.MemberRoster.TotalManCount * ((3f + 6f * MBRandom.RandomFloat) / (float)item.EquipmentElement.Item.Value)) : 0);
			if (item.Amount > num2)
			{
				int num3 = item.Amount - num2;
				num += num3 * item.EquipmentElement.Item.Value;
			}
		}
		if (num > 0)
		{
			if (mobileParty.IsPartyTradeActive)
			{
				mobileParty.PartyTradeGold += (int)(0.25f * (float)num);
			}
			settlement.SettlementComponent.ChangeGold((int)(0.25f * (float)num));
		}
	}

	private void CheckForSpawningBanditBoss(Settlement settlement, MobileParty mobileParty)
	{
		if (settlement.IsHideout && settlement.Hideout.IsSpotted && settlement.Parties.Any((MobileParty x) => x.IsBandit || x.IsBanditBossParty))
		{
			CultureObject culture = settlement.Culture;
			MobileParty mobileParty2 = settlement.Parties.FirstOrDefault((MobileParty x) => x.IsBanditBossParty);
			if (mobileParty2 == null)
			{
				AddBossParty(settlement, culture);
			}
			else if (!mobileParty2.MemberRoster.Contains(culture.BanditBoss))
			{
				mobileParty2.MemberRoster.AddToCounts(culture.BanditBoss, 1);
			}
		}
	}

	private void AddBossParty(Settlement settlement, CultureObject culture)
	{
		PartyTemplateObject banditBossPartyTemplate = culture.BanditBossPartyTemplate;
		if (banditBossPartyTemplate != null)
		{
			AddBanditToHideout(settlement.Hideout, banditBossPartyTemplate, isBanditBossParty: true).Ai.DisableAi();
		}
	}

	public void DailyTick()
	{
		if (_numberOfMaxHideoutsAtEachBanditFaction > 0)
		{
			AddNewHideouts();
		}
		foreach (MobileParty allBanditParty in MobileParty.AllBanditParties)
		{
			if (!allBanditParty.IsPartyTradeActive)
			{
				continue;
			}
			allBanditParty.PartyTradeGold = (int)((double)allBanditParty.PartyTradeGold * 0.95 + (double)(50f * (float)allBanditParty.Party.MemberRoster.TotalManCount * 0.05f));
			if (!(MBRandom.RandomFloat < 0.03f) || allBanditParty.MapEvent == null)
			{
				continue;
			}
			foreach (ItemObject item in Items.All)
			{
				if (item.IsFood)
				{
					int num = (IsLooterFaction(allBanditParty.MapFaction) ? 8 : 16);
					int num2 = MBRandom.RoundRandomized((float)allBanditParty.MemberRoster.TotalManCount * (1f / (float)item.Value) * (float)num * MBRandom.RandomFloat * MBRandom.RandomFloat * MBRandom.RandomFloat * MBRandom.RandomFloat);
					if (num2 > 0)
					{
						allBanditParty.ItemRoster.AddToCounts(item, num2);
					}
				}
			}
		}
	}

	private void HourlyTickClan(Clan clan)
	{
		if (Campaign.Current.IsNight && clan.IsBanditFaction)
		{
			if (IsLooterFaction(clan))
			{
				SpawnLooters(clan, 0.07f, uniformDistribution: false);
			}
			else if (IsBanditFaction(clan))
			{
				SpawnBanditsAroundHideout(clan, 0.1f);
			}
		}
	}

	private void SpawnBanditsAroundHideout(Clan clan, float ratio)
	{
		int count = clan.WarPartyComponents.Count;
		int num = MBRandom.RoundRandomized((float)(GetInfestedHideoutCount(clan) * _numberOfMaxBanditCountPerClanHideout - count) * ratio);
		for (int i = 0; i < num; i++)
		{
			SpawnBanditParty(clan);
		}
	}

	private void SpawnLooters(Clan clan, float ratio, bool uniformDistribution)
	{
		int count = clan.WarPartyComponents.Count;
		int num = MBRandom.RoundRandomized((float)(GetCurrentLimitForLooters(clan) - count) * ratio);
		for (int i = 0; i < num; i++)
		{
			SpawnLooterParty(clan, uniformDistribution);
		}
	}

	private void AddNewHideouts()
	{
		List<((Clan, int), float)> list = new List<((Clan, int), float)>();
		foreach (Clan banditFaction in Clan.BanditFactions)
		{
			if (IsBanditFaction(banditFaction))
			{
				int infestedHideoutCount = GetInfestedHideoutCount(banditFaction);
				if (infestedHideoutCount < _numberOfMaxHideoutsAtEachBanditFaction)
				{
					list.Add(((banditFaction, infestedHideoutCount), 1f - (float)infestedHideoutCount / (float)_numberOfMaxHideoutsAtEachBanditFaction));
				}
			}
		}
		var (clan, num) = MBRandom.ChooseWeighted(list, out var _);
		if (clan != null)
		{
			float num2 = (((float)num < (float)_numberOfMaxHideoutsAtEachBanditFaction * 0.5f) ? (0.2f + (float)(_numberOfMaxHideoutsAtEachBanditFaction - num) * 0.1f) : (0.1f + 0.5f * TaleWorlds.Library.MathF.Pow(1f - 0.25f * ((float)num - (float)_numberOfMaxHideoutsAtEachBanditFaction * 0.5f), 3f)));
			if (MBRandom.RandomFloat < num2)
			{
				FillANewHideoutWithBandits(clan);
			}
		}
	}

	private void FillANewHideoutWithBandits(Clan faction)
	{
		Hideout hideout = SelectANonInfestedHideoutOfSameCultureByWeight(faction);
		if (hideout != null)
		{
			for (int i = 0; (float)i < _numberOfMinimumBanditPartiesInAHideoutToInfestIt; i++)
			{
				AddBanditToHideout(hideout);
			}
		}
	}

	public MobileParty AddBanditToHideout(Hideout hideoutComponent, PartyTemplateObject overridenPartyTemplate = null, bool isBanditBossParty = false)
	{
		if (hideoutComponent.Owner.Settlement.Culture.IsBandit)
		{
			Clan clan = null;
			foreach (Clan banditFaction in Clan.BanditFactions)
			{
				if (hideoutComponent.Owner.Settlement.Culture == banditFaction.Culture && (IsBanditFaction(banditFaction) || IsLooterFaction(banditFaction)))
				{
					clan = banditFaction;
				}
			}
			PartyTemplateObject pt = overridenPartyTemplate ?? clan.DefaultPartyTemplate;
			MobileParty mobileParty = BanditPartyComponent.CreateBanditParty(clan.StringId + "_1", clan, hideoutComponent, isBanditBossParty, pt, hideoutComponent.Owner.Settlement.GatePosition);
			InitializeBanditParty(mobileParty, clan);
			mobileParty.SetMoveGoToSettlement(hideoutComponent.Owner.Settlement, mobileParty.NavigationCapability, isTargetingThePort: false);
			mobileParty.RecalculateShortTermBehavior();
			EnterSettlementAction.ApplyForParty(mobileParty, hideoutComponent.Owner.Settlement);
			return mobileParty;
		}
		return null;
	}

	private Hideout SelectBanditHideout(Clan faction)
	{
		MBList<(Hideout, float)> mBList = new MBList<(Hideout, float)>();
		foreach (Hideout item in Hideout.All)
		{
			if (item.Settlement.Culture == faction.Culture && item.IsInfested)
			{
				mBList.Add((item, GetSpawnChanceInSettlement(item.Settlement)));
			}
		}
		if (mBList.Count != 0)
		{
			return MBRandom.ChooseWeighted(mBList);
		}
		return SelectAHideoutByCheckingCultureAndInfestedState(faction);
	}

	private float GetSpawnChanceInSettlement(Settlement settlement)
	{
		if (_banditCountsPerHideout.ContainsKey(settlement) && _banditCountsPerHideout[settlement] != 0)
		{
			return 1f / TaleWorlds.Library.MathF.Pow(_banditCountsPerHideout[settlement], 2f);
		}
		return 1f;
	}

	private void OnHomeHideoutChanged(BanditPartyComponent banditPartyComponent, Hideout oldHomeHideout)
	{
		int value = 0;
		_banditCountsPerHideout.TryGetValue(oldHomeHideout.Settlement, out value);
		_banditCountsPerHideout[oldHomeHideout.Settlement] = value - 1;
		value = 0;
		_banditCountsPerHideout.TryGetValue(banditPartyComponent.HomeSettlement, out value);
		_banditCountsPerHideout[banditPartyComponent.HomeSettlement] = value + 1;
	}

	private Hideout SelectAHideoutByCheckingCultureAndInfestedState(Clan faction)
	{
		List<Hideout> list = new List<Hideout>();
		bool flag = false;
		bool flag2 = false;
		foreach (Hideout item in Hideout.All)
		{
			bool flag3 = item.Settlement.Culture == faction.Culture;
			bool isInfested = item.IsInfested;
			if (!flag2 && flag3)
			{
				flag2 = true;
				list.Clear();
			}
			if (flag2 && !flag && isInfested)
			{
				flag = true;
				list.Clear();
			}
			if ((!flag2 || flag3) && (!flag || isInfested))
			{
				list.Add(item);
			}
		}
		return list.GetRandomElement();
	}

	private Hideout SelectANonInfestedHideoutOfSameCultureByWeight(Clan faction)
	{
		float averageDistanceBetweenClosestTwoTownsWithNavigationType = Campaign.Current.GetAverageDistanceBetweenClosestTwoTownsWithNavigationType(MobileParty.NavigationType.Default);
		float num = averageDistanceBetweenClosestTwoTownsWithNavigationType * 0.33f * averageDistanceBetweenClosestTwoTownsWithNavigationType * 0.33f;
		List<(Hideout, float)> list = new List<(Hideout, float)>();
		foreach (Hideout item in Hideout.All)
		{
			if (item.IsInfested || item.Settlement.Culture != faction.Culture)
			{
				continue;
			}
			int num2 = 1;
			if (item.Settlement.LastThreatTime.ElapsedDaysUntilNow > 1.5f)
			{
				float num3 = Campaign.MapDiagonalSquared;
				float num4 = Campaign.MapDiagonalSquared;
				foreach (Hideout item2 in Hideout.All)
				{
					if (item != item2 && item2.IsInfested)
					{
						float num5 = item.Settlement.Position.DistanceSquared(item2.Settlement.Position);
						if (item.Settlement.Culture == item2.Settlement.Culture && num5 < num3)
						{
							num3 = num5;
						}
						if (num5 < num4)
						{
							num4 = num5;
						}
					}
					num2 = (int)TaleWorlds.Library.MathF.Max(averageDistanceBetweenClosestTwoTownsWithNavigationType * 0.015f, num3 / num + averageDistanceBetweenClosestTwoTownsWithNavigationType * 0.076f * (num4 / num));
				}
			}
			list.Add((item, num2));
		}
		return MBRandom.ChooseWeighted(list);
	}

	public void SpawnBanditsAroundHideoutAtNewGame()
	{
		foreach (Clan banditFaction in Clan.BanditFactions)
		{
			if (IsBanditFaction(banditFaction))
			{
				SpawnBanditsAroundHideout(banditFaction, MBRandom.RandomFloatRanged(0.5f, 0.75f));
			}
		}
	}

	public void SpawnLootersAtNewGame()
	{
		foreach (Clan banditFaction in Clan.BanditFactions)
		{
			if (IsLooterFaction(banditFaction))
			{
				SpawnLooters(banditFaction, MBRandom.RandomFloatRanged(0.5f, 0.75f), uniformDistribution: true);
			}
		}
	}

	private void SpawnLooterParty(Clan selectedFaction, bool uniformDistribution)
	{
		Settlement settlement = SelectARandomSettlementForLooterParty(uniformDistribution);
		CampaignVec2 spawnPositionAroundSettlement = GetSpawnPositionAroundSettlement(selectedFaction, settlement);
		MobileParty mobileParty = BanditPartyComponent.CreateLooterParty(selectedFaction.StringId + "_1", selectedFaction, settlement, isBossParty: false, selectedFaction.DefaultPartyTemplate, spawnPositionAroundSettlement);
		InitializeBanditParty(mobileParty, selectedFaction);
		mobileParty.SetMovePatrolAroundPoint(mobileParty.Position, MobileParty.NavigationType.Default);
	}

	private void SpawnBanditParty(Clan selectedFaction)
	{
		Hideout hideout = SelectBanditHideout(selectedFaction);
		CampaignVec2 spawnPositionAroundSettlement = GetSpawnPositionAroundSettlement(selectedFaction, hideout.Settlement);
		MobileParty mobileParty = BanditPartyComponent.CreateBanditParty(selectedFaction.StringId + "_1", selectedFaction, hideout, isBossParty: false, selectedFaction.DefaultPartyTemplate, spawnPositionAroundSettlement);
		InitializeBanditParty(mobileParty, selectedFaction);
		mobileParty.SetMovePatrolAroundPoint(mobileParty.Position, mobileParty.NavigationCapability);
	}

	private static bool IsLooterFaction(IFaction faction)
	{
		if (!faction.Culture.CanHaveSettlement && !faction.HasNavalNavigationCapability)
		{
			return faction.StringId != "deserters";
		}
		return false;
	}

	private float GetSpawnRadiusForClan(Clan selectedFaction)
	{
		return BanditSpawnRadiusAsDays * (IsLooterFaction(selectedFaction) ? 1.5f : 1f);
	}

	private int GetInfestedHideoutCount(Clan banditFaction)
	{
		int num = 0;
		foreach (Hideout item in _hideouts[banditFaction.Culture])
		{
			if (item.IsInfested && item.MapFaction == banditFaction)
			{
				num++;
			}
		}
		return num;
	}

	private int GetCurrentLimitForLooters(Clan clan)
	{
		return Math.Min(Hideout.All.Count((Hideout x) => x.IsInfested) * 7, Campaign.Current.Models.BanditDensityModel.GetMaxSupportedNumberOfLootersForClan(clan));
	}

	private Settlement SelectARandomSettlementForLooterParty(bool uniformDistribution)
	{
		MBList<(Settlement, float)> mBList = new MBList<(Settlement, float)>();
		foreach (Settlement item in Settlement.All)
		{
			if (item.IsTown || item.IsVillage)
			{
				mBList.Add((item, GetSpawnChanceInSettlement(item)));
			}
		}
		return MBRandom.ChooseWeighted(mBList);
	}

	private void GiveFoodToBanditParty(MobileParty banditParty)
	{
		int num = (IsLooterFaction(banditParty.MapFaction) ? 8 : 16);
		foreach (ItemObject item in Items.All)
		{
			if (item.IsFood)
			{
				int num2 = MBRandom.RoundRandomized((float)banditParty.MemberRoster.TotalManCount * (1f / (float)item.Value) * (float)num * MBRandom.RandomFloat * MBRandom.RandomFloat * MBRandom.RandomFloat * MBRandom.RandomFloat);
				if (num2 > 0)
				{
					banditParty.ItemRoster.AddToCounts(item, num2);
				}
			}
		}
	}

	private CampaignVec2 GetSpawnPositionAroundSettlement(Clan clan, Settlement settlement)
	{
		CampaignVec2 campaignVec = NavigationHelper.FindPointAroundPosition(settlement.GatePosition, MobileParty.NavigationType.Default, GetSpawnRadiusForClan(clan));
		if (campaignVec.DistanceSquared(MobileParty.MainParty.Position) < _radiusAroundPlayerPartySquared)
		{
			for (int i = 0; i < 15; i++)
			{
				CampaignVec2 campaignVec2 = NavigationHelper.FindReachablePointAroundPosition(campaignVec, MobileParty.NavigationType.Default, GetSpawnRadiusForClan(clan));
				if (NavigationHelper.IsPositionValidForNavigationType(campaignVec2, MobileParty.NavigationType.Default))
				{
					float landRatio;
					float num = DistanceHelper.FindClosestDistanceFromMobilePartyToPoint(MobileParty.MainParty, campaignVec2, MobileParty.NavigationType.Default, out landRatio);
					if (num * num > _radiusAroundPlayerPartySquared)
					{
						campaignVec = campaignVec2;
						break;
					}
				}
			}
		}
		return campaignVec;
	}

	private bool IsBanditFaction(Clan clan)
	{
		if (!clan.HasNavalNavigationCapability && clan.IsBanditFaction)
		{
			return clan.Culture.CanHaveSettlement;
		}
		return false;
	}

	private void InitializeBanditParty(MobileParty banditParty, Clan faction)
	{
		banditParty.Party.SetVisualAsDirty();
		banditParty.ActualClan = faction;
		banditParty.Aggressiveness = 1f - 0.2f * MBRandom.RandomFloat;
		CreatePartyTrade(banditParty);
		GiveFoodToBanditParty(banditParty);
	}

	private static void CreatePartyTrade(MobileParty banditParty)
	{
		int initialGold = (int)(10f * (float)banditParty.Party.MemberRoster.TotalManCount * (0.5f + 1f * MBRandom.RandomFloat));
		banditParty.InitializePartyTrade(initialGold);
	}
}
