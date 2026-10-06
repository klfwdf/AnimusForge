using System;
using System.Collections.Generic;
using Helpers;
using NavalDLC.CharacterDevelopment;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Actions;
using TaleWorlds.CampaignSystem.Naval;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.Core;
using TaleWorlds.Library;
using TaleWorlds.ObjectSystem;

namespace NavalDLC.CampaignBehaviors;

public class FishingPartyCampaignBehavior : CampaignBehaviorBase
{
	private ItemObject Fish;

	private int[] _invalidFishingTerrainTypes;

	private const float FishingZoneThreatClosenessDistanceSquared = 16f;

	private const float MinDistanceForInteractionSquared = 0.01f;

	private const int MinFishCountToDropOff = 5;

	private const float MinFishingTimeInHours = 8f;

	private const float MaxFishingTimeInHours = 10f;

	private const float MinRoamingTimeInDays = 1f;

	private const float MaxRoamingTimeInDays = 3f;

	private const float MaxFishToCatchPerHour = 1f;

	private const float MaxDistanceBetweenPointsInFishingSpots = 36f;

	private const float MinDistanceBetweenPointsInFishingSpots = 12f;

	private bool CanHaveFishingParties(Village village)
	{
		if (village.VillageType == DefaultVillageTypes.Fisherman && village.Settlement.Culture.FishingPartyTemplate != null)
		{
			return village.Settlement.HasPort;
		}
		return false;
	}

	private bool CanCreateFishingParties(Village village)
	{
		bool result = false;
		if (village.VillageState == Village.VillageStates.Normal && village.Settlement.Party.MapEvent == null && village.Hearth > (float)Campaign.Current.Models.PartySizeLimitModel.MinimumNumberOfVillagersAtVillagerParty && CanHaveFishingParties(village) && GetIdealFishingPartyCount(village) > village.FishingParties().Count)
		{
			int num = 0;
			for (int i = 0; i < village.Owner.ItemRoster.Count; i++)
			{
				num += village.Owner.ItemRoster[i].Amount;
			}
			result = num < village.GetWarehouseCapacity();
		}
		return result;
	}

	private float EndingRoamingChance(FishingPartyComponent fishingParty)
	{
		if (fishingParty.MobileParty.TotalWeightCarried >= (float)fishingParty.MobileParty.InventoryCapacity)
		{
			return 1f;
		}
		float num = 9f;
		float elapsedHoursUntilNow = fishingParty.RoamingStartTime.ElapsedHoursUntilNow;
		if (elapsedHoursUntilNow < 1f * (float)CampaignTime.HoursInDay - num * 0.5f)
		{
			return 0f;
		}
		if (elapsedHoursUntilNow > 3f * (float)CampaignTime.HoursInDay)
		{
			return 1f;
		}
		int num2 = TaleWorlds.Library.MathF.Round((3f * (float)CampaignTime.HoursInDay - elapsedHoursUntilNow) / num);
		return 1f / (float)(num2 + 1);
	}

	private float EndingFishingChance(FishingPartyComponent fishingParty)
	{
		if (fishingParty.MobileParty.TotalWeightCarried >= (float)fishingParty.MobileParty.InventoryCapacity)
		{
			return 1f;
		}
		float elapsedHoursUntilNow = fishingParty.FishingWaitStartTime.ElapsedHoursUntilNow;
		if (elapsedHoursUntilNow < 7.5f)
		{
			return 0f;
		}
		if (elapsedHoursUntilNow > 10f)
		{
			return 1f;
		}
		int num = TaleWorlds.Library.MathF.Round(10f - elapsedHoursUntilNow);
		return 1f / (float)(num + 1);
	}

	private int GetIdealFishingPartySize(Village village)
	{
		return Campaign.Current.Models.PartySizeLimitModel.GetIdealVillagerPartySize(village);
	}

	private int GetIdealFishingPartyCount(Village village)
	{
		int num = Math.Min(2, village.GetHearthLevel() + 1);
		Hero governor = village.Bound.Town.Governor;
		if (governor != null && governor.GetPerkValue(NavalPerks.Shipmaster.TheCorsairsEdge))
		{
			num += TaleWorlds.Library.MathF.Round(NavalPerks.Shipmaster.TheCorsairsEdge.SecondaryBonus);
		}
		return num;
	}

	public override void RegisterEvents()
	{
		CampaignEvents.OnNewGameCreatedPartialFollowUpEndEvent.AddNonSerializedListener(this, OnNewGameCreatedPartialFollowUpEnd);
		CampaignEvents.HourlyTickPartyEvent.AddNonSerializedListener(this, OnHourlyTickParty);
		CampaignEvents.DailyTickSettlementEvent.AddNonSerializedListener(this, OnDailySettlementTick);
		CampaignEvents.OnGameEarlyLoadedEvent.AddNonSerializedListener(this, OnGameEarlyLoaded);
	}

	private void InitializeCachedData()
	{
		Fish = MBObjectManager.Instance.GetObject<ItemObject>("fish");
		MBList<int> mBList = Campaign.Current.Models.PartyNavigationModel.GetInvalidTerrainTypesForNavigationType(MobileParty.NavigationType.Naval).ToMBList();
		mBList.Add(11);
		mBList.Add(22);
		mBList.Add(25);
		_invalidFishingTerrainTypes = mBList.ToArray();
	}

	private void OnGameEarlyLoaded(CampaignGameStarter starter)
	{
		InitializeCachedData();
		if (!MBSaveLoad.IsUpdatingGameVersion || !MBSaveLoad.LastLoadedGameVersion.IsOlderThan(ApplicationVersion.FromString("v1.3.9.103870")))
		{
			return;
		}
		foreach (MobileParty allVillagerParty in MobileParty.AllVillagerParties)
		{
			if (allVillagerParty.IsFishingParty())
			{
				int itemNumber = allVillagerParty.ItemRoster.GetItemNumber(Fish);
				int num = (int)((float)allVillagerParty.InventoryCapacity / (Fish.Weight + 5f));
				if (itemNumber > num)
				{
					allVillagerParty.ItemRoster.AddToCounts(Fish, num - itemNumber);
				}
			}
		}
	}

	private void OnDailySettlementTick(Settlement settlement)
	{
		Village village = settlement.Village;
		if (village != null && MBRandom.RandomFloat > 0.5f && CanCreateFishingParties(village))
		{
			MobileParty mobileParty = FishingPartyComponent.CreateFishingParty(settlement.OwnerClan.StringId + "_1", village);
			village.Hearth = TaleWorlds.Library.MathF.Max(0f, village.Hearth - (float)((mobileParty.MemberRoster.TotalManCount + 1) / 2));
			StartRoaming(mobileParty.PartyComponent as FishingPartyComponent);
		}
	}

	private void OnNewGameCreatedPartialFollowUpEnd(CampaignGameStarter starter)
	{
		InitializeCachedData();
		foreach (Village item in Village.All)
		{
			if (CanHaveFishingParties(item))
			{
				OnDailySettlementTick(item.Settlement);
			}
		}
	}

	public override void SyncData(IDataStore dataStore)
	{
	}

	private void TryReinforceParty(FishingPartyComponent fishingParty)
	{
		if (fishingParty.HomeSettlement.Party.MapEvent != null || fishingParty.Village.VillageState != Village.VillageStates.Normal)
		{
			return;
		}
		int num = GetIdealFishingPartySize(fishingParty.Village) - fishingParty.MobileParty.MemberRoster.TotalManCount;
		if (num > (int)fishingParty.Village.Hearth)
		{
			num = (int)fishingParty.Village.Hearth;
		}
		if (num > 0)
		{
			if (num > (int)fishingParty.HomeSettlement.Village.Hearth)
			{
				num = (int)fishingParty.HomeSettlement.Village.Hearth;
			}
			fishingParty.HomeSettlement.Village.Hearth -= (num + 1) / 2;
			CharacterObject character = fishingParty.HomeSettlement.Culture.FishingPartyTemplate.Stacks.GetRandomElement().Character;
			fishingParty.MobileParty.MemberRoster.AddToCounts(character, num);
		}
		foreach (Ship ship in fishingParty.MobileParty.Ships)
		{
			if (ship.HitPoints < ship.MaxHitPoints)
			{
				RepairShipAction.ApplyForFree(ship);
			}
		}
	}

	private void CatchFish(FishingPartyComponent fishingParty)
	{
		MobileParty mobileParty = fishingParty.MobileParty;
		int num = 0;
		float num2 = 1f * MBRandom.RandomFloat;
		Hero governor = fishingParty.Village.Bound.Town.Governor;
		if (governor != null && governor.GetPerkValue(NavalPerks.Shipmaster.MasterAngler))
		{
			num2 += num2 * NavalPerks.Shipmaster.MasterAngler.SecondaryBonus;
		}
		num = MBRandom.RoundRandomized(num2);
		int num3 = (int)(((float)mobileParty.InventoryCapacity - mobileParty.TotalWeightCarried) / (Fish.Weight + 0.1f));
		if (num3 < num)
		{
			num = num3;
		}
		if (num > 0)
		{
			mobileParty.ItemRoster.AddToCounts(Fish, num);
		}
	}

	private void OnHourlyTickParty(MobileParty party)
	{
		if (party.MapEvent != null || !(party.PartyComponent is FishingPartyComponent fishingPartyComponent))
		{
			return;
		}
		if (party.ShortTermBehavior != party.DefaultBehavior)
		{
			fishingPartyComponent.IsFishing = false;
			if (party.IsFleeing() && party.ShortTermTargetParty != null && party.ShortTermTargetParty.Position.DistanceSquared(party.TargetPosition) < 16f)
			{
				StartRoaming(fishingPartyComponent);
			}
		}
		else if (party.DefaultBehavior == AiBehavior.Hold)
		{
			if (fishingPartyComponent.IsRoaming)
			{
				if (fishingPartyComponent.IsFishing)
				{
					CatchFish(fishingPartyComponent);
					if (MBRandom.RandomFloat < EndingFishingChance(fishingPartyComponent))
					{
						if (MBRandom.RandomFloat < EndingRoamingChance(fishingPartyComponent))
						{
							StartDropOff(fishingPartyComponent);
						}
						else
						{
							GoToNewFishingPoint(fishingPartyComponent);
						}
					}
				}
				else if (MBRandom.RandomFloat < EndingRoamingChance(fishingPartyComponent) && fishingPartyComponent.MobileParty.ItemRoster.Count > 5)
				{
					StartDropOff(fishingPartyComponent);
				}
				else
				{
					GoToNewFishingPoint(fishingPartyComponent);
				}
			}
			else
			{
				StartDropOff(fishingPartyComponent);
			}
		}
		else
		{
			if (party.DefaultBehavior != AiBehavior.GoToPoint || !((party.Position - party.TargetPosition).LengthSquared < 0.01f))
			{
				return;
			}
			party.SetMoveModeHold();
			if ((party.Position - fishingPartyComponent.Village.Settlement.PortPosition).LengthSquared < 0.01f)
			{
				OnDropOff(fishingPartyComponent);
				if (fishingPartyComponent.Village.FishingParties().Count > GetIdealFishingPartyCount(fishingPartyComponent.Village) && !party.IsVisible)
				{
					DestroyPartyAction.Apply(null, party);
				}
				else
				{
					StartRoaming(fishingPartyComponent);
				}
			}
			else if (!fishingPartyComponent.IsRoaming)
			{
				Debug.FailedAssert("fishing ship not roaming nor dropping off", "C:\\BuildAgent\\work\\mb3\\Source\\Bannerlord\\NavalDLC\\CampaignBehaviors\\FishingPartyCampaignBehavior.cs", "OnHourlyTickParty", 452);
				StartDropOff(fishingPartyComponent);
			}
			else
			{
				fishingPartyComponent.IsFishing = true;
			}
		}
	}

	private void OnDropOff(FishingPartyComponent fishingParty)
	{
		TryReinforceParty(fishingParty);
		fishingParty.Village.Settlement.ItemRoster.Add(fishingParty.MobileParty.ItemRoster);
		fishingParty.MobileParty.ItemRoster.Clear();
		Town town = fishingParty.Village.Bound.Town;
		Hero governor = town.Governor;
		if (governor != null && governor.GetPerkValue(NavalPerks.Shipmaster.TheHelmsmansShield))
		{
			town.Prosperity += NavalPerks.Shipmaster.TheHelmsmansShield.SecondaryBonus;
		}
		Hero governor2 = town.Governor;
		if (governor2 != null && governor2.GetPerkValue(NavalPerks.Shipmaster.RavenEye))
		{
			town.Loyalty += NavalPerks.Shipmaster.RavenEye.SecondaryBonus;
		}
	}

	private void StartRoaming(FishingPartyComponent fishingParty)
	{
		fishingParty.IsRoaming = true;
		fishingParty.IsFishing = false;
		GoToNewFishingPoint(fishingParty);
	}

	private void StartDropOff(FishingPartyComponent fishingParty)
	{
		fishingParty.IsFishing = false;
		fishingParty.IsRoaming = false;
		CampaignVec2 portPosition = fishingParty.Village.Settlement.PortPosition;
		fishingParty.MobileParty.SetMoveGoToPoint(portPosition, MobileParty.NavigationType.Naval);
	}

	private void GoToNewFishingPoint(FishingPartyComponent fishingParty)
	{
		fishingParty.IsFishing = false;
		CampaignVec2 portPosition = fishingParty.Village.Settlement.PortPosition;
		int num = 20;
		do
		{
			portPosition = NavigationHelper.FindReachablePointAroundPosition(fishingParty.Village.Settlement.PortPosition, _invalidFishingTerrainTypes, 36f, 12f, useUniformDistribution: true);
			num--;
		}
		while (num > 0 && portPosition.Distance(fishingParty.MobileParty.Position) < 12f);
		fishingParty.MobileParty.SetMoveGoToPoint(portPosition, MobileParty.NavigationType.Naval);
	}

	[CommandLineFunctionality.CommandLineArgumentFunction("show_drop_off", "campaign")]
	public static string show_drop_off(List<string> strings)
	{
		return Village.All.MinBy((Village v) => v.Settlement.Position.DistanceSquared(MobileParty.MainParty.Position)).Name.ToString();
	}
}
