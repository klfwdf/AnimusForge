using System.Collections.Generic;
using TaleWorlds.Core;
using TaleWorlds.Engine;
using TaleWorlds.Library;

namespace SandBox.ViewModelCollection.Missions.NameMarker;

public class MissionNameMarkerVM : ViewModel
{
	private class MarkerDistanceComparer : IComparer<MissionNameMarkerTargetBaseVM>
	{
		public int Compare(MissionNameMarkerTargetBaseVM x, MissionNameMarkerTargetBaseVM y)
		{
			return y.Distance.CompareTo(x.Distance);
		}
	}

	private readonly Camera _missionCamera;

	private bool _prevEnabledState;

	private bool _fadeOutTimerStarted;

	private float _fadeOutTimer;

	private readonly MarkerDistanceComparer _distanceComparer;

	private readonly List<MissionNameMarkerProvider> _providers;

	private MBBindingList<MissionNameMarkerTargetBaseVM> _targets;

	private bool _isEnabled;

	public bool IsTargetsAdded { get; private set; }

	[DataSourceProperty]
	public MBBindingList<MissionNameMarkerTargetBaseVM> Targets
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
				UpdateTargetStates(value);
				Game.Current.EventManager.TriggerEvent(new MissionNameMarkerToggleEvent(value));
			}
		}
	}

	public MissionNameMarkerVM(List<MissionNameMarkerProvider> providers, Camera missionCamera)
	{
		Targets = new MBBindingList<MissionNameMarkerTargetBaseVM>();
		_providers = providers;
		_distanceComparer = new MarkerDistanceComparer();
		_missionCamera = missionCamera;
	}

	public override void RefreshValues()
	{
		base.RefreshValues();
		Targets.ApplyActionOnAllItems(delegate(MissionNameMarkerTargetBaseVM x)
		{
			x.RefreshValues();
		});
	}

	public override void OnFinalize()
	{
		base.OnFinalize();
		Targets.ApplyActionOnAllItems(delegate(MissionNameMarkerTargetBaseVM x)
		{
			x.OnFinalize();
		});
	}

	public void Tick(float dt)
	{
		if (!IsTargetsAdded)
		{
			List<MissionNameMarkerTargetBaseVM> list = new List<MissionNameMarkerTargetBaseVM>();
			for (int i = 0; i < _providers.Count; i++)
			{
				_providers[i].CreateMarkers(list);
			}
			GetTargetDifferences(Targets, list, out var removedTargets, out var addedTargets);
			for (int j = 0; j < removedTargets.Count; j++)
			{
				Targets.Remove(removedTargets[j]);
			}
			for (int k = 0; k < addedTargets.Count; k++)
			{
				Targets.Add(addedTargets[k]);
			}
			IsTargetsAdded = true;
		}
		if (IsEnabled)
		{
			UpdateTargetScreenPositions(forceUpdate: false);
			_fadeOutTimerStarted = false;
			_fadeOutTimer = 0f;
			_prevEnabledState = IsEnabled;
		}
		else
		{
			if (_prevEnabledState)
			{
				_fadeOutTimerStarted = true;
			}
			if (_fadeOutTimerStarted)
			{
				_fadeOutTimer += dt;
			}
			if (_fadeOutTimer >= 2f)
			{
				_fadeOutTimerStarted = false;
			}
			UpdateTargetScreenPositions(_fadeOutTimer < 2f);
		}
		_prevEnabledState = IsEnabled;
	}

	private static void GetTargetDifferences(IList<MissionNameMarkerTargetBaseVM> currentTargets, IList<MissionNameMarkerTargetBaseVM> newTargets, out MBReadOnlyList<MissionNameMarkerTargetBaseVM> removedTargets, out MBReadOnlyList<MissionNameMarkerTargetBaseVM> addedTargets)
	{
		MBList<MissionNameMarkerTargetBaseVM> mBList = new MBList<MissionNameMarkerTargetBaseVM>();
		MBList<MissionNameMarkerTargetBaseVM> mBList2 = new MBList<MissionNameMarkerTargetBaseVM>();
		for (int i = 0; i < currentTargets.Count; i++)
		{
			MissionNameMarkerTargetBaseVM missionNameMarkerTargetBaseVM = currentTargets[i];
			bool flag = true;
			for (int j = 0; j < newTargets.Count; j++)
			{
				MissionNameMarkerTargetBaseVM other = newTargets[j];
				if (missionNameMarkerTargetBaseVM.Equals(other))
				{
					flag = false;
					break;
				}
			}
			if (flag)
			{
				mBList.Add(missionNameMarkerTargetBaseVM);
			}
		}
		for (int k = 0; k < newTargets.Count; k++)
		{
			MissionNameMarkerTargetBaseVM missionNameMarkerTargetBaseVM2 = newTargets[k];
			bool flag2 = true;
			for (int l = 0; l < currentTargets.Count; l++)
			{
				if (currentTargets[l].Equals(missionNameMarkerTargetBaseVM2))
				{
					flag2 = false;
					break;
				}
			}
			if (flag2)
			{
				mBList2.Add(missionNameMarkerTargetBaseVM2);
			}
		}
		removedTargets = mBList;
		addedTargets = mBList2;
	}

	public void SetTargetsDirty()
	{
		IsTargetsAdded = false;
	}

	private void UpdateTargetScreenPositions(bool forceUpdate)
	{
		for (int i = 0; i < Targets.Count; i++)
		{
			MissionNameMarkerTargetBaseVM missionNameMarkerTargetBaseVM = Targets[i];
			if (missionNameMarkerTargetBaseVM.IsEnabled || forceUpdate)
			{
				missionNameMarkerTargetBaseVM.UpdatePosition(_missionCamera);
			}
		}
		Targets.Sort(_distanceComparer);
	}

	private void UpdateTargetStates(bool state)
	{
		foreach (MissionNameMarkerTargetBaseVM target in Targets)
		{
			target.SetEnabledState(state);
		}
	}
}
