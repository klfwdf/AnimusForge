using System;
using System.Collections.Generic;
using System.Linq;
using TaleWorlds.DotNet;
using TaleWorlds.Library;

namespace TaleWorlds.Engine
{
	// Token: 0x02000081 RID: 129
	[EngineClass("rglScene")]
	public sealed class Scene : NativeObject
	{
		// Token: 0x06000AB4 RID: 2740 RVA: 0x0000B12B File Offset: 0x0000932B
		private Scene()
		{
		}

		// Token: 0x06000AB5 RID: 2741 RVA: 0x0000B133 File Offset: 0x00009333
		internal Scene(UIntPtr pointer)
		{
			base.Construct(pointer);
		}

		// Token: 0x06000AB6 RID: 2742 RVA: 0x0000B142 File Offset: 0x00009342
		public bool IsDefaultEditorScene()
		{
			return EngineApplicationInterface.IScene.IsDefaultEditorScene(this);
		}

		// Token: 0x06000AB7 RID: 2743 RVA: 0x0000B14F File Offset: 0x0000934F
		public bool IsMultiplayerScene()
		{
			return EngineApplicationInterface.IScene.IsMultiplayerScene(this);
		}

		// Token: 0x06000AB8 RID: 2744 RVA: 0x0000B15C File Offset: 0x0000935C
		public string TakePhotoModePicture(bool saveAmbientOcclusionPass, bool savingObjectIdPass, bool saveShadowPass)
		{
			return EngineApplicationInterface.IScene.TakePhotoModePicture(this, saveAmbientOcclusionPass, savingObjectIdPass, saveShadowPass);
		}

		// Token: 0x06000AB9 RID: 2745 RVA: 0x0000B16C File Offset: 0x0000936C
		public string GetAllColorGradeNames()
		{
			return EngineApplicationInterface.IScene.GetAllColorGradeNames(this);
		}

		// Token: 0x06000ABA RID: 2746 RVA: 0x0000B179 File Offset: 0x00009379
		public string GetAllFilterNames()
		{
			return EngineApplicationInterface.IScene.GetAllFilterNames(this);
		}

		// Token: 0x06000ABB RID: 2747 RVA: 0x0000B186 File Offset: 0x00009386
		public float GetPhotoModeRoll()
		{
			return EngineApplicationInterface.IScene.GetPhotoModeRoll(this);
		}

		// Token: 0x06000ABC RID: 2748 RVA: 0x0000B193 File Offset: 0x00009393
		public bool GetPhotoModeOrbit()
		{
			return EngineApplicationInterface.IScene.GetPhotoModeOrbit(this);
		}

		// Token: 0x06000ABD RID: 2749 RVA: 0x0000B1A0 File Offset: 0x000093A0
		public bool GetPhotoModeOn()
		{
			return EngineApplicationInterface.IScene.GetPhotoModeOn(this);
		}

		// Token: 0x06000ABE RID: 2750 RVA: 0x0000B1AD File Offset: 0x000093AD
		public void GetPhotoModeFocus(ref float focus, ref float focusStart, ref float focusEnd, ref float exposure, ref bool vignetteOn)
		{
			EngineApplicationInterface.IScene.GetPhotoModeFocus(this, ref focus, ref focusStart, ref focusEnd, ref exposure, ref vignetteOn);
		}

		// Token: 0x06000ABF RID: 2751 RVA: 0x0000B1C1 File Offset: 0x000093C1
		public int GetSceneColorGradeIndex()
		{
			return EngineApplicationInterface.IScene.GetSceneColorGradeIndex(this);
		}

		// Token: 0x06000AC0 RID: 2752 RVA: 0x0000B1CE File Offset: 0x000093CE
		public int GetSceneFilterIndex()
		{
			return EngineApplicationInterface.IScene.GetSceneFilterIndex(this);
		}

		// Token: 0x06000AC1 RID: 2753 RVA: 0x0000B1DB File Offset: 0x000093DB
		public void EnableFixedTick()
		{
			EngineApplicationInterface.IScene.EnableFixedTick(this);
		}

		// Token: 0x06000AC2 RID: 2754 RVA: 0x0000B1E8 File Offset: 0x000093E8
		public string GetLoadingStateName()
		{
			return EngineApplicationInterface.IScene.GetLoadingStateName(this);
		}

		// Token: 0x06000AC3 RID: 2755 RVA: 0x0000B1F5 File Offset: 0x000093F5
		public bool IsLoadingFinished()
		{
			return EngineApplicationInterface.IScene.IsLoadingFinished(this);
		}

		// Token: 0x06000AC4 RID: 2756 RVA: 0x0000B202 File Offset: 0x00009402
		public void SetPhotoModeRoll(float roll)
		{
			EngineApplicationInterface.IScene.SetPhotoModeRoll(this, roll);
		}

		// Token: 0x06000AC5 RID: 2757 RVA: 0x0000B210 File Offset: 0x00009410
		public void SetPhotoModeOrbit(bool orbit)
		{
			EngineApplicationInterface.IScene.SetPhotoModeOrbit(this, orbit);
		}

		// Token: 0x06000AC6 RID: 2758 RVA: 0x0000B21E File Offset: 0x0000941E
		public float GetFallDensity()
		{
			return EngineApplicationInterface.IScene.GetFallDensity(base.Pointer);
		}

		// Token: 0x06000AC7 RID: 2759 RVA: 0x0000B230 File Offset: 0x00009430
		public void SetPhotoModeOn(bool on)
		{
			EngineApplicationInterface.IScene.SetPhotoModeOn(this, on);
		}

		// Token: 0x06000AC8 RID: 2760 RVA: 0x0000B23E File Offset: 0x0000943E
		public void SetPhotoModeFocus(float focusStart, float focusEnd, float focus, float exposure)
		{
			EngineApplicationInterface.IScene.SetPhotoModeFocus(this, focusStart, focusEnd, focus, exposure);
		}

		// Token: 0x06000AC9 RID: 2761 RVA: 0x0000B250 File Offset: 0x00009450
		public void SetPhotoModeFov(float verticalFov)
		{
			EngineApplicationInterface.IScene.SetPhotoModeFov(this, verticalFov);
		}

		// Token: 0x06000ACA RID: 2762 RVA: 0x0000B25E File Offset: 0x0000945E
		public float GetPhotoModeFov()
		{
			return EngineApplicationInterface.IScene.GetPhotoModeFov(this);
		}

		// Token: 0x06000ACB RID: 2763 RVA: 0x0000B26B File Offset: 0x0000946B
		public bool HasDecalRenderer()
		{
			return EngineApplicationInterface.IScene.HasDecalRenderer(base.Pointer);
		}

		// Token: 0x06000ACC RID: 2764 RVA: 0x0000B27D File Offset: 0x0000947D
		public void SetPhotoModeVignette(bool vignetteOn)
		{
			EngineApplicationInterface.IScene.SetPhotoModeVignette(this, vignetteOn);
		}

		// Token: 0x06000ACD RID: 2765 RVA: 0x0000B28B File Offset: 0x0000948B
		public void SetSceneColorGradeIndex(int index)
		{
			EngineApplicationInterface.IScene.SetSceneColorGradeIndex(this, index);
		}

		// Token: 0x06000ACE RID: 2766 RVA: 0x0000B299 File Offset: 0x00009499
		public int SetSceneFilterIndex(int index)
		{
			return EngineApplicationInterface.IScene.SetSceneFilterIndex(this, index);
		}

		// Token: 0x06000ACF RID: 2767 RVA: 0x0000B2A7 File Offset: 0x000094A7
		public void SetSceneColorGrade(string textureName)
		{
			EngineApplicationInterface.IScene.SetSceneColorGrade(this, textureName);
		}

		// Token: 0x06000AD0 RID: 2768 RVA: 0x0000B2B5 File Offset: 0x000094B5
		public void SetUpgradeLevel(int level)
		{
			EngineApplicationInterface.IScene.SetUpgradeLevel(base.Pointer, level);
		}

		// Token: 0x06000AD1 RID: 2769 RVA: 0x0000B2C8 File Offset: 0x000094C8
		public void CreateBurstParticle(int particleId, MatrixFrame frame)
		{
			EngineApplicationInterface.IScene.CreateBurstParticle(this, particleId, ref frame);
		}

		// Token: 0x06000AD2 RID: 2770 RVA: 0x0000B2D8 File Offset: 0x000094D8
		public float[] GetTerrainHeightData(int nodeXIndex, int nodeYIndex)
		{
			float[] array = new float[EngineApplicationInterface.IScene.GetNodeDataCount(this, nodeXIndex, nodeYIndex)];
			EngineApplicationInterface.IScene.FillTerrainHeightData(this, nodeXIndex, nodeYIndex, array);
			return array;
		}

		// Token: 0x06000AD3 RID: 2771 RVA: 0x0000B308 File Offset: 0x00009508
		public short[] GetTerrainPhysicsMaterialIndexData(int nodeXIndex, int nodeYIndex)
		{
			short[] array = new short[EngineApplicationInterface.IScene.GetNodeDataCount(this, nodeXIndex, nodeYIndex)];
			EngineApplicationInterface.IScene.FillTerrainPhysicsMaterialIndexData(this, nodeXIndex, nodeYIndex, array);
			return array;
		}

		// Token: 0x06000AD4 RID: 2772 RVA: 0x0000B337 File Offset: 0x00009537
		public void GetTerrainData(out Vec2i nodeDimension, out float nodeSize, out int layerCount, out int layerVersion)
		{
			EngineApplicationInterface.IScene.GetTerrainData(this, out nodeDimension, out nodeSize, out layerCount, out layerVersion);
		}

		// Token: 0x06000AD5 RID: 2773 RVA: 0x0000B349 File Offset: 0x00009549
		public void GetTerrainNodeData(int xIndex, int yIndex, out int vertexCountAlongAxis, out float quadLength, out float minHeight, out float maxHeight)
		{
			EngineApplicationInterface.IScene.GetTerrainNodeData(this, xIndex, yIndex, out vertexCountAlongAxis, out quadLength, out minHeight, out maxHeight);
		}

		// Token: 0x06000AD6 RID: 2774 RVA: 0x0000B360 File Offset: 0x00009560
		public PhysicsMaterial GetTerrainPhysicsMaterialAtLayer(int layerIndex)
		{
			int terrainPhysicsMaterialIndexAtLayer = EngineApplicationInterface.IScene.GetTerrainPhysicsMaterialIndexAtLayer(this, layerIndex);
			return new PhysicsMaterial(terrainPhysicsMaterialIndexAtLayer);
		}

		// Token: 0x06000AD7 RID: 2775 RVA: 0x0000B380 File Offset: 0x00009580
		public void SetSceneColorGrade(Scene scene, string textureName)
		{
			EngineApplicationInterface.IScene.SetSceneColorGrade(scene, textureName);
		}

		// Token: 0x06000AD8 RID: 2776 RVA: 0x0000B38E File Offset: 0x0000958E
		public float GetWaterLevel()
		{
			return EngineApplicationInterface.IScene.GetWaterLevel(this);
		}

		// Token: 0x06000AD9 RID: 2777 RVA: 0x0000B39B File Offset: 0x0000959B
		public float GetWaterLevelAtPosition(Vec2 position, bool useWaterRenderer, bool checkWaterBodyEntities)
		{
			return EngineApplicationInterface.IScene.GetWaterLevelAtPosition(this, position, useWaterRenderer, checkWaterBodyEntities);
		}

		// Token: 0x06000ADA RID: 2778 RVA: 0x0000B3AB File Offset: 0x000095AB
		public Vec3 GetWaterSpeedAtPosition(Vec2 position, bool doChoppinessCorrection)
		{
			return EngineApplicationInterface.IScene.GetWaterSpeedAtPosition(base.Pointer, position, doChoppinessCorrection);
		}

		// Token: 0x06000ADB RID: 2779 RVA: 0x0000B3C0 File Offset: 0x000095C0
		public void GetBulkWaterLevelAtPositions(Vec2[] waterHeightQueryArray, ref float[] waterHeightsAtVolumes, ref Vec3[] waterSurfaceNormals)
		{
			EngineApplicationInterface.IScene.GetBulkWaterLevelAtPositions(this, waterHeightQueryArray, waterHeightQueryArray.Length, waterHeightsAtVolumes, waterSurfaceNormals);
		}

		// Token: 0x06000ADC RID: 2780 RVA: 0x0000B3D5 File Offset: 0x000095D5
		public void GetInterpolationFactorForBodyWorldTransformSmoothing(out float interpolationFactor, out float fixedDt)
		{
			EngineApplicationInterface.IScene.GetInterpolationFactorForBodyWorldTransformSmoothing(this, out interpolationFactor, out fixedDt);
		}

		// Token: 0x06000ADD RID: 2781 RVA: 0x0000B3E4 File Offset: 0x000095E4
		public void GetBulkWaterLevelAtVolumes(UIntPtr waterHeightQueryArray, int waterHeightQueryArrayCount, in MatrixFrame globalFrame)
		{
			EngineApplicationInterface.IScene.GetBulkWaterLevelAtVolumes(base.Pointer, waterHeightQueryArray, waterHeightQueryArrayCount, globalFrame);
		}

		// Token: 0x06000ADE RID: 2782 RVA: 0x0000B3F9 File Offset: 0x000095F9
		public float GetWaterStrength()
		{
			return EngineApplicationInterface.IScene.GetWaterStrength(this);
		}

		// Token: 0x06000ADF RID: 2783 RVA: 0x0000B406 File Offset: 0x00009606
		public void DeRegisterShipVisual(UIntPtr visualPointer)
		{
			EngineApplicationInterface.IScene.DeRegisterShipVisual(base.Pointer, visualPointer);
		}

		// Token: 0x06000AE0 RID: 2784 RVA: 0x0000B419 File Offset: 0x00009619
		public UIntPtr RegisterShipVisualToWaterRenderer(WeakGameEntity entity, in Vec3 waterEffectBB)
		{
			return EngineApplicationInterface.IScene.RegisterShipVisualToWaterRenderer(base.Pointer, entity.Pointer, waterEffectBB);
		}

		// Token: 0x06000AE1 RID: 2785 RVA: 0x0000B433 File Offset: 0x00009633
		public void SetWaterStrength(float newWaterStrength)
		{
			EngineApplicationInterface.IScene.SetWaterStrength(this, newWaterStrength);
		}

		// Token: 0x06000AE2 RID: 2786 RVA: 0x0000B441 File Offset: 0x00009641
		public void AddWaterWakeWithSphere(Vec3 position, float radius, float wakeVisibility, float foamVisibility)
		{
			this.AddWaterWakeWithCapsule(position, radius, position, radius, wakeVisibility, foamVisibility);
		}

		// Token: 0x06000AE3 RID: 2787 RVA: 0x0000B450 File Offset: 0x00009650
		public void AddWaterWakeWithCapsule(Vec3 positionA, float radiusA, Vec3 positionB, float radiusB, float wakeVisibility, float foamVisibility)
		{
			EngineApplicationInterface.IScene.AddWaterWakeWithCapsule(this, positionA, radiusA, positionB, radiusB, wakeVisibility, foamVisibility);
		}

		// Token: 0x06000AE4 RID: 2788 RVA: 0x0000B468 File Offset: 0x00009668
		public bool GetPathBetweenAIFaces(UIntPtr startingFace, UIntPtr endingFace, Vec2 startingPosition, Vec2 endingPosition, float agentRadius, NavigationPath path, int[] excludedFaceIds)
		{
			int size = path.PathPoints.Length;
			if (EngineApplicationInterface.IScene.GetPathBetweenAIFacePointers(base.Pointer, startingFace, endingFace, startingPosition, endingPosition, agentRadius, path.PathPoints, ref size, excludedFaceIds, (excludedFaceIds != null) ? excludedFaceIds.Length : 0))
			{
				path.Size = size;
				return true;
			}
			path.Size = 0;
			return false;
		}

		// Token: 0x06000AE5 RID: 2789 RVA: 0x0000B4C1 File Offset: 0x000096C1
		public bool HasNavmeshFaceUnsharedEdges(in PathFaceRecord faceRecord)
		{
			return EngineApplicationInterface.IScene.HasNavmeshFaceUnsharedEdges(base.Pointer, faceRecord);
		}

		// Token: 0x06000AE6 RID: 2790 RVA: 0x0000B4D4 File Offset: 0x000096D4
		public int GetNavmeshFaceCountBetweenTwoIds(int firstId, int secondId)
		{
			return EngineApplicationInterface.IScene.GetNavmeshFaceCountBetweenTwoIds(base.Pointer, firstId, secondId);
		}

		// Token: 0x06000AE7 RID: 2791 RVA: 0x0000B4E8 File Offset: 0x000096E8
		public void GetNavmeshFaceRecordsBetweenTwoIds(int firstId, int secondId, PathFaceRecord[] faceRecords)
		{
			EngineApplicationInterface.IScene.GetNavmeshFaceRecordsBetweenTwoIds(base.Pointer, firstId, secondId, faceRecords);
		}

		// Token: 0x06000AE8 RID: 2792 RVA: 0x0000B4FD File Offset: 0x000096FD
		public void SetFixedTickCallbackActive(bool isActive)
		{
			EngineApplicationInterface.IScene.SetFixedTickCallbackActive(this, isActive);
		}

		// Token: 0x06000AE9 RID: 2793 RVA: 0x0000B50B File Offset: 0x0000970B
		public void SetOnCollisionFilterCallbackActive(bool isActive)
		{
			EngineApplicationInterface.IScene.SetOnCollisionFilterCallbackActive(this, isActive);
		}

		// Token: 0x06000AEA RID: 2794 RVA: 0x0000B51C File Offset: 0x0000971C
		public bool GetPathBetweenAIFaces(UIntPtr startingFace, UIntPtr endingFace, Vec2 startingPosition, Vec2 endingPosition, float agentRadius, NavigationPath path, int[] excludedFaceIds, int regionSwitchCostTo0, int regionSwitchCostTo1)
		{
			int size = path.PathPoints.Length;
			if (EngineApplicationInterface.IScene.GetPathBetweenAIFacePointersWithRegionSwitchCost(base.Pointer, startingFace, endingFace, startingPosition, endingPosition, agentRadius, path.PathPoints, ref size, excludedFaceIds, (excludedFaceIds != null) ? excludedFaceIds.Length : 0, regionSwitchCostTo0, regionSwitchCostTo1))
			{
				path.Size = size;
				return true;
			}
			path.Size = 0;
			return false;
		}

		// Token: 0x06000AEB RID: 2795 RVA: 0x0000B57C File Offset: 0x0000977C
		public bool GetPathBetweenAIFaces(int startingFace, int endingFace, Vec2 startingPosition, Vec2 endingPosition, float agentRadius, NavigationPath path, int[] excludedFaceIds, float extraCostMultiplier)
		{
			int size = path.PathPoints.Length;
			if (EngineApplicationInterface.IScene.GetPathBetweenAIFaceIndices(base.Pointer, startingFace, endingFace, startingPosition, endingPosition, agentRadius, path.PathPoints, ref size, excludedFaceIds, (excludedFaceIds != null) ? excludedFaceIds.Length : 0, extraCostMultiplier))
			{
				path.Size = size;
				return true;
			}
			path.Size = 0;
			return false;
		}

		// Token: 0x06000AEC RID: 2796 RVA: 0x0000B5D8 File Offset: 0x000097D8
		public bool GetPathBetweenAIFaces(int startingFace, int endingFace, Vec2 startingPosition, Vec2 endingPosition, float agentRadius, NavigationPath path, int[] excludedFaceIds, float extraCostMultiplier, int regionSwitchCostTo0, int regionSwitchCostTo1)
		{
			int size = path.PathPoints.Length;
			if (EngineApplicationInterface.IScene.GetPathBetweenAIFaceIndicesWithRegionSwitchCost(base.Pointer, startingFace, endingFace, startingPosition, endingPosition, agentRadius, path.PathPoints, ref size, excludedFaceIds, (excludedFaceIds != null) ? excludedFaceIds.Length : 0, extraCostMultiplier, regionSwitchCostTo0, regionSwitchCostTo1))
			{
				path.Size = size;
				return true;
			}
			path.Size = 0;
			return false;
		}

		// Token: 0x06000AED RID: 2797 RVA: 0x0000B638 File Offset: 0x00009838
		public bool GetPathDistanceBetweenAIFaces(int startingAiFace, int endingAiFace, Vec2 startingPosition, Vec2 endingPosition, float agentRadius, float distanceLimit, out float distance, int[] excludedFaceIds, int regionSwitchCostTo0, int regionSwitchCostTo1)
		{
			return EngineApplicationInterface.IScene.GetPathDistanceBetweenAIFaces(base.Pointer, startingAiFace, endingAiFace, startingPosition, endingPosition, agentRadius, distanceLimit, out distance, excludedFaceIds, (excludedFaceIds != null) ? excludedFaceIds.Length : 0, regionSwitchCostTo0, regionSwitchCostTo1);
		}

		// Token: 0x06000AEE RID: 2798 RVA: 0x0000B671 File Offset: 0x00009871
		public void GetNavMeshFaceIndex(ref PathFaceRecord record, Vec2 position, bool isRegion1, bool checkIfDisabled, bool ignoreHeight = false)
		{
			EngineApplicationInterface.IScene.GetNavMeshFaceIndex(base.Pointer, ref record, position, isRegion1, checkIfDisabled, ignoreHeight);
		}

		// Token: 0x06000AEF RID: 2799 RVA: 0x0000B68A File Offset: 0x0000988A
		public void GetNavMeshFaceIndex(ref PathFaceRecord record, Vec3 position, bool checkIfDisabled)
		{
			EngineApplicationInterface.IScene.GetNavMeshFaceIndex3(base.Pointer, ref record, position, checkIfDisabled);
		}

		// Token: 0x06000AF0 RID: 2800 RVA: 0x0000B69F File Offset: 0x0000989F
		public static Scene CreateNewScene(bool initialize_physics = true, bool enable_decals = true, DecalAtlasGroup atlasGroup = DecalAtlasGroup.All, string sceneName = "mono_renderscene")
		{
			return EngineApplicationInterface.IScene.CreateNewScene(initialize_physics, enable_decals, (int)atlasGroup, sceneName);
		}

		// Token: 0x06000AF1 RID: 2801 RVA: 0x0000B6AF File Offset: 0x000098AF
		public void AddAlwaysRenderedSkeleton(Skeleton skeleton)
		{
			EngineApplicationInterface.IScene.AddAlwaysRenderedSkeleton(base.Pointer, skeleton.Pointer);
		}

		// Token: 0x06000AF2 RID: 2802 RVA: 0x0000B6C7 File Offset: 0x000098C7
		public void RemoveAlwaysRenderedSkeleton(Skeleton skeleton)
		{
			EngineApplicationInterface.IScene.RemoveAlwaysRenderedSkeleton(base.Pointer, skeleton.Pointer);
		}

		// Token: 0x06000AF3 RID: 2803 RVA: 0x0000B6DF File Offset: 0x000098DF
		public MetaMesh CreatePathMesh(string baseEntityName, bool isWaterPath)
		{
			return EngineApplicationInterface.IScene.CreatePathMesh(base.Pointer, baseEntityName, isWaterPath);
		}

		// Token: 0x06000AF4 RID: 2804 RVA: 0x0000B6F4 File Offset: 0x000098F4
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

		// Token: 0x06000AF5 RID: 2805 RVA: 0x0000B759 File Offset: 0x00009959
		public void SetDoNotWaitForLoadingStatesToRender(bool value)
		{
			EngineApplicationInterface.IScene.SetDoNotWaitForLoadingStatesToRender(base.Pointer, value);
		}

		// Token: 0x06000AF6 RID: 2806 RVA: 0x0000B76C File Offset: 0x0000996C
		public void SetDynamicSnowTexture(Texture texture)
		{
			EngineApplicationInterface.IScene.SetDynamicSnowTexture(base.Pointer, (texture != null) ? texture.Pointer : UIntPtr.Zero);
		}

		// Token: 0x06000AF7 RID: 2807 RVA: 0x0000B794 File Offset: 0x00009994
		public void GetWindFlowMapData(float[] flowMapData)
		{
			EngineApplicationInterface.IScene.GetWindFlowMapData(base.Pointer, flowMapData);
		}

		// Token: 0x06000AF8 RID: 2808 RVA: 0x0000B7A7 File Offset: 0x000099A7
		public void CreateDynamicRainTexture(int w, int h)
		{
			EngineApplicationInterface.IScene.CreateDynamicRainTexture(base.Pointer, w, h);
		}

		// Token: 0x06000AF9 RID: 2809 RVA: 0x0000B7BC File Offset: 0x000099BC
		public MetaMesh CreatePathMesh(IList<GameEntity> pathNodes, bool isWaterPath = false)
		{
			return EngineApplicationInterface.IScene.CreatePathMesh2(base.Pointer, (from e in pathNodes
				select e.Pointer).ToArray<UIntPtr>(), pathNodes.Count, isWaterPath);
		}

		// Token: 0x06000AFA RID: 2810 RVA: 0x0000B80A File Offset: 0x00009A0A
		public GameEntity GetEntityWithGuid(string guid)
		{
			return EngineApplicationInterface.IScene.GetEntityWithGuid(base.Pointer, guid);
		}

		// Token: 0x06000AFB RID: 2811 RVA: 0x0000B81D File Offset: 0x00009A1D
		public bool IsEntityFrameChanged(string containsName)
		{
			return EngineApplicationInterface.IScene.CheckPathEntitiesFrameChanged(base.Pointer, containsName);
		}

		// Token: 0x06000AFC RID: 2812 RVA: 0x0000B830 File Offset: 0x00009A30
		public void GetTerrainHeightAndNormal(Vec2 position, out float height, out Vec3 normal)
		{
			EngineApplicationInterface.IScene.GetTerrainHeightAndNormal(base.Pointer, position, out height, out normal);
		}

		// Token: 0x06000AFD RID: 2813 RVA: 0x0000B845 File Offset: 0x00009A45
		public int GetFloraInstanceCount()
		{
			return EngineApplicationInterface.IScene.GetFloraInstanceCount(base.Pointer);
		}

		// Token: 0x06000AFE RID: 2814 RVA: 0x0000B857 File Offset: 0x00009A57
		public int GetFloraRendererTextureUsage()
		{
			return EngineApplicationInterface.IScene.GetFloraRendererTextureUsage(base.Pointer);
		}

		// Token: 0x06000AFF RID: 2815 RVA: 0x0000B869 File Offset: 0x00009A69
		public int GetTerrainMemoryUsage()
		{
			return EngineApplicationInterface.IScene.GetTerrainMemoryUsage(base.Pointer);
		}

		// Token: 0x06000B00 RID: 2816 RVA: 0x0000B87B File Offset: 0x00009A7B
		public void SetFetchCrcInfoOfScene(bool value)
		{
			EngineApplicationInterface.IScene.SetFetchCrcInfoOfScene(base.Pointer, value);
		}

		// Token: 0x06000B01 RID: 2817 RVA: 0x0000B88E File Offset: 0x00009A8E
		public uint GetSceneXMLCRC()
		{
			return EngineApplicationInterface.IScene.GetSceneXMLCRC(base.Pointer);
		}

		// Token: 0x06000B02 RID: 2818 RVA: 0x0000B8A0 File Offset: 0x00009AA0
		public uint GetNavigationMeshCRC()
		{
			return EngineApplicationInterface.IScene.GetNavigationMeshCRC(base.Pointer);
		}

		// Token: 0x06000B03 RID: 2819 RVA: 0x0000B8B2 File Offset: 0x00009AB2
		public void SetGlobalWindStrengthVector(in Vec2 windVector)
		{
			EngineApplicationInterface.IScene.SetGlobalWindStrengthVector(base.Pointer, windVector);
		}

		// Token: 0x06000B04 RID: 2820 RVA: 0x0000B8C5 File Offset: 0x00009AC5
		public Vec2 GetGlobalWindStrengthVector()
		{
			return EngineApplicationInterface.IScene.GetGlobalWindStrengthVector(base.Pointer);
		}

		// Token: 0x06000B05 RID: 2821 RVA: 0x0000B8D7 File Offset: 0x00009AD7
		public Vec2 GetGlobalWindVelocity()
		{
			return EngineApplicationInterface.IScene.GetGlobalWindVelocity(base.Pointer);
		}

		// Token: 0x06000B06 RID: 2822 RVA: 0x0000B8E9 File Offset: 0x00009AE9
		public void SetGlobalWindVelocity(in Vec2 windVector)
		{
			EngineApplicationInterface.IScene.SetGlobalWindVelocity(base.Pointer, windVector);
		}

		// Token: 0x06000B07 RID: 2823 RVA: 0x0000B8FC File Offset: 0x00009AFC
		public bool GetEnginePhysicsEnabled()
		{
			return EngineApplicationInterface.IScene.GetEnginePhysicsEnabled(base.Pointer);
		}

		// Token: 0x06000B08 RID: 2824 RVA: 0x0000B90E File Offset: 0x00009B0E
		public void ClearNavMesh()
		{
			EngineApplicationInterface.IScene.ClearNavMesh(base.Pointer);
		}

		// Token: 0x06000B09 RID: 2825 RVA: 0x0000B920 File Offset: 0x00009B20
		public void StallLoadingRenderingsUntilFurtherNotice()
		{
			EngineApplicationInterface.IScene.StallLoadingRenderingsUntilFurtherNotice(base.Pointer);
		}

		// Token: 0x06000B0A RID: 2826 RVA: 0x0000B932 File Offset: 0x00009B32
		public int GetNavMeshFaceCount()
		{
			return EngineApplicationInterface.IScene.GetNavMeshFaceCount(base.Pointer);
		}

		// Token: 0x06000B0B RID: 2827 RVA: 0x0000B944 File Offset: 0x00009B44
		public void ResumeLoadingRenderings()
		{
			EngineApplicationInterface.IScene.ResumeLoadingRenderings(base.Pointer);
		}

		// Token: 0x06000B0C RID: 2828 RVA: 0x0000B956 File Offset: 0x00009B56
		public uint GetUpgradeLevelMask()
		{
			return EngineApplicationInterface.IScene.GetUpgradeLevelMask(base.Pointer);
		}

		// Token: 0x06000B0D RID: 2829 RVA: 0x0000B968 File Offset: 0x00009B68
		public void SetUpgradeLevelVisibility(uint mask)
		{
			EngineApplicationInterface.IScene.SetUpgradeLevelVisibilityWithMask(base.Pointer, mask);
		}

		// Token: 0x06000B0E RID: 2830 RVA: 0x0000B97C File Offset: 0x00009B7C
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

		// Token: 0x06000B0F RID: 2831 RVA: 0x0000B9DB File Offset: 0x00009BDB
		public int GetIdOfNavMeshFace(int faceIndex)
		{
			return EngineApplicationInterface.IScene.GetIdOfNavMeshFace(base.Pointer, faceIndex);
		}

		// Token: 0x06000B10 RID: 2832 RVA: 0x0000B9EE File Offset: 0x00009BEE
		public void SetClothSimulationState(bool state)
		{
			EngineApplicationInterface.IScene.SetClothSimulationState(base.Pointer, state);
		}

		// Token: 0x06000B11 RID: 2833 RVA: 0x0000BA01 File Offset: 0x00009C01
		public void GetNavMeshCenterPosition(int faceIndex, ref Vec3 centerPosition)
		{
			EngineApplicationInterface.IScene.GetNavMeshFaceCenterPosition(base.Pointer, faceIndex, ref centerPosition);
		}

		// Token: 0x06000B12 RID: 2834 RVA: 0x0000BA15 File Offset: 0x00009C15
		public PathFaceRecord GetNavMeshPathFaceRecord(int faceIndex)
		{
			return EngineApplicationInterface.IScene.GetNavMeshPathFaceRecord(base.Pointer, faceIndex);
		}

		// Token: 0x06000B13 RID: 2835 RVA: 0x0000BA28 File Offset: 0x00009C28
		public PathFaceRecord GetPathFaceRecordFromNavMeshFacePointer(UIntPtr navMeshFacePointer)
		{
			return EngineApplicationInterface.IScene.GetPathFaceRecordFromNavMeshFacePointer(base.Pointer, navMeshFacePointer);
		}

		// Token: 0x06000B14 RID: 2836 RVA: 0x0000BA3B File Offset: 0x00009C3B
		public void GetAllNavmeshFaceRecords(PathFaceRecord[] faceRecords)
		{
			EngineApplicationInterface.IScene.GetAllNavmeshFaceRecords(base.Pointer, faceRecords);
		}

		// Token: 0x06000B15 RID: 2837 RVA: 0x0000BA4E File Offset: 0x00009C4E
		public GameEntity GetFirstEntityWithName(string name)
		{
			return EngineApplicationInterface.IScene.GetFirstEntityWithName(base.Pointer, name);
		}

		// Token: 0x06000B16 RID: 2838 RVA: 0x0000BA61 File Offset: 0x00009C61
		public GameEntity GetCampaignEntityWithName(string name)
		{
			return EngineApplicationInterface.IScene.GetCampaignEntityWithName(base.Pointer, name);
		}

		// Token: 0x06000B17 RID: 2839 RVA: 0x0000BA74 File Offset: 0x00009C74
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

		// Token: 0x06000B18 RID: 2840 RVA: 0x0000BAD4 File Offset: 0x00009CD4
		public GameEntity GetFirstEntityWithScriptComponent<T>() where T : ScriptComponentBehavior
		{
			string name = typeof(T).Name;
			return EngineApplicationInterface.IScene.GetFirstEntityWithScriptComponent(base.Pointer, name);
		}

		// Token: 0x06000B19 RID: 2841 RVA: 0x0000BB02 File Offset: 0x00009D02
		public GameEntity GetFirstEntityWithScriptComponent(string scriptName)
		{
			return EngineApplicationInterface.IScene.GetFirstEntityWithScriptComponent(base.Pointer, scriptName);
		}

		// Token: 0x06000B1A RID: 2842 RVA: 0x0000BB15 File Offset: 0x00009D15
		public uint GetUpgradeLevelMaskOfLevelName(string levelName)
		{
			return EngineApplicationInterface.IScene.GetUpgradeLevelMaskOfLevelName(base.Pointer, levelName);
		}

		// Token: 0x06000B1B RID: 2843 RVA: 0x0000BB28 File Offset: 0x00009D28
		public string GetUpgradeLevelNameOfIndex(int index)
		{
			return EngineApplicationInterface.IScene.GetUpgradeLevelNameOfIndex(base.Pointer, index);
		}

		// Token: 0x06000B1C RID: 2844 RVA: 0x0000BB3B File Offset: 0x00009D3B
		public int GetUpgradeLevelCount()
		{
			return EngineApplicationInterface.IScene.GetUpgradeLevelCount(base.Pointer);
		}

		// Token: 0x06000B1D RID: 2845 RVA: 0x0000BB4D File Offset: 0x00009D4D
		public float GetWinterTimeFactor()
		{
			return EngineApplicationInterface.IScene.GetWinterTimeFactor(base.Pointer);
		}

		// Token: 0x06000B1E RID: 2846 RVA: 0x0000BB5F File Offset: 0x00009D5F
		public float GetNavMeshFaceFirstVertexZ(int faceIndex)
		{
			return EngineApplicationInterface.IScene.GetNavMeshFaceFirstVertexZ(base.Pointer, faceIndex);
		}

		// Token: 0x06000B1F RID: 2847 RVA: 0x0000BB72 File Offset: 0x00009D72
		public void SetWinterTimeFactor(float winterTimeFactor)
		{
			EngineApplicationInterface.IScene.SetWinterTimeFactor(base.Pointer, winterTimeFactor);
		}

		// Token: 0x06000B20 RID: 2848 RVA: 0x0000BB85 File Offset: 0x00009D85
		public void SetDrynessFactor(float drynessFactor)
		{
			EngineApplicationInterface.IScene.SetDrynessFactor(base.Pointer, drynessFactor);
		}

		// Token: 0x06000B21 RID: 2849 RVA: 0x0000BB98 File Offset: 0x00009D98
		public float GetFog()
		{
			return EngineApplicationInterface.IScene.GetFog(base.Pointer);
		}

		// Token: 0x06000B22 RID: 2850 RVA: 0x0000BBAA File Offset: 0x00009DAA
		public void SetFog(float fogDensity, ref Vec3 fogColor, float fogFalloff)
		{
			EngineApplicationInterface.IScene.SetFog(base.Pointer, fogDensity, ref fogColor, fogFalloff);
		}

		// Token: 0x06000B23 RID: 2851 RVA: 0x0000BBBF File Offset: 0x00009DBF
		public void SetFogAdvanced(float fogFalloffOffset, float fogFalloffMinFog, float fogFalloffStartDist)
		{
			EngineApplicationInterface.IScene.SetFogAdvanced(base.Pointer, fogFalloffOffset, fogFalloffMinFog, fogFalloffStartDist);
		}

		// Token: 0x06000B24 RID: 2852 RVA: 0x0000BBD4 File Offset: 0x00009DD4
		public void SetFogAmbientColor(ref Vec3 fogAmbientColor)
		{
			EngineApplicationInterface.IScene.SetFogAmbientColor(base.Pointer, ref fogAmbientColor);
		}

		// Token: 0x06000B25 RID: 2853 RVA: 0x0000BBE7 File Offset: 0x00009DE7
		public void SetTemperature(float temperature)
		{
			EngineApplicationInterface.IScene.SetTemperature(base.Pointer, temperature);
		}

		// Token: 0x06000B26 RID: 2854 RVA: 0x0000BBFA File Offset: 0x00009DFA
		public void SetHumidity(float humidity)
		{
			EngineApplicationInterface.IScene.SetHumidity(base.Pointer, humidity);
		}

		// Token: 0x06000B27 RID: 2855 RVA: 0x0000BC0D File Offset: 0x00009E0D
		public void SetDynamicShadowmapCascadesRadiusMultiplier(float multiplier)
		{
			EngineApplicationInterface.IScene.SetDynamicShadowmapCascadesRadiusMultiplier(base.Pointer, multiplier);
		}

		// Token: 0x06000B28 RID: 2856 RVA: 0x0000BC20 File Offset: 0x00009E20
		public void SetEnvironmentMultiplier(bool useMultiplier, float multiplier)
		{
			EngineApplicationInterface.IScene.SetEnvironmentMultiplier(base.Pointer, useMultiplier, multiplier);
		}

		// Token: 0x06000B29 RID: 2857 RVA: 0x0000BC34 File Offset: 0x00009E34
		public void SetSkyRotation(float rotation)
		{
			EngineApplicationInterface.IScene.SetSkyRotation(base.Pointer, rotation);
		}

		// Token: 0x06000B2A RID: 2858 RVA: 0x0000BC47 File Offset: 0x00009E47
		public void SetSkyBrightness(float brightness)
		{
			EngineApplicationInterface.IScene.SetSkyBrightness(base.Pointer, brightness);
		}

		// Token: 0x06000B2B RID: 2859 RVA: 0x0000BC5A File Offset: 0x00009E5A
		public void SetForcedSnow(bool value)
		{
			EngineApplicationInterface.IScene.SetForcedSnow(base.Pointer, value);
		}

		// Token: 0x06000B2C RID: 2860 RVA: 0x0000BC6D File Offset: 0x00009E6D
		public void SetSunLight(ref Vec3 color, ref Vec3 direction)
		{
			EngineApplicationInterface.IScene.SetSunLight(base.Pointer, color, direction);
		}

		// Token: 0x06000B2D RID: 2861 RVA: 0x0000BC8B File Offset: 0x00009E8B
		public void SetSunDirection(ref Vec3 direction)
		{
			EngineApplicationInterface.IScene.SetSunDirection(base.Pointer, direction);
		}

		// Token: 0x06000B2E RID: 2862 RVA: 0x0000BCA3 File Offset: 0x00009EA3
		public void SetSun(ref Vec3 color, float altitude, float angle, float intensity)
		{
			EngineApplicationInterface.IScene.SetSun(base.Pointer, color, altitude, angle, intensity);
		}

		// Token: 0x06000B2F RID: 2863 RVA: 0x0000BCBF File Offset: 0x00009EBF
		public void SetSunAngleAltitude(float angle, float altitude)
		{
			EngineApplicationInterface.IScene.SetSunAngleAltitude(base.Pointer, angle, altitude);
		}

		// Token: 0x06000B30 RID: 2864 RVA: 0x0000BCD3 File Offset: 0x00009ED3
		public void SetSunSize(float size)
		{
			EngineApplicationInterface.IScene.SetSunSize(base.Pointer, size);
		}

		// Token: 0x06000B31 RID: 2865 RVA: 0x0000BCE6 File Offset: 0x00009EE6
		public void SetSunShaftStrength(float strength)
		{
			EngineApplicationInterface.IScene.SetSunShaftStrength(base.Pointer, strength);
		}

		// Token: 0x06000B32 RID: 2866 RVA: 0x0000BCF9 File Offset: 0x00009EF9
		public float GetRainDensity()
		{
			return EngineApplicationInterface.IScene.GetRainDensity(base.Pointer);
		}

		// Token: 0x06000B33 RID: 2867 RVA: 0x0000BD0B File Offset: 0x00009F0B
		public void SetRainDensity(float density)
		{
			EngineApplicationInterface.IScene.SetRainDensity(base.Pointer, density);
		}

		// Token: 0x06000B34 RID: 2868 RVA: 0x0000BD1E File Offset: 0x00009F1E
		public float GetSnowDensity()
		{
			return EngineApplicationInterface.IScene.GetSnowDensity(base.Pointer);
		}

		// Token: 0x06000B35 RID: 2869 RVA: 0x0000BD30 File Offset: 0x00009F30
		public void SetSnowDensity(float density)
		{
			EngineApplicationInterface.IScene.SetSnowDensity(base.Pointer, density);
		}

		// Token: 0x06000B36 RID: 2870 RVA: 0x0000BD43 File Offset: 0x00009F43
		public void AddDecalInstance(Decal decal, string decalSetID, bool deletable)
		{
			EngineApplicationInterface.IScene.AddDecalInstance(base.Pointer, decal.Pointer, decalSetID, deletable);
		}

		// Token: 0x06000B37 RID: 2871 RVA: 0x0000BD5D File Offset: 0x00009F5D
		public void RemoveDecalInstance(Decal decal, string decalSetID)
		{
			EngineApplicationInterface.IScene.RemoveDecalInstance(base.Pointer, decal.Pointer, decalSetID);
		}

		// Token: 0x06000B38 RID: 2872 RVA: 0x0000BD76 File Offset: 0x00009F76
		public void SetShadow(bool shadowEnabled)
		{
			EngineApplicationInterface.IScene.SetShadow(base.Pointer, shadowEnabled);
		}

		// Token: 0x06000B39 RID: 2873 RVA: 0x0000BD89 File Offset: 0x00009F89
		public int AddPointLight(ref Vec3 position, float radius)
		{
			return EngineApplicationInterface.IScene.AddPointLight(base.Pointer, position, radius);
		}

		// Token: 0x06000B3A RID: 2874 RVA: 0x0000BDA2 File Offset: 0x00009FA2
		public int AddDirectionalLight(ref Vec3 position, ref Vec3 direction, float radius)
		{
			return EngineApplicationInterface.IScene.AddDirectionalLight(base.Pointer, position, direction, radius);
		}

		// Token: 0x06000B3B RID: 2875 RVA: 0x0000BDC1 File Offset: 0x00009FC1
		public void SetLightPosition(int lightIndex, ref Vec3 position)
		{
			EngineApplicationInterface.IScene.SetLightPosition(base.Pointer, lightIndex, position);
		}

		// Token: 0x06000B3C RID: 2876 RVA: 0x0000BDDA File Offset: 0x00009FDA
		public void SetLightDiffuseColor(int lightIndex, ref Vec3 diffuseColor)
		{
			EngineApplicationInterface.IScene.SetLightDiffuseColor(base.Pointer, lightIndex, diffuseColor);
		}

		// Token: 0x06000B3D RID: 2877 RVA: 0x0000BDF3 File Offset: 0x00009FF3
		public void SetLightDirection(int lightIndex, ref Vec3 direction)
		{
			EngineApplicationInterface.IScene.SetLightDirection(base.Pointer, lightIndex, direction);
		}

		// Token: 0x06000B3E RID: 2878 RVA: 0x0000BE0C File Offset: 0x0000A00C
		public void SetMieScatterFocus(float strength)
		{
			EngineApplicationInterface.IScene.SetMieScatterFocus(base.Pointer, strength);
		}

		// Token: 0x06000B3F RID: 2879 RVA: 0x0000BE1F File Offset: 0x0000A01F
		public void SetMieScatterStrength(float strength)
		{
			EngineApplicationInterface.IScene.SetMieScatterStrength(base.Pointer, strength);
		}

		// Token: 0x06000B40 RID: 2880 RVA: 0x0000BE32 File Offset: 0x0000A032
		public void SetBrightpassThreshold(float threshold)
		{
			EngineApplicationInterface.IScene.SetBrightpassTreshold(base.Pointer, threshold);
		}

		// Token: 0x06000B41 RID: 2881 RVA: 0x0000BE45 File Offset: 0x0000A045
		public void SetLensDistortion(float amount)
		{
			EngineApplicationInterface.IScene.SetLensDistortion(base.Pointer, amount);
		}

		// Token: 0x06000B42 RID: 2882 RVA: 0x0000BE58 File Offset: 0x0000A058
		public void SetHexagonVignetteAlpha(float amount)
		{
			EngineApplicationInterface.IScene.SetHexagonVignetteAlpha(base.Pointer, amount);
		}

		// Token: 0x06000B43 RID: 2883 RVA: 0x0000BE6B File Offset: 0x0000A06B
		public void SetMinExposure(float minExposure)
		{
			EngineApplicationInterface.IScene.SetMinExposure(base.Pointer, minExposure);
		}

		// Token: 0x06000B44 RID: 2884 RVA: 0x0000BE7E File Offset: 0x0000A07E
		public void SetMaxExposure(float maxExposure)
		{
			EngineApplicationInterface.IScene.SetMaxExposure(base.Pointer, maxExposure);
		}

		// Token: 0x06000B45 RID: 2885 RVA: 0x0000BE91 File Offset: 0x0000A091
		public void SetTargetExposure(float targetExposure)
		{
			EngineApplicationInterface.IScene.SetTargetExposure(base.Pointer, targetExposure);
		}

		// Token: 0x06000B46 RID: 2886 RVA: 0x0000BEA4 File Offset: 0x0000A0A4
		public void SetMiddleGray(float middleGray)
		{
			EngineApplicationInterface.IScene.SetMiddleGray(base.Pointer, middleGray);
		}

		// Token: 0x06000B47 RID: 2887 RVA: 0x0000BEB7 File Offset: 0x0000A0B7
		public void SetBloomStrength(float bloomStrength)
		{
			EngineApplicationInterface.IScene.SetBloomStrength(base.Pointer, bloomStrength);
		}

		// Token: 0x06000B48 RID: 2888 RVA: 0x0000BECA File Offset: 0x0000A0CA
		public void SetBloomAmount(float bloomAmount)
		{
			EngineApplicationInterface.IScene.SetBloomAmount(base.Pointer, bloomAmount);
		}

		// Token: 0x06000B49 RID: 2889 RVA: 0x0000BEDD File Offset: 0x0000A0DD
		public void SetGrainAmount(float grainAmount)
		{
			EngineApplicationInterface.IScene.SetGrainAmount(base.Pointer, grainAmount);
		}

		// Token: 0x06000B4A RID: 2890 RVA: 0x0000BEF0 File Offset: 0x0000A0F0
		public GameEntity AddItemEntity(ref MatrixFrame placementFrame, MetaMesh metaMesh)
		{
			return EngineApplicationInterface.IScene.AddItemEntity(base.Pointer, ref placementFrame, metaMesh.Pointer);
		}

		// Token: 0x06000B4B RID: 2891 RVA: 0x0000BF09 File Offset: 0x0000A109
		public void RemoveEntity(GameEntity entity, int removeReason)
		{
			EngineApplicationInterface.IScene.RemoveEntity(base.Pointer, entity.Pointer, removeReason);
		}

		// Token: 0x06000B4C RID: 2892 RVA: 0x0000BF22 File Offset: 0x0000A122
		public void RemoveEntity(WeakGameEntity entity, int removeReason)
		{
			EngineApplicationInterface.IScene.RemoveEntity(base.Pointer, entity.Pointer, removeReason);
		}

		// Token: 0x06000B4D RID: 2893 RVA: 0x0000BF3C File Offset: 0x0000A13C
		public bool AttachEntity(GameEntity entity, bool showWarnings = false)
		{
			return EngineApplicationInterface.IScene.AttachEntity(base.Pointer, entity.Pointer, showWarnings);
		}

		// Token: 0x06000B4E RID: 2894 RVA: 0x0000BF55 File Offset: 0x0000A155
		public bool AttachEntity(WeakGameEntity entity, bool showWarnings = false)
		{
			return EngineApplicationInterface.IScene.AttachEntity(base.Pointer, entity.Pointer, showWarnings);
		}

		// Token: 0x06000B4F RID: 2895 RVA: 0x0000BF6F File Offset: 0x0000A16F
		public void AddEntityWithMesh(Mesh mesh, ref MatrixFrame frame)
		{
			EngineApplicationInterface.IScene.AddEntityWithMesh(base.Pointer, mesh.Pointer, ref frame);
		}

		// Token: 0x06000B50 RID: 2896 RVA: 0x0000BF88 File Offset: 0x0000A188
		public void AddEntityWithMultiMesh(MetaMesh mesh, ref MatrixFrame frame)
		{
			EngineApplicationInterface.IScene.AddEntityWithMultiMesh(base.Pointer, mesh.Pointer, ref frame);
		}

		// Token: 0x06000B51 RID: 2897 RVA: 0x0000BFA1 File Offset: 0x0000A1A1
		public void Tick(float dt)
		{
			EngineApplicationInterface.IScene.Tick(base.Pointer, dt);
		}

		// Token: 0x06000B52 RID: 2898 RVA: 0x0000BFB4 File Offset: 0x0000A1B4
		public void ClearAll()
		{
			EngineApplicationInterface.IScene.ClearAll(base.Pointer);
		}

		// Token: 0x06000B53 RID: 2899 RVA: 0x0000BFC8 File Offset: 0x0000A1C8
		public void SetDefaultLighting()
		{
			Vec3 vec = new Vec3(1.15f, 1.2f, 1.25f, -1f);
			Vec3 vec2 = new Vec3(1f, -1f, -1f, -1f);
			vec2.Normalize();
			this.SetSunLight(ref vec, ref vec2);
			this.SetShadow(false);
		}

		// Token: 0x06000B54 RID: 2900 RVA: 0x0000C024 File Offset: 0x0000A224
		public bool CalculateEffectiveLighting()
		{
			return EngineApplicationInterface.IScene.CalculateEffectiveLighting(base.Pointer);
		}

		// Token: 0x06000B55 RID: 2901 RVA: 0x0000C036 File Offset: 0x0000A236
		public bool GetPathDistanceBetweenPositions(ref WorldPosition point0, ref WorldPosition point1, float agentRadius, out float pathDistance)
		{
			pathDistance = 0f;
			return EngineApplicationInterface.IScene.GetPathDistanceBetweenPositions(base.Pointer, ref point0, ref point1, agentRadius, ref pathDistance);
		}

		// Token: 0x06000B56 RID: 2902 RVA: 0x0000C055 File Offset: 0x0000A255
		public bool IsLineToPointClear(ref WorldPosition position, ref WorldPosition destination, float agentRadius)
		{
			return EngineApplicationInterface.IScene.IsLineToPointClear2(base.Pointer, position.GetNavMesh(), position.AsVec2, destination.AsVec2, agentRadius);
		}

		// Token: 0x06000B57 RID: 2903 RVA: 0x0000C07A File Offset: 0x0000A27A
		public bool IsLineToPointClear(UIntPtr startingFace, Vec2 position, Vec2 destination, float agentRadius)
		{
			return EngineApplicationInterface.IScene.IsLineToPointClear2(base.Pointer, startingFace, position, destination, agentRadius);
		}

		// Token: 0x06000B58 RID: 2904 RVA: 0x0000C091 File Offset: 0x0000A291
		public bool IsLineToPointClear(int startingFace, Vec2 position, Vec2 destination, float agentRadius)
		{
			return EngineApplicationInterface.IScene.IsLineToPointClear(base.Pointer, startingFace, position, destination, agentRadius);
		}

		// Token: 0x06000B59 RID: 2905 RVA: 0x0000C0A8 File Offset: 0x0000A2A8
		public Vec2 GetLastPointOnNavigationMeshFromPositionToDestination(int startingFace, Vec2 position, Vec2 destination, int[] excludedFaceIds)
		{
			return EngineApplicationInterface.IScene.GetLastPointOnNavigationMeshFromPositionToDestination(base.Pointer, startingFace, position, destination, excludedFaceIds, (excludedFaceIds != null) ? excludedFaceIds.Length : 0);
		}

		// Token: 0x06000B5A RID: 2906 RVA: 0x0000C0CA File Offset: 0x0000A2CA
		public Vec2 GetLastPositionOnNavMeshFaceForPointAndDirection(PathFaceRecord record, Vec2 position, Vec2 destination)
		{
			return EngineApplicationInterface.IScene.GetLastPositionOnNavMeshFaceForPointAndDirection(base.Pointer, record, position, destination);
		}

		// Token: 0x06000B5B RID: 2907 RVA: 0x0000C0E0 File Offset: 0x0000A2E0
		public Vec3 GetLastPointOnNavigationMeshFromWorldPositionToDestination(ref WorldPosition position, Vec2 destination)
		{
			return EngineApplicationInterface.IScene.GetLastPointOnNavigationMeshFromWorldPositionToDestination(base.Pointer, ref position, destination);
		}

		// Token: 0x06000B5C RID: 2908 RVA: 0x0000C0F4 File Offset: 0x0000A2F4
		public bool DoesPathExistBetweenFaces(int firstNavMeshFace, int secondNavMeshFace, bool ignoreDisabled)
		{
			return EngineApplicationInterface.IScene.DoesPathExistBetweenFaces(base.Pointer, firstNavMeshFace, secondNavMeshFace, ignoreDisabled);
		}

		// Token: 0x06000B5D RID: 2909 RVA: 0x0000C109 File Offset: 0x0000A309
		public bool GetHeightAtPoint(Vec2 point, BodyFlags excludeBodyFlags, ref float height)
		{
			return EngineApplicationInterface.IScene.GetHeightAtPoint(base.Pointer, point, excludeBodyFlags, ref height);
		}

		// Token: 0x06000B5E RID: 2910 RVA: 0x0000C11E File Offset: 0x0000A31E
		public Vec3 GetNormalAt(Vec2 position)
		{
			return EngineApplicationInterface.IScene.GetNormalAt(base.Pointer, position);
		}

		// Token: 0x06000B5F RID: 2911 RVA: 0x0000C134 File Offset: 0x0000A334
		public void GetEntities(ref List<GameEntity> entities)
		{
			NativeObjectArray nativeObjectArray = NativeObjectArray.Create();
			EngineApplicationInterface.IScene.GetEntities(base.Pointer, nativeObjectArray.Pointer);
			for (int i = 0; i < nativeObjectArray.Count; i++)
			{
				entities.Add(nativeObjectArray.GetElementAt(i) as GameEntity);
			}
		}

		// Token: 0x06000B60 RID: 2912 RVA: 0x0000C181 File Offset: 0x0000A381
		public void GetRootEntities(NativeObjectArray entities)
		{
			EngineApplicationInterface.IScene.GetRootEntities(this, entities);
		}

		// Token: 0x1700007D RID: 125
		// (get) Token: 0x06000B61 RID: 2913 RVA: 0x0000C18F File Offset: 0x0000A38F
		public int RootEntityCount
		{
			get
			{
				return EngineApplicationInterface.IScene.GetRootEntityCount(base.Pointer);
			}
		}

		// Token: 0x06000B62 RID: 2914 RVA: 0x0000C1A4 File Offset: 0x0000A3A4
		public int SelectEntitiesInBoxWithScriptComponent<T>(ref Vec3 boundingBoxMin, ref Vec3 boundingBoxMax, WeakGameEntity[] entitiesOutput, UIntPtr[] entityIds) where T : ScriptComponentBehavior
		{
			string name = typeof(T).Name;
			int num = EngineApplicationInterface.IScene.SelectEntitiesInBoxWithScriptComponent(base.Pointer, ref boundingBoxMin, ref boundingBoxMax, entityIds, entitiesOutput.Length, name);
			for (int i = 0; i < num; i++)
			{
				entitiesOutput[i] = new WeakGameEntity(entityIds[i]);
			}
			return num;
		}

		// Token: 0x06000B63 RID: 2915 RVA: 0x0000C1F7 File Offset: 0x0000A3F7
		public int SelectEntitiesCollidedWith(ref Ray ray, Intersection[] intersectionsOutput, UIntPtr[] entityIds)
		{
			return EngineApplicationInterface.IScene.SelectEntitiesCollidedWith(base.Pointer, ref ray, entityIds, intersectionsOutput);
		}

		// Token: 0x06000B64 RID: 2916 RVA: 0x0000C20C File Offset: 0x0000A40C
		public bool RayCastExcludingTwoEntities(BodyFlags flags, in Ray ray, WeakGameEntity entity1, WeakGameEntity entity2)
		{
			return EngineApplicationInterface.IScene.RayCastExcludingTwoEntities(flags, base.Pointer, ray, entity1.Pointer, entity2.Pointer);
		}

		// Token: 0x06000B65 RID: 2917 RVA: 0x0000C230 File Offset: 0x0000A430
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

		// Token: 0x06000B66 RID: 2918 RVA: 0x0000C292 File Offset: 0x0000A492
		public int GenerateContactsWithCapsuleAgainstEntity(ref CapsuleData capsule, BodyFlags excludeFlags, WeakGameEntity entity, Intersection[] intersectionsOutput)
		{
			return EngineApplicationInterface.IScene.GenerateContactsWithCapsuleAgainstEntity(base.Pointer, ref capsule, excludeFlags, entity.Pointer, intersectionsOutput);
		}

		// Token: 0x06000B67 RID: 2919 RVA: 0x0000C2AF File Offset: 0x0000A4AF
		public void InvalidateTerrainPhysicsMaterials()
		{
			EngineApplicationInterface.IScene.InvalidateTerrainPhysicsMaterials(base.Pointer);
		}

		// Token: 0x06000B68 RID: 2920 RVA: 0x0000C2C4 File Offset: 0x0000A4C4
		public void Read(string sceneName)
		{
			SceneInitializationData sceneInitializationData = new SceneInitializationData(true);
			EngineApplicationInterface.IScene.Read(base.Pointer, sceneName, ref sceneInitializationData, "");
		}

		// Token: 0x06000B69 RID: 2921 RVA: 0x0000C2F1 File Offset: 0x0000A4F1
		public void Read(string sceneName, string moduleId, ref SceneInitializationData initData, string forcedAtmoName = "")
		{
			EngineApplicationInterface.IScene.ReadInModule(base.Pointer, sceneName, moduleId, ref initData, forcedAtmoName);
		}

		// Token: 0x06000B6A RID: 2922 RVA: 0x0000C308 File Offset: 0x0000A508
		public void Read(string sceneName, ref SceneInitializationData initData, string forcedAtmoName = "")
		{
			EngineApplicationInterface.IScene.Read(base.Pointer, sceneName, ref initData, forcedAtmoName);
		}

		// Token: 0x06000B6B RID: 2923 RVA: 0x0000C320 File Offset: 0x0000A520
		public MatrixFrame ReadAndCalculateInitialCamera()
		{
			MatrixFrame result = default(MatrixFrame);
			EngineApplicationInterface.IScene.ReadAndCalculateInitialCamera(base.Pointer, ref result);
			return result;
		}

		// Token: 0x06000B6C RID: 2924 RVA: 0x0000C348 File Offset: 0x0000A548
		public void OptimizeScene(bool optimizeFlora = true, bool optimizeOro = false)
		{
			EngineApplicationInterface.IScene.OptimizeScene(base.Pointer, optimizeFlora, optimizeOro);
		}

		// Token: 0x06000B6D RID: 2925 RVA: 0x0000C35C File Offset: 0x0000A55C
		public float GetTerrainHeight(Vec2 position, bool checkHoles = true)
		{
			return EngineApplicationInterface.IScene.GetTerrainHeight(base.Pointer, position, checkHoles);
		}

		// Token: 0x06000B6E RID: 2926 RVA: 0x0000C370 File Offset: 0x0000A570
		public void CheckResources(bool checkInvisibleEntities)
		{
			EngineApplicationInterface.IScene.CheckResources(base.Pointer, checkInvisibleEntities);
		}

		// Token: 0x06000B6F RID: 2927 RVA: 0x0000C383 File Offset: 0x0000A583
		public void ForceLoadResources(bool checkInvisibleEntities)
		{
			EngineApplicationInterface.IScene.ForceLoadResources(base.Pointer, checkInvisibleEntities);
		}

		// Token: 0x06000B70 RID: 2928 RVA: 0x0000C396 File Offset: 0x0000A596
		public void SetDepthOfFieldParameters(float depthOfFieldFocusStart, float depthOfFieldFocusEnd, bool isVignetteOn)
		{
			EngineApplicationInterface.IScene.SetDofParams(base.Pointer, depthOfFieldFocusStart, depthOfFieldFocusEnd, isVignetteOn);
		}

		// Token: 0x06000B71 RID: 2929 RVA: 0x0000C3AB File Offset: 0x0000A5AB
		public void SetDepthOfFieldFocus(float depthOfFieldFocus)
		{
			EngineApplicationInterface.IScene.SetDofFocus(base.Pointer, depthOfFieldFocus);
		}

		// Token: 0x06000B72 RID: 2930 RVA: 0x0000C3BE File Offset: 0x0000A5BE
		public void ResetDepthOfFieldParams()
		{
			EngineApplicationInterface.IScene.SetDofFocus(base.Pointer, 0f);
			EngineApplicationInterface.IScene.SetDofParams(base.Pointer, 0f, 0f, true);
		}

		// Token: 0x1700007E RID: 126
		// (get) Token: 0x06000B73 RID: 2931 RVA: 0x0000C3F0 File Offset: 0x0000A5F0
		public bool HasTerrainHeightmap
		{
			get
			{
				return EngineApplicationInterface.IScene.HasTerrainHeightmap(base.Pointer);
			}
		}

		// Token: 0x1700007F RID: 127
		// (get) Token: 0x06000B74 RID: 2932 RVA: 0x0000C402 File Offset: 0x0000A602
		public bool ContainsTerrain
		{
			get
			{
				return EngineApplicationInterface.IScene.ContainsTerrain(base.Pointer);
			}
		}

		// Token: 0x17000080 RID: 128
		// (get) Token: 0x06000B76 RID: 2934 RVA: 0x0000C427 File Offset: 0x0000A627
		// (set) Token: 0x06000B75 RID: 2933 RVA: 0x0000C414 File Offset: 0x0000A614
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

		// Token: 0x17000081 RID: 129
		// (get) Token: 0x06000B77 RID: 2935 RVA: 0x0000C43C File Offset: 0x0000A63C
		public bool IsDayTime
		{
			get
			{
				int num = MathF.Floor(this.TimeOfDay);
				return num >= 2 && num < 22;
			}
		}

		// Token: 0x17000082 RID: 130
		// (get) Token: 0x06000B78 RID: 2936 RVA: 0x0000C460 File Offset: 0x0000A660
		public bool IsAtmosphereIndoor
		{
			get
			{
				return EngineApplicationInterface.IScene.IsAtmosphereIndoor(base.Pointer);
			}
		}

		// Token: 0x06000B79 RID: 2937 RVA: 0x0000C472 File Offset: 0x0000A672
		public void PreloadForRendering()
		{
			EngineApplicationInterface.IScene.PreloadForRendering(base.Pointer);
		}

		// Token: 0x17000083 RID: 131
		// (get) Token: 0x06000B7A RID: 2938 RVA: 0x0000C484 File Offset: 0x0000A684
		public Vec3 LastFinalRenderCameraPosition
		{
			get
			{
				return EngineApplicationInterface.IScene.GetLastFinalRenderCameraPosition(base.Pointer);
			}
		}

		// Token: 0x17000084 RID: 132
		// (get) Token: 0x06000B7B RID: 2939 RVA: 0x0000C498 File Offset: 0x0000A698
		public MatrixFrame LastFinalRenderCameraFrame
		{
			get
			{
				MatrixFrame result = default(MatrixFrame);
				EngineApplicationInterface.IScene.GetLastFinalRenderCameraFrame(base.Pointer, ref result);
				return result;
			}
		}

		// Token: 0x06000B7C RID: 2940 RVA: 0x0000C4C0 File Offset: 0x0000A6C0
		public void SetColorGradeBlend(string texture1, string texture2, float alpha)
		{
			EngineApplicationInterface.IScene.SetColorGradeBlend(base.Pointer, texture1, texture2, alpha);
		}

		// Token: 0x06000B7D RID: 2941 RVA: 0x0000C4D5 File Offset: 0x0000A6D5
		public float GetGroundHeightAtPosition(Vec3 position, BodyFlags excludeFlags = BodyFlags.CommonCollisionExcludeFlags)
		{
			return EngineApplicationInterface.IScene.GetGroundHeightAtPosition(base.Pointer, position, (uint)excludeFlags);
		}

		// Token: 0x06000B7E RID: 2942 RVA: 0x0000C4E9 File Offset: 0x0000A6E9
		public float GetGroundHeightAndBodyFlagsAtPosition(Vec3 position, out BodyFlags contactPointFlags, BodyFlags excludeFlags = BodyFlags.CommonCollisionExcludeFlags)
		{
			return EngineApplicationInterface.IScene.GetGroundHeightAndBodyFlagsAtPosition(base.Pointer, position, out contactPointFlags, excludeFlags);
		}

		// Token: 0x06000B7F RID: 2943 RVA: 0x0000C4FE File Offset: 0x0000A6FE
		public float GetGroundHeightAtPosition(Vec3 position, out Vec3 normal, BodyFlags excludeFlags = BodyFlags.CommonCollisionExcludeFlags)
		{
			normal = Vec3.Invalid;
			return EngineApplicationInterface.IScene.GetGroundHeightAtPosition(base.Pointer, position, (uint)excludeFlags);
		}

		// Token: 0x06000B80 RID: 2944 RVA: 0x0000C51D File Offset: 0x0000A71D
		public void PauseSceneSounds()
		{
			EngineApplicationInterface.IScene.PauseSceneSounds(base.Pointer);
		}

		// Token: 0x06000B81 RID: 2945 RVA: 0x0000C52F File Offset: 0x0000A72F
		public void ResumeSceneSounds()
		{
			EngineApplicationInterface.IScene.ResumeSceneSounds(base.Pointer);
		}

		// Token: 0x06000B82 RID: 2946 RVA: 0x0000C541 File Offset: 0x0000A741
		public void FinishSceneSounds()
		{
			EngineApplicationInterface.IScene.FinishSceneSounds(base.Pointer);
		}

		// Token: 0x06000B83 RID: 2947 RVA: 0x0000C554 File Offset: 0x0000A754
		public bool BoxCastOnlyForCamera(Vec3[] boxPoints, in Vec3 centerPoint, bool castSupportRay, in Vec3 supportRaycastPoint, in Vec3 dir, float distance, WeakGameEntity ignoredEntity, out float collisionDistance, out Vec3 closestPoint, out WeakGameEntity collidedEntity, BodyFlags excludedBodyFlags = BodyFlags.Disabled | BodyFlags.Dynamic | BodyFlags.Ladder | BodyFlags.OnlyCollideWithRaycast | BodyFlags.AILimiter | BodyFlags.Barrier | BodyFlags.Barrier3D | BodyFlags.Ragdoll | BodyFlags.RagdollLimiter | BodyFlags.DroppedItem | BodyFlags.DoNotCollideWithRaycast | BodyFlags.DontCollideWithCamera | BodyFlags.WaterBody | BodyFlags.AgentOnly | BodyFlags.MissileOnly | BodyFlags.StealthBox)
		{
			collisionDistance = float.NaN;
			closestPoint = Vec3.Invalid;
			UIntPtr zero = UIntPtr.Zero;
			bool flag = castSupportRay && EngineApplicationInterface.IScene.RayCastForClosestEntityOrTerrainIgnoreEntity(base.Pointer, supportRaycastPoint, centerPoint, 0f, ref collisionDistance, ref closestPoint, ref zero, excludedBodyFlags, ignoredEntity.Pointer);
			if (!flag)
			{
				flag = EngineApplicationInterface.IScene.BoxCastOnlyForCamera(base.Pointer, boxPoints, centerPoint, dir, distance, ignoredEntity.Pointer, ref collisionDistance, ref closestPoint, ref zero, excludedBodyFlags);
			}
			if (flag && zero != UIntPtr.Zero)
			{
				collidedEntity = new WeakGameEntity(zero);
			}
			else
			{
				collidedEntity = WeakGameEntity.Invalid;
			}
			return flag;
		}

		// Token: 0x06000B84 RID: 2948 RVA: 0x0000C600 File Offset: 0x0000A800
		public bool BoxCast(Vec3 boxMin, Vec3 boxMax, bool castSupportRay, Vec3 supportRaycastPoint, Vec3 dir, float distance, out float collisionDistance, out Vec3 closestPoint, out WeakGameEntity collidedEntity, BodyFlags excludedBodyFlags = BodyFlags.CameraCollisionRayCastExludeFlags)
		{
			collisionDistance = float.NaN;
			closestPoint = Vec3.Invalid;
			UIntPtr zero = UIntPtr.Zero;
			Vec3 vec = (boxMin + boxMax) * 0.5f;
			bool flag = castSupportRay && EngineApplicationInterface.IScene.RayCastForClosestEntityOrTerrain(base.Pointer, supportRaycastPoint, vec, 0f, ref collisionDistance, ref closestPoint, ref zero, excludedBodyFlags, false);
			if (!flag)
			{
				flag = EngineApplicationInterface.IScene.BoxCast(base.Pointer, ref boxMin, ref boxMax, ref dir, distance, ref collisionDistance, ref closestPoint, ref zero, excludedBodyFlags);
			}
			if (flag && zero != UIntPtr.Zero)
			{
				collidedEntity = new WeakGameEntity(zero);
			}
			else
			{
				collidedEntity = WeakGameEntity.Invalid;
			}
			return flag;
		}

		// Token: 0x06000B85 RID: 2949 RVA: 0x0000C6B4 File Offset: 0x0000A8B4
		public bool RayCastForClosestEntityOrTerrain(Vec3 sourcePoint, Vec3 targetPoint, out float collisionDistance, out Vec3 closestPoint, out WeakGameEntity collidedEntity, float rayThickness = 0.01f, BodyFlags excludeBodyFlags = BodyFlags.CommonFocusRayCastExcludeFlags)
		{
			collisionDistance = float.NaN;
			closestPoint = Vec3.Invalid;
			UIntPtr zero = UIntPtr.Zero;
			bool flag = EngineApplicationInterface.IScene.RayCastForClosestEntityOrTerrain(base.Pointer, sourcePoint, targetPoint, rayThickness, ref collisionDistance, ref closestPoint, ref zero, excludeBodyFlags, false);
			if (flag && zero != UIntPtr.Zero)
			{
				collidedEntity = new WeakGameEntity(zero);
				return flag;
			}
			collidedEntity = WeakGameEntity.Invalid;
			return flag;
		}

		// Token: 0x06000B86 RID: 2950 RVA: 0x0000C724 File Offset: 0x0000A924
		public bool RayCastForClosestEntityOrTerrainFixedPhysics(Vec3 sourcePoint, Vec3 targetPoint, out float collisionDistance, out Vec3 closestPoint, out WeakGameEntity collidedEntity, float rayThickness = 0.01f, BodyFlags excludeBodyFlags = BodyFlags.CommonFocusRayCastExcludeFlags)
		{
			collisionDistance = float.NaN;
			closestPoint = Vec3.Invalid;
			UIntPtr zero = UIntPtr.Zero;
			bool flag = EngineApplicationInterface.IScene.RayCastForClosestEntityOrTerrain(base.Pointer, sourcePoint, targetPoint, rayThickness, ref collisionDistance, ref closestPoint, ref zero, excludeBodyFlags, true);
			if (flag && zero != UIntPtr.Zero)
			{
				collidedEntity = new WeakGameEntity(zero);
				return flag;
			}
			collidedEntity = WeakGameEntity.Invalid;
			return flag;
		}

		// Token: 0x06000B87 RID: 2951 RVA: 0x0000C794 File Offset: 0x0000A994
		public bool FocusRayCastForFixedPhysics(Vec3 sourcePoint, Vec3 targetPoint, out float collisionDistance, out Vec3 closestPoint, out WeakGameEntity collidedEntity, float rayThickness = 0.01f, BodyFlags excludeBodyFlags = BodyFlags.CommonFocusRayCastExcludeFlags)
		{
			collisionDistance = float.NaN;
			closestPoint = Vec3.Invalid;
			UIntPtr zero = UIntPtr.Zero;
			bool flag = EngineApplicationInterface.IScene.FocusRayCastForFixedPhysics(base.Pointer, sourcePoint, targetPoint, rayThickness, ref collisionDistance, ref closestPoint, ref zero, excludeBodyFlags, true);
			if (flag && zero != UIntPtr.Zero)
			{
				collidedEntity = new WeakGameEntity(zero);
				return flag;
			}
			collidedEntity = WeakGameEntity.Invalid;
			return flag;
		}

		// Token: 0x06000B88 RID: 2952 RVA: 0x0000C804 File Offset: 0x0000AA04
		public bool RayCastForClosestEntityOrTerrain(Vec3 sourcePoint, Vec3 targetPoint, out float collisionDistance, out WeakGameEntity collidedEntity, float rayThickness = 0.01f, BodyFlags excludeBodyFlags = BodyFlags.CommonFocusRayCastExcludeFlags)
		{
			Vec3 vec;
			return this.RayCastForClosestEntityOrTerrain(sourcePoint, targetPoint, out collisionDistance, out vec, out collidedEntity, rayThickness, excludeBodyFlags);
		}

		// Token: 0x06000B89 RID: 2953 RVA: 0x0000C824 File Offset: 0x0000AA24
		public bool RayCastForClosestEntityOrTerrainFixedPhysics(Vec3 sourcePoint, Vec3 targetPoint, out float collisionDistance, out WeakGameEntity collidedEntity, float rayThickness = 0.01f, BodyFlags excludeBodyFlags = BodyFlags.CommonFocusRayCastExcludeFlags)
		{
			Vec3 vec;
			return this.RayCastForClosestEntityOrTerrainFixedPhysics(sourcePoint, targetPoint, out collisionDistance, out vec, out collidedEntity, rayThickness, excludeBodyFlags);
		}

		// Token: 0x06000B8A RID: 2954 RVA: 0x0000C844 File Offset: 0x0000AA44
		public bool RayCastForRamming(in Vec3 sourcePoint, in Vec3 targetPoint, WeakGameEntity ignoredEntity, float rayThickness, out float collisionDistance, out Vec3 intersectionPoint, out WeakGameEntity collidedEntity, BodyFlags excludeBodyFlags = BodyFlags.CommonFocusRayCastExcludeFlags, BodyFlags includeBodyFlags = BodyFlags.None)
		{
			collisionDistance = float.NaN;
			intersectionPoint = Vec3.Invalid;
			UIntPtr zero = UIntPtr.Zero;
			bool flag = EngineApplicationInterface.IScene.RayCastForRamming(base.Pointer, sourcePoint, targetPoint, rayThickness, ref collisionDistance, ref intersectionPoint, ref zero, excludeBodyFlags, includeBodyFlags, ignoredEntity.Pointer);
			if (flag && zero != UIntPtr.Zero)
			{
				collidedEntity = new WeakGameEntity(zero);
				return flag;
			}
			collidedEntity = WeakGameEntity.Invalid;
			return flag;
		}

		// Token: 0x06000B8B RID: 2955 RVA: 0x0000C8BC File Offset: 0x0000AABC
		public bool RayCastForClosestEntityOrTerrainIgnoreEntity(in Vec3 sourcePoint, in Vec3 targetPoint, WeakGameEntity ignoredEntity, out float collisionDistance, out GameEntity collidedEntity, float rayThickness = 0.01f, BodyFlags excludeBodyFlags = BodyFlags.CommonFocusRayCastExcludeFlags)
		{
			Vec3 invalid = Vec3.Invalid;
			UIntPtr zero = UIntPtr.Zero;
			collisionDistance = float.NaN;
			bool flag = EngineApplicationInterface.IScene.RayCastForClosestEntityOrTerrainIgnoreEntity(base.Pointer, sourcePoint, targetPoint, rayThickness, ref collisionDistance, ref invalid, ref zero, excludeBodyFlags, ignoredEntity.Pointer);
			if (flag && zero != UIntPtr.Zero)
			{
				collidedEntity = new GameEntity(zero);
				return flag;
			}
			collidedEntity = null;
			return flag;
		}

		// Token: 0x06000B8C RID: 2956 RVA: 0x0000C920 File Offset: 0x0000AB20
		public bool RayCastForClosestEntityOrTerrain(Vec3 sourcePoint, Vec3 targetPoint, out float collisionDistance, out Vec3 closestPoint, float rayThickness = 0.01f, BodyFlags excludeBodyFlags = BodyFlags.CommonFocusRayCastExcludeFlags)
		{
			collisionDistance = float.NaN;
			closestPoint = Vec3.Invalid;
			UIntPtr zero = UIntPtr.Zero;
			return EngineApplicationInterface.IScene.RayCastForClosestEntityOrTerrain(base.Pointer, sourcePoint, targetPoint, rayThickness, ref collisionDistance, ref closestPoint, ref zero, excludeBodyFlags, false);
		}

		// Token: 0x06000B8D RID: 2957 RVA: 0x0000C964 File Offset: 0x0000AB64
		public bool RayCastForClosestEntityOrTerrainFixedPhysics(Vec3 sourcePoint, Vec3 targetPoint, out float collisionDistance, out Vec3 closestPoint, float rayThickness = 0.01f, BodyFlags excludeBodyFlags = BodyFlags.CommonFocusRayCastExcludeFlags)
		{
			collisionDistance = float.NaN;
			closestPoint = Vec3.Invalid;
			UIntPtr zero = UIntPtr.Zero;
			return EngineApplicationInterface.IScene.RayCastForClosestEntityOrTerrain(base.Pointer, sourcePoint, targetPoint, rayThickness, ref collisionDistance, ref closestPoint, ref zero, excludeBodyFlags, true);
		}

		// Token: 0x06000B8E RID: 2958 RVA: 0x0000C9A8 File Offset: 0x0000ABA8
		public bool RayCastForClosestEntityOrTerrainFixedPhysics(Vec3 sourcePoint, Vec3 targetPoint, out float collisionDistance, float rayThickness = 0.01f, BodyFlags excludeBodyFlags = BodyFlags.CommonFocusRayCastExcludeFlags)
		{
			Vec3 vec;
			return this.RayCastForClosestEntityOrTerrainFixedPhysics(sourcePoint, targetPoint, out collisionDistance, out vec, rayThickness, excludeBodyFlags);
		}

		// Token: 0x06000B8F RID: 2959 RVA: 0x0000C9C4 File Offset: 0x0000ABC4
		public bool RayCastForClosestEntityOrTerrain(Vec3 sourcePoint, Vec3 targetPoint, out float collisionDistance, float rayThickness = 0.01f, BodyFlags excludeBodyFlags = BodyFlags.CommonFocusRayCastExcludeFlags)
		{
			Vec3 vec;
			return this.RayCastForClosestEntityOrTerrain(sourcePoint, targetPoint, out collisionDistance, out vec, rayThickness, excludeBodyFlags);
		}

		// Token: 0x06000B90 RID: 2960 RVA: 0x0000C9E0 File Offset: 0x0000ABE0
		public void ImportNavigationMeshPrefab(string navMeshPrefabName, int navMeshGroupShift)
		{
			EngineApplicationInterface.IScene.LoadNavMeshPrefab(base.Pointer, navMeshPrefabName, navMeshGroupShift);
		}

		// Token: 0x06000B91 RID: 2961 RVA: 0x0000C9F4 File Offset: 0x0000ABF4
		public void ImportNavigationMeshPrefabWithFrame(string navMeshPrefabName, MatrixFrame frame)
		{
			EngineApplicationInterface.IScene.LoadNavMeshPrefabWithFrame(base.Pointer, navMeshPrefabName, frame);
		}

		// Token: 0x06000B92 RID: 2962 RVA: 0x0000CA08 File Offset: 0x0000AC08
		public void SaveNavMeshPrefabWithFrame(string navMeshPrefabName, MatrixFrame frame)
		{
			EngineApplicationInterface.IScene.SaveNavMeshPrefabWithFrame(base.Pointer, navMeshPrefabName, frame);
		}

		// Token: 0x06000B93 RID: 2963 RVA: 0x0000CA1C File Offset: 0x0000AC1C
		public void SetNavMeshRegionMap(bool[] regionMap)
		{
			EngineApplicationInterface.IScene.SetNavMeshRegionMap(base.Pointer, regionMap, regionMap.Length);
		}

		// Token: 0x06000B94 RID: 2964 RVA: 0x0000CA32 File Offset: 0x0000AC32
		public void MarkFacesWithIdAsLadder(int faceGroupId, bool isLadder)
		{
			EngineApplicationInterface.IScene.MarkFacesWithIdAsLadder(base.Pointer, faceGroupId, isLadder);
		}

		// Token: 0x06000B95 RID: 2965 RVA: 0x0000CA46 File Offset: 0x0000AC46
		public int SetAbilityOfFacesWithId(int faceGroupId, bool isEnabled)
		{
			return EngineApplicationInterface.IScene.SetAbilityOfFacesWithId(base.Pointer, faceGroupId, isEnabled);
		}

		// Token: 0x06000B96 RID: 2966 RVA: 0x0000CA5A File Offset: 0x0000AC5A
		public bool SwapFaceConnectionsWithID(int hubFaceGroupID, int toBeSeparatedFaceGroupId, int toBeMergedFaceGroupId, bool canFail)
		{
			return EngineApplicationInterface.IScene.SwapFaceConnectionsWithId(base.Pointer, hubFaceGroupID, toBeSeparatedFaceGroupId, toBeMergedFaceGroupId, canFail);
		}

		// Token: 0x06000B97 RID: 2967 RVA: 0x0000CA71 File Offset: 0x0000AC71
		public void MergeFacesWithId(int faceGroupId0, int faceGroupId1, int newFaceGroupId)
		{
			EngineApplicationInterface.IScene.MergeFacesWithId(base.Pointer, faceGroupId0, faceGroupId1, newFaceGroupId);
		}

		// Token: 0x06000B98 RID: 2968 RVA: 0x0000CA86 File Offset: 0x0000AC86
		public void SeparateFacesWithId(int faceGroupId0, int faceGroupId1)
		{
			EngineApplicationInterface.IScene.SeparateFacesWithId(base.Pointer, faceGroupId0, faceGroupId1);
		}

		// Token: 0x06000B99 RID: 2969 RVA: 0x0000CA9A File Offset: 0x0000AC9A
		public bool IsAnyFaceWithId(int faceGroupId)
		{
			return EngineApplicationInterface.IScene.IsAnyFaceWithId(base.Pointer, faceGroupId);
		}

		// Token: 0x06000B9A RID: 2970 RVA: 0x0000CAB0 File Offset: 0x0000ACB0
		public UIntPtr GetNavigationMeshForPosition(in Vec3 position)
		{
			int num;
			return this.GetNavigationMeshForPosition(position, out num, 1.5f, false);
		}

		// Token: 0x06000B9B RID: 2971 RVA: 0x0000CACC File Offset: 0x0000ACCC
		public UIntPtr GetNearestNavigationMeshForPosition(in Vec3 position, float heightDifferenceLimit, bool excludeDynamicNavigationMeshes)
		{
			return EngineApplicationInterface.IScene.GetNearestNavigationMeshForPosition(base.Pointer, position, heightDifferenceLimit, excludeDynamicNavigationMeshes);
		}

		// Token: 0x06000B9C RID: 2972 RVA: 0x0000CAE1 File Offset: 0x0000ACE1
		public UIntPtr GetNavigationMeshForPosition(in Vec3 position, out int faceGroupId, float heightDifferenceLimit, bool excludeDynamicNavigationMeshes)
		{
			faceGroupId = int.MinValue;
			return EngineApplicationInterface.IScene.GetNavigationMeshForPosition(base.Pointer, position, ref faceGroupId, heightDifferenceLimit, excludeDynamicNavigationMeshes);
		}

		// Token: 0x06000B9D RID: 2973 RVA: 0x0000CAFF File Offset: 0x0000ACFF
		public bool DoesPathExistBetweenPositions(WorldPosition position, WorldPosition destination)
		{
			return EngineApplicationInterface.IScene.DoesPathExistBetweenPositions(base.Pointer, position, destination);
		}

		// Token: 0x06000B9E RID: 2974 RVA: 0x0000CB13 File Offset: 0x0000AD13
		public void SetLandscapeRainMaskData(byte[] data)
		{
			EngineApplicationInterface.IScene.SetLandscapeRainMaskData(base.Pointer, data);
		}

		// Token: 0x06000B9F RID: 2975 RVA: 0x0000CB26 File Offset: 0x0000AD26
		public void EnsurePostfxSystem()
		{
			EngineApplicationInterface.IScene.EnsurePostfxSystem(base.Pointer);
		}

		// Token: 0x06000BA0 RID: 2976 RVA: 0x0000CB38 File Offset: 0x0000AD38
		public void SetBloom(bool mode)
		{
			EngineApplicationInterface.IScene.SetBloom(base.Pointer, mode);
		}

		// Token: 0x06000BA1 RID: 2977 RVA: 0x0000CB4B File Offset: 0x0000AD4B
		public void SetDofMode(bool mode)
		{
			EngineApplicationInterface.IScene.SetDofMode(base.Pointer, mode);
		}

		// Token: 0x06000BA2 RID: 2978 RVA: 0x0000CB5E File Offset: 0x0000AD5E
		public void SetOcclusionMode(bool mode)
		{
			EngineApplicationInterface.IScene.SetOcclusionMode(base.Pointer, mode);
		}

		// Token: 0x06000BA3 RID: 2979 RVA: 0x0000CB71 File Offset: 0x0000AD71
		public void SetExternalInjectionTexture(Texture texture)
		{
			EngineApplicationInterface.IScene.SetExternalInjectionTexture(base.Pointer, texture.Pointer);
		}

		// Token: 0x06000BA4 RID: 2980 RVA: 0x0000CB89 File Offset: 0x0000AD89
		public void SetSunshaftMode(bool mode)
		{
			EngineApplicationInterface.IScene.SetSunshaftMode(base.Pointer, mode);
		}

		// Token: 0x06000BA5 RID: 2981 RVA: 0x0000CB9C File Offset: 0x0000AD9C
		public Vec3 GetSunDirection()
		{
			return EngineApplicationInterface.IScene.GetSunDirection(base.Pointer);
		}

		// Token: 0x06000BA6 RID: 2982 RVA: 0x0000CBAE File Offset: 0x0000ADAE
		public float GetNorthAngle()
		{
			return EngineApplicationInterface.IScene.GetNorthAngle(base.Pointer);
		}

		// Token: 0x06000BA7 RID: 2983 RVA: 0x0000CBC0 File Offset: 0x0000ADC0
		public float GetNorthRotation()
		{
			float northAngle = this.GetNorthAngle();
			return 0.017453292f * -northAngle;
		}

		// Token: 0x06000BA8 RID: 2984 RVA: 0x0000CBDC File Offset: 0x0000ADDC
		public bool GetTerrainMinMaxHeight(out float minHeight, out float maxHeight)
		{
			minHeight = 0f;
			maxHeight = 0f;
			return EngineApplicationInterface.IScene.GetTerrainMinMaxHeight(this, ref minHeight, ref maxHeight);
		}

		// Token: 0x06000BA9 RID: 2985 RVA: 0x0000CBF9 File Offset: 0x0000ADF9
		public void GetPhysicsMinMax(ref Vec3 min_max)
		{
			EngineApplicationInterface.IScene.GetPhysicsMinMax(base.Pointer, ref min_max);
		}

		// Token: 0x06000BAA RID: 2986 RVA: 0x0000CC0C File Offset: 0x0000AE0C
		public bool IsEditorScene()
		{
			return EngineApplicationInterface.IScene.IsEditorScene(base.Pointer);
		}

		// Token: 0x06000BAB RID: 2987 RVA: 0x0000CC1E File Offset: 0x0000AE1E
		public void SetMotionBlurMode(bool mode)
		{
			EngineApplicationInterface.IScene.SetMotionBlurMode(base.Pointer, mode);
		}

		// Token: 0x06000BAC RID: 2988 RVA: 0x0000CC31 File Offset: 0x0000AE31
		public void SetAntialiasingMode(bool mode)
		{
			EngineApplicationInterface.IScene.SetAntialiasingMode(base.Pointer, mode);
		}

		// Token: 0x06000BAD RID: 2989 RVA: 0x0000CC44 File Offset: 0x0000AE44
		public void SetDLSSMode(bool mode)
		{
			EngineApplicationInterface.IScene.SetDLSSMode(base.Pointer, mode);
		}

		// Token: 0x06000BAE RID: 2990 RVA: 0x0000CC57 File Offset: 0x0000AE57
		public IEnumerable<WeakGameEntity> FindWeakEntitiesWithTag(string tag)
		{
			return WeakGameEntity.GetEntitiesWithTag(this, tag);
		}

		// Token: 0x06000BAF RID: 2991 RVA: 0x0000CC60 File Offset: 0x0000AE60
		public WeakGameEntity FindWeakEntityWithTag(string tag)
		{
			return WeakGameEntity.GetFirstEntityWithTag(this, tag);
		}

		// Token: 0x06000BB0 RID: 2992 RVA: 0x0000CC69 File Offset: 0x0000AE69
		public IEnumerable<GameEntity> FindEntitiesWithTag(string tag)
		{
			return GameEntity.GetEntitiesWithTag(this, tag);
		}

		// Token: 0x06000BB1 RID: 2993 RVA: 0x0000CC72 File Offset: 0x0000AE72
		public GameEntity FindEntityWithTag(string tag)
		{
			return GameEntity.GetFirstEntityWithTag(this, tag);
		}

		// Token: 0x06000BB2 RID: 2994 RVA: 0x0000CC7B File Offset: 0x0000AE7B
		public GameEntity FindEntityWithName(string name)
		{
			return GameEntity.GetFirstEntityWithName(this, name);
		}

		// Token: 0x06000BB3 RID: 2995 RVA: 0x0000CC84 File Offset: 0x0000AE84
		public IEnumerable<WeakGameEntity> FindWeakEntitiesWithTagExpression(string expression)
		{
			return WeakGameEntity.GetEntitiesWithTagExpression(this, expression);
		}

		// Token: 0x06000BB4 RID: 2996 RVA: 0x0000CC8D File Offset: 0x0000AE8D
		public IEnumerable<GameEntity> FindEntitiesWithTagExpression(string expression)
		{
			return GameEntity.GetEntitiesWithTagExpression(this, expression);
		}

		// Token: 0x06000BB5 RID: 2997 RVA: 0x0000CC96 File Offset: 0x0000AE96
		public int GetSoftBoundaryVertexCount()
		{
			return EngineApplicationInterface.IScene.GetSoftBoundaryVertexCount(base.Pointer);
		}

		// Token: 0x06000BB6 RID: 2998 RVA: 0x0000CCA8 File Offset: 0x0000AEA8
		public int GetHardBoundaryVertexCount()
		{
			return EngineApplicationInterface.IScene.GetHardBoundaryVertexCount(base.Pointer);
		}

		// Token: 0x06000BB7 RID: 2999 RVA: 0x0000CCBA File Offset: 0x0000AEBA
		public Vec2 GetSoftBoundaryVertex(int index)
		{
			return EngineApplicationInterface.IScene.GetSoftBoundaryVertex(base.Pointer, index);
		}

		// Token: 0x06000BB8 RID: 3000 RVA: 0x0000CCCD File Offset: 0x0000AECD
		public Vec2 GetHardBoundaryVertex(int index)
		{
			return EngineApplicationInterface.IScene.GetHardBoundaryVertex(base.Pointer, index);
		}

		// Token: 0x06000BB9 RID: 3001 RVA: 0x0000CCE0 File Offset: 0x0000AEE0
		public Path GetPathWithName(string name)
		{
			return EngineApplicationInterface.IScene.GetPathWithName(base.Pointer, name);
		}

		// Token: 0x06000BBA RID: 3002 RVA: 0x0000CCF3 File Offset: 0x0000AEF3
		public void DeletePathWithName(string name)
		{
			EngineApplicationInterface.IScene.DeletePathWithName(base.Pointer, name);
		}

		// Token: 0x06000BBB RID: 3003 RVA: 0x0000CD06 File Offset: 0x0000AF06
		public void AddPath(string name)
		{
			EngineApplicationInterface.IScene.AddPath(base.Pointer, name);
		}

		// Token: 0x06000BBC RID: 3004 RVA: 0x0000CD19 File Offset: 0x0000AF19
		public void AddPathPoint(string name, MatrixFrame frame)
		{
			EngineApplicationInterface.IScene.AddPathPoint(base.Pointer, name, ref frame);
		}

		// Token: 0x06000BBD RID: 3005 RVA: 0x0000CD2E File Offset: 0x0000AF2E
		public void GetBoundingBox(out Vec3 min, out Vec3 max)
		{
			min = Vec3.Invalid;
			max = Vec3.Invalid;
			EngineApplicationInterface.IScene.GetBoundingBox(base.Pointer, ref min, ref max);
		}

		// Token: 0x06000BBE RID: 3006 RVA: 0x0000CD58 File Offset: 0x0000AF58
		public void GetSceneLimits(out Vec3 min, out Vec3 max)
		{
			min = Vec3.Invalid;
			max = Vec3.Invalid;
			EngineApplicationInterface.IScene.GetSceneLimits(base.Pointer, ref min, ref max);
		}

		// Token: 0x06000BBF RID: 3007 RVA: 0x0000CD82 File Offset: 0x0000AF82
		public void SetName(string name)
		{
			EngineApplicationInterface.IScene.SetName(base.Pointer, name);
		}

		// Token: 0x06000BC0 RID: 3008 RVA: 0x0000CD95 File Offset: 0x0000AF95
		public string GetName()
		{
			return EngineApplicationInterface.IScene.GetName(base.Pointer);
		}

		// Token: 0x06000BC1 RID: 3009 RVA: 0x0000CDA7 File Offset: 0x0000AFA7
		public string GetModulePath()
		{
			return EngineApplicationInterface.IScene.GetModulePath(base.Pointer);
		}

		// Token: 0x17000085 RID: 133
		// (get) Token: 0x06000BC2 RID: 3010 RVA: 0x0000CDB9 File Offset: 0x0000AFB9
		// (set) Token: 0x06000BC3 RID: 3011 RVA: 0x0000CDCB File Offset: 0x0000AFCB
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

		// Token: 0x06000BC4 RID: 3012 RVA: 0x0000CDDE File Offset: 0x0000AFDE
		public void SetOwnerThread()
		{
			EngineApplicationInterface.IScene.SetOwnerThread(base.Pointer);
		}

		// Token: 0x06000BC5 RID: 3013 RVA: 0x0000CDF0 File Offset: 0x0000AFF0
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

		// Token: 0x06000BC6 RID: 3014 RVA: 0x0000CE4B File Offset: 0x0000B04B
		public void SetUseConstantTime(bool value)
		{
			EngineApplicationInterface.IScene.SetUseConstantTime(base.Pointer, value);
		}

		// Token: 0x06000BC7 RID: 3015 RVA: 0x0000CE5E File Offset: 0x0000B05E
		public bool CheckPointCanSeePoint(Vec3 source, Vec3 target, float? distanceToCheck = null)
		{
			if (distanceToCheck == null)
			{
				distanceToCheck = new float?(source.Distance(target));
			}
			return EngineApplicationInterface.IScene.CheckPointCanSeePoint(base.Pointer, source, target, distanceToCheck.Value);
		}

		// Token: 0x06000BC8 RID: 3016 RVA: 0x0000CE91 File Offset: 0x0000B091
		public void SetPlaySoundEventsAfterReadyToRender(bool value)
		{
			EngineApplicationInterface.IScene.SetPlaySoundEventsAfterReadyToRender(base.Pointer, value);
		}

		// Token: 0x06000BC9 RID: 3017 RVA: 0x0000CEA4 File Offset: 0x0000B0A4
		public void DisableStaticShadows(bool value)
		{
			EngineApplicationInterface.IScene.DisableStaticShadows(base.Pointer, value);
		}

		// Token: 0x06000BCA RID: 3018 RVA: 0x0000CEB7 File Offset: 0x0000B0B7
		public Mesh GetSkyboxMesh()
		{
			return EngineApplicationInterface.IScene.GetSkyboxMesh(base.Pointer);
		}

		// Token: 0x06000BCB RID: 3019 RVA: 0x0000CEC9 File Offset: 0x0000B0C9
		public void SetAtmosphereWithName(string name)
		{
			EngineApplicationInterface.IScene.SetAtmosphereWithName(base.Pointer, name);
		}

		// Token: 0x06000BCC RID: 3020 RVA: 0x0000CEDC File Offset: 0x0000B0DC
		public void FillEntityWithHardBorderPhysicsBarrier(GameEntity entity)
		{
			EngineApplicationInterface.IScene.FillEntityWithHardBorderPhysicsBarrier(base.Pointer, entity.Pointer);
		}

		// Token: 0x06000BCD RID: 3021 RVA: 0x0000CEF4 File Offset: 0x0000B0F4
		public void ClearDecals()
		{
			EngineApplicationInterface.IScene.ClearDecals(base.Pointer);
		}

		// Token: 0x06000BCE RID: 3022 RVA: 0x0000CF06 File Offset: 0x0000B106
		public void SetPhotoAtmosphereViaTod(float tod, bool withStorm)
		{
			EngineApplicationInterface.IScene.SetPhotoAtmosphereViaTod(base.Pointer, tod, withStorm);
		}

		// Token: 0x06000BCF RID: 3023 RVA: 0x0000CF1A File Offset: 0x0000B11A
		public bool IsPositionOnADynamicNavMesh(Vec3 position)
		{
			return EngineApplicationInterface.IScene.IsPositionOnADynamicNavMesh(base.Pointer, position);
		}

		// Token: 0x06000BD0 RID: 3024 RVA: 0x0000CF2D File Offset: 0x0000B12D
		public void WaitWaterRendererCPUSimulation()
		{
			EngineApplicationInterface.IScene.WaitWaterRendererCPUSimulation(base.Pointer);
		}

		// Token: 0x06000BD1 RID: 3025 RVA: 0x0000CF3F File Offset: 0x0000B13F
		public void EnableInclusiveAsyncPhysx()
		{
			EngineApplicationInterface.IScene.EnableInclusiveAsyncPhysx(base.Pointer);
		}

		// Token: 0x06000BD2 RID: 3026 RVA: 0x0000CF51 File Offset: 0x0000B151
		public void EnsureWaterWakeRenderer()
		{
			EngineApplicationInterface.IScene.EnsureWaterWakeRenderer(base.Pointer);
		}

		// Token: 0x06000BD3 RID: 3027 RVA: 0x0000CF63 File Offset: 0x0000B163
		public void DeleteWaterWakeRenderer()
		{
			EngineApplicationInterface.IScene.DeleteWaterWakeRenderer(base.Pointer);
		}

		// Token: 0x06000BD4 RID: 3028 RVA: 0x0000CF75 File Offset: 0x0000B175
		public bool SceneHadWaterWakeRenderer()
		{
			return EngineApplicationInterface.IScene.SceneHadWaterWakeRenderer(base.Pointer);
		}

		// Token: 0x06000BD5 RID: 3029 RVA: 0x0000CF87 File Offset: 0x0000B187
		public void SetWaterWakeWorldSize(float worldSize, float eraseFactor)
		{
			EngineApplicationInterface.IScene.SetWaterWakeWorldSize(base.Pointer, worldSize, eraseFactor);
		}

		// Token: 0x06000BD6 RID: 3030 RVA: 0x0000CF9B File Offset: 0x0000B19B
		public void SetWaterWakeCameraOffset(float cameraOffset)
		{
			EngineApplicationInterface.IScene.SetWaterWakeCameraOffset(base.Pointer, cameraOffset);
		}

		// Token: 0x06000BD7 RID: 3031 RVA: 0x0000CFAE File Offset: 0x0000B1AE
		public void TickWake(float dt)
		{
			EngineApplicationInterface.IScene.TickWake(base.Pointer, dt);
		}

		// Token: 0x06000BD8 RID: 3032 RVA: 0x0000CFC1 File Offset: 0x0000B1C1
		public void SetDoNotAddEntitiesToTickList(bool value)
		{
			EngineApplicationInterface.IScene.SetDoNotAddEntitiesToTickList(base.Pointer, value);
		}

		// Token: 0x06000BD9 RID: 3033 RVA: 0x0000CFD4 File Offset: 0x0000B1D4
		public void SetDontLoadInvisibleEntities(bool value)
		{
			EngineApplicationInterface.IScene.SetDontLoadInvisibleEntities(base.Pointer, value);
		}

		// Token: 0x06000BDA RID: 3034 RVA: 0x0000CFE7 File Offset: 0x0000B1E7
		public void SetUsesDeleteLaterSystem(bool value)
		{
			EngineApplicationInterface.IScene.SetUsesDeleteLaterSystem(base.Pointer, value);
		}

		// Token: 0x06000BDB RID: 3035 RVA: 0x0000CFFA File Offset: 0x0000B1FA
		public void ClearCurrentFrameTickEntities()
		{
			EngineApplicationInterface.IScene.ClearCurrentFrameTickEntities(base.Pointer);
		}

		// Token: 0x06000BDC RID: 3036 RVA: 0x0000D00C File Offset: 0x0000B20C
		public Vec2 FindClosestExitPositionForPositionOnABoundaryFace(Vec3 position, UIntPtr boundaryFacePointer)
		{
			return EngineApplicationInterface.IScene.FindClosestExitPositionForPositionOnABoundaryFace(base.Pointer, position, boundaryFacePointer);
		}

		// Token: 0x040001A8 RID: 424
		public static float MaximumWindSpeed = 30f;

		// Token: 0x040001A9 RID: 425
		public const float AutoClimbHeight = 1.5f;

		// Token: 0x040001AA RID: 426
		public const float NavMeshHeightLimit = 1.5f;

		// Token: 0x040001AB RID: 427
		public const int SunRise = 2;

		// Token: 0x040001AC RID: 428
		public const int SunSet = 22;

		// Token: 0x040001AD RID: 429
		public static readonly TWSharedMutex PhysicsAndRayCastLock = new TWSharedMutex();
	}
}
