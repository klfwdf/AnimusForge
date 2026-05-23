using System;
using System.Collections.Generic;
using Helpers;
using SandBox.ViewModelCollection.Nameplate.NameplateNotifications.SettlementNotificationTypes;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.MapEvents;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.CampaignSystem.Siege;
using TaleWorlds.Core;
using TaleWorlds.Core.ViewModelCollection.ImageIdentifiers;
using TaleWorlds.Core.ViewModelCollection.Tutorial;
using TaleWorlds.Engine;
using TaleWorlds.Library;
using TaleWorlds.MountAndBlade;

namespace SandBox.ViewModelCollection.Nameplate
{
	// Token: 0x02000021 RID: 33
	public class SettlementNameplateVM : NameplateVM
	{
		// Token: 0x170000F3 RID: 243
		// (get) Token: 0x06000301 RID: 769 RVA: 0x0000CFC7 File Offset: 0x0000B1C7
		public Settlement Settlement { get; }

		// Token: 0x170000F4 RID: 244
		// (get) Token: 0x06000302 RID: 770 RVA: 0x0000CFCF File Offset: 0x0000B1CF
		// (set) Token: 0x06000303 RID: 771 RVA: 0x0000CFD7 File Offset: 0x0000B1D7
		public SettlementNameplateVM.Type SettlementTypeEnum { get; private set; }

		// Token: 0x06000304 RID: 772 RVA: 0x0000CFE0 File Offset: 0x0000B1E0
		public SettlementNameplateVM(Settlement settlement, GameEntity entity, Camera mapCamera, Action<CampaignVec2> fastMoveCameraToPosition)
		{
			this.Settlement = settlement;
			this._mapCamera = mapCamera;
			this._entity = entity;
			this._fastMoveCameraToPosition = fastMoveCameraToPosition;
			this.SettlementNotifications = new SettlementNameplateNotificationsVM(settlement);
			this.SettlementParties = new SettlementNameplatePartyMarkersVM(settlement);
			this.SettlementEvents = new SettlementNameplateEventsVM(settlement);
			this.Name = this.Settlement.Name.ToString();
			this.IsTracked = Campaign.Current.VisualTrackerManager.CheckTracked(settlement);
			if (this.Settlement.IsCastle)
			{
				this.SettlementTypeEnum = SettlementNameplateVM.Type.Castle;
				this._isCastle = true;
			}
			else if (this.Settlement.IsVillage)
			{
				this.SettlementTypeEnum = SettlementNameplateVM.Type.Village;
				this._isVillage = true;
			}
			else if (this.Settlement.IsTown)
			{
				this.SettlementTypeEnum = SettlementNameplateVM.Type.Town;
				this._isTown = true;
			}
			else
			{
				this.SettlementTypeEnum = SettlementNameplateVM.Type.Village;
				this._isTown = true;
			}
			this.SettlementType = (int)this.SettlementTypeEnum;
			if (this._entity != null)
			{
				this._worldPos = this._entity.GlobalPosition;
			}
			else
			{
				this._worldPos = this.Settlement.GetPositionAsVec3();
			}
			this.RefreshDynamicProperties(false);
			this._rebelliousClans = new List<Clan>();
			if (Game.Current != null)
			{
				Game.Current.EventManager.RegisterEvent<TutorialNotificationElementChangeEvent>(new Action<TutorialNotificationElementChangeEvent>(base.OnTutorialNotificationElementChanged));
			}
		}

		// Token: 0x06000305 RID: 773 RVA: 0x0000D13E File Offset: 0x0000B33E
		public override void OnFinalize()
		{
			base.OnFinalize();
			this.SettlementNotifications.UnloadEvents();
			this.SettlementParties.UnloadEvents();
			Game.Current.EventManager.UnregisterEvent<TutorialNotificationElementChangeEvent>(new Action<TutorialNotificationElementChangeEvent>(base.OnTutorialNotificationElementChanged));
		}

		// Token: 0x06000306 RID: 774 RVA: 0x0000D177 File Offset: 0x0000B377
		public override void RefreshValues()
		{
			base.RefreshValues();
			this.Name = this.Settlement.Name.ToString();
		}

		// Token: 0x06000307 RID: 775 RVA: 0x0000D198 File Offset: 0x0000B398
		public override void RefreshDynamicProperties(bool forceUpdate)
		{
			base.RefreshDynamicProperties(forceUpdate);
			if ((this._bindIsVisibleOnMap && this._currentFaction != this.Settlement.MapFaction) || forceUpdate)
			{
				string str = "#";
				IFaction mapFaction = this.Settlement.MapFaction;
				this._bindFactionColor = str + Color.UIntToColorString((mapFaction != null) ? mapFaction.Color : uint.MaxValue);
				Banner banner = this.Settlement.Banner;
				int num = ((banner != null) ? banner.GetVersionNo() : 0);
				if ((this._latestBanner != banner && !this._latestBanner.IsContentsSameWith(banner)) || this._latestBannerVersionNo != num)
				{
					this._bindBanner = ((banner != null) ? new BannerImageIdentifierVM(banner, true) : new BannerImageIdentifierVM(null, false));
					this._latestBannerVersionNo = num;
					this._latestBanner = banner;
				}
				this._currentFaction = this.Settlement.MapFaction;
			}
			PartyBase party = this.Settlement.Party;
			if ((party != null && party.IsVisualDirty) || forceUpdate)
			{
				this._bindName = this.Settlement.Party.Name.ToString();
			}
			this._bindIsTracked = Campaign.Current.VisualTrackerManager.CheckTracked(this.Settlement);
			if (this.Settlement.IsHideout)
			{
				ISpottable spottable = this.Settlement.SettlementComponent as ISpottable;
				this._bindIsInRange = spottable != null && spottable.IsSpotted;
			}
			else
			{
				this._bindIsInRange = this.Settlement.IsInspected;
			}
			this._bindHasPort = this.Settlement.HasPort;
		}

		// Token: 0x06000308 RID: 776 RVA: 0x0000D318 File Offset: 0x0000B518
		public override void RefreshRelationStatus()
		{
			this._bindRelation = 0;
			if (this.Settlement.OwnerClan != null)
			{
				if (FactionManager.IsAtWarAgainstFaction(this.Settlement.MapFaction, Hero.MainHero.MapFaction))
				{
					this._bindRelation = 2;
					return;
				}
				if (DiplomacyHelper.IsSameFactionAndNotEliminated(this.Settlement.MapFaction, Hero.MainHero.MapFaction))
				{
					this._bindRelation = 1;
				}
			}
		}

		// Token: 0x06000309 RID: 777 RVA: 0x0000D380 File Offset: 0x0000B580
		public override void RefreshPosition()
		{
			base.RefreshPosition();
			this._bindWPos = this._wPosAfterPositionCalculation;
			this._bindWSign = (int)this._bindWPos;
			this._bindIsInside = this._latestIsInsideWindow;
			if (this._bindIsVisibleOnMap)
			{
				this._bindPosition = new Vec2(this._latestX, this._latestY);
				return;
			}
			this._bindPosition = new Vec2(-1000f, -1000f);
		}

		// Token: 0x0600030A RID: 778 RVA: 0x0000D3F0 File Offset: 0x0000B5F0
		public override void RefreshTutorialStatus(string newTutorialHighlightElementID)
		{
			base.RefreshTutorialStatus(newTutorialHighlightElementID);
			Settlement settlement = this.Settlement;
			bool flag;
			if (settlement == null)
			{
				flag = null != null;
			}
			else
			{
				PartyBase party = settlement.Party;
				flag = ((party != null) ? party.Id : null) != null;
			}
			if (!flag)
			{
				Debug.FailedAssert("Settlement party id is null when refreshing tutorial status", "C:\\BuildAgent\\work\\mb3\\Source\\Bannerlord\\SandBox.ViewModelCollection\\Nameplate\\SettlementNameplateVM.cs", "RefreshTutorialStatus", 249);
				return;
			}
			this._bindIsTargetedByTutorial = this.Settlement.Party.Id == newTutorialHighlightElementID;
		}

		// Token: 0x0600030B RID: 779 RVA: 0x0000D45C File Offset: 0x0000B65C
		public void OnSiegeEventStartedOnSettlement(SiegeEvent siegeEvent)
		{
			this.MapEventVisualType = 2;
			if (this.Settlement.MapFaction == Hero.MainHero.MapFaction && (BannerlordConfig.AutoTrackAttackedSettlements == 0 || (BannerlordConfig.AutoTrackAttackedSettlements == 1 && this.Settlement.MapFaction.Leader == Hero.MainHero)))
			{
				this.Track();
			}
		}

		// Token: 0x0600030C RID: 780 RVA: 0x0000D4B4 File Offset: 0x0000B6B4
		public void OnSiegeEventEndedOnSettlement(SiegeEvent siegeEvent)
		{
			Settlement settlement = this.Settlement;
			bool flag;
			if (settlement == null)
			{
				flag = null != null;
			}
			else
			{
				PartyBase party = settlement.Party;
				flag = ((party != null) ? party.MapEvent : null) != null;
			}
			if (flag && !this.Settlement.Party.MapEvent.IsFinalized)
			{
				this.OnMapEventStartedOnSettlement(this.Settlement.Party.MapEvent);
			}
			else
			{
				this.OnMapEventEndedOnSettlement();
			}
			if (!this._isTrackedManually && BannerlordConfig.AutoTrackAttackedSettlements < 2 && this.Settlement.MapFaction == Hero.MainHero.MapFaction)
			{
				this.Untrack();
			}
		}

		// Token: 0x0600030D RID: 781 RVA: 0x0000D544 File Offset: 0x0000B744
		public void OnMapEventStartedOnSettlement(MapEvent mapEvent)
		{
			this.MapEventVisualType = (int)SandBoxUIHelper.GetMapEventVisualTypeFromMapEvent(mapEvent);
			if (this.Settlement.MapFaction == Hero.MainHero.MapFaction && (this.Settlement.IsUnderRaid || this.Settlement.IsUnderSiege || this.Settlement.InRebelliousState) && (BannerlordConfig.AutoTrackAttackedSettlements == 0 || (BannerlordConfig.AutoTrackAttackedSettlements == 1 && this.Settlement.MapFaction.Leader == Hero.MainHero)))
			{
				this.Track();
			}
		}

		// Token: 0x0600030E RID: 782 RVA: 0x0000D5C8 File Offset: 0x0000B7C8
		public void OnMapEventEndedOnSettlement()
		{
			this.MapEventVisualType = 0;
			if (!this._isTrackedManually && BannerlordConfig.AutoTrackAttackedSettlements < 2 && !this.Settlement.IsUnderSiege && !this.Settlement.IsUnderRaid && !this.Settlement.InRebelliousState)
			{
				this.Untrack();
			}
		}

		// Token: 0x0600030F RID: 783 RVA: 0x0000D61C File Offset: 0x0000B81C
		public void OnRebelliousClanFormed(Clan clan)
		{
			this.MapEventVisualType = 4;
			this._rebelliousClans.Add(clan);
			if (this.Settlement.MapFaction == Hero.MainHero.MapFaction && (BannerlordConfig.AutoTrackAttackedSettlements == 0 || (BannerlordConfig.AutoTrackAttackedSettlements == 1 && this.Settlement.MapFaction.Leader == Hero.MainHero)))
			{
				this.Track();
			}
		}

		// Token: 0x06000310 RID: 784 RVA: 0x0000D680 File Offset: 0x0000B880
		public void OnRebelliousClanDisbanded(Clan clan)
		{
			this._rebelliousClans.Remove(clan);
			if (this._rebelliousClans.IsEmpty<Clan>())
			{
				if (this.Settlement.IsUnderSiege)
				{
					this.MapEventVisualType = 2;
					return;
				}
				this.MapEventVisualType = 0;
				if (!this._isTrackedManually && BannerlordConfig.AutoTrackAttackedSettlements < 2)
				{
					this.Untrack();
				}
			}
		}

		// Token: 0x06000311 RID: 785 RVA: 0x0000D6D9 File Offset: 0x0000B8D9
		public void UpdateNameplateMT(Vec3 cameraPosition)
		{
			this.CalculatePosition(cameraPosition);
			this.DetermineIsInsideWindow();
			this.DetermineIsVisibleOnMap(cameraPosition);
			this.RefreshPosition();
			this.RefreshDynamicProperties(false);
		}

		// Token: 0x06000312 RID: 786 RVA: 0x0000D700 File Offset: 0x0000B900
		private void CalculatePosition(in Vec3 cameraPosition)
		{
			this._worldPosWithHeight = this._worldPos;
			if (this._isVillage)
			{
				this._heightOffset = 0.5f + MathF.Clamp(cameraPosition.z / 30f, 0f, 1f) * 2.5f;
			}
			else if (this._isCastle)
			{
				this._heightOffset = 0.5f + MathF.Clamp(cameraPosition.z / 30f, 0f, 1f) * 3f;
			}
			else if (this._isTown)
			{
				this._heightOffset = 0.5f + MathF.Clamp(cameraPosition.z / 30f, 0f, 1f) * 6f;
			}
			else
			{
				this._heightOffset = 1f;
			}
			this._worldPosWithHeight += new Vec3(0f, 0f, this._heightOffset, -1f);
			if (this._worldPosWithHeight.IsValidXYZW && this._mapCamera.Position.IsValidXYZW)
			{
				this._latestX = 0f;
				this._latestY = 0f;
				this._latestW = 0f;
				MBWindowManager.WorldToScreenInsideUsableArea(this._mapCamera, this._worldPosWithHeight, ref this._latestX, ref this._latestY, ref this._latestW);
			}
			this._wPosAfterPositionCalculation = ((this._latestW < 0f) ? (-1f) : 1.1f);
		}

		// Token: 0x06000313 RID: 787 RVA: 0x0000D87E File Offset: 0x0000BA7E
		private void DetermineIsVisibleOnMap(in Vec3 cameraPosition)
		{
			this._bindIsVisibleOnMap = this.IsVisible(cameraPosition);
		}

		// Token: 0x06000314 RID: 788 RVA: 0x0000D88D File Offset: 0x0000BA8D
		private void DetermineIsInsideWindow()
		{
			this._latestIsInsideWindow = this.IsInsideWindow();
		}

		// Token: 0x06000315 RID: 789 RVA: 0x0000D89C File Offset: 0x0000BA9C
		public void RefreshBindValues()
		{
			base.FactionColor = this._bindFactionColor;
			this.Banner = this._bindBanner;
			this.Relation = this._bindRelation;
			this.WPos = this._bindWPos;
			this.WSign = this._bindWSign;
			this.IsInside = this._bindIsInside;
			base.Position = this._bindPosition;
			base.IsVisibleOnMap = this._bindIsVisibleOnMap;
			this.IsInRange = this._bindIsInRange;
			this.HasPort = this._bindHasPort;
			this.IsTracked = this._bindIsTracked;
			base.IsTargetedByTutorial = this._bindIsTargetedByTutorial;
			base.DistanceToCamera = this._bindDistanceToCamera;
			this.Name = this._bindName;
			if (this.SettlementNotifications.IsEventsRegistered)
			{
				this.SettlementNotifications.Tick();
			}
			if (this.SettlementEvents.IsEventsRegistered)
			{
				this.SettlementEvents.Tick();
			}
		}

		// Token: 0x06000316 RID: 790 RVA: 0x0000D984 File Offset: 0x0000BB84
		private bool IsVisible(in Vec3 cameraPosition)
		{
			this._bindDistanceToCamera = this._worldPos.Distance(cameraPosition);
			if (this.IsTracked)
			{
				return true;
			}
			if (this.WPos < 0f || !this._latestIsInsideWindow)
			{
				return false;
			}
			if (cameraPosition.z > 400f)
			{
				return this.Settlement.IsTown;
			}
			if (cameraPosition.z > 200f)
			{
				return this.Settlement.IsFortification;
			}
			return this._bindDistanceToCamera < cameraPosition.z + 100f;
		}

		// Token: 0x06000317 RID: 791 RVA: 0x0000DA10 File Offset: 0x0000BC10
		private bool IsInsideWindow()
		{
			float num = Screen.RealScreenResolutionWidth * 0.00052083336f;
			return this._latestX <= Screen.RealScreenResolutionWidth + 200f * num && this._latestY <= Screen.RealScreenResolutionHeight + 100f * num && this._latestX + 200f * num >= 0f && this._latestY + 100f * num >= 0f;
		}

		// Token: 0x06000318 RID: 792 RVA: 0x0000DA82 File Offset: 0x0000BC82
		public void ExecuteTrack()
		{
			if (this.IsTracked)
			{
				this.Untrack();
				this._isTrackedManually = false;
				return;
			}
			this.Track();
			this._isTrackedManually = true;
		}

		// Token: 0x06000319 RID: 793 RVA: 0x0000DAA7 File Offset: 0x0000BCA7
		private void Track()
		{
			this.IsTracked = true;
			if (!Campaign.Current.VisualTrackerManager.CheckTracked(this.Settlement))
			{
				Campaign.Current.VisualTrackerManager.RegisterObject(this.Settlement);
			}
		}

		// Token: 0x0600031A RID: 794 RVA: 0x0000DADC File Offset: 0x0000BCDC
		private void Untrack()
		{
			this.IsTracked = false;
			if (Campaign.Current.VisualTrackerManager.CheckTracked(this.Settlement))
			{
				Campaign.Current.VisualTrackerManager.RemoveTrackedObject(this.Settlement, false);
			}
		}

		// Token: 0x0600031B RID: 795 RVA: 0x0000DB12 File Offset: 0x0000BD12
		public void ExecuteSetCameraPosition()
		{
			this._fastMoveCameraToPosition(this.Settlement.Position);
		}

		// Token: 0x0600031C RID: 796 RVA: 0x0000DB2A File Offset: 0x0000BD2A
		public void ExecuteOpenEncyclopedia()
		{
			Campaign.Current.EncyclopediaManager.GoToLink(this.Settlement.EncyclopediaLink);
		}

		// Token: 0x170000F5 RID: 245
		// (get) Token: 0x0600031D RID: 797 RVA: 0x0000DB46 File Offset: 0x0000BD46
		// (set) Token: 0x0600031E RID: 798 RVA: 0x0000DB4E File Offset: 0x0000BD4E
		public SettlementNameplateNotificationsVM SettlementNotifications
		{
			get
			{
				return this._settlementNotifications;
			}
			set
			{
				if (value != this._settlementNotifications)
				{
					this._settlementNotifications = value;
					base.OnPropertyChangedWithValue<SettlementNameplateNotificationsVM>(value, "SettlementNotifications");
				}
			}
		}

		// Token: 0x170000F6 RID: 246
		// (get) Token: 0x0600031F RID: 799 RVA: 0x0000DB6C File Offset: 0x0000BD6C
		// (set) Token: 0x06000320 RID: 800 RVA: 0x0000DB74 File Offset: 0x0000BD74
		public SettlementNameplatePartyMarkersVM SettlementParties
		{
			get
			{
				return this._settlementParties;
			}
			set
			{
				if (value != this._settlementParties)
				{
					this._settlementParties = value;
					base.OnPropertyChangedWithValue<SettlementNameplatePartyMarkersVM>(value, "SettlementParties");
				}
			}
		}

		// Token: 0x170000F7 RID: 247
		// (get) Token: 0x06000321 RID: 801 RVA: 0x0000DB92 File Offset: 0x0000BD92
		// (set) Token: 0x06000322 RID: 802 RVA: 0x0000DB9A File Offset: 0x0000BD9A
		public SettlementNameplateEventsVM SettlementEvents
		{
			get
			{
				return this._settlementEvents;
			}
			set
			{
				if (value != this._settlementEvents)
				{
					this._settlementEvents = value;
					base.OnPropertyChangedWithValue<SettlementNameplateEventsVM>(value, "SettlementEvents");
				}
			}
		}

		// Token: 0x170000F8 RID: 248
		// (get) Token: 0x06000323 RID: 803 RVA: 0x0000DBB8 File Offset: 0x0000BDB8
		// (set) Token: 0x06000324 RID: 804 RVA: 0x0000DBC0 File Offset: 0x0000BDC0
		public int Relation
		{
			get
			{
				return this._relation;
			}
			set
			{
				if (value != this._relation)
				{
					this._relation = value;
					base.OnPropertyChangedWithValue(value, "Relation");
				}
			}
		}

		// Token: 0x170000F9 RID: 249
		// (get) Token: 0x06000325 RID: 805 RVA: 0x0000DBDE File Offset: 0x0000BDDE
		// (set) Token: 0x06000326 RID: 806 RVA: 0x0000DBE6 File Offset: 0x0000BDE6
		public int MapEventVisualType
		{
			get
			{
				return this._mapEventVisualType;
			}
			set
			{
				if (value != this._mapEventVisualType)
				{
					this._mapEventVisualType = value;
					base.OnPropertyChangedWithValue(value, "MapEventVisualType");
				}
			}
		}

		// Token: 0x170000FA RID: 250
		// (get) Token: 0x06000327 RID: 807 RVA: 0x0000DC04 File Offset: 0x0000BE04
		// (set) Token: 0x06000328 RID: 808 RVA: 0x0000DC0C File Offset: 0x0000BE0C
		public int WSign
		{
			get
			{
				return this._wSign;
			}
			set
			{
				if (value != this._wSign)
				{
					this._wSign = value;
					base.OnPropertyChangedWithValue(value, "WSign");
				}
			}
		}

		// Token: 0x170000FB RID: 251
		// (get) Token: 0x06000329 RID: 809 RVA: 0x0000DC2A File Offset: 0x0000BE2A
		// (set) Token: 0x0600032A RID: 810 RVA: 0x0000DC32 File Offset: 0x0000BE32
		public float WPos
		{
			get
			{
				return this._wPos;
			}
			set
			{
				if (value != this._wPos)
				{
					this._wPos = value;
					base.OnPropertyChangedWithValue(value, "WPos");
				}
			}
		}

		// Token: 0x170000FC RID: 252
		// (get) Token: 0x0600032B RID: 811 RVA: 0x0000DC50 File Offset: 0x0000BE50
		// (set) Token: 0x0600032C RID: 812 RVA: 0x0000DC58 File Offset: 0x0000BE58
		public BannerImageIdentifierVM Banner
		{
			get
			{
				return this._banner;
			}
			set
			{
				if (value != this._banner)
				{
					this._banner = value;
					base.OnPropertyChangedWithValue<BannerImageIdentifierVM>(value, "Banner");
				}
			}
		}

		// Token: 0x170000FD RID: 253
		// (get) Token: 0x0600032D RID: 813 RVA: 0x0000DC76 File Offset: 0x0000BE76
		// (set) Token: 0x0600032E RID: 814 RVA: 0x0000DC7E File Offset: 0x0000BE7E
		public string Name
		{
			get
			{
				return this._name;
			}
			set
			{
				if (value != this._name)
				{
					this._name = value;
					base.OnPropertyChangedWithValue<string>(value, "Name");
				}
			}
		}

		// Token: 0x170000FE RID: 254
		// (get) Token: 0x0600032F RID: 815 RVA: 0x0000DCA1 File Offset: 0x0000BEA1
		// (set) Token: 0x06000330 RID: 816 RVA: 0x0000DCB3 File Offset: 0x0000BEB3
		public bool IsTracked
		{
			get
			{
				return this._isTracked || this._bindIsTargetedByTutorial;
			}
			set
			{
				if (value != this._isTracked)
				{
					this._isTracked = value;
					base.OnPropertyChangedWithValue(value, "IsTracked");
				}
			}
		}

		// Token: 0x170000FF RID: 255
		// (get) Token: 0x06000331 RID: 817 RVA: 0x0000DCD1 File Offset: 0x0000BED1
		// (set) Token: 0x06000332 RID: 818 RVA: 0x0000DCD9 File Offset: 0x0000BED9
		public bool IsInside
		{
			get
			{
				return this._isInside;
			}
			set
			{
				if (value != this._isInside)
				{
					this._isInside = value;
					base.OnPropertyChangedWithValue(value, "IsInside");
				}
			}
		}

		// Token: 0x17000100 RID: 256
		// (get) Token: 0x06000333 RID: 819 RVA: 0x0000DCF7 File Offset: 0x0000BEF7
		// (set) Token: 0x06000334 RID: 820 RVA: 0x0000DD00 File Offset: 0x0000BF00
		public bool IsInRange
		{
			get
			{
				return this._isInRange;
			}
			set
			{
				if (value != this._isInRange)
				{
					this._isInRange = value;
					base.OnPropertyChangedWithValue(value, "IsInRange");
					if (this.IsInRange)
					{
						this.SettlementNotifications.RegisterEvents();
						this.SettlementParties.RegisterEvents();
						SettlementNameplateEventsVM settlementEvents = this.SettlementEvents;
						if (settlementEvents == null)
						{
							return;
						}
						settlementEvents.RegisterEvents();
						return;
					}
					else
					{
						this.SettlementNotifications.UnloadEvents();
						this.SettlementParties.UnloadEvents();
						SettlementNameplateEventsVM settlementEvents2 = this.SettlementEvents;
						if (settlementEvents2 == null)
						{
							return;
						}
						settlementEvents2.UnloadEvents();
					}
				}
			}
		}

		// Token: 0x17000101 RID: 257
		// (get) Token: 0x06000335 RID: 821 RVA: 0x0000DD7E File Offset: 0x0000BF7E
		// (set) Token: 0x06000336 RID: 822 RVA: 0x0000DD86 File Offset: 0x0000BF86
		public bool HasPort
		{
			get
			{
				return this._hasPort;
			}
			set
			{
				if (value != this._hasPort)
				{
					this._hasPort = value;
					base.OnPropertyChangedWithValue(value, "HasPort");
				}
			}
		}

		// Token: 0x17000102 RID: 258
		// (get) Token: 0x06000337 RID: 823 RVA: 0x0000DDA4 File Offset: 0x0000BFA4
		// (set) Token: 0x06000338 RID: 824 RVA: 0x0000DDAC File Offset: 0x0000BFAC
		public int SettlementType
		{
			get
			{
				return this._settlementType;
			}
			set
			{
				if (value != this._settlementType)
				{
					this._settlementType = value;
					base.OnPropertyChangedWithValue(value, "SettlementType");
				}
			}
		}

		// Token: 0x04000173 RID: 371
		private readonly Camera _mapCamera;

		// Token: 0x04000176 RID: 374
		private float _latestX;

		// Token: 0x04000177 RID: 375
		private float _latestY;

		// Token: 0x04000178 RID: 376
		private float _latestW;

		// Token: 0x04000179 RID: 377
		private float _heightOffset;

		// Token: 0x0400017A RID: 378
		private bool _latestIsInsideWindow;

		// Token: 0x0400017B RID: 379
		private Banner _latestBanner;

		// Token: 0x0400017C RID: 380
		private int _latestBannerVersionNo;

		// Token: 0x0400017D RID: 381
		private bool _isTrackedManually;

		// Token: 0x0400017E RID: 382
		private readonly GameEntity _entity;

		// Token: 0x0400017F RID: 383
		private Vec3 _worldPos;

		// Token: 0x04000180 RID: 384
		private Vec3 _worldPosWithHeight;

		// Token: 0x04000181 RID: 385
		private IFaction _currentFaction;

		// Token: 0x04000182 RID: 386
		private readonly Action<CampaignVec2> _fastMoveCameraToPosition;

		// Token: 0x04000183 RID: 387
		private readonly bool _isVillage;

		// Token: 0x04000184 RID: 388
		private readonly bool _isCastle;

		// Token: 0x04000185 RID: 389
		private readonly bool _isTown;

		// Token: 0x04000186 RID: 390
		private float _wPosAfterPositionCalculation;

		// Token: 0x04000187 RID: 391
		private string _bindName;

		// Token: 0x04000188 RID: 392
		private string _bindFactionColor;

		// Token: 0x04000189 RID: 393
		private bool _bindIsTracked;

		// Token: 0x0400018A RID: 394
		private BannerImageIdentifierVM _bindBanner;

		// Token: 0x0400018B RID: 395
		private int _bindRelation;

		// Token: 0x0400018C RID: 396
		private float _bindWPos;

		// Token: 0x0400018D RID: 397
		private float _bindDistanceToCamera;

		// Token: 0x0400018E RID: 398
		private int _bindWSign;

		// Token: 0x0400018F RID: 399
		private bool _bindIsInside;

		// Token: 0x04000190 RID: 400
		private Vec2 _bindPosition;

		// Token: 0x04000191 RID: 401
		private bool _bindIsVisibleOnMap;

		// Token: 0x04000192 RID: 402
		private bool _bindIsInRange;

		// Token: 0x04000193 RID: 403
		private bool _bindHasPort;

		// Token: 0x04000194 RID: 404
		private List<Clan> _rebelliousClans;

		// Token: 0x04000195 RID: 405
		private string _name;

		// Token: 0x04000196 RID: 406
		private int _settlementType = -1;

		// Token: 0x04000197 RID: 407
		private BannerImageIdentifierVM _banner;

		// Token: 0x04000198 RID: 408
		private int _relation;

		// Token: 0x04000199 RID: 409
		private int _wSign;

		// Token: 0x0400019A RID: 410
		private float _wPos;

		// Token: 0x0400019B RID: 411
		private bool _isTracked;

		// Token: 0x0400019C RID: 412
		private bool _isInside;

		// Token: 0x0400019D RID: 413
		private bool _isInRange;

		// Token: 0x0400019E RID: 414
		private bool _hasPort;

		// Token: 0x0400019F RID: 415
		private int _mapEventVisualType;

		// Token: 0x040001A0 RID: 416
		private SettlementNameplateNotificationsVM _settlementNotifications;

		// Token: 0x040001A1 RID: 417
		private SettlementNameplatePartyMarkersVM _settlementParties;

		// Token: 0x040001A2 RID: 418
		private SettlementNameplateEventsVM _settlementEvents;

		// Token: 0x0200008C RID: 140
		public enum Type
		{
			// Token: 0x04000382 RID: 898
			Village,
			// Token: 0x04000383 RID: 899
			Castle,
			// Token: 0x04000384 RID: 900
			Town
		}

		// Token: 0x0200008D RID: 141
		public enum RelationType
		{
			// Token: 0x04000386 RID: 902
			Neutral,
			// Token: 0x04000387 RID: 903
			Ally,
			// Token: 0x04000388 RID: 904
			Enemy
		}

		// Token: 0x0200008E RID: 142
		public enum IssueTypes
		{
			// Token: 0x0400038A RID: 906
			None,
			// Token: 0x0400038B RID: 907
			Possible,
			// Token: 0x0400038C RID: 908
			Active
		}

		// Token: 0x0200008F RID: 143
		public enum MainQuestTypes
		{
			// Token: 0x0400038E RID: 910
			None,
			// Token: 0x0400038F RID: 911
			Possible,
			// Token: 0x04000390 RID: 912
			Active
		}
	}
}
