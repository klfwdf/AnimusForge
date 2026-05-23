using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Map;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.Core;
using TaleWorlds.DotNet;
using TaleWorlds.Engine;
using TaleWorlds.Library;
using TaleWorlds.ModuleManager;
using TaleWorlds.MountAndBlade;

namespace SandBox;

public class MapScene : IMapScene
{
	private int _snowAndRainDataTextureWidth;

	private int _snowAndRainDataTextureHeight;

	public const int FlowMapTextureDimension = 512;

	private const string MapCampArea1Tag = "map_camp_area_1";

	private const string MapCampArea2Tag = "map_camp_area_2";

	private Scene _scene;

	private MBAgentRendererSceneController _agentRendererSceneController;

	private byte[] _snowAndRainData;

	private float[] _windFlowMapData;

	private Vec2 _minimumPositionCache;

	private Vec2 _maximumPositionCache;

	private float _maximumHeightCache;

	private Dictionary<string, uint> _sceneLevels;

	private int _battleTerrainIndexMapWidth;

	private int _battleTerrainIndexMapHeight;

	private byte[] _battleTerrainIndexMap;

	private Vec2 _terrainSize;

	private ReaderWriterLockSlim _sharedLock;

	public Scene Scene => _scene;

	public MapScene()
	{
		_sharedLock = new ReaderWriterLockSlim();
		_sceneLevels = new Dictionary<string, uint>();
	}

	public Vec2 GetTerrainSize()
	{
		return _terrainSize;
	}

	public uint GetSceneLevel(string name)
	{
		_sharedLock.EnterReadLock();
		uint value;
		bool num = _sceneLevels.TryGetValue(name, out value) && value != int.MaxValue;
		_sharedLock.ExitReadLock();
		if (num)
		{
			return value;
		}
		uint upgradeLevelMaskOfLevelName = _scene.GetUpgradeLevelMaskOfLevelName(name);
		_sharedLock.EnterWriteLock();
		_sceneLevels[name] = upgradeLevelMaskOfLevelName;
		_sharedLock.ExitWriteLock();
		return upgradeLevelMaskOfLevelName;
	}

	public void SetSceneLevels(List<string> levels)
	{
		foreach (string level in levels)
		{
			_sceneLevels.Add(level, 2147483647u);
		}
	}

	public List<AtmosphereState> GetAtmosphereStates()
	{
		List<AtmosphereState> list = new List<AtmosphereState>();
		foreach (GameEntity item2 in Scene.FindEntitiesWithTag("atmosphere_probe"))
		{
			MapAtmosphereProbe firstScriptOfType = item2.GetFirstScriptOfType<MapAtmosphereProbe>();
			Vec3 globalPosition = item2.GlobalPosition;
			AtmosphereState item = new AtmosphereState
			{
				Position = globalPosition,
				HumidityAverage = firstScriptOfType.rainDensity,
				HumidityVariance = 5f,
				TemperatureAverage = firstScriptOfType.temperature,
				TemperatureVariance = 5f,
				distanceForMaxWeight = firstScriptOfType.minRadius,
				distanceForMinWeight = firstScriptOfType.maxRadius,
				ColorGradeTexture = firstScriptOfType.colorGrade
			};
			list.Add(item);
		}
		return list;
	}

	public void ValidateAgentVisualsReseted()
	{
		if (_scene != null && _agentRendererSceneController != null)
		{
			MBAgentRendererSceneController.ValidateAgentVisualsReseted(_scene, _agentRendererSceneController);
		}
	}

	public void SetAtmosphereColorgrade(TerrainType terrainType)
	{
	}

	public void AddNewEntityToMapScene(string entityId, in CampaignVec2 position)
	{
		GameEntity gameEntity = GameEntity.Instantiate(_scene, entityId, callScriptCallbacks: true);
		if (gameEntity != null)
		{
			gameEntity.SetLocalPosition(position.AsVec3());
		}
	}

	public void GetMapBorders(out Vec2 minimumPosition, out Vec2 maximumPosition, out float maximumHeight)
	{
		minimumPosition = _minimumPositionCache;
		maximumPosition = _maximumPositionCache;
		maximumHeight = _maximumHeightCache;
	}

	public void Load()
	{
		Debug.Print("Creating map scene");
		_scene = Scene.CreateNewScene(initialize_physics: false, enable_decals: true, DecalAtlasGroup.Worldmap, "MapScene");
		_scene.SetClothSimulationState(state: true);
		_agentRendererSceneController = MBAgentRendererSceneController.CreateNewAgentRendererSceneController(_scene);
		_agentRendererSceneController.SetDoTimerBasedForcedSkeletonUpdates(value: false);
		_scene.SetOcclusionMode(mode: true);
		SceneInitializationData initData = new SceneInitializationData(initializeWithDefaults: true);
		initData.UsePhysicsMaterials = false;
		initData.EnableFloraPhysics = false;
		initData.UseTerrainMeshBlending = false;
		initData.CreateOros = false;
		_scene.SetFetchCrcInfoOfScene(value: true);
		DisableUnwalkableNavigationMeshes();
		bool[] regionMapping = SandBoxHelpers.MapSceneHelper.GetRegionMapping(Campaign.Current.Models.PartyNavigationModel);
		_scene.SetNavMeshRegionMap(regionMapping);
		ModuleInfo mainMapModule = GetMainMapModule();
		if (mainMapModule != null)
		{
			_scene.Read("Main_map", mainMapModule.Id, ref initData);
		}
		else
		{
			_scene.Read("Main_map", ref initData);
		}
		Utilities.SetAllocationAlwaysValidScene(_scene);
		GameEntity firstEntityWithName = _scene.GetFirstEntityWithName("border_min");
		GameEntity firstEntityWithName2 = _scene.GetFirstEntityWithName("border_max");
		_minimumPositionCache = firstEntityWithName.GetGlobalFrame().origin.AsVec2;
		_maximumPositionCache = firstEntityWithName2.GetGlobalFrame().origin.AsVec2;
		_maximumHeightCache = firstEntityWithName2.GetGlobalFrame().origin.z;
		_scene.DisableStaticShadows(value: true);
		_scene.InvalidateTerrainPhysicsMaterials();
		_scene.SetDontLoadInvisibleEntities(value: true);
		GameEntity firstEntityWithName3 = _scene.GetFirstEntityWithName("medit_water_plane");
		if (firstEntityWithName3 != null)
		{
			firstEntityWithName3.SetDoNotCheckVisibility(value: true);
		}
		if (_scene.GetFirstEntityWithScriptComponent("Town Scene Manager") == null)
		{
			MBDebug.ShowWarning("Main map scene must have an entity with 'Town Scene Manager' script for mesh memory optimization.");
		}
		LoadAtmosphereData(_scene);
		Campaign.Current.Models.MapWeatherModel.InitializeCaches();
		MBMapScene.ValidateTerrainSoundIds();
		_scene.OptimizeScene();
		_scene.GetTerrainData(out var nodeDimension, out var nodeSize, out var _, out var _);
		_terrainSize.x = (float)nodeDimension.X * nodeSize;
		_terrainSize.y = (float)nodeDimension.Y * nodeSize;
		MBMapScene.GetBattleSceneIndexMap(_scene, ref _battleTerrainIndexMap, ref _battleTerrainIndexMapWidth, ref _battleTerrainIndexMapHeight);
		Debug.Print("Ticking map scene for first initialization");
		_scene.Tick(0.1f);
		AsyncTask campaignLateAITickTask = AsyncTask.CreateWithDelegate(new ManagedDelegate
		{
			Instance = Campaign.LateAITick
		}, isBackground: false);
		Campaign.Current.CampaignLateAITickTask = campaignLateAITickTask;
	}

	public void SetSnowAndRainDataWithDimension(Texture snowRainTexture, int weatherNodeGridWidthAndHeight)
	{
		_scene.CreateDynamicRainTexture(weatherNodeGridWidthAndHeight, weatherNodeGridWidthAndHeight);
		_snowAndRainDataTextureWidth = snowRainTexture.Width;
		_snowAndRainDataTextureHeight = snowRainTexture.Height;
		_snowAndRainData = new byte[_snowAndRainDataTextureWidth * _snowAndRainDataTextureHeight * 2];
		snowRainTexture.GetPixelData(_snowAndRainData);
		_scene.SetDynamicSnowTexture(snowRainTexture);
		Campaign.Current.DefaultWeatherNodeDimension = weatherNodeGridWidthAndHeight;
		_windFlowMapData = new float[524288];
		_scene.GetWindFlowMapData(_windFlowMapData);
	}

	public void AfterLoad()
	{
	}

	private ModuleInfo GetMainMapModule()
	{
		ModuleInfo result = null;
		foreach (ModuleInfo activeModule in ModuleHelper.GetActiveModules())
		{
			if (activeModule.IsActive && File.Exists(System.IO.Path.Combine(activeModule.FolderPath, "SceneObj", "Main_map", "scene.xscene")))
			{
				result = activeModule;
			}
		}
		return result;
	}

	public void Destroy()
	{
		MBAgentRendererSceneController.DestructAgentRendererSceneController(_scene, _agentRendererSceneController, deleteThisFrame: false);
		_agentRendererSceneController = null;
	}

	public void DisableUnwalkableNavigationMeshes()
	{
		int[] invalidTerrainTypesForNavigationType = Campaign.Current.Models.PartyNavigationModel.GetInvalidTerrainTypesForNavigationType(MobileParty.NavigationType.All);
		foreach (int faceGroupId in invalidTerrainTypesForNavigationType)
		{
			Scene.SetAbilityOfFacesWithId(faceGroupId, isEnabled: false);
		}
	}

	public PathFaceRecord GetFaceIndex(in CampaignVec2 vec2)
	{
		PathFaceRecord record = new PathFaceRecord(-1, -1, -1);
		_scene.GetNavMeshFaceIndex(ref record, vec2.ToVec2(), vec2.IsOnLand, checkIfDisabled: false, ignoreHeight: true);
		return record;
	}

	private void LoadAtmosphereData(Scene mapScene)
	{
		MBMapScene.LoadAtmosphereData(mapScene);
	}

	public TerrainType GetTerrainTypeAtPosition(in CampaignVec2 position)
	{
		PathFaceRecord face = position.Face;
		return GetFaceTerrainType(face);
	}

	public TerrainType GetFaceTerrainType(PathFaceRecord navMeshFace)
	{
		if (!navMeshFace.IsValid())
		{
			Debug.FailedAssert("Null nav mesh face tried to get terrain type.", "C:\\BuildAgent\\work\\mb3\\Source\\Bannerlord\\SandBox\\MapScene.cs", "GetFaceTerrainType", 338);
			return TerrainType.Plain;
		}
		return (TerrainType)navMeshFace.FaceGroupIndex;
	}

	public CampaignVec2 GetNearestFaceCenterForPosition(in CampaignVec2 position, int[] excludedFaceIds)
	{
		return new CampaignVec2(MBMapScene.GetNearestFaceCenterForPosition(_scene, position.ToVec2(), position.IsOnLand, excludedFaceIds), position.IsOnLand);
	}

	public CampaignVec2 GetNearestFaceCenterForPositionWithPath(PathFaceRecord pathFaceRecord, bool targetIsLand, float maxDist, int[] excludedFaceIds)
	{
		return new CampaignVec2(MBMapScene.GetNearestFaceCenterForPositionWithPath(_scene, pathFaceRecord, targetIsLand, maxDist, excludedFaceIds), targetIsLand);
	}

	public List<TerrainType> GetEnvironmentTerrainTypes(in CampaignVec2 originPosition)
	{
		List<TerrainType> list = new List<TerrainType>();
		Vec2 vec = new Vec2(1f, 0f);
		CampaignVec2 position = originPosition;
		list.Add(GetTerrainTypeAtPosition(in position));
		for (int i = 0; i < 8; i++)
		{
			vec.RotateCCW(System.MathF.PI / 4f * (float)i);
			for (int j = 1; j < 7; j++)
			{
				position += j * vec;
				TerrainType terrainTypeAtPosition = GetTerrainTypeAtPosition(in position);
				if (!list.Contains(terrainTypeAtPosition))
				{
					list.Add(terrainTypeAtPosition);
				}
			}
		}
		return list;
	}

	public List<TerrainType> GetEnvironmentTerrainTypesCount(in CampaignVec2 originPosition, out TerrainType currentPositionTerrainType)
	{
		List<TerrainType> list = new List<TerrainType>();
		Vec2 vec = new Vec2(1f, 0f);
		CampaignVec2 position = originPosition;
		currentPositionTerrainType = GetTerrainTypeAtPosition(in position);
		list.Add(currentPositionTerrainType);
		for (int i = 0; i < 8; i++)
		{
			vec.RotateCCW(System.MathF.PI / 4f * (float)i);
			for (int j = 1; j < 7; j++)
			{
				position += j * vec;
				if (position.Face.IsValid())
				{
					TerrainType faceTerrainType = GetFaceTerrainType(position.Face);
					list.Add(faceTerrainType);
				}
			}
		}
		return list;
	}

	public MapPatchData GetMapPatchAtPosition(in CampaignVec2 position)
	{
		if (_battleTerrainIndexMap != null)
		{
			int value = TaleWorlds.Library.MathF.Floor(position.X / _terrainSize.X * (float)_battleTerrainIndexMapWidth);
			int value2 = TaleWorlds.Library.MathF.Floor(position.Y / _terrainSize.Y * (float)_battleTerrainIndexMapHeight);
			value = MBMath.ClampIndex(value, 0, _battleTerrainIndexMapWidth);
			int num = (MBMath.ClampIndex(value2, 0, _battleTerrainIndexMapHeight) * _battleTerrainIndexMapWidth + value) * 2;
			byte sceneIndex = _battleTerrainIndexMap[num];
			byte b = _battleTerrainIndexMap[num + 1];
			Vec2 normalizedCoordinates = new Vec2((float)(b & 0xF) / 15f, (float)((b >> 4) & 0xF) / 15f);
			return new MapPatchData
			{
				sceneIndex = sceneIndex,
				normalizedCoordinates = normalizedCoordinates
			};
		}
		return default(MapPatchData);
	}

	public CampaignVec2 GetAccessiblePointNearPosition(in CampaignVec2 pos, float radius)
	{
		return new CampaignVec2(MBMapScene.GetAccessiblePointNearPosition(_scene, pos.ToVec2(), pos.IsOnLand, radius), pos.IsOnLand);
	}

	public bool GetPathBetweenAIFaces(PathFaceRecord startingFace, PathFaceRecord endingFace, Vec2 startingPosition, Vec2 endingPosition, float agentRadius, NavigationPath path, int[] excludedFaceIds, float extraCostMultiplier, int regionSwitchCostFromLandToSea, int regionSwitchCostFromSeaToLand)
	{
		if (regionSwitchCostFromLandToSea == 0 && regionSwitchCostFromSeaToLand == 0)
		{
			return _scene.GetPathBetweenAIFaces(startingFace.FaceIndex, endingFace.FaceIndex, startingPosition, endingPosition, agentRadius, path, excludedFaceIds, extraCostMultiplier);
		}
		return _scene.GetPathBetweenAIFaces(startingFace.FaceIndex, endingFace.FaceIndex, startingPosition, endingPosition, agentRadius, path, excludedFaceIds, extraCostMultiplier, regionSwitchCostFromLandToSea, regionSwitchCostFromSeaToLand);
	}

	public bool GetPathDistanceBetweenAIFaces(PathFaceRecord startingAiFace, PathFaceRecord endingAiFace, Vec2 startingPosition, Vec2 endingPosition, float agentRadius, float distanceLimit, out float distance, int[] excludedFaceIds, int regionSwitchCostFromLandToSea, int regionSwitchCostFromSeaToLand)
	{
		return _scene.GetPathDistanceBetweenAIFaces(startingAiFace.FaceIndex, endingAiFace.FaceIndex, startingPosition, endingPosition, agentRadius, distanceLimit, out distance, excludedFaceIds, regionSwitchCostFromLandToSea, regionSwitchCostFromSeaToLand);
	}

	public bool IsLineToPointClear(PathFaceRecord startingFace, Vec2 position, Vec2 destination, float agentRadius)
	{
		return _scene.IsLineToPointClear(startingFace.FaceIndex, position, destination, agentRadius);
	}

	public Vec2 GetLastPointOnNavigationMeshFromPositionToDestination(PathFaceRecord startingFace, Vec2 position, Vec2 destination, int[] excludedFaceIds = null)
	{
		return _scene.GetLastPointOnNavigationMeshFromPositionToDestination(startingFace.FaceIndex, position, destination, excludedFaceIds);
	}

	public Vec2 GetLastPositionOnNavMeshFaceForPointAndDirection(PathFaceRecord startingFace, Vec2 position, Vec2 destination)
	{
		return _scene.GetLastPositionOnNavMeshFaceForPointAndDirection(startingFace, position, destination);
	}

	public Vec2 GetNavigationMeshCenterPosition(PathFaceRecord face)
	{
		Vec3 centerPosition = Vec3.Zero;
		_scene.GetNavMeshCenterPosition(face.FaceIndex, ref centerPosition);
		return centerPosition.AsVec2;
	}

	public Vec2 GetNavigationMeshCenterPosition(int faceIndex)
	{
		Vec3 centerPosition = Vec3.Zero;
		_scene.GetNavMeshCenterPosition(faceIndex, ref centerPosition);
		return centerPosition.AsVec2;
	}

	public int GetNumberOfNavigationMeshFaces()
	{
		return _scene.GetNavMeshFaceCount();
	}

	public PathFaceRecord GetFaceAtIndex(int faceIndex)
	{
		return _scene.GetNavMeshPathFaceRecord(faceIndex);
	}

	public bool GetHeightAtPoint(in CampaignVec2 point, ref float height)
	{
		BodyFlags bodyFlags = BodyFlags.Moveable;
		bodyFlags = (BodyFlags)((uint)bodyFlags | (uint)(point.IsOnLand ? 128 : 544321929));
		return _scene.GetHeightAtPoint(point.ToVec2(), bodyFlags, ref height);
	}

	public float GetWinterTimeFactor()
	{
		return _scene.GetWinterTimeFactor();
	}

	public float GetFaceVertexZ(PathFaceRecord navMeshFace)
	{
		return _scene.GetNavMeshFaceFirstVertexZ(navMeshFace.FaceIndex);
	}

	public Vec3 GetGroundNormal(Vec2 position)
	{
		return _scene.GetNormalAt(position);
	}

	public void GetSiegeCampFrames(Settlement settlement, out List<MatrixFrame> siegeCamp1GlobalFrames, out List<MatrixFrame> siegeCamp2GlobalFrames)
	{
		siegeCamp1GlobalFrames = new List<MatrixFrame>();
		siegeCamp2GlobalFrames = new List<MatrixFrame>();
		GameEntity campaignEntityWithName = _scene.GetCampaignEntityWithName(settlement.Party.Id);
		if (!(campaignEntityWithName != null) || !settlement.IsFortification)
		{
			return;
		}
		List<GameEntity> children = new List<GameEntity>();
		campaignEntityWithName.GetChildrenRecursive(ref children);
		foreach (GameEntity item in children)
		{
			if (item.HasTag("map_camp_area_1"))
			{
				siegeCamp1GlobalFrames.Add(item.GetGlobalFrame());
			}
			else if (item.HasTag("map_camp_area_2"))
			{
				siegeCamp2GlobalFrames.Add(item.GetGlobalFrame());
			}
		}
	}

	public void GetTerrainHeightAndNormal(Vec2 position, out float height, out Vec3 normal)
	{
		_scene.GetTerrainHeightAndNormal(position, out height, out normal);
	}

	public string GetTerrainTypeName(TerrainType type)
	{
		string result = "Invalid";
		switch (type)
		{
		case TerrainType.Water:
			result = "Water";
			break;
		case TerrainType.Mountain:
			result = "Mountain";
			break;
		case TerrainType.Snow:
			result = "Snow";
			break;
		case TerrainType.Steppe:
			result = "Steppe";
			break;
		case TerrainType.Plain:
			result = "Plain";
			break;
		case TerrainType.Desert:
			result = "Desert";
			break;
		case TerrainType.Swamp:
			result = "Swamp";
			break;
		case TerrainType.Dune:
			result = "Dune";
			break;
		case TerrainType.Bridge:
			result = "Bridge";
			break;
		case TerrainType.River:
			result = "River";
			break;
		case TerrainType.Forest:
			result = "Forest";
			break;
		case TerrainType.Fording:
			result = "Fording";
			break;
		case TerrainType.Lake:
			result = "Lake";
			break;
		case TerrainType.Canyon:
			result = "Canyon";
			break;
		}
		return result;
	}

	public uint GetSceneXmlCrc()
	{
		return _scene.GetSceneXMLCRC();
	}

	public uint GetSceneNavigationMeshCrc()
	{
		return _scene.GetNavigationMeshCRC();
	}

	public Vec2 GetWindAtPosition(Vec2 position)
	{
		int textureDataIndexForPosition = GetTextureDataIndexForPosition(position, 512, 512);
		return new Vec2(_windFlowMapData[textureDataIndexForPosition * 2], _windFlowMapData[textureDataIndexForPosition * 2 + 1]);
	}

	public float GetSnowAmountAtPosition(Vec2 position)
	{
		int textureDataIndexForPosition = GetTextureDataIndexForPosition(position, _snowAndRainDataTextureWidth, _snowAndRainDataTextureHeight);
		return (int)_snowAndRainData[textureDataIndexForPosition * 2];
	}

	public float GetRainAmountAtPosition(Vec2 position)
	{
		int textureDataIndexForPosition = GetTextureDataIndexForPosition(position, _snowAndRainDataTextureWidth, _snowAndRainDataTextureHeight);
		return (int)_snowAndRainData[textureDataIndexForPosition * 2 + 1];
	}

	private int GetTextureDataIndexForPosition(Vec2 position, int dimensionX, int dimensionY)
	{
		Vec2 terrainSize = GetTerrainSize();
		int value = TaleWorlds.Library.MathF.Floor(position.x / terrainSize.X * (float)dimensionX);
		int value2 = TaleWorlds.Library.MathF.Floor(position.y / terrainSize.Y * (float)dimensionY);
		value = MBMath.ClampIndex(value, 0, dimensionX);
		return MBMath.ClampIndex(value2, 0, dimensionY) * dimensionX + value;
	}

	public void SetupWaterWake(float wakeWorldSize, float wakeCameraOffset)
	{
		_scene.EnsureWaterWakeRenderer();
		_scene.SetWaterWakeWorldSize(wakeWorldSize, 0.994f);
		_scene.SetWaterWakeCameraOffset(wakeCameraOffset);
	}

	PathFaceRecord IMapScene.GetFaceIndex(in CampaignVec2 vec2)
	{
		return GetFaceIndex(in vec2);
	}

	TerrainType IMapScene.GetTerrainTypeAtPosition(in CampaignVec2 vec2)
	{
		return GetTerrainTypeAtPosition(in vec2);
	}

	List<TerrainType> IMapScene.GetEnvironmentTerrainTypes(in CampaignVec2 vec2)
	{
		return GetEnvironmentTerrainTypes(in vec2);
	}

	List<TerrainType> IMapScene.GetEnvironmentTerrainTypesCount(in CampaignVec2 vec2, out TerrainType currentPositionTerrainType)
	{
		return GetEnvironmentTerrainTypesCount(in vec2, out currentPositionTerrainType);
	}

	MapPatchData IMapScene.GetMapPatchAtPosition(in CampaignVec2 position)
	{
		return GetMapPatchAtPosition(in position);
	}

	CampaignVec2 IMapScene.GetNearestFaceCenterForPosition(in CampaignVec2 vec2, int[] excludedFaceIds)
	{
		return GetNearestFaceCenterForPosition(in vec2, excludedFaceIds);
	}

	CampaignVec2 IMapScene.GetAccessiblePointNearPosition(in CampaignVec2 vec2, float radius)
	{
		return GetAccessiblePointNearPosition(in vec2, radius);
	}

	bool IMapScene.GetHeightAtPoint(in CampaignVec2 point, ref float height)
	{
		return GetHeightAtPoint(in point, ref height);
	}

	void IMapScene.AddNewEntityToMapScene(string entityId, in CampaignVec2 position)
	{
		AddNewEntityToMapScene(entityId, in position);
	}
}
