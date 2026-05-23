using TaleWorlds.CampaignSystem.ViewModelCollection.Map.Tracker;
using TaleWorlds.Library;

namespace SandBox.ViewModelCollection.Map.Tracker;

public class MapTrackerCollectionVM : ViewModel
{
	private readonly MapTrackerProvider _mapTrackerProvider;

	private MBBindingList<MapTrackerItemVM> _trackers;

	public MBBindingList<MapTrackerItemVM> Trackers
	{
		get
		{
			return _trackers;
		}
		set
		{
			if (value != _trackers)
			{
				_trackers = value;
				OnPropertyChangedWithValue(value, "Trackers");
			}
		}
	}

	public MapTrackerCollectionVM()
	{
		_mapTrackerProvider = new MapTrackerProvider();
		Trackers = new MBBindingList<MapTrackerItemVM>();
		MapTrackerItemVM[] trackers = _mapTrackerProvider.GetTrackers();
		foreach (MapTrackerItemVM item in trackers)
		{
			Trackers.Add(item);
		}
		_mapTrackerProvider.OnTrackerAddedOrRemoved += OnTrackerAddedOrRemoved;
	}

	private void OnTrackerAddedOrRemoved(MapTrackerItemVM item, bool added)
	{
		if (added)
		{
			Trackers.Add(item);
		}
		else
		{
			Trackers.Remove(item);
		}
	}

	public void Tick(float dt)
	{
		for (int i = 0; i < Trackers.Count; i++)
		{
			Trackers[i].RefreshBinding();
		}
	}

	public override void OnFinalize()
	{
		base.OnFinalize();
		_mapTrackerProvider.OnTrackerAddedOrRemoved -= OnTrackerAddedOrRemoved;
		Trackers.ApplyActionOnAllItems(delegate(MapTrackerItemVM t)
		{
			t.OnFinalize();
		});
	}

	public void UpdateProperties()
	{
		Trackers.ApplyActionOnAllItems(delegate(MapTrackerItemVM t)
		{
			t.UpdateProperties();
		});
	}
}
