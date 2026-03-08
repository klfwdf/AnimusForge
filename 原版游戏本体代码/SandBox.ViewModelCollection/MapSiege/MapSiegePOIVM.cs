using System;
using System.Collections.Generic;
using System.Linq;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.CampaignSystem.Siege;
using TaleWorlds.Core;
using TaleWorlds.Core.ViewModelCollection.Information;
using TaleWorlds.Engine;
using TaleWorlds.Library;
using TaleWorlds.MountAndBlade;

namespace SandBox.ViewModelCollection.MapSiege
{
	// Token: 0x02000052 RID: 82
	public class MapSiegePOIVM : ViewModel
	{
		// Token: 0x17000171 RID: 369
		// (get) Token: 0x060004F3 RID: 1267 RVA: 0x00012E78 File Offset: 0x00011078
		private SiegeEvent Siege
		{
			get
			{
				return PlayerSiege.PlayerSiegeEvent;
			}
		}

		// Token: 0x17000172 RID: 370
		// (get) Token: 0x060004F4 RID: 1268 RVA: 0x00012E7F File Offset: 0x0001107F
		private BattleSideEnum PlayerSide
		{
			get
			{
				return PlayerSiege.PlayerSide;
			}
		}

		// Token: 0x17000173 RID: 371
		// (get) Token: 0x060004F5 RID: 1269 RVA: 0x00012E86 File Offset: 0x00011086
		private Settlement Settlement
		{
			get
			{
				return this.Siege.BesiegedSettlement;
			}
		}

		// Token: 0x17000174 RID: 372
		// (get) Token: 0x060004F6 RID: 1270 RVA: 0x00012E93 File Offset: 0x00011093
		public MapSiegePOIVM.POIType Type { get; }

		// Token: 0x17000175 RID: 373
		// (get) Token: 0x060004F7 RID: 1271 RVA: 0x00012E9B File Offset: 0x0001109B
		public int MachineIndex { get; }

		// Token: 0x17000176 RID: 374
		// (get) Token: 0x060004F8 RID: 1272 RVA: 0x00012EA3 File Offset: 0x000110A3
		public float LatestW
		{
			get
			{
				return this._latestW;
			}
		}

		// Token: 0x17000177 RID: 375
		// (get) Token: 0x060004F9 RID: 1273 RVA: 0x00012EAB File Offset: 0x000110AB
		// (set) Token: 0x060004FA RID: 1274 RVA: 0x00012EB3 File Offset: 0x000110B3
		public SiegeEvent.SiegeEngineConstructionProgress Machine { get; private set; }

		// Token: 0x17000178 RID: 376
		// (get) Token: 0x060004FB RID: 1275 RVA: 0x00012EBC File Offset: 0x000110BC
		// (set) Token: 0x060004FC RID: 1276 RVA: 0x00012EC4 File Offset: 0x000110C4
		public MatrixFrame MapSceneLocationFrame { get; private set; }

		// Token: 0x060004FD RID: 1277 RVA: 0x00012ED0 File Offset: 0x000110D0
		public MapSiegePOIVM(MapSiegePOIVM.POIType type, MatrixFrame mapSceneLocation, Camera mapCamera, int machineIndex, Action<MapSiegePOIVM> onSelection)
		{
			this.Type = type;
			this._onSelection = onSelection;
			this._thisSide = ((this.Type == MapSiegePOIVM.POIType.AttackerRamSiegeMachine || this.Type == MapSiegePOIVM.POIType.AttackerTowerSiegeMachine || this.Type == MapSiegePOIVM.POIType.AttackerRangedSiegeMachine) ? BattleSideEnum.Attacker : BattleSideEnum.Defender);
			this.MapSceneLocationFrame = mapSceneLocation;
			this._mapSceneLocation = this.MapSceneLocationFrame.origin;
			this._mapCamera = mapCamera;
			this.MachineIndex = machineIndex;
			Color sidePrimaryColor;
			if (this._thisSide != BattleSideEnum.Attacker)
			{
				IFaction mapFaction = this.Siege.BesiegedSettlement.MapFaction;
				sidePrimaryColor = Color.FromUint((mapFaction != null) ? mapFaction.Color : 0U);
			}
			else
			{
				IFaction mapFaction2 = this.Siege.BesiegerCamp.MapFaction;
				sidePrimaryColor = Color.FromUint((mapFaction2 != null) ? mapFaction2.Color : 0U);
			}
			this.SidePrimaryColor = sidePrimaryColor;
			Color sideSecondaryColor;
			if (this._thisSide != BattleSideEnum.Attacker)
			{
				IFaction mapFaction3 = this.Siege.BesiegedSettlement.MapFaction;
				sideSecondaryColor = Color.FromUint((mapFaction3 != null) ? mapFaction3.Color2 : 0U);
			}
			else
			{
				IFaction mapFaction4 = this.Siege.BesiegerCamp.MapFaction;
				sideSecondaryColor = Color.FromUint((mapFaction4 != null) ? mapFaction4.Color2 : 0U);
			}
			this.SideSecondaryColor = sideSecondaryColor;
			this.IsPlayerSidePOI = this.DetermineIfPOIIsPlayerSide();
		}

		// Token: 0x060004FE RID: 1278 RVA: 0x00012FFE File Offset: 0x000111FE
		public void ExecuteSelection()
		{
			this._onSelection(this);
			this.IsSelected = true;
		}

		// Token: 0x060004FF RID: 1279 RVA: 0x00013014 File Offset: 0x00011214
		public void UpdateProperties()
		{
			this.Machine = this.GetDesiredMachine();
			this._bindHasItem = this.Type == MapSiegePOIVM.POIType.WallSection || this.Machine != null;
			SiegeEvent.SiegeEngineConstructionProgress machine = this.Machine;
			this._bindIsConstructing = machine != null && !machine.IsActive;
			this.RefreshMachineType();
			this.RefreshHitpoints();
			this.RefreshQueueIndex();
		}

		// Token: 0x06000500 RID: 1280 RVA: 0x00013074 File Offset: 0x00011274
		public void RefreshDistanceValue(float newDistance)
		{
			this._bindIsInVisibleRange = newDistance <= 20f;
		}

		// Token: 0x06000501 RID: 1281 RVA: 0x00013088 File Offset: 0x00011288
		public void RefreshPosition()
		{
			this._latestX = 0f;
			this._latestY = 0f;
			this._latestW = 0f;
			MBWindowManager.WorldToScreenInsideUsableArea(this._mapCamera, this._mapSceneLocation, ref this._latestX, ref this._latestY, ref this._latestW);
			this._bindWPos = this._latestW;
			this._bindWSign = (int)this._bindWPos;
			this._bindIsInside = this.IsInsideWindow();
			if (!this._bindIsInside)
			{
				this._bindPosition = new Vec2(-1000f, -1000f);
				return;
			}
			this._bindPosition = new Vec2(this._latestX, this._latestY);
		}

		// Token: 0x06000502 RID: 1282 RVA: 0x00013134 File Offset: 0x00011334
		public void RefreshBinding()
		{
			this.Position = this._bindPosition;
			this.IsInside = this._bindIsInside;
			this.CurrentHitpoints = this._bindCurrentHitpoints;
			this.MaxHitpoints = this._bindMaxHitpoints;
			this.HasItem = this._bindHasItem;
			this.IsConstructing = this._bindIsConstructing;
			this.MachineType = this._bindMachineType;
			this.QueueIndex = this._bindQueueIndex;
			this.IsInVisibleRange = this._bindIsInVisibleRange;
		}

		// Token: 0x06000503 RID: 1283 RVA: 0x000131B0 File Offset: 0x000113B0
		private void RefreshHitpoints()
		{
			if (this.Siege == null)
			{
				this._bindCurrentHitpoints = 0f;
				this._bindMaxHitpoints = 0f;
				return;
			}
			MapSiegePOIVM.POIType type = this.Type;
			if (type == MapSiegePOIVM.POIType.WallSection)
			{
				MBReadOnlyList<float> settlementWallSectionHitPointsRatioList = this.Settlement.SettlementWallSectionHitPointsRatioList;
				this._bindMaxHitpoints = this.Settlement.MaxWallHitPoints / (float)this.Settlement.WallSectionCount;
				this._bindCurrentHitpoints = settlementWallSectionHitPointsRatioList[this.MachineIndex] * this._bindMaxHitpoints;
				this._bindMachineType = ((this._bindCurrentHitpoints <= 0f) ? 1 : 0);
				return;
			}
			if (type - MapSiegePOIVM.POIType.DefenderSiegeMachine > 3)
			{
				return;
			}
			if (this.Machine == null)
			{
				this._bindCurrentHitpoints = 0f;
				this._bindMaxHitpoints = 0f;
				return;
			}
			if (this.Machine.IsActive)
			{
				this._bindCurrentHitpoints = this.Machine.Hitpoints;
				this._bindMaxHitpoints = this.Machine.MaxHitPoints;
				return;
			}
			if (this.Machine.IsBeingRedeployed)
			{
				this._bindCurrentHitpoints = this.Machine.RedeploymentProgress;
				this._bindMaxHitpoints = 1f;
				return;
			}
			this._bindCurrentHitpoints = this.Machine.Progress;
			this._bindMaxHitpoints = 1f;
		}

		// Token: 0x06000504 RID: 1284 RVA: 0x000132E0 File Offset: 0x000114E0
		private void RefreshMachineType()
		{
			if (this.Siege == null)
			{
				this._bindMachineType = -1;
				return;
			}
			MapSiegePOIVM.POIType type = this.Type;
			if (type == MapSiegePOIVM.POIType.WallSection)
			{
				this._bindMachineType = 0;
				return;
			}
			if (type - MapSiegePOIVM.POIType.DefenderSiegeMachine > 3)
			{
				return;
			}
			this._bindMachineType = (int)((this.Machine != null) ? this.GetMachineTypeFromId(this.Machine.SiegeEngine.StringId) : MapSiegePOIVM.MachineTypes.None);
		}

		// Token: 0x06000505 RID: 1285 RVA: 0x00013340 File Offset: 0x00011540
		private void RefreshQueueIndex()
		{
			int bindQueueIndex;
			if (this.Machine == null)
			{
				bindQueueIndex = -1;
			}
			else
			{
				bindQueueIndex = (from e in this.Siege.GetSiegeEventSide(this.PlayerSide).SiegeEngines.DeployedSiegeEngines
					where !e.IsActive
					select e).ToList<SiegeEvent.SiegeEngineConstructionProgress>().IndexOf(this.Machine);
			}
			this._bindQueueIndex = bindQueueIndex;
		}

		// Token: 0x06000506 RID: 1286 RVA: 0x000133B0 File Offset: 0x000115B0
		private bool DetermineIfPOIIsPlayerSide()
		{
			MapSiegePOIVM.POIType type = this.Type;
			if (type > MapSiegePOIVM.POIType.DefenderSiegeMachine)
			{
				return type - MapSiegePOIVM.POIType.AttackerRamSiegeMachine <= 2 && this.PlayerSide == BattleSideEnum.Attacker;
			}
			return this.PlayerSide == BattleSideEnum.Defender;
		}

		// Token: 0x06000507 RID: 1287 RVA: 0x000133E8 File Offset: 0x000115E8
		private bool IsInsideWindow()
		{
			return this._latestX <= Screen.RealScreenResolutionWidth && this._latestY <= Screen.RealScreenResolutionHeight && this._latestX + 200f >= 0f && this._latestY + 100f >= 0f;
		}

		// Token: 0x06000508 RID: 1288 RVA: 0x0001343A File Offset: 0x0001163A
		public void ExecuteShowTooltip()
		{
			InformationManager.ShowTooltip(typeof(List<TooltipProperty>), new object[] { SandBoxUIHelper.GetSiegeEngineInProgressTooltip(this.Machine) });
		}

		// Token: 0x06000509 RID: 1289 RVA: 0x0001345F File Offset: 0x0001165F
		public void ExecuteHideTooltip()
		{
			MBInformationManager.HideInformations();
		}

		// Token: 0x0600050A RID: 1290 RVA: 0x00013468 File Offset: 0x00011668
		private MapSiegePOIVM.MachineTypes GetMachineTypeFromId(string id)
		{
			string text = id.ToLower();
			uint num = <PrivateImplementationDetails>.ComputeStringHash(text);
			if (num > 746114623U)
			{
				if (num <= 1820818168U)
				{
					if (num <= 1241455715U)
					{
						if (num != 808481256U)
						{
							if (num != 1241455715U)
							{
								return MapSiegePOIVM.MachineTypes.None;
							}
							if (!(text == "ram"))
							{
								return MapSiegePOIVM.MachineTypes.None;
							}
							return MapSiegePOIVM.MachineTypes.Ram;
						}
						else if (!(text == "fire_ballista"))
						{
							return MapSiegePOIVM.MachineTypes.None;
						}
					}
					else if (num != 1748194790U)
					{
						if (num != 1820818168U)
						{
							return MapSiegePOIVM.MachineTypes.None;
						}
						if (!(text == "fire_onager"))
						{
							return MapSiegePOIVM.MachineTypes.None;
						}
						return MapSiegePOIVM.MachineTypes.Mangonel;
					}
					else
					{
						if (!(text == "fire_catapult"))
						{
							return MapSiegePOIVM.MachineTypes.None;
						}
						return MapSiegePOIVM.MachineTypes.Mangonel;
					}
				}
				else if (num <= 1898442385U)
				{
					if (num != 1839032341U)
					{
						if (num != 1898442385U)
						{
							return MapSiegePOIVM.MachineTypes.None;
						}
						if (!(text == "catapult"))
						{
							return MapSiegePOIVM.MachineTypes.None;
						}
						return MapSiegePOIVM.MachineTypes.Mangonel;
					}
					else
					{
						if (!(text == "trebuchet"))
						{
							return MapSiegePOIVM.MachineTypes.None;
						}
						return MapSiegePOIVM.MachineTypes.Trebuchet;
					}
				}
				else if (num != 2806198843U)
				{
					if (num != 4036530155U)
					{
						return MapSiegePOIVM.MachineTypes.None;
					}
					if (!(text == "ballista"))
					{
						return MapSiegePOIVM.MachineTypes.None;
					}
				}
				else
				{
					if (!(text == "onager"))
					{
						return MapSiegePOIVM.MachineTypes.None;
					}
					return MapSiegePOIVM.MachineTypes.Mangonel;
				}
				return MapSiegePOIVM.MachineTypes.Ballista;
			}
			if (num > 473034592U)
			{
				if (num <= 712590611U)
				{
					if (num != 695812992U)
					{
						if (num != 712590611U)
						{
							return MapSiegePOIVM.MachineTypes.None;
						}
						if (!(text == "siege_tower_level2"))
						{
							return MapSiegePOIVM.MachineTypes.None;
						}
					}
					else if (!(text == "siege_tower_level3"))
					{
						return MapSiegePOIVM.MachineTypes.None;
					}
				}
				else if (num != 729368230U)
				{
					if (num != 746114623U)
					{
						return MapSiegePOIVM.MachineTypes.None;
					}
					if (!(text == "fire_mangonel"))
					{
						return MapSiegePOIVM.MachineTypes.None;
					}
					return MapSiegePOIVM.MachineTypes.Mangonel;
				}
				else if (!(text == "siege_tower_level1"))
				{
					return MapSiegePOIVM.MachineTypes.None;
				}
				return MapSiegePOIVM.MachineTypes.SiegeTower;
			}
			if (num != 6339497U)
			{
				if (num != 390431385U)
				{
					if (num != 473034592U)
					{
						return MapSiegePOIVM.MachineTypes.None;
					}
					if (!(text == "mangonel"))
					{
						return MapSiegePOIVM.MachineTypes.None;
					}
				}
				else
				{
					if (!(text == "bricole"))
					{
						return MapSiegePOIVM.MachineTypes.None;
					}
					return MapSiegePOIVM.MachineTypes.Trebuchet;
				}
			}
			else
			{
				if (!(text == "ladder"))
				{
					return MapSiegePOIVM.MachineTypes.None;
				}
				return MapSiegePOIVM.MachineTypes.Ladder;
			}
			return MapSiegePOIVM.MachineTypes.Mangonel;
		}

		// Token: 0x0600050B RID: 1291 RVA: 0x0001368C File Offset: 0x0001188C
		private SiegeEvent.SiegeEngineConstructionProgress GetDesiredMachine()
		{
			if (this.Siege != null)
			{
				switch (this.Type)
				{
				case MapSiegePOIVM.POIType.DefenderSiegeMachine:
					return this.Siege.GetSiegeEventSide(BattleSideEnum.Defender).SiegeEngines.DeployedRangedSiegeEngines[this.MachineIndex];
				case MapSiegePOIVM.POIType.AttackerRamSiegeMachine:
				case MapSiegePOIVM.POIType.AttackerTowerSiegeMachine:
					return this.Siege.GetSiegeEventSide(BattleSideEnum.Attacker).SiegeEngines.DeployedMeleeSiegeEngines[this.MachineIndex];
				case MapSiegePOIVM.POIType.AttackerRangedSiegeMachine:
					return this.Siege.GetSiegeEventSide(BattleSideEnum.Attacker).SiegeEngines.DeployedRangedSiegeEngines[this.MachineIndex];
				}
				return null;
			}
			return null;
		}

		// Token: 0x17000179 RID: 377
		// (get) Token: 0x0600050C RID: 1292 RVA: 0x00013721 File Offset: 0x00011921
		// (set) Token: 0x0600050D RID: 1293 RVA: 0x00013729 File Offset: 0x00011929
		public Vec2 Position
		{
			get
			{
				return this._position;
			}
			set
			{
				if (this._position != value)
				{
					this._position = value;
					base.OnPropertyChangedWithValue(value, "Position");
				}
			}
		}

		// Token: 0x1700017A RID: 378
		// (get) Token: 0x0600050E RID: 1294 RVA: 0x0001374C File Offset: 0x0001194C
		// (set) Token: 0x0600050F RID: 1295 RVA: 0x00013754 File Offset: 0x00011954
		public Color SidePrimaryColor
		{
			get
			{
				return this._sidePrimaryColor;
			}
			set
			{
				if (this._sidePrimaryColor != value)
				{
					this._sidePrimaryColor = value;
					base.OnPropertyChangedWithValue(value, "SidePrimaryColor");
				}
			}
		}

		// Token: 0x1700017B RID: 379
		// (get) Token: 0x06000510 RID: 1296 RVA: 0x00013777 File Offset: 0x00011977
		// (set) Token: 0x06000511 RID: 1297 RVA: 0x0001377F File Offset: 0x0001197F
		public Color SideSecondaryColor
		{
			get
			{
				return this._sideSecondaryColor;
			}
			set
			{
				if (this._sideSecondaryColor != value)
				{
					this._sideSecondaryColor = value;
					base.OnPropertyChangedWithValue(value, "SideSecondaryColor");
				}
			}
		}

		// Token: 0x1700017C RID: 380
		// (get) Token: 0x06000512 RID: 1298 RVA: 0x000137A2 File Offset: 0x000119A2
		// (set) Token: 0x06000513 RID: 1299 RVA: 0x000137AA File Offset: 0x000119AA
		public int QueueIndex
		{
			get
			{
				return this._queueIndex;
			}
			set
			{
				if (this._queueIndex != value)
				{
					this._queueIndex = value;
					base.OnPropertyChangedWithValue(value, "QueueIndex");
				}
			}
		}

		// Token: 0x1700017D RID: 381
		// (get) Token: 0x06000514 RID: 1300 RVA: 0x000137C8 File Offset: 0x000119C8
		// (set) Token: 0x06000515 RID: 1301 RVA: 0x000137D0 File Offset: 0x000119D0
		public int MachineType
		{
			get
			{
				return this._machineType;
			}
			set
			{
				if (this._machineType != value)
				{
					this._machineType = value;
					base.OnPropertyChangedWithValue(value, "MachineType");
				}
			}
		}

		// Token: 0x1700017E RID: 382
		// (get) Token: 0x06000516 RID: 1302 RVA: 0x000137EE File Offset: 0x000119EE
		// (set) Token: 0x06000517 RID: 1303 RVA: 0x000137F6 File Offset: 0x000119F6
		public float CurrentHitpoints
		{
			get
			{
				return this._currentHitpoints;
			}
			set
			{
				if (this._currentHitpoints != value)
				{
					this._currentHitpoints = value;
					base.OnPropertyChangedWithValue(value, "CurrentHitpoints");
				}
			}
		}

		// Token: 0x1700017F RID: 383
		// (get) Token: 0x06000518 RID: 1304 RVA: 0x00013814 File Offset: 0x00011A14
		// (set) Token: 0x06000519 RID: 1305 RVA: 0x0001381C File Offset: 0x00011A1C
		public float MaxHitpoints
		{
			get
			{
				return this._maxHitpoints;
			}
			set
			{
				if (this._maxHitpoints != value)
				{
					this._maxHitpoints = value;
					base.OnPropertyChangedWithValue(value, "MaxHitpoints");
				}
			}
		}

		// Token: 0x17000180 RID: 384
		// (get) Token: 0x0600051A RID: 1306 RVA: 0x0001383A File Offset: 0x00011A3A
		// (set) Token: 0x0600051B RID: 1307 RVA: 0x00013842 File Offset: 0x00011A42
		public bool IsPlayerSidePOI
		{
			get
			{
				return this._isPlayerSidePOI;
			}
			set
			{
				if (this._isPlayerSidePOI != value)
				{
					this._isPlayerSidePOI = value;
					base.OnPropertyChangedWithValue(value, "IsPlayerSidePOI");
				}
			}
		}

		// Token: 0x17000181 RID: 385
		// (get) Token: 0x0600051C RID: 1308 RVA: 0x00013860 File Offset: 0x00011A60
		// (set) Token: 0x0600051D RID: 1309 RVA: 0x00013868 File Offset: 0x00011A68
		public bool IsFireVersion
		{
			get
			{
				return this._isFireVersion;
			}
			set
			{
				if (this._isFireVersion != value)
				{
					this._isFireVersion = value;
					base.OnPropertyChangedWithValue(value, "IsFireVersion");
				}
			}
		}

		// Token: 0x17000182 RID: 386
		// (get) Token: 0x0600051E RID: 1310 RVA: 0x00013886 File Offset: 0x00011A86
		// (set) Token: 0x0600051F RID: 1311 RVA: 0x0001388E File Offset: 0x00011A8E
		public bool IsInVisibleRange
		{
			get
			{
				return this._isInVisibleRange;
			}
			set
			{
				if (this._isInVisibleRange != value)
				{
					this._isInVisibleRange = value;
					base.OnPropertyChangedWithValue(value, "IsInVisibleRange");
				}
			}
		}

		// Token: 0x17000183 RID: 387
		// (get) Token: 0x06000520 RID: 1312 RVA: 0x000138AC File Offset: 0x00011AAC
		// (set) Token: 0x06000521 RID: 1313 RVA: 0x000138B4 File Offset: 0x00011AB4
		public bool IsConstructing
		{
			get
			{
				return this._isConstructing;
			}
			set
			{
				if (this._isConstructing != value)
				{
					this._isConstructing = value;
					base.OnPropertyChangedWithValue(value, "IsConstructing");
				}
			}
		}

		// Token: 0x17000184 RID: 388
		// (get) Token: 0x06000522 RID: 1314 RVA: 0x000138D2 File Offset: 0x00011AD2
		// (set) Token: 0x06000523 RID: 1315 RVA: 0x000138DA File Offset: 0x00011ADA
		public bool IsSelected
		{
			get
			{
				return this._isSelected;
			}
			set
			{
				if (this._isSelected != value)
				{
					this._isSelected = value;
					base.OnPropertyChangedWithValue(value, "IsSelected");
				}
			}
		}

		// Token: 0x17000185 RID: 389
		// (get) Token: 0x06000524 RID: 1316 RVA: 0x000138F8 File Offset: 0x00011AF8
		// (set) Token: 0x06000525 RID: 1317 RVA: 0x00013900 File Offset: 0x00011B00
		public bool HasItem
		{
			get
			{
				return this._hasItem;
			}
			set
			{
				if (this._hasItem != value)
				{
					this._hasItem = value;
					base.OnPropertyChangedWithValue(value, "HasItem");
				}
			}
		}

		// Token: 0x17000186 RID: 390
		// (get) Token: 0x06000526 RID: 1318 RVA: 0x0001391E File Offset: 0x00011B1E
		// (set) Token: 0x06000527 RID: 1319 RVA: 0x00013926 File Offset: 0x00011B26
		public bool IsInside
		{
			get
			{
				return this._isInside;
			}
			set
			{
				if (this._isInside != value)
				{
					this._isInside = value;
					base.OnPropertyChangedWithValue(value, "IsInside");
				}
			}
		}

		// Token: 0x04000275 RID: 629
		private readonly Vec3 _mapSceneLocation;

		// Token: 0x04000276 RID: 630
		private readonly Camera _mapCamera;

		// Token: 0x04000277 RID: 631
		private readonly BattleSideEnum _thisSide;

		// Token: 0x04000278 RID: 632
		private readonly Action<MapSiegePOIVM> _onSelection;

		// Token: 0x04000279 RID: 633
		private float _latestX;

		// Token: 0x0400027A RID: 634
		private float _latestY;

		// Token: 0x0400027B RID: 635
		private float _latestW;

		// Token: 0x0400027C RID: 636
		private float _bindCurrentHitpoints;

		// Token: 0x0400027D RID: 637
		private float _bindMaxHitpoints;

		// Token: 0x0400027E RID: 638
		private float _bindWPos;

		// Token: 0x0400027F RID: 639
		private int _bindWSign;

		// Token: 0x04000280 RID: 640
		private int _bindMachineType = -1;

		// Token: 0x04000281 RID: 641
		private int _bindQueueIndex;

		// Token: 0x04000282 RID: 642
		private bool _bindIsInside;

		// Token: 0x04000283 RID: 643
		private bool _bindHasItem;

		// Token: 0x04000284 RID: 644
		private bool _bindIsConstructing;

		// Token: 0x04000285 RID: 645
		private Vec2 _bindPosition;

		// Token: 0x04000286 RID: 646
		private bool _bindIsInVisibleRange;

		// Token: 0x04000287 RID: 647
		private Color _sidePrimaryColor;

		// Token: 0x04000288 RID: 648
		private Color _sideSecondaryColor;

		// Token: 0x04000289 RID: 649
		private Vec2 _position;

		// Token: 0x0400028A RID: 650
		private float _currentHitpoints;

		// Token: 0x0400028B RID: 651
		private int _machineType = -1;

		// Token: 0x0400028C RID: 652
		private float _maxHitpoints;

		// Token: 0x0400028D RID: 653
		private int _queueIndex;

		// Token: 0x0400028E RID: 654
		private bool _isInside;

		// Token: 0x0400028F RID: 655
		private bool _hasItem;

		// Token: 0x04000290 RID: 656
		private bool _isConstructing;

		// Token: 0x04000291 RID: 657
		private bool _isPlayerSidePOI;

		// Token: 0x04000292 RID: 658
		private bool _isFireVersion;

		// Token: 0x04000293 RID: 659
		private bool _isInVisibleRange;

		// Token: 0x04000294 RID: 660
		private bool _isSelected;

		// Token: 0x020000A8 RID: 168
		public enum POIType
		{
			// Token: 0x040003C7 RID: 967
			WallSection,
			// Token: 0x040003C8 RID: 968
			DefenderSiegeMachine,
			// Token: 0x040003C9 RID: 969
			AttackerRamSiegeMachine,
			// Token: 0x040003CA RID: 970
			AttackerTowerSiegeMachine,
			// Token: 0x040003CB RID: 971
			AttackerRangedSiegeMachine
		}

		// Token: 0x020000A9 RID: 169
		public enum MachineTypes
		{
			// Token: 0x040003CD RID: 973
			None = -1,
			// Token: 0x040003CE RID: 974
			Wall,
			// Token: 0x040003CF RID: 975
			BrokenWall,
			// Token: 0x040003D0 RID: 976
			Ballista,
			// Token: 0x040003D1 RID: 977
			Trebuchet,
			// Token: 0x040003D2 RID: 978
			Ladder,
			// Token: 0x040003D3 RID: 979
			Ram,
			// Token: 0x040003D4 RID: 980
			SiegeTower,
			// Token: 0x040003D5 RID: 981
			Mangonel
		}
	}
}
