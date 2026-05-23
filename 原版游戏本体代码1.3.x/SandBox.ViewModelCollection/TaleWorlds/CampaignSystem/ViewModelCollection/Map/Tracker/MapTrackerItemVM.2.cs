using System;
using TaleWorlds.CampaignSystem.ViewModelCollection.Quests;
using TaleWorlds.Core;
using TaleWorlds.Core.ViewModelCollection.ImageIdentifiers;
using TaleWorlds.Library;

namespace TaleWorlds.CampaignSystem.ViewModelCollection.Map.Tracker
{
	// Token: 0x02000005 RID: 5
	public abstract class MapTrackerItemVM : ViewModel
	{
		// Token: 0x0600000C RID: 12 RVA: 0x000022ED File Offset: 0x000004ED
		public MapTrackerItemVM(ITrackableCampaignObject trackedObject)
		{
			this.TrackedObject = trackedObject;
			this.Quests = new MBBindingList<QuestMarkerVM>();
			this.UpdateProperties();
		}

		// Token: 0x0600000D RID: 13
		protected abstract void OnShowTooltip();

		// Token: 0x0600000E RID: 14
		protected abstract void OnUpdateProperties();

		// Token: 0x0600000F RID: 15
		protected abstract void OnUpdatePosition(float screenX, float screenY, float screenW);

		// Token: 0x06000010 RID: 16
		protected abstract void OnToggleTrack();

		// Token: 0x06000011 RID: 17
		protected abstract void OnGoToPosition();

		// Token: 0x06000012 RID: 18
		protected abstract void OnRefreshBinding();

		// Token: 0x06000013 RID: 19
		protected abstract bool IsVisibleOnMap();

		// Token: 0x06000014 RID: 20
		protected abstract bool GetCanToggleTrack();

		// Token: 0x06000015 RID: 21
		protected abstract string GetTrackerType();

		// Token: 0x06000016 RID: 22
		protected abstract CampaignUIHelper.IssueQuestFlags GetRelatedQuests();

		// Token: 0x06000017 RID: 23 RVA: 0x0000230D File Offset: 0x0000050D
		public void UpdateProperties()
		{
			this.OnUpdateProperties();
		}

		// Token: 0x06000018 RID: 24 RVA: 0x00002315 File Offset: 0x00000515
		public void UpdatePosition(float screenX, float screenY, float screenW)
		{
			this.OnUpdatePosition(screenX, screenY, screenW);
		}

		// Token: 0x06000019 RID: 25 RVA: 0x00002320 File Offset: 0x00000520
		public void ExecuteToggleTrack()
		{
			this.OnToggleTrack();
		}

		// Token: 0x0600001A RID: 26 RVA: 0x00002328 File Offset: 0x00000528
		public void ExecuteGoToPosition()
		{
			this.OnGoToPosition();
		}

		// Token: 0x0600001B RID: 27 RVA: 0x00002330 File Offset: 0x00000530
		public void ExecuteShowTooltip()
		{
			this.OnShowTooltip();
		}

		// Token: 0x0600001C RID: 28 RVA: 0x00002338 File Offset: 0x00000538
		public void ExecuteHideTooltip()
		{
			MBInformationManager.HideInformations();
		}

		// Token: 0x0600001D RID: 29 RVA: 0x0000233F File Offset: 0x0000053F
		public void RefreshBinding()
		{
			this.OnRefreshBinding();
			this.TrackerType = this.GetTrackerType();
		}

		// Token: 0x17000002 RID: 2
		// (get) Token: 0x0600001E RID: 30 RVA: 0x00002353 File Offset: 0x00000553
		// (set) Token: 0x0600001F RID: 31 RVA: 0x0000235B File Offset: 0x0000055B
		[DataSourceProperty]
		public bool IsTracked
		{
			get
			{
				return this._isTracked;
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

		// Token: 0x17000003 RID: 3
		// (get) Token: 0x06000020 RID: 32 RVA: 0x00002379 File Offset: 0x00000579
		// (set) Token: 0x06000021 RID: 33 RVA: 0x00002381 File Offset: 0x00000581
		[DataSourceProperty]
		public bool CanToggleTrack
		{
			get
			{
				return this._canToggleTrack;
			}
			set
			{
				if (value != this._canToggleTrack)
				{
					this._canToggleTrack = value;
					base.OnPropertyChangedWithValue(value, "CanToggleTrack");
				}
			}
		}

		// Token: 0x17000004 RID: 4
		// (get) Token: 0x06000022 RID: 34 RVA: 0x0000239F File Offset: 0x0000059F
		// (set) Token: 0x06000023 RID: 35 RVA: 0x000023A7 File Offset: 0x000005A7
		[DataSourceProperty]
		public bool IsEnabled
		{
			get
			{
				return this._isEnabled;
			}
			set
			{
				if (value != this._isEnabled)
				{
					this._isEnabled = value;
					base.OnPropertyChangedWithValue(value, "IsEnabled");
				}
			}
		}

		// Token: 0x17000005 RID: 5
		// (get) Token: 0x06000024 RID: 36 RVA: 0x000023C5 File Offset: 0x000005C5
		// (set) Token: 0x06000025 RID: 37 RVA: 0x000023CD File Offset: 0x000005CD
		[DataSourceProperty]
		public bool IsBehind
		{
			get
			{
				return this._isBehind;
			}
			set
			{
				if (value != this._isBehind)
				{
					this._isBehind = value;
					base.OnPropertyChangedWithValue(value, "IsBehind");
				}
			}
		}

		// Token: 0x17000006 RID: 6
		// (get) Token: 0x06000026 RID: 38 RVA: 0x000023EB File Offset: 0x000005EB
		// (set) Token: 0x06000027 RID: 39 RVA: 0x000023F3 File Offset: 0x000005F3
		[DataSourceProperty]
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

		// Token: 0x17000007 RID: 7
		// (get) Token: 0x06000028 RID: 40 RVA: 0x00002416 File Offset: 0x00000616
		// (set) Token: 0x06000029 RID: 41 RVA: 0x0000241E File Offset: 0x0000061E
		[DataSourceProperty]
		public string TrackerType
		{
			get
			{
				return this._trackerType;
			}
			set
			{
				if (value != this._trackerType)
				{
					this._trackerType = value;
					base.OnPropertyChangedWithValue<string>(value, "TrackerType");
				}
			}
		}

		// Token: 0x17000008 RID: 8
		// (get) Token: 0x0600002A RID: 42 RVA: 0x00002441 File Offset: 0x00000641
		// (set) Token: 0x0600002B RID: 43 RVA: 0x00002449 File Offset: 0x00000649
		[DataSourceProperty]
		public Vec2 PartyPosition
		{
			get
			{
				return this._partyPosition;
			}
			set
			{
				if (value != this._partyPosition)
				{
					this._partyPosition = value;
					base.OnPropertyChangedWithValue(value, "PartyPosition");
				}
			}
		}

		// Token: 0x17000009 RID: 9
		// (get) Token: 0x0600002C RID: 44 RVA: 0x0000246C File Offset: 0x0000066C
		// (set) Token: 0x0600002D RID: 45 RVA: 0x00002474 File Offset: 0x00000674
		[DataSourceProperty]
		public BannerImageIdentifierVM FactionVisual
		{
			get
			{
				return this._factionVisual;
			}
			set
			{
				if (value != this._factionVisual)
				{
					this._factionVisual = value;
					base.OnPropertyChangedWithValue<BannerImageIdentifierVM>(value, "FactionVisual");
				}
			}
		}

		// Token: 0x1700000A RID: 10
		// (get) Token: 0x0600002E RID: 46 RVA: 0x00002492 File Offset: 0x00000692
		// (set) Token: 0x0600002F RID: 47 RVA: 0x0000249A File Offset: 0x0000069A
		[DataSourceProperty]
		public MBBindingList<QuestMarkerVM> Quests
		{
			get
			{
				return this._quests;
			}
			set
			{
				if (value != this._quests)
				{
					this._quests = value;
					base.OnPropertyChangedWithValue<MBBindingList<QuestMarkerVM>>(value, "Quests");
				}
			}
		}

		// Token: 0x04000001 RID: 1
		public readonly ITrackableCampaignObject TrackedObject;

		// Token: 0x04000002 RID: 2
		protected float _latestX;

		// Token: 0x04000003 RID: 3
		protected float _latestY;

		// Token: 0x04000004 RID: 4
		protected float _latestW;

		// Token: 0x04000005 RID: 5
		protected CampaignUIHelper.IssueQuestFlags _previousQuestsBind;

		// Token: 0x04000006 RID: 6
		protected CampaignUIHelper.IssueQuestFlags _questsBind;

		// Token: 0x04000007 RID: 7
		protected bool _isVisibleOnMapBind;

		// Token: 0x04000008 RID: 8
		protected bool _isBehindBind;

		// Token: 0x04000009 RID: 9
		protected bool _canToggleTrackBind;

		// Token: 0x0400000A RID: 10
		protected string _nameBind;

		// Token: 0x0400000B RID: 11
		protected Vec2 _partyPositionBind;

		// Token: 0x0400000C RID: 12
		protected BannerImageIdentifierVM _factionVisualBind;

		// Token: 0x0400000D RID: 13
		public static Action<CampaignVec2> OnFastMoveCameraToPosition;

		// Token: 0x0400000E RID: 14
		private bool _isTracked;

		// Token: 0x0400000F RID: 15
		private bool _canToggleTrack;

		// Token: 0x04000010 RID: 16
		private bool _isEnabled;

		// Token: 0x04000011 RID: 17
		private bool _isBehind;

		// Token: 0x04000012 RID: 18
		private string _name;

		// Token: 0x04000013 RID: 19
		private string _trackerType;

		// Token: 0x04000014 RID: 20
		private Vec2 _partyPosition;

		// Token: 0x04000015 RID: 21
		private BannerImageIdentifierVM _factionVisual;

		// Token: 0x04000016 RID: 22
		private MBBindingList<QuestMarkerVM> _quests;
	}
}
