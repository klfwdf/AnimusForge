using System.Collections.Generic;
using TaleWorlds.CampaignSystem.MapEvents;
using TaleWorlds.Engine;
using TaleWorlds.Library;

namespace SandBox.ViewModelCollection.Map;

public class MapEventVisualsVM : ViewModel
{
	private readonly Camera _mapCamera;

	private readonly Dictionary<MapEvent, MapEventVisualItemVM> _eventToVisualMap = new Dictionary<MapEvent, MapEventVisualItemVM>();

	private readonly TWParallel.ParallelForAuxPredicate UpdateMapEventsAuxPredicate;

	private MBBindingList<MapEventVisualItemVM> _mapEvents;

	public MBBindingList<MapEventVisualItemVM> MapEvents
	{
		get
		{
			return _mapEvents;
		}
		set
		{
			if (_mapEvents != value)
			{
				_mapEvents = value;
				OnPropertyChangedWithValue(value, "MapEvents");
			}
		}
	}

	public MapEventVisualsVM(Camera mapCamera)
	{
		_mapCamera = mapCamera;
		MapEvents = new MBBindingList<MapEventVisualItemVM>();
		UpdateMapEventsAuxPredicate = UpdateMapEventsAux;
	}

	private void UpdateMapEventsAux(int startInclusive, int endExclusive)
	{
		for (int i = startInclusive; i < endExclusive; i++)
		{
			MapEvents[i].ParallelUpdatePosition();
			MapEvents[i].DetermineIsVisibleOnMap();
		}
	}

	public void Update(float dt)
	{
		TWParallel.For(0, MapEvents.Count, UpdateMapEventsAuxPredicate);
		for (int i = 0; i < MapEvents.Count; i++)
		{
			MapEvents[i].UpdateBindingProperties();
		}
	}

	public void OnMapEventVisibilityChanged(MapEvent mapEvent)
	{
		if (_eventToVisualMap.ContainsKey(mapEvent))
		{
			_eventToVisualMap[mapEvent].UpdateProperties();
		}
	}

	public void OnMapEventStarted(MapEvent mapEvent)
	{
		if (_eventToVisualMap.ContainsKey(mapEvent))
		{
			if (!IsMapEventSettlementRelated(mapEvent))
			{
				_eventToVisualMap[mapEvent].UpdateProperties();
				return;
			}
			MapEventVisualItemVM item = _eventToVisualMap[mapEvent];
			MapEvents.Remove(item);
			_eventToVisualMap.Remove(mapEvent);
		}
		else if (!IsMapEventSettlementRelated(mapEvent))
		{
			MapEventVisualItemVM mapEventVisualItemVM = new MapEventVisualItemVM(_mapCamera, mapEvent);
			_eventToVisualMap.Add(mapEvent, mapEventVisualItemVM);
			MapEvents.Add(mapEventVisualItemVM);
			mapEventVisualItemVM.UpdateProperties();
		}
	}

	public void OnMapEventEnded(MapEvent mapEvent)
	{
		if (_eventToVisualMap.ContainsKey(mapEvent))
		{
			MapEventVisualItemVM item = _eventToVisualMap[mapEvent];
			MapEvents.Remove(item);
			_eventToVisualMap.Remove(mapEvent);
		}
	}

	private bool IsMapEventSettlementRelated(MapEvent mapEvent)
	{
		return mapEvent.MapEventSettlement != null;
	}
}
