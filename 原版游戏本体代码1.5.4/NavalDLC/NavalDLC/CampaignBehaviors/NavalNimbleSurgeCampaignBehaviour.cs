using System.Collections.Generic;
using Helpers;
using NavalDLC.CharacterDevelopment;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.CampaignSystem.Settlements;

namespace NavalDLC.CampaignBehaviors;

public class NavalNimbleSurgeCampaignBehaviour : CampaignBehaviorBase
{
	private Dictionary<MobileParty, Dictionary<Settlement, CampaignTime>> _lastTimeEntered = new Dictionary<MobileParty, Dictionary<Settlement, CampaignTime>>();

	public override void RegisterEvents()
	{
		CampaignEvents.SettlementEntered.AddNonSerializedListener(this, OnSettlementEntered);
		CampaignEvents.MobilePartyDestroyed.AddNonSerializedListener(this, OnMobilePartyDestroyed);
	}

	private void OnMobilePartyDestroyed(MobileParty mobileParty, PartyBase destroyerParty)
	{
		_lastTimeEntered.Remove(mobileParty);
	}

	public override void SyncData(IDataStore dataStore)
	{
		DoCleanUp();
		dataStore.SyncData("_lastTimeEntered", ref _lastTimeEntered);
	}

	private void OnSettlementEntered(MobileParty mobileParty, Settlement settlement, Hero hero)
	{
		if (mobileParty == null || !mobileParty.IsCaravan || !mobileParty.HasNavalNavigationCapability || !settlement.IsFortification || settlement.Town.Governor == null || settlement.Town.BuildingsInProgress.Count <= 0)
		{
			return;
		}
		Town town = settlement.Town;
		if (town.Governor.GetPerkValue(NavalPerks.Shipmaster.FavorableTide) && (!_lastTimeEntered.ContainsKey(mobileParty) || !_lastTimeEntered[mobileParty].ContainsKey(settlement) || _lastTimeEntered[mobileParty][settlement].ElapsedDaysUntilNow > 1f))
		{
			if (!_lastTimeEntered.ContainsKey(mobileParty))
			{
				_lastTimeEntered[mobileParty] = new Dictionary<Settlement, CampaignTime>();
			}
			_lastTimeEntered[mobileParty][settlement] = CampaignTime.Now;
			float secondaryBonus = NavalPerks.Shipmaster.FavorableTide.SecondaryBonus;
			town.CurrentBuilding.BuildingProgress += secondaryBonus;
			BuildingHelper.CheckIfBuildingIsComplete(town.CurrentBuilding);
		}
	}

	private void DoCleanUp()
	{
		foreach (KeyValuePair<MobileParty, Dictionary<Settlement, CampaignTime>> item in _lastTimeEntered)
		{
			List<Settlement> list = new List<Settlement>();
			foreach (KeyValuePair<Settlement, CampaignTime> item2 in item.Value)
			{
				if (item2.Value.ElapsedDaysUntilNow > 1f)
				{
					list.Add(item2.Key);
				}
			}
			foreach (Settlement item3 in list)
			{
				item.Value.Remove(item3);
			}
		}
		List<MobileParty> list2 = new List<MobileParty>();
		foreach (KeyValuePair<MobileParty, Dictionary<Settlement, CampaignTime>> item4 in _lastTimeEntered)
		{
			if (_lastTimeEntered[item4.Key] == null || _lastTimeEntered[item4.Key].Count == 0)
			{
				list2.Add(item4.Key);
			}
		}
		foreach (MobileParty item5 in list2)
		{
			_lastTimeEntered.Remove(item5);
		}
	}
}
