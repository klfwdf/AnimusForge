using System.Collections.Generic;
using System.Linq;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.CampaignSystem.Siege;
using TaleWorlds.Core;
using TaleWorlds.Library;

namespace SandBox.ViewModelCollection.MapSiege;

public class MapSiegeProductionVM : ViewModel
{
	private MBBindingList<MapSiegeProductionMachineVM> _possibleProductionMachines;

	private bool _isEnabled;

	private SiegeEvent Siege => PlayerSiege.PlayerSiegeEvent;

	private BattleSideEnum PlayerSide => PlayerSiege.PlayerSide;

	private Settlement Settlement => Siege.BesiegedSettlement;

	public MapSiegePOIVM LatestSelectedPOI { get; private set; }

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
			}
		}
	}

	[DataSourceProperty]
	public MBBindingList<MapSiegeProductionMachineVM> PossibleProductionMachines
	{
		get
		{
			return _possibleProductionMachines;
		}
		set
		{
			if (value != _possibleProductionMachines)
			{
				_possibleProductionMachines = value;
				OnPropertyChangedWithValue(value, "PossibleProductionMachines");
			}
		}
	}

	public MapSiegeProductionVM()
	{
		PossibleProductionMachines = new MBBindingList<MapSiegeProductionMachineVM>();
	}

	public override void RefreshValues()
	{
		base.RefreshValues();
		PossibleProductionMachines.ApplyActionOnAllItems(delegate(MapSiegeProductionMachineVM x)
		{
			x.RefreshValues();
		});
	}

	public void Update()
	{
		if (IsEnabled && LatestSelectedPOI.Machine == null && PossibleProductionMachines.Any((MapSiegeProductionMachineVM m) => m.IsReserveOption))
		{
			ExecuteDisable();
		}
	}

	public void OnMachineSelection(MapSiegePOIVM poi)
	{
		PossibleProductionMachines.Clear();
		LatestSelectedPOI = poi;
		if (LatestSelectedPOI?.Machine != null)
		{
			PossibleProductionMachines.Add(new MapSiegeProductionMachineVM(OnPossibleMachineSelection, !LatestSelectedPOI.Machine.IsActive && !LatestSelectedPOI.Machine.IsBeingRedeployed));
		}
		else
		{
			IEnumerable<SiegeEngineType> enumerable;
			switch (poi.Type)
			{
			case MapSiegePOIVM.POIType.DefenderSiegeMachine:
				enumerable = GetAllDefenderMachines();
				break;
			case MapSiegePOIVM.POIType.AttackerRangedSiegeMachine:
				enumerable = GetAllAttackerRangedMachines();
				break;
			case MapSiegePOIVM.POIType.AttackerRamSiegeMachine:
				enumerable = GetAllAttackerRamMachines();
				break;
			case MapSiegePOIVM.POIType.AttackerTowerSiegeMachine:
				enumerable = GetAllAttackerTowerMachines();
				break;
			default:
				IsEnabled = false;
				return;
			}
			foreach (SiegeEngineType desMachine in enumerable)
			{
				int number = Siege.GetSiegeEventSide(PlayerSide).SiegeEngines.ReservedSiegeEngines.Count((SiegeEvent.SiegeEngineConstructionProgress m) => m.SiegeEngine == desMachine);
				PossibleProductionMachines.Add(new MapSiegeProductionMachineVM(desMachine, number, OnPossibleMachineSelection));
			}
		}
		IsEnabled = true;
	}

	private void OnPossibleMachineSelection(MapSiegeProductionMachineVM machine)
	{
		if (LatestSelectedPOI.Machine == null || LatestSelectedPOI.Machine.SiegeEngine != machine.Engine)
		{
			ISiegeEventSide siegeEventSide = Siege.GetSiegeEventSide(PlayerSide);
			if (machine.IsReserveOption && LatestSelectedPOI.Machine != null)
			{
				bool moveToReserve = LatestSelectedPOI.Machine.IsActive || LatestSelectedPOI.Machine.IsBeingRedeployed;
				siegeEventSide.SiegeEngines.RemoveDeployedSiegeEngine(LatestSelectedPOI.MachineIndex, LatestSelectedPOI.Machine.SiegeEngine.IsRanged, moveToReserve);
			}
			else
			{
				SiegeEvent.SiegeEngineConstructionProgress siegeEngineConstructionProgress = siegeEventSide.SiegeEngines.ReservedSiegeEngines.FirstOrDefault((SiegeEvent.SiegeEngineConstructionProgress e) => e.SiegeEngine == machine.Engine);
				if (siegeEngineConstructionProgress == null)
				{
					float siegeEngineHitPoints = Campaign.Current.Models.SiegeEventModel.GetSiegeEngineHitPoints(PlayerSiege.PlayerSiegeEvent, machine.Engine, PlayerSide);
					siegeEngineConstructionProgress = new SiegeEvent.SiegeEngineConstructionProgress(machine.Engine, 0f, siegeEngineHitPoints);
				}
				if (siegeEventSide.SiegeStrategy != DefaultSiegeStrategies.Custom && Campaign.Current.Models.EncounterModel.GetLeaderOfSiegeEvent(Siege, siegeEventSide.BattleSide) == Hero.MainHero)
				{
					siegeEventSide.SetSiegeStrategy(DefaultSiegeStrategies.Custom);
				}
				siegeEventSide.SiegeEngines.DeploySiegeEngineAtIndex(siegeEngineConstructionProgress, LatestSelectedPOI.MachineIndex);
			}
			Siege.BesiegedSettlement.Party.SetVisualAsDirty();
			Game.Current.EventManager.TriggerEvent(new PlayerStartEngineConstructionEvent(machine.Engine));
		}
		IsEnabled = false;
	}

	public void ExecuteDisable()
	{
		IsEnabled = false;
	}

	private IEnumerable<SiegeEngineType> GetAllDefenderMachines()
	{
		return Campaign.Current.Models.SiegeEventModel.GetAvailableDefenderSiegeEngines(PartyBase.MainParty);
	}

	private IEnumerable<SiegeEngineType> GetAllAttackerRangedMachines()
	{
		return Campaign.Current.Models.SiegeEventModel.GetAvailableAttackerRangedSiegeEngines(PartyBase.MainParty);
	}

	private IEnumerable<SiegeEngineType> GetAllAttackerRamMachines()
	{
		return Campaign.Current.Models.SiegeEventModel.GetAvailableAttackerRamSiegeEngines(PartyBase.MainParty);
	}

	private IEnumerable<SiegeEngineType> GetAllAttackerTowerMachines()
	{
		return Campaign.Current.Models.SiegeEventModel.GetAvailableAttackerTowerSiegeEngines(PartyBase.MainParty);
	}
}
