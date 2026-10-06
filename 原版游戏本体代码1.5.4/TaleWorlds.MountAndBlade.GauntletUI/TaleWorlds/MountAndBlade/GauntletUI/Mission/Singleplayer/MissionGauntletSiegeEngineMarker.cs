using System.Collections.Generic;
using TaleWorlds.Core;
using TaleWorlds.Engine.GauntletUI;
using TaleWorlds.MountAndBlade.View;
using TaleWorlds.MountAndBlade.View.MissionViews;
using TaleWorlds.MountAndBlade.View.MissionViews.Singleplayer;
using TaleWorlds.MountAndBlade.ViewModelCollection.HUD.FormationMarker;
using TaleWorlds.ScreenSystem;

namespace TaleWorlds.MountAndBlade.GauntletUI.Mission.Singleplayer;

[OverrideView(typeof(MissionSiegeEngineMarkerView))]
public class MissionGauntletSiegeEngineMarker : MissionBattleUIBaseView
{
	private List<SiegeWeapon> _siegeEngines;

	private MissionSiegeEngineMarkerVM _dataSource;

	private GauntletLayer _gauntletLayer;

	private MissionGauntletSingleplayerOrderUIHandler _orderHandler;

	protected override void OnCreateView()
	{
		_dataSource = new MissionSiegeEngineMarkerVM(base.Mission, base.MissionScreen.CombatCamera);
		_gauntletLayer = new GauntletLayer("MissionSiegeEngineMarker", ViewOrderPriority);
		_gauntletLayer.LoadMovie("SiegeEngineMarker", _dataSource);
		base.MissionScreen.AddLayer(_gauntletLayer);
		_orderHandler = base.Mission.GetMissionBehavior<MissionGauntletSingleplayerOrderUIHandler>();
	}

	public override void OnDeploymentFinished()
	{
		base.OnDeploymentFinished();
		_siegeEngines = new List<SiegeWeapon>();
		foreach (MissionObject activeMissionObject in base.Mission.ActiveMissionObjects)
		{
			if (activeMissionObject is SiegeWeapon { DestructionComponent: not null, Side: not BattleSideEnum.None } siegeWeapon)
			{
				_siegeEngines.Add(siegeWeapon);
			}
		}
	}

	protected override void OnDestroyView()
	{
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

	public override void OnMissionScreenTick(float dt)
	{
		base.OnMissionScreenTick(dt);
		if (base.IsViewCreated)
		{
			if (!_dataSource.IsInitialized && _siegeEngines != null)
			{
				_dataSource.InitializeWith(_siegeEngines);
			}
			if (!_orderHandler.IsDeployment)
			{
				_dataSource.IsEnabled = base.Input.IsGameKeyDown(5);
			}
			_dataSource.Tick(dt);
		}
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
