using System;
using NetworkMessages.FromClient;
using TaleWorlds.Core;
using TaleWorlds.Engine.GauntletUI;
using TaleWorlds.InputSystem;
using TaleWorlds.Library;
using TaleWorlds.MountAndBlade.View;
using TaleWorlds.MountAndBlade.View.MissionViews;
using TaleWorlds.MountAndBlade.ViewModelCollection;
using TaleWorlds.MountAndBlade.ViewModelCollection.HUD;
using TaleWorlds.ScreenSystem;

namespace TaleWorlds.MountAndBlade.GauntletUI.Mission;

[OverrideView(typeof(MissionMainAgentEquipDropView))]
public class MissionGauntletMainAgentEquipDropView : MissionView
{
	private const int _missionTimeSpeedRequestID = 624;

	private const float _slowDownAmountWhileRadialIsOpen = 0.25f;

	private bool _isSlowDownApplied;

	private GauntletLayer _gauntletLayer;

	private MissionMainAgentControllerEquipDropVM _dataSource;

	private MissionMainAgentController _missionMainAgentController;

	private EquipmentControllerLeaveLogic _missionControllerLeaveLogic;

	private const float _minOpenHoldTime = 0.3f;

	private const float _minDropHoldTime = 0.5f;

	private readonly IMissionScreen _missionScreenAsInterface;

	private bool _holdHandled;

	private float _toggleHoldTime;

	private float _weaponDropHoldTime;

	private bool _prevKeyDown;

	private bool _weaponDropHandled;

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

	public MissionGauntletMainAgentEquipDropView()
	{
		_missionScreenAsInterface = base.MissionScreen;
		HoldHandled = false;
	}

	public override void EarlyStart()
	{
		base.EarlyStart();
		_gauntletLayer = new GauntletLayer("MissionEquipDrop", ViewOrderPriority);
		_dataSource = new MissionMainAgentControllerEquipDropVM(OnToggleItem);
		_missionMainAgentController = base.Mission.GetMissionBehavior<MissionMainAgentController>();
		_missionControllerLeaveLogic = base.Mission.GetMissionBehavior<EquipmentControllerLeaveLogic>();
		_gauntletLayer.Input.RegisterHotKeyCategory(HotKeyManager.GetCategory("CombatHotKeyCategory"));
		_gauntletLayer.InputRestrictions.SetInputRestrictions(isMouseVisible: false, InputUsageMask.Invalid);
		_gauntletLayer.LoadMovie("MainAgentControllerEquipDrop", _dataSource);
		base.MissionScreen.AddLayer(_gauntletLayer);
		base.Mission.OnMainAgentChanged += OnMainAgentChanged;
		TaleWorlds.InputSystem.Input.OnGamepadActiveStateChanged = (Action)Delegate.Combine(TaleWorlds.InputSystem.Input.OnGamepadActiveStateChanged, new Action(OnGamepadActiveChanged));
	}

	public override void AfterStart()
	{
		base.AfterStart();
		_dataSource.InitializeMainAgentPropterties();
	}

	public override void OnMissionScreenFinalize()
	{
		base.OnMissionScreenFinalize();
		TaleWorlds.InputSystem.Input.OnGamepadActiveStateChanged = (Action)Delegate.Remove(TaleWorlds.InputSystem.Input.OnGamepadActiveStateChanged, new Action(OnGamepadActiveChanged));
		base.Mission.OnMainAgentChanged -= OnMainAgentChanged;
		base.MissionScreen.RemoveLayer(_gauntletLayer);
		_gauntletLayer = null;
		_dataSource.OnFinalize();
		_dataSource = null;
		_missionMainAgentController = null;
		_missionControllerLeaveLogic = null;
	}

	public override void OnMissionScreenTick(float dt)
	{
		base.OnMissionScreenTick(dt);
		if (_dataSource.IsActive && !IsMainAgentAvailable())
		{
			HandleClosingHold();
		}
		if (IsMainAgentAvailable() && (!base.MissionScreen.IsRadialMenuActive || _dataSource.IsActive))
		{
			TickControls(dt);
		}
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
			_dataSource.OnCancelHoldController();
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
		if ((base.MissionScreen.SceneLayer.Input.IsGameKeyDown(34) || _gauntletLayer.Input.IsGameKeyDown(34)) && !IsDisplayingADialog && !base.MissionScreen.IsPhotoModeEnabled && base.Mission.Mode != MissionMode.Deployment && base.Mission.Mode != MissionMode.CutScene && !base.MissionScreen.IsRadialMenuActive)
		{
			if (_toggleHoldTime > 0.3f && !HoldHandled)
			{
				HandleOpeningHold();
				HoldHandled = true;
			}
			_toggleHoldTime += dt;
			_prevKeyDown = true;
		}
		else if (_prevKeyDown && !base.MissionScreen.SceneLayer.Input.IsGameKeyDown(34) && !_gauntletLayer.Input.IsGameKeyDown(34))
		{
			if (_toggleHoldTime < 0.3f)
			{
				HandleQuickRelease();
			}
			else
			{
				HandleClosingHold();
			}
			HoldHandled = false;
			_toggleHoldTime = 0f;
			_weaponDropHoldTime = 0f;
			_prevKeyDown = false;
			_weaponDropHandled = false;
		}
		if (HoldHandled)
		{
			int keyWeaponIndex = GetKeyWeaponIndex(isReleased: false);
			int keyWeaponIndex2 = GetKeyWeaponIndex(isReleased: true);
			_dataSource.SetDropProgressForIndex(EquipmentIndex.None, _weaponDropHoldTime / 0.5f);
			if (keyWeaponIndex != -1)
			{
				if (!_weaponDropHandled)
				{
					int num = keyWeaponIndex;
					if (_weaponDropHoldTime > 0.5f && !Agent.Main.Equipment[num].IsEmpty)
					{
						OnDropEquipment((EquipmentIndex)num);
						_dataSource.OnWeaponDroppedAtIndex(keyWeaponIndex);
						_weaponDropHandled = true;
					}
					_dataSource.SetDropProgressForIndex((EquipmentIndex)num, _weaponDropHoldTime / 0.5f);
				}
				_weaponDropHoldTime += dt;
			}
			else if (keyWeaponIndex2 != -1)
			{
				if (!_weaponDropHandled)
				{
					int num2 = keyWeaponIndex2;
					if (!Agent.Main.Equipment[num2].IsEmpty && num2 != 4)
					{
						OnToggleItem((EquipmentIndex)num2);
						_dataSource.OnWeaponEquippedAtIndex(keyWeaponIndex2);
						_weaponDropHandled = true;
					}
				}
				_weaponDropHoldTime = 0f;
			}
			else
			{
				_weaponDropHoldTime = 0f;
				_weaponDropHandled = false;
			}
		}
		else
		{
			_weaponDropHoldTime = 0f;
			_weaponDropHandled = false;
		}
	}

	private void HandleOpeningHold()
	{
		_dataSource?.OnToggle(isEnabled: true);
		base.MissionScreen.RegisterRadialMenuObject(this);
		_missionControllerLeaveLogic?.SetIsEquipmentSelectionActive(isActive: true);
		if (!GameNetwork.IsMultiplayer && !_isSlowDownApplied)
		{
			base.Mission.AddTimeSpeedRequest(new TaleWorlds.MountAndBlade.Mission.TimeSpeedRequest(0.25f, 624));
			_isSlowDownApplied = true;
		}
		_gauntletLayer.IsFocusLayer = true;
		ScreenManager.TrySetFocus(_gauntletLayer);
	}

	private void HandleClosingHold()
	{
		_dataSource?.OnToggle(isEnabled: false);
		base.MissionScreen.UnregisterRadialMenuObject(this);
		_missionControllerLeaveLogic?.SetIsEquipmentSelectionActive(isActive: false);
		if (!GameNetwork.IsMultiplayer && _isSlowDownApplied)
		{
			base.Mission.RemoveTimeSpeedRequest(624);
			_isSlowDownApplied = false;
		}
		_gauntletLayer.IsFocusLayer = false;
		ScreenManager.TryLoseFocus(_gauntletLayer);
	}

	private void HandleQuickRelease()
	{
		_missionMainAgentController.OnWeaponUsageToggleRequested();
		_dataSource?.OnToggle(isEnabled: false);
		base.MissionScreen.UnregisterRadialMenuObject(this);
		_missionControllerLeaveLogic?.SetIsEquipmentSelectionActive(isActive: false);
	}

	private void OnToggleItem(EquipmentIndex indexToToggle)
	{
		bool flag = indexToToggle == Agent.Main.GetPrimaryWieldedItemIndex();
		bool flag2 = indexToToggle == Agent.Main.GetOffhandWieldedItemIndex();
		if (flag || flag2)
		{
			Agent.Main.TryToSheathWeaponInHand((!flag) ? Agent.HandIndex.OffHand : Agent.HandIndex.MainHand, Agent.WeaponWieldActionType.WithAnimation);
		}
		else
		{
			Agent.Main.TryToWieldWeaponInSlot(indexToToggle, Agent.WeaponWieldActionType.WithAnimation, isWieldedOnSpawn: false);
		}
	}

	private void OnDropEquipment(EquipmentIndex indexToDrop)
	{
		if (GameNetwork.IsClient)
		{
			GameNetwork.BeginModuleEventAsClient();
			GameNetwork.WriteMessage(new DropWeapon(base.Input.IsGameKeyDown(10), indexToDrop));
			GameNetwork.EndModuleEventAsClient();
		}
		else
		{
			Agent.Main.HandleDropWeapon(base.Input.IsGameKeyDown(10), indexToDrop);
		}
	}

	private bool IsMainAgentAvailable()
	{
		Agent main = Agent.Main;
		if (main != null && main.IsActive())
		{
			Agent main2 = Agent.Main;
			if (main2 == null || main2.Mission.IsNavalBattle)
			{
				return !Agent.Main.IsUsingGameObject;
			}
			return true;
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

	private void OnGamepadActiveChanged()
	{
		_dataSource.OnGamepadActiveChanged(TaleWorlds.InputSystem.Input.IsGamepadActive);
	}

	private int GetKeyWeaponIndex(bool isReleased)
	{
		Func<string, bool> func = ((!isReleased) ? new Func<string, bool>(_gauntletLayer.Input.IsHotKeyDown) : new Func<string, bool>(_gauntletLayer.Input.IsHotKeyReleased));
		string text = string.Empty;
		if (func("ControllerEquipDropWeapon1"))
		{
			text = "ControllerEquipDropWeapon1";
		}
		else if (func("ControllerEquipDropWeapon2"))
		{
			text = "ControllerEquipDropWeapon2";
		}
		else if (func("ControllerEquipDropWeapon3"))
		{
			text = "ControllerEquipDropWeapon3";
		}
		else if (func("ControllerEquipDropWeapon4"))
		{
			text = "ControllerEquipDropWeapon4";
		}
		else if (func("ControllerEquipDropExtraWeapon"))
		{
			text = "ControllerEquipDropExtraWeapon";
		}
		if (!string.IsNullOrEmpty(text))
		{
			for (int i = 0; i < _dataSource.EquippedWeapons.Count; i++)
			{
				if (_dataSource.EquippedWeapons[i].ShortcutKey?.HotKey.Id == text)
				{
					return (int)_dataSource.EquippedWeapons[i].Identifier;
				}
			}
			if (_dataSource.EquippedExtraWeapon?.ShortcutKey?.HotKey.Id == text)
			{
				return (int)_dataSource.EquippedExtraWeapon.Identifier;
			}
		}
		return -1;
	}
}
