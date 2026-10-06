using System;
using System.Collections.Generic;
using SandBox.View.Map.Visuals;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.CampaignSystem.Siege;
using TaleWorlds.Core;
using TaleWorlds.Engine;
using TaleWorlds.Library;
using TaleWorlds.ScreenSystem;

namespace SandBox.View.Map.Managers;

public class SettlementVisualManager : EntityVisualManagerBase<PartyBase>
{
	private const string _emptyAttackerRangedDecalMaterialName = "decal_siege_ranged";

	private const string _attackerRamMachineDecalMaterialName = "decal_siege_ram";

	private const string _attackerTowerMachineDecalMaterialName = "decal_siege_tower";

	private const string _attackerRangedMachineDecalMaterialName = "decal_siege_ranged";

	private const string _defenderRangedMachineDecalMaterialName = "decal_defender_ranged_siege";

	private const uint _preperationOrEnemySiegeEngineDecalColor = 4287064638u;

	private const uint _normalStartSiegeEngineDecalColor = 4278394186u;

	private const float _defenderMachineCircleDecalScale = 0.25f;

	private const float _attackerMachineDecalScale = 0.38f;

	private bool _isNewDecalScaleImplementationEnabled;

	private const uint _normalEndSiegeEngineDecalColor = 4284320212u;

	private const uint _hoveredSiegeEngineDecalColor = 4293956364u;

	private const uint _withMachineSiegeEngineDecalColor = 4283683126u;

	private const float _machineDecalAnimLoopTime = 0.5f;

	private readonly Dictionary<PartyBase, SettlementVisual> _settlementVisuals = new Dictionary<PartyBase, SettlementVisual>();

	private readonly List<SettlementVisual> _visualsFlattened = new List<SettlementVisual>();

	private int _dirtyPartyVisualCount;

	private SettlementVisual[] _dirtyPartiesList = new SettlementVisual[2500];

	private UIntPtr _hoveredSiegeEntityID;

	private bool _playerSiegeMachineSlotMeshesAdded;

	private MapView _mapSiegeOverlayView;

	private GameEntity[] _defenderMachinesCircleEntities;

	private GameEntity[] _attackerRamMachinesCircleEntities;

	private GameEntity[] _attackerTowerMachinesCircleEntities;

	private GameEntity[] _attackerRangedMachinesCircleEntities;

	private float _timeSinceCreation;

	public override int Priority => 40;

	public static SettlementVisualManager Current => SandBoxViewSubModule.SandBoxViewVisualManager.GetEntityComponent<SettlementVisualManager>();

	public override void OnTick(float realDt, float dt)
	{
		_dirtyPartyVisualCount = -1;
		TWParallel.For(0, _visualsFlattened.Count, delegate(int startInclusive, int endExclusive)
		{
			for (int i = startInclusive; i < endExclusive; i++)
			{
				_visualsFlattened[i].Tick(dt, ref _dirtyPartyVisualCount, ref _dirtyPartiesList);
			}
		});
		for (int num = 0; num < _dirtyPartyVisualCount + 1; num++)
		{
			_dirtyPartiesList[num].ValidateIsDirty();
		}
	}

	public override bool OnVisualIntersected(Ray mouseRay, UIntPtr[] intersectedEntityIDs, Intersection[] intersectionInfos, int entityCount, Vec3 worldMouseNear, Vec3 worldMouseFar, Vec3 terrainIntersectionPoint, ref MapEntityVisual hoveredVisual, ref MapEntityVisual selectedVisual)
	{
		bool flag = false;
		for (int num = entityCount - 1; num >= 0; num--)
		{
			UIntPtr uIntPtr = intersectedEntityIDs[num];
			if (uIntPtr != UIntPtr.Zero)
			{
				if (MapScreen.VisualsOfEntities.TryGetValue(uIntPtr, out var value) && value is SettlementVisual && value.IsVisibleOrFadingOut())
				{
					if (hoveredVisual == null)
					{
						hoveredVisual = value;
					}
					selectedVisual = value;
				}
				if (PlayerSiege.PlayerSiegeEvent != null && ScreenManager.FirstHitLayer == MapScreen.Instance.SceneLayer && MapScreen.FrameAndVisualOfEngines.ContainsKey(uIntPtr))
				{
					flag = true;
					HandleSiegeEngineHover(uIntPtr);
				}
			}
		}
		if (!flag)
		{
			HandleSiegeEngineHoverEnd();
		}
		return selectedVisual != null;
	}

	public override void OnFrameTick(float dt)
	{
		RefreshMapSiegeOverlayRequired();
		if (PlayerSiege.PlayerSiegeEvent != null && _playerSiegeMachineSlotMeshesAdded)
		{
			TickSiegeMachineCircles();
		}
		if (GameStateManager.Current.ActiveStateDisabledByUser)
		{
			HandleSiegeEngineHoverEnd();
		}
		_timeSinceCreation += dt;
	}

	public override bool OnMouseClick(MapEntityVisual visualOfSelectedEntity, Vec3 intersectionPoint, PathFaceRecord mouseOverFaceIndex, bool isDoubleClick)
	{
		bool result = false;
		if (MapScreen.Instance.MapState.AtMenu && _hoveredSiegeEntityID != UIntPtr.Zero)
		{
			Tuple<MatrixFrame, SettlementVisual> tuple = MapScreen.FrameAndVisualOfEngines[_hoveredSiegeEntityID];
			MapScreen.Instance.OnSiegeEngineFrameClick(tuple.Item1);
			result = true;
		}
		return result;
	}

	public override MapEntityVisual<PartyBase> GetVisualOfEntity(PartyBase partyBase)
	{
		_settlementVisuals.TryGetValue(partyBase, out var value);
		return value;
	}

	public SettlementVisual GetSettlementVisual(Settlement settlement)
	{
		return _settlementVisuals[settlement.Party];
	}

	protected override void OnInitialize()
	{
		base.OnInitialize();
		foreach (Settlement item in Settlement.All)
		{
			AddNewPartyVisualForParty(item.Party);
		}
		_ = Campaign.Current.MapSceneWrapper;
	}

	protected override void OnFinalize()
	{
		foreach (SettlementVisual value in _settlementVisuals.Values)
		{
			value.ReleaseResources();
		}
		CampaignEventDispatcher.Instance.RemoveListeners(this);
	}

	private void TickSiegeMachineCircles()
	{
		SiegeEvent playerSiegeEvent = PlayerSiege.PlayerSiegeEvent;
		bool isPlayerLeader = playerSiegeEvent != null && playerSiegeEvent.IsPlayerSiegeEvent && Campaign.Current.Models.EncounterModel.GetLeaderOfSiegeEvent(playerSiegeEvent, PlayerSiege.PlayerSide) == Hero.MainHero;
		Settlement besiegedSettlement = playerSiegeEvent.BesiegedSettlement;
		SettlementVisual settlementVisual = GetSettlementVisual(besiegedSettlement);
		Tuple<MatrixFrame, SettlementVisual> tuple = null;
		if (_hoveredSiegeEntityID != UIntPtr.Zero)
		{
			tuple = MapScreen.FrameAndVisualOfEngines[_hoveredSiegeEntityID];
		}
		for (int i = 0; i < settlementVisual.GetDefenderRangedSiegeEngineFrames().Length; i++)
		{
			bool isEmpty = playerSiegeEvent.GetSiegeEventSide(BattleSideEnum.Defender).SiegeEngines.DeployedRangedSiegeEngines[i] == null;
			bool isEnemy = PlayerSiege.PlayerSide != BattleSideEnum.Defender;
			string desiredMaterialName = GetDesiredMaterialName(isRanged: true, isAttacker: false, isTower: false);
			Decal decal = _defenderMachinesCircleEntities[i].GetComponentAtIndex(0, GameEntity.ComponentType.Decal) as Decal;
			if (decal.GetMaterial()?.Name != desiredMaterialName)
			{
				decal.SetMaterial(Material.GetFromResource(desiredMaterialName));
			}
			bool isHovered = tuple != null && _defenderMachinesCircleEntities[i].GetGlobalFrame().NearlyEquals(tuple.Item1);
			uint desiredDecalColor = GetDesiredDecalColor(isHovered, isEnemy, isEmpty, isPlayerLeader);
			if (desiredDecalColor != decal.GetFactor1())
			{
				decal.SetFactor1(desiredDecalColor);
			}
		}
		for (int j = 0; j < settlementVisual.GetAttackerRangedSiegeEngineFrames().Length; j++)
		{
			bool isEmpty2 = playerSiegeEvent.GetSiegeEventSide(BattleSideEnum.Attacker).SiegeEngines.DeployedRangedSiegeEngines[j] == null;
			bool isEnemy2 = PlayerSiege.PlayerSide != BattleSideEnum.Attacker;
			string desiredMaterialName2 = GetDesiredMaterialName(isRanged: true, isAttacker: true, isTower: false);
			Decal decal2 = _attackerRangedMachinesCircleEntities[j].GetComponentAtIndex(0, GameEntity.ComponentType.Decal) as Decal;
			if (decal2.GetMaterial()?.Name != desiredMaterialName2)
			{
				decal2.SetMaterial(Material.GetFromResource(desiredMaterialName2));
			}
			bool isHovered2 = tuple != null && _attackerRangedMachinesCircleEntities[j].GetGlobalFrame().NearlyEquals(tuple.Item1);
			uint desiredDecalColor2 = GetDesiredDecalColor(isHovered2, isEnemy2, isEmpty2, isPlayerLeader);
			if (desiredDecalColor2 != decal2.GetFactor1())
			{
				decal2.SetFactor1(desiredDecalColor2);
			}
		}
		for (int k = 0; k < settlementVisual.GetAttackerBatteringRamSiegeEngineFrames().Length; k++)
		{
			bool isEmpty3 = playerSiegeEvent.GetSiegeEventSide(BattleSideEnum.Attacker).SiegeEngines.DeployedMeleeSiegeEngines[k] == null;
			bool isEnemy3 = PlayerSiege.PlayerSide != BattleSideEnum.Attacker;
			string desiredMaterialName3 = GetDesiredMaterialName(isRanged: false, isAttacker: true, isTower: false);
			Decal decal3 = _attackerRamMachinesCircleEntities[k].GetComponentAtIndex(0, GameEntity.ComponentType.Decal) as Decal;
			if (decal3.GetMaterial()?.Name != desiredMaterialName3)
			{
				decal3.SetMaterial(Material.GetFromResource(desiredMaterialName3));
			}
			bool isHovered3 = tuple != null && _attackerRamMachinesCircleEntities[k].GetGlobalFrame().NearlyEquals(tuple.Item1);
			uint desiredDecalColor3 = GetDesiredDecalColor(isHovered3, isEnemy3, isEmpty3, isPlayerLeader);
			if (desiredDecalColor3 != decal3.GetFactor1())
			{
				decal3.SetFactor1(desiredDecalColor3);
			}
		}
		for (int l = 0; l < settlementVisual.GetAttackerTowerSiegeEngineFrames().Length; l++)
		{
			bool isEmpty4 = playerSiegeEvent.GetSiegeEventSide(BattleSideEnum.Attacker).SiegeEngines.DeployedMeleeSiegeEngines[settlementVisual.GetAttackerBatteringRamSiegeEngineFrames().Length + l] == null;
			bool isEnemy4 = PlayerSiege.PlayerSide != BattleSideEnum.Attacker;
			string desiredMaterialName4 = GetDesiredMaterialName(isRanged: false, isAttacker: true, isTower: true);
			Decal decal4 = _attackerTowerMachinesCircleEntities[l].GetComponentAtIndex(0, GameEntity.ComponentType.Decal) as Decal;
			if (decal4.GetMaterial()?.Name != desiredMaterialName4)
			{
				decal4.SetMaterial(Material.GetFromResource(desiredMaterialName4));
			}
			bool isHovered4 = tuple != null && _attackerTowerMachinesCircleEntities[l].GetGlobalFrame().NearlyEquals(tuple.Item1);
			uint desiredDecalColor4 = GetDesiredDecalColor(isHovered4, isEnemy4, isEmpty4, isPlayerLeader);
			if (desiredDecalColor4 != decal4.GetFactor1())
			{
				decal4.SetFactor1(desiredDecalColor4);
			}
		}
	}

	private void AddNewPartyVisualForParty(PartyBase partyBase)
	{
		SettlementVisual settlementVisual = new SettlementVisual(partyBase);
		settlementVisual.OnStartup();
		_settlementVisuals.Add(partyBase, settlementVisual);
		_visualsFlattened.Add(settlementVisual);
	}

	private uint GetDesiredDecalColor(bool isHovered, bool isEnemy, bool isEmpty, bool isPlayerLeader)
	{
		if (!isEnemy)
		{
			if (isHovered && isPlayerLeader)
			{
				return 4293956364u;
			}
			if (!isEmpty)
			{
				return 4283683126u;
			}
			if (isPlayerLeader)
			{
				float ratio = TaleWorlds.Library.MathF.PingPong(0f, 0.5f, _timeSinceCreation) / 0.5f;
				Color start = Color.FromUint(4278394186u);
				Color end = Color.FromUint(4284320212u);
				return Color.Lerp(start, end, ratio).ToUnsignedInteger();
			}
			return 4278394186u;
		}
		return 4287064638u;
	}

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
		if (!isTower)
		{
			return "decal_siege_ram";
		}
		return "decal_siege_tower";
	}

	private void RemoveSiegeCircleVisuals()
	{
		if (_playerSiegeMachineSlotMeshesAdded)
		{
			MapScene mapScene = Campaign.Current.MapSceneWrapper as MapScene;
			for (int i = 0; i < _defenderMachinesCircleEntities.Length; i++)
			{
				_defenderMachinesCircleEntities[i].SetVisibilityExcludeParents(visible: false);
				mapScene.Scene.RemoveEntity(_defenderMachinesCircleEntities[i], 107);
				_defenderMachinesCircleEntities[i] = null;
			}
			for (int j = 0; j < _attackerRamMachinesCircleEntities.Length; j++)
			{
				_attackerRamMachinesCircleEntities[j].SetVisibilityExcludeParents(visible: false);
				mapScene.Scene.RemoveEntity(_attackerRamMachinesCircleEntities[j], 108);
				_attackerRamMachinesCircleEntities[j] = null;
			}
			for (int k = 0; k < _attackerTowerMachinesCircleEntities.Length; k++)
			{
				_attackerTowerMachinesCircleEntities[k].SetVisibilityExcludeParents(visible: false);
				mapScene.Scene.RemoveEntity(_attackerTowerMachinesCircleEntities[k], 109);
				_attackerTowerMachinesCircleEntities[k] = null;
			}
			for (int l = 0; l < _attackerRangedMachinesCircleEntities.Length; l++)
			{
				_attackerRangedMachinesCircleEntities[l].SetVisibilityExcludeParents(visible: false);
				mapScene.Scene.RemoveEntity(_attackerRangedMachinesCircleEntities[l], 110);
				_attackerRangedMachinesCircleEntities[l] = null;
			}
			_playerSiegeMachineSlotMeshesAdded = false;
		}
	}

	private void RefreshMapSiegeOverlayRequired()
	{
		MapScreen.Instance.MapCameraView.OnRefreshMapSiegeOverlayRequired(_mapSiegeOverlayView == null);
		if (_playerSiegeMachineSlotMeshesAdded && PlayerSiege.PlayerSiegeEvent != null)
		{
			Settlement besiegedSettlement = PlayerSiege.PlayerSiegeEvent.BesiegedSettlement;
			if (besiegedSettlement != null && besiegedSettlement.CurrentSiegeState == Settlement.SiegeState.InTheLordsHall)
			{
				RemoveSiegeCircleVisuals();
				_playerSiegeMachineSlotMeshesAdded = false;
				return;
			}
		}
		if (PlayerSiege.PlayerSiegeEvent == null && _mapSiegeOverlayView != null)
		{
			MapScreen.Instance.RemoveMapView(_mapSiegeOverlayView);
			_mapSiegeOverlayView = null;
			if (_playerSiegeMachineSlotMeshesAdded)
			{
				RemoveSiegeCircleVisuals();
				_playerSiegeMachineSlotMeshesAdded = false;
			}
		}
		else if (PlayerSiege.PlayerSiegeEvent != null && _mapSiegeOverlayView == null)
		{
			_mapSiegeOverlayView = MapScreen.Instance.AddMapView<MapSiegeOverlayView>(Array.Empty<object>());
			if (!_playerSiegeMachineSlotMeshesAdded)
			{
				InitializeSiegeCircleVisuals();
				_playerSiegeMachineSlotMeshesAdded = true;
			}
		}
	}

	private void InitializeSiegeCircleVisuals()
	{
		Settlement besiegedSettlement = PlayerSiege.PlayerSiegeEvent.BesiegedSettlement;
		SettlementVisual settlementVisual = GetSettlementVisual(besiegedSettlement);
		MapScene mapScene = Campaign.Current.MapSceneWrapper as MapScene;
		MatrixFrame[] defenderRangedSiegeEngineFrames = settlementVisual.GetDefenderRangedSiegeEngineFrames();
		_defenderMachinesCircleEntities = new GameEntity[defenderRangedSiegeEngineFrames.Length];
		for (int i = 0; i < defenderRangedSiegeEngineFrames.Length; i++)
		{
			MatrixFrame matrixFrame = defenderRangedSiegeEngineFrames[i];
			_defenderMachinesCircleEntities[i] = GameEntity.CreateEmpty(mapScene.Scene);
			_defenderMachinesCircleEntities[i].Name = "dRangedMachineCircle_" + i;
			Decal decal = Decal.CreateDecal();
			decal.SetMaterial(Material.GetFromResource("decal_defender_ranged_siege"));
			decal.SetFactor1Linear(4287064638u);
			_defenderMachinesCircleEntities[i].AddComponent(decal);
			MatrixFrame frame = matrixFrame;
			if (_isNewDecalScaleImplementationEnabled)
			{
				frame.Scale(new Vec3(0.25f, 0.25f, 0.25f));
			}
			_defenderMachinesCircleEntities[i].SetGlobalFrame(in frame);
			_defenderMachinesCircleEntities[i].SetVisibilityExcludeParents(visible: true);
			mapScene.Scene.AddDecalInstance(decal, "editor_set", deletable: true);
		}
		defenderRangedSiegeEngineFrames = settlementVisual.GetAttackerBatteringRamSiegeEngineFrames();
		_attackerRamMachinesCircleEntities = new GameEntity[defenderRangedSiegeEngineFrames.Length];
		for (int j = 0; j < defenderRangedSiegeEngineFrames.Length; j++)
		{
			MatrixFrame matrixFrame2 = defenderRangedSiegeEngineFrames[j];
			_attackerRamMachinesCircleEntities[j] = GameEntity.CreateEmpty(mapScene.Scene);
			_attackerRamMachinesCircleEntities[j].Name = "InitializeSiegeCircleVisuals";
			_attackerRamMachinesCircleEntities[j].Name = "aRamMachineCircle_" + j;
			Decal decal2 = Decal.CreateDecal();
			decal2.SetMaterial(Material.GetFromResource("decal_siege_ram"));
			decal2.SetFactor1Linear(4287064638u);
			_attackerRamMachinesCircleEntities[j].AddComponent(decal2);
			MatrixFrame frame2 = matrixFrame2;
			if (_isNewDecalScaleImplementationEnabled)
			{
				frame2.Scale(new Vec3(0.38f, 0.38f, 0.38f));
			}
			_attackerRamMachinesCircleEntities[j].SetGlobalFrame(in frame2);
			_attackerRamMachinesCircleEntities[j].SetVisibilityExcludeParents(visible: true);
			mapScene.Scene.AddDecalInstance(decal2, "editor_set", deletable: true);
		}
		defenderRangedSiegeEngineFrames = settlementVisual.GetAttackerTowerSiegeEngineFrames();
		_attackerTowerMachinesCircleEntities = new GameEntity[defenderRangedSiegeEngineFrames.Length];
		for (int k = 0; k < defenderRangedSiegeEngineFrames.Length; k++)
		{
			MatrixFrame matrixFrame3 = defenderRangedSiegeEngineFrames[k];
			_attackerTowerMachinesCircleEntities[k] = GameEntity.CreateEmpty(mapScene.Scene);
			_attackerTowerMachinesCircleEntities[k].Name = "aTowerMachineCircle_" + k;
			Decal decal3 = Decal.CreateDecal();
			decal3.SetMaterial(Material.GetFromResource("decal_siege_tower"));
			decal3.SetFactor1Linear(4287064638u);
			_attackerTowerMachinesCircleEntities[k].AddComponent(decal3);
			MatrixFrame frame3 = matrixFrame3;
			if (_isNewDecalScaleImplementationEnabled)
			{
				frame3.Scale(new Vec3(0.38f, 0.38f, 0.38f));
			}
			_attackerTowerMachinesCircleEntities[k].SetGlobalFrame(in frame3);
			_attackerTowerMachinesCircleEntities[k].SetVisibilityExcludeParents(visible: true);
			mapScene.Scene.AddDecalInstance(decal3, "editor_set", deletable: true);
		}
		defenderRangedSiegeEngineFrames = settlementVisual.GetAttackerRangedSiegeEngineFrames();
		_attackerRangedMachinesCircleEntities = new GameEntity[defenderRangedSiegeEngineFrames.Length];
		for (int l = 0; l < defenderRangedSiegeEngineFrames.Length; l++)
		{
			MatrixFrame matrixFrame4 = defenderRangedSiegeEngineFrames[l];
			_attackerRangedMachinesCircleEntities[l] = GameEntity.CreateEmpty(mapScene.Scene);
			_attackerRangedMachinesCircleEntities[l].Name = "aRangedMachineCircle_" + l;
			Decal decal4 = Decal.CreateDecal();
			decal4.SetMaterial(Material.GetFromResource("decal_siege_ranged"));
			decal4.SetFactor1Linear(4287064638u);
			_attackerRangedMachinesCircleEntities[l].AddComponent(decal4);
			MatrixFrame frame4 = matrixFrame4;
			if (_isNewDecalScaleImplementationEnabled)
			{
				frame4.Scale(new Vec3(0.38f, 0.38f, 0.38f));
			}
			_attackerRangedMachinesCircleEntities[l].SetGlobalFrame(in frame4);
			_attackerRangedMachinesCircleEntities[l].SetVisibilityExcludeParents(visible: true);
			mapScene.Scene.AddDecalInstance(decal4, "editor_set", deletable: true);
		}
	}

	private void HandleSiegeEngineHover(UIntPtr newID)
	{
		if (_hoveredSiegeEntityID != newID)
		{
			_hoveredSiegeEntityID = newID;
			Tuple<MatrixFrame, SettlementVisual> tuple = MapScreen.FrameAndVisualOfEngines[_hoveredSiegeEntityID];
			tuple.Item2.OnMapHoverSiegeEngine(tuple.Item1);
		}
	}

	private void HandleSiegeEngineHoverEnd()
	{
		if (_hoveredSiegeEntityID != UIntPtr.Zero)
		{
			MapScreen.FrameAndVisualOfEngines[_hoveredSiegeEntityID].Item2.OnMapHoverSiegeEngineEnd();
			_hoveredSiegeEntityID = UIntPtr.Zero;
		}
	}
}
