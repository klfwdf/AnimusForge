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

	private readonly bool _isMultiplayer;

	private const float MultiplayerFarAlphaTarget = 0.35f;

	private const float MultiplayerFarDistanceCutoff = 350f;

	private const float MultiplayerCloseDistanceCutoff = 5f;

	private const float MultiplayerClosestFadeoutRange = 1f;

	private const float MultiplayerAlwaysOnDistance = 25f;

	private const float SingleplayerFarAlphaTarget = 0.7f;

	private const float SingleplayerFarDistanceCutoff = 500f;

	private const float SingleplayerCloseDistanceCutoff = 10f;

	private const float SingleplayerClosestFadeoutRange = 5f;

	private const float SingleplayerAlwaysOnDistance = 25f;

	private const float NoOverride = -1f;

	private float _overrideFarAlphaTarget = -1f;

	private float _overrideFarDistanceCutoff = -1f;

	private float _overrideAlwaysOnDistance = -1f;

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
		: this(mission, isMultiplayer: false)
	{
	}

	public MissionFormationMarkerVM(Mission mission, bool isMultiplayer)
	{
		_mission = mission;
		_isMultiplayer = isMultiplayer;
		_comparer = new FormationMarkerDistanceComparer();
		Targets = new MBBindingList<MissionFormationMarkerTargetVM>();
	}

	public void SetMarkerDistanceConfig(float farDistanceCutoff, float farAlphaTarget, float alwaysOnDistance)
	{
		_overrideFarDistanceCutoff = farDistanceCutoff;
		_overrideFarAlphaTarget = farAlphaTarget;
		_overrideAlwaysOnDistance = alwaysOnDistance;
		GetMarkerDistanceConfig(out var farAlphaTarget2, out var farDistanceCutoff2, out var closeDistanceCutoff, out var closestFadeoutRange, out var alwaysOnDistance2);
		foreach (MissionFormationMarkerTargetVM target in Targets)
		{
			ApplyMarkerDistanceConfig(target, farAlphaTarget2, farDistanceCutoff2, closeDistanceCutoff, closestFadeoutRange, alwaysOnDistance2);
		}
	}

	private void GetMarkerDistanceConfig(out float farAlphaTarget, out float farDistanceCutoff, out float closeDistanceCutoff, out float closestFadeoutRange, out float alwaysOnDistance)
	{
		closeDistanceCutoff = (_isMultiplayer ? 5f : 10f);
		closestFadeoutRange = (_isMultiplayer ? 1f : 5f);
		float num = (_isMultiplayer ? 0.35f : 0.7f);
		float num2 = (_isMultiplayer ? 350f : 500f);
		float num3 = (_isMultiplayer ? 25f : 25f);
		farAlphaTarget = ((_overrideFarAlphaTarget >= 0f) ? _overrideFarAlphaTarget : num);
		farDistanceCutoff = ((_overrideFarDistanceCutoff >= 0f) ? _overrideFarDistanceCutoff : num2);
		alwaysOnDistance = ((_overrideAlwaysOnDistance >= 0f) ? _overrideAlwaysOnDistance : num3);
		if (farDistanceCutoff <= closeDistanceCutoff)
		{
			farDistanceCutoff = num2;
		}
		if (farAlphaTarget <= 0f && _overrideFarDistanceCutoff < 0f)
		{
			farAlphaTarget = num;
		}
		if (alwaysOnDistance >= farDistanceCutoff)
		{
			alwaysOnDistance = num3;
		}
	}

	private static void ApplyMarkerDistanceConfig(MissionFormationMarkerTargetVM target, float farAlphaTarget, float farDistanceCutoff, float closeDistanceCutoff, float closestFadeoutRange, float alwaysOnDistance)
	{
		target.FarAlphaTarget = farAlphaTarget;
		target.FarDistanceCutoff = farDistanceCutoff;
		target.CloseDistanceCutoff = closeDistanceCutoff;
		target.ClosestFadeoutRange = closestFadeoutRange;
		target.AlwaysOnDistance = alwaysOnDistance;
	}

	public void RefreshFormationMarkers()
	{
		IEnumerable<Formation> formationList = _mission.Teams.SelectMany((Team t) => t.FormationsIncludingEmpty.WhereQ((Formation f) => f.CountOfUnits > 0));
		GetMarkerDistanceConfig(out var farAlphaTarget, out var farDistanceCutoff, out var closeDistanceCutoff, out var closestFadeoutRange, out var alwaysOnDistance);
		foreach (Formation formation in formationList)
		{
			if (Targets.All((MissionFormationMarkerTargetVM t) => t.Formation != formation))
			{
				MissionFormationMarkerTargetVM missionFormationMarkerTargetVM = new MissionFormationMarkerTargetVM(formation);
				Targets.Add(missionFormationMarkerTargetVM);
				missionFormationMarkerTargetVM.IsEnabled = IsEnabled;
				missionFormationMarkerTargetVM.IsFormationTargetRelevant = IsFormationTargetRelevant;
				missionFormationMarkerTargetVM.ShowDistanceTexts = ShowDistanceTexts;
				ApplyMarkerDistanceConfig(missionFormationMarkerTargetVM, farAlphaTarget, farDistanceCutoff, closeDistanceCutoff, closestFadeoutRange, alwaysOnDistance);
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
