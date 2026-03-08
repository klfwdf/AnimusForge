using System;
using TaleWorlds.CampaignSystem.ViewModelCollection.Map.Tracker;
using TaleWorlds.Library;

namespace SandBox.ViewModelCollection.Map.Tracker
{
	// Token: 0x0200004A RID: 74
	public class MapTrackerCollectionVM : ViewModel
	{
		// Token: 0x0600048A RID: 1162 RVA: 0x00011C90 File Offset: 0x0000FE90
		public MapTrackerCollectionVM()
		{
			this._mapTrackerProvider = new MapTrackerProvider();
			this.Trackers = new MBBindingList<MapTrackerItemVM>();
			foreach (MapTrackerItemVM item in this._mapTrackerProvider.GetTrackers())
			{
				this.Trackers.Add(item);
			}
			this._mapTrackerProvider.OnTrackerAddedOrRemoved += this.OnTrackerAddedOrRemoved;
		}

		// Token: 0x0600048B RID: 1163 RVA: 0x00011CFA File Offset: 0x0000FEFA
		private void OnTrackerAddedOrRemoved(MapTrackerItemVM item, bool added)
		{
			if (added)
			{
				this.Trackers.Add(item);
				return;
			}
			this.Trackers.Remove(item);
		}

		// Token: 0x0600048C RID: 1164 RVA: 0x00011D1C File Offset: 0x0000FF1C
		public void Tick(float dt)
		{
			for (int i = 0; i < this.Trackers.Count; i++)
			{
				this.Trackers[i].RefreshBinding();
			}
		}

		// Token: 0x0600048D RID: 1165 RVA: 0x00011D50 File Offset: 0x0000FF50
		public override void OnFinalize()
		{
			base.OnFinalize();
			this._mapTrackerProvider.OnTrackerAddedOrRemoved -= this.OnTrackerAddedOrRemoved;
			this.Trackers.ApplyActionOnAllItems(delegate(MapTrackerItemVM t)
			{
				t.OnFinalize();
			});
		}

		// Token: 0x0600048E RID: 1166 RVA: 0x00011DA4 File Offset: 0x0000FFA4
		public void UpdateProperties()
		{
			this.Trackers.ApplyActionOnAllItems(delegate(MapTrackerItemVM t)
			{
				t.UpdateProperties();
			});
		}

		// Token: 0x1700015A RID: 346
		// (get) Token: 0x0600048F RID: 1167 RVA: 0x00011DD0 File Offset: 0x0000FFD0
		// (set) Token: 0x06000490 RID: 1168 RVA: 0x00011DD8 File Offset: 0x0000FFD8
		public MBBindingList<MapTrackerItemVM> Trackers
		{
			get
			{
				return this._trackers;
			}
			set
			{
				if (value != this._trackers)
				{
					this._trackers = value;
					base.OnPropertyChangedWithValue<MBBindingList<MapTrackerItemVM>>(value, "Trackers");
				}
			}
		}

		// Token: 0x04000249 RID: 585
		private readonly MapTrackerProvider _mapTrackerProvider;

		// Token: 0x0400024A RID: 586
		private MBBindingList<MapTrackerItemVM> _trackers;
	}
}
