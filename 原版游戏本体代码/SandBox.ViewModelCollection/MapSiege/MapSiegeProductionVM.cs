using System;
using System.Collections.Generic;
using System.Linq;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.CampaignSystem.Siege;
using TaleWorlds.Core;
using TaleWorlds.Library;

namespace SandBox.ViewModelCollection.MapSiege
{
	// Token: 0x02000053 RID: 83
	public class MapSiegeProductionVM : ViewModel
	{
		// Token: 0x17000187 RID: 391
		// (get) Token: 0x06000528 RID: 1320 RVA: 0x00013944 File Offset: 0x00011B44
		private SiegeEvent Siege
		{
			get
			{
				return PlayerSiege.PlayerSiegeEvent;
			}
		}

		// Token: 0x17000188 RID: 392
		// (get) Token: 0x06000529 RID: 1321 RVA: 0x0001394B File Offset: 0x00011B4B
		private BattleSideEnum PlayerSide
		{
			get
			{
				return PlayerSiege.PlayerSide;
			}
		}

		// Token: 0x17000189 RID: 393
		// (get) Token: 0x0600052A RID: 1322 RVA: 0x00013952 File Offset: 0x00011B52
		private Settlement Settlement
		{
			get
			{
				return this.Siege.BesiegedSettlement;
			}
		}

		// Token: 0x1700018A RID: 394
		// (get) Token: 0x0600052B RID: 1323 RVA: 0x0001395F File Offset: 0x00011B5F
		// (set) Token: 0x0600052C RID: 1324 RVA: 0x00013967 File Offset: 0x00011B67
		public MapSiegePOIVM LatestSelectedPOI { get; private set; }

		// Token: 0x0600052D RID: 1325 RVA: 0x00013970 File Offset: 0x00011B70
		public MapSiegeProductionVM()
		{
			this.PossibleProductionMachines = new MBBindingList<MapSiegeProductionMachineVM>();
		}

		// Token: 0x0600052E RID: 1326 RVA: 0x00013983 File Offset: 0x00011B83
		public override void RefreshValues()
		{
			base.RefreshValues();
			this.PossibleProductionMachines.ApplyActionOnAllItems(delegate(MapSiegeProductionMachineVM x)
			{
				x.RefreshValues();
			});
		}

		// Token: 0x0600052F RID: 1327 RVA: 0x000139B8 File Offset: 0x00011BB8
		public void Update()
		{
			if (this.IsEnabled && this.LatestSelectedPOI.Machine == null)
			{
				if (this.PossibleProductionMachines.Any((MapSiegeProductionMachineVM m) => m.IsReserveOption))
				{
					this.ExecuteDisable();
				}
			}
		}

		// Token: 0x06000530 RID: 1328 RVA: 0x00013A0C File Offset: 0x00011C0C
		public void OnMachineSelection(MapSiegePOIVM poi)
		{
			this.PossibleProductionMachines.Clear();
			this.LatestSelectedPOI = poi;
			MapSiegePOIVM latestSelectedPOI = this.LatestSelectedPOI;
			if (((latestSelectedPOI != null) ? latestSelectedPOI.Machine : null) != null)
			{
				this.PossibleProductionMachines.Add(new MapSiegeProductionMachineVM(new Action<MapSiegeProductionMachineVM>(this.OnPossibleMachineSelection), !this.LatestSelectedPOI.Machine.IsActive && !this.LatestSelectedPOI.Machine.IsBeingRedeployed));
			}
			else
			{
				IEnumerable<SiegeEngineType> enumerable;
				switch (poi.Type)
				{
				case MapSiegePOIVM.POIType.DefenderSiegeMachine:
					enumerable = this.GetAllDefenderMachines();
					goto IL_BE;
				case MapSiegePOIVM.POIType.AttackerRamSiegeMachine:
					enumerable = this.GetAllAttackerRamMachines();
					goto IL_BE;
				case MapSiegePOIVM.POIType.AttackerTowerSiegeMachine:
					enumerable = this.GetAllAttackerTowerMachines();
					goto IL_BE;
				case MapSiegePOIVM.POIType.AttackerRangedSiegeMachine:
					enumerable = this.GetAllAttackerRangedMachines();
					goto IL_BE;
				}
				this.IsEnabled = false;
				return;
				IL_BE:
				using (IEnumerator<SiegeEngineType> enumerator = enumerable.GetEnumerator())
				{
					while (enumerator.MoveNext())
					{
						SiegeEngineType desMachine = enumerator.Current;
						int number = this.Siege.GetSiegeEventSide(this.PlayerSide).SiegeEngines.ReservedSiegeEngines.Count((SiegeEvent.SiegeEngineConstructionProgress m) => m.SiegeEngine == desMachine);
						this.PossibleProductionMachines.Add(new MapSiegeProductionMachineVM(desMachine, number, new Action<MapSiegeProductionMachineVM>(this.OnPossibleMachineSelection)));
					}
				}
			}
			this.IsEnabled = true;
		}

		// Token: 0x06000531 RID: 1329 RVA: 0x00013B70 File Offset: 0x00011D70
		private void OnPossibleMachineSelection(MapSiegeProductionMachineVM machine)
		{
			if (this.LatestSelectedPOI.Machine == null || this.LatestSelectedPOI.Machine.SiegeEngine != machine.Engine)
			{
				ISiegeEventSide siegeEventSide = this.Siege.GetSiegeEventSide(this.PlayerSide);
				if (machine.IsReserveOption && this.LatestSelectedPOI.Machine != null)
				{
					bool moveToReserve = this.LatestSelectedPOI.Machine.IsActive || this.LatestSelectedPOI.Machine.IsBeingRedeployed;
					siegeEventSide.SiegeEngines.RemoveDeployedSiegeEngine(this.LatestSelectedPOI.MachineIndex, this.LatestSelectedPOI.Machine.SiegeEngine.IsRanged, moveToReserve);
				}
				else
				{
					SiegeEvent.SiegeEngineConstructionProgress siegeEngineConstructionProgress = siegeEventSide.SiegeEngines.ReservedSiegeEngines.FirstOrDefault((SiegeEvent.SiegeEngineConstructionProgress e) => e.SiegeEngine == machine.Engine);
					if (siegeEngineConstructionProgress == null)
					{
						float siegeEngineHitPoints = Campaign.Current.Models.SiegeEventModel.GetSiegeEngineHitPoints(PlayerSiege.PlayerSiegeEvent, machine.Engine, this.PlayerSide);
						siegeEngineConstructionProgress = new SiegeEvent.SiegeEngineConstructionProgress(machine.Engine, 0f, siegeEngineHitPoints);
					}
					if (siegeEventSide.SiegeStrategy != DefaultSiegeStrategies.Custom && Campaign.Current.Models.EncounterModel.GetLeaderOfSiegeEvent(this.Siege, siegeEventSide.BattleSide) == Hero.MainHero)
					{
						siegeEventSide.SetSiegeStrategy(DefaultSiegeStrategies.Custom);
					}
					siegeEventSide.SiegeEngines.DeploySiegeEngineAtIndex(siegeEngineConstructionProgress, this.LatestSelectedPOI.MachineIndex);
				}
				this.Siege.BesiegedSettlement.Party.SetVisualAsDirty();
				Game.Current.EventManager.TriggerEvent<PlayerStartEngineConstructionEvent>(new PlayerStartEngineConstructionEvent(machine.Engine));
			}
			this.IsEnabled = false;
		}

		// Token: 0x06000532 RID: 1330 RVA: 0x00013D32 File Offset: 0x00011F32
		public void ExecuteDisable()
		{
			this.IsEnabled = false;
		}

		// Token: 0x06000533 RID: 1331 RVA: 0x00013D3B File Offset: 0x00011F3B
		private IEnumerable<SiegeEngineType> GetAllDefenderMachines()
		{
			return Campaign.Current.Models.SiegeEventModel.GetAvailableDefenderSiegeEngines(PartyBase.MainParty);
		}

		// Token: 0x06000534 RID: 1332 RVA: 0x00013D56 File Offset: 0x00011F56
		private IEnumerable<SiegeEngineType> GetAllAttackerRangedMachines()
		{
			return Campaign.Current.Models.SiegeEventModel.GetAvailableAttackerRangedSiegeEngines(PartyBase.MainParty);
		}

		// Token: 0x06000535 RID: 1333 RVA: 0x00013D71 File Offset: 0x00011F71
		private IEnumerable<SiegeEngineType> GetAllAttackerRamMachines()
		{
			return Campaign.Current.Models.SiegeEventModel.GetAvailableAttackerRamSiegeEngines(PartyBase.MainParty);
		}

		// Token: 0x06000536 RID: 1334 RVA: 0x00013D8C File Offset: 0x00011F8C
		private IEnumerable<SiegeEngineType> GetAllAttackerTowerMachines()
		{
			return Campaign.Current.Models.SiegeEventModel.GetAvailableAttackerTowerSiegeEngines(PartyBase.MainParty);
		}

		// Token: 0x1700018B RID: 395
		// (get) Token: 0x06000537 RID: 1335 RVA: 0x00013DA7 File Offset: 0x00011FA7
		// (set) Token: 0x06000538 RID: 1336 RVA: 0x00013DAF File Offset: 0x00011FAF
		[DataSourceProperty]
		public bool IsEnabled
		{
			get
			{
				return this._isEnabled;
			}
			set
			{
				if (value != this._isEnabled)
				{
					this._isEnabled = value;
					base.OnPropertyChangedWithValue(value, "IsEnabled");
				}
			}
		}

		// Token: 0x1700018C RID: 396
		// (get) Token: 0x06000539 RID: 1337 RVA: 0x00013DCD File Offset: 0x00011FCD
		// (set) Token: 0x0600053A RID: 1338 RVA: 0x00013DD5 File Offset: 0x00011FD5
		[DataSourceProperty]
		public MBBindingList<MapSiegeProductionMachineVM> PossibleProductionMachines
		{
			get
			{
				return this._possibleProductionMachines;
			}
			set
			{
				if (value != this._possibleProductionMachines)
				{
					this._possibleProductionMachines = value;
					base.OnPropertyChangedWithValue<MBBindingList<MapSiegeProductionMachineVM>>(value, "PossibleProductionMachines");
				}
			}
		}

		// Token: 0x04000296 RID: 662
		private MBBindingList<MapSiegeProductionMachineVM> _possibleProductionMachines;

		// Token: 0x04000297 RID: 663
		private bool _isEnabled;
	}
}
