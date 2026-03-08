using System;
using System.Collections.Generic;
using TaleWorlds.CampaignSystem.MapEvents;
using TaleWorlds.Engine;
using TaleWorlds.Library;

namespace SandBox.ViewModelCollection.Map
{
	// Token: 0x02000045 RID: 69
	public class MapEventVisualsVM : ViewModel
	{
		// Token: 0x06000461 RID: 1121 RVA: 0x0001175D File Offset: 0x0000F95D
		public MapEventVisualsVM(Camera mapCamera)
		{
			this._mapCamera = mapCamera;
			this.MapEvents = new MBBindingList<MapEventVisualItemVM>();
			this.UpdateMapEventsAuxPredicate = new TWParallel.ParallelForAuxPredicate(this.UpdateMapEventsAux);
		}

		// Token: 0x06000462 RID: 1122 RVA: 0x00011794 File Offset: 0x0000F994
		private void UpdateMapEventsAux(int startInclusive, int endExclusive)
		{
			for (int i = startInclusive; i < endExclusive; i++)
			{
				this.MapEvents[i].ParallelUpdatePosition();
				this.MapEvents[i].DetermineIsVisibleOnMap();
			}
		}

		// Token: 0x06000463 RID: 1123 RVA: 0x000117D0 File Offset: 0x0000F9D0
		public void Update(float dt)
		{
			TWParallel.For(0, this.MapEvents.Count, this.UpdateMapEventsAuxPredicate, 16);
			for (int i = 0; i < this.MapEvents.Count; i++)
			{
				this.MapEvents[i].UpdateBindingProperties();
			}
		}

		// Token: 0x06000464 RID: 1124 RVA: 0x0001181D File Offset: 0x0000FA1D
		public void OnMapEventVisibilityChanged(MapEvent mapEvent)
		{
			if (this._eventToVisualMap.ContainsKey(mapEvent))
			{
				this._eventToVisualMap[mapEvent].UpdateProperties();
			}
		}

		// Token: 0x06000465 RID: 1125 RVA: 0x00011840 File Offset: 0x0000FA40
		public void OnMapEventStarted(MapEvent mapEvent)
		{
			if (!this._eventToVisualMap.ContainsKey(mapEvent))
			{
				if (!this.IsMapEventSettlementRelated(mapEvent))
				{
					MapEventVisualItemVM mapEventVisualItemVM = new MapEventVisualItemVM(this._mapCamera, mapEvent);
					this._eventToVisualMap.Add(mapEvent, mapEventVisualItemVM);
					this.MapEvents.Add(mapEventVisualItemVM);
					mapEventVisualItemVM.UpdateProperties();
				}
				return;
			}
			if (!this.IsMapEventSettlementRelated(mapEvent))
			{
				this._eventToVisualMap[mapEvent].UpdateProperties();
				return;
			}
			MapEventVisualItemVM item = this._eventToVisualMap[mapEvent];
			this.MapEvents.Remove(item);
			this._eventToVisualMap.Remove(mapEvent);
		}

		// Token: 0x06000466 RID: 1126 RVA: 0x000118D4 File Offset: 0x0000FAD4
		public void OnMapEventEnded(MapEvent mapEvent)
		{
			if (this._eventToVisualMap.ContainsKey(mapEvent))
			{
				MapEventVisualItemVM item = this._eventToVisualMap[mapEvent];
				this.MapEvents.Remove(item);
				this._eventToVisualMap.Remove(mapEvent);
			}
		}

		// Token: 0x06000467 RID: 1127 RVA: 0x00011916 File Offset: 0x0000FB16
		private bool IsMapEventSettlementRelated(MapEvent mapEvent)
		{
			return mapEvent.MapEventSettlement != null;
		}

		// Token: 0x17000155 RID: 341
		// (get) Token: 0x06000468 RID: 1128 RVA: 0x00011921 File Offset: 0x0000FB21
		// (set) Token: 0x06000469 RID: 1129 RVA: 0x00011929 File Offset: 0x0000FB29
		public MBBindingList<MapEventVisualItemVM> MapEvents
		{
			get
			{
				return this._mapEvents;
			}
			set
			{
				if (this._mapEvents != value)
				{
					this._mapEvents = value;
					base.OnPropertyChangedWithValue<MBBindingList<MapEventVisualItemVM>>(value, "MapEvents");
				}
			}
		}

		// Token: 0x04000238 RID: 568
		private readonly Camera _mapCamera;

		// Token: 0x04000239 RID: 569
		private readonly Dictionary<MapEvent, MapEventVisualItemVM> _eventToVisualMap = new Dictionary<MapEvent, MapEventVisualItemVM>();

		// Token: 0x0400023A RID: 570
		private readonly TWParallel.ParallelForAuxPredicate UpdateMapEventsAuxPredicate;

		// Token: 0x0400023B RID: 571
		private MBBindingList<MapEventVisualItemVM> _mapEvents;
	}
}
