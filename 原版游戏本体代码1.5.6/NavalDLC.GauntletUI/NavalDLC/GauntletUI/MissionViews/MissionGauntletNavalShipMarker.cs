using System;
using System.Collections.Generic;
using NavalDLC.Missions;
using NavalDLC.Missions.MissionLogics;
using NavalDLC.Missions.Objects;
using NavalDLC.View.MissionViews;
using NavalDLC.ViewModelCollection.HUD.ShipMarker;
using TaleWorlds.Core;
using TaleWorlds.Engine.GauntletUI;
using TaleWorlds.Library;
using TaleWorlds.MountAndBlade;
using TaleWorlds.MountAndBlade.View;
using TaleWorlds.MountAndBlade.View.MissionViews;
using TaleWorlds.ScreenSystem;

namespace NavalDLC.GauntletUI.MissionViews;

[OverrideView(typeof(NavalMissionShipMarkerUIHandler))]
public class MissionGauntletNavalShipMarker : MissionBattleUIBaseView
{
	private NavalShipMarkersVM _dataSource;

	private GauntletLayer _gauntletLayer;

	private NavalShipTargetSelectionHandler _shipTargetHandler;

	private NavalShipsLogic _navalShipsLogic;

	private MissionShip _focusedShip;

	private readonly Vec3 _heightOffset = new Vec3(0f, 0f, 3f);

	private float _fadeOutTimer;

	private bool _showDistanceTexts;

	protected override void OnCreateView()
	{
		_dataSource = new NavalShipMarkersVM(base.Mission);
		_gauntletLayer = new GauntletLayer("NavalShipMarker", ViewOrderPriority);
		_gauntletLayer.LoadMovie("NavalShipMarker", _dataSource);
		base.MissionScreen.AddLayer(_gauntletLayer);
		_shipTargetHandler = base.Mission.GetMissionBehavior<NavalShipTargetSelectionHandler>();
		_navalShipsLogic = Mission.Current.GetMissionBehavior<NavalShipsLogic>();
		if (_shipTargetHandler != null)
		{
			_shipTargetHandler.OnShipsFocused += OnShipFocusedFromHandler;
		}
		ManagedOptions.OnManagedOptionChanged = (ManagedOptions.OnManagedOptionChangedDelegate)Delegate.Combine(ManagedOptions.OnManagedOptionChanged, new ManagedOptions.OnManagedOptionChangedDelegate(OnManagedOptionChanged));
		UpdateShowDistanceTexts();
	}

	protected override void OnDestroyView()
	{
		ManagedOptions.OnManagedOptionChanged = (ManagedOptions.OnManagedOptionChangedDelegate)Delegate.Remove(ManagedOptions.OnManagedOptionChanged, new ManagedOptions.OnManagedOptionChangedDelegate(OnManagedOptionChanged));
		if (_shipTargetHandler != null)
		{
			_shipTargetHandler.OnShipsFocused -= OnShipFocusedFromHandler;
		}
		base.MissionScreen.RemoveLayer(_gauntletLayer);
		_gauntletLayer = null;
		_dataSource.OnFinalize();
		_dataSource = null;
	}

	protected override void OnSuspendView()
	{
		if (_gauntletLayer != null)
		{
			ScreenManager.SetSuspendLayer(_gauntletLayer, isSuspended: true);
		}
	}

	protected override void OnResumeView()
	{
		if (_gauntletLayer != null)
		{
			ScreenManager.SetSuspendLayer(_gauntletLayer, isSuspended: false);
		}
	}

	private void OnManagedOptionChanged(ManagedOptions.ManagedOptionsType optionType)
	{
		if (optionType == ManagedOptions.ManagedOptionsType.ShowFormationDistances)
		{
			UpdateShowDistanceTexts();
		}
	}

	private void UpdateShowDistanceTexts()
	{
		_showDistanceTexts = ManagedOptions.GetConfig(ManagedOptions.ManagedOptionsType.ShowFormationDistances) > 1E-05f;
	}

	public override void OnMissionScreenTick(float dt)
	{
		base.OnMissionScreenTick(dt);
		if (base.IsViewCreated)
		{
			if (base.Mission.Mode != MissionMode.Deployment)
			{
				_dataSource.IsEnabled = base.Input.IsGameKeyDown(5) || base.Mission.IsOrderMenuOpen;
			}
			_dataSource.IsShipTargetingRelevant = _shipTargetHandler != null && base.Mission.IsOrderMenuOpen;
			_dataSource.ShowDistanceTexts = _showDistanceTexts;
			if (_dataSource.IsEnabled)
			{
				_dataSource.RefreshShipMarkers();
				RefreshShipTargetProperties();
				UpdateMarkerPositions();
				_fadeOutTimer = 2f;
			}
			else if (_fadeOutTimer >= 0f)
			{
				_dataSource.RefreshShipMarkers();
				_fadeOutTimer -= dt;
				UpdateMarkerPositions();
			}
		}
	}

	private void UpdateMarkerPositions()
	{
		for (int i = 0; i < _dataSource.ShipMarkers.Count; i++)
		{
			NavalShipMarkerItemVM navalShipMarkerItemVM = _dataSource.ShipMarkers[i];
			float screenX = 0f;
			float screenY = 0f;
			float w = 0f;
			Vec3 vec = ((!navalShipMarkerItemVM.IsShipActive()) ? navalShipMarkerItemVM.Formation.CachedMedianPosition.GetNavMeshVec3() : navalShipMarkerItemVM.Ship.GlobalFrame.origin);
			if (vec.IsValid)
			{
				MBWindowManager.WorldToScreen(base.MissionScreen.CombatCamera, vec + _heightOffset, ref screenX, ref screenY, ref w);
				if (!TaleWorlds.Library.MathF.IsValidValue(w) || !TaleWorlds.Library.MathF.IsValidValue(screenX) || !TaleWorlds.Library.MathF.IsValidValue(screenY))
				{
					screenX = -10000f;
					screenY = -10000f;
					w = -1f;
				}
				navalShipMarkerItemVM.WSign = ((!(w < 0f)) ? 1 : (-1));
				navalShipMarkerItemVM.Distance = base.MissionScreen.CombatCamera.Position.Distance(vec);
				navalShipMarkerItemVM.ScreenPosition = new Vec2(screenX, screenY);
				if (_dataSource.ShowDistanceTexts)
				{
					Agent main = Agent.Main;
					navalShipMarkerItemVM.DistanceText = ((main != null && main.IsActive()) ? ((int)Agent.Main.Position.Distance(vec)).ToString() : ((int)navalShipMarkerItemVM.Distance).ToString());
				}
				else
				{
					navalShipMarkerItemVM.DistanceText = string.Empty;
				}
			}
			else
			{
				navalShipMarkerItemVM.WSign = -1;
				navalShipMarkerItemVM.Distance = 10000f;
				navalShipMarkerItemVM.DistanceText = string.Empty;
				navalShipMarkerItemVM.ScreenPosition = new Vec2(-10000f, -10000f);
			}
		}
	}

	private void RefreshShipTargetProperties()
	{
		if (!_dataSource.IsShipTargetingRelevant)
		{
			for (int i = 0; i < _dataSource.ShipMarkers.Count; i++)
			{
				_dataSource.ShipMarkers[i].SetTargetedState(isFocused: false, isTargetingAShip: false);
			}
			return;
		}
		List<MissionShip> list = new List<MissionShip>();
		List<Formation> list2 = new List<Formation>();
		MBReadOnlyList<Formation> mBReadOnlyList = Agent.Main?.Team.PlayerOrderController?.SelectedFormations;
		if (mBReadOnlyList != null)
		{
			for (int j = 0; j < mBReadOnlyList.Count; j++)
			{
				MissionShip missionShip = _navalShipsLogic.GetShipAssignment(mBReadOnlyList[j].Team.TeamSide, mBReadOnlyList[j].FormationIndex)?.MissionShip;
				if (missionShip == null)
				{
					continue;
				}
				if (mBReadOnlyList[j].TargetFormation != null)
				{
					MovementOrder readonlyMovementOrderReference = mBReadOnlyList[j].GetReadonlyMovementOrderReference();
					if (readonlyMovementOrderReference.OrderType == OrderType.Charge || readonlyMovementOrderReference.OrderType == OrderType.Advance)
					{
						list2.Add(mBReadOnlyList[j].TargetFormation);
					}
				}
				if (missionShip.ShipOrder.MovementOrderEnum == ShipOrder.ShipMovementOrderEnum.Engage && missionShip.ShipOrder.TargetShip != null && !missionShip.ShipOrder.IsAutoSelectingTargetShip)
				{
					list.Add(missionShip.ShipOrder.TargetShip);
				}
			}
		}
		for (int k = 0; k < _dataSource.ShipMarkers.Count; k++)
		{
			NavalShipMarkerItemVM navalShipMarkerItemVM = _dataSource.ShipMarkers[k];
			if (navalShipMarkerItemVM.TeamType == 2)
			{
				bool isTargetingAShip = list.Contains(navalShipMarkerItemVM.Ship) || list2.Contains(navalShipMarkerItemVM.Formation);
				navalShipMarkerItemVM.SetTargetedState(_focusedShip == navalShipMarkerItemVM.Ship, isTargetingAShip);
			}
		}
	}

	private void OnShipFocusedFromHandler(MissionShip focusedShip)
	{
		_focusedShip = focusedShip;
	}

	public override void OnPhotoModeActivated()
	{
		base.OnPhotoModeActivated();
		if (base.IsViewCreated)
		{
			_gauntletLayer.UIContext.ContextAlpha = 0f;
		}
	}

	public override void OnPhotoModeDeactivated()
	{
		base.OnPhotoModeDeactivated();
		if (base.IsViewCreated)
		{
			_gauntletLayer.UIContext.ContextAlpha = 1f;
		}
	}
}
