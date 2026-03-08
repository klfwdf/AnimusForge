using System;
using System.Collections.Generic;
using System.Linq;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.MapEvents;

namespace SandBox.GauntletUI.Map
{
	// Token: 0x02000036 RID: 54
	public class GauntletMapEventVisualCreator : IMapEventVisualCreator
	{
		// Token: 0x0600028F RID: 655 RVA: 0x0000F830 File Offset: 0x0000DA30
		public IMapEventVisual CreateMapEventVisual(MapEvent mapEvent)
		{
			GauntletMapEventVisual newEventVisual = new GauntletMapEventVisual(mapEvent, new Action<GauntletMapEventVisual>(this.OnMapEventInitialized), new Action<GauntletMapEventVisual>(this.OnMapEventVisibilityChanged), new Action<GauntletMapEventVisual>(this.OnMapEventOver));
			List<IGauntletMapEventVisualHandler> handlers = this.Handlers;
			if (handlers != null)
			{
				handlers.ForEach(delegate(IGauntletMapEventVisualHandler h)
				{
					h.OnNewEventStarted(newEventVisual);
				});
			}
			this._listOfEvents.Add(newEventVisual);
			return newEventVisual;
		}

		// Token: 0x06000290 RID: 656 RVA: 0x0000F8A8 File Offset: 0x0000DAA8
		private void OnMapEventOver(GauntletMapEventVisual overEvent)
		{
			this._listOfEvents.Remove(overEvent);
			List<IGauntletMapEventVisualHandler> handlers = this.Handlers;
			if (handlers == null)
			{
				return;
			}
			handlers.ForEach(delegate(IGauntletMapEventVisualHandler h)
			{
				h.OnEventEnded(overEvent);
			});
		}

		// Token: 0x06000291 RID: 657 RVA: 0x0000F8F0 File Offset: 0x0000DAF0
		private void OnMapEventInitialized(GauntletMapEventVisual initializedEvent)
		{
			List<IGauntletMapEventVisualHandler> handlers = this.Handlers;
			if (handlers == null)
			{
				return;
			}
			handlers.ForEach(delegate(IGauntletMapEventVisualHandler h)
			{
				h.OnInitialized(initializedEvent);
			});
		}

		// Token: 0x06000292 RID: 658 RVA: 0x0000F928 File Offset: 0x0000DB28
		private void OnMapEventVisibilityChanged(GauntletMapEventVisual visibilityChangedEvent)
		{
			List<IGauntletMapEventVisualHandler> handlers = this.Handlers;
			if (handlers == null)
			{
				return;
			}
			handlers.ForEach(delegate(IGauntletMapEventVisualHandler h)
			{
				h.OnEventVisibilityChanged(visibilityChangedEvent);
			});
		}

		// Token: 0x06000293 RID: 659 RVA: 0x0000F95E File Offset: 0x0000DB5E
		public IEnumerable<GauntletMapEventVisual> GetCurrentEvents()
		{
			return this._listOfEvents.AsEnumerable<GauntletMapEventVisual>();
		}

		// Token: 0x040000ED RID: 237
		public List<IGauntletMapEventVisualHandler> Handlers = new List<IGauntletMapEventVisualHandler>();

		// Token: 0x040000EE RID: 238
		private readonly List<GauntletMapEventVisual> _listOfEvents = new List<GauntletMapEventVisual>();
	}
}
