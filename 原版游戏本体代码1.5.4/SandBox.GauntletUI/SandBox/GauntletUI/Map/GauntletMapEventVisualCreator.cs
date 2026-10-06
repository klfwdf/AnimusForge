using System.Collections.Generic;
using System.Linq;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.MapEvents;

namespace SandBox.GauntletUI.Map;

public class GauntletMapEventVisualCreator : IMapEventVisualCreator
{
	public List<IGauntletMapEventVisualHandler> Handlers = new List<IGauntletMapEventVisualHandler>();

	private readonly List<GauntletMapEventVisual> _listOfEvents = new List<GauntletMapEventVisual>();

	public IMapEventVisual CreateMapEventVisual(MapEvent mapEvent)
	{
		GauntletMapEventVisual newEventVisual = new GauntletMapEventVisual(mapEvent, OnMapEventInitialized, OnMapEventVisibilityChanged, OnMapEventOver);
		Handlers?.ForEach(delegate(IGauntletMapEventVisualHandler h)
		{
			h.OnNewEventStarted(newEventVisual);
		});
		_listOfEvents.Add(newEventVisual);
		return newEventVisual;
	}

	private void OnMapEventOver(GauntletMapEventVisual overEvent)
	{
		_listOfEvents.Remove(overEvent);
		Handlers?.ForEach(delegate(IGauntletMapEventVisualHandler h)
		{
			h.OnEventEnded(overEvent);
		});
	}

	private void OnMapEventInitialized(GauntletMapEventVisual initializedEvent)
	{
		Handlers?.ForEach(delegate(IGauntletMapEventVisualHandler h)
		{
			h.OnInitialized(initializedEvent);
		});
	}

	private void OnMapEventVisibilityChanged(GauntletMapEventVisual visibilityChangedEvent)
	{
		Handlers?.ForEach(delegate(IGauntletMapEventVisualHandler h)
		{
			h.OnEventVisibilityChanged(visibilityChangedEvent);
		});
	}

	public IEnumerable<GauntletMapEventVisual> GetCurrentEvents()
	{
		return _listOfEvents.AsEnumerable();
	}
}
