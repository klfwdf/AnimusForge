using System.Collections.Generic;
using TaleWorlds.Engine;
using TaleWorlds.Library;
using TaleWorlds.MountAndBlade.Missions.MissionLogics;
using TaleWorlds.MountAndBlade.Missions.Objectives;

namespace TaleWorlds.MountAndBlade.ViewModelCollection.Missions.Objective;

public class MissionObjectiveMarkersVM : ViewModel
{
	private class MarkerDistanceComparer : IComparer<MissionObjectiveMarkerVM>
	{
		public int Compare(MissionObjectiveMarkerVM x, MissionObjectiveMarkerVM y)
		{
			return y.Distance.CompareTo(x.Distance);
		}
	}

	private readonly Camera _missionCamera;

	private readonly MarkerDistanceComparer _distanceComparer;

	private readonly MissionObjectiveLogic _objectiveLogic;

	private MissionObjective _latestObjective;

	private MBBindingList<MissionObjectiveMarkerVM> _targets;

	private bool _isEnabled;

	[DataSourceProperty]
	public MBBindingList<MissionObjectiveMarkerVM> Targets
	{
		get
		{
			return _targets;
		}
		set
		{
			if (value != _targets)
			{
				_targets = value;
				OnPropertyChangedWithValue(value, "Targets");
			}
		}
	}

	[DataSourceProperty]
	public bool IsEnabled
	{
		get
		{
			return _isEnabled;
		}
		set
		{
			if (value != _isEnabled)
			{
				_isEnabled = value;
				OnPropertyChangedWithValue(value, "IsEnabled");
				Targets.ApplyActionOnAllItems(delegate(MissionObjectiveMarkerVM t)
				{
					t.IsEnabled = value;
				});
			}
		}
	}

	public MissionObjectiveMarkersVM(MissionObjectiveLogic objectiveLogic, Camera missionCamera)
	{
		Targets = new MBBindingList<MissionObjectiveMarkerVM>();
		_distanceComparer = new MarkerDistanceComparer();
		_objectiveLogic = objectiveLogic;
		_missionCamera = missionCamera;
		IsEnabled = true;
	}

	public override void RefreshValues()
	{
		base.RefreshValues();
		if (IsEnabled)
		{
			Targets.ApplyActionOnAllItems(delegate(MissionObjectiveMarkerVM x)
			{
				x.RefreshValues();
			});
		}
	}

	public override void OnFinalize()
	{
		base.OnFinalize();
		Targets.ApplyActionOnAllItems(delegate(MissionObjectiveMarkerVM x)
		{
			x.OnFinalize();
		});
	}

	public void UpdateObjective(MissionObjective objective)
	{
		if (_latestObjective == objective)
		{
			return;
		}
		_latestObjective = objective;
		Targets.Clear();
		if (_latestObjective != null)
		{
			MBReadOnlyList<MissionObjectiveTarget> targetsCopy = _latestObjective.GetTargetsCopy();
			if (targetsCopy != null)
			{
				for (int i = 0; i < targetsCopy.Count; i++)
				{
					MissionObjectiveMarkerVM item = new MissionObjectiveMarkerVM(targetsCopy[i]);
					Targets.Add(item);
				}
			}
		}
		RefreshValues();
	}

	public void Tick(float dt)
	{
		UpdateTargets();
	}

	private void UpdateTargets()
	{
		for (int i = 0; i < Targets.Count; i++)
		{
			MissionObjectiveMarkerVM missionObjectiveMarkerVM = Targets[i];
			missionObjectiveMarkerVM.UpdateActiveState();
			if (missionObjectiveMarkerVM.IsActive)
			{
				missionObjectiveMarkerVM.UpdatePosition(_missionCamera);
			}
		}
		Targets.Sort(_distanceComparer);
	}
}
