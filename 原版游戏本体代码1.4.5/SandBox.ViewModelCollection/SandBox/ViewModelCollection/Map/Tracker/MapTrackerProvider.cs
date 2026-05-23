using System;
using System.Collections.Generic;
using System.Linq;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Actions;
using TaleWorlds.CampaignSystem.Map;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.CampaignSystem.ViewModelCollection.Map.Tracker;
using TaleWorlds.Library;

namespace SandBox.ViewModelCollection.Map.Tracker;

public class MapTrackerProvider
{
	private class TrackerContainer
	{
		private readonly Dictionary<ITrackableCampaignObject, MapTrackerItemVM> _trackers;

		public OnTrackerAddedOrRemovedDelegate OnTrackerAddedOrRemoved;

		public TrackerContainer()
		{
			_trackers = new Dictionary<ITrackableCampaignObject, MapTrackerItemVM>();
		}

		public MapTrackerItemVM[] GetTrackers()
		{
			return _trackers.Values.ToArray();
		}

		public bool HasTrackerFor(ITrackableCampaignObject trackable)
		{
			return GetTrackerFor(trackable) != null;
		}

		public MapTrackerItemVM GetTrackerFor(ITrackableCampaignObject trackable)
		{
			if (_trackers.TryGetValue(trackable, out var value))
			{
				return value;
			}
			return null;
		}

		public void AddTracker(MapTrackerItemVM tracker)
		{
			if (_trackers.ContainsKey(tracker.TrackedObject))
			{
				Debug.FailedAssert("Trying to add a tracker that was already added", "C:\\BuildAgent\\work\\mb3\\Source\\Bannerlord\\SandBox.ViewModelCollection\\Map\\Tracker\\MapTrackerProvider.cs", "AddTracker", 54);
				return;
			}
			_trackers.Add(tracker.TrackedObject, tracker);
			OnTrackerAddedOrRemoved?.Invoke(tracker, added: true);
		}

		public void RemoveTracker(MapTrackerItemVM tracker)
		{
			if (!_trackers.ContainsKey(tracker.TrackedObject))
			{
				Debug.FailedAssert("Trying to remove a tracker that was not added", "C:\\BuildAgent\\work\\mb3\\Source\\Bannerlord\\SandBox.ViewModelCollection\\Map\\Tracker\\MapTrackerProvider.cs", "RemoveTracker", 66);
				return;
			}
			_trackers.Remove(tracker.TrackedObject);
			OnTrackerAddedOrRemoved?.Invoke(tracker, added: false);
		}

		public void ClearTrackers()
		{
			MapTrackerItemVM[] array = _trackers.Values.ToArray();
			for (int i = 0; i < array.Length; i++)
			{
				RemoveTracker(array[i]);
			}
		}
	}

	public delegate void OnTrackerAddedOrRemovedDelegate(MapTrackerItemVM tracker, bool added);

	private TrackerContainer _trackerContainer;

	public event OnTrackerAddedOrRemovedDelegate OnTrackerAddedOrRemoved
	{
		add
		{
			TrackerContainer trackerContainer = _trackerContainer;
			trackerContainer.OnTrackerAddedOrRemoved = (OnTrackerAddedOrRemovedDelegate)Delegate.Combine(trackerContainer.OnTrackerAddedOrRemoved, value);
		}
		remove
		{
			TrackerContainer trackerContainer = _trackerContainer;
			trackerContainer.OnTrackerAddedOrRemoved = (OnTrackerAddedOrRemovedDelegate)Delegate.Remove(trackerContainer.OnTrackerAddedOrRemoved, value);
		}
	}

	public MapTrackerProvider()
	{
		CampaignEvents.ArmyCreated.AddNonSerializedListener(this, OnArmyCreated);
		CampaignEvents.ArmyDispersed.AddNonSerializedListener(this, OnArmyDispersed);
		CampaignEvents.MobilePartyCreated.AddNonSerializedListener(this, OnMobilePartyCreated);
		CampaignEvents.MobilePartyDestroyed.AddNonSerializedListener(this, OnPartyDestroyed);
		CampaignEvents.MobilePartyQuestStatusChanged.AddNonSerializedListener(this, OnPartyQuestStatusChanged);
		CampaignEvents.OnPartyDisbandedEvent.AddNonSerializedListener(this, OnPartyDisbanded);
		CampaignEvents.OnClanChangedKingdomEvent.AddNonSerializedListener(this, OnClanChangedKingdom);
		CampaignEvents.OnClanCreatedEvent.AddNonSerializedListener(this, OnCompanionClanCreated);
		CampaignEvents.OnMapMarkerCreatedEvent.AddNonSerializedListener(this, OnMapMarkerCreated);
		CampaignEvents.OnMapMarkerRemovedEvent.AddNonSerializedListener(this, OnMapMarkerRemoved);
		_trackerContainer = new TrackerContainer();
		ResetTrackers();
	}

	private void OnFinalize()
	{
		_trackerContainer.ClearTrackers();
		CampaignEventDispatcher.Instance.RemoveListeners(this);
	}

	public MapTrackerItemVM[] GetTrackers()
	{
		return _trackerContainer.GetTrackers();
	}

	private void ResetTrackers()
	{
		_trackerContainer.ClearTrackers();
		MBReadOnlyList<MobileParty> all = MobileParty.All;
		for (int i = 0; i < all.Count; i++)
		{
			MobileParty party = all[i];
			AddIfEligible(party);
		}
		Army[] array = Kingdom.All.SelectMany((Kingdom k) => k.Armies).ToArray();
		foreach (Army army in array)
		{
			AddIfEligible(army);
		}
		if (Campaign.Current.MapMarkerManager == null)
		{
			return;
		}
		foreach (MapMarker mapMarker in Campaign.Current.MapMarkerManager.MapMarkers)
		{
			AddIfEligible(mapMarker);
		}
	}

	private bool CanAddMobileParty(MobileParty party)
	{
		if (!party.IsMainParty && !party.IsMilitia && !party.IsGarrison && !party.IsVillager && !party.IsBandit && !party.IsPatrolParty && !party.IsBanditBossParty && !party.IsCurrentlyUsedByAQuest && (!party.IsCaravan || party.CaravanPartyComponent.Owner == Hero.MainHero))
		{
			if (party.IsLordParty)
			{
				for (int i = 0; i < Clan.PlayerClan.WarPartyComponents.Count; i++)
				{
					if (Clan.PlayerClan.WarPartyComponents[i].MobileParty == party)
					{
						return true;
					}
				}
			}
			for (int j = 0; j < Clan.PlayerClan.Heroes.Count; j++)
			{
				Hero hero = Clan.PlayerClan.Heroes[j];
				for (int k = 0; k < hero.OwnedCaravans.Count; k++)
				{
					if (hero.OwnedCaravans[k].MobileParty == party)
					{
						return true;
					}
				}
			}
		}
		if (party.LeaderHero == null && party.IsCurrentlyUsedByAQuest && Campaign.Current.VisualTrackerManager.CheckTracked(party))
		{
			return true;
		}
		return false;
	}

	private bool CanAddArmy(Army army)
	{
		if (army.Kingdom == Hero.MainHero.MapFaction)
		{
			return !army.Parties.Contains(MobileParty.MainParty);
		}
		return false;
	}

	private void RemoveIfExists(ITrackableCampaignObject trackable)
	{
		MapTrackerItemVM trackerFor = _trackerContainer.GetTrackerFor(trackable);
		if (trackerFor != null)
		{
			_trackerContainer.RemoveTracker(trackerFor);
		}
	}

	private void AddIfEligible(MobileParty party)
	{
		if (CanAddMobileParty(party) && !_trackerContainer.HasTrackerFor(party))
		{
			_trackerContainer.AddTracker(new MapMobilePartyTrackItemVM(party));
		}
	}

	private void AddIfEligible(Army army)
	{
		if (CanAddArmy(army) && !_trackerContainer.HasTrackerFor(army))
		{
			_trackerContainer.AddTracker(new MapArmyTrackItemVM(army));
		}
	}

	private void AddIfEligible(MapMarker mapMarker)
	{
		if (!_trackerContainer.HasTrackerFor(mapMarker))
		{
			_trackerContainer.AddTracker(new MapMarkerTrackerItemVM(mapMarker));
		}
	}

	private void OnPartyDestroyed(MobileParty mobileParty, PartyBase arg2)
	{
		RemoveIfExists(mobileParty);
	}

	private void OnPartyQuestStatusChanged(MobileParty mobileParty, bool isUsedByQuest)
	{
		if (isUsedByQuest)
		{
			if (mobileParty.LeaderHero == null && Campaign.Current.VisualTrackerManager.CheckTracked(mobileParty))
			{
				AddIfEligible(mobileParty);
			}
			else
			{
				RemoveIfExists(mobileParty);
			}
		}
		else
		{
			AddIfEligible(mobileParty);
		}
	}

	private void OnPartyDisbanded(MobileParty disbandedParty, Settlement relatedSettlement)
	{
		RemoveIfExists(disbandedParty);
	}

	private void OnMobilePartyCreated(MobileParty mobileParty)
	{
		AddIfEligible(mobileParty);
	}

	private void OnArmyDispersed(Army army, Army.ArmyDispersionReason arg2, bool arg3)
	{
		RemoveIfExists(army);
	}

	private void OnArmyCreated(Army army)
	{
		AddIfEligible(army);
	}

	private void OnClanChangedKingdom(Clan clan, Kingdom oldKingdom, Kingdom newKingdom, ChangeKingdomAction.ChangeKingdomActionDetail detail, bool showNotification)
	{
		if (clan == Clan.PlayerClan)
		{
			ResetTrackers();
		}
	}

	private void OnCompanionClanCreated(Clan clan, bool isCompanion)
	{
		if (isCompanion && clan.Leader.PartyBelongedTo != null)
		{
			RemoveIfExists(clan.Leader.PartyBelongedTo);
		}
	}

	private void OnMapMarkerRemoved(MapMarker marker)
	{
		RemoveIfExists(marker);
	}

	private void OnMapMarkerCreated(MapMarker marker)
	{
		AddIfEligible(marker);
	}
}
