using System;
using System.Collections.Generic;
using TaleWorlds.Core;
using TaleWorlds.Engine;
using TaleWorlds.Engine.GauntletUI;
using TaleWorlds.Library;
using TaleWorlds.MountAndBlade.View;
using TaleWorlds.MountAndBlade.View.MissionViews;
using TaleWorlds.MountAndBlade.View.MissionViews.Singleplayer;
using TaleWorlds.MountAndBlade.ViewModelCollection.HUD.FormationMarker;
using TaleWorlds.ScreenSystem;

namespace TaleWorlds.MountAndBlade.GauntletUI.Mission.Singleplayer;

[OverrideView(typeof(MissionFormationMarkerUIHandler))]
public class MissionGauntletFormationMarker : MissionBattleUIBaseView
{
	private MissionFormationMarkerVM _dataSource;

	private GauntletLayer _gauntletLayer;

	private MissionFormationTargetSelectionHandler _formationTargetHandler;

	private MBReadOnlyList<Formation> _focusedFormationsCache;

	private readonly Vec3 _heightOffset = new Vec3(0f, 0f, 3f);

	private float _fadeOutTimer;

	private bool _showDistanceTexts;

	protected override void OnCreateView()
	{
		_dataSource = new MissionFormationMarkerVM(base.Mission);
		_gauntletLayer = new GauntletLayer("MissionFormationMarker", ViewOrderPriority++);
		_gauntletLayer.LoadMovie("FormationMarker", _dataSource);
		base.MissionScreen.AddLayer(_gauntletLayer);
		_formationTargetHandler = base.Mission.GetMissionBehavior<MissionFormationTargetSelectionHandler>();
		if (_formationTargetHandler != null)
		{
			_formationTargetHandler.OnFormationFocused += OnFormationFocusedFromHandler;
		}
		ManagedOptions.OnManagedOptionChanged = (ManagedOptions.OnManagedOptionChangedDelegate)Delegate.Combine(ManagedOptions.OnManagedOptionChanged, new ManagedOptions.OnManagedOptionChangedDelegate(OnManagedOptionChanged));
		UpdateShowDistanceTexts();
	}

	protected override void OnDestroyView()
	{
		ManagedOptions.OnManagedOptionChanged = (ManagedOptions.OnManagedOptionChangedDelegate)Delegate.Remove(ManagedOptions.OnManagedOptionChanged, new ManagedOptions.OnManagedOptionChangedDelegate(OnManagedOptionChanged));
		if (_formationTargetHandler != null)
		{
			_formationTargetHandler.OnFormationFocused -= OnFormationFocusedFromHandler;
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
			_dataSource.IsFormationTargetRelevant = _formationTargetHandler != null && base.Mission.IsOrderMenuOpen;
			_dataSource.ShowDistanceTexts = _showDistanceTexts;
			if (_dataSource.IsEnabled)
			{
				_dataSource.RefreshFormationMarkers();
				RefreshTargetProperties();
				UpdateMarkerPositions();
				_fadeOutTimer = 2f;
			}
			else if (_fadeOutTimer >= 0f)
			{
				_fadeOutTimer -= dt;
				UpdateMarkerPositions();
			}
		}
	}

	private void UpdateMarkerPositions()
	{
		for (int i = 0; i < _dataSource.Targets.Count; i++)
		{
			MissionFormationMarkerTargetVM missionFormationMarkerTargetVM = _dataSource.Targets[i];
			float screenX = 0f;
			float screenY = 0f;
			float w = 0f;
			WorldPosition cachedMedianPosition = missionFormationMarkerTargetVM.Formation.CachedMedianPosition;
			if (cachedMedianPosition.IsValid)
			{
				MBWindowManager.WorldToScreen(base.MissionScreen.CombatCamera, cachedMedianPosition.GetGroundVec3() + _heightOffset, ref screenX, ref screenY, ref w);
				if (!TaleWorlds.Library.MathF.IsValidValue(w) || !TaleWorlds.Library.MathF.IsValidValue(screenX) || !TaleWorlds.Library.MathF.IsValidValue(screenY))
				{
					screenX = -10000f;
					screenY = -10000f;
					w = -1f;
				}
				missionFormationMarkerTargetVM.WSign = ((!(w < 0f)) ? 1 : (-1));
				missionFormationMarkerTargetVM.Distance = base.MissionScreen.CombatCamera.Position.Distance(cachedMedianPosition.GetGroundVec3());
				missionFormationMarkerTargetVM.ScreenPosition = new Vec2(screenX, screenY);
				if (_dataSource.ShowDistanceTexts)
				{
					Agent main = Agent.Main;
					missionFormationMarkerTargetVM.DistanceText = ((main != null && main.IsActive()) ? ((int)Agent.Main.Position.Distance(cachedMedianPosition.GetGroundVec3())).ToString() : ((int)missionFormationMarkerTargetVM.Distance).ToString());
				}
				else
				{
					missionFormationMarkerTargetVM.DistanceText = string.Empty;
				}
			}
			else
			{
				missionFormationMarkerTargetVM.WSign = -1;
				missionFormationMarkerTargetVM.Distance = 10000f;
				missionFormationMarkerTargetVM.DistanceText = string.Empty;
				missionFormationMarkerTargetVM.ScreenPosition = new Vec2(-10000f, -10000f);
			}
		}
	}

	private void RefreshTargetProperties()
	{
		if (!_dataSource.IsFormationTargetRelevant)
		{
			for (int i = 0; i < _dataSource.Targets.Count; i++)
			{
				_dataSource.Targets[i].SetTargetedState(isFocused: false, isTargetingAFormation: false);
			}
			return;
		}
		List<Formation> list = new List<Formation>();
		MBReadOnlyList<Formation> mBReadOnlyList = Agent.Main?.Team.PlayerOrderController?.SelectedFormations;
		if (mBReadOnlyList != null)
		{
			for (int j = 0; j < mBReadOnlyList.Count; j++)
			{
				if (mBReadOnlyList[j].TargetFormation != null)
				{
					MovementOrder readonlyMovementOrderReference = mBReadOnlyList[j].GetReadonlyMovementOrderReference();
					if (readonlyMovementOrderReference.OrderType == OrderType.Charge || readonlyMovementOrderReference.OrderType == OrderType.Advance)
					{
						list.Add(mBReadOnlyList[j].TargetFormation);
					}
				}
			}
		}
		for (int k = 0; k < _dataSource.Targets.Count; k++)
		{
			MissionFormationMarkerTargetVM missionFormationMarkerTargetVM = _dataSource.Targets[k];
			if (missionFormationMarkerTargetVM.TeamType == 2)
			{
				bool isTargetingAFormation = list.Contains(missionFormationMarkerTargetVM.Formation);
				missionFormationMarkerTargetVM.SetTargetedState(_focusedFormationsCache?.Contains(missionFormationMarkerTargetVM.Formation) ?? false, isTargetingAFormation);
			}
		}
	}

	private void OnFormationFocusedFromHandler(MBReadOnlyList<Formation> focusedFormations)
	{
		_focusedFormationsCache = focusedFormations;
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
