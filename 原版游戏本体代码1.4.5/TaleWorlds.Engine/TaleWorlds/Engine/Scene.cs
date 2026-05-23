using System;
using System.Collections.Generic;
using System.Linq;
using TaleWorlds.DotNet;
using TaleWorlds.Library;

namespace TaleWorlds.Engine;

[EngineClass("rglScene")]
public sealed class Scene : NativeObject
{
	public static float MaximumWindSpeed = 30f;

	public const float AutoClimbHeight = 1.5f;

	public const float NavMeshHeightLimit = 1.5f;

	public const int SunRise = 2;

	public const int SunSet = 22;

	public static readonly TWSharedMutex PhysicsAndRayCastLock = new TWSharedMutex();

	public int RootEntityCount => EngineApplicationInterface.IScene.GetRootEntityCount(base.Pointer);

	public bool HasTerrainHeightmap => EngineApplicationInterface.IScene.HasTerrainHeightmap(base.Pointer);

	public bool ContainsTerrain => EngineApplicationInterface.IScene.ContainsTerrain(base.Pointer);

	public float TimeOfDay
	{
		get
		{
			return EngineApplicationInterface.IScene.GetTimeOfDay(base.Pointer);
		}
		set
		{
			EngineApplicationInterface.IScene.SetTimeOfDay(base.Pointer, value);
		}
	}

	public bool IsDayTime
	{
		get
		{
			int num = TaleWorlds.Library.MathF.Floor(TimeOfDay);
			if (num >= 2)
			{
				return num < 22;
			}
			return false;
		}
	}

	public bool IsAtmosphereIndoor => EngineApplicationInterface.IScene.IsAtmosphereIndoor(base.Pointer);

	public Vec3 LastFinalRenderCameraPosition => EngineApplicationInterface.IScene.GetLastFinalRenderCameraPosition(base.Pointer);

	public MatrixFrame LastFinalRenderCameraFrame
	{
		get
		{
			MatrixFrame outFrame = default(MatrixFrame);
			EngineApplicationInterface.IScene.GetLastFinalRenderCameraFrame(base.Pointer, ref outFrame);
			return outFrame;
		}
	}

	public float TimeSpeed
	{
		get
		{
			return EngineApplicationInterface.IScene.GetTimeSpeed(base.Pointer);
		}
		set
		{
			EngineApplicationInterface.IScene.SetTimeSpeed(base.Pointer, value);
		}
	}

	private Scene()
	{
	}

	internal Scene(UIntPtr pointer)
	{
		Construct(pointer);
	}

	public bool IsDefaultEditorScene()
	{
		return EngineApplicationInterface.IScene.IsDefaultEditorScene(this);
	}

	public bool IsMultiplayerScene()
	{
		return EngineApplicationInterface.IScene.IsMultiplayerScene(this);
	}

	public string TakePhotoModePicture(bool saveAmbientOcclusionPass, bool savingObjectIdPass, bool saveShadowPass)
	{
		return EngineApplicationInterface.IScene.TakePhotoModePicture(this, saveAmbientOcclusionPass, savingObjectIdPass, saveShadowPass);
	}

	public string GetAllColorGradeNames()
	{
		return EngineApplicationInterface.IScene.GetAllColorGradeNames(this);
	}

	public string GetAllFilterNames()
	{
		return EngineApplicationInterface.IScene.GetAllFilterNames(this);
	}

	public float GetPhotoModeRoll()
	{
		return EngineApplicationInterface.IScene.GetPhotoModeRoll(this);
	}

	public bool GetPhotoModeOrbit()
	{
		return EngineApplicationInterface.IScene.GetPhotoModeOrbit(this);
	}

	public bool GetPhotoModeOn()
	{
		return EngineApplicationInterface.IScene.GetPhotoModeOn(this);
	}

	public void GetPhotoModeFocus(ref float focus, ref float focusStart, ref float focusEnd, ref float exposure, ref bool vignetteOn)
	{
		EngineApplicationInterface.IScene.GetPhotoModeFocus(this, ref focus, ref focusStart, ref focusEnd, ref exposure, ref vignetteOn);
	}

	public int GetSceneColorGradeIndex()
	{
		return EngineApplicationInterface.IScene.GetSceneColorGradeIndex(this);
	}

	public int GetSceneFilterIndex()
	{
		return EngineApplicationInterface.IScene.GetSceneFilterIndex(this);
	}

	public void EnableFixedTick()
	{
		EngineApplicationInterface.IScene.EnableFixedTick(this);
	}

	public string GetLoadingStateName()
	{
		return EngineApplicationInterface.IScene.GetLoadingStateName(this);
	}

	public bool IsLoadingFinished()
	{
		return EngineApplicationInterface.IScene.IsLoadingFinished(this);
	}

	public void SetPhotoModeRoll(float roll)
	{
		EngineApplicationInterface.IScene.SetPhotoModeRoll(this, roll);
	}

	public void SetPhotoModeOrbit(bool orbit)
	{
		EngineApplicationInterface.IScene.SetPhotoModeOrbit(this, orbit);
	}

	public float GetFallDensity()
	{
		return EngineApplicationInterface.IScene.GetFallDensity(base.Pointer);
	}

	public void SetPhotoModeOn(bool on)
	{
		EngineApplicationInterface.IScene.SetPhotoModeOn(this, on);
	}

	public void SetPhotoModeFocus(float focusStart, float focusEnd, float focus, float exposure)
	{
		EngineApplicationInterface.IScene.SetPhotoModeFocus(this, focusStart, focusEnd, focus, exposure);
	}

	public void SetPhotoModeFov(float verticalFov)
	{
		EngineApplicationInterface.IScene.SetPhotoModeFov(this, verticalFov);
	}

	public float GetPhotoModeFov()
	{
		return EngineApplicationInterface.IScene.GetPhotoModeFov(this);
	}

	public bool HasDecalRenderer()
	{
		return EngineApplicationInterface.IScene.HasDecalRenderer(base.Pointer);
	}

	public void SetPhotoModeVignette(bool vignetteOn)
	{
		EngineApplicationInterface.IScene.SetPhotoModeVignette(this, vignetteOn);
	}

	public void SetSceneColorGradeIndex(int index)
	{
		EngineApplicationInterface.IScene.SetSceneColorGradeIndex(this, index);
	}

	public int SetSceneFilterIndex(int index)
	{
		return EngineApplicationInterface.IScene.SetSceneFilterIndex(this, index);
	}

	public void SetSceneColorGrade(string textureName)
	{
		EngineApplicationInterface.IScene.SetSceneColorGrade(this, textureName);
	}

	public void SetUpgradeLevel(int level)
	{
		EngineApplicationInterface.IScene.SetUpgradeLevel(base.Pointer, level);
	}

	public void CreateBurstParticle(int particleId, MatrixFrame frame)
	{
		EngineApplicationInterface.IScene.CreateBurstParticle(this, particleId, ref frame);
	}

	public float[] GetTerrainHeightData(int nodeXIndex, int nodeYIndex)
	{
		float[] array = new float[EngineApplicationInterface.IScene.GetNodeDataCount(this, nodeXIndex, nodeYIndex)];
		EngineApplicationInterface.IScene.FillTerrainHeightData(this, nodeXIndex, nodeYIndex, array);
		return array;
	}

	public short[] GetTerrainPhysicsMaterialIndexData(int nodeXIndex, int nodeYIndex)
	{
		short[] array = new short[EngineApplicationInterface.IScene.GetNodeDataCount(this, nodeXIndex, nodeYIndex)];
		EngineApplicationInterface.IScene.FillTerrainPhysicsMaterialIndexData(this, nodeXIndex, nodeYIndex, array);
		return array;
	}

	public void GetTerrainData(out Vec2i nodeDimension, out float nodeSize, out int layerCount, out int layerVersion)
	{
		EngineApplicationInterface.IScene.GetTerrainData(this, out nodeDimension, out nodeSize, out layerCount, out layerVersion);
	}

	public void GetTerrainNodeData(int xIndex, int yIndex, out int vertexCountAlongAxis, out float quadLength, out float minHeight, out float maxHeight)
	{
		EngineApplicationInterface.IScene.GetTerrainNodeData(this, xIndex, yIndex, out vertexCountAlongAxis, out quadLength, out minHeight, out maxHeight);
	}

	public PhysicsMaterial GetTerrainPhysicsMaterialAtLayer(int layerIndex)
	{
		int terrainPhysicsMaterialIndexAtLayer = EngineApplicationInterface.IScene.GetTerrainPhysicsMaterialIndexAtLayer(this, layerIndex);
		return new PhysicsMaterial(terrainPhysicsMaterialIndexAtLayer);
	}

	public void SetSceneColorGrade(Scene scene, string textureName)
	{
		EngineApplicationInterface.IScene.SetSceneColorGrade(scene, textureName);
	}

	public float GetWaterLevel()
	{
		return EngineApplicationInterface.IScene.GetWaterLevel(this);
	}

	public float GetWaterLevelAtPosition(Vec2 position, bool useWaterRenderer, bool checkWaterBodyEntities)
	{
		return EngineApplicationInterface.IScene.GetWaterLevelAtPosition(this, position, useWaterRenderer, checkWaterBodyEntities);
	}

	public Vec3 GetWaterSpeedAtPosition(Vec2 position, bool doChoppinessCorrection)
	{
		return EngineApplicationInterface.IScene.GetWaterSpeedAtPosition(base.Pointer, in position, doChoppinessCorrection);
	}

	public void GetBulkWaterLevelAtPositions(Vec2[] waterHeightQueryArray, ref float[] waterHeightsAtVolumes, ref Vec3[] waterSurfaceNormals)
	{
		EngineApplicationInterface.IScene.GetBulkWaterLevelAtPositions(this, waterHeightQueryArray, waterHeightQueryArray.Length, waterHeightsAtVolumes, waterSurfaceNormals);
	}

	public void GetInterpolationFactorForBodyWorldTransformSmoothing(out float interpolationFactor, out float fixedDt)
	{
		EngineApplicationInterface.IScene.GetInterpolationFactorForBodyWorldTransformSmoothing(this, out interpolationFactor, out fixedDt);
	}

	public void GetBulkWaterLevelAtVolumes(UIntPtr waterHeightQueryArray, int waterHeightQueryArrayCount, in MatrixFrame globalFrame)
	{
		EngineApplicationInterface.IScene.GetBulkWaterLevelAtVolumes(base.Pointer, waterHeightQueryArray, waterHeightQueryArrayCount, in globalFrame);
	}

	public float GetWaterStrength()
	{
		return EngineApplicationInterface.IScene.GetWaterStrength(this);
	}

	public void DeRegisterShipVisual(UIntPtr visualPointer)
	{
		EngineApplicationInterface.IScene.DeRegisterShipVisual(base.Pointer, visualPointer);
	}

	public UIntPtr RegisterShipVisualToWaterRenderer(WeakGameEntity entity, in Vec3 waterEffectBB)
	{
		return EngineApplicationInterface.IScene.RegisterShipVisualToWaterRenderer(base.Pointer, entity.Pointer, in waterEffectBB);
	}

	public void SetWaterStrength(float newWaterStrength)
	{
		EngineApplicationInterface.IScene.SetWaterStrength(this, newWaterStrength);
	}

	public void AddWaterWakeWithSphere(Vec3 position, float radius, float wakeVisibility, float foamVisibility)
	{
		AddWaterWakeWithCapsule(position, radius, position, radius, wakeVisibility, foamVisibility);
	}

	public void AddWaterWakeWithCapsule(Vec3 positionA, float radiusA, Vec3 positionB, float radiusB, float wakeVisibility, float foamVisibility)
	{
		EngineApplicationInterface.IScene.AddWaterWakeWithCapsule(this, positionA, radiusA, positionB, radiusB, wakeVisibility, foamVisibility);
	}

	public bool GetPathBetweenAIFaces(UIntPtr startingFace, UIntPtr endingFace, Vec2 startingPosition, Vec2 endingPosition, float agentRadius, NavigationPath path, int[] excludedFaceIds)
	{
		int pathSize = path.PathPoints.Length;
		if (EngineApplicationInterface.IScene.GetPathBetweenAIFacePointers(base.Pointer, startingFace, endingFace, startingPosition, endingPosition, agentRadius, path.PathPoints, ref pathSize, excludedFaceIds, (excludedFaceIds != null) ? excludedFaceIds.Length : 0))
		{
			path.Size = pathSize;
			return true;
		}
		path.Size = 0;
		return false;
	}

	public bool HasNavmeshFaceUnsharedEdges(in PathFaceRecord faceRecord)
	{
		return EngineApplicationInterface.IScene.HasNavmeshFaceUnsharedEdges(base.Pointer, in faceRecord);
	}

	public int GetNavmeshFaceCountBetweenTwoIds(int firstId, int secondId)
	{
		return EngineApplicationInterface.IScene.GetNavmeshFaceCountBetweenTwoIds(base.Pointer, firstId, secondId);
	}

	public void GetNavmeshFaceRecordsBetweenTwoIds(int firstId, int secondId, PathFaceRecord[] faceRecords)
	{
		EngineApplicationInterface.IScene.GetNavmeshFaceRecordsBetweenTwoIds(base.Pointer, firstId, secondId, faceRecords);
	}

	public void SetFixedTickCallbackActive(bool isActive)
	{
		EngineApplicationInterface.IScene.SetFixedTickCallbackActive(this, isActive);
	}

	public void SetOnCollisionFilterCallbackActive(bool isActive)
	{
		EngineApplicationInterface.IScene.SetOnCollisionFilterCallbackActive(this, isActive);
	}

	public bool GetPathBetweenAIFaces(UIntPtr startingFace, UIntPtr endingFace, Vec2 startingPosition, Vec2 endingPosition, float agentRadius, NavigationPath path, int[] excludedFaceIds, int regionSwitchCostTo0, int regionSwitchCostTo1)
	{
		int pathSize = path.PathPoints.Length;
		if (EngineApplicationInterface.IScene.GetPathBetweenAIFacePointersWithRegionSwitchCost(base.Pointer, startingFace, endingFace, startingPosition, endingPosition, agentRadius, path.PathPoints, ref pathSize, excludedFaceIds, (excludedFaceIds != null) ? excludedFaceIds.Length : 0, regionSwitchCostTo0, regionSwitchCostTo1))
		{
			path.Size = pathSize;
			return true;
		}
		path.Size = 0;
		return false;
	}

	public bool GetPathBetweenAIFaces(int startingFace, int endingFace, Vec2 startingPosition, Vec2 endingPosition, float agentRadius, NavigationPath path, int[] excludedFaceIds, float extraCostMultiplier)
	{
		int pathSize = path.PathPoints.Length;
		if (EngineApplicationInterface.IScene.GetPathBetweenAIFaceIndices(base.Pointer, startingFace, endingFace, startingPosition, endingPosition, agentRadius, path.PathPoints, ref pathSize, excludedFaceIds, (excludedFaceIds != null) ? excludedFaceIds.Length : 0, extraCostMultiplier))
		{
			path.Size = pathSize;
			return true;
		}
		path.Size = 0;
		return false;
	}

	public bool GetPathBetweenAIFaces(int startingFace, int endingFace, Vec2 startingPosition, Vec2 endingPosition, float agentRadius, NavigationPath path, int[] excludedFaceIds, float extraCostMultiplier, int regionSwitchCostTo0, int regionSwitchCostTo1)
	{
		int pathSize = path.PathPoints.Length;
		if (EngineApplicationInterface.IScene.GetPathBetweenAIFaceIndicesWithRegionSwitchCost(base.Pointer, startingFace, endingFace, startingPosition, endingPosition, agentRadius, path.PathPoints, ref pathSize, excludedFaceIds, (excludedFaceIds != null) ? excludedFaceIds.Length : 0, extraCostMultiplier, regionSwitchCostTo0, regionSwitchCostTo1))
		{
			path.Size = pathSize;
			return true;
		}
		path.Size = 0;
		return false;
	}

	public bool GetPathDistanceBetweenAIFaces(int startingAiFace, int endingAiFace, Vec2 startingPosition, Vec2 endingPosition, float agentRadius, float distanceLimit, out float distance, int[] excludedFaceIds, int regionSwitchCostTo0, int regionSwitchCostTo1)
	{
		return EngineApplicationInterface.IScene.GetPathDistanceBetweenAIFaces(base.Pointer, startingAiFace, endingAiFace, startingPosition, endingPosition, agentRadius, distanceLimit, out distance, excludedFaceIds, (excludedFaceIds != null) ? excludedFaceIds.Length : 0, regionSwitchCostTo0, regionSwitchCostTo1);
	}

	public void GetNavMeshFaceIndex(ref PathFaceRecord record, Vec2 position, bool isRegion1, bool checkIfDisabled, bool ignoreHeight = false)
	{
		EngineApplicationInterface.IScene.GetNavMeshFaceIndex(base.Pointer, ref record, position, isRegion1, checkIfDisabled, ignoreHeight);
	}

	public void GetNavMeshFaceIndex(ref PathFaceRecord record, Vec3 position, bool checkIfDisabled)
	{
		EngineApplicationInterface.IScene.GetNavMeshFaceIndex3(base.Pointer, ref record, position, checkIfDisabled);
	}

	public static Scene CreateNewScene(bool initialize_physics = true, bool enable_decals = true, DecalAtlasGroup atlasGroup = DecalAtlasGroup.All, string sceneName = "mono_renderscene")
	{
		return EngineApplicationInterface.IScene.CreateNewScene(initialize_physics, enable_decals, (int)atlasGroup, sceneName);
	}

	public void AddAlwaysRenderedSkeleton(Skeleton skeleton)
	{
		EngineApplicationInterface.IScene.AddAlwaysRenderedSkeleton(base.Pointer, skeleton.Pointer);
	}

	public void RemoveAlwaysRenderedSkeleton(Skeleton skeleton)
	{
		EngineApplicationInterface.IScene.RemoveAlwaysRenderedSkeleton(base.Pointer, skeleton.Pointer);
	}

	public MetaMesh CreatePathMesh(string baseEntityName, bool isWaterPath)
	{
		return EngineApplicationInterface.IScene.CreatePathMesh(base.Pointer, baseEntityName, isWaterPath);
	}

	public void SetActiveVisibilityLevels(List<string> levelsToActivate)
	{
		string text = "";
		for (int i = 0; i < levelsToActivate.Count; i++)
		{
			if (!levelsToActivate[i].Contains("$"))
			{
				if (i != 0)
				{
					text += "$";
				}
				text += levelsToActivate[i];
			}
		}
		EngineApplicationInterface.IScene.SetActiveVisibilityLevels(base.Pointer, text);
	}

	public void SetDoNotWaitForLoadingStatesToRender(bool value)
	{
		EngineApplicationInterface.IScene.SetDoNotWaitForLoadingStatesToRender(base.Pointer, value);
	}

	public void SetDynamicSnowTexture(Texture texture)
	{
		EngineApplicationInterface.IScene.SetDynamicSnowTexture(base.Pointer, (texture != null) ? texture.Pointer : UIntPtr.Zero);
	}

	public void GetWindFlowMapData(float[] flowMapData)
	{
		EngineApplicationInterface.IScene.GetWindFlowMapData(base.Pointer, flowMapData);
	}

	public void CreateDynamicRainTexture(int w, int h)
	{
		EngineApplicationInterface.IScene.CreateDynamicRainTexture(base.Pointer, w, h);
	}

	public MetaMesh CreatePathMesh(IList<GameEntity> pathNodes, bool isWaterPath = false)
	{
		return EngineApplicationInterface.IScene.CreatePathMesh2(base.Pointer, pathNodes.Select((GameEntity e) => e.Pointer).ToArray(), pathNodes.Count, isWaterPath);
	}

	public GameEntity GetEntityWithGuid(string guid)
	{
		return EngineApplicationInterface.IScene.GetEntityWithGuid(base.Pointer, guid);
	}

	public bool IsEntityFrameChanged(string containsName)
	{
		return EngineApplicationInterface.IScene.CheckPathEntitiesFrameChanged(base.Pointer, containsName);
	}

	public void GetTerrainHeightAndNormal(Vec2 position, out float height, out Vec3 normal)
	{
		EngineApplicationInterface.IScene.GetTerrainHeightAndNormal(base.Pointer, position, out height, out normal);
	}

	public int GetFloraInstanceCount()
	{
		return EngineApplicationInterface.IScene.GetFloraInstanceCount(base.Pointer);
	}

	public int GetFloraRendererTextureUsage()
	{
		return EngineApplicationInterface.IScene.GetFloraRendererTextureUsage(base.Pointer);
	}

	public int GetTerrainMemoryUsage()
	{
		return EngineApplicationInterface.IScene.GetTerrainMemoryUsage(base.Pointer);
	}

	public void SetFetchCrcInfoOfScene(bool value)
	{
		EngineApplicationInterface.IScene.SetFetchCrcInfoOfScene(base.Pointer, value);
	}

	public uint GetSceneXMLCRC()
	{
		return EngineApplicationInterface.IScene.GetSceneXMLCRC(base.Pointer);
	}

	public uint GetNavigationMeshCRC()
	{
		return EngineApplicationInterface.IScene.GetNavigationMeshCRC(base.Pointer);
	}

	public void SetGlobalWindStrengthVector(in Vec2 windVector)
	{
		EngineApplicationInterface.IScene.SetGlobalWindStrengthVector(base.Pointer, in windVector);
	}

	public Vec2 GetGlobalWindStrengthVector()
	{
		return EngineApplicationInterface.IScene.GetGlobalWindStrengthVector(base.Pointer);
	}

	public Vec2 GetGlobalWindVelocity()
	{
		return EngineApplicationInterface.IScene.GetGlobalWindVelocity(base.Pointer);
	}

	public void SetGlobalWindVelocity(in Vec2 windVector)
	{
		EngineApplicationInterface.IScene.SetGlobalWindVelocity(base.Pointer, in windVector);
	}

	public bool GetEnginePhysicsEnabled()
	{
		return EngineApplicationInterface.IScene.GetEnginePhysicsEnabled(base.Pointer);
	}

	public void ClearNavMesh()
	{
		EngineApplicationInterface.IScene.ClearNavMesh(base.Pointer);
	}

	public void StallLoadingRenderingsUntilFurtherNotice()
	{
		EngineApplicationInterface.IScene.StallLoadingRenderingsUntilFurtherNotice(base.Pointer);
	}

	public int GetNavMeshFaceCount()
	{
		return EngineApplicationInterface.IScene.GetNavMeshFaceCount(base.Pointer);
	}

	public void ResumeLoadingRenderings()
	{
		EngineApplicationInterface.IScene.ResumeLoadingRenderings(base.Pointer);
	}

	public uint GetUpgradeLevelMask()
	{
		return EngineApplicationInterface.IScene.GetUpgradeLevelMask(base.Pointer);
	}

	public void SetUpgradeLevelVisibility(uint mask)
	{
		EngineApplicationInterface.IScene.SetUpgradeLevelVisibilityWithMask(base.Pointer, mask);
	}

	public void SetUpgradeLevelVisibility(List<string> levels)
	{
		string text = "";
		for (int i = 0; i < levels.Count - 1; i++)
		{
			text = text + levels[i] + "|";
		}
		text += levels[levels.Count - 1];
		EngineApplicationInterface.IScene.SetUpgradeLevelVisibility(base.Pointer, text);
	}

	public int GetIdOfNavMeshFace(int faceIndex)
	{
		return EngineApplicationInterface.IScene.GetIdOfNavMeshFace(base.Pointer, faceIndex);
	}

	public void SetClothSimulationState(bool state)
	{
		EngineApplicationInterface.IScene.SetClothSimulationState(base.Pointer, state);
	}

	public void GetNavMeshCenterPosition(int faceIndex, ref Vec3 centerPosition)
	{
		EngineApplicationInterface.IScene.GetNavMeshFaceCenterPosition(base.Pointer, faceIndex, ref centerPosition);
	}

	public PathFaceRecord GetNavMeshPathFaceRecord(int faceIndex)
	{
		return EngineApplicationInterface.IScene.GetNavMeshPathFaceRecord(base.Pointer, faceIndex);
	}

	public PathFaceRecord GetPathFaceRecordFromNavMeshFacePointer(UIntPtr navMeshFacePointer)
	{
		return EngineApplicationInterface.IScene.GetPathFaceRecordFromNavMeshFacePointer(base.Pointer, navMeshFacePointer);
	}

	public void GetAllNavmeshFaceRecords(PathFaceRecord[] faceRecords)
	{
		EngineApplicationInterface.IScene.GetAllNavmeshFaceRecords(base.Pointer, faceRecords);
	}

	public GameEntity GetFirstEntityWithName(string name)
	{
		return EngineApplicationInterface.IScene.GetFirstEntityWithName(base.Pointer, name);
	}

	public GameEntity GetCampaignEntityWithName(string name)
	{
		return EngineApplicationInterface.IScene.GetCampaignEntityWithName(base.Pointer, name);
	}

	public void GetAllEntitiesWithScriptComponent<T>(ref List<GameEntity> entities) where T : ScriptComponentBehavior
	{
		NativeObjectArray nativeObjectArray = NativeObjectArray.Create();
		string name = typeof(T).Name;
		EngineApplicationInterface.IScene.GetAllEntitiesWithScriptComponent(base.Pointer, name, nativeObjectArray.Pointer);
		for (int i = 0; i < nativeObjectArray.Count; i++)
		{
			entities.Add(nativeObjectArray.GetElementAt(i) as GameEntity);
		}
	}

	public GameEntity GetFirstEntityWithScriptComponent<T>() where T : ScriptComponentBehavior
	{
		string name = typeof(T).Name;
		return EngineApplicationInterface.IScene.GetFirstEntityWithScriptComponent(base.Pointer, name);
	}

	public GameEntity GetFirstEntityWithScriptComponent(string scriptName)
	{
		return EngineApplicationInterface.IScene.GetFirstEntityWithScriptComponent(base.Pointer, scriptName);
	}

	public uint GetUpgradeLevelMaskOfLevelName(string levelName)
	{
		return EngineApplicationInterface.IScene.GetUpgradeLevelMaskOfLevelName(base.Pointer, levelName);
	}

	public string GetUpgradeLevelNameOfIndex(int index)
	{
		return EngineApplicationInterface.IScene.GetUpgradeLevelNameOfIndex(base.Pointer, index);
	}

	public int GetUpgradeLevelCount()
	{
		return EngineApplicationInterface.IScene.GetUpgradeLevelCount(base.Pointer);
	}

	public float GetWinterTimeFactor()
	{
		return EngineApplicationInterface.IScene.GetWinterTimeFactor(base.Pointer);
	}

	public float GetNavMeshFaceFirstVertexZ(int faceIndex)
	{
		return EngineApplicationInterface.IScene.GetNavMeshFaceFirstVertexZ(base.Pointer, faceIndex);
	}

	public void SetWinterTimeFactor(float winterTimeFactor)
	{
		EngineApplicationInterface.IScene.SetWinterTimeFactor(base.Pointer, winterTimeFactor);
	}

	public void SetDrynessFactor(float drynessFactor)
	{
		EngineApplicationInterface.IScene.SetDrynessFactor(base.Pointer, drynessFactor);
	}

	public float GetFog()
	{
		return EngineApplicationInterface.IScene.GetFog(base.Pointer);
	}

	public void SetFog(float fogDensity, ref Vec3 fogColor, float fogFalloff)
	{
		EngineApplicationInterface.IScene.SetFog(base.Pointer, fogDensity, ref fogColor, fogFalloff);
	}

	public void SetFogAdvanced(float fogFalloffOffset, float fogFalloffMinFog, float fogFalloffStartDist)
	{
		EngineApplicationInterface.IScene.SetFogAdvanced(base.Pointer, fogFalloffOffset, fogFalloffMinFog, fogFalloffStartDist);
	}

	public void SetFogAmbientColor(ref Vec3 fogAmbientColor)
	{
		EngineApplicationInterface.IScene.SetFogAmbientColor(base.Pointer, ref fogAmbientColor);
	}

	public void SetTemperature(float temperature)
	{
		EngineApplicationInterface.IScene.SetTemperature(base.Pointer, temperature);
	}

	public void SetHumidity(float humidity)
	{
		EngineApplicationInterface.IScene.SetHumidity(base.Pointer, humidity);
	}

	public void SetDynamicShadowmapCascadesRadiusMultiplier(float multiplier)
	{
		EngineApplicationInterface.IScene.SetDynamicShadowmapCascadesRadiusMultiplier(base.Pointer, multiplier);
	}

	public void SetEnvironmentMultiplier(bool useMultiplier, float multiplier)
	{
		EngineApplicationInterface.IScene.SetEnvironmentMultiplier(base.Pointer, useMultiplier, multiplier);
	}

	public void SetSkyRotation(float rotation)
	{
		EngineApplicationInterface.IScene.SetSkyRotation(base.Pointer, rotation);
	}

	public void SetSkyBrightness(float brightness)
	{
		EngineApplicationInterface.IScene.SetSkyBrightness(base.Pointer, brightness);
	}

	public void SetForcedSnow(bool value)
	{
		EngineApplicationInterface.IScene.SetForcedSnow(base.Pointer, value);
	}

	public void SetSunLight(ref Vec3 color, ref Vec3 direction)
	{
		EngineApplicationInterface.IScene.SetSunLight(base.Pointer, color, direction);
	}

	public void SetSunDirection(ref Vec3 direction)
	{
		EngineApplicationInterface.IScene.SetSunDirection(base.Pointer, direction);
	}

	public void SetSun(ref Vec3 color, float altitude, float angle, float intensity)
	{
		EngineApplicationInterface.IScene.SetSun(base.Pointer, color, altitude, angle, intensity);
	}

	public void SetSunAngleAltitude(float angle, float altitude)
	{
		EngineApplicationInterface.IScene.SetSunAngleAltitude(base.Pointer, angle, altitude);
	}

	public void SetSunSize(float size)
	{
		EngineApplicationInterface.IScene.SetSunSize(base.Pointer, size);
	}

	public void SetSunShaftStrength(float strength)
	{
		EngineApplicationInterface.IScene.SetSunShaftStrength(base.Pointer, strength);
	}

	public float GetRainDensity()
	{
		return EngineApplicationInterface.IScene.GetRainDensity(base.Pointer);
	}

	public void SetRainDensity(float density)
	{
		EngineApplicationInterface.IScene.SetRainDensity(base.Pointer, density);
	}

	public float GetSnowDensity()
	{
		return EngineApplicationInterface.IScene.GetSnowDensity(base.Pointer);
	}

	public void SetSnowDensity(float density)
	{
		EngineApplicationInterface.IScene.SetSnowDensity(base.Pointer, density);
	}

	public void AddDecalInstance(Decal decal, string decalSetID, bool deletable)
	{
		EngineApplicationInterface.IScene.AddDecalInstance(base.Pointer, decal.Pointer, decalSetID, deletable);
	}

	public void RemoveDecalInstance(Decal decal, string decalSetID)
	{
		EngineApplicationInterface.IScene.RemoveDecalInstance(base.Pointer, decal.Pointer, decalSetID);
	}

	public void SetShadow(bool shadowEnabled)
	{
		EngineApplicationInterface.IScene.SetShadow(base.Pointer, shadowEnabled);
	}

	public int AddPointLight(ref Vec3 position, float radius)
	{
		return EngineApplicationInterface.IScene.AddPointLight(base.Pointer, position, radius);
	}

	public int AddDirectionalLight(ref Vec3 position, ref Vec3 direction, float radius)
	{
		return EngineApplicationInterface.IScene.AddDirectionalLight(base.Pointer, position, direction, radius);
	}

	public void SetLightPosition(int lightIndex, ref Vec3 position)
	{
		EngineApplicationInterface.IScene.SetLightPosition(base.Pointer, lightIndex, position);
	}

	public void SetLightDiffuseColor(int lightIndex, ref Vec3 diffuseColor)
	{
		EngineApplicationInterface.IScene.SetLightDiffuseColor(base.Pointer, lightIndex, diffuseColor);
	}

	public void SetLightDirection(int lightIndex, ref Vec3 direction)
	{
		EngineApplicationInterface.IScene.SetLightDirection(base.Pointer, lightIndex, direction);
	}

	public void SetMieScatterFocus(float strength)
	{
		EngineApplicationInterface.IScene.SetMieScatterFocus(base.Pointer, strength);
	}

	public void SetMieScatterStrength(float strength)
	{
		EngineApplicationInterface.IScene.SetMieScatterStrength(base.Pointer, strength);
	}

	public void SetBrightpassThreshold(float threshold)
	{
		EngineApplicationInterface.IScene.SetBrightpassTreshold(base.Pointer, threshold);
	}

	public void SetLensDistortion(float amount)
	{
		EngineApplicationInterface.IScene.SetLensDistortion(base.Pointer, amount);
	}

	public void SetHexagonVignetteAlpha(float amount)
	{
		EngineApplicationInterface.IScene.SetHexagonVignetteAlpha(base.Pointer, amount);
	}

	public void SetMinExposure(float minExposure)
	{
		EngineApplicationInterface.IScene.SetMinExposure(base.Pointer, minExposure);
	}

	public void SetMaxExposure(float maxExposure)
	{
		EngineApplicationInterface.IScene.SetMaxExposure(base.Pointer, maxExposure);
	}

	public void SetTargetExposure(float targetExposure)
	{
		EngineApplicationInterface.IScene.SetTargetExposure(base.Pointer, targetExposure);
	}

	public void SetMiddleGray(float middleGray)
	{
		EngineApplicationInterface.IScene.SetMiddleGray(base.Pointer, middleGray);
	}

	public void SetBloomStrength(float bloomStrength)
	{
		EngineApplicationInterface.IScene.SetBloomStrength(base.Pointer, bloomStrength);
	}

	public void SetBloomAmount(float bloomAmount)
	{
		EngineApplicationInterface.IScene.SetBloomAmount(base.Pointer, bloomAmount);
	}

	public void SetGrainAmount(float grainAmount)
	{
		EngineApplicationInterface.IScene.SetGrainAmount(base.Pointer, grainAmount);
	}

	public GameEntity AddItemEntity(ref MatrixFrame placementFrame, MetaMesh metaMesh)
	{
		return EngineApplicationInterface.IScene.AddItemEntity(base.Pointer, ref placementFrame, metaMesh.Pointer);
	}

	public void RemoveEntity(GameEntity entity, int removeReason)
	{
		EngineApplicationInterface.IScene.RemoveEntity(base.Pointer, entity.Pointer, removeReason);
	}

	public void RemoveEntity(WeakGameEntity entity, int removeReason)
	{
		EngineApplicationInterface.IScene.RemoveEntity(base.Pointer, entity.Pointer, removeReason);
	}

	public bool AttachEntity(GameEntity entity, bool showWarnings = false)
	{
		return EngineApplicationInterface.IScene.AttachEntity(base.Pointer, entity.Pointer, showWarnings);
	}

	public bool AttachEntity(WeakGameEntity entity, bool showWarnings = false)
	{
		return EngineApplicationInterface.IScene.AttachEntity(base.Pointer, entity.Pointer, showWarnings);
	}

	public void AddEntityWithMesh(Mesh mesh, ref MatrixFrame frame)
	{
		EngineApplicationInterface.IScene.AddEntityWithMesh(base.Pointer, mesh.Pointer, ref frame);
	}

	public void AddEntityWithMultiMesh(MetaMesh mesh, ref MatrixFrame frame)
	{
		EngineApplicationInterface.IScene.AddEntityWithMultiMesh(base.Pointer, mesh.Pointer, ref frame);
	}

	public void Tick(float dt)
	{
		EngineApplicationInterface.IScene.Tick(base.Pointer, dt);
	}

	public void ClearAll()
	{
		EngineApplicationInterface.IScene.ClearAll(base.Pointer);
	}

	public void SetDefaultLighting()
	{
		Vec3 color = new Vec3(1.15f, 1.2f, 1.25f);
		Vec3 direction = new Vec3(1f, -1f, -1f);
		direction.Normalize();
		SetSunLight(ref color, ref direction);
		SetShadow(shadowEnabled: false);
	}

	public bool CalculateEffectiveLighting()
	{
		return EngineApplicationInterface.IScene.CalculateEffectiveLighting(base.Pointer);
	}

	public bool GetPathDistanceBetweenPositions(ref WorldPosition point0, ref WorldPosition point1, float agentRadius, out float pathDistance)
	{
		pathDistance = 0f;
		return EngineApplicationInterface.IScene.GetPathDistanceBetweenPositions(base.Pointer, ref point0, ref point1, agentRadius, ref pathDistance);
	}

	public bool IsLineToPointClear(ref WorldPosition position, ref WorldPosition destination, float agentRadius)
	{
		return EngineApplicationInterface.IScene.IsLineToPointClear2(base.Pointer, position.GetNavMesh(), position.AsVec2, destination.AsVec2, agentRadius);
	}

	public bool IsLineToPointClear(UIntPtr startingFace, Vec2 position, Vec2 destination, float agentRadius)
	{
		return EngineApplicationInterface.IScene.IsLineToPointClear2(base.Pointer, startingFace, position, destination, agentRadius);
	}

	public bool IsLineToPointClear(int startingFace, Vec2 position, Vec2 destination, float agentRadius)
	{
		return EngineApplicationInterface.IScene.IsLineToPointClear(base.Pointer, startingFace, position, destination, agentRadius);
	}

	public Vec2 GetLastPointOnNavigationMeshFromPositionToDestination(int startingFace, Vec2 position, Vec2 destination, int[] excludedFaceIds)
	{
		return EngineApplicationInterface.IScene.GetLastPointOnNavigationMeshFromPositionToDestination(base.Pointer, startingFace, position, destination, excludedFaceIds, (excludedFaceIds != null) ? excludedFaceIds.Length : 0);
	}

	public Vec2 GetLastPositionOnNavMeshFaceForPointAndDirection(PathFaceRecord record, Vec2 position, Vec2 destination)
	{
		return EngineApplicationInterface.IScene.GetLastPositionOnNavMeshFaceForPointAndDirection(base.Pointer, in record, position, destination);
	}

	public Vec3 GetLastPointOnNavigationMeshFromWorldPositionToDestination(ref WorldPosition position, Vec2 destination)
	{
		return EngineApplicationInterface.IScene.GetLastPointOnNavigationMeshFromWorldPositionToDestination(base.Pointer, ref position, destination);
	}

	public bool DoesPathExistBetweenFaces(int firstNavMeshFace, int secondNavMeshFace, bool ignoreDisabled)
	{
		return EngineApplicationInterface.IScene.DoesPathExistBetweenFaces(base.Pointer, firstNavMeshFace, secondNavMeshFace, ignoreDisabled);
	}

	public bool GetHeightAtPoint(Vec2 point, BodyFlags excludeBodyFlags, ref float height)
	{
		return EngineApplicationInterface.IScene.GetHeightAtPoint(base.Pointer, point, excludeBodyFlags, ref height);
	}

	public Vec3 GetNormalAt(Vec2 position)
	{
		return EngineApplicationInterface.IScene.GetNormalAt(base.Pointer, position);
	}

	public void GetEntities(ref List<GameEntity> entities)
	{
		NativeObjectArray nativeObjectArray = NativeObjectArray.Create();
		EngineApplicationInterface.IScene.GetEntities(base.Pointer, nativeObjectArray.Pointer);
		for (int i = 0; i < nativeObjectArray.Count; i++)
		{
			entities.Add(nativeObjectArray.GetElementAt(i) as GameEntity);
		}
	}

	public void GetRootEntities(NativeObjectArray entities)
	{
		EngineApplicationInterface.IScene.GetRootEntities(this, entities);
	}

	public int SelectEntitiesInBoxWithScriptComponent<T>(ref Vec3 boundingBoxMin, ref Vec3 boundingBoxMax, WeakGameEntity[] entitiesOutput, UIntPtr[] entityIds, bool isFixedTick) where T : ScriptComponentBehavior
	{
		string name = typeof(T).Name;
		int num = EngineApplicationInterface.IScene.SelectEntitiesInBoxWithScriptComponent(base.Pointer, ref boundingBoxMin, ref boundingBoxMax, entityIds, entitiesOutput.Length, name, isFixedTick);
		for (int i = 0; i < num; i++)
		{
			entitiesOutput[i] = new WeakGameEntity(entityIds[i]);
		}
		return num;
	}

	public int SelectEntitiesCollidedWith(ref Ray ray, Intersection[] intersectionsOutput, UIntPtr[] entityIds)
	{
		return EngineApplicationInterface.IScene.SelectEntitiesCollidedWith(base.Pointer, ref ray, entityIds, intersectionsOutput);
	}

	public bool RayCastExcludingTwoEntities(BodyFlags flags, in Ray ray, WeakGameEntity entity1, WeakGameEntity entity2)
	{
		return EngineApplicationInterface.IScene.RayCastExcludingTwoEntities(flags, base.Pointer, in ray, entity1.Pointer, entity2.Pointer);
	}

	public int GenerateContactsWithCapsule(ref CapsuleData capsule, BodyFlags exclude_flags, bool isFixedTick, Intersection[] intersectionsOutput, WeakGameEntity[] gameEntities, UIntPtr[] entityPointers)
	{
		int num = EngineApplicationInterface.IScene.GenerateContactsWithCapsule(base.Pointer, ref capsule, exclude_flags, isFixedTick, intersectionsOutput, entityPointers);
		for (int i = 0; i < num; i++)
		{
			if (entityPointers[i] != UIntPtr.Zero)
			{
				gameEntities[i] = new WeakGameEntity(entityPointers[i]);
			}
			else
			{
				gameEntities[i] = WeakGameEntity.Invalid;
			}
		}
		return num;
	}

	public int GenerateContactsWithCapsuleAgainstEntity(ref CapsuleData capsule, BodyFlags excludeFlags, WeakGameEntity entity, Intersection[] intersectionsOutput)
	{
		return EngineApplicationInterface.IScene.GenerateContactsWithCapsuleAgainstEntity(base.Pointer, ref capsule, excludeFlags, entity.Pointer, intersectionsOutput);
	}

	public void InvalidateTerrainPhysicsMaterials()
	{
		EngineApplicationInterface.IScene.InvalidateTerrainPhysicsMaterials(base.Pointer);
	}

	public void Read(string sceneName)
	{
		SceneInitializationData initData = new SceneInitializationData(initializeWithDefaults: true);
		EngineApplicationInterface.IScene.Read(base.Pointer, sceneName, ref initData, "");
	}

	public void Read(string sceneName, string moduleId, ref SceneInitializationData initData, string forcedAtmoName = "")
	{
		EngineApplicationInterface.IScene.ReadInModule(base.Pointer, sceneName, moduleId, ref initData, forcedAtmoName);
	}

	public void Read(string sceneName, ref SceneInitializationData initData, string forcedAtmoName = "")
	{
		EngineApplicationInterface.IScene.Read(base.Pointer, sceneName, ref initData, forcedAtmoName);
	}

	public MatrixFrame ReadAndCalculateInitialCamera()
	{
		MatrixFrame outFrame = default(MatrixFrame);
		EngineApplicationInterface.IScene.ReadAndCalculateInitialCamera(base.Pointer, ref outFrame);
		return outFrame;
	}

	public void OptimizeScene(bool optimizeFlora = true, bool optimizeOro = false)
	{
		EngineApplicationInterface.IScene.OptimizeScene(base.Pointer, optimizeFlora, optimizeOro);
	}

	public float GetTerrainHeight(Vec2 position, bool checkHoles = true)
	{
		return EngineApplicationInterface.IScene.GetTerrainHeight(base.Pointer, position, checkHoles);
	}

	public void CheckResources(bool checkInvisibleEntities)
	{
		EngineApplicationInterface.IScene.CheckResources(base.Pointer, checkInvisibleEntities);
	}

	public void ForceLoadResources(bool checkInvisibleEntities)
	{
		EngineApplicationInterface.IScene.ForceLoadResources(base.Pointer, checkInvisibleEntities);
	}

	public void SetDepthOfFieldParameters(float depthOfFieldFocusStart, float depthOfFieldFocusEnd, bool isVignetteOn)
	{
		EngineApplicationInterface.IScene.SetDofParams(base.Pointer, depthOfFieldFocusStart, depthOfFieldFocusEnd, isVignetteOn);
	}

	public void SetDepthOfFieldFocus(float depthOfFieldFocus)
	{
		EngineApplicationInterface.IScene.SetDofFocus(base.Pointer, depthOfFieldFocus);
	}

	public void ResetDepthOfFieldParams()
	{
		EngineApplicationInterface.IScene.SetDofFocus(base.Pointer, 0f);
		EngineApplicationInterface.IScene.SetDofParams(base.Pointer, 0f, 0f, isVignetteOn: true);
	}

	public void PreloadForRendering()
	{
		EngineApplicationInterface.IScene.PreloadForRendering(base.Pointer);
	}

	public void SetColorGradeBlend(string texture1, string texture2, float alpha)
	{
		EngineApplicationInterface.IScene.SetColorGradeBlend(base.Pointer, texture1, texture2, alpha);
	}

	public float GetGroundHeightAtPosition(Vec3 position, BodyFlags excludeFlags = BodyFlags.CommonCollisionExcludeFlags)
	{
		return EngineApplicationInterface.IScene.GetGroundHeightAtPosition(base.Pointer, position, (uint)excludeFlags);
	}

	public float GetGroundHeightAndBodyFlagsAtPosition(Vec3 position, out BodyFlags contactPointFlags, BodyFlags excludeFlags = BodyFlags.CommonCollisionExcludeFlags)
	{
		return EngineApplicationInterface.IScene.GetGroundHeightAndBodyFlagsAtPosition(base.Pointer, position, out contactPointFlags, excludeFlags);
	}

	public float GetGroundHeightAtPosition(Vec3 position, out Vec3 normal, BodyFlags excludeFlags = BodyFlags.CommonCollisionExcludeFlags)
	{
		normal = Vec3.Invalid;
		return EngineApplicationInterface.IScene.GetGroundHeightAtPosition(base.Pointer, position, (uint)excludeFlags);
	}

	public void PauseSceneSounds()
	{
		EngineApplicationInterface.IScene.PauseSceneSounds(base.Pointer);
	}

	public void ResumeSceneSounds()
	{
		EngineApplicationInterface.IScene.ResumeSceneSounds(base.Pointer);
	}

	public void FinishSceneSounds()
	{
		EngineApplicationInterface.IScene.FinishSceneSounds(base.Pointer);
	}

	public bool BoxCastOnlyForCamera(Vec3[] boxPoints, in Vec3 centerPoint, bool castSupportRay, in Vec3 supportRaycastPoint, in Vec3 dir, float distance, WeakGameEntity ignoredEntity, out float collisionDistance, out Vec3 closestPoint, out WeakGameEntity collidedEntity, BodyFlags excludedBodyFlags = BodyFlags.CameraCollisionRayCastExludeFlags | BodyFlags.DontCollideWithCamera)
	{
		collisionDistance = float.NaN;
		closestPoint = Vec3.Invalid;
		UIntPtr entityIndex = UIntPtr.Zero;
		bool flag = castSupportRay && EngineApplicationInterface.IScene.RayCastForClosestEntityOrTerrainIgnoreEntity(base.Pointer, in supportRaycastPoint, in centerPoint, 0f, ref collisionDistance, ref closestPoint, ref entityIndex, excludedBodyFlags, ignoredEntity.Pointer);
		if (!flag)
		{
			flag = EngineApplicationInterface.IScene.BoxCastOnlyForCamera(base.Pointer, boxPoints, in centerPoint, in dir, distance, ignoredEntity.Pointer, ref collisionDistance, ref closestPoint, ref entityIndex, excludedBodyFlags);
		}
		if (flag && entityIndex != UIntPtr.Zero)
		{
			collidedEntity = new WeakGameEntity(entityIndex);
		}
		else
		{
			collidedEntity = WeakGameEntity.Invalid;
		}
		return flag;
	}

	public bool BoxCast(Vec3 boxMin, Vec3 boxMax, bool castSupportRay, Vec3 supportRaycastPoint, Vec3 dir, float distance, out float collisionDistance, out Vec3 closestPoint, out WeakGameEntity collidedEntity, BodyFlags excludedBodyFlags = BodyFlags.CameraCollisionRayCastExludeFlags)
	{
		collisionDistance = float.NaN;
		closestPoint = Vec3.Invalid;
		UIntPtr entityIndex = UIntPtr.Zero;
		Vec3 targetPoint = (boxMin + boxMax) * 0.5f;
		bool flag = castSupportRay && EngineApplicationInterface.IScene.RayCastForClosestEntityOrTerrain(base.Pointer, in supportRaycastPoint, in targetPoint, 0f, ref collisionDistance, ref closestPoint, ref entityIndex, excludedBodyFlags, isFixedWorld: false);
		if (!flag)
		{
			flag = EngineApplicationInterface.IScene.BoxCast(base.Pointer, ref boxMin, ref boxMax, ref dir, distance, ref collisionDistance, ref closestPoint, ref entityIndex, excludedBodyFlags);
		}
		if (flag && entityIndex != UIntPtr.Zero)
		{
			collidedEntity = new WeakGameEntity(entityIndex);
		}
		else
		{
			collidedEntity = WeakGameEntity.Invalid;
		}
		return flag;
	}

	public bool RayCastForClosestEntityOrTerrain(Vec3 sourcePoint, Vec3 targetPoint, out float collisionDistance, out Vec3 closestPoint, out WeakGameEntity collidedEntity, float rayThickness = 0.01f, BodyFlags excludeBodyFlags = BodyFlags.CommonFocusRayCastExcludeFlags)
	{
		collisionDistance = float.NaN;
		closestPoint = Vec3.Invalid;
		UIntPtr entityIndex = UIntPtr.Zero;
		bool num = EngineApplicationInterface.IScene.RayCastForClosestEntityOrTerrain(base.Pointer, in sourcePoint, in targetPoint, rayThickness, ref collisionDistance, ref closestPoint, ref entityIndex, excludeBodyFlags, isFixedWorld: false);
		if (num && entityIndex != UIntPtr.Zero)
		{
			collidedEntity = new WeakGameEntity(entityIndex);
			return num;
		}
		collidedEntity = WeakGameEntity.Invalid;
		return num;
	}

	public bool RayCastForClosestEntityOrTerrainFixedPhysics(Vec3 sourcePoint, Vec3 targetPoint, out float collisionDistance, out Vec3 closestPoint, out WeakGameEntity collidedEntity, float rayThickness = 0.01f, BodyFlags excludeBodyFlags = BodyFlags.CommonFocusRayCastExcludeFlags)
	{
		collisionDistance = float.NaN;
		closestPoint = Vec3.Invalid;
		UIntPtr entityIndex = UIntPtr.Zero;
		bool num = EngineApplicationInterface.IScene.RayCastForClosestEntityOrTerrain(base.Pointer, in sourcePoint, in targetPoint, rayThickness, ref collisionDistance, ref closestPoint, ref entityIndex, excludeBodyFlags, isFixedWorld: true);
		if (num && entityIndex != UIntPtr.Zero)
		{
			collidedEntity = new WeakGameEntity(entityIndex);
			return num;
		}
		collidedEntity = WeakGameEntity.Invalid;
		return num;
	}

	public bool FocusRayCastForFixedPhysics(Vec3 sourcePoint, Vec3 targetPoint, out float collisionDistance, out Vec3 closestPoint, out WeakGameEntity collidedEntity, float rayThickness = 0.01f, BodyFlags excludeBodyFlags = BodyFlags.CommonFocusRayCastExcludeFlags)
	{
		collisionDistance = float.NaN;
		closestPoint = Vec3.Invalid;
		UIntPtr entityIndex = UIntPtr.Zero;
		bool num = EngineApplicationInterface.IScene.FocusRayCastForFixedPhysics(base.Pointer, in sourcePoint, in targetPoint, rayThickness, ref collisionDistance, ref closestPoint, ref entityIndex, excludeBodyFlags, isFixedWorld: true);
		if (num && entityIndex != UIntPtr.Zero)
		{
			collidedEntity = new WeakGameEntity(entityIndex);
			return num;
		}
		collidedEntity = WeakGameEntity.Invalid;
		return num;
	}

	public bool RayCastForClosestEntityOrTerrain(Vec3 sourcePoint, Vec3 targetPoint, out float collisionDistance, out WeakGameEntity collidedEntity, float rayThickness = 0.01f, BodyFlags excludeBodyFlags = BodyFlags.CommonFocusRayCastExcludeFlags)
	{
		Vec3 closestPoint;
		return RayCastForClosestEntityOrTerrain(sourcePoint, targetPoint, out collisionDistance, out closestPoint, out collidedEntity, rayThickness, excludeBodyFlags);
	}

	public bool RayCastForClosestEntityOrTerrainFixedPhysics(Vec3 sourcePoint, Vec3 targetPoint, out float collisionDistance, out WeakGameEntity collidedEntity, float rayThickness = 0.01f, BodyFlags excludeBodyFlags = BodyFlags.CommonFocusRayCastExcludeFlags)
	{
		Vec3 closestPoint;
		return RayCastForClosestEntityOrTerrainFixedPhysics(sourcePoint, targetPoint, out collisionDistance, out closestPoint, out collidedEntity, rayThickness, excludeBodyFlags);
	}

	public bool RayCastForRamming(in Vec3 sourcePoint, in Vec3 targetPoint, WeakGameEntity ignoredEntity, float rayThickness, out float collisionDistance, out Vec3 intersectionPoint, out WeakGameEntity collidedEntity, BodyFlags excludeBodyFlags = BodyFlags.CommonFocusRayCastExcludeFlags, BodyFlags includeBodyFlags = BodyFlags.None)
	{
		collisionDistance = float.NaN;
		intersectionPoint = Vec3.Invalid;
		UIntPtr intersectedEntityIndex = UIntPtr.Zero;
		bool num = EngineApplicationInterface.IScene.RayCastForRamming(base.Pointer, in sourcePoint, in targetPoint, rayThickness, ref collisionDistance, ref intersectionPoint, ref intersectedEntityIndex, excludeBodyFlags, includeBodyFlags, ignoredEntity.Pointer);
		if (num && intersectedEntityIndex != UIntPtr.Zero)
		{
			collidedEntity = new WeakGameEntity(intersectedEntityIndex);
			return num;
		}
		collidedEntity = WeakGameEntity.Invalid;
		return num;
	}

	public bool RayCastForClosestEntityOrTerrainIgnoreEntity(in Vec3 sourcePoint, in Vec3 targetPoint, WeakGameEntity ignoredEntity, out float collisionDistance, out GameEntity collidedEntity, float rayThickness = 0.01f, BodyFlags excludeBodyFlags = BodyFlags.CommonFocusRayCastExcludeFlags)
	{
		Vec3 closestPoint = Vec3.Invalid;
		UIntPtr entityIndex = UIntPtr.Zero;
		collisionDistance = float.NaN;
		bool num = EngineApplicationInterface.IScene.RayCastForClosestEntityOrTerrainIgnoreEntity(base.Pointer, in sourcePoint, in targetPoint, rayThickness, ref collisionDistance, ref closestPoint, ref entityIndex, excludeBodyFlags, ignoredEntity.Pointer);
		if (num && entityIndex != UIntPtr.Zero)
		{
			collidedEntity = new GameEntity(entityIndex);
			return num;
		}
		collidedEntity = null;
		return num;
	}

	public bool RayCastForClosestEntityOrTerrain(Vec3 sourcePoint, Vec3 targetPoint, out float collisionDistance, out Vec3 closestPoint, float rayThickness = 0.01f, BodyFlags excludeBodyFlags = BodyFlags.CommonFocusRayCastExcludeFlags)
	{
		collisionDistance = float.NaN;
		closestPoint = Vec3.Invalid;
		UIntPtr entityIndex = UIntPtr.Zero;
		return EngineApplicationInterface.IScene.RayCastForClosestEntityOrTerrain(base.Pointer, in sourcePoint, in targetPoint, rayThickness, ref collisionDistance, ref closestPoint, ref entityIndex, excludeBodyFlags, isFixedWorld: false);
	}

	public bool RayCastForClosestEntityOrTerrainFixedPhysics(Vec3 sourcePoint, Vec3 targetPoint, out float collisionDistance, out Vec3 closestPoint, float rayThickness = 0.01f, BodyFlags excludeBodyFlags = BodyFlags.CommonFocusRayCastExcludeFlags)
	{
		collisionDistance = float.NaN;
		closestPoint = Vec3.Invalid;
		UIntPtr entityIndex = UIntPtr.Zero;
		return EngineApplicationInterface.IScene.RayCastForClosestEntityOrTerrain(base.Pointer, in sourcePoint, in targetPoint, rayThickness, ref collisionDistance, ref closestPoint, ref entityIndex, excludeBodyFlags, isFixedWorld: true);
	}

	public bool RayCastForClosestEntityOrTerrainFixedPhysics(Vec3 sourcePoint, Vec3 targetPoint, out float collisionDistance, float rayThickness = 0.01f, BodyFlags excludeBodyFlags = BodyFlags.CommonFocusRayCastExcludeFlags)
	{
		Vec3 closestPoint;
		return RayCastForClosestEntityOrTerrainFixedPhysics(sourcePoint, targetPoint, out collisionDistance, out closestPoint, rayThickness, excludeBodyFlags);
	}

	public bool RayCastForClosestEntityOrTerrain(Vec3 sourcePoint, Vec3 targetPoint, out float collisionDistance, float rayThickness = 0.01f, BodyFlags excludeBodyFlags = BodyFlags.CommonFocusRayCastExcludeFlags)
	{
		Vec3 closestPoint;
		return RayCastForClosestEntityOrTerrain(sourcePoint, targetPoint, out collisionDistance, out closestPoint, rayThickness, excludeBodyFlags);
	}

	public void ImportNavigationMeshPrefab(string navMeshPrefabName, int navMeshGroupShift)
	{
		EngineApplicationInterface.IScene.LoadNavMeshPrefab(base.Pointer, navMeshPrefabName, navMeshGroupShift);
	}

	public void ImportNavigationMeshPrefabWithFrame(string navMeshPrefabName, MatrixFrame frame)
	{
		EngineApplicationInterface.IScene.LoadNavMeshPrefabWithFrame(base.Pointer, navMeshPrefabName, frame);
	}

	public void SaveNavMeshPrefabWithFrame(string navMeshPrefabName, MatrixFrame frame)
	{
		EngineApplicationInterface.IScene.SaveNavMeshPrefabWithFrame(base.Pointer, navMeshPrefabName, frame);
	}

	public void SetNavMeshRegionMap(bool[] regionMap)
	{
		EngineApplicationInterface.IScene.SetNavMeshRegionMap(base.Pointer, regionMap, regionMap.Length);
	}

	public void MarkFacesWithIdAsLadder(int faceGroupId, bool isLadder)
	{
		EngineApplicationInterface.IScene.MarkFacesWithIdAsLadder(base.Pointer, faceGroupId, isLadder);
	}

	public int SetAbilityOfFacesWithId(int faceGroupId, bool isEnabled)
	{
		return EngineApplicationInterface.IScene.SetAbilityOfFacesWithId(base.Pointer, faceGroupId, isEnabled);
	}

	public void SetBlockerDirectionForFacesWithId(int faceGroupId, float rotation)
	{
		EngineApplicationInterface.IScene.SetBlockerDirectionForFacesWithId(base.Pointer, faceGroupId, rotation);
	}

	public bool SwapFaceConnectionsWithID(int hubFaceGroupID, int toBeSeparatedFaceGroupId, int toBeMergedFaceGroupId, bool canFail)
	{
		return EngineApplicationInterface.IScene.SwapFaceConnectionsWithId(base.Pointer, hubFaceGroupID, toBeSeparatedFaceGroupId, toBeMergedFaceGroupId, canFail);
	}

	public void MergeFacesWithId(int faceGroupId0, int faceGroupId1, int newFaceGroupId)
	{
		EngineApplicationInterface.IScene.MergeFacesWithId(base.Pointer, faceGroupId0, faceGroupId1, newFaceGroupId);
	}

	public void SeparateFacesWithId(int faceGroupId0, int faceGroupId1)
	{
		EngineApplicationInterface.IScene.SeparateFacesWithId(base.Pointer, faceGroupId0, faceGroupId1);
	}

	public bool IsAnyFaceWithId(int faceGroupId)
	{
		return EngineApplicationInterface.IScene.IsAnyFaceWithId(base.Pointer, faceGroupId);
	}

	public UIntPtr GetNavigationMeshForPosition(in Vec3 position)
	{
		int faceGroupId;
		return GetNavigationMeshForPosition(in position, out faceGroupId, 1.5f, excludeDynamicNavigationMeshes: false);
	}

	public UIntPtr GetNearestNavigationMeshForPosition(in Vec3 position, float heightDifferenceLimit, bool excludeDynamicNavigationMeshes)
	{
		return EngineApplicationInterface.IScene.GetNearestNavigationMeshForPosition(base.Pointer, in position, heightDifferenceLimit, excludeDynamicNavigationMeshes);
	}

	public UIntPtr GetNavigationMeshForPosition(in Vec3 position, out int faceGroupId, float heightDifferenceLimit, bool excludeDynamicNavigationMeshes)
	{
		faceGroupId = int.MinValue;
		return EngineApplicationInterface.IScene.GetNavigationMeshForPosition(base.Pointer, in position, ref faceGroupId, heightDifferenceLimit, excludeDynamicNavigationMeshes);
	}

	public bool DoesPathExistBetweenPositions(WorldPosition position, WorldPosition destination)
	{
		return EngineApplicationInterface.IScene.DoesPathExistBetweenPositions(base.Pointer, position, destination);
	}

	public void SetLandscapeRainMaskData(byte[] data)
	{
		EngineApplicationInterface.IScene.SetLandscapeRainMaskData(base.Pointer, data);
	}

	public void EnsurePostfxSystem()
	{
		EngineApplicationInterface.IScene.EnsurePostfxSystem(base.Pointer);
	}

	public void SetBloom(bool mode)
	{
		EngineApplicationInterface.IScene.SetBloom(base.Pointer, mode);
	}

	public void SetDofMode(bool mode)
	{
		EngineApplicationInterface.IScene.SetDofMode(base.Pointer, mode);
	}

	public void SetOcclusionMode(bool mode)
	{
		EngineApplicationInterface.IScene.SetOcclusionMode(base.Pointer, mode);
	}

	public void SetExternalInjectionTexture(Texture texture)
	{
		EngineApplicationInterface.IScene.SetExternalInjectionTexture(base.Pointer, texture.Pointer);
	}

	public void SetSunshaftMode(bool mode)
	{
		EngineApplicationInterface.IScene.SetSunshaftMode(base.Pointer, mode);
	}

	public Vec3 GetSunDirection()
	{
		return EngineApplicationInterface.IScene.GetSunDirection(base.Pointer);
	}

	public float GetNorthAngle()
	{
		return EngineApplicationInterface.IScene.GetNorthAngle(base.Pointer);
	}

	public float GetNorthRotation()
	{
		float northAngle = GetNorthAngle();
		return System.MathF.PI / 180f * (0f - northAngle);
	}

	public bool GetTerrainMinMaxHeight(out float minHeight, out float maxHeight)
	{
		minHeight = 0f;
		maxHeight = 0f;
		return EngineApplicationInterface.IScene.GetTerrainMinMaxHeight(this, ref minHeight, ref maxHeight);
	}

	public void GetPhysicsMinMax(ref Vec3 min_max)
	{
		EngineApplicationInterface.IScene.GetPhysicsMinMax(base.Pointer, ref min_max);
	}

	public bool IsEditorScene()
	{
		return EngineApplicationInterface.IScene.IsEditorScene(base.Pointer);
	}

	public void SetMotionBlurMode(bool mode)
	{
		EngineApplicationInterface.IScene.SetMotionBlurMode(base.Pointer, mode);
	}

	public void SetAntialiasingMode(bool mode)
	{
		EngineApplicationInterface.IScene.SetAntialiasingMode(base.Pointer, mode);
	}

	public void SetDLSSMode(bool mode)
	{
		EngineApplicationInterface.IScene.SetDLSSMode(base.Pointer, mode);
	}

	public IEnumerable<WeakGameEntity> FindWeakEntitiesWithTag(string tag)
	{
		return WeakGameEntity.GetEntitiesWithTag(this, tag);
	}

	public WeakGameEntity FindWeakEntityWithTag(string tag)
	{
		return WeakGameEntity.GetFirstEntityWithTag(this, tag);
	}

	public IEnumerable<GameEntity> FindEntitiesWithTag(string tag)
	{
		return GameEntity.GetEntitiesWithTag(this, tag);
	}

	public GameEntity FindEntityWithTag(string tag)
	{
		return GameEntity.GetFirstEntityWithTag(this, tag);
	}

	public GameEntity FindEntityWithName(string name)
	{
		return GameEntity.GetFirstEntityWithName(this, name);
	}

	public IEnumerable<WeakGameEntity> FindWeakEntitiesWithTagExpression(string expression)
	{
		return WeakGameEntity.GetEntitiesWithTagExpression(this, expression);
	}

	public IEnumerable<GameEntity> FindEntitiesWithTagExpression(string expression)
	{
		return GameEntity.GetEntitiesWithTagExpression(this, expression);
	}

	public int GetSoftBoundaryVertexCount()
	{
		return EngineApplicationInterface.IScene.GetSoftBoundaryVertexCount(base.Pointer);
	}

	public int GetHardBoundaryVertexCount()
	{
		return EngineApplicationInterface.IScene.GetHardBoundaryVertexCount(base.Pointer);
	}

	public Vec2 GetSoftBoundaryVertex(int index)
	{
		return EngineApplicationInterface.IScene.GetSoftBoundaryVertex(base.Pointer, index);
	}

	public Vec2 GetHardBoundaryVertex(int index)
	{
		return EngineApplicationInterface.IScene.GetHardBoundaryVertex(base.Pointer, index);
	}

	public Path GetPathWithName(string name)
	{
		return EngineApplicationInterface.IScene.GetPathWithName(base.Pointer, name);
	}

	public void DeletePathWithName(string name)
	{
		EngineApplicationInterface.IScene.DeletePathWithName(base.Pointer, name);
	}

	public void AddPath(string name)
	{
		EngineApplicationInterface.IScene.AddPath(base.Pointer, name);
	}

	public void AddPathPoint(string name, MatrixFrame frame)
	{
		EngineApplicationInterface.IScene.AddPathPoint(base.Pointer, name, ref frame);
	}

	public void GetBoundingBox(out Vec3 min, out Vec3 max)
	{
		min = Vec3.Invalid;
		max = Vec3.Invalid;
		EngineApplicationInterface.IScene.GetBoundingBox(base.Pointer, ref min, ref max);
	}

	public void GetSceneLimits(out Vec3 min, out Vec3 max)
	{
		min = Vec3.Invalid;
		max = Vec3.Invalid;
		EngineApplicationInterface.IScene.GetSceneLimits(base.Pointer, ref min, ref max);
	}

	public void SetName(string name)
	{
		EngineApplicationInterface.IScene.SetName(base.Pointer, name);
	}

	public string GetName()
	{
		return EngineApplicationInterface.IScene.GetName(base.Pointer);
	}

	public string GetModulePath()
	{
		return EngineApplicationInterface.IScene.GetModulePath(base.Pointer);
	}

	public void SetOwnerThread()
	{
		EngineApplicationInterface.IScene.SetOwnerThread(base.Pointer);
	}

	public Path[] GetPathsWithNamePrefix(string prefix)
	{
		int numberOfPathsWithNamePrefix = EngineApplicationInterface.IScene.GetNumberOfPathsWithNamePrefix(base.Pointer, prefix);
		UIntPtr[] array = new UIntPtr[numberOfPathsWithNamePrefix];
		EngineApplicationInterface.IScene.GetPathsWithNamePrefix(base.Pointer, array, prefix);
		Path[] array2 = new Path[numberOfPathsWithNamePrefix];
		for (int i = 0; i < numberOfPathsWithNamePrefix; i++)
		{
			UIntPtr pointer = array[i];
			array2[i] = new Path(pointer);
		}
		return array2;
	}

	public void SetUseConstantTime(bool value)
	{
		EngineApplicationInterface.IScene.SetUseConstantTime(base.Pointer, value);
	}

	public bool CheckPointCanSeePoint(Vec3 source, Vec3 target, float? distanceToCheck = null)
	{
		if (!distanceToCheck.HasValue)
		{
			distanceToCheck = source.Distance(target);
		}
		return EngineApplicationInterface.IScene.CheckPointCanSeePoint(base.Pointer, source, target, distanceToCheck.Value);
	}

	public void SetPlaySoundEventsAfterReadyToRender(bool value)
	{
		EngineApplicationInterface.IScene.SetPlaySoundEventsAfterReadyToRender(base.Pointer, value);
	}

	public void DisableStaticShadows(bool value)
	{
		EngineApplicationInterface.IScene.DisableStaticShadows(base.Pointer, value);
	}

	public Mesh GetSkyboxMesh()
	{
		return EngineApplicationInterface.IScene.GetSkyboxMesh(base.Pointer);
	}

	public void SetAtmosphereWithName(string name)
	{
		EngineApplicationInterface.IScene.SetAtmosphereWithName(base.Pointer, name);
	}

	public void FillEntityWithHardBorderPhysicsBarrier(GameEntity entity)
	{
		EngineApplicationInterface.IScene.FillEntityWithHardBorderPhysicsBarrier(base.Pointer, entity.Pointer);
	}

	public void ClearDecals()
	{
		EngineApplicationInterface.IScene.ClearDecals(base.Pointer);
	}

	public void SetPhotoAtmosphereViaTod(float tod, bool withStorm)
	{
		EngineApplicationInterface.IScene.SetPhotoAtmosphereViaTod(base.Pointer, tod, withStorm);
	}

	public bool IsPositionOnADynamicNavMesh(Vec3 position)
	{
		return EngineApplicationInterface.IScene.IsPositionOnADynamicNavMesh(base.Pointer, position);
	}

	public void WaitWaterRendererCPUSimulation()
	{
		EngineApplicationInterface.IScene.WaitWaterRendererCPUSimulation(base.Pointer);
	}

	public void EnableInclusiveAsyncPhysx()
	{
		EngineApplicationInterface.IScene.EnableInclusiveAsyncPhysx(base.Pointer);
	}

	public void EnsureWaterWakeRenderer()
	{
		EngineApplicationInterface.IScene.EnsureWaterWakeRenderer(base.Pointer);
	}

	public void DeleteWaterWakeRenderer()
	{
		EngineApplicationInterface.IScene.DeleteWaterWakeRenderer(base.Pointer);
	}

	public bool SceneHadWaterWakeRenderer()
	{
		return EngineApplicationInterface.IScene.SceneHadWaterWakeRenderer(base.Pointer);
	}

	public void SetWaterWakeWorldSize(float worldSize, float eraseFactor)
	{
		EngineApplicationInterface.IScene.SetWaterWakeWorldSize(base.Pointer, worldSize, eraseFactor);
	}

	public void SetWaterWakeCameraOffset(float cameraOffset)
	{
		EngineApplicationInterface.IScene.SetWaterWakeCameraOffset(base.Pointer, cameraOffset);
	}

	public void TickWake(float dt)
	{
		EngineApplicationInterface.IScene.TickWake(base.Pointer, dt);
	}

	public void SetDoNotAddEntitiesToTickList(bool value)
	{
		EngineApplicationInterface.IScene.SetDoNotAddEntitiesToTickList(base.Pointer, value);
	}

	public void SetDontLoadInvisibleEntities(bool value)
	{
		EngineApplicationInterface.IScene.SetDontLoadInvisibleEntities(base.Pointer, value);
	}

	public void SetUsesDeleteLaterSystem(bool value)
	{
		EngineApplicationInterface.IScene.SetUsesDeleteLaterSystem(base.Pointer, value);
	}

	public void HandleCurrentFrameTickEntities()
	{
		EngineApplicationInterface.IScene.HandleCurrentFrameTickEntities(base.Pointer);
	}

	public void ClearCurrentFrameTickEntities()
	{
		EngineApplicationInterface.IScene.ClearCurrentFrameTickEntities(base.Pointer);
	}

	public void SetUseAdvancedWaterRendering(bool value)
	{
		EngineApplicationInterface.IScene.SetUseAdvancedWaterRendering(base.Pointer, value);
	}

	public Vec2 FindClosestExitPositionForPositionOnABoundaryFace(Vec3 position, UIntPtr boundaryFacePointer)
	{
		return EngineApplicationInterface.IScene.FindClosestExitPositionForPositionOnABoundaryFace(base.Pointer, position, boundaryFacePointer);
	}
}
