using System;
using NetworkMessages.FromClient;
using TaleWorlds.Core;
using TaleWorlds.Engine.GauntletUI;
using TaleWorlds.Library;
using TaleWorlds.MountAndBlade.View;
using TaleWorlds.MountAndBlade.View.MissionViews;
using TaleWorlds.MountAndBlade.ViewModelCollection;
using TaleWorlds.MountAndBlade.ViewModelCollection.HUD;

namespace TaleWorlds.MountAndBlade.GauntletUI.Mission;

[OverrideView(typeof(MissionMainAgentEquipmentControllerView))]
public class MissionGauntletMainAgentEquipmentControllerView : MissionView
{
	private const float _minHoldTime = 0.5f;

	private readonly IMissionScreen _missionScreenAsInterface;

	private bool _equipmentWasInFocusFirstFrameOfEquipDown;

	private bool _firstFrameOfEquipDownHandled;

	private bool _equipHoldHandled;

	private bool _isFocusedOnEquipment;

	private float _equipHoldTime;

	private bool _prevEquipKeyDown;

	private SpawnedItemEntity _focusedWeaponItem;

	private bool _dropHoldHandled;

	private float _dropHoldTime;

	private bool _prevDropKeyDown;

	private bool _isCurrentFocusedItemInteractable;

	private GauntletLayer _gauntletLayer;

	private MissionMainAgentEquipmentControllerVM _dataSource;

	private bool IsDisplayingADialog => _missionScreenAsInterface?.GetDisplayDialog() ?? false;

	private bool EquipHoldHandled
	{
		get
		{
			return _equipHoldHandled;
		}
		set
		{
			_equipHoldHandled = value;
			if (_equipHoldHandled)
			{
				base.MissionScreen?.RegisterRadialMenuObject(this);
			}
			else
			{
				base.MissionScreen?.UnregisterRadialMenuObject(this);
			}
		}
	}

	private bool DropHoldHandled
	{
		get
		{
			return _dropHoldHandled;
		}
		set
		{
			_dropHoldHandled = value;
			if (_dropHoldHandled)
			{
				base.MissionScreen?.RegisterRadialMenuObject(this);
			}
			else
			{
				base.MissionScreen?.UnregisterRadialMenuObject(this);
			}
		}
	}

	public event Action<bool> OnEquipmentDropInteractionViewToggled;

	public event Action<bool> OnEquipmentEquipInteractionViewToggled;

	public MissionGauntletMainAgentEquipmentControllerView()
	{
		_missionScreenAsInterface = base.MissionScreen;
		EquipHoldHandled = false;
		DropHoldHandled = false;
	}

	public override void OnMissionScreenInitialize()
	{
		base.OnMissionScreenInitialize();
		_gauntletLayer = new GauntletLayer("MissionEquipmentController", ViewOrderPriority);
		_dataSource = new MissionMainAgentEquipmentControllerVM(OnDropEquipment, OnEquipItem);
		_gauntletLayer.LoadMovie("MainAgentEquipmentController", _dataSource);
		_gauntletLayer.InputRestrictions.SetInputRestrictions(isMouseVisible: false, InputUsageMask.Invalid);
		base.MissionScreen.AddLayer(_gauntletLayer);
		base.Mission.OnMainAgentChanged += OnMainAgentChanged;
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
		if (IsMainAgentAvailable() && base.Mission.IsMainAgentItemInteractionEnabled)
		{
			DropWeaponTick(dt);
			EquipWeaponTick(dt);
		}
		else
		{
			_prevDropKeyDown = false;
			_prevEquipKeyDown = false;
		}
	}

	public override void OnFocusGained(Agent agent, IFocusable focusableObject, bool isInteractable)
	{
		base.OnFocusGained(agent, focusableObject, isInteractable);
		if (focusableObject is UsableMissionObject usableMissionObject && usableMissionObject is SpawnedItemEntity spawnedItemEntity)
		{
			_isCurrentFocusedItemInteractable = isInteractable;
			if (!spawnedItemEntity.WeaponCopy.IsEmpty)
			{
				_isFocusedOnEquipment = true;
				_focusedWeaponItem = spawnedItemEntity;
				_dataSource.SetCurrentFocusedWeaponEntity(_focusedWeaponItem);
			}
		}
	}

	public override void OnFocusLost(Agent agent, IFocusable focusableObject)
	{
		base.OnFocusLost(agent, focusableObject);
		_isCurrentFocusedItemInteractable = false;
		_isFocusedOnEquipment = false;
		_focusedWeaponItem = null;
		_dataSource?.SetCurrentFocusedWeaponEntity(_focusedWeaponItem);
		if (EquipHoldHandled)
		{
			EquipHoldHandled = false;
			_equipHoldTime = 0f;
			_dataSource?.OnCancelEquipController();
			this.OnEquipmentEquipInteractionViewToggled?.Invoke(obj: false);
			_equipmentWasInFocusFirstFrameOfEquipDown = false;
		}
	}

	private void OnMainAgentChanged(Agent oldAgent)
	{
		if (base.Mission.MainAgent == null)
		{
			if (EquipHoldHandled)
			{
				EquipHoldHandled = false;
				this.OnEquipmentEquipInteractionViewToggled?.Invoke(obj: false);
			}
			_equipHoldTime = 0f;
			_dataSource.OnCancelEquipController();
			if (DropHoldHandled)
			{
				this.OnEquipmentDropInteractionViewToggled?.Invoke(obj: false);
				DropHoldHandled = false;
			}
			_dropHoldTime = 0f;
			_dataSource.OnCancelDropController();
		}
	}

	private void EquipWeaponTick(float dt)
	{
		if (base.MissionScreen.SceneLayer.Input.IsGameKeyDown(13) && !_prevDropKeyDown && !IsDisplayingADialog && IsMainAgentAvailable() && !base.MissionScreen.Mission.IsOrderMenuOpen)
		{
			if (!_firstFrameOfEquipDownHandled)
			{
				_equipmentWasInFocusFirstFrameOfEquipDown = _isFocusedOnEquipment;
				_firstFrameOfEquipDownHandled = true;
			}
			if (_equipmentWasInFocusFirstFrameOfEquipDown)
			{
				_equipHoldTime += dt;
				if (_equipHoldTime > 0.5f && !EquipHoldHandled && _isFocusedOnEquipment && _isCurrentFocusedItemInteractable)
				{
					HandleOpeningHoldEquip();
					EquipHoldHandled = true;
				}
			}
			_prevEquipKeyDown = true;
		}
		else
		{
			if (!_prevEquipKeyDown || base.MissionScreen.SceneLayer.Input.IsGameKeyDown(13))
			{
				return;
			}
			if (_equipmentWasInFocusFirstFrameOfEquipDown)
			{
				if (_equipHoldTime < 0.5f)
				{
					if (_focusedWeaponItem != null)
					{
						Agent main = Agent.Main;
						if (main != null && main.CanQuickPickUp(_focusedWeaponItem))
						{
							HandleQuickReleaseEquip();
						}
					}
				}
				else
				{
					HandleClosingHoldEquip();
				}
			}
			if (EquipHoldHandled)
			{
				EquipHoldHandled = false;
			}
			_equipHoldTime = 0f;
			_firstFrameOfEquipDownHandled = false;
			_prevEquipKeyDown = false;
		}
	}

	private void DropWeaponTick(float dt)
	{
		if (base.MissionScreen.SceneLayer.Input.IsGameKeyDown(22) && !_prevEquipKeyDown && !IsDisplayingADialog && IsMainAgentAvailable() && IsMainAgentHasAtLeastOneItem() && !base.MissionScreen.Mission.IsOrderMenuOpen)
		{
			_dropHoldTime += dt;
			if (_dropHoldTime > 0.5f && !DropHoldHandled)
			{
				HandleOpeningHoldDrop();
				DropHoldHandled = true;
			}
			_prevDropKeyDown = true;
		}
		else if (_prevDropKeyDown && !base.MissionScreen.SceneLayer.Input.IsGameKeyDown(22))
		{
			if (_dropHoldTime < 0.5f)
			{
				HandleQuickReleaseDrop();
			}
			else
			{
				HandleClosingHoldDrop();
			}
			DropHoldHandled = false;
			_dropHoldTime = 0f;
			_prevDropKeyDown = false;
		}
	}

	private void HandleOpeningHoldEquip()
	{
		_dataSource?.OnEquipControllerToggle(isActive: true);
		this.OnEquipmentEquipInteractionViewToggled?.Invoke(obj: true);
	}

	private void HandleClosingHoldEquip()
	{
		_dataSource?.OnEquipControllerToggle(isActive: false);
		this.OnEquipmentEquipInteractionViewToggled?.Invoke(obj: false);
	}

	private void HandleQuickReleaseEquip()
	{
		OnEquipItem(_focusedWeaponItem, EquipmentIndex.None);
	}

	private void HandleOpeningHoldDrop()
	{
		_dataSource?.OnDropControllerToggle(isActive: true);
		this.OnEquipmentDropInteractionViewToggled?.Invoke(obj: true);
	}

	private void HandleClosingHoldDrop()
	{
		_dataSource?.OnDropControllerToggle(isActive: false);
		this.OnEquipmentDropInteractionViewToggled?.Invoke(obj: false);
	}

	private void HandleQuickReleaseDrop()
	{
		OnDropEquipment(EquipmentIndex.None);
	}

	private void OnEquipItem(SpawnedItemEntity itemToEquip, EquipmentIndex indexToEquipItTo)
	{
		if (itemToEquip.GameEntity.IsValid)
		{
			Agent.Main?.HandleStartUsingAction(itemToEquip, (int)indexToEquipItTo);
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
		return Agent.Main?.IsActive() ?? false;
	}

	private bool IsMainAgentHasAtLeastOneItem()
	{
		for (EquipmentIndex equipmentIndex = EquipmentIndex.WeaponItemBeginSlot; equipmentIndex < EquipmentIndex.NumAllWeaponSlots; equipmentIndex++)
		{
			if (!Agent.Main.Equipment[equipmentIndex].IsEmpty)
			{
				return true;
			}
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
