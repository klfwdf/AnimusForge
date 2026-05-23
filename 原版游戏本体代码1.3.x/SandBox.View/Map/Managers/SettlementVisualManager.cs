using System;
using System.Collections.Generic;
using SandBox.View.Map.Visuals;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Map;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.CampaignSystem.Siege;
using TaleWorlds.Core;
using TaleWorlds.Engine;
using TaleWorlds.Library;
using TaleWorlds.ScreenSystem;

namespace SandBox.View.Map.Managers
{
	// Token: 0x02000076 RID: 118
	public class SettlementVisualManager : EntityVisualManagerBase<PartyBase>
	{
		// Token: 0x170000AD RID: 173
		// (get) Token: 0x06000526 RID: 1318 RVA: 0x0002702B File Offset: 0x0002522B
		public override int Priority
		{
			get
			{
				return 4;
			}
		}

		// Token: 0x170000AE RID: 174
		// (get) Token: 0x06000527 RID: 1319 RVA: 0x0002702E File Offset: 0x0002522E
		public static SettlementVisualManager Current
		{
			get
			{
				return SandBoxViewSubModule.SandBoxViewVisualManager.GetEntityComponent<SettlementVisualManager>();
			}
		}

		// Token: 0x06000528 RID: 1320 RVA: 0x0002703C File Offset: 0x0002523C
		public override void OnTick(float realDt, float dt)
		{
			this._dirtyPartyVisualCount = -1;
			TWParallel.For(0, this._visualsFlattened.Count, delegate(int startInclusive, int endExclusive)
			{
				for (int j = startInclusive; j < endExclusive; j++)
				{
					this._visualsFlattened[j].Tick(dt, ref this._dirtyPartyVisualCount, ref this._dirtyPartiesList);
				}
			}, 16);
			for (int i = 0; i < this._dirtyPartyVisualCount + 1; i++)
			{
				this._dirtyPartiesList[i].ValidateIsDirty();
			}
		}

		// Token: 0x06000529 RID: 1321 RVA: 0x000270A4 File Offset: 0x000252A4
		public override bool OnVisualIntersected(Ray mouseRay, UIntPtr[] intersectedEntityIDs, Intersection[] intersectionInfos, int entityCount, Vec3 worldMouseNear, Vec3 worldMouseFar, Vec3 terrainIntersectionPoint, ref MapEntityVisual hoveredVisual, ref MapEntityVisual selectedVisual)
		{
			bool flag = false;
			for (int i = entityCount - 1; i >= 0; i--)
			{
				UIntPtr uintPtr = intersectedEntityIDs[i];
				if (uintPtr != UIntPtr.Zero)
				{
					MapEntityVisual mapEntityVisual;
					if (MapScreen.VisualsOfEntities.TryGetValue(uintPtr, out mapEntityVisual) && mapEntityVisual is SettlementVisual && mapEntityVisual.IsVisibleOrFadingOut())
					{
						if (hoveredVisual == null)
						{
							hoveredVisual = mapEntityVisual;
						}
						selectedVisual = mapEntityVisual;
					}
					if (PlayerSiege.PlayerSiegeEvent != null && ScreenManager.FirstHitLayer == MapScreen.Instance.SceneLayer && MapScreen.FrameAndVisualOfEngines.ContainsKey(uintPtr))
					{
						flag = true;
						this.HandleSiegeEngineHover(uintPtr);
					}
				}
			}
			if (!flag)
			{
				this.HandleSiegeEngineHoverEnd();
			}
			return selectedVisual != null;
		}

		// Token: 0x0600052A RID: 1322 RVA: 0x0002713C File Offset: 0x0002533C
		public override void OnFrameTick(float dt)
		{
			this.RefreshMapSiegeOverlayRequired();
			if (PlayerSiege.PlayerSiegeEvent != null && this._playerSiegeMachineSlotMeshesAdded)
			{
				this.TickSiegeMachineCircles();
			}
			if (GameStateManager.Current.ActiveStateDisabledByUser)
			{
				this.HandleSiegeEngineHoverEnd();
			}
			this._timeSinceCreation += dt;
		}

		// Token: 0x0600052B RID: 1323 RVA: 0x0002717C File Offset: 0x0002537C
		public override bool OnMouseClick(MapEntityVisual visualOfSelectedEntity, Vec3 intersectionPoint, PathFaceRecord mouseOverFaceIndex, bool isDoubleClick)
		{
			bool result = false;
			if (MapScreen.Instance.MapState.AtMenu && this._hoveredSiegeEntityID != UIntPtr.Zero)
			{
				Tuple<MatrixFrame, SettlementVisual> tuple = MapScreen.FrameAndVisualOfEngines[this._hoveredSiegeEntityID];
				MapScreen.Instance.OnSiegeEngineFrameClick(tuple.Item1);
				result = true;
			}
			return result;
		}

		// Token: 0x0600052C RID: 1324 RVA: 0x000271D4 File Offset: 0x000253D4
		public override MapEntityVisual<PartyBase> GetVisualOfEntity(PartyBase partyBase)
		{
			SettlementVisual result;
			this._settlementVisuals.TryGetValue(partyBase, out result);
			return result;
		}

		// Token: 0x0600052D RID: 1325 RVA: 0x000271F1 File Offset: 0x000253F1
		public SettlementVisual GetSettlementVisual(Settlement settlement)
		{
			return this._settlementVisuals[settlement.Party];
		}

		// Token: 0x0600052E RID: 1326 RVA: 0x00027204 File Offset: 0x00025404
		protected override void OnInitialize()
		{
			base.OnInitialize();
			foreach (Settlement settlement in Settlement.All)
			{
				this.AddNewPartyVisualForParty(settlement.Party);
			}
			IMapScene mapSceneWrapper = Campaign.Current.MapSceneWrapper;
		}

		// Token: 0x0600052F RID: 1327 RVA: 0x0002726C File Offset: 0x0002546C
		protected override void OnFinalize()
		{
			foreach (SettlementVisual settlementVisual in this._settlementVisuals.Values)
			{
				settlementVisual.ReleaseResources();
			}
			CampaignEventDispatcher.Instance.RemoveListeners(this);
		}

		// Token: 0x06000530 RID: 1328 RVA: 0x000272CC File Offset: 0x000254CC
		private void TickSiegeMachineCircles()
		{
			SiegeEvent playerSiegeEvent = PlayerSiege.PlayerSiegeEvent;
			bool isPlayerLeader = playerSiegeEvent != null && playerSiegeEvent.IsPlayerSiegeEvent && Campaign.Current.Models.EncounterModel.GetLeaderOfSiegeEvent(playerSiegeEvent, PlayerSiege.PlayerSide) == Hero.MainHero;
			Settlement besiegedSettlement = playerSiegeEvent.BesiegedSettlement;
			SettlementVisual settlementVisual = this.GetSettlementVisual(besiegedSettlement);
			Tuple<MatrixFrame, SettlementVisual> tuple = null;
			if (this._hoveredSiegeEntityID != UIntPtr.Zero)
			{
				tuple = MapScreen.FrameAndVisualOfEngines[this._hoveredSiegeEntityID];
			}
			for (int i = 0; i < settlementVisual.GetDefenderRangedSiegeEngineFrames().Length; i++)
			{
				bool isEmpty = playerSiegeEvent.GetSiegeEventSide(BattleSideEnum.Defender).SiegeEngines.DeployedRangedSiegeEngines[i] == null;
				bool isEnemy = PlayerSiege.PlayerSide > BattleSideEnum.Defender;
				string desiredMaterialName = this.GetDesiredMaterialName(true, false, false);
				Decal decal = this._defenderMachinesCircleEntities[i].GetComponentAtIndex(0, GameEntity.ComponentType.Decal) as Decal;
				Material material = decal.GetMaterial();
				if (((material != null) ? material.Name : null) != desiredMaterialName)
				{
					decal.SetMaterial(Material.GetFromResource(desiredMaterialName));
				}
				bool isHovered = tuple != null && this._defenderMachinesCircleEntities[i].GetGlobalFrame().NearlyEquals(tuple.Item1, 1E-05f);
				uint desiredDecalColor = this.GetDesiredDecalColor(isHovered, isEnemy, isEmpty, isPlayerLeader);
				if (desiredDecalColor != decal.GetFactor1())
				{
					decal.SetFactor1(desiredDecalColor);
				}
			}
			for (int j = 0; j < settlementVisual.GetAttackerRangedSiegeEngineFrames().Length; j++)
			{
				bool isEmpty2 = playerSiegeEvent.GetSiegeEventSide(BattleSideEnum.Attacker).SiegeEngines.DeployedRangedSiegeEngines[j] == null;
				bool isEnemy2 = PlayerSiege.PlayerSide != BattleSideEnum.Attacker;
				string desiredMaterialName2 = this.GetDesiredMaterialName(true, true, false);
				Decal decal2 = this._attackerRangedMachinesCircleEntities[j].GetComponentAtIndex(0, GameEntity.ComponentType.Decal) as Decal;
				Material material2 = decal2.GetMaterial();
				if (((material2 != null) ? material2.Name : null) != desiredMaterialName2)
				{
					decal2.SetMaterial(Material.GetFromResource(desiredMaterialName2));
				}
				bool isHovered2 = tuple != null && this._attackerRangedMachinesCircleEntities[j].GetGlobalFrame().NearlyEquals(tuple.Item1, 1E-05f);
				uint desiredDecalColor2 = this.GetDesiredDecalColor(isHovered2, isEnemy2, isEmpty2, isPlayerLeader);
				if (desiredDecalColor2 != decal2.GetFactor1())
				{
					decal2.SetFactor1(desiredDecalColor2);
				}
			}
			for (int k = 0; k < settlementVisual.GetAttackerBatteringRamSiegeEngineFrames().Length; k++)
			{
				bool isEmpty3 = playerSiegeEvent.GetSiegeEventSide(BattleSideEnum.Attacker).SiegeEngines.DeployedMeleeSiegeEngines[k] == null;
				bool isEnemy3 = PlayerSiege.PlayerSide != BattleSideEnum.Attacker;
				string desiredMaterialName3 = this.GetDesiredMaterialName(false, true, false);
				Decal decal3 = this._attackerRamMachinesCircleEntities[k].GetComponentAtIndex(0, GameEntity.ComponentType.Decal) as Decal;
				Material material3 = decal3.GetMaterial();
				if (((material3 != null) ? material3.Name : null) != desiredMaterialName3)
				{
					decal3.SetMaterial(Material.GetFromResource(desiredMaterialName3));
				}
				bool isHovered3 = tuple != null && this._attackerRamMachinesCircleEntities[k].GetGlobalFrame().NearlyEquals(tuple.Item1, 1E-05f);
				uint desiredDecalColor3 = this.GetDesiredDecalColor(isHovered3, isEnemy3, isEmpty3, isPlayerLeader);
				if (desiredDecalColor3 != decal3.GetFactor1())
				{
					decal3.SetFactor1(desiredDecalColor3);
				}
			}
			for (int l = 0; l < settlementVisual.GetAttackerTowerSiegeEngineFrames().Length; l++)
			{
				bool isEmpty4 = playerSiegeEvent.GetSiegeEventSide(BattleSideEnum.Attacker).SiegeEngines.DeployedMeleeSiegeEngines[settlementVisual.GetAttackerBatteringRamSiegeEngineFrames().Length + l] == null;
				bool isEnemy4 = PlayerSiege.PlayerSide != BattleSideEnum.Attacker;
				string desiredMaterialName4 = this.GetDesiredMaterialName(false, true, true);
				Decal decal4 = this._attackerTowerMachinesCircleEntities[l].GetComponentAtIndex(0, GameEntity.ComponentType.Decal) as Decal;
				Material material4 = decal4.GetMaterial();
				if (((material4 != null) ? material4.Name : null) != desiredMaterialName4)
				{
					decal4.SetMaterial(Material.GetFromResource(desiredMaterialName4));
				}
				bool isHovered4 = tuple != null && this._attackerTowerMachinesCircleEntities[l].GetGlobalFrame().NearlyEquals(tuple.Item1, 1E-05f);
				uint desiredDecalColor4 = this.GetDesiredDecalColor(isHovered4, isEnemy4, isEmpty4, isPlayerLeader);
				if (desiredDecalColor4 != decal4.GetFactor1())
				{
					decal4.SetFactor1(desiredDecalColor4);
				}
			}
		}

		// Token: 0x06000531 RID: 1329 RVA: 0x000276C4 File Offset: 0x000258C4
		private void AddNewPartyVisualForParty(PartyBase partyBase)
		{
			SettlementVisual settlementVisual = new SettlementVisual(partyBase);
			settlementVisual.OnStartup();
			this._settlementVisuals.Add(partyBase, settlementVisual);
			this._visualsFlattened.Add(settlementVisual);
		}

		// Token: 0x06000532 RID: 1330 RVA: 0x000276F8 File Offset: 0x000258F8
		private uint GetDesiredDecalColor(bool isHovered, bool isEnemy, bool isEmpty, bool isPlayerLeader)
		{
			if (isEnemy)
			{
				return 4287064638U;
			}
			if (isHovered && isPlayerLeader)
			{
				return 4293956364U;
			}
			if (!isEmpty)
			{
				return 4283683126U;
			}
			if (isPlayerLeader)
			{
				float ratio = MathF.PingPong(0f, 0.5f, this._timeSinceCreation) / 0.5f;
				Color start = Color.FromUint(4278394186U);
				Color end = Color.FromUint(4284320212U);
				return Color.Lerp(start, end, ratio).ToUnsignedInteger();
			}
			return 4278394186U;
		}

		// Token: 0x06000533 RID: 1331 RVA: 0x0002776D File Offset: 0x0002596D
		private string GetDesiredMaterialName(bool isRanged, bool isAttacker, bool isTower)
		{
			if (isRanged)
			{
				if (!isAttacker)
				{
					return "decal_defender_ranged_siege";
				}
				return "decal_siege_ranged";
			}
			else
			{
				if (!isTower)
				{
					return "decal_siege_ram";
				}
				return "decal_siege_tower";
			}
		}

		// Token: 0x06000534 RID: 1332 RVA: 0x00027790 File Offset: 0x00025990
		private void RemoveSiegeCircleVisuals()
		{
			if (this._playerSiegeMachineSlotMeshesAdded)
			{
				MapScene mapScene = Campaign.Current.MapSceneWrapper as MapScene;
				for (int i = 0; i < this._defenderMachinesCircleEntities.Length; i++)
				{
					this._defenderMachinesCircleEntities[i].SetVisibilityExcludeParents(false);
					mapScene.Scene.RemoveEntity(this._defenderMachinesCircleEntities[i], 107);
					this._defenderMachinesCircleEntities[i] = null;
				}
				for (int j = 0; j < this._attackerRamMachinesCircleEntities.Length; j++)
				{
					this._attackerRamMachinesCircleEntities[j].SetVisibilityExcludeParents(false);
					mapScene.Scene.RemoveEntity(this._attackerRamMachinesCircleEntities[j], 108);
					this._attackerRamMachinesCircleEntities[j] = null;
				}
				for (int k = 0; k < this._attackerTowerMachinesCircleEntities.Length; k++)
				{
					this._attackerTowerMachinesCircleEntities[k].SetVisibilityExcludeParents(false);
					mapScene.Scene.RemoveEntity(this._attackerTowerMachinesCircleEntities[k], 109);
					this._attackerTowerMachinesCircleEntities[k] = null;
				}
				for (int l = 0; l < this._attackerRangedMachinesCircleEntities.Length; l++)
				{
					this._attackerRangedMachinesCircleEntities[l].SetVisibilityExcludeParents(false);
					mapScene.Scene.RemoveEntity(this._attackerRangedMachinesCircleEntities[l], 110);
					this._attackerRangedMachinesCircleEntities[l] = null;
				}
				this._playerSiegeMachineSlotMeshesAdded = false;
			}
		}

		// Token: 0x06000535 RID: 1333 RVA: 0x000278C4 File Offset: 0x00025AC4
		private void RefreshMapSiegeOverlayRequired()
		{
			MapScreen.Instance.MapCameraView.OnRefreshMapSiegeOverlayRequired(this._mapSiegeOverlayView == null);
			if (this._playerSiegeMachineSlotMeshesAdded && PlayerSiege.PlayerSiegeEvent != null)
			{
				Settlement besiegedSettlement = PlayerSiege.PlayerSiegeEvent.BesiegedSettlement;
				if (besiegedSettlement != null && besiegedSettlement.CurrentSiegeState == Settlement.SiegeState.InTheLordsHall)
				{
					this.RemoveSiegeCircleVisuals();
					this._playerSiegeMachineSlotMeshesAdded = false;
					return;
				}
			}
			if (PlayerSiege.PlayerSiegeEvent == null && this._mapSiegeOverlayView != null)
			{
				MapScreen.Instance.RemoveMapView(this._mapSiegeOverlayView);
				this._mapSiegeOverlayView = null;
				if (this._playerSiegeMachineSlotMeshesAdded)
				{
					this.RemoveSiegeCircleVisuals();
					this._playerSiegeMachineSlotMeshesAdded = false;
					return;
				}
			}
			else if (PlayerSiege.PlayerSiegeEvent != null && this._mapSiegeOverlayView == null)
			{
				this._mapSiegeOverlayView = MapScreen.Instance.AddMapView<MapSiegeOverlayView>(Array.Empty<object>());
				if (!this._playerSiegeMachineSlotMeshesAdded)
				{
					this.InitializeSiegeCircleVisuals();
					this._playerSiegeMachineSlotMeshesAdded = true;
				}
			}
		}

		// Token: 0x06000536 RID: 1334 RVA: 0x00027998 File Offset: 0x00025B98
		private void InitializeSiegeCircleVisuals()
		{
			Settlement besiegedSettlement = PlayerSiege.PlayerSiegeEvent.BesiegedSettlement;
			SettlementVisual settlementVisual = this.GetSettlementVisual(besiegedSettlement);
			MapScene mapScene = Campaign.Current.MapSceneWrapper as MapScene;
			MatrixFrame[] array = settlementVisual.GetDefenderRangedSiegeEngineFrames();
			this._defenderMachinesCircleEntities = new GameEntity[array.Length];
			for (int i = 0; i < array.Length; i++)
			{
				MatrixFrame matrixFrame = array[i];
				this._defenderMachinesCircleEntities[i] = GameEntity.CreateEmpty(mapScene.Scene, true, true, true);
				this._defenderMachinesCircleEntities[i].Name = "dRangedMachineCircle_" + i;
				Decal decal = Decal.CreateDecal(null);
				decal.SetMaterial(Material.GetFromResource("decal_defender_ranged_siege"));
				decal.SetFactor1Linear(4287064638U);
				this._defenderMachinesCircleEntities[i].AddComponent(decal);
				MatrixFrame matrixFrame2 = matrixFrame;
				if (this._isNewDecalScaleImplementationEnabled)
				{
					Vec3 vec = new Vec3(0.25f, 0.25f, 0.25f, -1f);
					matrixFrame2.Scale(vec);
				}
				this._defenderMachinesCircleEntities[i].SetGlobalFrame(matrixFrame2, true);
				this._defenderMachinesCircleEntities[i].SetVisibilityExcludeParents(true);
				mapScene.Scene.AddDecalInstance(decal, "editor_set", true);
			}
			array = settlementVisual.GetAttackerBatteringRamSiegeEngineFrames();
			this._attackerRamMachinesCircleEntities = new GameEntity[array.Length];
			for (int j = 0; j < array.Length; j++)
			{
				MatrixFrame matrixFrame3 = array[j];
				this._attackerRamMachinesCircleEntities[j] = GameEntity.CreateEmpty(mapScene.Scene, true, true, true);
				this._attackerRamMachinesCircleEntities[j].Name = "InitializeSiegeCircleVisuals";
				this._attackerRamMachinesCircleEntities[j].Name = "aRamMachineCircle_" + j;
				Decal decal2 = Decal.CreateDecal(null);
				decal2.SetMaterial(Material.GetFromResource("decal_siege_ram"));
				decal2.SetFactor1Linear(4287064638U);
				this._attackerRamMachinesCircleEntities[j].AddComponent(decal2);
				MatrixFrame matrixFrame4 = matrixFrame3;
				if (this._isNewDecalScaleImplementationEnabled)
				{
					Vec3 vec = new Vec3(0.38f, 0.38f, 0.38f, -1f);
					matrixFrame4.Scale(vec);
				}
				this._attackerRamMachinesCircleEntities[j].SetGlobalFrame(matrixFrame4, true);
				this._attackerRamMachinesCircleEntities[j].SetVisibilityExcludeParents(true);
				mapScene.Scene.AddDecalInstance(decal2, "editor_set", true);
			}
			array = settlementVisual.GetAttackerTowerSiegeEngineFrames();
			this._attackerTowerMachinesCircleEntities = new GameEntity[array.Length];
			for (int k = 0; k < array.Length; k++)
			{
				MatrixFrame matrixFrame5 = array[k];
				this._attackerTowerMachinesCircleEntities[k] = GameEntity.CreateEmpty(mapScene.Scene, true, true, true);
				this._attackerTowerMachinesCircleEntities[k].Name = "aTowerMachineCircle_" + k;
				Decal decal3 = Decal.CreateDecal(null);
				decal3.SetMaterial(Material.GetFromResource("decal_siege_tower"));
				decal3.SetFactor1Linear(4287064638U);
				this._attackerTowerMachinesCircleEntities[k].AddComponent(decal3);
				MatrixFrame matrixFrame6 = matrixFrame5;
				if (this._isNewDecalScaleImplementationEnabled)
				{
					Vec3 vec = new Vec3(0.38f, 0.38f, 0.38f, -1f);
					matrixFrame6.Scale(vec);
				}
				this._attackerTowerMachinesCircleEntities[k].SetGlobalFrame(matrixFrame6, true);
				this._attackerTowerMachinesCircleEntities[k].SetVisibilityExcludeParents(true);
				mapScene.Scene.AddDecalInstance(decal3, "editor_set", true);
			}
			array = settlementVisual.GetAttackerRangedSiegeEngineFrames();
			this._attackerRangedMachinesCircleEntities = new GameEntity[array.Length];
			for (int l = 0; l < array.Length; l++)
			{
				MatrixFrame matrixFrame7 = array[l];
				this._attackerRangedMachinesCircleEntities[l] = GameEntity.CreateEmpty(mapScene.Scene, true, true, true);
				this._attackerRangedMachinesCircleEntities[l].Name = "aRangedMachineCircle_" + l;
				Decal decal4 = Decal.CreateDecal(null);
				decal4.SetMaterial(Material.GetFromResource("decal_siege_ranged"));
				decal4.SetFactor1Linear(4287064638U);
				this._attackerRangedMachinesCircleEntities[l].AddComponent(decal4);
				MatrixFrame matrixFrame8 = matrixFrame7;
				if (this._isNewDecalScaleImplementationEnabled)
				{
					Vec3 vec = new Vec3(0.38f, 0.38f, 0.38f, -1f);
					matrixFrame8.Scale(vec);
				}
				this._attackerRangedMachinesCircleEntities[l].SetGlobalFrame(matrixFrame8, true);
				this._attackerRangedMachinesCircleEntities[l].SetVisibilityExcludeParents(true);
				mapScene.Scene.AddDecalInstance(decal4, "editor_set", true);
			}
		}

		// Token: 0x06000537 RID: 1335 RVA: 0x00027DE0 File Offset: 0x00025FE0
		private void HandleSiegeEngineHover(UIntPtr newID)
		{
			if (this._hoveredSiegeEntityID != newID)
			{
				this._hoveredSiegeEntityID = newID;
				Tuple<MatrixFrame, SettlementVisual> tuple = MapScreen.FrameAndVisualOfEngines[this._hoveredSiegeEntityID];
				tuple.Item2.OnMapHoverSiegeEngine(tuple.Item1);
			}
		}

		// Token: 0x06000538 RID: 1336 RVA: 0x00027E24 File Offset: 0x00026024
		private void HandleSiegeEngineHoverEnd()
		{
			if (this._hoveredSiegeEntityID != UIntPtr.Zero)
			{
				MapScreen.FrameAndVisualOfEngines[this._hoveredSiegeEntityID].Item2.OnMapHoverSiegeEngineEnd();
				this._hoveredSiegeEntityID = UIntPtr.Zero;
			}
		}

		// Token: 0x04000254 RID: 596
		private const string _emptyAttackerRangedDecalMaterialName = "decal_siege_ranged";

		// Token: 0x04000255 RID: 597
		private const string _attackerRamMachineDecalMaterialName = "decal_siege_ram";

		// Token: 0x04000256 RID: 598
		private const string _attackerTowerMachineDecalMaterialName = "decal_siege_tower";

		// Token: 0x04000257 RID: 599
		private const string _attackerRangedMachineDecalMaterialName = "decal_siege_ranged";

		// Token: 0x04000258 RID: 600
		private const string _defenderRangedMachineDecalMaterialName = "decal_defender_ranged_siege";

		// Token: 0x04000259 RID: 601
		private const uint _preperationOrEnemySiegeEngineDecalColor = 4287064638U;

		// Token: 0x0400025A RID: 602
		private const uint _normalStartSiegeEngineDecalColor = 4278394186U;

		// Token: 0x0400025B RID: 603
		private const float _defenderMachineCircleDecalScale = 0.25f;

		// Token: 0x0400025C RID: 604
		private const float _attackerMachineDecalScale = 0.38f;

		// Token: 0x0400025D RID: 605
		private bool _isNewDecalScaleImplementationEnabled;

		// Token: 0x0400025E RID: 606
		private const uint _normalEndSiegeEngineDecalColor = 4284320212U;

		// Token: 0x0400025F RID: 607
		private const uint _hoveredSiegeEngineDecalColor = 4293956364U;

		// Token: 0x04000260 RID: 608
		private const uint _withMachineSiegeEngineDecalColor = 4283683126U;

		// Token: 0x04000261 RID: 609
		private const float _machineDecalAnimLoopTime = 0.5f;

		// Token: 0x04000262 RID: 610
		private readonly Dictionary<PartyBase, SettlementVisual> _settlementVisuals = new Dictionary<PartyBase, SettlementVisual>();

		// Token: 0x04000263 RID: 611
		private readonly List<SettlementVisual> _visualsFlattened = new List<SettlementVisual>();

		// Token: 0x04000264 RID: 612
		private int _dirtyPartyVisualCount;

		// Token: 0x04000265 RID: 613
		private SettlementVisual[] _dirtyPartiesList = new SettlementVisual[2500];

		// Token: 0x04000266 RID: 614
		private UIntPtr _hoveredSiegeEntityID;

		// Token: 0x04000267 RID: 615
		private bool _playerSiegeMachineSlotMeshesAdded;

		// Token: 0x04000268 RID: 616
		private MapView _mapSiegeOverlayView;

		// Token: 0x04000269 RID: 617
		private GameEntity[] _defenderMachinesCircleEntities;

		// Token: 0x0400026A RID: 618
		private GameEntity[] _attackerRamMachinesCircleEntities;

		// Token: 0x0400026B RID: 619
		private GameEntity[] _attackerTowerMachinesCircleEntities;

		// Token: 0x0400026C RID: 620
		private GameEntity[] _attackerRangedMachinesCircleEntities;

		// Token: 0x0400026D RID: 621
		private float _timeSinceCreation;
	}
}
