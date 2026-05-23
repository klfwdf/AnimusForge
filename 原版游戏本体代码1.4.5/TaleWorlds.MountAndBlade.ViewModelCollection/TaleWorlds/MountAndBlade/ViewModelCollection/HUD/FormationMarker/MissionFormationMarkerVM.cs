using System.Collections.Generic;
using System.Linq;
using TaleWorlds.Library;
using TaleWorlds.LinQuick;

namespace TaleWorlds.MountAndBlade.ViewModelCollection.HUD.FormationMarker;

public class MissionFormationMarkerVM : ViewModel
{
	public class FormationMarkerDistanceComparer : IComparer<MissionFormationMarkerTargetVM>
	{
		public int Compare(MissionFormationMarkerTargetVM x, MissionFormationMarkerTargetVM y)
		{
			return y.Distance.CompareTo(x.Distance);
		}
	}

	private readonly Mission _mission;

	private readonly FormationMarkerDistanceComparer _comparer;

	private bool _isEnabled;

	private bool _isFormationTargetRelevant;

	private bool _showDistanceTexts;

	private MBBindingList<MissionFormationMarkerTargetVM> _targets;

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
				for (int i = 0; i < Targets.Count; i++)
				{
					Targets[i].IsEnabled = value;
				}
			}
		}
	}

	[DataSourceProperty]
	public bool IsFormationTargetRelevant
	{
		get
		{
			return _isFormationTargetRelevant;
		}
		set
		{
			if (value != _isFormationTargetRelevant)
			{
				_isFormationTargetRelevant = value;
				OnPropertyChangedWithValue(value, "IsFormationTargetRelevant");
				for (int i = 0; i < Targets.Count; i++)
				{
					Targets[i].IsFormationTargetRelevant = value;
				}
			}
		}
	}

	[DataSourceProperty]
	public bool ShowDistanceTexts
	{
		get
		{
			return _showDistanceTexts;
		}
		set
		{
			if (_showDistanceTexts != value)
			{
				_showDistanceTexts = value;
				OnPropertyChangedWithValue(value, "ShowDistanceTexts");
				for (int i = 0; i < Targets.Count; i++)
				{
					Targets[i].ShowDistanceTexts = value;
				}
			}
		}
	}

	[DataSourceProperty]
	public MBBindingList<MissionFormationMarkerTargetVM> Targets
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

	public MissionFormationMarkerVM(Mission mission)
	{
		_mission = mission;
		_comparer = new FormationMarkerDistanceComparer();
		Targets = new MBBindingList<MissionFormationMarkerTargetVM>();
	}

	public void RefreshFormationMarkers()
	{
		IEnumerable<Formation> formationList = _mission.Teams.SelectMany((Team t) => t.FormationsIncludingEmpty.WhereQ((Formation f) => f.CountOfUnits > 0));
		foreach (Formation formation in formationList)
		{
			if (Targets.All((MissionFormationMarkerTargetVM t) => t.Formation != formation))
			{
				MissionFormationMarkerTargetVM missionFormationMarkerTargetVM = new MissionFormationMarkerTargetVM(formation);
				Targets.Add(missionFormationMarkerTargetVM);
				missionFormationMarkerTargetVM.IsEnabled = IsEnabled;
				missionFormationMarkerTargetVM.IsFormationTargetRelevant = IsFormationTargetRelevant;
				missionFormationMarkerTargetVM.ShowDistanceTexts = ShowDistanceTexts;
			}
		}
		if (formationList.CountQ() < Targets.Count)
		{
			foreach (MissionFormationMarkerTargetVM item in Targets.WhereQ((MissionFormationMarkerTargetVM t) => !formationList.Contains(t.Formation)).ToList())
			{
				Targets.Remove(item);
			}
		}
		Targets.Sort(_comparer);
		foreach (MissionFormationMarkerTargetVM target in Targets)
		{
			target.Refresh();
		}
	}
}
