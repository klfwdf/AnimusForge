using System.Collections.Generic;
using System.Linq;
using NavalDLC.Missions.MissionLogics;
using NavalDLC.Missions.Objects;
using TaleWorlds.Library;
using TaleWorlds.MountAndBlade;

namespace NavalDLC.ViewModelCollection.HUD.ShipMarker;

public class NavalShipMarkersVM : ViewModel
{
	public class ShipMarkerDistanceComparer : IComparer<NavalShipMarkerItemVM>
	{
		public int Compare(NavalShipMarkerItemVM x, NavalShipMarkerItemVM y)
		{
			return y.Distance.CompareTo(x.Distance);
		}
	}

	private readonly Mission _mission;

	private NavalShipsLogic _navalShipsLogic;

	private readonly ShipMarkerDistanceComparer _comparer;

	private bool _isEnabled;

	private bool _isShipTargetingRelevant;

	private bool _showDistanceTexts;

	private MBBindingList<NavalShipMarkerItemVM> _shipMarkers;

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
				for (int i = 0; i < ShipMarkers.Count; i++)
				{
					ShipMarkers[i].IsEnabled = value;
				}
			}
		}
	}

	[DataSourceProperty]
	public bool IsShipTargetingRelevant
	{
		get
		{
			return _isShipTargetingRelevant;
		}
		set
		{
			if (value != _isShipTargetingRelevant)
			{
				_isShipTargetingRelevant = value;
				OnPropertyChangedWithValue(value, "IsShipTargetingRelevant");
				for (int i = 0; i < ShipMarkers.Count; i++)
				{
					ShipMarkers[i].IsShipTargetRelevant = value;
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
			if (value != _showDistanceTexts)
			{
				_showDistanceTexts = value;
				OnPropertyChangedWithValue(value, "ShowDistanceTexts");
				for (int i = 0; i < ShipMarkers.Count; i++)
				{
					ShipMarkers[i].ShowDistanceTexts = value;
				}
			}
		}
	}

	[DataSourceProperty]
	public MBBindingList<NavalShipMarkerItemVM> ShipMarkers
	{
		get
		{
			return _shipMarkers;
		}
		set
		{
			if (value != _shipMarkers)
			{
				_shipMarkers = value;
				OnPropertyChangedWithValue(value, "ShipMarkers");
			}
		}
	}

	public NavalShipMarkersVM(Mission mission)
	{
		_mission = mission;
		_comparer = new ShipMarkerDistanceComparer();
		ShipMarkers = new MBBindingList<NavalShipMarkerItemVM>();
	}

	public void RefreshShipMarkers()
	{
		if (_navalShipsLogic == null)
		{
			_navalShipsLogic = _mission.GetMissionBehavior<NavalShipsLogic>();
		}
		if (_navalShipsLogic == null)
		{
			ShipMarkers.Clear();
			return;
		}
		List<Formation> allFormations = _mission.Teams.SelectMany((Team x) => x.FormationsIncludingSpecialAndEmpty).ToList();
		GetShipChanges(allFormations, ShipMarkers, out var markersToRemove, out var markersToAdd);
		for (int num = 0; num < markersToRemove.Count; num++)
		{
			NavalShipMarkerItemVM item = markersToRemove[num];
			ShipMarkers.Remove(item);
		}
		for (int num2 = 0; num2 < markersToAdd.Count; num2++)
		{
			NavalShipMarkerItemVM navalShipMarkerItemVM = markersToAdd[num2];
			ShipMarkers.Add(navalShipMarkerItemVM);
			navalShipMarkerItemVM.IsEnabled = IsEnabled;
			navalShipMarkerItemVM.IsShipTargetRelevant = IsShipTargetingRelevant;
			navalShipMarkerItemVM.ShowDistanceTexts = ShowDistanceTexts;
		}
		ShipMarkers.Sort(_comparer);
		for (int num3 = 0; num3 < ShipMarkers.Count; num3++)
		{
			NavalShipMarkerItemVM navalShipMarkerItemVM2 = ShipMarkers[num3];
			navalShipMarkerItemVM2.Refresh();
			navalShipMarkerItemVM2.IsEnabled = IsEnabled && (navalShipMarkerItemVM2.Ship == null || navalShipMarkerItemVM2.Ship != _navalShipsLogic.PlayerControlledShip);
		}
	}

	private void GetShipChanges(List<Formation> allFormations, MBBindingList<NavalShipMarkerItemVM> activeMarkers, out MBList<NavalShipMarkerItemVM> markersToRemove, out MBList<NavalShipMarkerItemVM> markersToAdd)
	{
		markersToAdd = new MBList<NavalShipMarkerItemVM>();
		markersToRemove = new MBList<NavalShipMarkerItemVM>();
		List<(Formation, MissionShip)> list = new List<(Formation, MissionShip)>();
		for (int i = 0; i < allFormations.Count; i++)
		{
			Formation formation = allFormations[i];
			_navalShipsLogic.GetShip(formation, out var ship);
			if ((ship != null || formation.CountOfUnits > 0) && (ship == null || (!ship.IsDisabled && !ship.IsRemoved)))
			{
				list.Add((formation, ship));
			}
		}
		for (int j = 0; j < activeMarkers.Count; j++)
		{
			NavalShipMarkerItemVM navalShipMarkerItemVM = activeMarkers[j];
			bool flag = false;
			for (int k = 0; k < list.Count; k++)
			{
				Formation item = list[k].Item1;
				MissionShip item2 = list[k].Item2;
				if (item == navalShipMarkerItemVM.Formation && item2 == navalShipMarkerItemVM.Ship)
				{
					flag = true;
					break;
				}
			}
			if (!flag)
			{
				markersToRemove.Add(navalShipMarkerItemVM);
			}
		}
		for (int l = 0; l < list.Count; l++)
		{
			Formation item3 = list[l].Item1;
			MissionShip item4 = list[l].Item2;
			bool flag2 = false;
			for (int m = 0; m < activeMarkers.Count; m++)
			{
				NavalShipMarkerItemVM navalShipMarkerItemVM2 = activeMarkers[m];
				if (navalShipMarkerItemVM2.Formation == item3 && navalShipMarkerItemVM2.Ship == item4)
				{
					flag2 = true;
					break;
				}
			}
			if (!flag2)
			{
				markersToAdd.Add(new NavalShipMarkerItemVM(item3, item4));
			}
		}
	}

	public void UpdateCrewCounts()
	{
		for (int i = 0; i < ShipMarkers.Count; i++)
		{
			NavalShipMarkerItemVM navalShipMarkerItemVM = ShipMarkers[i];
			navalShipMarkerItemVM.CrewCount = navalShipMarkerItemVM.Formation.CountOfUnits;
		}
	}
}
