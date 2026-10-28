using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.ViewModelCollection.Map.Tracker;
using TaleWorlds.Library;

namespace SandBox.ViewModelCollection.Map.Tracker;

public class MapTrackerCollectionVM : ViewModel
{
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
		Trackers = new MBBindingList<MapTrackerItemVM>();
	}

	public override void OnFinalize()
	{
		base.OnFinalize();
		Trackers.Clear();
	}

	public bool HasTrackerFor(ITrackableCampaignObject trackable)
	{
		return GetTrackerFor(trackable) != null;
	}

	public MapTrackerItemVM GetTrackerFor(ITrackableCampaignObject trackable)
	{
		for (int i = 0; i < Trackers.Count; i++)
		{
			if (Trackers[i].TrackedObject == trackable)
			{
				return Trackers[i];
			}
		}
		return null;
	}

	public void AddTracker(MapTrackerItemVM tracker)
	{
		if (HasTrackerFor(tracker.TrackedObject))
		{
			Debug.FailedAssert("Trying to add a tracker that was already added", "C:\\BuildAgent\\work\\mb3\\Source\\Bannerlord\\SandBox.ViewModelCollection\\Map\\Tracker\\MapTrackerCollectionVM.cs", "AddTracker", 43);
		}
		else
		{
			Trackers.Add(tracker);
		}
	}

	public void RemoveTrackerIfExists(ITrackableCampaignObject trackable)
	{
		MapTrackerItemVM trackerFor = GetTrackerFor(trackable);
		if (trackerFor != null)
		{
			Trackers.Remove(trackerFor);
		}
	}

	public void Update()
	{
		for (int i = 0; i < Trackers.Count; i++)
		{
			Trackers[i].RefreshBinding();
		}
	}

	public void UpdateProperties()
	{
		Trackers.ApplyActionOnAllItems(delegate(MapTrackerItemVM t)
		{
			t.UpdateProperties();
		});
	}
}
