using System;
using TaleWorlds.Core;
using TaleWorlds.Engine.GauntletUI;
using TaleWorlds.InputSystem;
using TaleWorlds.Library;
using TaleWorlds.MountAndBlade.GauntletUI;
using TaleWorlds.MountAndBlade.GauntletUI.Mission.Singleplayer;
using TaleWorlds.MountAndBlade.Multiplayer.View.MissionViews;
using TaleWorlds.MountAndBlade.View;
using TaleWorlds.MountAndBlade.View.MissionViews;
using TaleWorlds.MountAndBlade.View.MissionViews.Order;
using TaleWorlds.MountAndBlade.ViewModelCollection.Order;

namespace TaleWorlds.MountAndBlade.Multiplayer.GauntletUI.Mission;

[OverrideView(typeof(MultiplayerMissionOrderUIHandler))]
public class MissionGauntletMultiplayerOrderUIHandler : GauntletOrderUIHandler
{
	private IRoundComponent _roundComponent;

	private bool _isValid;

	private bool _shouldTick;

	private bool _shouldInitializeFormationInfo;

	private const int NotAppliedYet = int.MinValue;

	private int _appliedVisibilityModeInt = int.MinValue;

	private int _appliedVisibilityThreshold = int.MinValue;

	private int _appliedVisibilityAppliesAtCloseRangeInt = int.MinValue;

	private int _appliedMarkerFarDistanceCutoff = int.MinValue;

	private int _appliedMarkerFarAlphaTarget = int.MinValue;

	private int _appliedMarkerAlwaysOnDistance = int.MinValue;

	public override bool IsDeployment => false;

	public override bool IsSiegeDeployment => false;

	public override bool IsValidForTick
	{
		get
		{
			if (_shouldTick && (!base.MissionScreen.IsRadialMenuActive || _dataSource.IsToggleOrderShown))
			{
				return !GameStateManager.Current.ActiveStateDisabledByUser;
			}
			return false;
		}
	}

	public MissionGauntletMultiplayerOrderUIHandler()
	{
		ViewOrderPriority = 19;
	}

	public override bool IsReady()
	{
		return true;
	}

	public override void AfterStart()
	{
		base.AfterStart();
		MultiplayerOptions.Instance.GetOptionFromOptionType(MultiplayerOptions.OptionType.NumberOfBotsPerFormation).GetValue(out int value);
		_shouldTick = value > 0;
		RefreshFormationTargetingVisibilityConfig();
	}

	private void RefreshFormationTargetingVisibilityConfig()
	{
		RefreshFormationTargetingVisibilityGate();
		RefreshFormationMarkerDistanceConfig();
	}

	private void RefreshFormationTargetingVisibilityGate()
	{
		MultiplayerOptions.Instance.GetOptionFromOptionType(MultiplayerOptions.OptionType.FormationTargetingVisibilityMode).GetValue(out int value);
		MultiplayerOptions.Instance.GetOptionFromOptionType(MultiplayerOptions.OptionType.FormationTargetingVisibilityThreshold).GetValue(out int value2);
		MultiplayerOptions.Instance.GetOptionFromOptionType(MultiplayerOptions.OptionType.FormationTargetingVisibilityAppliesAtCloseRange).GetValue(out int value3);
		if (_appliedVisibilityModeInt != value || _appliedVisibilityThreshold != value2 || _appliedVisibilityAppliesAtCloseRangeInt != value3)
		{
			MissionFormationTargetSelectionHandler missionBehavior = base.Mission.GetMissionBehavior<MissionFormationTargetSelectionHandler>();
			if (missionBehavior != null)
			{
				missionBehavior.SetVisibilityConfig(new MissionFormationTargetSelectionHandler.VisibilityConfig
				{
					Mode = (MultiplayerOptions.FormationTargetingVisibilityModes)value,
					Threshold = value2,
					AppliesAtCloseRange = (value3 != 0)
				});
				_appliedVisibilityModeInt = value;
				_appliedVisibilityThreshold = value2;
				_appliedVisibilityAppliesAtCloseRangeInt = value3;
			}
		}
	}

	private void RefreshFormationMarkerDistanceConfig()
	{
		MultiplayerOptions.Instance.GetOptionFromOptionType(MultiplayerOptions.OptionType.FormationMarkerFarDistanceCutoff).GetValue(out int value);
		MultiplayerOptions.Instance.GetOptionFromOptionType(MultiplayerOptions.OptionType.FormationMarkerFarAlphaTarget).GetValue(out int value2);
		MultiplayerOptions.Instance.GetOptionFromOptionType(MultiplayerOptions.OptionType.FormationMarkerAlwaysOnDistance).GetValue(out int value3);
		if (_appliedMarkerFarDistanceCutoff != value || _appliedMarkerFarAlphaTarget != value2 || _appliedMarkerAlwaysOnDistance != value3)
		{
			MissionGauntletFormationMarker missionBehavior = base.Mission.GetMissionBehavior<MissionGauntletFormationMarker>();
			if (missionBehavior != null && missionBehavior.IsViewCreated)
			{
				float farDistanceCutoff = ((value >= 0) ? ((float)value) : (-1f));
				float farAlphaTarget = ((value2 >= 0) ? ((float)value2 / 100f) : (-1f));
				float alwaysOnDistance = ((value3 >= 0) ? ((float)value3) : (-1f));
				missionBehavior.SetMarkerDistanceConfig(farDistanceCutoff, farAlphaTarget, alwaysOnDistance);
				_appliedMarkerFarDistanceCutoff = value;
				_appliedMarkerFarAlphaTarget = value2;
				_appliedMarkerAlwaysOnDistance = value3;
			}
		}
	}

	public override void OnMissionScreenTick(float dt)
	{
		RefreshFormationTargetingVisibilityConfig();
		if (IsValidForTick)
		{
			if (!_isInitialized)
			{
				Team team = (GameNetwork.IsMyPeerReady ? GameNetwork.MyPeer.GetComponent<MissionPeer>().Team : null);
				if (team != null && (team == base.Mission.AttackerTeam || team == base.Mission.DefenderTeam))
				{
					InitializeInADisgustingManner();
				}
			}
			if (!_isValid)
			{
				Team team2 = (GameNetwork.IsMyPeerReady ? GameNetwork.MyPeer.GetComponent<MissionPeer>().Team : null);
				if (team2 != null && (team2 == base.Mission.AttackerTeam || team2 == base.Mission.DefenderTeam))
				{
					ValidateInADisgustingManner();
				}
				return;
			}
			if (_shouldInitializeFormationInfo)
			{
				Team team3 = (GameNetwork.IsMyPeerReady ? GameNetwork.MyPeer.GetComponent<MissionPeer>().Team : null);
				if (_dataSource != null && team3 != null)
				{
					_dataSource.AfterInitialize();
					_shouldInitializeFormationInfo = false;
				}
			}
		}
		base.OnMissionScreenTick(dt);
	}

	public override void OnMissionScreenInitialize()
	{
		base.OnMissionScreenInitialize();
		base.MissionScreen.SceneLayer.Input.RegisterHotKeyCategory(HotKeyManager.GetCategory("MissionOrderHotkeyCategory"));
		_siegeDeploymentHandler = null;
		ManagedOptions.OnManagedOptionChanged = (ManagedOptions.OnManagedOptionChangedDelegate)Delegate.Combine(ManagedOptions.OnManagedOptionChanged, new ManagedOptions.OnManagedOptionChangedDelegate(OnManagedOptionChanged));
		_roundComponent = base.Mission.GetMissionBehavior<MissionMultiplayerGameModeBaseClient>()?.RoundComponent;
		if (_roundComponent != null)
		{
			_roundComponent.OnRoundStarted += OnRoundStarted;
			_roundComponent.OnPreparationEnded += OnPreparationEnded;
		}
	}

	private void OnRoundStarted()
	{
		_dataSource?.AfterInitialize();
	}

	private void OnPreparationEnded()
	{
		_shouldInitializeFormationInfo = true;
	}

	private void OnManagedOptionChanged(ManagedOptions.ManagedOptionsType changedManagedOptionsType)
	{
		switch (changedManagedOptionsType)
		{
		case ManagedOptions.ManagedOptionsType.OrderType:
			if (_gauntletLayer != null && _movie != null)
			{
				_gauntletLayer.ReleaseMovie(_movie);
				string movieName = ((BannerlordConfig.OrderType == 0) ? _barOrderMovieName : _radialOrderMovieName);
				_movie = _gauntletLayer.LoadMovie(movieName, _dataSource);
			}
			break;
		case ManagedOptions.ManagedOptionsType.OrderLayoutType:
			_dataSource?.OnOrderLayoutTypeChanged();
			break;
		}
	}

	public override void OnMissionScreenFinalize()
	{
		Clear();
		_orderTroopPlacer = null;
		MissionPeer.OnTeamChanged -= TeamChange;
		ManagedOptions.OnManagedOptionChanged = (ManagedOptions.OnManagedOptionChangedDelegate)Delegate.Remove(ManagedOptions.OnManagedOptionChanged, new ManagedOptions.OnManagedOptionChangedDelegate(OnManagedOptionChanged));
		if (_formationTargetHandler != null)
		{
			_formationTargetHandler.OnFormationFocused -= OnFormationFocused;
			_formationTargetHandler = null;
		}
		if (_roundComponent != null)
		{
			_roundComponent.OnRoundStarted -= OnRoundStarted;
			_roundComponent.OnPreparationEnded -= OnPreparationEnded;
		}
		base.OnMissionScreenFinalize();
	}

	protected override void OnTransferFinished()
	{
	}

	protected override void SetLayerEnabled(bool isEnabled)
	{
		if (isEnabled)
		{
			_orderTroopPlacer.SuspendTroopPlacer = false;
			base.MissionScreen.SetOrderFlagVisibility(value: true);
			Game.Current.EventManager.TriggerEvent(new MissionPlayerToggledOrderViewEvent(newIsEnabledState: true));
		}
		else
		{
			_orderTroopPlacer.SuspendTroopPlacer = true;
			base.MissionScreen.SetOrderFlagVisibility(value: false);
			base.MissionScreen.UnregisterRadialMenuObject(this);
			Game.Current.EventManager.TriggerEvent(new MissionPlayerToggledOrderViewEvent(newIsEnabledState: false));
		}
	}

	public void InitializeInADisgustingManner()
	{
		if (_isInitialized)
		{
			Debug.Print("InitializeInADisgustingManner called while already initialized!");
			Debug.FailedAssert("InitializeInADisgustingManner called while already initialized!", "C:\\BuildAgent\\work\\mb3\\Source\\Bannerlord\\TaleWorlds.MountAndBlade.Multiplayer.GauntletUI\\Mission\\MissionGauntletMultiplayerOrderUIHandler.cs", "InitializeInADisgustingManner", 274);
		}
		Debug.Print($"InitializeInADisgustingManner is called. IsValidForTick: {IsValidForTick}");
		base.AfterStart();
		_orderTroopPlacer = base.Mission.GetMissionBehavior<OrderTroopPlacer>();
		if (_orderTroopPlacer?.OrderFlag == null)
		{
			Debug.FailedAssert("Order troop placer's order flag is null", "C:\\BuildAgent\\work\\mb3\\Source\\Bannerlord\\TaleWorlds.MountAndBlade.Multiplayer.GauntletUI\\Mission\\MissionGauntletMultiplayerOrderUIHandler.cs", "InitializeInADisgustingManner", 283);
		}
		base.MissionScreen.OrderFlag = _orderTroopPlacer.OrderFlag;
		Debug.Print("MissionScreen.OrderFlag has been set (MP)");
		base.MissionScreen.SetOrderFlagVisibility(value: false);
		_formationTargetHandler = base.Mission.GetMissionBehavior<MissionFormationTargetSelectionHandler>();
		if (_formationTargetHandler != null)
		{
			_formationTargetHandler.OnFormationFocused += OnFormationFocused;
		}
		MissionPeer.OnTeamChanged += TeamChange;
		_isInitialized = true;
	}

	private void OnFormationFocused(Formation focusedFormation)
	{
		_focusedFormation = focusedFormation;
	}

	public void ValidateInADisgustingManner()
	{
		_dataSource = new MissionOrderVM(base.Mission.PlayerTeam.PlayerOrderController, isDeployment: false, isMultiplayer: true);
		_dataSource.SetCallbacks(new MissionOrderCallbacks
		{
			ToggleMissionInputs = base.ToggleScreenRotation,
			GetVisualOrderExecutionParameters = base.GetVisualOrderExecutionParameters,
			SetSuspendTroopPlacer = SetSuspendTroopPlacer,
			OnActivateToggleOrder = base.OnActivateToggleOrder,
			OnDeactivateToggleOrder = base.OnDeactivateToggleOrder,
			OnTransferTroopsFinished = OnTransferFinished,
			OnBeforeOrder = base.OnBeforeOrder
		});
		_dataSource.SetCancelInputKey(HotKeyManager.GetCategory("GenericPanelGameKeyCategory").GetHotKey("ToggleEscapeMenu"));
		_dataSource.TroopController.SetDoneInputKey(HotKeyManager.GetCategory("GenericPanelGameKeyCategory").GetHotKey("Confirm"));
		_dataSource.TroopController.SetCancelInputKey(HotKeyManager.GetCategory("GenericPanelGameKeyCategory").GetHotKey("Exit"));
		_dataSource.TroopController.SetResetInputKey(HotKeyManager.GetCategory("GenericPanelGameKeyCategory").GetHotKey("Reset"));
		GameKeyContext category = HotKeyManager.GetCategory("MissionOrderHotkeyCategory");
		_dataSource.SetOrderIndexKey(0, category.GetGameKey(69));
		_dataSource.SetOrderIndexKey(1, category.GetGameKey(70));
		_dataSource.SetOrderIndexKey(2, category.GetGameKey(71));
		_dataSource.SetOrderIndexKey(3, category.GetGameKey(72));
		_dataSource.SetOrderIndexKey(4, category.GetGameKey(73));
		_dataSource.SetOrderIndexKey(5, category.GetGameKey(74));
		_dataSource.SetOrderIndexKey(6, category.GetGameKey(75));
		_dataSource.SetOrderIndexKey(7, category.GetGameKey(76));
		_dataSource.SetOrderIndexKey(8, category.GetGameKey(77));
		_dataSource.SetReturnKey(category.GetGameKey(77));
		_gauntletLayer = new GauntletLayer("MultiplayerOrder", ViewOrderPriority);
		_spriteCategory = UIResourceManager.LoadSpriteCategory("ui_order");
		string movieName = ((BannerlordConfig.OrderType == 0) ? _barOrderMovieName : _radialOrderMovieName);
		_movie = _gauntletLayer.LoadMovie(movieName, _dataSource);
		_dataSource.InputRestrictions = _gauntletLayer.InputRestrictions;
		base.MissionScreen.AddLayer(_gauntletLayer);
		_dataSource.AfterInitialize();
		_isValid = true;
	}

	private void Clear()
	{
		if (_gauntletLayer != null)
		{
			base.MissionScreen.RemoveLayer(_gauntletLayer);
		}
		if (_dataSource != null)
		{
			_dataSource.OnFinalize();
		}
		_gauntletLayer = null;
		_dataSource = null;
		_movie = null;
		if (_isValid)
		{
			_spriteCategory.Unload();
		}
	}

	private void TeamChange(NetworkCommunicator peer, Team previousTeam, Team newTeam)
	{
		if (peer.IsMine)
		{
			Clear();
			_isValid = false;
		}
	}
}
