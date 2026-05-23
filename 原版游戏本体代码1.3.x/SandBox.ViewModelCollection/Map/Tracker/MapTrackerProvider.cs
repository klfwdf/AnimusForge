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

namespace SandBox.ViewModelCollection.Map.Tracker
{
	// Token: 0x0200004B RID: 75
	public class MapTrackerProvider
	{
		// Token: 0x14000002 RID: 2
		// (add) Token: 0x06000491 RID: 1169 RVA: 0x00011DF6 File Offset: 0x0000FFF6
		// (remove) Token: 0x06000492 RID: 1170 RVA: 0x00011E14 File Offset: 0x00010014
		public event MapTrackerProvider.OnTrackerAddedOrRemovedDelegate OnTrackerAddedOrRemoved
		{
			add
			{
				MapTrackerProvider.TrackerContainer trackerContainer = this._trackerContainer;
				trackerContainer.OnTrackerAddedOrRemoved = (MapTrackerProvider.OnTrackerAddedOrRemovedDelegate)Delegate.Combine(trackerContainer.OnTrackerAddedOrRemoved, value);
			}
			remove
			{
				MapTrackerProvider.TrackerContainer trackerContainer = this._trackerContainer;
				trackerContainer.OnTrackerAddedOrRemoved = (MapTrackerProvider.OnTrackerAddedOrRemovedDelegate)Delegate.Remove(trackerContainer.OnTrackerAddedOrRemoved, value);
			}
		}

		// Token: 0x06000493 RID: 1171 RVA: 0x00011E34 File Offset: 0x00010034
		public MapTrackerProvider()
		{
			CampaignEvents.ArmyCreated.AddNonSerializedListener(this, new Action<Army>(this.OnArmyCreated));
			CampaignEvents.ArmyDispersed.AddNonSerializedListener(this, new Action<Army, Army.ArmyDispersionReason, bool>(this.OnArmyDispersed));
			CampaignEvents.MobilePartyCreated.AddNonSerializedListener(this, new Action<MobileParty>(this.OnMobilePartyCreated));
			CampaignEvents.MobilePartyDestroyed.AddNonSerializedListener(this, new Action<MobileParty, PartyBase>(this.OnPartyDestroyed));
			CampaignEvents.MobilePartyQuestStatusChanged.AddNonSerializedListener(this, new Action<MobileParty, bool>(this.OnPartyQuestStatusChanged));
			CampaignEvents.OnPartyDisbandedEvent.AddNonSerializedListener(this, new Action<MobileParty, Settlement>(this.OnPartyDisbanded));
			CampaignEvents.OnClanChangedKingdomEvent.AddNonSerializedListener(this, new Action<Clan, Kingdom, Kingdom, ChangeKingdomAction.ChangeKingdomActionDetail, bool>(this.OnClanChangedKingdom));
			CampaignEvents.OnClanCreatedEvent.AddNonSerializedListener(this, new Action<Clan, bool>(this.OnCompanionClanCreated));
			CampaignEvents.OnMapMarkerCreatedEvent.AddNonSerializedListener(this, new Action<MapMarker>(this.OnMapMarkerCreated));
			CampaignEvents.OnMapMarkerRemovedEvent.AddNonSerializedListener(this, new Action<MapMarker>(this.OnMapMarkerRemoved));
			this._trackerContainer = new MapTrackerProvider.TrackerContainer();
			this.ResetTrackers();
		}

		// Token: 0x06000494 RID: 1172 RVA: 0x00011F3E File Offset: 0x0001013E
		private void OnFinalize()
		{
			this._trackerContainer.ClearTrackers();
			CampaignEventDispatcher.Instance.RemoveListeners(this);
		}

		// Token: 0x06000495 RID: 1173 RVA: 0x00011F56 File Offset: 0x00010156
		public MapTrackerItemVM[] GetTrackers()
		{
			return this._trackerContainer.GetTrackers();
		}

		// Token: 0x06000496 RID: 1174 RVA: 0x00011F64 File Offset: 0x00010164
		private void ResetTrackers()
		{
			this._trackerContainer.ClearTrackers();
			MBReadOnlyList<MobileParty> all = MobileParty.All;
			for (int i = 0; i < all.Count; i++)
			{
				MobileParty party = all[i];
				this.AddIfEligible(party);
			}
			foreach (Army army in Kingdom.All.SelectMany((Kingdom k) => k.Armies).ToArray<Army>())
			{
				this.AddIfEligible(army);
			}
			if (Campaign.Current.MapMarkerManager != null)
			{
				foreach (MapMarker mapMarker in Campaign.Current.MapMarkerManager.MapMarkers)
				{
					this.AddIfEligible(mapMarker);
				}
			}
		}

		// Token: 0x06000497 RID: 1175 RVA: 0x00012050 File Offset: 0x00010250
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
			return party.LeaderHero == null && party.IsCurrentlyUsedByAQuest && Campaign.Current.VisualTrackerManager.CheckTracked(party);
		}

		// Token: 0x06000498 RID: 1176 RVA: 0x0001217A File Offset: 0x0001037A
		private bool CanAddArmy(Army army)
		{
			return army.Kingdom == Hero.MainHero.MapFaction && !army.Parties.Contains(MobileParty.MainParty);
		}

		// Token: 0x06000499 RID: 1177 RVA: 0x000121A4 File Offset: 0x000103A4
		private void RemoveIfExists(ITrackableCampaignObject trackable)
		{
			MapTrackerItemVM trackerFor = this._trackerContainer.GetTrackerFor(trackable);
			if (trackerFor != null)
			{
				this._trackerContainer.RemoveTracker(trackerFor);
			}
		}

		// Token: 0x0600049A RID: 1178 RVA: 0x000121CD File Offset: 0x000103CD
		private void AddIfEligible(MobileParty party)
		{
			if (this.CanAddMobileParty(party) && !this._trackerContainer.HasTrackerFor(party))
			{
				this._trackerContainer.AddTracker(new MapMobilePartyTrackItemVM(party));
			}
		}

		// Token: 0x0600049B RID: 1179 RVA: 0x000121F7 File Offset: 0x000103F7
		private void AddIfEligible(Army army)
		{
			if (this.CanAddArmy(army) && !this._trackerContainer.HasTrackerFor(army))
			{
				this._trackerContainer.AddTracker(new MapArmyTrackItemVM(army));
			}
		}

		// Token: 0x0600049C RID: 1180 RVA: 0x00012221 File Offset: 0x00010421
		private void AddIfEligible(MapMarker mapMarker)
		{
			if (!this._trackerContainer.HasTrackerFor(mapMarker))
			{
				this._trackerContainer.AddTracker(new MapMarkerTrackerItemVM(mapMarker));
			}
		}

		// Token: 0x0600049D RID: 1181 RVA: 0x00012242 File Offset: 0x00010442
		private void OnPartyDestroyed(MobileParty mobileParty, PartyBase arg2)
		{
			this.RemoveIfExists(mobileParty);
		}

		// Token: 0x0600049E RID: 1182 RVA: 0x0001224B File Offset: 0x0001044B
		private void OnPartyQuestStatusChanged(MobileParty mobileParty, bool isUsedByQuest)
		{
			if (!isUsedByQuest)
			{
				this.AddIfEligible(mobileParty);
				return;
			}
			if (mobileParty.LeaderHero == null && Campaign.Current.VisualTrackerManager.CheckTracked(mobileParty))
			{
				this.AddIfEligible(mobileParty);
				return;
			}
			this.RemoveIfExists(mobileParty);
		}

		// Token: 0x0600049F RID: 1183 RVA: 0x00012281 File Offset: 0x00010481
		private void OnPartyDisbanded(MobileParty disbandedParty, Settlement relatedSettlement)
		{
			this.RemoveIfExists(disbandedParty);
		}

		// Token: 0x060004A0 RID: 1184 RVA: 0x0001228A File Offset: 0x0001048A
		private void OnMobilePartyCreated(MobileParty mobileParty)
		{
			this.AddIfEligible(mobileParty);
		}

		// Token: 0x060004A1 RID: 1185 RVA: 0x00012293 File Offset: 0x00010493
		private void OnArmyDispersed(Army army, Army.ArmyDispersionReason arg2, bool arg3)
		{
			this.RemoveIfExists(army);
		}

		// Token: 0x060004A2 RID: 1186 RVA: 0x0001229C File Offset: 0x0001049C
		private void OnArmyCreated(Army army)
		{
			this.AddIfEligible(army);
		}

		// Token: 0x060004A3 RID: 1187 RVA: 0x000122A5 File Offset: 0x000104A5
		private void OnClanChangedKingdom(Clan clan, Kingdom oldKingdom, Kingdom newKingdom, ChangeKingdomAction.ChangeKingdomActionDetail detail, bool showNotification)
		{
			if (clan == Clan.PlayerClan)
			{
				this.ResetTrackers();
			}
		}

		// Token: 0x060004A4 RID: 1188 RVA: 0x000122B5 File Offset: 0x000104B5
		private void OnCompanionClanCreated(Clan clan, bool isCompanion)
		{
			if (isCompanion && clan.Leader.PartyBelongedTo != null)
			{
				this.RemoveIfExists(clan.Leader.PartyBelongedTo);
			}
		}

		// Token: 0x060004A5 RID: 1189 RVA: 0x000122D8 File Offset: 0x000104D8
		private void OnMapMarkerRemoved(MapMarker marker)
		{
			this.RemoveIfExists(marker);
		}

		// Token: 0x060004A6 RID: 1190 RVA: 0x000122E1 File Offset: 0x000104E1
		private void OnMapMarkerCreated(MapMarker marker)
		{
			this.AddIfEligible(marker);
		}

		// Token: 0x0400024B RID: 587
		private MapTrackerProvider.TrackerContainer _trackerContainer;

		// Token: 0x020000A4 RID: 164
		private class TrackerContainer
		{
			// Token: 0x060006A8 RID: 1704 RVA: 0x00016CAC File Offset: 0x00014EAC
			public TrackerContainer()
			{
				this._trackers = new Dictionary<ITrackableCampaignObject, MapTrackerItemVM>();
			}

			// Token: 0x060006A9 RID: 1705 RVA: 0x00016CBF File Offset: 0x00014EBF
			public MapTrackerItemVM[] GetTrackers()
			{
				return this._trackers.Values.ToArray<MapTrackerItemVM>();
			}

			// Token: 0x060006AA RID: 1706 RVA: 0x00016CD1 File Offset: 0x00014ED1
			public bool HasTrackerFor(ITrackableCampaignObject trackable)
			{
				return this.GetTrackerFor(trackable) != null;
			}

			// Token: 0x060006AB RID: 1707 RVA: 0x00016CE0 File Offset: 0x00014EE0
			public MapTrackerItemVM GetTrackerFor(ITrackableCampaignObject trackable)
			{
				MapTrackerItemVM result;
				if (this._trackers.TryGetValue(trackable, out result))
				{
					return result;
				}
				return null;
			}

			// Token: 0x060006AC RID: 1708 RVA: 0x00016D00 File Offset: 0x00014F00
			public void AddTracker(MapTrackerItemVM tracker)
			{
				if (this._trackers.ContainsKey(tracker.TrackedObject))
				{
					Debug.FailedAssert("Trying to add a tracker that was already added", "C:\\BuildAgent\\work\\mb3\\Source\\Bannerlord\\SandBox.ViewModelCollection\\Map\\Tracker\\MapTrackerProvider.cs", "AddTracker", 54);
					return;
				}
				this._trackers.Add(tracker.TrackedObject, tracker);
				MapTrackerProvider.OnTrackerAddedOrRemovedDelegate onTrackerAddedOrRemoved = this.OnTrackerAddedOrRemoved;
				if (onTrackerAddedOrRemoved == null)
				{
					return;
				}
				onTrackerAddedOrRemoved(tracker, true);
			}

			// Token: 0x060006AD RID: 1709 RVA: 0x00016D5C File Offset: 0x00014F5C
			public void RemoveTracker(MapTrackerItemVM tracker)
			{
				if (!this._trackers.ContainsKey(tracker.TrackedObject))
				{
					Debug.FailedAssert("Trying to remove a tracker that was not added", "C:\\BuildAgent\\work\\mb3\\Source\\Bannerlord\\SandBox.ViewModelCollection\\Map\\Tracker\\MapTrackerProvider.cs", "RemoveTracker", 66);
					return;
				}
				this._trackers.Remove(tracker.TrackedObject);
				MapTrackerProvider.OnTrackerAddedOrRemovedDelegate onTrackerAddedOrRemoved = this.OnTrackerAddedOrRemoved;
				if (onTrackerAddedOrRemoved == null)
				{
					return;
				}
				onTrackerAddedOrRemoved(tracker, false);
			}

			// Token: 0x060006AE RID: 1710 RVA: 0x00016DB8 File Offset: 0x00014FB8
			public void ClearTrackers()
			{
				MapTrackerItemVM[] array = this._trackers.Values.ToArray<MapTrackerItemVM>();
				for (int i = 0; i < array.Length; i++)
				{
					this.RemoveTracker(array[i]);
				}
			}

			// Token: 0x040003BF RID: 959
			private readonly Dictionary<ITrackableCampaignObject, MapTrackerItemVM> _trackers;

			// Token: 0x040003C0 RID: 960
			public MapTrackerProvider.OnTrackerAddedOrRemovedDelegate OnTrackerAddedOrRemoved;
		}

		// Token: 0x020000A5 RID: 165
		// (Invoke) Token: 0x060006B0 RID: 1712
		public delegate void OnTrackerAddedOrRemovedDelegate(MapTrackerItemVM tracker, bool added);
	}
}
