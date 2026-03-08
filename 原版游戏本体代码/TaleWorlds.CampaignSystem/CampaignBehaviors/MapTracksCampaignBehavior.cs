using System;
using System.Collections.Generic;
using System.Linq;
using TaleWorlds.CampaignSystem.CharacterDevelopment;
using TaleWorlds.CampaignSystem.Map;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.Core;
using TaleWorlds.Library;
using TaleWorlds.Localization;

namespace TaleWorlds.CampaignSystem.CampaignBehaviors
{
	// Token: 0x02000415 RID: 1045
	public class MapTracksCampaignBehavior : CampaignBehaviorBase, IMapTracksCampaignBehavior, ICampaignBehavior
	{
		// Token: 0x17000E1C RID: 3612
		// (get) Token: 0x06004252 RID: 16978 RVA: 0x0013EDF1 File Offset: 0x0013CFF1
		public MBReadOnlyList<Track> DetectedTracks
		{
			get
			{
				return this._detectedTracksCache;
			}
		}

		// Token: 0x06004253 RID: 16979 RVA: 0x0013EDFC File Offset: 0x0013CFFC
		public MapTracksCampaignBehavior()
		{
			this._trackPool = new MapTracksCampaignBehavior.TrackPool(2048);
		}

		// Token: 0x06004254 RID: 16980 RVA: 0x0013EE54 File Offset: 0x0013D054
		public override void RegisterEvents()
		{
			CampaignEvents.HourlyTickEvent.AddNonSerializedListener(this, new Action(this.OnHourlyTick));
			CampaignEvents.OnGameLoadFinishedEvent.AddNonSerializedListener(this, new Action(this.GameLoadFinished));
			CampaignEvents.HourlyTickPartyEvent.AddNonSerializedListener(this, new Action<MobileParty>(this.OnHourlyTickParty));
			CampaignEvents.MobilePartyDestroyed.AddNonSerializedListener(this, new Action<MobileParty, PartyBase>(this.OnMobilePartyDestroyed));
			CampaignEvents.OnNewGameCreatedEvent.AddNonSerializedListener(this, new Action<CampaignGameStarter>(this.OnNewGameCreated));
		}

		// Token: 0x06004255 RID: 16981 RVA: 0x0013EED4 File Offset: 0x0013D0D4
		private void OnMobilePartyDestroyed(MobileParty mobileParty, PartyBase destroyerParty)
		{
			if (this._trackDataDictionary.ContainsKey(mobileParty))
			{
				this._trackDataDictionary.Remove(mobileParty);
			}
		}

		// Token: 0x06004256 RID: 16982 RVA: 0x0013EEF1 File Offset: 0x0013D0F1
		private void OnNewGameCreated(CampaignGameStarter gameStarted)
		{
			this.AddEventHandler();
		}

		// Token: 0x06004257 RID: 16983 RVA: 0x0013EEFC File Offset: 0x0013D0FC
		public override void SyncData(IDataStore dataStore)
		{
			dataStore.SyncData<List<Track>>("_allTracks", ref this._allTracks);
			dataStore.SyncData<Dictionary<MobileParty, CampaignVec2>>("_trackDataDictionary2", ref this._trackDataDictionary);
			if (dataStore.IsLoading && MBSaveLoad.IsUpdatingGameVersion && MBSaveLoad.LastLoadedGameVersion.IsOlderThan(ApplicationVersion.FromString("v1.3.0", 0)))
			{
				Dictionary<MobileParty, Vec2> dictionary = new Dictionary<MobileParty, Vec2>();
				dataStore.SyncData<Dictionary<MobileParty, Vec2>>("_trackDataDictionary", ref dictionary);
				if (dictionary.Any<KeyValuePair<MobileParty, Vec2>>())
				{
					foreach (KeyValuePair<MobileParty, Vec2> keyValuePair in dictionary)
					{
						this._trackDataDictionary.Add(keyValuePair.Key, new CampaignVec2(keyValuePair.Value, true));
					}
				}
			}
		}

		// Token: 0x06004258 RID: 16984 RVA: 0x0013EFD0 File Offset: 0x0013D1D0
		private void OnHourlyTickParty(MobileParty mobileParty)
		{
			if (Campaign.Current.Models.MapTrackModel.CanPartyLeaveTrack(mobileParty))
			{
				CampaignVec2 v = CampaignVec2.Zero;
				if (this._trackDataDictionary.ContainsKey(mobileParty))
				{
					v = this._trackDataDictionary[mobileParty];
				}
				if (v.DistanceSquared(mobileParty.Position.ToVec2()) > 5f && this.IsTrackDropped(mobileParty))
				{
					CampaignVec2 position = mobileParty.Position;
					CampaignVec2 campaignVec = mobileParty.Position - v;
					campaignVec.Normalize();
					this.AddTrack(mobileParty, position, campaignVec.ToVec2());
					this._trackDataDictionary[mobileParty] = position;
				}
			}
		}

		// Token: 0x06004259 RID: 16985 RVA: 0x0013F072 File Offset: 0x0013D272
		private void OnHourlyTick()
		{
			this.RemoveExpiredTracks();
		}

		// Token: 0x0600425A RID: 16986 RVA: 0x0013F07C File Offset: 0x0013D27C
		private void GameLoadFinished()
		{
			this._allTracks.RemoveAll((Track x) => x.IsExpired);
			this._detectedTracksCache = (from x in this._allTracks
				where x.IsDetected
				select x).ToMBList<Track>();
			this.AddEventHandler();
			foreach (Track locatable in this._allTracks)
			{
				this._trackLocator.UpdateLocator(locatable);
			}
			foreach (MobileParty mobileParty in this._trackDataDictionary.Keys.ToList<MobileParty>())
			{
				if (!mobileParty.IsActive)
				{
					this._trackDataDictionary.Remove(mobileParty);
				}
			}
		}

		// Token: 0x0600425B RID: 16987 RVA: 0x0013F198 File Offset: 0x0013D398
		private void AddEventHandler()
		{
			this._quarterHourlyTick = CampaignPeriodicEventManager.CreatePeriodicEvent(CampaignTime.Hours(0.25f), CampaignTime.Hours(0.1f));
			this._quarterHourlyTick.AddHandler(new MBCampaignEvent.CampaignEventDelegate(this.QuarterHourlyTick));
		}

		// Token: 0x0600425C RID: 16988 RVA: 0x0013F1D0 File Offset: 0x0013D3D0
		private void QuarterHourlyTick(MBCampaignEvent campaignEvent, object[] delegateParams)
		{
			if (!PartyBase.MainParty.IsValid)
			{
				return;
			}
			int num = ((MobileParty.MainParty.EffectiveScout != null) ? MobileParty.MainParty.EffectiveScout.GetSkillValue(DefaultSkills.Scouting) : 0);
			if (num != 0)
			{
				float maxTrackSpottingDistanceForMainParty = Campaign.Current.Models.MapTrackModel.GetMaxTrackSpottingDistanceForMainParty();
				LocatableSearchData<Track> locatableSearchData = this._trackLocator.StartFindingLocatablesAroundPosition(MobileParty.MainParty.Position.ToVec2(), maxTrackSpottingDistanceForMainParty);
				for (Track track = this._trackLocator.FindNextLocatable(ref locatableSearchData); track != null; track = this._trackLocator.FindNextLocatable(ref locatableSearchData))
				{
					if (!track.IsDetected && this._allTracks.Contains(track) && Campaign.Current.Models.MapTrackModel.GetTrackDetectionDifficultyForMainParty(track, maxTrackSpottingDistanceForMainParty) < (float)num)
					{
						this.TrackDetected(track);
					}
				}
			}
		}

		// Token: 0x0600425D RID: 16989 RVA: 0x0013F2A4 File Offset: 0x0013D4A4
		private void RemoveExpiredTracks()
		{
			for (int i = this._allTracks.Count - 1; i >= 0; i--)
			{
				Track track = this._allTracks[i];
				if (track.IsExpired)
				{
					this._allTracks.Remove(track);
					if (this._detectedTracksCache.Contains(track))
					{
						this._detectedTracksCache.Remove(track);
						CampaignEventDispatcher.Instance.TrackLost(track);
					}
					this._trackLocator.RemoveLocatable(track);
					this._trackPool.ReleaseTrack(track);
				}
			}
		}

		// Token: 0x0600425E RID: 16990 RVA: 0x0013F329 File Offset: 0x0013D529
		private void TrackDetected(Track track)
		{
			track.IsDetected = true;
			this._detectedTracksCache.Add(track);
			CampaignEventDispatcher.Instance.TrackDetected(track);
			SkillLevelingManager.OnTrackDetected(track);
		}

		// Token: 0x0600425F RID: 16991 RVA: 0x0013F350 File Offset: 0x0013D550
		public bool IsTrackDropped(MobileParty mobileParty)
		{
			float skipTrackChance = Campaign.Current.Models.MapTrackModel.GetSkipTrackChance(mobileParty);
			if (MBRandom.RandomFloat < skipTrackChance)
			{
				return false;
			}
			float num = mobileParty.Position.DistanceSquared(MobileParty.MainParty.Position);
			float num2 = (MobileParty.MainParty.IsActive ? (MobileParty.MainParty._lastCalculatedSpeed * Campaign.Current.Models.MapTrackModel.MaxTrackLife) : 0f);
			return num2 * num2 > num;
		}

		// Token: 0x06004260 RID: 16992 RVA: 0x0013F3D0 File Offset: 0x0013D5D0
		public void AddTrack(MobileParty party, CampaignVec2 trackPosition, Vec2 trackDirection)
		{
			Track track = this._trackPool.RequestTrack(party, trackPosition, trackDirection);
			this._allTracks.Add(track);
			this._trackLocator.UpdateLocator(track);
		}

		// Token: 0x06004261 RID: 16993 RVA: 0x0013F408 File Offset: 0x0013D608
		public void AddMapArrow(TextObject pointerName, CampaignVec2 trackPosition, Vec2 trackDirection, float life)
		{
			Track track = this._trackPool.RequestMapArrow(pointerName, trackPosition, trackDirection, life);
			this._allTracks.Add(track);
			this._trackLocator.UpdateLocator(track);
			this.TrackDetected(track);
		}

		// Token: 0x040012EF RID: 4847
		private const float PartyTrackPositionDelta = 5f;

		// Token: 0x040012F0 RID: 4848
		private List<Track> _allTracks = new List<Track>();

		// Token: 0x040012F1 RID: 4849
		private MBList<Track> _detectedTracksCache = new MBList<Track>();

		// Token: 0x040012F2 RID: 4850
		private Dictionary<MobileParty, CampaignVec2> _trackDataDictionary = new Dictionary<MobileParty, CampaignVec2>();

		// Token: 0x040012F3 RID: 4851
		private MBCampaignEvent _quarterHourlyTick;

		// Token: 0x040012F4 RID: 4852
		private LocatorGrid<Track> _trackLocator = new LocatorGrid<Track>(5f, 32, 32);

		// Token: 0x040012F5 RID: 4853
		private MapTracksCampaignBehavior.TrackPool _trackPool;

		// Token: 0x0200081E RID: 2078
		private class TrackPool
		{
			// Token: 0x1700151F RID: 5407
			// (get) Token: 0x06006627 RID: 26151 RVA: 0x001C200D File Offset: 0x001C020D
			private int MaxSize { get; }

			// Token: 0x17001520 RID: 5408
			// (get) Token: 0x06006628 RID: 26152 RVA: 0x001C2015 File Offset: 0x001C0215
			public int Size
			{
				get
				{
					Stack<Track> stack = this._stack;
					if (stack == null)
					{
						return 0;
					}
					return stack.Count;
				}
			}

			// Token: 0x06006629 RID: 26153 RVA: 0x001C2028 File Offset: 0x001C0228
			public TrackPool(int size)
			{
				this.MaxSize = size;
				this._stack = new Stack<Track>();
				for (int i = 0; i < size; i++)
				{
					this._stack.Push(new Track());
				}
			}

			// Token: 0x0600662A RID: 26154 RVA: 0x001C206C File Offset: 0x001C026C
			public Track RequestTrack(MobileParty party, CampaignVec2 trackPosition, Vec2 trackDirection)
			{
				Track track = ((this._stack.Count > 0) ? this._stack.Pop() : new Track());
				int num = party.Party.NumberOfAllMembers;
				int num2 = party.Party.NumberOfHealthyMembers;
				int num3 = party.Party.NumberOfMenWithHorse;
				int num4 = party.Party.NumberOfMenWithoutHorse;
				int num5 = party.Party.NumberOfPackAnimals;
				int num6 = party.Party.NumberOfPrisoners;
				TextObject partyName = party.Name;
				if (party.Army != null && party.Army.LeaderParty == party)
				{
					partyName = party.ArmyName;
					foreach (MobileParty mobileParty in party.Army.LeaderParty.AttachedParties)
					{
						num += mobileParty.Party.NumberOfAllMembers;
						num2 += mobileParty.Party.NumberOfHealthyMembers;
						num3 += mobileParty.Party.NumberOfMenWithHorse;
						num4 += mobileParty.Party.NumberOfMenWithoutHorse;
						num5 += mobileParty.Party.NumberOfPackAnimals;
						num6 += mobileParty.Party.NumberOfPrisoners;
					}
				}
				track.Position = trackPosition;
				track.Direction = trackDirection.RotationInRadians;
				track.PartyType = Track.GetPartyTypeEnum(party);
				track.PartyName = partyName;
				track.Culture = party.Party.Culture;
				if (track.Culture == null)
				{
					string message = string.Format("Track culture is null for {0}: {1}", party.StringId, party.Name);
					Debug.Print(message, 0, Debug.DebugColor.White, 17592186044416UL);
					Debug.FailedAssert(message, "C:\\BuildAgent\\work\\mb3\\Source\\Bannerlord\\TaleWorlds.CampaignSystem\\CampaignBehaviors\\MapTracksCampaignBehavior.cs", "RequestTrack", 62);
				}
				track.Speed = party.Speed;
				track.Life = (float)Campaign.Current.Models.MapTrackModel.GetTrackLife(party);
				track.IsEnemy = FactionManager.IsAtWarAgainstFaction(Hero.MainHero.MapFaction, party.MapFaction);
				track.NumberOfAllMembers = num;
				track.NumberOfHealthyMembers = num2;
				track.NumberOfMenWithHorse = num3;
				track.NumberOfMenWithoutHorse = num4;
				track.NumberOfPackAnimals = num5;
				track.NumberOfPrisoners = num6;
				track.IsPointer = false;
				track.IsDetected = false;
				track.CreationTime = CampaignTime.Now;
				return track;
			}

			// Token: 0x0600662B RID: 26155 RVA: 0x001C22C0 File Offset: 0x001C04C0
			public Track RequestMapArrow(TextObject pointerName, CampaignVec2 trackPosition, Vec2 trackDirection, float life)
			{
				Track track = ((this._stack.Count > 0) ? this._stack.Pop() : new Track());
				track.Position = trackPosition;
				track.Direction = trackDirection.RotationInRadians;
				track.PartyName = pointerName;
				track.Life = life;
				track.IsPointer = true;
				track.IsDetected = true;
				track.CreationTime = CampaignTime.Now;
				return track;
			}

			// Token: 0x0600662C RID: 26156 RVA: 0x001C2329 File Offset: 0x001C0529
			public void ReleaseTrack(Track track)
			{
				track.Reset();
				if (this._stack.Count < this.MaxSize)
				{
					this._stack.Push(track);
				}
			}

			// Token: 0x0600662D RID: 26157 RVA: 0x001C2350 File Offset: 0x001C0550
			public override string ToString()
			{
				return string.Format("TrackPool: {0}", this.Size);
			}

			// Token: 0x04002286 RID: 8838
			private Stack<Track> _stack;
		}
	}
}
