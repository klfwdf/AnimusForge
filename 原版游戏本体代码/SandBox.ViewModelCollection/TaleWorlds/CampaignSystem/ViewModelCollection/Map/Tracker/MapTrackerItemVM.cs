using System;
using TaleWorlds.CampaignSystem.ViewModelCollection.Quests;
using TaleWorlds.Core;
using TaleWorlds.Core.ViewModelCollection.ImageIdentifiers;
using TaleWorlds.Library;

namespace TaleWorlds.CampaignSystem.ViewModelCollection.Map.Tracker
{
	// Token: 0x02000004 RID: 4
	public abstract class MapTrackerItemVM<T> : MapTrackerItemVM where T : ITrackableCampaignObject
	{
		// Token: 0x17000001 RID: 1
		// (get) Token: 0x06000003 RID: 3 RVA: 0x00002058 File Offset: 0x00000258
		public new T TrackedObject
		{
			get
			{
				return (T)((object)this.TrackedObject);
			}
		}

		// Token: 0x06000004 RID: 4 RVA: 0x00002065 File Offset: 0x00000265
		protected MapTrackerItemVM(T trackableObject)
			: base(trackableObject)
		{
			base.IsTracked = Campaign.Current.VisualTrackerManager.CheckTracked(this.TrackedObject);
		}

		// Token: 0x06000005 RID: 5 RVA: 0x00002094 File Offset: 0x00000294
		protected sealed override void OnUpdateProperties()
		{
			T trackedObject = this.TrackedObject;
			this._nameBind = trackedObject.GetName().ToString();
			trackedObject = this.TrackedObject;
			Banner banner = trackedObject.GetBanner();
			this._factionVisualBind = new BannerImageIdentifierVM(banner, true);
			this._isVisibleOnMapBind = this.IsVisibleOnMap();
			this._canToggleTrackBind = this.GetCanToggleTrack();
			this._questsBind = this.GetRelatedQuests();
		}

		// Token: 0x06000006 RID: 6 RVA: 0x00002108 File Offset: 0x00000308
		protected sealed override void OnUpdatePosition(float screenX, float screenY, float screenW)
		{
			this._latestX = screenX;
			this._latestY = screenY;
			this._latestW = screenW;
			this._partyPositionBind = new Vec2(this._latestX, this._latestY);
			this._isBehindBind = this._latestW < 0f;
		}

		// Token: 0x06000007 RID: 7 RVA: 0x00002154 File Offset: 0x00000354
		protected sealed override void OnToggleTrack()
		{
			if (!this.GetCanToggleTrack())
			{
				return;
			}
			if (base.IsTracked)
			{
				this.Untrack();
				return;
			}
			this.Track();
		}

		// Token: 0x06000008 RID: 8 RVA: 0x00002174 File Offset: 0x00000374
		protected sealed override void OnGoToPosition()
		{
			Action<CampaignVec2> onFastMoveCameraToPosition = MapTrackerItemVM.OnFastMoveCameraToPosition;
			if (onFastMoveCameraToPosition == null)
			{
				return;
			}
			T trackedObject = this.TrackedObject;
			onFastMoveCameraToPosition(new CampaignVec2(trackedObject.GetPosition().AsVec2, true));
		}

		// Token: 0x06000009 RID: 9 RVA: 0x000021B4 File Offset: 0x000003B4
		protected sealed override void OnRefreshBinding()
		{
			base.Name = this._nameBind;
			base.IsEnabled = this._isVisibleOnMapBind;
			base.IsBehind = this._isBehindBind;
			base.FactionVisual = this._factionVisualBind;
			base.CanToggleTrack = this._canToggleTrackBind;
			if (base.IsEnabled)
			{
				base.PartyPosition = this._partyPositionBind;
			}
			if (this._previousQuestsBind != this._questsBind)
			{
				base.Quests.Clear();
				foreach (CampaignUIHelper.IssueQuestFlags issueQuestFlags in CampaignUIHelper.IssueQuestFlagsValues)
				{
					if (issueQuestFlags != CampaignUIHelper.IssueQuestFlags.None && (this._questsBind & issueQuestFlags) != CampaignUIHelper.IssueQuestFlags.None)
					{
						base.Quests.Add(new QuestMarkerVM(issueQuestFlags, null, null));
					}
				}
				this._previousQuestsBind = this._questsBind;
			}
		}

		// Token: 0x0600000A RID: 10 RVA: 0x0000226E File Offset: 0x0000046E
		private void Track()
		{
			base.IsTracked = true;
			if (!Campaign.Current.VisualTrackerManager.CheckTracked(this.TrackedObject))
			{
				Campaign.Current.VisualTrackerManager.RegisterObject(this.TrackedObject);
			}
		}

		// Token: 0x0600000B RID: 11 RVA: 0x000022AD File Offset: 0x000004AD
		private void Untrack()
		{
			base.IsTracked = false;
			if (Campaign.Current.VisualTrackerManager.CheckTracked(this.TrackedObject))
			{
				Campaign.Current.VisualTrackerManager.RemoveTrackedObject(this.TrackedObject, false);
			}
		}
	}
}
