using System;
using System.Collections.Generic;
using System.Linq;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.CampaignSystem.Siege;
using TaleWorlds.Core;
using TaleWorlds.Core.ViewModelCollection.Information;
using TaleWorlds.Engine;
using TaleWorlds.Library;
using TaleWorlds.MountAndBlade;

namespace SandBox.ViewModelCollection.MapSiege;

public class MapSiegePOIVM : ViewModel
{
	public enum POIType
	{
		WallSection,
		DefenderSiegeMachine,
		AttackerRamSiegeMachine,
		AttackerTowerSiegeMachine,
		AttackerRangedSiegeMachine
	}

	public enum MachineTypes
	{
		None = -1,
		Wall,
		BrokenWall,
		Ballista,
		Trebuchet,
		Ladder,
		Ram,
		SiegeTower,
		Mangonel
	}

	private readonly Vec3 _mapSceneLocation;

	private readonly Camera _mapCamera;

	private readonly BattleSideEnum _thisSide;

	private readonly Action<MapSiegePOIVM> _onSelection;

	private float _latestX;

	private float _latestY;

	private float _latestW;

	private float _bindCurrentHitpoints;

	private float _bindMaxHitpoints;

	private float _bindWPos;

	private int _bindWSign;

	private int _bindMachineType = -1;

	private int _bindQueueIndex;

	private bool _bindIsInside;

	private bool _bindHasItem;

	private bool _bindIsConstructing;

	private Vec2 _bindPosition;

	private bool _bindIsInVisibleRange;

	private Color _sidePrimaryColor;

	private Color _sideSecondaryColor;

	private Vec2 _position;

	private float _currentHitpoints;

	private int _machineType = -1;

	private float _maxHitpoints;

	private int _queueIndex;

	private bool _isInside;

	private bool _hasItem;

	private bool _isConstructing;

	private bool _isPlayerSidePOI;

	private bool _isFireVersion;

	private bool _isInVisibleRange;

	private bool _isSelected;

	private SiegeEvent Siege => PlayerSiege.PlayerSiegeEvent;

	private BattleSideEnum PlayerSide => PlayerSiege.PlayerSide;

	private Settlement Settlement => Siege.BesiegedSettlement;

	public POIType Type { get; }

	public int MachineIndex { get; }

	public float LatestW => _latestW;

	public SiegeEvent.SiegeEngineConstructionProgress Machine { get; private set; }

	public MatrixFrame MapSceneLocationFrame { get; private set; }

	public Vec2 Position
	{
		get
		{
			return _position;
		}
		set
		{
			if (_position != value)
			{
				_position = value;
				OnPropertyChangedWithValue(value, "Position");
			}
		}
	}

	public Color SidePrimaryColor
	{
		get
		{
			return _sidePrimaryColor;
		}
		set
		{
			if (_sidePrimaryColor != value)
			{
				_sidePrimaryColor = value;
				OnPropertyChangedWithValue(value, "SidePrimaryColor");
			}
		}
	}

	public Color SideSecondaryColor
	{
		get
		{
			return _sideSecondaryColor;
		}
		set
		{
			if (_sideSecondaryColor != value)
			{
				_sideSecondaryColor = value;
				OnPropertyChangedWithValue(value, "SideSecondaryColor");
			}
		}
	}

	public int QueueIndex
	{
		get
		{
			return _queueIndex;
		}
		set
		{
			if (_queueIndex != value)
			{
				_queueIndex = value;
				OnPropertyChangedWithValue(value, "QueueIndex");
			}
		}
	}

	public int MachineType
	{
		get
		{
			return _machineType;
		}
		set
		{
			if (_machineType != value)
			{
				_machineType = value;
				OnPropertyChangedWithValue(value, "MachineType");
			}
		}
	}

	public float CurrentHitpoints
	{
		get
		{
			return _currentHitpoints;
		}
		set
		{
			if (_currentHitpoints != value)
			{
				_currentHitpoints = value;
				OnPropertyChangedWithValue(value, "CurrentHitpoints");
			}
		}
	}

	public float MaxHitpoints
	{
		get
		{
			return _maxHitpoints;
		}
		set
		{
			if (_maxHitpoints != value)
			{
				_maxHitpoints = value;
				OnPropertyChangedWithValue(value, "MaxHitpoints");
			}
		}
	}

	public bool IsPlayerSidePOI
	{
		get
		{
			return _isPlayerSidePOI;
		}
		set
		{
			if (_isPlayerSidePOI != value)
			{
				_isPlayerSidePOI = value;
				OnPropertyChangedWithValue(value, "IsPlayerSidePOI");
			}
		}
	}

	public bool IsFireVersion
	{
		get
		{
			return _isFireVersion;
		}
		set
		{
			if (_isFireVersion != value)
			{
				_isFireVersion = value;
				OnPropertyChangedWithValue(value, "IsFireVersion");
			}
		}
	}

	public bool IsInVisibleRange
	{
		get
		{
			return _isInVisibleRange;
		}
		set
		{
			if (_isInVisibleRange != value)
			{
				_isInVisibleRange = value;
				OnPropertyChangedWithValue(value, "IsInVisibleRange");
			}
		}
	}

	public bool IsConstructing
	{
		get
		{
			return _isConstructing;
		}
		set
		{
			if (_isConstructing != value)
			{
				_isConstructing = value;
				OnPropertyChangedWithValue(value, "IsConstructing");
			}
		}
	}

	public bool IsSelected
	{
		get
		{
			return _isSelected;
		}
		set
		{
			if (_isSelected != value)
			{
				_isSelected = value;
				OnPropertyChangedWithValue(value, "IsSelected");
			}
		}
	}

	public bool HasItem
	{
		get
		{
			return _hasItem;
		}
		set
		{
			if (_hasItem != value)
			{
				_hasItem = value;
				OnPropertyChangedWithValue(value, "HasItem");
			}
		}
	}

	public bool IsInside
	{
		get
		{
			return _isInside;
		}
		set
		{
			if (_isInside != value)
			{
				_isInside = value;
				OnPropertyChangedWithValue(value, "IsInside");
			}
		}
	}

	public MapSiegePOIVM(POIType type, MatrixFrame mapSceneLocation, Camera mapCamera, int machineIndex, Action<MapSiegePOIVM> onSelection)
	{
		Type = type;
		_onSelection = onSelection;
		_thisSide = ((Type == POIType.AttackerRamSiegeMachine || Type == POIType.AttackerTowerSiegeMachine || Type == POIType.AttackerRangedSiegeMachine) ? BattleSideEnum.Attacker : BattleSideEnum.Defender);
		MapSceneLocationFrame = mapSceneLocation;
		_mapSceneLocation = MapSceneLocationFrame.origin;
		_mapCamera = mapCamera;
		MachineIndex = machineIndex;
		SidePrimaryColor = ((_thisSide == BattleSideEnum.Attacker) ? Color.FromUint(Siege.BesiegerCamp.MapFaction?.Color ?? 0) : Color.FromUint(Siege.BesiegedSettlement.MapFaction?.Color ?? 0));
		SideSecondaryColor = ((_thisSide == BattleSideEnum.Attacker) ? Color.FromUint(Siege.BesiegerCamp.MapFaction?.Color2 ?? 0) : Color.FromUint(Siege.BesiegedSettlement.MapFaction?.Color2 ?? 0));
		IsPlayerSidePOI = DetermineIfPOIIsPlayerSide();
	}

	public void ExecuteSelection()
	{
		_onSelection(this);
		IsSelected = true;
	}

	public void UpdateProperties()
	{
		Machine = GetDesiredMachine();
		_bindHasItem = Type == POIType.WallSection || Machine != null;
		SiegeEvent.SiegeEngineConstructionProgress machine = Machine;
		_bindIsConstructing = machine != null && !machine.IsActive;
		RefreshMachineType();
		RefreshHitpoints();
		RefreshQueueIndex();
	}

	public void RefreshDistanceValue(float newDistance)
	{
		_bindIsInVisibleRange = newDistance <= 20f;
	}

	public void RefreshPosition()
	{
		_latestX = 0f;
		_latestY = 0f;
		_latestW = 0f;
		MBWindowManager.WorldToScreenInsideUsableArea(_mapCamera, _mapSceneLocation, ref _latestX, ref _latestY, ref _latestW);
		if (!TaleWorlds.Library.MathF.IsValidValue(_latestX) || !TaleWorlds.Library.MathF.IsValidValue(_latestY) || !TaleWorlds.Library.MathF.IsValidValue(_latestW))
		{
			_latestX = -10000f;
			_latestY = -10000f;
			_latestW = -1f;
		}
		_bindWPos = _latestW;
		_bindWSign = (int)_bindWPos;
		_bindIsInside = IsInsideWindow();
		_bindPosition = new Vec2(_latestX, _latestY);
	}

	public void RefreshBinding()
	{
		Position = _bindPosition;
		IsInside = _bindIsInside;
		CurrentHitpoints = _bindCurrentHitpoints;
		MaxHitpoints = _bindMaxHitpoints;
		HasItem = _bindHasItem;
		IsConstructing = _bindIsConstructing;
		MachineType = _bindMachineType;
		QueueIndex = _bindQueueIndex;
		IsInVisibleRange = _bindIsInVisibleRange;
	}

	private void RefreshHitpoints()
	{
		if (Siege != null)
		{
			switch (Type)
			{
			case POIType.WallSection:
			{
				MBReadOnlyList<float> settlementWallSectionHitPointsRatioList = Settlement.SettlementWallSectionHitPointsRatioList;
				_bindMaxHitpoints = Settlement.MaxWallHitPoints / (float)Settlement.WallSectionCount;
				_bindCurrentHitpoints = settlementWallSectionHitPointsRatioList[MachineIndex] * _bindMaxHitpoints;
				_bindMachineType = ((_bindCurrentHitpoints <= 0f) ? 1 : 0);
				break;
			}
			case POIType.DefenderSiegeMachine:
			case POIType.AttackerRamSiegeMachine:
			case POIType.AttackerTowerSiegeMachine:
			case POIType.AttackerRangedSiegeMachine:
				if (Machine != null)
				{
					if (Machine.IsActive)
					{
						_bindCurrentHitpoints = Machine.Hitpoints;
						_bindMaxHitpoints = Machine.MaxHitPoints;
					}
					else if (Machine.IsBeingRedeployed)
					{
						_bindCurrentHitpoints = Machine.RedeploymentProgress;
						_bindMaxHitpoints = 1f;
					}
					else
					{
						_bindCurrentHitpoints = Machine.Progress;
						_bindMaxHitpoints = 1f;
					}
				}
				else
				{
					_bindCurrentHitpoints = 0f;
					_bindMaxHitpoints = 0f;
				}
				break;
			}
		}
		else
		{
			_bindCurrentHitpoints = 0f;
			_bindMaxHitpoints = 0f;
		}
	}

	private void RefreshMachineType()
	{
		if (Siege != null)
		{
			switch (Type)
			{
			case POIType.WallSection:
				_bindMachineType = 0;
				break;
			case POIType.DefenderSiegeMachine:
			case POIType.AttackerRamSiegeMachine:
			case POIType.AttackerTowerSiegeMachine:
			case POIType.AttackerRangedSiegeMachine:
				_bindMachineType = (int)((Machine != null) ? GetMachineTypeFromId(Machine.SiegeEngine.StringId) : MachineTypes.None);
				break;
			}
		}
		else
		{
			_bindMachineType = -1;
		}
	}

	private void RefreshQueueIndex()
	{
		_bindQueueIndex = ((Machine != null) ? Siege.GetSiegeEventSide(PlayerSide).SiegeEngines.DeployedSiegeEngines.Where((SiegeEvent.SiegeEngineConstructionProgress e) => !e.IsActive).ToList().IndexOf(Machine) : (-1));
	}

	private bool DetermineIfPOIIsPlayerSide()
	{
		switch (Type)
		{
		case POIType.WallSection:
		case POIType.DefenderSiegeMachine:
			return PlayerSide == BattleSideEnum.Defender;
		case POIType.AttackerRamSiegeMachine:
		case POIType.AttackerTowerSiegeMachine:
		case POIType.AttackerRangedSiegeMachine:
			return PlayerSide == BattleSideEnum.Attacker;
		default:
			return false;
		}
	}

	private bool IsInsideWindow()
	{
		if (!(_latestX - 100f > Screen.RealScreenResolutionWidth) && !(_latestY - 100f > Screen.RealScreenResolutionHeight) && !(_latestX + 100f < 0f))
		{
			return !(_latestY + 100f < 0f);
		}
		return false;
	}

	public void ExecuteShowTooltip()
	{
		InformationManager.ShowTooltip(typeof(List<TooltipProperty>), SandBoxUIHelper.GetSiegeEngineInProgressTooltip(Machine));
	}

	public void ExecuteHideTooltip()
	{
		MBInformationManager.HideInformations();
	}

	private MachineTypes GetMachineTypeFromId(string id)
	{
		switch (id.ToLower())
		{
		case "ballista":
		case "fire_ballista":
			return MachineTypes.Ballista;
		case "ram":
			return MachineTypes.Ram;
		case "siege_tower_level1":
		case "siege_tower_level2":
		case "siege_tower_level3":
			return MachineTypes.SiegeTower;
		case "catapult":
		case "mangonel":
		case "onager":
		case "fire_onager":
		case "fire_mangonel":
		case "fire_catapult":
			return MachineTypes.Mangonel;
		case "trebuchet":
		case "bricole":
			return MachineTypes.Trebuchet;
		case "ladder":
			return MachineTypes.Ladder;
		default:
			return MachineTypes.None;
		}
	}

	private SiegeEvent.SiegeEngineConstructionProgress GetDesiredMachine()
	{
		if (Siege != null)
		{
			switch (Type)
			{
			case POIType.DefenderSiegeMachine:
				return Siege.GetSiegeEventSide(BattleSideEnum.Defender).SiegeEngines.DeployedRangedSiegeEngines[MachineIndex];
			case POIType.AttackerRamSiegeMachine:
			case POIType.AttackerTowerSiegeMachine:
				return Siege.GetSiegeEventSide(BattleSideEnum.Attacker).SiegeEngines.DeployedMeleeSiegeEngines[MachineIndex];
			case POIType.AttackerRangedSiegeMachine:
				return Siege.GetSiegeEventSide(BattleSideEnum.Attacker).SiegeEngines.DeployedRangedSiegeEngines[MachineIndex];
			default:
				return null;
			}
		}
		return null;
	}
}
