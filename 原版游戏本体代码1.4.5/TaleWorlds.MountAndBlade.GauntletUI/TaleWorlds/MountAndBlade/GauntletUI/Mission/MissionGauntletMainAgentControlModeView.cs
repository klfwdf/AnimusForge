using System.Linq;
using TaleWorlds.Core;
using TaleWorlds.Engine.GauntletUI;
using TaleWorlds.InputSystem;
using TaleWorlds.Library;
using TaleWorlds.Localization;
using TaleWorlds.MountAndBlade.View;
using TaleWorlds.MountAndBlade.View.MissionViews;
using TaleWorlds.MountAndBlade.ViewModelCollection;
using TaleWorlds.MountAndBlade.ViewModelCollection.HUD.WalkMode;
using TaleWorlds.MountAndBlade.ViewModelCollection.Input;

namespace TaleWorlds.MountAndBlade.GauntletUI.Mission;

[OverrideView(typeof(MissionMainAgentControlModeView))]
public class MissionGauntletMainAgentControlModeView : MissionView
{
	private const int _missionTimeSpeedRequestID = 813;

	private readonly IMissionScreen _missionScreenAsInterface;

	private GauntletLayer _gauntletLayer;

	private MissionMainAgentWalkModeControllerVM _dataSource;

	private MissionMainAgentController _mainAgentController;

	private bool _isSlowDownApplied;

	private bool _holdHandled;

	private bool _prevKeyDown;

	private float _toggleHoldTime;

	private float _playerDismountTimer;

	private float _slowDownAmountWhileRadialIsOpen => 0.25f;

	private float _minOpenHoldTime => 0.22f;

	private bool IsDisplayingADialog
	{
		get
		{
			IMissionScreen missionScreenAsInterface = _missionScreenAsInterface;
			if ((missionScreenAsInterface == null || !missionScreenAsInterface.GetDisplayDialog()) && !base.MissionScreen.IsRadialMenuActive)
			{
				return base.Mission.IsOrderMenuOpen;
			}
			return true;
		}
	}

	private bool HoldHandled
	{
		get
		{
			return _holdHandled;
		}
		set
		{
			_holdHandled = value;
		}
	}

	public MissionGauntletMainAgentControlModeView()
	{
		_missionScreenAsInterface = base.MissionScreen;
		HoldHandled = false;
	}

	public override void EarlyStart()
	{
		base.EarlyStart();
		_gauntletLayer = new GauntletLayer("MissionAgentControlMode", 3);
		_dataSource = new MissionMainAgentWalkModeControllerVM();
		_gauntletLayer.Input.RegisterHotKeyCategory(HotKeyManager.GetCategory("CombatHotKeyCategory"));
		_gauntletLayer.InputRestrictions.SetInputRestrictions(isMouseVisible: false, InputUsageMask.Invalid);
		_gauntletLayer.LoadMovie("MainAgentControlMode", _dataSource);
		base.MissionScreen.AddLayer(_gauntletLayer);
		_mainAgentController = base.Mission.GetMissionBehavior<MissionMainAgentController>();
		base.Mission.OnMainAgentChanged += OnMainAgentChanged;
	}

	public override void AfterStart()
	{
		base.AfterStart();
		InitializeWalkModes();
	}

	public override void OnMissionScreenFinalize()
	{
		base.OnMissionScreenFinalize();
		base.Mission.OnMainAgentChanged -= OnMainAgentChanged;
		base.MissionScreen.RemoveLayer(_gauntletLayer);
		_gauntletLayer = null;
		_dataSource.OnFinalize();
		_dataSource = null;
	}

	public override void OnMissionScreenTick(float dt)
	{
		base.OnMissionScreenTick(dt);
		Agent mainAgent = base.Mission.MainAgent;
		if (mainAgent == null || mainAgent.HasMount)
		{
			_playerDismountTimer = 0f;
		}
		else if (_playerDismountTimer < 2f)
		{
			_playerDismountTimer += dt;
		}
		if (IsMainAgentAvailable() && (!base.MissionScreen.IsRadialMenuActive || _dataSource.IsEnabled))
		{
			TickControls(dt);
		}
		else if (_dataSource.IsEnabled)
		{
			HandleClosingHold();
		}
	}

	private void InitializeWalkModes()
	{
		GameKeyContext category = HotKeyManager.GetCategory("CombatHotKeyCategory");
		_dataSource.AddWalkMode("walk", new TextObject("{=zmS2FpJH}Toggle Walk"), () => base.Mission.MainAgent != null && base.Mission.MainAgent.WalkMode, delegate(bool value)
		{
			if (base.Mission.MainAgent != null)
			{
				if (value)
				{
					_mainAgentController.AddOverrideControlsForFrame(MissionMainAgentController.OverrideMainAgentControlFlag.Walk);
				}
				else
				{
					_mainAgentController.AddOverrideControlsForFrame(MissionMainAgentController.OverrideMainAgentControlFlag.Run);
				}
			}
		}, () => true, category.GetHotKey("ControllerToggleWalk"), isHotkeyConsoleOnly: true);
		_dataSource.AddWalkMode("crouch", new TextObject("{=0pd93SuK}Toggle Crouch"), () => base.Mission.MainAgent != null && base.Mission.MainAgent.CrouchMode, delegate(bool value)
		{
			if (base.Mission.MainAgent != null)
			{
				if (value)
				{
					_mainAgentController.AddOverrideControlsForFrame(MissionMainAgentController.OverrideMainAgentControlFlag.Crouch);
				}
				else
				{
					_mainAgentController.AddOverrideControlsForFrame(MissionMainAgentController.OverrideMainAgentControlFlag.Stand);
				}
			}
		}, () => base.Mission.MainAgent == null || base.Mission.MainAgent.IsCrouchingAllowed(), category.GetHotKey("ControllerToggleCrouch"), isHotkeyConsoleOnly: true);
		_dataSource.LastUsedItem = _dataSource.ControlModes.FirstOrDefault((WalkModeItemVM w) => w.TypeId == "crouch");
	}

	private void OnMainAgentChanged(Agent oldAgent)
	{
		if (base.Mission.MainAgent == null)
		{
			if (HoldHandled)
			{
				HoldHandled = false;
			}
			_toggleHoldTime = 0f;
			_dataSource.SetEnabled(isEnabled: false);
		}
	}

	public override void OnAgentRemoved(Agent affectedAgent, Agent affectorAgent, AgentState agentState, KillingBlow blow)
	{
		if (affectedAgent == Agent.Main)
		{
			HandleClosingHold();
		}
	}

	private void TickControls(float dt)
	{
		if (base.MissionScreen.SceneLayer.Input.IsHotKeyDown("ControlModeToggle") && !base.MissionScreen.IsPhotoModeEnabled && !IsDisplayingADialog && base.Mission.Mode != MissionMode.Deployment && base.Mission.Mode != MissionMode.CutScene && !base.MissionScreen.IsRadialMenuActive)
		{
			if (_toggleHoldTime > _minOpenHoldTime && !HoldHandled)
			{
				HandleOpeningHold();
				HoldHandled = true;
			}
			_toggleHoldTime += dt;
			_prevKeyDown = true;
		}
		else if (_prevKeyDown && !base.MissionScreen.SceneLayer.Input.IsHotKeyDown("ControlModeToggle"))
		{
			if (_toggleHoldTime < _minOpenHoldTime)
			{
				HandleQuickRelease();
			}
			else
			{
				HandleClosingHold();
			}
			HoldHandled = false;
			_toggleHoldTime = 0f;
			_prevKeyDown = false;
		}
		if (!_dataSource.IsEnabled)
		{
			return;
		}
		for (int i = 0; i < _dataSource.ControlModes.Count; i++)
		{
			WalkModeItemVM walkModeItemVM = _dataSource.ControlModes[i];
			InputKeyItemVM toggleInputKey = walkModeItemVM.ToggleInputKey;
			if (((toggleInputKey.HotKey != null && base.Input.IsHotKeyReleased(toggleInputKey.HotKey.Id)) || (toggleInputKey.GameKey != null && base.Input.IsGameKeyReleased(toggleInputKey.GameKey.Id))) && !walkModeItemVM.IsDisabled)
			{
				walkModeItemVM.ToggleState();
				HandleClosingHold();
				break;
			}
		}
	}

	private void HandleOpeningHold()
	{
		_dataSource?.SetEnabled(isEnabled: true);
		base.MissionScreen.RegisterRadialMenuObject(this);
		if (!GameNetwork.IsMultiplayer && !_isSlowDownApplied)
		{
			base.Mission.AddTimeSpeedRequest(new TaleWorlds.MountAndBlade.Mission.TimeSpeedRequest(_slowDownAmountWhileRadialIsOpen, 813));
			_isSlowDownApplied = true;
		}
	}

	private void HandleClosingHold()
	{
		_dataSource?.SetEnabled(isEnabled: false);
		base.MissionScreen.UnregisterRadialMenuObject(this);
		if (!GameNetwork.IsMultiplayer && _isSlowDownApplied)
		{
			base.Mission.RemoveTimeSpeedRequest(813);
			_isSlowDownApplied = false;
		}
	}

	private void HandleQuickRelease()
	{
		_dataSource?.LastUsedItem?.ToggleState();
		_dataSource?.SetEnabled(isEnabled: false);
		base.MissionScreen.UnregisterRadialMenuObject(this);
	}

	private bool IsMainAgentAvailable()
	{
		Agent main = Agent.Main;
		if (main != null && main.IsActive() && Agent.Main.MountAgent == null && _playerDismountTimer >= 2f && !Agent.Main.IsUsingGameObject)
		{
			return !Agent.Main.IsInWater();
		}
		return false;
	}

	public override void OnPhotoModeActivated()
	{
		base.OnPhotoModeActivated();
		if (_gauntletLayer != null)
		{
			_gauntletLayer.UIContext.ContextAlpha = 0f;
		}
	}

	public override void OnPhotoModeDeactivated()
	{
		base.OnPhotoModeDeactivated();
		if (_gauntletLayer != null)
		{
			_gauntletLayer.UIContext.ContextAlpha = 1f;
		}
	}
}
