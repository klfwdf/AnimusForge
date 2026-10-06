using System;
using TaleWorlds.Core;
using TaleWorlds.Core.ViewModelCollection.ImageIdentifiers;
using TaleWorlds.Engine;
using TaleWorlds.Library;
using TaleWorlds.MountAndBlade.ViewModelCollection.HUD;
using TaleWorlds.MountAndBlade.ViewModelCollection.HUD.DamageFeed;
using TaleWorlds.MountAndBlade.ViewModelCollection.Missions.Interaction;

namespace TaleWorlds.MountAndBlade.ViewModelCollection;

public class MissionAgentStatusVM : ViewModel
{
	private enum PassiveUsageStates
	{
		NotPossible,
		ConditionsNotMet,
		Possible,
		Active
	}

	private readonly Mission _mission;

	private readonly Camera _missionCamera;

	private float _combatUIRemainTimer;

	private const float _combatUIRemainDuration = 2f;

	private MissionPeer _missionPeer;

	private MissionMultiplayerGameModeBaseClient _mpGameMode;

	private readonly Func<float> _getCameraToggleProgress;

	private int _agentHealth;

	private int _agentHealthMax;

	private int _horseHealth;

	private int _horseHealthMax;

	private int _shieldHealth;

	private int _shieldHealthMax;

	private bool _isPlayerActive = true;

	private bool _isCombatUIActive;

	private bool _showAgentHealthBar;

	private bool _showMountHealthBar;

	private bool _showShieldHealthBar;

	private bool _troopsAmmoAvailable;

	private bool _isAgentStatusAvailable;

	private bool _isInteractionAvailable;

	private bool _isAgentStatusPrioritized;

	private float _troopsAmmoPercentage;

	private int _troopCount;

	private int _goldAmount;

	private bool _isTroopsActive;

	private bool _isGoldActive;

	private AgentInteractionInterfaceVM _interactionInterface;

	private ItemImageIdentifierVM _offhandWeapon;

	private ItemImageIdentifierVM _primaryWeapon;

	private MissionAgentTakenDamageVM _takenDamageController;

	private MissionAgentDamageFeedVM _takenDamageFeed;

	private int _ammoCount;

	private int _couchLanceState = -1;

	private int _spearBraceState = -1;

	private bool _showAmmoCount;

	private bool _isAmmoCountAlertEnabled;

	private float _cameraToggleProgress;

	private string _cameraToggleText;

	public bool IsInDeployement { get; set; }

	private MissionPeer _myMissionPeer
	{
		get
		{
			if (_missionPeer != null)
			{
				return _missionPeer;
			}
			if (GameNetwork.MyPeer != null)
			{
				_missionPeer = GameNetwork.MyPeer.GetComponent<MissionPeer>();
			}
			return _missionPeer;
		}
	}

	[DataSourceProperty]
	public MissionAgentTakenDamageVM TakenDamageController
	{
		get
		{
			return _takenDamageController;
		}
		set
		{
			if (value != _takenDamageController)
			{
				_takenDamageController = value;
				OnPropertyChangedWithValue(value, "TakenDamageController");
			}
		}
	}

	[DataSourceProperty]
	public AgentInteractionInterfaceVM InteractionInterface
	{
		get
		{
			return _interactionInterface;
		}
		set
		{
			if (value != _interactionInterface)
			{
				_interactionInterface = value;
				OnPropertyChangedWithValue(value, "InteractionInterface");
			}
		}
	}

	[DataSourceProperty]
	public int AgentHealth
	{
		get
		{
			return _agentHealth;
		}
		set
		{
			if (value != _agentHealth)
			{
				if (value <= 0)
				{
					_agentHealth = 0;
					OffhandWeapon = new ItemImageIdentifierVM(null);
					PrimaryWeapon = new ItemImageIdentifierVM(null);
					AmmoCount = -1;
					ShieldHealth = 100;
					IsPlayerActive = false;
				}
				else
				{
					_agentHealth = value;
				}
				OnPropertyChangedWithValue(value, "AgentHealth");
			}
		}
	}

	[DataSourceProperty]
	public int AgentHealthMax
	{
		get
		{
			return _agentHealthMax;
		}
		set
		{
			if (value != _agentHealthMax)
			{
				_agentHealthMax = value;
				OnPropertyChangedWithValue(value, "AgentHealthMax");
			}
		}
	}

	[DataSourceProperty]
	public int HorseHealth
	{
		get
		{
			return _horseHealth;
		}
		set
		{
			if (value != _horseHealth)
			{
				_horseHealth = value;
				OnPropertyChangedWithValue(value, "HorseHealth");
			}
		}
	}

	[DataSourceProperty]
	public int HorseHealthMax
	{
		get
		{
			return _horseHealthMax;
		}
		set
		{
			if (value != _horseHealthMax)
			{
				_horseHealthMax = value;
				OnPropertyChangedWithValue(value, "HorseHealthMax");
			}
		}
	}

	[DataSourceProperty]
	public int ShieldHealth
	{
		get
		{
			return _shieldHealth;
		}
		set
		{
			if (value != _shieldHealth)
			{
				_shieldHealth = value;
				OnPropertyChangedWithValue(value, "ShieldHealth");
			}
		}
	}

	[DataSourceProperty]
	public int ShieldHealthMax
	{
		get
		{
			return _shieldHealthMax;
		}
		set
		{
			if (value != _shieldHealthMax)
			{
				_shieldHealthMax = value;
				OnPropertyChangedWithValue(value, "ShieldHealthMax");
			}
		}
	}

	[DataSourceProperty]
	public bool IsPlayerActive
	{
		get
		{
			return _isPlayerActive;
		}
		set
		{
			if (value != _isPlayerActive)
			{
				_isPlayerActive = value;
				OnPropertyChangedWithValue(value, "IsPlayerActive");
			}
		}
	}

	public bool IsCombatUIActive
	{
		get
		{
			return _isCombatUIActive;
		}
		set
		{
			if (value != _isCombatUIActive)
			{
				_isCombatUIActive = value;
				OnPropertyChangedWithValue(value, "IsCombatUIActive");
				_combatUIRemainTimer = 0f;
			}
		}
	}

	[DataSourceProperty]
	public bool ShowAgentHealthBar
	{
		get
		{
			return _showAgentHealthBar;
		}
		set
		{
			if (value != _showAgentHealthBar)
			{
				_showAgentHealthBar = value;
				OnPropertyChangedWithValue(value, "ShowAgentHealthBar");
			}
		}
	}

	[DataSourceProperty]
	public bool ShowMountHealthBar
	{
		get
		{
			return _showMountHealthBar;
		}
		set
		{
			if (value != _showMountHealthBar)
			{
				_showMountHealthBar = value;
				OnPropertyChangedWithValue(value, "ShowMountHealthBar");
			}
		}
	}

	[DataSourceProperty]
	public bool ShowShieldHealthBar
	{
		get
		{
			return _showShieldHealthBar;
		}
		set
		{
			if (value != _showShieldHealthBar)
			{
				_showShieldHealthBar = value;
				OnPropertyChangedWithValue(value, "ShowShieldHealthBar");
			}
		}
	}

	[DataSourceProperty]
	public bool IsInteractionAvailable
	{
		get
		{
			return _isInteractionAvailable;
		}
		set
		{
			if (value != _isInteractionAvailable)
			{
				_isInteractionAvailable = value;
				OnPropertyChangedWithValue(value, "IsInteractionAvailable");
			}
		}
	}

	[DataSourceProperty]
	public bool IsAgentStatusPrioritized
	{
		get
		{
			return _isAgentStatusPrioritized;
		}
		set
		{
			if (value != _isAgentStatusPrioritized)
			{
				_isAgentStatusPrioritized = value;
				OnPropertyChangedWithValue(value, "IsAgentStatusPrioritized");
			}
		}
	}

	[DataSourceProperty]
	public bool IsAgentStatusAvailable
	{
		get
		{
			return _isAgentStatusAvailable;
		}
		set
		{
			if (value != _isAgentStatusAvailable)
			{
				_isAgentStatusAvailable = value;
				OnPropertyChangedWithValue(value, "IsAgentStatusAvailable");
			}
		}
	}

	[DataSourceProperty]
	public int CouchLanceState
	{
		get
		{
			return _couchLanceState;
		}
		set
		{
			if (value != _couchLanceState)
			{
				_couchLanceState = value;
				OnPropertyChangedWithValue(value, "CouchLanceState");
			}
		}
	}

	[DataSourceProperty]
	public int SpearBraceState
	{
		get
		{
			return _spearBraceState;
		}
		set
		{
			if (value != _spearBraceState)
			{
				_spearBraceState = value;
				OnPropertyChangedWithValue(value, "SpearBraceState");
			}
		}
	}

	[DataSourceProperty]
	public int TroopCount
	{
		get
		{
			return _troopCount;
		}
		set
		{
			if (value != _troopCount)
			{
				_troopCount = value;
				OnPropertyChangedWithValue(value, "TroopCount");
			}
		}
	}

	[DataSourceProperty]
	public bool IsTroopsActive
	{
		get
		{
			return _isTroopsActive;
		}
		set
		{
			if (value != _isTroopsActive)
			{
				_isTroopsActive = value;
				OnPropertyChangedWithValue(value, "IsTroopsActive");
			}
		}
	}

	[DataSourceProperty]
	public bool IsGoldActive
	{
		get
		{
			return _isGoldActive;
		}
		set
		{
			if (value != _isGoldActive)
			{
				_isGoldActive = value;
				OnPropertyChangedWithValue(value, "IsGoldActive");
			}
		}
	}

	[DataSourceProperty]
	public int GoldAmount
	{
		get
		{
			return _goldAmount;
		}
		set
		{
			if (value != _goldAmount)
			{
				_goldAmount = value;
				OnPropertyChangedWithValue(value, "GoldAmount");
			}
		}
	}

	[DataSourceProperty]
	public bool ShowAmmoCount
	{
		get
		{
			return _showAmmoCount;
		}
		set
		{
			if (value != _showAmmoCount)
			{
				_showAmmoCount = value;
				OnPropertyChangedWithValue(value, "ShowAmmoCount");
			}
		}
	}

	[DataSourceProperty]
	public int AmmoCount
	{
		get
		{
			return _ammoCount;
		}
		set
		{
			if (value != _ammoCount)
			{
				_ammoCount = value;
				OnPropertyChangedWithValue(value, "AmmoCount");
				ShowAmmoCount = value >= 0;
			}
		}
	}

	[DataSourceProperty]
	public float TroopsAmmoPercentage
	{
		get
		{
			return _troopsAmmoPercentage;
		}
		set
		{
			if (value != _troopsAmmoPercentage)
			{
				_troopsAmmoPercentage = value;
				OnPropertyChangedWithValue(value, "TroopsAmmoPercentage");
			}
		}
	}

	[DataSourceProperty]
	public bool TroopsAmmoAvailable
	{
		get
		{
			return _troopsAmmoAvailable;
		}
		set
		{
			if (value != _troopsAmmoAvailable)
			{
				_troopsAmmoAvailable = value;
				OnPropertyChangedWithValue(value, "TroopsAmmoAvailable");
			}
		}
	}

	[DataSourceProperty]
	public bool IsAmmoCountAlertEnabled
	{
		get
		{
			return _isAmmoCountAlertEnabled;
		}
		set
		{
			if (value != _isAmmoCountAlertEnabled)
			{
				_isAmmoCountAlertEnabled = value;
				OnPropertyChangedWithValue(value, "IsAmmoCountAlertEnabled");
			}
		}
	}

	[DataSourceProperty]
	public float CameraToggleProgress
	{
		get
		{
			return _cameraToggleProgress;
		}
		set
		{
			if (value != _cameraToggleProgress)
			{
				_cameraToggleProgress = value;
				OnPropertyChangedWithValue(value, "CameraToggleProgress");
			}
		}
	}

	[DataSourceProperty]
	public string CameraToggleText
	{
		get
		{
			return _cameraToggleText;
		}
		set
		{
			if (value != _cameraToggleText)
			{
				_cameraToggleText = value;
				OnPropertyChangedWithValue(value, "CameraToggleText");
			}
		}
	}

	[DataSourceProperty]
	public ItemImageIdentifierVM OffhandWeapon
	{
		get
		{
			return _offhandWeapon;
		}
		set
		{
			if (value != _offhandWeapon)
			{
				_offhandWeapon = value;
				OnPropertyChangedWithValue(value, "OffhandWeapon");
			}
		}
	}

	[DataSourceProperty]
	public ItemImageIdentifierVM PrimaryWeapon
	{
		get
		{
			return _primaryWeapon;
		}
		set
		{
			if (value != _primaryWeapon)
			{
				_primaryWeapon = value;
				OnPropertyChangedWithValue(value, "PrimaryWeapon");
			}
		}
	}

	[DataSourceProperty]
	public MissionAgentDamageFeedVM TakenDamageFeed
	{
		get
		{
			return _takenDamageFeed;
		}
		set
		{
			if (value != _takenDamageFeed)
			{
				_takenDamageFeed = value;
				OnPropertyChangedWithValue(value, "TakenDamageFeed");
			}
		}
	}

	public MissionAgentStatusVM(Mission mission, Camera missionCamera, Func<float> getCameraToggleProgress)
	{
		InteractionInterface = new AgentInteractionInterfaceVM(mission);
		_mission = mission;
		_missionCamera = missionCamera;
		_getCameraToggleProgress = getCameraToggleProgress;
		PrimaryWeapon = new ItemImageIdentifierVM(null);
		OffhandWeapon = new ItemImageIdentifierVM(null);
		TakenDamageFeed = new MissionAgentDamageFeedVM();
		TakenDamageController = new MissionAgentTakenDamageVM(_missionCamera);
		IsInteractionAvailable = true;
		IsAgentStatusPrioritized = true;
		RefreshValues();
	}

	public void InitializeMainAgentPropterties()
	{
		Mission.Current.OnMainAgentChanged += OnMainAgentChanged;
		OnMainAgentChanged(null);
		OnMainAgentWeaponChange();
		_mpGameMode = Mission.Current.GetMissionBehavior<MissionMultiplayerGameModeBaseClient>();
	}

	public override void RefreshValues()
	{
		base.RefreshValues();
		CameraToggleText = GameTexts.FindText("str_toggle_camera").ToString();
	}

	private void OnMainAgentChanged(Agent oldAgent)
	{
		if (oldAgent != null)
		{
			oldAgent.OnMainAgentWieldedItemChange = (Agent.OnMainAgentWieldedItemChangeDelegate)Delegate.Remove(oldAgent.OnMainAgentWieldedItemChange, new Agent.OnMainAgentWieldedItemChangeDelegate(OnMainAgentWeaponChange));
		}
		if (Agent.Main != null)
		{
			Agent main = Agent.Main;
			main.OnMainAgentWieldedItemChange = (Agent.OnMainAgentWieldedItemChangeDelegate)Delegate.Combine(main.OnMainAgentWieldedItemChange, new Agent.OnMainAgentWieldedItemChangeDelegate(OnMainAgentWeaponChange));
			OnMainAgentWeaponChange();
		}
	}

	public override void OnFinalize()
	{
		base.OnFinalize();
		if (Agent.Main != null)
		{
			Agent main = Agent.Main;
			main.OnMainAgentWieldedItemChange = (Agent.OnMainAgentWieldedItemChangeDelegate)Delegate.Remove(main.OnMainAgentWieldedItemChange, new Agent.OnMainAgentWieldedItemChangeDelegate(OnMainAgentWeaponChange));
		}
		Mission.Current.OnMainAgentChanged -= OnMainAgentChanged;
		TakenDamageFeed.OnFinalize();
	}

	public void Tick(float dt)
	{
		if (_mission == null)
		{
			return;
		}
		CouchLanceState = GetCouchLanceState();
		SpearBraceState = GetSpearBraceState();
		CameraToggleProgress = _getCameraToggleProgress?.Invoke() ?? 0f;
		if (_mission.MainAgent != null && !IsInDeployement)
		{
			ShowAgentHealthBar = true;
			InteractionInterface.Tick(dt);
			if (_mission.Mode == MissionMode.Battle && !_mission.IsFriendlyMission && _myMissionPeer != null)
			{
				IsTroopsActive = _myMissionPeer?.ControlledFormation != null;
				if (IsTroopsActive)
				{
					TroopCount = _myMissionPeer.ControlledFormation.CountOfUnits;
					if (_myMissionPeer.ControlledFormation.QuerySystem.RangedUnitRatio > 0f || _myMissionPeer.ControlledFormation.QuerySystem.RangedCavalryUnitRatio > 0f)
					{
						int totalCurrentAmmo = 0;
						int totalMaxAmmo = 0;
						_myMissionPeer.ControlledFormation.ApplyActionOnEachUnit(delegate(Agent agent)
						{
							if (!agent.IsMainAgent)
							{
								GetMaxAndCurrentAmmoOfAgent(agent, out var currentAmmo, out var maxAmmo);
								totalCurrentAmmo += currentAmmo;
								totalMaxAmmo += maxAmmo;
							}
						});
						if (totalMaxAmmo > 0)
						{
							TroopsAmmoAvailable = true;
							TroopsAmmoPercentage = (float)totalCurrentAmmo / (float)totalMaxAmmo;
						}
						else
						{
							TroopsAmmoAvailable = false;
						}
					}
					else
					{
						TroopsAmmoAvailable = false;
					}
				}
			}
			UpdateWeaponStatuses();
			UpdateAgentAndMountStatuses();
			IsPlayerActive = true;
			IsCombatUIActive = true;
		}
		else
		{
			AgentHealth = 0;
			ShowMountHealthBar = false;
			ShowShieldHealthBar = false;
			if (IsCombatUIActive)
			{
				_combatUIRemainTimer += dt;
				if (_combatUIRemainTimer >= 2f)
				{
					IsCombatUIActive = false;
				}
			}
		}
		IsGoldActive = _mpGameMode?.IsGameModeUsingGold ?? false;
		if (IsGoldActive && _myMissionPeer != null && _myMissionPeer.GetNetworkPeer().IsSynchronized)
		{
			GoldAmount = _mpGameMode?.GetGoldAmount() ?? 0;
		}
		TakenDamageController?.Tick(dt);
	}

	private void UpdateWeaponStatuses()
	{
		bool isAmmoCountAlertEnabled = false;
		if (_mission.MainAgent != null)
		{
			int ammoCount = -1;
			EquipmentIndex primaryWieldedItemIndex = _mission.MainAgent.GetPrimaryWieldedItemIndex();
			EquipmentIndex offhandWieldedItemIndex = _mission.MainAgent.GetOffhandWieldedItemIndex();
			if (primaryWieldedItemIndex != EquipmentIndex.None && _mission.MainAgent.Equipment[primaryWieldedItemIndex].CurrentUsageItem != null)
			{
				if (_mission.MainAgent.Equipment[primaryWieldedItemIndex].CurrentUsageItem.IsRangedWeapon && _mission.MainAgent.Equipment[primaryWieldedItemIndex].CurrentUsageItem.IsConsumable)
				{
					int num = ((!_mission.MainAgent.Equipment[primaryWieldedItemIndex].Item.PrimaryWeapon.IsConsumable && _mission.MainAgent.Equipment[primaryWieldedItemIndex].CurrentUsageItem.IsConsumable) ? 1 : _mission.MainAgent.Equipment.GetAmmoAmount(primaryWieldedItemIndex));
					if (_mission.MainAgent.Equipment[primaryWieldedItemIndex].ModifiedMaxAmount == 1 || num > 0)
					{
						ammoCount = num;
					}
				}
				else if (_mission.MainAgent.Equipment[primaryWieldedItemIndex].CurrentUsageItem.IsRangedWeapon)
				{
					bool flag = _mission.MainAgent.Equipment[primaryWieldedItemIndex].CurrentUsageItem.WeaponClass == WeaponClass.Crossbow;
					ammoCount = _mission.MainAgent.Equipment.GetAmmoAmount(primaryWieldedItemIndex) + (flag ? _mission.MainAgent.Equipment[primaryWieldedItemIndex].Ammo : 0);
				}
				if (!_mission.MainAgent.Equipment[primaryWieldedItemIndex].IsEmpty)
				{
					int num2 = ((!_mission.MainAgent.Equipment[primaryWieldedItemIndex].Item.PrimaryWeapon.IsConsumable && _mission.MainAgent.Equipment[primaryWieldedItemIndex].CurrentUsageItem.IsConsumable) ? 1 : _mission.MainAgent.Equipment.GetMaxAmmo(primaryWieldedItemIndex));
					float f = (float)num2 * 0.2f;
					isAmmoCountAlertEnabled = num2 != AmmoCount && AmmoCount <= TaleWorlds.Library.MathF.Ceiling(f);
				}
			}
			if (offhandWieldedItemIndex != EquipmentIndex.None && _mission.MainAgent.Equipment[offhandWieldedItemIndex].CurrentUsageItem != null)
			{
				MissionWeapon missionWeapon = _mission.MainAgent.Equipment[offhandWieldedItemIndex];
				ShowShieldHealthBar = missionWeapon.CurrentUsageItem.IsShield;
				if (ShowShieldHealthBar)
				{
					ShieldHealthMax = missionWeapon.ModifiedMaxHitPoints;
					ShieldHealth = missionWeapon.HitPoints;
				}
			}
			AmmoCount = ammoCount;
		}
		else
		{
			ShieldHealth = 0;
			AmmoCount = 0;
			ShowShieldHealthBar = false;
		}
		IsAmmoCountAlertEnabled = isAmmoCountAlertEnabled;
	}

	public void OnEquipmentInteractionViewToggled(bool isActive)
	{
		IsInteractionAvailable = !isActive;
	}

	private void UpdateAgentAndMountStatuses()
	{
		if (_mission.MainAgent != null)
		{
			AgentHealthMax = (int)_mission.MainAgent.HealthLimit;
			AgentHealth = (int)_mission.MainAgent.Health;
			if (_mission.MainAgent.MountAgent != null)
			{
				HorseHealthMax = (int)_mission.MainAgent.MountAgent.HealthLimit;
				HorseHealth = (int)_mission.MainAgent.MountAgent.Health;
				ShowMountHealthBar = true;
			}
			else
			{
				ShowMountHealthBar = false;
			}
		}
		else
		{
			AgentHealthMax = 1;
			AgentHealth = (int)_mission.MainAgent.Health;
			HorseHealthMax = 1;
			HorseHealth = 0;
			ShowMountHealthBar = false;
		}
	}

	public void OnMainAgentWeaponChange()
	{
		if (_mission.MainAgent != null)
		{
			MissionWeapon missionWeapon = MissionWeapon.Invalid;
			MissionWeapon missionWeapon2 = MissionWeapon.Invalid;
			EquipmentIndex offhandWieldedItemIndex = _mission.MainAgent.GetOffhandWieldedItemIndex();
			if (offhandWieldedItemIndex > EquipmentIndex.None && offhandWieldedItemIndex < EquipmentIndex.NumAllWeaponSlots)
			{
				missionWeapon = _mission.MainAgent.Equipment[offhandWieldedItemIndex];
			}
			offhandWieldedItemIndex = _mission.MainAgent.GetPrimaryWieldedItemIndex();
			if (offhandWieldedItemIndex > EquipmentIndex.None && offhandWieldedItemIndex < EquipmentIndex.NumAllWeaponSlots)
			{
				missionWeapon2 = _mission.MainAgent.Equipment[offhandWieldedItemIndex];
			}
			ShowShieldHealthBar = missionWeapon.CurrentUsageItem?.IsShield ?? false;
			PrimaryWeapon = (missionWeapon2.IsEmpty ? new ItemImageIdentifierVM(null) : new ItemImageIdentifierVM(missionWeapon2.Item));
			OffhandWeapon = (missionWeapon.IsEmpty ? new ItemImageIdentifierVM(null) : new ItemImageIdentifierVM(missionWeapon.Item));
		}
	}

	public void OnAgentRemoved(Agent agent)
	{
		InteractionInterface.CheckAndClearFocusedAgent(agent);
	}

	public void OnAgentDeleted(Agent agent)
	{
		InteractionInterface.CheckAndClearFocusedAgent(agent);
	}

	public void OnMainAgentHit(int damage, float distance)
	{
		TakenDamageController.OnMainAgentHit(damage, distance);
		TakenDamageFeed.OnMainAgentHit(damage);
	}

	public void OnFocusGained(Agent mainAgent, IFocusable focusableObject, bool isInteractable)
	{
		InteractionInterface.OnFocusGained(mainAgent, focusableObject, isInteractable);
	}

	public void OnFocusLost(Agent agent, IFocusable focusableObject)
	{
		InteractionInterface.OnFocusLost(agent, focusableObject);
	}

	public void OnSecondaryFocusGained(Agent agent, IFocusable focusableObject, bool isInteractable)
	{
	}

	public void OnSecondaryFocusLost(Agent agent, IFocusable focusableObject)
	{
	}

	public void OnAgentInteraction(Agent userAgent, Agent agent, sbyte agentBoneIndex)
	{
		InteractionInterface.OnAgentInteraction(userAgent, agent, agentBoneIndex);
	}

	private void GetMaxAndCurrentAmmoOfAgent(Agent agent, out int currentAmmo, out int maxAmmo)
	{
		currentAmmo = 0;
		maxAmmo = 0;
		for (EquipmentIndex equipmentIndex = EquipmentIndex.WeaponItemBeginSlot; equipmentIndex < EquipmentIndex.ExtraWeaponSlot; equipmentIndex++)
		{
			if (!agent.Equipment[equipmentIndex].IsEmpty && agent.Equipment[equipmentIndex].CurrentUsageItem.IsRangedWeapon)
			{
				currentAmmo = agent.Equipment.GetAmmoAmount(equipmentIndex);
				maxAmmo = agent.Equipment.GetMaxAmmo(equipmentIndex);
				break;
			}
		}
	}

	private int GetCouchLanceState()
	{
		int result = 0;
		if (Agent.Main != null)
		{
			MissionWeapon wieldedWeapon = Agent.Main.WieldedWeapon;
			if (Agent.Main.HasMount && IsWeaponCouchable(wieldedWeapon))
			{
				if (IsPassiveUsageActiveWithCurrentWeapon(wieldedWeapon))
				{
					result = 3;
				}
				else if (IsConditionsMetForCouching())
				{
					result = 2;
				}
			}
		}
		return result;
	}

	private bool IsWeaponCouchable(MissionWeapon weapon)
	{
		if (weapon.IsEmpty)
		{
			return false;
		}
		foreach (WeaponComponentData weapon2 in weapon.Item.Weapons)
		{
			string weaponDescriptionId = weapon2.WeaponDescriptionId;
			if (weaponDescriptionId != null && weaponDescriptionId.IndexOf("couch", StringComparison.OrdinalIgnoreCase) >= 0)
			{
				return true;
			}
		}
		return false;
	}

	private bool IsConditionsMetForCouching()
	{
		if (Agent.Main.HasMount)
		{
			return Agent.Main.IsPassiveUsageConditionsAreMet;
		}
		return false;
	}

	private int GetSpearBraceState()
	{
		int result = 0;
		if (Agent.Main != null)
		{
			MissionWeapon wieldedWeapon = Agent.Main.WieldedWeapon;
			if (!Agent.Main.HasMount && Agent.Main.GetOffhandWieldedItemIndex() == EquipmentIndex.None && IsWeaponBracable(wieldedWeapon))
			{
				if (IsPassiveUsageActiveWithCurrentWeapon(wieldedWeapon))
				{
					result = 3;
				}
				else if (IsConditionsMetForBracing())
				{
					result = 2;
				}
			}
		}
		return result;
	}

	private bool IsWeaponBracable(MissionWeapon weapon)
	{
		if (weapon.IsEmpty)
		{
			return false;
		}
		foreach (WeaponComponentData weapon2 in weapon.Item.Weapons)
		{
			string weaponDescriptionId = weapon2.WeaponDescriptionId;
			if (weaponDescriptionId != null && weaponDescriptionId.IndexOf("bracing", StringComparison.OrdinalIgnoreCase) >= 0)
			{
				return true;
			}
		}
		return false;
	}

	private bool IsConditionsMetForBracing()
	{
		if (!Agent.Main.HasMount && !Agent.Main.WalkMode)
		{
			return Agent.Main.IsPassiveUsageConditionsAreMet;
		}
		return false;
	}

	private bool IsPassiveUsageActiveWithCurrentWeapon(MissionWeapon weapon)
	{
		if (!weapon.IsEmpty)
		{
			return MBItem.GetItemIsPassiveUsage(weapon.CurrentUsageItem.ItemUsage);
		}
		return false;
	}
}
