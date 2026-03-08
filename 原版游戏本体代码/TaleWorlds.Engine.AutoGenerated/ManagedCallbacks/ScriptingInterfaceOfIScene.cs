using System;
using System.Runtime.InteropServices;
using System.Security;
using System.Text;
using TaleWorlds.DotNet;
using TaleWorlds.Engine;
using TaleWorlds.Library;

namespace ManagedCallbacks
{
	// Token: 0x02000022 RID: 34
	internal class ScriptingInterfaceOfIScene : IScene
	{
		// Token: 0x060003FB RID: 1019 RVA: 0x000152E7 File Offset: 0x000134E7
		public void AddAlwaysRenderedSkeleton(UIntPtr scenePointer, UIntPtr skeletonPointer)
		{
			ScriptingInterfaceOfIScene.call_AddAlwaysRenderedSkeletonDelegate(scenePointer, skeletonPointer);
		}

		// Token: 0x060003FC RID: 1020 RVA: 0x000152F8 File Offset: 0x000134F8
		public void AddDecalInstance(UIntPtr scenePointer, UIntPtr decalMeshPointer, string decalSetID, bool deletable)
		{
			byte[] array = null;
			if (decalSetID != null)
			{
				int byteCount = ScriptingInterfaceOfIScene._utf8.GetByteCount(decalSetID);
				array = ((byteCount < 1024) ? CallbackStringBufferManager.StringBuffer0 : new byte[byteCount + 1]);
				ScriptingInterfaceOfIScene._utf8.GetBytes(decalSetID, 0, decalSetID.Length, array, 0);
				array[byteCount] = 0;
			}
			ScriptingInterfaceOfIScene.call_AddDecalInstanceDelegate(scenePointer, decalMeshPointer, array, deletable);
		}

		// Token: 0x060003FD RID: 1021 RVA: 0x00015356 File Offset: 0x00013556
		public int AddDirectionalLight(UIntPtr scenePointer, Vec3 position, Vec3 direction, float radius)
		{
			return ScriptingInterfaceOfIScene.call_AddDirectionalLightDelegate(scenePointer, position, direction, radius);
		}

		// Token: 0x060003FE RID: 1022 RVA: 0x00015367 File Offset: 0x00013567
		public void AddEntityWithMesh(UIntPtr scenePointer, UIntPtr meshPointer, ref MatrixFrame frame)
		{
			ScriptingInterfaceOfIScene.call_AddEntityWithMeshDelegate(scenePointer, meshPointer, ref frame);
		}

		// Token: 0x060003FF RID: 1023 RVA: 0x00015376 File Offset: 0x00013576
		public void AddEntityWithMultiMesh(UIntPtr scenePointer, UIntPtr multiMeshPointer, ref MatrixFrame frame)
		{
			ScriptingInterfaceOfIScene.call_AddEntityWithMultiMeshDelegate(scenePointer, multiMeshPointer, ref frame);
		}

		// Token: 0x06000400 RID: 1024 RVA: 0x00015388 File Offset: 0x00013588
		public GameEntity AddItemEntity(UIntPtr scenePointer, ref MatrixFrame frame, UIntPtr meshPointer)
		{
			NativeObjectPointer nativeObjectPointer = ScriptingInterfaceOfIScene.call_AddItemEntityDelegate(scenePointer, ref frame, meshPointer);
			GameEntity result = null;
			if (nativeObjectPointer.Pointer != UIntPtr.Zero)
			{
				result = new GameEntity(nativeObjectPointer.Pointer);
				LibraryApplicationInterface.IManaged.DecreaseReferenceCount(nativeObjectPointer.Pointer);
			}
			return result;
		}

		// Token: 0x06000401 RID: 1025 RVA: 0x000153D4 File Offset: 0x000135D4
		public void AddPath(UIntPtr scenePointer, string name)
		{
			byte[] array = null;
			if (name != null)
			{
				int byteCount = ScriptingInterfaceOfIScene._utf8.GetByteCount(name);
				array = ((byteCount < 1024) ? CallbackStringBufferManager.StringBuffer0 : new byte[byteCount + 1]);
				ScriptingInterfaceOfIScene._utf8.GetBytes(name, 0, name.Length, array, 0);
				array[byteCount] = 0;
			}
			ScriptingInterfaceOfIScene.call_AddPathDelegate(scenePointer, array);
		}

		// Token: 0x06000402 RID: 1026 RVA: 0x00015430 File Offset: 0x00013630
		public void AddPathPoint(UIntPtr scenePointer, string name, ref MatrixFrame frame)
		{
			byte[] array = null;
			if (name != null)
			{
				int byteCount = ScriptingInterfaceOfIScene._utf8.GetByteCount(name);
				array = ((byteCount < 1024) ? CallbackStringBufferManager.StringBuffer0 : new byte[byteCount + 1]);
				ScriptingInterfaceOfIScene._utf8.GetBytes(name, 0, name.Length, array, 0);
				array[byteCount] = 0;
			}
			ScriptingInterfaceOfIScene.call_AddPathPointDelegate(scenePointer, array, ref frame);
		}

		// Token: 0x06000403 RID: 1027 RVA: 0x0001548C File Offset: 0x0001368C
		public int AddPointLight(UIntPtr scenePointer, Vec3 position, float radius)
		{
			return ScriptingInterfaceOfIScene.call_AddPointLightDelegate(scenePointer, position, radius);
		}

		// Token: 0x06000404 RID: 1028 RVA: 0x0001549C File Offset: 0x0001369C
		public void AddWaterWakeWithCapsule(Scene scene, Vec3 positionA, float radiusA, Vec3 positionB, float radiusB, float wakeVisibility, float foamVisibility)
		{
			UIntPtr scene2 = ((scene != null) ? scene.Pointer : UIntPtr.Zero);
			ScriptingInterfaceOfIScene.call_AddWaterWakeWithCapsuleDelegate(scene2, positionA, radiusA, positionB, radiusB, wakeVisibility, foamVisibility);
		}

		// Token: 0x06000405 RID: 1029 RVA: 0x000154D5 File Offset: 0x000136D5
		public bool AttachEntity(UIntPtr scenePointer, UIntPtr entity, bool showWarnings)
		{
			return ScriptingInterfaceOfIScene.call_AttachEntityDelegate(scenePointer, entity, showWarnings);
		}

		// Token: 0x06000406 RID: 1030 RVA: 0x000154E4 File Offset: 0x000136E4
		public bool BoxCast(UIntPtr scenePointer, ref Vec3 boxPointBegin, ref Vec3 boxPointEnd, ref Vec3 dir, float distance, ref float collisionDistance, ref Vec3 closestPoint, ref UIntPtr entityIndex, BodyFlags bodyExcludeFlags)
		{
			return ScriptingInterfaceOfIScene.call_BoxCastDelegate(scenePointer, ref boxPointBegin, ref boxPointEnd, ref dir, distance, ref collisionDistance, ref closestPoint, ref entityIndex, bodyExcludeFlags);
		}

		// Token: 0x06000407 RID: 1031 RVA: 0x0001550C File Offset: 0x0001370C
		public bool BoxCastOnlyForCamera(UIntPtr scenePointer, Vec3[] boxPoints, in Vec3 centerPoint, in Vec3 dir, float distance, UIntPtr ignoredEntityPointer, ref float collisionDistance, ref Vec3 closestPoint, ref UIntPtr entityPointer, BodyFlags bodyExcludeFlags)
		{
			PinnedArrayData<Vec3> pinnedArrayData = new PinnedArrayData<Vec3>(boxPoints, false);
			IntPtr pointer = pinnedArrayData.Pointer;
			bool result = ScriptingInterfaceOfIScene.call_BoxCastOnlyForCameraDelegate(scenePointer, pointer, centerPoint, dir, distance, ignoredEntityPointer, ref collisionDistance, ref closestPoint, ref entityPointer, bodyExcludeFlags);
			pinnedArrayData.Dispose();
			return result;
		}

		// Token: 0x06000408 RID: 1032 RVA: 0x0001554C File Offset: 0x0001374C
		public bool CalculateEffectiveLighting(UIntPtr scenePointer)
		{
			return ScriptingInterfaceOfIScene.call_CalculateEffectiveLightingDelegate(scenePointer);
		}

		// Token: 0x06000409 RID: 1033 RVA: 0x0001555C File Offset: 0x0001375C
		public bool CheckPathEntitiesFrameChanged(UIntPtr scenePointer, string containsName)
		{
			byte[] array = null;
			if (containsName != null)
			{
				int byteCount = ScriptingInterfaceOfIScene._utf8.GetByteCount(containsName);
				array = ((byteCount < 1024) ? CallbackStringBufferManager.StringBuffer0 : new byte[byteCount + 1]);
				ScriptingInterfaceOfIScene._utf8.GetBytes(containsName, 0, containsName.Length, array, 0);
				array[byteCount] = 0;
			}
			return ScriptingInterfaceOfIScene.call_CheckPathEntitiesFrameChangedDelegate(scenePointer, array);
		}

		// Token: 0x0600040A RID: 1034 RVA: 0x000155B7 File Offset: 0x000137B7
		public bool CheckPointCanSeePoint(UIntPtr scenePointer, Vec3 sourcePoint, Vec3 targetPoint, float distanceToCheck)
		{
			return ScriptingInterfaceOfIScene.call_CheckPointCanSeePointDelegate(scenePointer, sourcePoint, targetPoint, distanceToCheck);
		}

		// Token: 0x0600040B RID: 1035 RVA: 0x000155C8 File Offset: 0x000137C8
		public void CheckResources(UIntPtr scenePointer, bool checkInvisibleEntities)
		{
			ScriptingInterfaceOfIScene.call_CheckResourcesDelegate(scenePointer, checkInvisibleEntities);
		}

		// Token: 0x0600040C RID: 1036 RVA: 0x000155D6 File Offset: 0x000137D6
		public void ClearAll(UIntPtr scenePointer)
		{
			ScriptingInterfaceOfIScene.call_ClearAllDelegate(scenePointer);
		}

		// Token: 0x0600040D RID: 1037 RVA: 0x000155E3 File Offset: 0x000137E3
		public void ClearCurrentFrameTickEntities(UIntPtr scenePointer)
		{
			ScriptingInterfaceOfIScene.call_ClearCurrentFrameTickEntitiesDelegate(scenePointer);
		}

		// Token: 0x0600040E RID: 1038 RVA: 0x000155F0 File Offset: 0x000137F0
		public void ClearDecals(UIntPtr scenePointer)
		{
			ScriptingInterfaceOfIScene.call_ClearDecalsDelegate(scenePointer);
		}

		// Token: 0x0600040F RID: 1039 RVA: 0x000155FD File Offset: 0x000137FD
		public void ClearNavMesh(UIntPtr scenePointer)
		{
			ScriptingInterfaceOfIScene.call_ClearNavMeshDelegate(scenePointer);
		}

		// Token: 0x06000410 RID: 1040 RVA: 0x0001560A File Offset: 0x0001380A
		public bool ContainsTerrain(UIntPtr scenePointer)
		{
			return ScriptingInterfaceOfIScene.call_ContainsTerrainDelegate(scenePointer);
		}

		// Token: 0x06000411 RID: 1041 RVA: 0x00015618 File Offset: 0x00013818
		public void CreateBurstParticle(Scene scene, int particleId, ref MatrixFrame frame)
		{
			UIntPtr scene2 = ((scene != null) ? scene.Pointer : UIntPtr.Zero);
			ScriptingInterfaceOfIScene.call_CreateBurstParticleDelegate(scene2, particleId, ref frame);
		}

		// Token: 0x06000412 RID: 1042 RVA: 0x00015649 File Offset: 0x00013849
		public void CreateDynamicRainTexture(UIntPtr scenePointer, int w, int h)
		{
			ScriptingInterfaceOfIScene.call_CreateDynamicRainTextureDelegate(scenePointer, w, h);
		}

		// Token: 0x06000413 RID: 1043 RVA: 0x00015658 File Offset: 0x00013858
		public Scene CreateNewScene(bool initializePhysics, bool enableDecals, int atlasGroup, string sceneName)
		{
			byte[] array = null;
			if (sceneName != null)
			{
				int byteCount = ScriptingInterfaceOfIScene._utf8.GetByteCount(sceneName);
				array = ((byteCount < 1024) ? CallbackStringBufferManager.StringBuffer0 : new byte[byteCount + 1]);
				ScriptingInterfaceOfIScene._utf8.GetBytes(sceneName, 0, sceneName.Length, array, 0);
				array[byteCount] = 0;
			}
			NativeObjectPointer nativeObjectPointer = ScriptingInterfaceOfIScene.call_CreateNewSceneDelegate(initializePhysics, enableDecals, atlasGroup, array);
			Scene result = null;
			if (nativeObjectPointer.Pointer != UIntPtr.Zero)
			{
				result = new Scene(nativeObjectPointer.Pointer);
				LibraryApplicationInterface.IManaged.DecreaseReferenceCount(nativeObjectPointer.Pointer);
			}
			return result;
		}

		// Token: 0x06000414 RID: 1044 RVA: 0x000156EC File Offset: 0x000138EC
		public MetaMesh CreatePathMesh(UIntPtr scenePointer, string baseEntityName, bool isWaterPath)
		{
			byte[] array = null;
			if (baseEntityName != null)
			{
				int byteCount = ScriptingInterfaceOfIScene._utf8.GetByteCount(baseEntityName);
				array = ((byteCount < 1024) ? CallbackStringBufferManager.StringBuffer0 : new byte[byteCount + 1]);
				ScriptingInterfaceOfIScene._utf8.GetBytes(baseEntityName, 0, baseEntityName.Length, array, 0);
				array[byteCount] = 0;
			}
			NativeObjectPointer nativeObjectPointer = ScriptingInterfaceOfIScene.call_CreatePathMeshDelegate(scenePointer, array, isWaterPath);
			MetaMesh result = null;
			if (nativeObjectPointer.Pointer != UIntPtr.Zero)
			{
				result = new MetaMesh(nativeObjectPointer.Pointer);
				LibraryApplicationInterface.IManaged.DecreaseReferenceCount(nativeObjectPointer.Pointer);
			}
			return result;
		}

		// Token: 0x06000415 RID: 1045 RVA: 0x0001577C File Offset: 0x0001397C
		public MetaMesh CreatePathMesh2(UIntPtr scenePointer, UIntPtr[] pathNodes, int pathNodeCount, bool isWaterPath)
		{
			PinnedArrayData<UIntPtr> pinnedArrayData = new PinnedArrayData<UIntPtr>(pathNodes, false);
			IntPtr pointer = pinnedArrayData.Pointer;
			NativeObjectPointer nativeObjectPointer = ScriptingInterfaceOfIScene.call_CreatePathMesh2Delegate(scenePointer, pointer, pathNodeCount, isWaterPath);
			pinnedArrayData.Dispose();
			MetaMesh result = null;
			if (nativeObjectPointer.Pointer != UIntPtr.Zero)
			{
				result = new MetaMesh(nativeObjectPointer.Pointer);
				LibraryApplicationInterface.IManaged.DecreaseReferenceCount(nativeObjectPointer.Pointer);
			}
			return result;
		}

		// Token: 0x06000416 RID: 1046 RVA: 0x000157E4 File Offset: 0x000139E4
		public void DeletePathWithName(UIntPtr scenePointer, string name)
		{
			byte[] array = null;
			if (name != null)
			{
				int byteCount = ScriptingInterfaceOfIScene._utf8.GetByteCount(name);
				array = ((byteCount < 1024) ? CallbackStringBufferManager.StringBuffer0 : new byte[byteCount + 1]);
				ScriptingInterfaceOfIScene._utf8.GetBytes(name, 0, name.Length, array, 0);
				array[byteCount] = 0;
			}
			ScriptingInterfaceOfIScene.call_DeletePathWithNameDelegate(scenePointer, array);
		}

		// Token: 0x06000417 RID: 1047 RVA: 0x0001583F File Offset: 0x00013A3F
		public void DeleteWaterWakeRenderer(UIntPtr scenePointer)
		{
			ScriptingInterfaceOfIScene.call_DeleteWaterWakeRendererDelegate(scenePointer);
		}

		// Token: 0x06000418 RID: 1048 RVA: 0x0001584C File Offset: 0x00013A4C
		public void DeRegisterShipVisual(UIntPtr scenePointer, UIntPtr visualPointer)
		{
			ScriptingInterfaceOfIScene.call_DeRegisterShipVisualDelegate(scenePointer, visualPointer);
		}

		// Token: 0x06000419 RID: 1049 RVA: 0x0001585A File Offset: 0x00013A5A
		public void DisableStaticShadows(UIntPtr ptr, bool value)
		{
			ScriptingInterfaceOfIScene.call_DisableStaticShadowsDelegate(ptr, value);
		}

		// Token: 0x0600041A RID: 1050 RVA: 0x00015868 File Offset: 0x00013A68
		public bool DoesPathExistBetweenFaces(UIntPtr scenePointer, int firstNavMeshFace, int secondNavMeshFace, bool ignoreDisabled)
		{
			return ScriptingInterfaceOfIScene.call_DoesPathExistBetweenFacesDelegate(scenePointer, firstNavMeshFace, secondNavMeshFace, ignoreDisabled);
		}

		// Token: 0x0600041B RID: 1051 RVA: 0x00015879 File Offset: 0x00013A79
		public bool DoesPathExistBetweenPositions(UIntPtr scenePointer, WorldPosition position, WorldPosition destination)
		{
			return ScriptingInterfaceOfIScene.call_DoesPathExistBetweenPositionsDelegate(scenePointer, position, destination);
		}

		// Token: 0x0600041C RID: 1052 RVA: 0x00015888 File Offset: 0x00013A88
		public void EnableFixedTick(Scene scene)
		{
			UIntPtr scene2 = ((scene != null) ? scene.Pointer : UIntPtr.Zero);
			ScriptingInterfaceOfIScene.call_EnableFixedTickDelegate(scene2);
		}

		// Token: 0x0600041D RID: 1053 RVA: 0x000158B7 File Offset: 0x00013AB7
		public void EnableInclusiveAsyncPhysx(UIntPtr scenePointer)
		{
			ScriptingInterfaceOfIScene.call_EnableInclusiveAsyncPhysxDelegate(scenePointer);
		}

		// Token: 0x0600041E RID: 1054 RVA: 0x000158C4 File Offset: 0x00013AC4
		public void EnsurePostfxSystem(UIntPtr scenePointer)
		{
			ScriptingInterfaceOfIScene.call_EnsurePostfxSystemDelegate(scenePointer);
		}

		// Token: 0x0600041F RID: 1055 RVA: 0x000158D1 File Offset: 0x00013AD1
		public void EnsureWaterWakeRenderer(UIntPtr scenePointer)
		{
			ScriptingInterfaceOfIScene.call_EnsureWaterWakeRendererDelegate(scenePointer);
		}

		// Token: 0x06000420 RID: 1056 RVA: 0x000158DE File Offset: 0x00013ADE
		public void FillEntityWithHardBorderPhysicsBarrier(UIntPtr scenePointer, UIntPtr entityPointer)
		{
			ScriptingInterfaceOfIScene.call_FillEntityWithHardBorderPhysicsBarrierDelegate(scenePointer, entityPointer);
		}

		// Token: 0x06000421 RID: 1057 RVA: 0x000158EC File Offset: 0x00013AEC
		public void FillTerrainHeightData(Scene scene, int xIndex, int yIndex, float[] heightArray)
		{
			UIntPtr scene2 = ((scene != null) ? scene.Pointer : UIntPtr.Zero);
			PinnedArrayData<float> pinnedArrayData = new PinnedArrayData<float>(heightArray, false);
			IntPtr pointer = pinnedArrayData.Pointer;
			ScriptingInterfaceOfIScene.call_FillTerrainHeightDataDelegate(scene2, xIndex, yIndex, pointer);
			pinnedArrayData.Dispose();
		}

		// Token: 0x06000422 RID: 1058 RVA: 0x00015938 File Offset: 0x00013B38
		public void FillTerrainPhysicsMaterialIndexData(Scene scene, int xIndex, int yIndex, short[] materialIndexArray)
		{
			UIntPtr scene2 = ((scene != null) ? scene.Pointer : UIntPtr.Zero);
			PinnedArrayData<short> pinnedArrayData = new PinnedArrayData<short>(materialIndexArray, false);
			IntPtr pointer = pinnedArrayData.Pointer;
			ScriptingInterfaceOfIScene.call_FillTerrainPhysicsMaterialIndexDataDelegate(scene2, xIndex, yIndex, pointer);
			pinnedArrayData.Dispose();
		}

		// Token: 0x06000423 RID: 1059 RVA: 0x00015983 File Offset: 0x00013B83
		public Vec2 FindClosestExitPositionForPositionOnABoundaryFace(UIntPtr scenePointer, Vec3 position, UIntPtr boundaryNavMeshFacePointer)
		{
			return ScriptingInterfaceOfIScene.call_FindClosestExitPositionForPositionOnABoundaryFaceDelegate(scenePointer, position, boundaryNavMeshFacePointer);
		}

		// Token: 0x06000424 RID: 1060 RVA: 0x00015992 File Offset: 0x00013B92
		public void FinishSceneSounds(UIntPtr scenePointer)
		{
			ScriptingInterfaceOfIScene.call_FinishSceneSoundsDelegate(scenePointer);
		}

		// Token: 0x06000425 RID: 1061 RVA: 0x000159A0 File Offset: 0x00013BA0
		public bool FocusRayCastForFixedPhysics(UIntPtr scenePointer, in Vec3 sourcePoint, in Vec3 targetPoint, float rayThickness, ref float collisionDistance, ref Vec3 closestPoint, ref UIntPtr entityIndex, BodyFlags bodyExcludeFlags, bool isFixedWorld)
		{
			return ScriptingInterfaceOfIScene.call_FocusRayCastForFixedPhysicsDelegate(scenePointer, sourcePoint, targetPoint, rayThickness, ref collisionDistance, ref closestPoint, ref entityIndex, bodyExcludeFlags, isFixedWorld);
		}

		// Token: 0x06000426 RID: 1062 RVA: 0x000159C6 File Offset: 0x00013BC6
		public void ForceLoadResources(UIntPtr scenePointer, bool checkInvisibleEntities)
		{
			ScriptingInterfaceOfIScene.call_ForceLoadResourcesDelegate(scenePointer, checkInvisibleEntities);
		}

		// Token: 0x06000427 RID: 1063 RVA: 0x000159D4 File Offset: 0x00013BD4
		public int GenerateContactsWithCapsule(UIntPtr scenePointer, ref CapsuleData cap, BodyFlags excludeFlags, bool isFixedTick, Intersection[] intersections, UIntPtr[] entityIds)
		{
			PinnedArrayData<Intersection> pinnedArrayData = new PinnedArrayData<Intersection>(intersections, false);
			IntPtr pointer = pinnedArrayData.Pointer;
			PinnedArrayData<UIntPtr> pinnedArrayData2 = new PinnedArrayData<UIntPtr>(entityIds, false);
			IntPtr pointer2 = pinnedArrayData2.Pointer;
			int result = ScriptingInterfaceOfIScene.call_GenerateContactsWithCapsuleDelegate(scenePointer, ref cap, excludeFlags, isFixedTick, pointer, pointer2);
			pinnedArrayData.Dispose();
			pinnedArrayData2.Dispose();
			return result;
		}

		// Token: 0x06000428 RID: 1064 RVA: 0x00015A24 File Offset: 0x00013C24
		public int GenerateContactsWithCapsuleAgainstEntity(UIntPtr scenePointer, ref CapsuleData cap, BodyFlags excludeFlags, UIntPtr entityId, Intersection[] intersections)
		{
			PinnedArrayData<Intersection> pinnedArrayData = new PinnedArrayData<Intersection>(intersections, false);
			IntPtr pointer = pinnedArrayData.Pointer;
			int result = ScriptingInterfaceOfIScene.call_GenerateContactsWithCapsuleAgainstEntityDelegate(scenePointer, ref cap, excludeFlags, entityId, pointer);
			pinnedArrayData.Dispose();
			return result;
		}

		// Token: 0x06000429 RID: 1065 RVA: 0x00015A5C File Offset: 0x00013C5C
		public string GetAllColorGradeNames(Scene scene)
		{
			UIntPtr scene2 = ((scene != null) ? scene.Pointer : UIntPtr.Zero);
			if (ScriptingInterfaceOfIScene.call_GetAllColorGradeNamesDelegate(scene2) != 1)
			{
				return null;
			}
			return Managed.ReturnValueFromEngine;
		}

		// Token: 0x0600042A RID: 1066 RVA: 0x00015A98 File Offset: 0x00013C98
		public void GetAllEntitiesWithScriptComponent(UIntPtr scenePointer, string scriptComponentName, UIntPtr output)
		{
			byte[] array = null;
			if (scriptComponentName != null)
			{
				int byteCount = ScriptingInterfaceOfIScene._utf8.GetByteCount(scriptComponentName);
				array = ((byteCount < 1024) ? CallbackStringBufferManager.StringBuffer0 : new byte[byteCount + 1]);
				ScriptingInterfaceOfIScene._utf8.GetBytes(scriptComponentName, 0, scriptComponentName.Length, array, 0);
				array[byteCount] = 0;
			}
			ScriptingInterfaceOfIScene.call_GetAllEntitiesWithScriptComponentDelegate(scenePointer, array, output);
		}

		// Token: 0x0600042B RID: 1067 RVA: 0x00015AF4 File Offset: 0x00013CF4
		public string GetAllFilterNames(Scene scene)
		{
			UIntPtr scene2 = ((scene != null) ? scene.Pointer : UIntPtr.Zero);
			if (ScriptingInterfaceOfIScene.call_GetAllFilterNamesDelegate(scene2) != 1)
			{
				return null;
			}
			return Managed.ReturnValueFromEngine;
		}

		// Token: 0x0600042C RID: 1068 RVA: 0x00015B30 File Offset: 0x00013D30
		public void GetAllNavmeshFaceRecords(UIntPtr scenePointer, PathFaceRecord[] faceRecords)
		{
			PinnedArrayData<PathFaceRecord> pinnedArrayData = new PinnedArrayData<PathFaceRecord>(faceRecords, false);
			IntPtr pointer = pinnedArrayData.Pointer;
			ScriptingInterfaceOfIScene.call_GetAllNavmeshFaceRecordsDelegate(scenePointer, pointer);
			pinnedArrayData.Dispose();
		}

		// Token: 0x0600042D RID: 1069 RVA: 0x00015B61 File Offset: 0x00013D61
		public void GetBoundingBox(UIntPtr scenePointer, ref Vec3 min, ref Vec3 max)
		{
			ScriptingInterfaceOfIScene.call_GetBoundingBoxDelegate(scenePointer, ref min, ref max);
		}

		// Token: 0x0600042E RID: 1070 RVA: 0x00015B70 File Offset: 0x00013D70
		public void GetBulkWaterLevelAtPositions(Scene scene, Vec2[] waterHeightQueryArray, int arraySize, float[] waterHeightsAtVolumes, Vec3[] waterSurfaceNormals)
		{
			UIntPtr scene2 = ((scene != null) ? scene.Pointer : UIntPtr.Zero);
			PinnedArrayData<Vec2> pinnedArrayData = new PinnedArrayData<Vec2>(waterHeightQueryArray, false);
			IntPtr pointer = pinnedArrayData.Pointer;
			PinnedArrayData<float> pinnedArrayData2 = new PinnedArrayData<float>(waterHeightsAtVolumes, false);
			IntPtr pointer2 = pinnedArrayData2.Pointer;
			PinnedArrayData<Vec3> pinnedArrayData3 = new PinnedArrayData<Vec3>(waterSurfaceNormals, false);
			IntPtr pointer3 = pinnedArrayData3.Pointer;
			ScriptingInterfaceOfIScene.call_GetBulkWaterLevelAtPositionsDelegate(scene2, pointer, arraySize, pointer2, pointer3);
			pinnedArrayData.Dispose();
			pinnedArrayData2.Dispose();
			pinnedArrayData3.Dispose();
		}

		// Token: 0x0600042F RID: 1071 RVA: 0x00015BF1 File Offset: 0x00013DF1
		public void GetBulkWaterLevelAtVolumes(UIntPtr scene, UIntPtr volumeQueryDataArray, int volumeCount, in MatrixFrame entityFrame)
		{
			ScriptingInterfaceOfIScene.call_GetBulkWaterLevelAtVolumesDelegate(scene, volumeQueryDataArray, volumeCount, entityFrame);
		}

		// Token: 0x06000430 RID: 1072 RVA: 0x00015C04 File Offset: 0x00013E04
		public GameEntity GetCampaignEntityWithName(UIntPtr scenePointer, string entityName)
		{
			byte[] array = null;
			if (entityName != null)
			{
				int byteCount = ScriptingInterfaceOfIScene._utf8.GetByteCount(entityName);
				array = ((byteCount < 1024) ? CallbackStringBufferManager.StringBuffer0 : new byte[byteCount + 1]);
				ScriptingInterfaceOfIScene._utf8.GetBytes(entityName, 0, entityName.Length, array, 0);
				array[byteCount] = 0;
			}
			NativeObjectPointer nativeObjectPointer = ScriptingInterfaceOfIScene.call_GetCampaignEntityWithNameDelegate(scenePointer, array);
			GameEntity result = null;
			if (nativeObjectPointer.Pointer != UIntPtr.Zero)
			{
				result = new GameEntity(nativeObjectPointer.Pointer);
				LibraryApplicationInterface.IManaged.DecreaseReferenceCount(nativeObjectPointer.Pointer);
			}
			return result;
		}

		// Token: 0x06000431 RID: 1073 RVA: 0x00015C91 File Offset: 0x00013E91
		public bool GetEnginePhysicsEnabled(UIntPtr scenePointer)
		{
			return ScriptingInterfaceOfIScene.call_GetEnginePhysicsEnabledDelegate(scenePointer);
		}

		// Token: 0x06000432 RID: 1074 RVA: 0x00015C9E File Offset: 0x00013E9E
		public void GetEntities(UIntPtr scenePointer, UIntPtr entityObjectsArrayPointer)
		{
			ScriptingInterfaceOfIScene.call_GetEntitiesDelegate(scenePointer, entityObjectsArrayPointer);
		}

		// Token: 0x06000433 RID: 1075 RVA: 0x00015CAC File Offset: 0x00013EAC
		public int GetEntityCount(UIntPtr scenePointer)
		{
			return ScriptingInterfaceOfIScene.call_GetEntityCountDelegate(scenePointer);
		}

		// Token: 0x06000434 RID: 1076 RVA: 0x00015CBC File Offset: 0x00013EBC
		public GameEntity GetEntityWithGuid(UIntPtr scenePointer, string guid)
		{
			byte[] array = null;
			if (guid != null)
			{
				int byteCount = ScriptingInterfaceOfIScene._utf8.GetByteCount(guid);
				array = ((byteCount < 1024) ? CallbackStringBufferManager.StringBuffer0 : new byte[byteCount + 1]);
				ScriptingInterfaceOfIScene._utf8.GetBytes(guid, 0, guid.Length, array, 0);
				array[byteCount] = 0;
			}
			NativeObjectPointer nativeObjectPointer = ScriptingInterfaceOfIScene.call_GetEntityWithGuidDelegate(scenePointer, array);
			GameEntity result = null;
			if (nativeObjectPointer.Pointer != UIntPtr.Zero)
			{
				result = new GameEntity(nativeObjectPointer.Pointer);
				LibraryApplicationInterface.IManaged.DecreaseReferenceCount(nativeObjectPointer.Pointer);
			}
			return result;
		}

		// Token: 0x06000435 RID: 1077 RVA: 0x00015D49 File Offset: 0x00013F49
		public float GetFallDensity(UIntPtr scenepTR)
		{
			return ScriptingInterfaceOfIScene.call_GetFallDensityDelegate(scenepTR);
		}

		// Token: 0x06000436 RID: 1078 RVA: 0x00015D58 File Offset: 0x00013F58
		public GameEntity GetFirstEntityWithName(UIntPtr scenePointer, string entityName)
		{
			byte[] array = null;
			if (entityName != null)
			{
				int byteCount = ScriptingInterfaceOfIScene._utf8.GetByteCount(entityName);
				array = ((byteCount < 1024) ? CallbackStringBufferManager.StringBuffer0 : new byte[byteCount + 1]);
				ScriptingInterfaceOfIScene._utf8.GetBytes(entityName, 0, entityName.Length, array, 0);
				array[byteCount] = 0;
			}
			NativeObjectPointer nativeObjectPointer = ScriptingInterfaceOfIScene.call_GetFirstEntityWithNameDelegate(scenePointer, array);
			GameEntity result = null;
			if (nativeObjectPointer.Pointer != UIntPtr.Zero)
			{
				result = new GameEntity(nativeObjectPointer.Pointer);
				LibraryApplicationInterface.IManaged.DecreaseReferenceCount(nativeObjectPointer.Pointer);
			}
			return result;
		}

		// Token: 0x06000437 RID: 1079 RVA: 0x00015DE8 File Offset: 0x00013FE8
		public GameEntity GetFirstEntityWithScriptComponent(UIntPtr scenePointer, string scriptComponentName)
		{
			byte[] array = null;
			if (scriptComponentName != null)
			{
				int byteCount = ScriptingInterfaceOfIScene._utf8.GetByteCount(scriptComponentName);
				array = ((byteCount < 1024) ? CallbackStringBufferManager.StringBuffer0 : new byte[byteCount + 1]);
				ScriptingInterfaceOfIScene._utf8.GetBytes(scriptComponentName, 0, scriptComponentName.Length, array, 0);
				array[byteCount] = 0;
			}
			NativeObjectPointer nativeObjectPointer = ScriptingInterfaceOfIScene.call_GetFirstEntityWithScriptComponentDelegate(scenePointer, array);
			GameEntity result = null;
			if (nativeObjectPointer.Pointer != UIntPtr.Zero)
			{
				result = new GameEntity(nativeObjectPointer.Pointer);
				LibraryApplicationInterface.IManaged.DecreaseReferenceCount(nativeObjectPointer.Pointer);
			}
			return result;
		}

		// Token: 0x06000438 RID: 1080 RVA: 0x00015E75 File Offset: 0x00014075
		public int GetFloraInstanceCount(UIntPtr scenePointer)
		{
			return ScriptingInterfaceOfIScene.call_GetFloraInstanceCountDelegate(scenePointer);
		}

		// Token: 0x06000439 RID: 1081 RVA: 0x00015E82 File Offset: 0x00014082
		public int GetFloraRendererTextureUsage(UIntPtr scenePointer)
		{
			return ScriptingInterfaceOfIScene.call_GetFloraRendererTextureUsageDelegate(scenePointer);
		}

		// Token: 0x0600043A RID: 1082 RVA: 0x00015E8F File Offset: 0x0001408F
		public float GetFog(UIntPtr scenePointer)
		{
			return ScriptingInterfaceOfIScene.call_GetFogDelegate(scenePointer);
		}

		// Token: 0x0600043B RID: 1083 RVA: 0x00015E9C File Offset: 0x0001409C
		public Vec2 GetGlobalWindStrengthVector(UIntPtr scenePointer)
		{
			return ScriptingInterfaceOfIScene.call_GetGlobalWindStrengthVectorDelegate(scenePointer);
		}

		// Token: 0x0600043C RID: 1084 RVA: 0x00015EA9 File Offset: 0x000140A9
		public Vec2 GetGlobalWindVelocity(UIntPtr scenePointer)
		{
			return ScriptingInterfaceOfIScene.call_GetGlobalWindVelocityDelegate(scenePointer);
		}

		// Token: 0x0600043D RID: 1085 RVA: 0x00015EB6 File Offset: 0x000140B6
		public float GetGroundHeightAndBodyFlagsAtPosition(UIntPtr scenePointer, Vec3 position, out BodyFlags contactPointFlags, BodyFlags excludeFlags)
		{
			return ScriptingInterfaceOfIScene.call_GetGroundHeightAndBodyFlagsAtPositionDelegate(scenePointer, position, out contactPointFlags, excludeFlags);
		}

		// Token: 0x0600043E RID: 1086 RVA: 0x00015EC7 File Offset: 0x000140C7
		public float GetGroundHeightAndNormalAtPosition(UIntPtr scenePointer, Vec3 position, ref Vec3 normal, uint excludeFlags)
		{
			return ScriptingInterfaceOfIScene.call_GetGroundHeightAndNormalAtPositionDelegate(scenePointer, position, ref normal, excludeFlags);
		}

		// Token: 0x0600043F RID: 1087 RVA: 0x00015ED8 File Offset: 0x000140D8
		public float GetGroundHeightAtPosition(UIntPtr scenePointer, Vec3 position, uint excludeFlags)
		{
			return ScriptingInterfaceOfIScene.call_GetGroundHeightAtPositionDelegate(scenePointer, position, excludeFlags);
		}

		// Token: 0x06000440 RID: 1088 RVA: 0x00015EE7 File Offset: 0x000140E7
		public Vec2 GetHardBoundaryVertex(UIntPtr scenePointer, int index)
		{
			return ScriptingInterfaceOfIScene.call_GetHardBoundaryVertexDelegate(scenePointer, index);
		}

		// Token: 0x06000441 RID: 1089 RVA: 0x00015EF5 File Offset: 0x000140F5
		public int GetHardBoundaryVertexCount(UIntPtr scenePointer)
		{
			return ScriptingInterfaceOfIScene.call_GetHardBoundaryVertexCountDelegate(scenePointer);
		}

		// Token: 0x06000442 RID: 1090 RVA: 0x00015F02 File Offset: 0x00014102
		public bool GetHeightAtPoint(UIntPtr scenePointer, Vec2 point, BodyFlags excludeBodyFlags, ref float height)
		{
			return ScriptingInterfaceOfIScene.call_GetHeightAtPointDelegate(scenePointer, point, excludeBodyFlags, ref height);
		}

		// Token: 0x06000443 RID: 1091 RVA: 0x00015F13 File Offset: 0x00014113
		public int GetIdOfNavMeshFace(UIntPtr scenePointer, int navMeshFace)
		{
			return ScriptingInterfaceOfIScene.call_GetIdOfNavMeshFaceDelegate(scenePointer, navMeshFace);
		}

		// Token: 0x06000444 RID: 1092 RVA: 0x00015F24 File Offset: 0x00014124
		public void GetInterpolationFactorForBodyWorldTransformSmoothing(Scene scene, out float interpolationFactor, out float fixedDt)
		{
			UIntPtr scene2 = ((scene != null) ? scene.Pointer : UIntPtr.Zero);
			ScriptingInterfaceOfIScene.call_GetInterpolationFactorForBodyWorldTransformSmoothingDelegate(scene2, out interpolationFactor, out fixedDt);
		}

		// Token: 0x06000445 RID: 1093 RVA: 0x00015F55 File Offset: 0x00014155
		public void GetLastFinalRenderCameraFrame(UIntPtr scenePointer, ref MatrixFrame outFrame)
		{
			ScriptingInterfaceOfIScene.call_GetLastFinalRenderCameraFrameDelegate(scenePointer, ref outFrame);
		}

		// Token: 0x06000446 RID: 1094 RVA: 0x00015F63 File Offset: 0x00014163
		public Vec3 GetLastFinalRenderCameraPosition(UIntPtr scenePointer)
		{
			return ScriptingInterfaceOfIScene.call_GetLastFinalRenderCameraPositionDelegate(scenePointer);
		}

		// Token: 0x06000447 RID: 1095 RVA: 0x00015F70 File Offset: 0x00014170
		public Vec2 GetLastPointOnNavigationMeshFromPositionToDestination(UIntPtr scenePointer, int startingFace, Vec2 position, Vec2 destination, int[] exclusionGroupIds, int exclusionGroupIdsCount)
		{
			PinnedArrayData<int> pinnedArrayData = new PinnedArrayData<int>(exclusionGroupIds, false);
			IntPtr pointer = pinnedArrayData.Pointer;
			Vec2 result = ScriptingInterfaceOfIScene.call_GetLastPointOnNavigationMeshFromPositionToDestinationDelegate(scenePointer, startingFace, position, destination, pointer, exclusionGroupIdsCount);
			pinnedArrayData.Dispose();
			return result;
		}

		// Token: 0x06000448 RID: 1096 RVA: 0x00015FA8 File Offset: 0x000141A8
		public Vec3 GetLastPointOnNavigationMeshFromWorldPositionToDestination(UIntPtr scenePointer, ref WorldPosition position, Vec2 destination)
		{
			return ScriptingInterfaceOfIScene.call_GetLastPointOnNavigationMeshFromWorldPositionToDestinationDelegate(scenePointer, ref position, destination);
		}

		// Token: 0x06000449 RID: 1097 RVA: 0x00015FB7 File Offset: 0x000141B7
		public Vec2 GetLastPositionOnNavMeshFaceForPointAndDirection(UIntPtr scenePointer, in PathFaceRecord record, Vec2 position, Vec2 direction)
		{
			return ScriptingInterfaceOfIScene.call_GetLastPositionOnNavMeshFaceForPointAndDirectionDelegate(scenePointer, record, position, direction);
		}

		// Token: 0x0600044A RID: 1098 RVA: 0x00015FC8 File Offset: 0x000141C8
		public string GetLoadingStateName(Scene scene)
		{
			UIntPtr scene2 = ((scene != null) ? scene.Pointer : UIntPtr.Zero);
			if (ScriptingInterfaceOfIScene.call_GetLoadingStateNameDelegate(scene2) != 1)
			{
				return null;
			}
			return Managed.ReturnValueFromEngine;
		}

		// Token: 0x0600044B RID: 1099 RVA: 0x00016001 File Offset: 0x00014201
		public string GetModulePath(UIntPtr scenePointer)
		{
			if (ScriptingInterfaceOfIScene.call_GetModulePathDelegate(scenePointer) != 1)
			{
				return null;
			}
			return Managed.ReturnValueFromEngine;
		}

		// Token: 0x0600044C RID: 1100 RVA: 0x00016018 File Offset: 0x00014218
		public string GetName(UIntPtr scenePointer)
		{
			if (ScriptingInterfaceOfIScene.call_GetNameDelegate(scenePointer) != 1)
			{
				return null;
			}
			return Managed.ReturnValueFromEngine;
		}

		// Token: 0x0600044D RID: 1101 RVA: 0x0001602F File Offset: 0x0001422F
		public uint GetNavigationMeshCRC(UIntPtr scenePointer)
		{
			return ScriptingInterfaceOfIScene.call_GetNavigationMeshCRCDelegate(scenePointer);
		}

		// Token: 0x0600044E RID: 1102 RVA: 0x0001603C File Offset: 0x0001423C
		public UIntPtr GetNavigationMeshForPosition(UIntPtr scenePointer, in Vec3 position, ref int faceGroupId, float heightDifferenceLimit, bool excludeDynamicNavigationMeshes)
		{
			return ScriptingInterfaceOfIScene.call_GetNavigationMeshForPositionDelegate(scenePointer, position, ref faceGroupId, heightDifferenceLimit, excludeDynamicNavigationMeshes);
		}

		// Token: 0x0600044F RID: 1103 RVA: 0x0001604F File Offset: 0x0001424F
		public void GetNavMeshFaceCenterPosition(UIntPtr scenePointer, int navMeshFace, ref Vec3 centerPos)
		{
			ScriptingInterfaceOfIScene.call_GetNavMeshFaceCenterPositionDelegate(scenePointer, navMeshFace, ref centerPos);
		}

		// Token: 0x06000450 RID: 1104 RVA: 0x0001605E File Offset: 0x0001425E
		public int GetNavMeshFaceCount(UIntPtr scenePointer)
		{
			return ScriptingInterfaceOfIScene.call_GetNavMeshFaceCountDelegate(scenePointer);
		}

		// Token: 0x06000451 RID: 1105 RVA: 0x0001606B File Offset: 0x0001426B
		public int GetNavmeshFaceCountBetweenTwoIds(UIntPtr scenePointer, int firstId, int secondId)
		{
			return ScriptingInterfaceOfIScene.call_GetNavmeshFaceCountBetweenTwoIdsDelegate(scenePointer, firstId, secondId);
		}

		// Token: 0x06000452 RID: 1106 RVA: 0x0001607A File Offset: 0x0001427A
		public float GetNavMeshFaceFirstVertexZ(UIntPtr scenePointer, int navMeshFaceIndex)
		{
			return ScriptingInterfaceOfIScene.call_GetNavMeshFaceFirstVertexZDelegate(scenePointer, navMeshFaceIndex);
		}

		// Token: 0x06000453 RID: 1107 RVA: 0x00016088 File Offset: 0x00014288
		public void GetNavMeshFaceIndex(UIntPtr scenePointer, ref PathFaceRecord record, Vec2 position, bool isRegion1, bool checkIfDisabled, bool ignoreHeight)
		{
			ScriptingInterfaceOfIScene.call_GetNavMeshFaceIndexDelegate(scenePointer, ref record, position, isRegion1, checkIfDisabled, ignoreHeight);
		}

		// Token: 0x06000454 RID: 1108 RVA: 0x0001609D File Offset: 0x0001429D
		public void GetNavMeshFaceIndex3(UIntPtr scenePointer, ref PathFaceRecord record, Vec3 position, bool checkIfDisabled)
		{
			ScriptingInterfaceOfIScene.call_GetNavMeshFaceIndex3Delegate(scenePointer, ref record, position, checkIfDisabled);
		}

		// Token: 0x06000455 RID: 1109 RVA: 0x000160B0 File Offset: 0x000142B0
		public void GetNavmeshFaceRecordsBetweenTwoIds(UIntPtr scenePointer, int firstId, int secondId, PathFaceRecord[] faceRecords)
		{
			PinnedArrayData<PathFaceRecord> pinnedArrayData = new PinnedArrayData<PathFaceRecord>(faceRecords, false);
			IntPtr pointer = pinnedArrayData.Pointer;
			ScriptingInterfaceOfIScene.call_GetNavmeshFaceRecordsBetweenTwoIdsDelegate(scenePointer, firstId, secondId, pointer);
			pinnedArrayData.Dispose();
		}

		// Token: 0x06000456 RID: 1110 RVA: 0x000160E4 File Offset: 0x000142E4
		public PathFaceRecord GetNavMeshPathFaceRecord(UIntPtr scenePointer, int navMeshFace)
		{
			return ScriptingInterfaceOfIScene.call_GetNavMeshPathFaceRecordDelegate(scenePointer, navMeshFace);
		}

		// Token: 0x06000457 RID: 1111 RVA: 0x000160F2 File Offset: 0x000142F2
		public UIntPtr GetNearestNavigationMeshForPosition(UIntPtr scenePointer, in Vec3 position, float heightDifferenceLimit, bool excludeDynamicNavigationMeshes)
		{
			return ScriptingInterfaceOfIScene.call_GetNearestNavigationMeshForPositionDelegate(scenePointer, position, heightDifferenceLimit, excludeDynamicNavigationMeshes);
		}

		// Token: 0x06000458 RID: 1112 RVA: 0x00016104 File Offset: 0x00014304
		public int GetNodeDataCount(Scene scene, int xIndex, int yIndex)
		{
			UIntPtr scene2 = ((scene != null) ? scene.Pointer : UIntPtr.Zero);
			return ScriptingInterfaceOfIScene.call_GetNodeDataCountDelegate(scene2, xIndex, yIndex);
		}

		// Token: 0x06000459 RID: 1113 RVA: 0x00016135 File Offset: 0x00014335
		public Vec3 GetNormalAt(UIntPtr scenePointer, Vec2 position)
		{
			return ScriptingInterfaceOfIScene.call_GetNormalAtDelegate(scenePointer, position);
		}

		// Token: 0x0600045A RID: 1114 RVA: 0x00016143 File Offset: 0x00014343
		public float GetNorthAngle(UIntPtr scenePointer)
		{
			return ScriptingInterfaceOfIScene.call_GetNorthAngleDelegate(scenePointer);
		}

		// Token: 0x0600045B RID: 1115 RVA: 0x00016150 File Offset: 0x00014350
		public int GetNumberOfPathsWithNamePrefix(UIntPtr ptr, string prefix)
		{
			byte[] array = null;
			if (prefix != null)
			{
				int byteCount = ScriptingInterfaceOfIScene._utf8.GetByteCount(prefix);
				array = ((byteCount < 1024) ? CallbackStringBufferManager.StringBuffer0 : new byte[byteCount + 1]);
				ScriptingInterfaceOfIScene._utf8.GetBytes(prefix, 0, prefix.Length, array, 0);
				array[byteCount] = 0;
			}
			return ScriptingInterfaceOfIScene.call_GetNumberOfPathsWithNamePrefixDelegate(ptr, array);
		}

		// Token: 0x0600045C RID: 1116 RVA: 0x000161AC File Offset: 0x000143AC
		public bool GetPathBetweenAIFaceIndices(UIntPtr scenePointer, int startingAiFace, int endingAiFace, Vec2 startingPosition, Vec2 endingPosition, float agentRadius, Vec2[] result, ref int pathSize, int[] exclusionGroupIds, int exclusionGroupIdsCount, float extraCostMultiplier)
		{
			PinnedArrayData<Vec2> pinnedArrayData = new PinnedArrayData<Vec2>(result, false);
			IntPtr pointer = pinnedArrayData.Pointer;
			PinnedArrayData<int> pinnedArrayData2 = new PinnedArrayData<int>(exclusionGroupIds, false);
			IntPtr pointer2 = pinnedArrayData2.Pointer;
			bool result2 = ScriptingInterfaceOfIScene.call_GetPathBetweenAIFaceIndicesDelegate(scenePointer, startingAiFace, endingAiFace, startingPosition, endingPosition, agentRadius, pointer, ref pathSize, pointer2, exclusionGroupIdsCount, extraCostMultiplier);
			pinnedArrayData.Dispose();
			pinnedArrayData2.Dispose();
			return result2;
		}

		// Token: 0x0600045D RID: 1117 RVA: 0x00016208 File Offset: 0x00014408
		public bool GetPathBetweenAIFaceIndicesWithRegionSwitchCost(UIntPtr scenePointer, int startingAiFace, int endingAiFace, Vec2 startingPosition, Vec2 endingPosition, float agentRadius, Vec2[] result, ref int pathSize, int[] exclusionGroupIds, int exclusionGroupIdsCount, float extraCostMultiplier, int regionSwitchCostTo0, int regionSwitchCostTo1)
		{
			PinnedArrayData<Vec2> pinnedArrayData = new PinnedArrayData<Vec2>(result, false);
			IntPtr pointer = pinnedArrayData.Pointer;
			PinnedArrayData<int> pinnedArrayData2 = new PinnedArrayData<int>(exclusionGroupIds, false);
			IntPtr pointer2 = pinnedArrayData2.Pointer;
			bool result2 = ScriptingInterfaceOfIScene.call_GetPathBetweenAIFaceIndicesWithRegionSwitchCostDelegate(scenePointer, startingAiFace, endingAiFace, startingPosition, endingPosition, agentRadius, pointer, ref pathSize, pointer2, exclusionGroupIdsCount, extraCostMultiplier, regionSwitchCostTo0, regionSwitchCostTo1);
			pinnedArrayData.Dispose();
			pinnedArrayData2.Dispose();
			return result2;
		}

		// Token: 0x0600045E RID: 1118 RVA: 0x00016268 File Offset: 0x00014468
		public bool GetPathBetweenAIFacePointers(UIntPtr scenePointer, UIntPtr startingAiFace, UIntPtr endingAiFace, Vec2 startingPosition, Vec2 endingPosition, float agentRadius, Vec2[] result, ref int pathSize, int[] exclusionGroupIds, int exclusionGroupIdsCount)
		{
			PinnedArrayData<Vec2> pinnedArrayData = new PinnedArrayData<Vec2>(result, false);
			IntPtr pointer = pinnedArrayData.Pointer;
			PinnedArrayData<int> pinnedArrayData2 = new PinnedArrayData<int>(exclusionGroupIds, false);
			IntPtr pointer2 = pinnedArrayData2.Pointer;
			bool result2 = ScriptingInterfaceOfIScene.call_GetPathBetweenAIFacePointersDelegate(scenePointer, startingAiFace, endingAiFace, startingPosition, endingPosition, agentRadius, pointer, ref pathSize, pointer2, exclusionGroupIdsCount);
			pinnedArrayData.Dispose();
			pinnedArrayData2.Dispose();
			return result2;
		}

		// Token: 0x0600045F RID: 1119 RVA: 0x000162C0 File Offset: 0x000144C0
		public bool GetPathBetweenAIFacePointersWithRegionSwitchCost(UIntPtr scenePointer, UIntPtr startingAiFace, UIntPtr endingAiFace, Vec2 startingPosition, Vec2 endingPosition, float agentRadius, Vec2[] result, ref int pathSize, int[] exclusionGroupIds, int exclusionGroupIdsCount, int regionSwitchCostTo0, int regionSwitchCostTo1)
		{
			PinnedArrayData<Vec2> pinnedArrayData = new PinnedArrayData<Vec2>(result, false);
			IntPtr pointer = pinnedArrayData.Pointer;
			PinnedArrayData<int> pinnedArrayData2 = new PinnedArrayData<int>(exclusionGroupIds, false);
			IntPtr pointer2 = pinnedArrayData2.Pointer;
			bool result2 = ScriptingInterfaceOfIScene.call_GetPathBetweenAIFacePointersWithRegionSwitchCostDelegate(scenePointer, startingAiFace, endingAiFace, startingPosition, endingPosition, agentRadius, pointer, ref pathSize, pointer2, exclusionGroupIdsCount, regionSwitchCostTo0, regionSwitchCostTo1);
			pinnedArrayData.Dispose();
			pinnedArrayData2.Dispose();
			return result2;
		}

		// Token: 0x06000460 RID: 1120 RVA: 0x0001631C File Offset: 0x0001451C
		public bool GetPathDistanceBetweenAIFaces(UIntPtr scenePointer, int startingAiFace, int endingAiFace, Vec2 startingPosition, Vec2 endingPosition, float agentRadius, float distanceLimit, out float distance, int[] exclusionGroupIds, int exclusionGroupIdsCount, int regionSwitchCostTo0, int regionSwitchCostTo1)
		{
			PinnedArrayData<int> pinnedArrayData = new PinnedArrayData<int>(exclusionGroupIds, false);
			IntPtr pointer = pinnedArrayData.Pointer;
			bool result = ScriptingInterfaceOfIScene.call_GetPathDistanceBetweenAIFacesDelegate(scenePointer, startingAiFace, endingAiFace, startingPosition, endingPosition, agentRadius, distanceLimit, out distance, pointer, exclusionGroupIdsCount, regionSwitchCostTo0, regionSwitchCostTo1);
			pinnedArrayData.Dispose();
			return result;
		}

		// Token: 0x06000461 RID: 1121 RVA: 0x00016360 File Offset: 0x00014560
		public bool GetPathDistanceBetweenPositions(UIntPtr scenePointer, ref WorldPosition position, ref WorldPosition destination, float agentRadius, ref float pathLength)
		{
			return ScriptingInterfaceOfIScene.call_GetPathDistanceBetweenPositionsDelegate(scenePointer, ref position, ref destination, agentRadius, ref pathLength);
		}

		// Token: 0x06000462 RID: 1122 RVA: 0x00016373 File Offset: 0x00014573
		public PathFaceRecord GetPathFaceRecordFromNavMeshFacePointer(UIntPtr scenePointer, UIntPtr navMeshFacePointer)
		{
			return ScriptingInterfaceOfIScene.call_GetPathFaceRecordFromNavMeshFacePointerDelegate(scenePointer, navMeshFacePointer);
		}

		// Token: 0x06000463 RID: 1123 RVA: 0x00016384 File Offset: 0x00014584
		public void GetPathsWithNamePrefix(UIntPtr ptr, UIntPtr[] points, string prefix)
		{
			PinnedArrayData<UIntPtr> pinnedArrayData = new PinnedArrayData<UIntPtr>(points, false);
			IntPtr pointer = pinnedArrayData.Pointer;
			byte[] array = null;
			if (prefix != null)
			{
				int byteCount = ScriptingInterfaceOfIScene._utf8.GetByteCount(prefix);
				array = ((byteCount < 1024) ? CallbackStringBufferManager.StringBuffer0 : new byte[byteCount + 1]);
				ScriptingInterfaceOfIScene._utf8.GetBytes(prefix, 0, prefix.Length, array, 0);
				array[byteCount] = 0;
			}
			ScriptingInterfaceOfIScene.call_GetPathsWithNamePrefixDelegate(ptr, pointer, array);
			pinnedArrayData.Dispose();
		}

		// Token: 0x06000464 RID: 1124 RVA: 0x000163F8 File Offset: 0x000145F8
		public Path GetPathWithName(UIntPtr scenePointer, string name)
		{
			byte[] array = null;
			if (name != null)
			{
				int byteCount = ScriptingInterfaceOfIScene._utf8.GetByteCount(name);
				array = ((byteCount < 1024) ? CallbackStringBufferManager.StringBuffer0 : new byte[byteCount + 1]);
				ScriptingInterfaceOfIScene._utf8.GetBytes(name, 0, name.Length, array, 0);
				array[byteCount] = 0;
			}
			NativeObjectPointer nativeObjectPointer = ScriptingInterfaceOfIScene.call_GetPathWithNameDelegate(scenePointer, array);
			Path result = null;
			if (nativeObjectPointer.Pointer != UIntPtr.Zero)
			{
				result = new Path(nativeObjectPointer.Pointer);
				LibraryApplicationInterface.IManaged.DecreaseReferenceCount(nativeObjectPointer.Pointer);
			}
			return result;
		}

		// Token: 0x06000465 RID: 1125 RVA: 0x00016488 File Offset: 0x00014688
		public void GetPhotoModeFocus(Scene scene, ref float focus, ref float focusStart, ref float focusEnd, ref float exposure, ref bool vignetteOn)
		{
			UIntPtr scene2 = ((scene != null) ? scene.Pointer : UIntPtr.Zero);
			ScriptingInterfaceOfIScene.call_GetPhotoModeFocusDelegate(scene2, ref focus, ref focusStart, ref focusEnd, ref exposure, ref vignetteOn);
		}

		// Token: 0x06000466 RID: 1126 RVA: 0x000164C0 File Offset: 0x000146C0
		public float GetPhotoModeFov(Scene scene)
		{
			UIntPtr scene2 = ((scene != null) ? scene.Pointer : UIntPtr.Zero);
			return ScriptingInterfaceOfIScene.call_GetPhotoModeFovDelegate(scene2);
		}

		// Token: 0x06000467 RID: 1127 RVA: 0x000164F0 File Offset: 0x000146F0
		public bool GetPhotoModeOn(Scene scene)
		{
			UIntPtr scene2 = ((scene != null) ? scene.Pointer : UIntPtr.Zero);
			return ScriptingInterfaceOfIScene.call_GetPhotoModeOnDelegate(scene2);
		}

		// Token: 0x06000468 RID: 1128 RVA: 0x00016520 File Offset: 0x00014720
		public bool GetPhotoModeOrbit(Scene scene)
		{
			UIntPtr scene2 = ((scene != null) ? scene.Pointer : UIntPtr.Zero);
			return ScriptingInterfaceOfIScene.call_GetPhotoModeOrbitDelegate(scene2);
		}

		// Token: 0x06000469 RID: 1129 RVA: 0x00016550 File Offset: 0x00014750
		public float GetPhotoModeRoll(Scene scene)
		{
			UIntPtr scene2 = ((scene != null) ? scene.Pointer : UIntPtr.Zero);
			return ScriptingInterfaceOfIScene.call_GetPhotoModeRollDelegate(scene2);
		}

		// Token: 0x0600046A RID: 1130 RVA: 0x0001657F File Offset: 0x0001477F
		public void GetPhysicsMinMax(UIntPtr scenePointer, ref Vec3 min_max)
		{
			ScriptingInterfaceOfIScene.call_GetPhysicsMinMaxDelegate(scenePointer, ref min_max);
		}

		// Token: 0x0600046B RID: 1131 RVA: 0x0001658D File Offset: 0x0001478D
		public float GetRainDensity(UIntPtr scenePointer)
		{
			return ScriptingInterfaceOfIScene.call_GetRainDensityDelegate(scenePointer);
		}

		// Token: 0x0600046C RID: 1132 RVA: 0x0001659C File Offset: 0x0001479C
		public void GetRootEntities(Scene scene, NativeObjectArray output)
		{
			UIntPtr scene2 = ((scene != null) ? scene.Pointer : UIntPtr.Zero);
			UIntPtr output2 = ((output != null) ? output.Pointer : UIntPtr.Zero);
			ScriptingInterfaceOfIScene.call_GetRootEntitiesDelegate(scene2, output2);
		}

		// Token: 0x0600046D RID: 1133 RVA: 0x000165E3 File Offset: 0x000147E3
		public int GetRootEntityCount(UIntPtr scenePointer)
		{
			return ScriptingInterfaceOfIScene.call_GetRootEntityCountDelegate(scenePointer);
		}

		// Token: 0x0600046E RID: 1134 RVA: 0x000165F0 File Offset: 0x000147F0
		public int GetSceneColorGradeIndex(Scene scene)
		{
			UIntPtr scene2 = ((scene != null) ? scene.Pointer : UIntPtr.Zero);
			return ScriptingInterfaceOfIScene.call_GetSceneColorGradeIndexDelegate(scene2);
		}

		// Token: 0x0600046F RID: 1135 RVA: 0x00016620 File Offset: 0x00014820
		public int GetSceneFilterIndex(Scene scene)
		{
			UIntPtr scene2 = ((scene != null) ? scene.Pointer : UIntPtr.Zero);
			return ScriptingInterfaceOfIScene.call_GetSceneFilterIndexDelegate(scene2);
		}

		// Token: 0x06000470 RID: 1136 RVA: 0x0001664F File Offset: 0x0001484F
		public void GetSceneLimits(UIntPtr scenePointer, ref Vec3 min, ref Vec3 max)
		{
			ScriptingInterfaceOfIScene.call_GetSceneLimitsDelegate(scenePointer, ref min, ref max);
		}

		// Token: 0x06000471 RID: 1137 RVA: 0x0001665E File Offset: 0x0001485E
		public uint GetSceneXMLCRC(UIntPtr scenePointer)
		{
			return ScriptingInterfaceOfIScene.call_GetSceneXMLCRCDelegate(scenePointer);
		}

		// Token: 0x06000472 RID: 1138 RVA: 0x0001666C File Offset: 0x0001486C
		public GameEntity GetScriptedEntity(UIntPtr scenePointer, int index)
		{
			NativeObjectPointer nativeObjectPointer = ScriptingInterfaceOfIScene.call_GetScriptedEntityDelegate(scenePointer, index);
			GameEntity result = null;
			if (nativeObjectPointer.Pointer != UIntPtr.Zero)
			{
				result = new GameEntity(nativeObjectPointer.Pointer);
				LibraryApplicationInterface.IManaged.DecreaseReferenceCount(nativeObjectPointer.Pointer);
			}
			return result;
		}

		// Token: 0x06000473 RID: 1139 RVA: 0x000166B7 File Offset: 0x000148B7
		public int GetScriptedEntityCount(UIntPtr scenePointer)
		{
			return ScriptingInterfaceOfIScene.call_GetScriptedEntityCountDelegate(scenePointer);
		}

		// Token: 0x06000474 RID: 1140 RVA: 0x000166C4 File Offset: 0x000148C4
		public Mesh GetSkyboxMesh(UIntPtr ptr)
		{
			NativeObjectPointer nativeObjectPointer = ScriptingInterfaceOfIScene.call_GetSkyboxMeshDelegate(ptr);
			Mesh result = null;
			if (nativeObjectPointer.Pointer != UIntPtr.Zero)
			{
				result = new Mesh(nativeObjectPointer.Pointer);
				LibraryApplicationInterface.IManaged.DecreaseReferenceCount(nativeObjectPointer.Pointer);
			}
			return result;
		}

		// Token: 0x06000475 RID: 1141 RVA: 0x0001670E File Offset: 0x0001490E
		public float GetSnowDensity(UIntPtr scenePointer)
		{
			return ScriptingInterfaceOfIScene.call_GetSnowDensityDelegate(scenePointer);
		}

		// Token: 0x06000476 RID: 1142 RVA: 0x0001671B File Offset: 0x0001491B
		public Vec2 GetSoftBoundaryVertex(UIntPtr scenePointer, int index)
		{
			return ScriptingInterfaceOfIScene.call_GetSoftBoundaryVertexDelegate(scenePointer, index);
		}

		// Token: 0x06000477 RID: 1143 RVA: 0x00016729 File Offset: 0x00014929
		public int GetSoftBoundaryVertexCount(UIntPtr scenePointer)
		{
			return ScriptingInterfaceOfIScene.call_GetSoftBoundaryVertexCountDelegate(scenePointer);
		}

		// Token: 0x06000478 RID: 1144 RVA: 0x00016736 File Offset: 0x00014936
		public Vec3 GetSunDirection(UIntPtr scenePointer)
		{
			return ScriptingInterfaceOfIScene.call_GetSunDirectionDelegate(scenePointer);
		}

		// Token: 0x06000479 RID: 1145 RVA: 0x00016744 File Offset: 0x00014944
		public void GetTerrainData(Scene scene, out Vec2i nodeDimension, out float nodeSize, out int layerCount, out int layerVersion)
		{
			UIntPtr scene2 = ((scene != null) ? scene.Pointer : UIntPtr.Zero);
			ScriptingInterfaceOfIScene.call_GetTerrainDataDelegate(scene2, out nodeDimension, out nodeSize, out layerCount, out layerVersion);
		}

		// Token: 0x0600047A RID: 1146 RVA: 0x00016779 File Offset: 0x00014979
		public float GetTerrainHeight(UIntPtr scenePointer, Vec2 position, bool checkHoles)
		{
			return ScriptingInterfaceOfIScene.call_GetTerrainHeightDelegate(scenePointer, position, checkHoles);
		}

		// Token: 0x0600047B RID: 1147 RVA: 0x00016788 File Offset: 0x00014988
		public void GetTerrainHeightAndNormal(UIntPtr scenePointer, Vec2 position, out float height, out Vec3 normal)
		{
			ScriptingInterfaceOfIScene.call_GetTerrainHeightAndNormalDelegate(scenePointer, position, out height, out normal);
		}

		// Token: 0x0600047C RID: 1148 RVA: 0x00016799 File Offset: 0x00014999
		public int GetTerrainMemoryUsage(UIntPtr scenePointer)
		{
			return ScriptingInterfaceOfIScene.call_GetTerrainMemoryUsageDelegate(scenePointer);
		}

		// Token: 0x0600047D RID: 1149 RVA: 0x000167A8 File Offset: 0x000149A8
		public bool GetTerrainMinMaxHeight(Scene scene, ref float min, ref float max)
		{
			UIntPtr scene2 = ((scene != null) ? scene.Pointer : UIntPtr.Zero);
			return ScriptingInterfaceOfIScene.call_GetTerrainMinMaxHeightDelegate(scene2, ref min, ref max);
		}

		// Token: 0x0600047E RID: 1150 RVA: 0x000167DC File Offset: 0x000149DC
		public void GetTerrainNodeData(Scene scene, int xIndex, int yIndex, out int vertexCountAlongAxis, out float quadLength, out float minHeight, out float maxHeight)
		{
			UIntPtr scene2 = ((scene != null) ? scene.Pointer : UIntPtr.Zero);
			ScriptingInterfaceOfIScene.call_GetTerrainNodeDataDelegate(scene2, xIndex, yIndex, out vertexCountAlongAxis, out quadLength, out minHeight, out maxHeight);
		}

		// Token: 0x0600047F RID: 1151 RVA: 0x00016818 File Offset: 0x00014A18
		public int GetTerrainPhysicsMaterialIndexAtLayer(Scene scene, int layerIndex)
		{
			UIntPtr scene2 = ((scene != null) ? scene.Pointer : UIntPtr.Zero);
			return ScriptingInterfaceOfIScene.call_GetTerrainPhysicsMaterialIndexAtLayerDelegate(scene2, layerIndex);
		}

		// Token: 0x06000480 RID: 1152 RVA: 0x00016848 File Offset: 0x00014A48
		public float GetTimeOfDay(UIntPtr scenePointer)
		{
			return ScriptingInterfaceOfIScene.call_GetTimeOfDayDelegate(scenePointer);
		}

		// Token: 0x06000481 RID: 1153 RVA: 0x00016855 File Offset: 0x00014A55
		public float GetTimeSpeed(UIntPtr scenePointer)
		{
			return ScriptingInterfaceOfIScene.call_GetTimeSpeedDelegate(scenePointer);
		}

		// Token: 0x06000482 RID: 1154 RVA: 0x00016862 File Offset: 0x00014A62
		public int GetUpgradeLevelCount(UIntPtr scenePointer)
		{
			return ScriptingInterfaceOfIScene.call_GetUpgradeLevelCountDelegate(scenePointer);
		}

		// Token: 0x06000483 RID: 1155 RVA: 0x0001686F File Offset: 0x00014A6F
		public uint GetUpgradeLevelMask(UIntPtr scenePointer)
		{
			return ScriptingInterfaceOfIScene.call_GetUpgradeLevelMaskDelegate(scenePointer);
		}

		// Token: 0x06000484 RID: 1156 RVA: 0x0001687C File Offset: 0x00014A7C
		public uint GetUpgradeLevelMaskOfLevelName(UIntPtr scenePointer, string levelName)
		{
			byte[] array = null;
			if (levelName != null)
			{
				int byteCount = ScriptingInterfaceOfIScene._utf8.GetByteCount(levelName);
				array = ((byteCount < 1024) ? CallbackStringBufferManager.StringBuffer0 : new byte[byteCount + 1]);
				ScriptingInterfaceOfIScene._utf8.GetBytes(levelName, 0, levelName.Length, array, 0);
				array[byteCount] = 0;
			}
			return ScriptingInterfaceOfIScene.call_GetUpgradeLevelMaskOfLevelNameDelegate(scenePointer, array);
		}

		// Token: 0x06000485 RID: 1157 RVA: 0x000168D7 File Offset: 0x00014AD7
		public string GetUpgradeLevelNameOfIndex(UIntPtr scenePointer, int index)
		{
			if (ScriptingInterfaceOfIScene.call_GetUpgradeLevelNameOfIndexDelegate(scenePointer, index) != 1)
			{
				return null;
			}
			return Managed.ReturnValueFromEngine;
		}

		// Token: 0x06000486 RID: 1158 RVA: 0x000168F0 File Offset: 0x00014AF0
		public float GetWaterLevel(Scene scene)
		{
			UIntPtr scene2 = ((scene != null) ? scene.Pointer : UIntPtr.Zero);
			return ScriptingInterfaceOfIScene.call_GetWaterLevelDelegate(scene2);
		}

		// Token: 0x06000487 RID: 1159 RVA: 0x00016920 File Offset: 0x00014B20
		public float GetWaterLevelAtPosition(Scene scene, Vec2 position, bool useWaterRenderer, bool checkWaterBodyEntities)
		{
			UIntPtr scene2 = ((scene != null) ? scene.Pointer : UIntPtr.Zero);
			return ScriptingInterfaceOfIScene.call_GetWaterLevelAtPositionDelegate(scene2, position, useWaterRenderer, checkWaterBodyEntities);
		}

		// Token: 0x06000488 RID: 1160 RVA: 0x00016953 File Offset: 0x00014B53
		public Vec3 GetWaterSpeedAtPosition(UIntPtr scenePointer, in Vec2 position, bool doChoppinessCorrection)
		{
			return ScriptingInterfaceOfIScene.call_GetWaterSpeedAtPositionDelegate(scenePointer, position, doChoppinessCorrection);
		}

		// Token: 0x06000489 RID: 1161 RVA: 0x00016964 File Offset: 0x00014B64
		public float GetWaterStrength(Scene scene)
		{
			UIntPtr scene2 = ((scene != null) ? scene.Pointer : UIntPtr.Zero);
			return ScriptingInterfaceOfIScene.call_GetWaterStrengthDelegate(scene2);
		}

		// Token: 0x0600048A RID: 1162 RVA: 0x00016994 File Offset: 0x00014B94
		public void GetWindFlowMapData(UIntPtr scenePointer, float[] flowmapData)
		{
			PinnedArrayData<float> pinnedArrayData = new PinnedArrayData<float>(flowmapData, false);
			IntPtr pointer = pinnedArrayData.Pointer;
			ScriptingInterfaceOfIScene.call_GetWindFlowMapDataDelegate(scenePointer, pointer);
			pinnedArrayData.Dispose();
		}

		// Token: 0x0600048B RID: 1163 RVA: 0x000169C5 File Offset: 0x00014BC5
		public float GetWinterTimeFactor(UIntPtr scenePointer)
		{
			return ScriptingInterfaceOfIScene.call_GetWinterTimeFactorDelegate(scenePointer);
		}

		// Token: 0x0600048C RID: 1164 RVA: 0x000169D2 File Offset: 0x00014BD2
		public bool HasDecalRenderer(UIntPtr scenePointer)
		{
			return ScriptingInterfaceOfIScene.call_HasDecalRendererDelegate(scenePointer);
		}

		// Token: 0x0600048D RID: 1165 RVA: 0x000169DF File Offset: 0x00014BDF
		public bool HasNavmeshFaceUnsharedEdges(UIntPtr scenePointer, in PathFaceRecord faceRecord)
		{
			return ScriptingInterfaceOfIScene.call_HasNavmeshFaceUnsharedEdgesDelegate(scenePointer, faceRecord);
		}

		// Token: 0x0600048E RID: 1166 RVA: 0x000169ED File Offset: 0x00014BED
		public bool HasTerrainHeightmap(UIntPtr scenePointer)
		{
			return ScriptingInterfaceOfIScene.call_HasTerrainHeightmapDelegate(scenePointer);
		}

		// Token: 0x0600048F RID: 1167 RVA: 0x000169FA File Offset: 0x00014BFA
		public void InvalidateTerrainPhysicsMaterials(UIntPtr scenePointer)
		{
			ScriptingInterfaceOfIScene.call_InvalidateTerrainPhysicsMaterialsDelegate(scenePointer);
		}

		// Token: 0x06000490 RID: 1168 RVA: 0x00016A07 File Offset: 0x00014C07
		public bool IsAnyFaceWithId(UIntPtr scenePointer, int faceGroupId)
		{
			return ScriptingInterfaceOfIScene.call_IsAnyFaceWithIdDelegate(scenePointer, faceGroupId);
		}

		// Token: 0x06000491 RID: 1169 RVA: 0x00016A15 File Offset: 0x00014C15
		public bool IsAtmosphereIndoor(UIntPtr scenePointer)
		{
			return ScriptingInterfaceOfIScene.call_IsAtmosphereIndoorDelegate(scenePointer);
		}

		// Token: 0x06000492 RID: 1170 RVA: 0x00016A24 File Offset: 0x00014C24
		public bool IsDefaultEditorScene(Scene scene)
		{
			UIntPtr scene2 = ((scene != null) ? scene.Pointer : UIntPtr.Zero);
			return ScriptingInterfaceOfIScene.call_IsDefaultEditorSceneDelegate(scene2);
		}

		// Token: 0x06000493 RID: 1171 RVA: 0x00016A53 File Offset: 0x00014C53
		public bool IsEditorScene(UIntPtr scenePointer)
		{
			return ScriptingInterfaceOfIScene.call_IsEditorSceneDelegate(scenePointer);
		}

		// Token: 0x06000494 RID: 1172 RVA: 0x00016A60 File Offset: 0x00014C60
		public bool IsLineToPointClear(UIntPtr scenePointer, int startingFace, Vec2 position, Vec2 destination, float agentRadius)
		{
			return ScriptingInterfaceOfIScene.call_IsLineToPointClearDelegate(scenePointer, startingFace, position, destination, agentRadius);
		}

		// Token: 0x06000495 RID: 1173 RVA: 0x00016A73 File Offset: 0x00014C73
		public bool IsLineToPointClear2(UIntPtr scenePointer, UIntPtr startingFace, Vec2 position, Vec2 destination, float agentRadius)
		{
			return ScriptingInterfaceOfIScene.call_IsLineToPointClear2Delegate(scenePointer, startingFace, position, destination, agentRadius);
		}

		// Token: 0x06000496 RID: 1174 RVA: 0x00016A88 File Offset: 0x00014C88
		public bool IsLoadingFinished(Scene scene)
		{
			UIntPtr scene2 = ((scene != null) ? scene.Pointer : UIntPtr.Zero);
			return ScriptingInterfaceOfIScene.call_IsLoadingFinishedDelegate(scene2);
		}

		// Token: 0x06000497 RID: 1175 RVA: 0x00016AB8 File Offset: 0x00014CB8
		public bool IsMultiplayerScene(Scene scene)
		{
			UIntPtr scene2 = ((scene != null) ? scene.Pointer : UIntPtr.Zero);
			return ScriptingInterfaceOfIScene.call_IsMultiplayerSceneDelegate(scene2);
		}

		// Token: 0x06000498 RID: 1176 RVA: 0x00016AE7 File Offset: 0x00014CE7
		public bool IsPositionOnADynamicNavMesh(UIntPtr scenePointer, Vec3 position)
		{
			return ScriptingInterfaceOfIScene.call_IsPositionOnADynamicNavMeshDelegate(scenePointer, position);
		}

		// Token: 0x06000499 RID: 1177 RVA: 0x00016AF8 File Offset: 0x00014CF8
		public void LoadNavMeshPrefab(UIntPtr scenePointer, string navMeshPrefabName, int navMeshGroupIdShift)
		{
			byte[] array = null;
			if (navMeshPrefabName != null)
			{
				int byteCount = ScriptingInterfaceOfIScene._utf8.GetByteCount(navMeshPrefabName);
				array = ((byteCount < 1024) ? CallbackStringBufferManager.StringBuffer0 : new byte[byteCount + 1]);
				ScriptingInterfaceOfIScene._utf8.GetBytes(navMeshPrefabName, 0, navMeshPrefabName.Length, array, 0);
				array[byteCount] = 0;
			}
			ScriptingInterfaceOfIScene.call_LoadNavMeshPrefabDelegate(scenePointer, array, navMeshGroupIdShift);
		}

		// Token: 0x0600049A RID: 1178 RVA: 0x00016B54 File Offset: 0x00014D54
		public void LoadNavMeshPrefabWithFrame(UIntPtr scenePointer, string navMeshPrefabName, MatrixFrame frame)
		{
			byte[] array = null;
			if (navMeshPrefabName != null)
			{
				int byteCount = ScriptingInterfaceOfIScene._utf8.GetByteCount(navMeshPrefabName);
				array = ((byteCount < 1024) ? CallbackStringBufferManager.StringBuffer0 : new byte[byteCount + 1]);
				ScriptingInterfaceOfIScene._utf8.GetBytes(navMeshPrefabName, 0, navMeshPrefabName.Length, array, 0);
				array[byteCount] = 0;
			}
			ScriptingInterfaceOfIScene.call_LoadNavMeshPrefabWithFrameDelegate(scenePointer, array, frame);
		}

		// Token: 0x0600049B RID: 1179 RVA: 0x00016BB0 File Offset: 0x00014DB0
		public void MarkFacesWithIdAsLadder(UIntPtr scenePointer, int faceGroupId, bool isLadder)
		{
			ScriptingInterfaceOfIScene.call_MarkFacesWithIdAsLadderDelegate(scenePointer, faceGroupId, isLadder);
		}

		// Token: 0x0600049C RID: 1180 RVA: 0x00016BBF File Offset: 0x00014DBF
		public void MergeFacesWithId(UIntPtr scenePointer, int faceGroupId0, int faceGroupId1, int newFaceGroupId)
		{
			ScriptingInterfaceOfIScene.call_MergeFacesWithIdDelegate(scenePointer, faceGroupId0, faceGroupId1, newFaceGroupId);
		}

		// Token: 0x0600049D RID: 1181 RVA: 0x00016BD0 File Offset: 0x00014DD0
		public void OptimizeScene(UIntPtr scenePointer, bool optimizeFlora, bool optimizeOro)
		{
			ScriptingInterfaceOfIScene.call_OptimizeSceneDelegate(scenePointer, optimizeFlora, optimizeOro);
		}

		// Token: 0x0600049E RID: 1182 RVA: 0x00016BDF File Offset: 0x00014DDF
		public void PauseSceneSounds(UIntPtr scenePointer)
		{
			ScriptingInterfaceOfIScene.call_PauseSceneSoundsDelegate(scenePointer);
		}

		// Token: 0x0600049F RID: 1183 RVA: 0x00016BEC File Offset: 0x00014DEC
		public void PreloadForRendering(UIntPtr scenePointer)
		{
			ScriptingInterfaceOfIScene.call_PreloadForRenderingDelegate(scenePointer);
		}

		// Token: 0x060004A0 RID: 1184 RVA: 0x00016BF9 File Offset: 0x00014DF9
		public bool RayCastExcludingTwoEntities(BodyFlags flags, UIntPtr scenePointer, in Ray ray, UIntPtr entity1, UIntPtr entity2)
		{
			return ScriptingInterfaceOfIScene.call_RayCastExcludingTwoEntitiesDelegate(flags, scenePointer, ray, entity1, entity2);
		}

		// Token: 0x060004A1 RID: 1185 RVA: 0x00016C0C File Offset: 0x00014E0C
		public bool RayCastForClosestEntityOrTerrain(UIntPtr scenePointer, in Vec3 sourcePoint, in Vec3 targetPoint, float rayThickness, ref float collisionDistance, ref Vec3 closestPoint, ref UIntPtr entityIndex, BodyFlags bodyExcludeFlags, bool isFixedWorld)
		{
			return ScriptingInterfaceOfIScene.call_RayCastForClosestEntityOrTerrainDelegate(scenePointer, sourcePoint, targetPoint, rayThickness, ref collisionDistance, ref closestPoint, ref entityIndex, bodyExcludeFlags, isFixedWorld);
		}

		// Token: 0x060004A2 RID: 1186 RVA: 0x00016C34 File Offset: 0x00014E34
		public bool RayCastForClosestEntityOrTerrainIgnoreEntity(UIntPtr scenePointer, in Vec3 sourcePoint, in Vec3 targetPoint, float rayThickness, ref float collisionDistance, ref Vec3 closestPoint, ref UIntPtr entityIndex, BodyFlags bodyExcludeFlags, UIntPtr ignoredEntityPointer)
		{
			return ScriptingInterfaceOfIScene.call_RayCastForClosestEntityOrTerrainIgnoreEntityDelegate(scenePointer, sourcePoint, targetPoint, rayThickness, ref collisionDistance, ref closestPoint, ref entityIndex, bodyExcludeFlags, ignoredEntityPointer);
		}

		// Token: 0x060004A3 RID: 1187 RVA: 0x00016C5C File Offset: 0x00014E5C
		public bool RayCastForRamming(UIntPtr scenePointer, in Vec3 sourcePoint, in Vec3 targetPoint, float rayThickness, ref float collisionDistance, ref Vec3 intersectionPoint, ref UIntPtr intersectedEntityIndex, BodyFlags bodyExcludeFlags, BodyFlags bodyIncludeFlags, UIntPtr ignoredEntityPointer)
		{
			return ScriptingInterfaceOfIScene.call_RayCastForRammingDelegate(scenePointer, sourcePoint, targetPoint, rayThickness, ref collisionDistance, ref intersectionPoint, ref intersectedEntityIndex, bodyExcludeFlags, bodyIncludeFlags, ignoredEntityPointer);
		}

		// Token: 0x060004A4 RID: 1188 RVA: 0x00016C84 File Offset: 0x00014E84
		public void Read(UIntPtr scenePointer, string sceneName, ref SceneInitializationData initData, string forcedAtmoName)
		{
			byte[] array = null;
			if (sceneName != null)
			{
				int byteCount = ScriptingInterfaceOfIScene._utf8.GetByteCount(sceneName);
				array = ((byteCount < 1024) ? CallbackStringBufferManager.StringBuffer0 : new byte[byteCount + 1]);
				ScriptingInterfaceOfIScene._utf8.GetBytes(sceneName, 0, sceneName.Length, array, 0);
				array[byteCount] = 0;
			}
			byte[] array2 = null;
			if (forcedAtmoName != null)
			{
				int byteCount2 = ScriptingInterfaceOfIScene._utf8.GetByteCount(forcedAtmoName);
				array2 = ((byteCount2 < 1024) ? CallbackStringBufferManager.StringBuffer1 : new byte[byteCount2 + 1]);
				ScriptingInterfaceOfIScene._utf8.GetBytes(forcedAtmoName, 0, forcedAtmoName.Length, array2, 0);
				array2[byteCount2] = 0;
			}
			ScriptingInterfaceOfIScene.call_ReadDelegate(scenePointer, array, ref initData, array2);
		}

		// Token: 0x060004A5 RID: 1189 RVA: 0x00016D27 File Offset: 0x00014F27
		public void ReadAndCalculateInitialCamera(UIntPtr scenePointer, ref MatrixFrame outFrame)
		{
			ScriptingInterfaceOfIScene.call_ReadAndCalculateInitialCameraDelegate(scenePointer, ref outFrame);
		}

		// Token: 0x060004A6 RID: 1190 RVA: 0x00016D38 File Offset: 0x00014F38
		public void ReadInModule(UIntPtr scenePointer, string sceneName, string moduleId, ref SceneInitializationData initData, string forcedAtmoName)
		{
			byte[] array = null;
			if (sceneName != null)
			{
				int byteCount = ScriptingInterfaceOfIScene._utf8.GetByteCount(sceneName);
				array = ((byteCount < 1024) ? CallbackStringBufferManager.StringBuffer0 : new byte[byteCount + 1]);
				ScriptingInterfaceOfIScene._utf8.GetBytes(sceneName, 0, sceneName.Length, array, 0);
				array[byteCount] = 0;
			}
			byte[] array2 = null;
			if (moduleId != null)
			{
				int byteCount2 = ScriptingInterfaceOfIScene._utf8.GetByteCount(moduleId);
				array2 = ((byteCount2 < 1024) ? CallbackStringBufferManager.StringBuffer1 : new byte[byteCount2 + 1]);
				ScriptingInterfaceOfIScene._utf8.GetBytes(moduleId, 0, moduleId.Length, array2, 0);
				array2[byteCount2] = 0;
			}
			byte[] array3 = null;
			if (forcedAtmoName != null)
			{
				int byteCount3 = ScriptingInterfaceOfIScene._utf8.GetByteCount(forcedAtmoName);
				array3 = ((byteCount3 < 1024) ? CallbackStringBufferManager.StringBuffer2 : new byte[byteCount3 + 1]);
				ScriptingInterfaceOfIScene._utf8.GetBytes(forcedAtmoName, 0, forcedAtmoName.Length, array3, 0);
				array3[byteCount3] = 0;
			}
			ScriptingInterfaceOfIScene.call_ReadInModuleDelegate(scenePointer, array, array2, ref initData, array3);
		}

		// Token: 0x060004A7 RID: 1191 RVA: 0x00016E27 File Offset: 0x00015027
		public UIntPtr RegisterShipVisualToWaterRenderer(UIntPtr scenePointer, UIntPtr entityPointer, in Vec3 bb)
		{
			return ScriptingInterfaceOfIScene.call_RegisterShipVisualToWaterRendererDelegate(scenePointer, entityPointer, bb);
		}

		// Token: 0x060004A8 RID: 1192 RVA: 0x00016E36 File Offset: 0x00015036
		public void RemoveAlwaysRenderedSkeleton(UIntPtr scenePointer, UIntPtr skeletonPointer)
		{
			ScriptingInterfaceOfIScene.call_RemoveAlwaysRenderedSkeletonDelegate(scenePointer, skeletonPointer);
		}

		// Token: 0x060004A9 RID: 1193 RVA: 0x00016E44 File Offset: 0x00015044
		public void RemoveDecalInstance(UIntPtr scenePointer, UIntPtr decalMeshPointer, string decalSetID)
		{
			byte[] array = null;
			if (decalSetID != null)
			{
				int byteCount = ScriptingInterfaceOfIScene._utf8.GetByteCount(decalSetID);
				array = ((byteCount < 1024) ? CallbackStringBufferManager.StringBuffer0 : new byte[byteCount + 1]);
				ScriptingInterfaceOfIScene._utf8.GetBytes(decalSetID, 0, decalSetID.Length, array, 0);
				array[byteCount] = 0;
			}
			ScriptingInterfaceOfIScene.call_RemoveDecalInstanceDelegate(scenePointer, decalMeshPointer, array);
		}

		// Token: 0x060004AA RID: 1194 RVA: 0x00016EA0 File Offset: 0x000150A0
		public void RemoveEntity(UIntPtr scenePointer, UIntPtr entityId, int removeReason)
		{
			ScriptingInterfaceOfIScene.call_RemoveEntityDelegate(scenePointer, entityId, removeReason);
		}

		// Token: 0x060004AB RID: 1195 RVA: 0x00016EAF File Offset: 0x000150AF
		public void ResumeLoadingRenderings(UIntPtr scenePointer)
		{
			ScriptingInterfaceOfIScene.call_ResumeLoadingRenderingsDelegate(scenePointer);
		}

		// Token: 0x060004AC RID: 1196 RVA: 0x00016EBC File Offset: 0x000150BC
		public void ResumeSceneSounds(UIntPtr scenePointer)
		{
			ScriptingInterfaceOfIScene.call_ResumeSceneSoundsDelegate(scenePointer);
		}

		// Token: 0x060004AD RID: 1197 RVA: 0x00016ECC File Offset: 0x000150CC
		public void SaveNavMeshPrefabWithFrame(UIntPtr scenePointer, string navMeshPrefabName, MatrixFrame frame)
		{
			byte[] array = null;
			if (navMeshPrefabName != null)
			{
				int byteCount = ScriptingInterfaceOfIScene._utf8.GetByteCount(navMeshPrefabName);
				array = ((byteCount < 1024) ? CallbackStringBufferManager.StringBuffer0 : new byte[byteCount + 1]);
				ScriptingInterfaceOfIScene._utf8.GetBytes(navMeshPrefabName, 0, navMeshPrefabName.Length, array, 0);
				array[byteCount] = 0;
			}
			ScriptingInterfaceOfIScene.call_SaveNavMeshPrefabWithFrameDelegate(scenePointer, array, frame);
		}

		// Token: 0x060004AE RID: 1198 RVA: 0x00016F28 File Offset: 0x00015128
		public bool SceneHadWaterWakeRenderer(UIntPtr scenePointer)
		{
			return ScriptingInterfaceOfIScene.call_SceneHadWaterWakeRendererDelegate(scenePointer);
		}

		// Token: 0x060004AF RID: 1199 RVA: 0x00016F38 File Offset: 0x00015138
		public int SelectEntitiesCollidedWith(UIntPtr scenePointer, ref Ray ray, UIntPtr[] entityIds, Intersection[] intersections)
		{
			PinnedArrayData<UIntPtr> pinnedArrayData = new PinnedArrayData<UIntPtr>(entityIds, false);
			IntPtr pointer = pinnedArrayData.Pointer;
			PinnedArrayData<Intersection> pinnedArrayData2 = new PinnedArrayData<Intersection>(intersections, false);
			IntPtr pointer2 = pinnedArrayData2.Pointer;
			int result = ScriptingInterfaceOfIScene.call_SelectEntitiesCollidedWithDelegate(scenePointer, ref ray, pointer, pointer2);
			pinnedArrayData.Dispose();
			pinnedArrayData2.Dispose();
			return result;
		}

		// Token: 0x060004B0 RID: 1200 RVA: 0x00016F84 File Offset: 0x00015184
		public int SelectEntitiesInBoxWithScriptComponent(UIntPtr scenePointer, ref Vec3 boundingBoxMin, ref Vec3 boundingBoxMax, UIntPtr[] entitiesOutput, int maxCount, string scriptComponentName)
		{
			PinnedArrayData<UIntPtr> pinnedArrayData = new PinnedArrayData<UIntPtr>(entitiesOutput, false);
			IntPtr pointer = pinnedArrayData.Pointer;
			byte[] array = null;
			if (scriptComponentName != null)
			{
				int byteCount = ScriptingInterfaceOfIScene._utf8.GetByteCount(scriptComponentName);
				array = ((byteCount < 1024) ? CallbackStringBufferManager.StringBuffer0 : new byte[byteCount + 1]);
				ScriptingInterfaceOfIScene._utf8.GetBytes(scriptComponentName, 0, scriptComponentName.Length, array, 0);
				array[byteCount] = 0;
			}
			int result = ScriptingInterfaceOfIScene.call_SelectEntitiesInBoxWithScriptComponentDelegate(scenePointer, ref boundingBoxMin, ref boundingBoxMax, pointer, maxCount, array);
			pinnedArrayData.Dispose();
			return result;
		}

		// Token: 0x060004B1 RID: 1201 RVA: 0x00017001 File Offset: 0x00015201
		public void SeparateFacesWithId(UIntPtr scenePointer, int faceGroupId0, int faceGroupId1)
		{
			ScriptingInterfaceOfIScene.call_SeparateFacesWithIdDelegate(scenePointer, faceGroupId0, faceGroupId1);
		}

		// Token: 0x060004B2 RID: 1202 RVA: 0x00017010 File Offset: 0x00015210
		public void SetAberrationOffset(UIntPtr scenePointer, float aberrationOffset)
		{
			ScriptingInterfaceOfIScene.call_SetAberrationOffsetDelegate(scenePointer, aberrationOffset);
		}

		// Token: 0x060004B3 RID: 1203 RVA: 0x0001701E File Offset: 0x0001521E
		public void SetAberrationSize(UIntPtr scenePointer, float aberrationSize)
		{
			ScriptingInterfaceOfIScene.call_SetAberrationSizeDelegate(scenePointer, aberrationSize);
		}

		// Token: 0x060004B4 RID: 1204 RVA: 0x0001702C File Offset: 0x0001522C
		public void SetAberrationSmooth(UIntPtr scenePointer, float aberrationSmooth)
		{
			ScriptingInterfaceOfIScene.call_SetAberrationSmoothDelegate(scenePointer, aberrationSmooth);
		}

		// Token: 0x060004B5 RID: 1205 RVA: 0x0001703A File Offset: 0x0001523A
		public int SetAbilityOfFacesWithId(UIntPtr scenePointer, int faceGroupId, bool isEnabled)
		{
			return ScriptingInterfaceOfIScene.call_SetAbilityOfFacesWithIdDelegate(scenePointer, faceGroupId, isEnabled);
		}

		// Token: 0x060004B6 RID: 1206 RVA: 0x0001704C File Offset: 0x0001524C
		public void SetActiveVisibilityLevels(UIntPtr scenePointer, string levelsAppended)
		{
			byte[] array = null;
			if (levelsAppended != null)
			{
				int byteCount = ScriptingInterfaceOfIScene._utf8.GetByteCount(levelsAppended);
				array = ((byteCount < 1024) ? CallbackStringBufferManager.StringBuffer0 : new byte[byteCount + 1]);
				ScriptingInterfaceOfIScene._utf8.GetBytes(levelsAppended, 0, levelsAppended.Length, array, 0);
				array[byteCount] = 0;
			}
			ScriptingInterfaceOfIScene.call_SetActiveVisibilityLevelsDelegate(scenePointer, array);
		}

		// Token: 0x060004B7 RID: 1207 RVA: 0x000170A7 File Offset: 0x000152A7
		public void SetAntialiasingMode(UIntPtr scenePointer, bool mode)
		{
			ScriptingInterfaceOfIScene.call_SetAntialiasingModeDelegate(scenePointer, mode);
		}

		// Token: 0x060004B8 RID: 1208 RVA: 0x000170B8 File Offset: 0x000152B8
		public void SetAtmosphereWithName(UIntPtr ptr, string name)
		{
			byte[] array = null;
			if (name != null)
			{
				int byteCount = ScriptingInterfaceOfIScene._utf8.GetByteCount(name);
				array = ((byteCount < 1024) ? CallbackStringBufferManager.StringBuffer0 : new byte[byteCount + 1]);
				ScriptingInterfaceOfIScene._utf8.GetBytes(name, 0, name.Length, array, 0);
				array[byteCount] = 0;
			}
			ScriptingInterfaceOfIScene.call_SetAtmosphereWithNameDelegate(ptr, array);
		}

		// Token: 0x060004B9 RID: 1209 RVA: 0x00017113 File Offset: 0x00015313
		public void SetBloom(UIntPtr scenePointer, bool mode)
		{
			ScriptingInterfaceOfIScene.call_SetBloomDelegate(scenePointer, mode);
		}

		// Token: 0x060004BA RID: 1210 RVA: 0x00017121 File Offset: 0x00015321
		public void SetBloomAmount(UIntPtr scenePointer, float bloomAmount)
		{
			ScriptingInterfaceOfIScene.call_SetBloomAmountDelegate(scenePointer, bloomAmount);
		}

		// Token: 0x060004BB RID: 1211 RVA: 0x0001712F File Offset: 0x0001532F
		public void SetBloomStrength(UIntPtr scenePointer, float bloomStrength)
		{
			ScriptingInterfaceOfIScene.call_SetBloomStrengthDelegate(scenePointer, bloomStrength);
		}

		// Token: 0x060004BC RID: 1212 RVA: 0x0001713D File Offset: 0x0001533D
		public void SetBrightpassTreshold(UIntPtr scenePointer, float threshold)
		{
			ScriptingInterfaceOfIScene.call_SetBrightpassTresholdDelegate(scenePointer, threshold);
		}

		// Token: 0x060004BD RID: 1213 RVA: 0x0001714B File Offset: 0x0001534B
		public void SetClothSimulationState(UIntPtr scenePointer, bool state)
		{
			ScriptingInterfaceOfIScene.call_SetClothSimulationStateDelegate(scenePointer, state);
		}

		// Token: 0x060004BE RID: 1214 RVA: 0x0001715C File Offset: 0x0001535C
		public void SetColorGradeBlend(UIntPtr scenePointer, string texture1, string texture2, float alpha)
		{
			byte[] array = null;
			if (texture1 != null)
			{
				int byteCount = ScriptingInterfaceOfIScene._utf8.GetByteCount(texture1);
				array = ((byteCount < 1024) ? CallbackStringBufferManager.StringBuffer0 : new byte[byteCount + 1]);
				ScriptingInterfaceOfIScene._utf8.GetBytes(texture1, 0, texture1.Length, array, 0);
				array[byteCount] = 0;
			}
			byte[] array2 = null;
			if (texture2 != null)
			{
				int byteCount2 = ScriptingInterfaceOfIScene._utf8.GetByteCount(texture2);
				array2 = ((byteCount2 < 1024) ? CallbackStringBufferManager.StringBuffer1 : new byte[byteCount2 + 1]);
				ScriptingInterfaceOfIScene._utf8.GetBytes(texture2, 0, texture2.Length, array2, 0);
				array2[byteCount2] = 0;
			}
			ScriptingInterfaceOfIScene.call_SetColorGradeBlendDelegate(scenePointer, array, array2, alpha);
		}

		// Token: 0x060004BF RID: 1215 RVA: 0x000171FC File Offset: 0x000153FC
		public void SetDLSSMode(UIntPtr scenePointer, bool mode)
		{
			ScriptingInterfaceOfIScene.call_SetDLSSModeDelegate(scenePointer, mode);
		}

		// Token: 0x060004C0 RID: 1216 RVA: 0x0001720A File Offset: 0x0001540A
		public void SetDofFocus(UIntPtr scenePointer, float dofFocus)
		{
			ScriptingInterfaceOfIScene.call_SetDofFocusDelegate(scenePointer, dofFocus);
		}

		// Token: 0x060004C1 RID: 1217 RVA: 0x00017218 File Offset: 0x00015418
		public void SetDofMode(UIntPtr scenePointer, bool mode)
		{
			ScriptingInterfaceOfIScene.call_SetDofModeDelegate(scenePointer, mode);
		}

		// Token: 0x060004C2 RID: 1218 RVA: 0x00017226 File Offset: 0x00015426
		public void SetDofParams(UIntPtr scenePointer, float dofFocusStart, float dofFocusEnd, bool isVignetteOn)
		{
			ScriptingInterfaceOfIScene.call_SetDofParamsDelegate(scenePointer, dofFocusStart, dofFocusEnd, isVignetteOn);
		}

		// Token: 0x060004C3 RID: 1219 RVA: 0x00017237 File Offset: 0x00015437
		public void SetDoNotAddEntitiesToTickList(UIntPtr scenePointer, bool value)
		{
			ScriptingInterfaceOfIScene.call_SetDoNotAddEntitiesToTickListDelegate(scenePointer, value);
		}

		// Token: 0x060004C4 RID: 1220 RVA: 0x00017245 File Offset: 0x00015445
		public void SetDoNotWaitForLoadingStatesToRender(UIntPtr scenePointer, bool value)
		{
			ScriptingInterfaceOfIScene.call_SetDoNotWaitForLoadingStatesToRenderDelegate(scenePointer, value);
		}

		// Token: 0x060004C5 RID: 1221 RVA: 0x00017253 File Offset: 0x00015453
		public void SetDontLoadInvisibleEntities(UIntPtr scenePointer, bool value)
		{
			ScriptingInterfaceOfIScene.call_SetDontLoadInvisibleEntitiesDelegate(scenePointer, value);
		}

		// Token: 0x060004C6 RID: 1222 RVA: 0x00017261 File Offset: 0x00015461
		public void SetDrynessFactor(UIntPtr scenePointer, float drynessFactor)
		{
			ScriptingInterfaceOfIScene.call_SetDrynessFactorDelegate(scenePointer, drynessFactor);
		}

		// Token: 0x060004C7 RID: 1223 RVA: 0x0001726F File Offset: 0x0001546F
		public void SetDynamicShadowmapCascadesRadiusMultiplier(UIntPtr scenePointer, float extraRadius)
		{
			ScriptingInterfaceOfIScene.call_SetDynamicShadowmapCascadesRadiusMultiplierDelegate(scenePointer, extraRadius);
		}

		// Token: 0x060004C8 RID: 1224 RVA: 0x0001727D File Offset: 0x0001547D
		public void SetDynamicSnowTexture(UIntPtr scenePointer, UIntPtr texturePointer)
		{
			ScriptingInterfaceOfIScene.call_SetDynamicSnowTextureDelegate(scenePointer, texturePointer);
		}

		// Token: 0x060004C9 RID: 1225 RVA: 0x0001728B File Offset: 0x0001548B
		public void SetEnvironmentMultiplier(UIntPtr scenePointer, bool useMultiplier, float multiplier)
		{
			ScriptingInterfaceOfIScene.call_SetEnvironmentMultiplierDelegate(scenePointer, useMultiplier, multiplier);
		}

		// Token: 0x060004CA RID: 1226 RVA: 0x0001729A File Offset: 0x0001549A
		public void SetExternalInjectionTexture(UIntPtr scenePointer, UIntPtr texturePointer)
		{
			ScriptingInterfaceOfIScene.call_SetExternalInjectionTextureDelegate(scenePointer, texturePointer);
		}

		// Token: 0x060004CB RID: 1227 RVA: 0x000172A8 File Offset: 0x000154A8
		public void SetFetchCrcInfoOfScene(UIntPtr scenePointer, bool value)
		{
			ScriptingInterfaceOfIScene.call_SetFetchCrcInfoOfSceneDelegate(scenePointer, value);
		}

		// Token: 0x060004CC RID: 1228 RVA: 0x000172B8 File Offset: 0x000154B8
		public void SetFixedTickCallbackActive(Scene scene, bool isActive)
		{
			UIntPtr scene2 = ((scene != null) ? scene.Pointer : UIntPtr.Zero);
			ScriptingInterfaceOfIScene.call_SetFixedTickCallbackActiveDelegate(scene2, isActive);
		}

		// Token: 0x060004CD RID: 1229 RVA: 0x000172E8 File Offset: 0x000154E8
		public void SetFog(UIntPtr scenePointer, float fogDensity, ref Vec3 fogColor, float fogFalloff)
		{
			ScriptingInterfaceOfIScene.call_SetFogDelegate(scenePointer, fogDensity, ref fogColor, fogFalloff);
		}

		// Token: 0x060004CE RID: 1230 RVA: 0x000172F9 File Offset: 0x000154F9
		public void SetFogAdvanced(UIntPtr scenePointer, float fogFalloffOffset, float fogFalloffMinFog, float fogFalloffStartDist)
		{
			ScriptingInterfaceOfIScene.call_SetFogAdvancedDelegate(scenePointer, fogFalloffOffset, fogFalloffMinFog, fogFalloffStartDist);
		}

		// Token: 0x060004CF RID: 1231 RVA: 0x0001730A File Offset: 0x0001550A
		public void SetFogAmbientColor(UIntPtr scenePointer, ref Vec3 fogAmbientColor)
		{
			ScriptingInterfaceOfIScene.call_SetFogAmbientColorDelegate(scenePointer, ref fogAmbientColor);
		}

		// Token: 0x060004D0 RID: 1232 RVA: 0x00017318 File Offset: 0x00015518
		public void SetForcedSnow(UIntPtr scenePointer, bool value)
		{
			ScriptingInterfaceOfIScene.call_SetForcedSnowDelegate(scenePointer, value);
		}

		// Token: 0x060004D1 RID: 1233 RVA: 0x00017326 File Offset: 0x00015526
		public void SetGlobalWindStrengthVector(UIntPtr scenePointer, in Vec2 strengthVector)
		{
			ScriptingInterfaceOfIScene.call_SetGlobalWindStrengthVectorDelegate(scenePointer, strengthVector);
		}

		// Token: 0x060004D2 RID: 1234 RVA: 0x00017334 File Offset: 0x00015534
		public void SetGlobalWindVelocity(UIntPtr scenePointer, in Vec2 windVelocity)
		{
			ScriptingInterfaceOfIScene.call_SetGlobalWindVelocityDelegate(scenePointer, windVelocity);
		}

		// Token: 0x060004D3 RID: 1235 RVA: 0x00017342 File Offset: 0x00015542
		public void SetGrainAmount(UIntPtr scenePointer, float grainAmount)
		{
			ScriptingInterfaceOfIScene.call_SetGrainAmountDelegate(scenePointer, grainAmount);
		}

		// Token: 0x060004D4 RID: 1236 RVA: 0x00017350 File Offset: 0x00015550
		public void SetHexagonVignetteAlpha(UIntPtr scenePointer, float Alpha)
		{
			ScriptingInterfaceOfIScene.call_SetHexagonVignetteAlphaDelegate(scenePointer, Alpha);
		}

		// Token: 0x060004D5 RID: 1237 RVA: 0x0001735E File Offset: 0x0001555E
		public void SetHexagonVignetteColor(UIntPtr scenePointer, ref Vec3 p_hexagon_vignette_color)
		{
			ScriptingInterfaceOfIScene.call_SetHexagonVignetteColorDelegate(scenePointer, ref p_hexagon_vignette_color);
		}

		// Token: 0x060004D6 RID: 1238 RVA: 0x0001736C File Offset: 0x0001556C
		public void SetHumidity(UIntPtr scenePointer, float humidity)
		{
			ScriptingInterfaceOfIScene.call_SetHumidityDelegate(scenePointer, humidity);
		}

		// Token: 0x060004D7 RID: 1239 RVA: 0x0001737C File Offset: 0x0001557C
		public void SetLandscapeRainMaskData(UIntPtr scenePointer, byte[] data)
		{
			PinnedArrayData<byte> pinnedArrayData = new PinnedArrayData<byte>(data, false);
			IntPtr pointer = pinnedArrayData.Pointer;
			ManagedArray data2 = new ManagedArray(pointer, (data != null) ? data.Length : 0);
			ScriptingInterfaceOfIScene.call_SetLandscapeRainMaskDataDelegate(scenePointer, data2);
			pinnedArrayData.Dispose();
		}

		// Token: 0x060004D8 RID: 1240 RVA: 0x000173BE File Offset: 0x000155BE
		public void SetLensDistortion(UIntPtr scenePointer, float lensDistortion)
		{
			ScriptingInterfaceOfIScene.call_SetLensDistortionDelegate(scenePointer, lensDistortion);
		}

		// Token: 0x060004D9 RID: 1241 RVA: 0x000173CC File Offset: 0x000155CC
		public void SetLensFlareAberrationOffset(UIntPtr scenePointer, float lensFlareAberrationOffset)
		{
			ScriptingInterfaceOfIScene.call_SetLensFlareAberrationOffsetDelegate(scenePointer, lensFlareAberrationOffset);
		}

		// Token: 0x060004DA RID: 1242 RVA: 0x000173DA File Offset: 0x000155DA
		public void SetLensFlareAmount(UIntPtr scenePointer, float lensFlareAmount)
		{
			ScriptingInterfaceOfIScene.call_SetLensFlareAmountDelegate(scenePointer, lensFlareAmount);
		}

		// Token: 0x060004DB RID: 1243 RVA: 0x000173E8 File Offset: 0x000155E8
		public void SetLensFlareBlurSigma(UIntPtr scenePointer, float lensFlareBlurSigma)
		{
			ScriptingInterfaceOfIScene.call_SetLensFlareBlurSigmaDelegate(scenePointer, lensFlareBlurSigma);
		}

		// Token: 0x060004DC RID: 1244 RVA: 0x000173F6 File Offset: 0x000155F6
		public void SetLensFlareBlurSize(UIntPtr scenePointer, int lensFlareBlurSize)
		{
			ScriptingInterfaceOfIScene.call_SetLensFlareBlurSizeDelegate(scenePointer, lensFlareBlurSize);
		}

		// Token: 0x060004DD RID: 1245 RVA: 0x00017404 File Offset: 0x00015604
		public void SetLensFlareDiffractionWeight(UIntPtr scenePointer, float lensFlareDiffractionWeight)
		{
			ScriptingInterfaceOfIScene.call_SetLensFlareDiffractionWeightDelegate(scenePointer, lensFlareDiffractionWeight);
		}

		// Token: 0x060004DE RID: 1246 RVA: 0x00017412 File Offset: 0x00015612
		public void SetLensFlareDirtWeight(UIntPtr scenePointer, float lensFlareDirtWeight)
		{
			ScriptingInterfaceOfIScene.call_SetLensFlareDirtWeightDelegate(scenePointer, lensFlareDirtWeight);
		}

		// Token: 0x060004DF RID: 1247 RVA: 0x00017420 File Offset: 0x00015620
		public void SetLensFlareGhostSamples(UIntPtr scenePointer, int lensFlareGhostSamples)
		{
			ScriptingInterfaceOfIScene.call_SetLensFlareGhostSamplesDelegate(scenePointer, lensFlareGhostSamples);
		}

		// Token: 0x060004E0 RID: 1248 RVA: 0x0001742E File Offset: 0x0001562E
		public void SetLensFlareGhostWeight(UIntPtr scenePointer, float lensFlareGhostWeight)
		{
			ScriptingInterfaceOfIScene.call_SetLensFlareGhostWeightDelegate(scenePointer, lensFlareGhostWeight);
		}

		// Token: 0x060004E1 RID: 1249 RVA: 0x0001743C File Offset: 0x0001563C
		public void SetLensFlareHaloWeight(UIntPtr scenePointer, float lensFlareHaloWeight)
		{
			ScriptingInterfaceOfIScene.call_SetLensFlareHaloWeightDelegate(scenePointer, lensFlareHaloWeight);
		}

		// Token: 0x060004E2 RID: 1250 RVA: 0x0001744A File Offset: 0x0001564A
		public void SetLensFlareHaloWidth(UIntPtr scenePointer, float lensFlareHaloWidth)
		{
			ScriptingInterfaceOfIScene.call_SetLensFlareHaloWidthDelegate(scenePointer, lensFlareHaloWidth);
		}

		// Token: 0x060004E3 RID: 1251 RVA: 0x00017458 File Offset: 0x00015658
		public void SetLensFlareStrength(UIntPtr scenePointer, float lensFlareStrength)
		{
			ScriptingInterfaceOfIScene.call_SetLensFlareStrengthDelegate(scenePointer, lensFlareStrength);
		}

		// Token: 0x060004E4 RID: 1252 RVA: 0x00017466 File Offset: 0x00015666
		public void SetLensFlareThreshold(UIntPtr scenePointer, float lensFlareThreshold)
		{
			ScriptingInterfaceOfIScene.call_SetLensFlareThresholdDelegate(scenePointer, lensFlareThreshold);
		}

		// Token: 0x060004E5 RID: 1253 RVA: 0x00017474 File Offset: 0x00015674
		public void SetLightDiffuseColor(UIntPtr scenePointer, int lightIndex, Vec3 diffuseColor)
		{
			ScriptingInterfaceOfIScene.call_SetLightDiffuseColorDelegate(scenePointer, lightIndex, diffuseColor);
		}

		// Token: 0x060004E6 RID: 1254 RVA: 0x00017483 File Offset: 0x00015683
		public void SetLightDirection(UIntPtr scenePointer, int lightIndex, Vec3 direction)
		{
			ScriptingInterfaceOfIScene.call_SetLightDirectionDelegate(scenePointer, lightIndex, direction);
		}

		// Token: 0x060004E7 RID: 1255 RVA: 0x00017492 File Offset: 0x00015692
		public void SetLightPosition(UIntPtr scenePointer, int lightIndex, Vec3 position)
		{
			ScriptingInterfaceOfIScene.call_SetLightPositionDelegate(scenePointer, lightIndex, position);
		}

		// Token: 0x060004E8 RID: 1256 RVA: 0x000174A1 File Offset: 0x000156A1
		public void SetMaxExposure(UIntPtr scenePointer, float maxExposure)
		{
			ScriptingInterfaceOfIScene.call_SetMaxExposureDelegate(scenePointer, maxExposure);
		}

		// Token: 0x060004E9 RID: 1257 RVA: 0x000174AF File Offset: 0x000156AF
		public void SetMiddleGray(UIntPtr scenePointer, float middleGray)
		{
			ScriptingInterfaceOfIScene.call_SetMiddleGrayDelegate(scenePointer, middleGray);
		}

		// Token: 0x060004EA RID: 1258 RVA: 0x000174BD File Offset: 0x000156BD
		public void SetMieScatterFocus(UIntPtr scenePointer, float strength)
		{
			ScriptingInterfaceOfIScene.call_SetMieScatterFocusDelegate(scenePointer, strength);
		}

		// Token: 0x060004EB RID: 1259 RVA: 0x000174CB File Offset: 0x000156CB
		public void SetMieScatterStrength(UIntPtr scenePointer, float strength)
		{
			ScriptingInterfaceOfIScene.call_SetMieScatterStrengthDelegate(scenePointer, strength);
		}

		// Token: 0x060004EC RID: 1260 RVA: 0x000174D9 File Offset: 0x000156D9
		public void SetMinExposure(UIntPtr scenePointer, float minExposure)
		{
			ScriptingInterfaceOfIScene.call_SetMinExposureDelegate(scenePointer, minExposure);
		}

		// Token: 0x060004ED RID: 1261 RVA: 0x000174E7 File Offset: 0x000156E7
		public void SetMotionBlurMode(UIntPtr scenePointer, bool mode)
		{
			ScriptingInterfaceOfIScene.call_SetMotionBlurModeDelegate(scenePointer, mode);
		}

		// Token: 0x060004EE RID: 1262 RVA: 0x000174F8 File Offset: 0x000156F8
		public void SetName(UIntPtr scenePointer, string name)
		{
			byte[] array = null;
			if (name != null)
			{
				int byteCount = ScriptingInterfaceOfIScene._utf8.GetByteCount(name);
				array = ((byteCount < 1024) ? CallbackStringBufferManager.StringBuffer0 : new byte[byteCount + 1]);
				ScriptingInterfaceOfIScene._utf8.GetBytes(name, 0, name.Length, array, 0);
				array[byteCount] = 0;
			}
			ScriptingInterfaceOfIScene.call_SetNameDelegate(scenePointer, array);
		}

		// Token: 0x060004EF RID: 1263 RVA: 0x00017554 File Offset: 0x00015754
		public void SetNavMeshRegionMap(UIntPtr scenePointer, bool[] regionMap, int regionMapSize)
		{
			PinnedArrayData<bool> pinnedArrayData = new PinnedArrayData<bool>(regionMap, false);
			IntPtr pointer = pinnedArrayData.Pointer;
			ScriptingInterfaceOfIScene.call_SetNavMeshRegionMapDelegate(scenePointer, pointer, regionMapSize);
			pinnedArrayData.Dispose();
		}

		// Token: 0x060004F0 RID: 1264 RVA: 0x00017586 File Offset: 0x00015786
		public void SetOcclusionMode(UIntPtr scenePointer, bool mode)
		{
			ScriptingInterfaceOfIScene.call_SetOcclusionModeDelegate(scenePointer, mode);
		}

		// Token: 0x060004F1 RID: 1265 RVA: 0x00017594 File Offset: 0x00015794
		public void SetOnCollisionFilterCallbackActive(Scene scene, bool isActive)
		{
			UIntPtr scene2 = ((scene != null) ? scene.Pointer : UIntPtr.Zero);
			ScriptingInterfaceOfIScene.call_SetOnCollisionFilterCallbackActiveDelegate(scene2, isActive);
		}

		// Token: 0x060004F2 RID: 1266 RVA: 0x000175C4 File Offset: 0x000157C4
		public void SetOwnerThread(UIntPtr scenePointer)
		{
			ScriptingInterfaceOfIScene.call_SetOwnerThreadDelegate(scenePointer);
		}

		// Token: 0x060004F3 RID: 1267 RVA: 0x000175D1 File Offset: 0x000157D1
		public void SetPhotoAtmosphereViaTod(UIntPtr scenePointer, float tod, bool withStorm)
		{
			ScriptingInterfaceOfIScene.call_SetPhotoAtmosphereViaTodDelegate(scenePointer, tod, withStorm);
		}

		// Token: 0x060004F4 RID: 1268 RVA: 0x000175E0 File Offset: 0x000157E0
		public void SetPhotoModeFocus(Scene scene, float focusStart, float focusEnd, float focus, float exposure)
		{
			UIntPtr scene2 = ((scene != null) ? scene.Pointer : UIntPtr.Zero);
			ScriptingInterfaceOfIScene.call_SetPhotoModeFocusDelegate(scene2, focusStart, focusEnd, focus, exposure);
		}

		// Token: 0x060004F5 RID: 1269 RVA: 0x00017618 File Offset: 0x00015818
		public void SetPhotoModeFov(Scene scene, float verticalFov)
		{
			UIntPtr scene2 = ((scene != null) ? scene.Pointer : UIntPtr.Zero);
			ScriptingInterfaceOfIScene.call_SetPhotoModeFovDelegate(scene2, verticalFov);
		}

		// Token: 0x060004F6 RID: 1270 RVA: 0x00017648 File Offset: 0x00015848
		public void SetPhotoModeOn(Scene scene, bool on)
		{
			UIntPtr scene2 = ((scene != null) ? scene.Pointer : UIntPtr.Zero);
			ScriptingInterfaceOfIScene.call_SetPhotoModeOnDelegate(scene2, on);
		}

		// Token: 0x060004F7 RID: 1271 RVA: 0x00017678 File Offset: 0x00015878
		public void SetPhotoModeOrbit(Scene scene, bool orbit)
		{
			UIntPtr scene2 = ((scene != null) ? scene.Pointer : UIntPtr.Zero);
			ScriptingInterfaceOfIScene.call_SetPhotoModeOrbitDelegate(scene2, orbit);
		}

		// Token: 0x060004F8 RID: 1272 RVA: 0x000176A8 File Offset: 0x000158A8
		public void SetPhotoModeRoll(Scene scene, float roll)
		{
			UIntPtr scene2 = ((scene != null) ? scene.Pointer : UIntPtr.Zero);
			ScriptingInterfaceOfIScene.call_SetPhotoModeRollDelegate(scene2, roll);
		}

		// Token: 0x060004F9 RID: 1273 RVA: 0x000176D8 File Offset: 0x000158D8
		public void SetPhotoModeVignette(Scene scene, bool vignetteOn)
		{
			UIntPtr scene2 = ((scene != null) ? scene.Pointer : UIntPtr.Zero);
			ScriptingInterfaceOfIScene.call_SetPhotoModeVignetteDelegate(scene2, vignetteOn);
		}

		// Token: 0x060004FA RID: 1274 RVA: 0x00017708 File Offset: 0x00015908
		public void SetPlaySoundEventsAfterReadyToRender(UIntPtr ptr, bool value)
		{
			ScriptingInterfaceOfIScene.call_SetPlaySoundEventsAfterReadyToRenderDelegate(ptr, value);
		}

		// Token: 0x060004FB RID: 1275 RVA: 0x00017716 File Offset: 0x00015916
		public void SetRainDensity(UIntPtr scenePointer, float density)
		{
			ScriptingInterfaceOfIScene.call_SetRainDensityDelegate(scenePointer, density);
		}

		// Token: 0x060004FC RID: 1276 RVA: 0x00017724 File Offset: 0x00015924
		public void SetSceneColorGrade(Scene scene, string textureName)
		{
			UIntPtr scene2 = ((scene != null) ? scene.Pointer : UIntPtr.Zero);
			byte[] array = null;
			if (textureName != null)
			{
				int byteCount = ScriptingInterfaceOfIScene._utf8.GetByteCount(textureName);
				array = ((byteCount < 1024) ? CallbackStringBufferManager.StringBuffer0 : new byte[byteCount + 1]);
				ScriptingInterfaceOfIScene._utf8.GetBytes(textureName, 0, textureName.Length, array, 0);
				array[byteCount] = 0;
			}
			ScriptingInterfaceOfIScene.call_SetSceneColorGradeDelegate(scene2, array);
		}

		// Token: 0x060004FD RID: 1277 RVA: 0x00017798 File Offset: 0x00015998
		public void SetSceneColorGradeIndex(Scene scene, int index)
		{
			UIntPtr scene2 = ((scene != null) ? scene.Pointer : UIntPtr.Zero);
			ScriptingInterfaceOfIScene.call_SetSceneColorGradeIndexDelegate(scene2, index);
		}

		// Token: 0x060004FE RID: 1278 RVA: 0x000177C8 File Offset: 0x000159C8
		public int SetSceneFilterIndex(Scene scene, int index)
		{
			UIntPtr scene2 = ((scene != null) ? scene.Pointer : UIntPtr.Zero);
			return ScriptingInterfaceOfIScene.call_SetSceneFilterIndexDelegate(scene2, index);
		}

		// Token: 0x060004FF RID: 1279 RVA: 0x000177F8 File Offset: 0x000159F8
		public void SetShadow(UIntPtr scenePointer, bool shadowEnabled)
		{
			ScriptingInterfaceOfIScene.call_SetShadowDelegate(scenePointer, shadowEnabled);
		}

		// Token: 0x06000500 RID: 1280 RVA: 0x00017806 File Offset: 0x00015A06
		public void SetSkyBrightness(UIntPtr scenePointer, float brightness)
		{
			ScriptingInterfaceOfIScene.call_SetSkyBrightnessDelegate(scenePointer, brightness);
		}

		// Token: 0x06000501 RID: 1281 RVA: 0x00017814 File Offset: 0x00015A14
		public void SetSkyRotation(UIntPtr scenePointer, float rotation)
		{
			ScriptingInterfaceOfIScene.call_SetSkyRotationDelegate(scenePointer, rotation);
		}

		// Token: 0x06000502 RID: 1282 RVA: 0x00017822 File Offset: 0x00015A22
		public void SetSnowDensity(UIntPtr scenePointer, float density)
		{
			ScriptingInterfaceOfIScene.call_SetSnowDensityDelegate(scenePointer, density);
		}

		// Token: 0x06000503 RID: 1283 RVA: 0x00017830 File Offset: 0x00015A30
		public void SetStreakAmount(UIntPtr scenePointer, float streakAmount)
		{
			ScriptingInterfaceOfIScene.call_SetStreakAmountDelegate(scenePointer, streakAmount);
		}

		// Token: 0x06000504 RID: 1284 RVA: 0x0001783E File Offset: 0x00015A3E
		public void SetStreakIntensity(UIntPtr scenePointer, float stretchAmount)
		{
			ScriptingInterfaceOfIScene.call_SetStreakIntensityDelegate(scenePointer, stretchAmount);
		}

		// Token: 0x06000505 RID: 1285 RVA: 0x0001784C File Offset: 0x00015A4C
		public void SetStreakStrength(UIntPtr scenePointer, float strengthAmount)
		{
			ScriptingInterfaceOfIScene.call_SetStreakStrengthDelegate(scenePointer, strengthAmount);
		}

		// Token: 0x06000506 RID: 1286 RVA: 0x0001785A File Offset: 0x00015A5A
		public void SetStreakStretch(UIntPtr scenePointer, float stretchAmount)
		{
			ScriptingInterfaceOfIScene.call_SetStreakStretchDelegate(scenePointer, stretchAmount);
		}

		// Token: 0x06000507 RID: 1287 RVA: 0x00017868 File Offset: 0x00015A68
		public void SetStreakThreshold(UIntPtr scenePointer, float streakThreshold)
		{
			ScriptingInterfaceOfIScene.call_SetStreakThresholdDelegate(scenePointer, streakThreshold);
		}

		// Token: 0x06000508 RID: 1288 RVA: 0x00017876 File Offset: 0x00015A76
		public void SetStreakTint(UIntPtr scenePointer, ref Vec3 p_streak_tint_color)
		{
			ScriptingInterfaceOfIScene.call_SetStreakTintDelegate(scenePointer, ref p_streak_tint_color);
		}

		// Token: 0x06000509 RID: 1289 RVA: 0x00017884 File Offset: 0x00015A84
		public void SetSun(UIntPtr scenePointer, Vec3 color, float altitude, float angle, float intensity)
		{
			ScriptingInterfaceOfIScene.call_SetSunDelegate(scenePointer, color, altitude, angle, intensity);
		}

		// Token: 0x0600050A RID: 1290 RVA: 0x00017897 File Offset: 0x00015A97
		public void SetSunAngleAltitude(UIntPtr scenePointer, float angle, float altitude)
		{
			ScriptingInterfaceOfIScene.call_SetSunAngleAltitudeDelegate(scenePointer, angle, altitude);
		}

		// Token: 0x0600050B RID: 1291 RVA: 0x000178A6 File Offset: 0x00015AA6
		public void SetSunDirection(UIntPtr scenePointer, Vec3 direction)
		{
			ScriptingInterfaceOfIScene.call_SetSunDirectionDelegate(scenePointer, direction);
		}

		// Token: 0x0600050C RID: 1292 RVA: 0x000178B4 File Offset: 0x00015AB4
		public void SetSunLight(UIntPtr scenePointer, Vec3 color, Vec3 direction)
		{
			ScriptingInterfaceOfIScene.call_SetSunLightDelegate(scenePointer, color, direction);
		}

		// Token: 0x0600050D RID: 1293 RVA: 0x000178C3 File Offset: 0x00015AC3
		public void SetSunshaftMode(UIntPtr scenePointer, bool mode)
		{
			ScriptingInterfaceOfIScene.call_SetSunshaftModeDelegate(scenePointer, mode);
		}

		// Token: 0x0600050E RID: 1294 RVA: 0x000178D1 File Offset: 0x00015AD1
		public void SetSunShaftStrength(UIntPtr scenePointer, float strength)
		{
			ScriptingInterfaceOfIScene.call_SetSunShaftStrengthDelegate(scenePointer, strength);
		}

		// Token: 0x0600050F RID: 1295 RVA: 0x000178DF File Offset: 0x00015ADF
		public void SetSunSize(UIntPtr scenePointer, float size)
		{
			ScriptingInterfaceOfIScene.call_SetSunSizeDelegate(scenePointer, size);
		}

		// Token: 0x06000510 RID: 1296 RVA: 0x000178ED File Offset: 0x00015AED
		public void SetTargetExposure(UIntPtr scenePointer, float targetExposure)
		{
			ScriptingInterfaceOfIScene.call_SetTargetExposureDelegate(scenePointer, targetExposure);
		}

		// Token: 0x06000511 RID: 1297 RVA: 0x000178FB File Offset: 0x00015AFB
		public void SetTemperature(UIntPtr scenePointer, float temperature)
		{
			ScriptingInterfaceOfIScene.call_SetTemperatureDelegate(scenePointer, temperature);
		}

		// Token: 0x06000512 RID: 1298 RVA: 0x00017909 File Offset: 0x00015B09
		public void SetTerrainDynamicParams(UIntPtr scenePointer, Vec3 dynamic_params)
		{
			ScriptingInterfaceOfIScene.call_SetTerrainDynamicParamsDelegate(scenePointer, dynamic_params);
		}

		// Token: 0x06000513 RID: 1299 RVA: 0x00017917 File Offset: 0x00015B17
		public void SetTimeOfDay(UIntPtr scenePointer, float value)
		{
			ScriptingInterfaceOfIScene.call_SetTimeOfDayDelegate(scenePointer, value);
		}

		// Token: 0x06000514 RID: 1300 RVA: 0x00017925 File Offset: 0x00015B25
		public void SetTimeSpeed(UIntPtr scenePointer, float value)
		{
			ScriptingInterfaceOfIScene.call_SetTimeSpeedDelegate(scenePointer, value);
		}

		// Token: 0x06000515 RID: 1301 RVA: 0x00017933 File Offset: 0x00015B33
		public void SetUpgradeLevel(UIntPtr scenePointer, int level)
		{
			ScriptingInterfaceOfIScene.call_SetUpgradeLevelDelegate(scenePointer, level);
		}

		// Token: 0x06000516 RID: 1302 RVA: 0x00017944 File Offset: 0x00015B44
		public void SetUpgradeLevelVisibility(UIntPtr scenePointer, string concatLevels)
		{
			byte[] array = null;
			if (concatLevels != null)
			{
				int byteCount = ScriptingInterfaceOfIScene._utf8.GetByteCount(concatLevels);
				array = ((byteCount < 1024) ? CallbackStringBufferManager.StringBuffer0 : new byte[byteCount + 1]);
				ScriptingInterfaceOfIScene._utf8.GetBytes(concatLevels, 0, concatLevels.Length, array, 0);
				array[byteCount] = 0;
			}
			ScriptingInterfaceOfIScene.call_SetUpgradeLevelVisibilityDelegate(scenePointer, array);
		}

		// Token: 0x06000517 RID: 1303 RVA: 0x0001799F File Offset: 0x00015B9F
		public void SetUpgradeLevelVisibilityWithMask(UIntPtr scenePointer, uint mask)
		{
			ScriptingInterfaceOfIScene.call_SetUpgradeLevelVisibilityWithMaskDelegate(scenePointer, mask);
		}

		// Token: 0x06000518 RID: 1304 RVA: 0x000179AD File Offset: 0x00015BAD
		public void SetUseConstantTime(UIntPtr ptr, bool value)
		{
			ScriptingInterfaceOfIScene.call_SetUseConstantTimeDelegate(ptr, value);
		}

		// Token: 0x06000519 RID: 1305 RVA: 0x000179BB File Offset: 0x00015BBB
		public void SetUsesDeleteLaterSystem(UIntPtr scenePointer, bool value)
		{
			ScriptingInterfaceOfIScene.call_SetUsesDeleteLaterSystemDelegate(scenePointer, value);
		}

		// Token: 0x0600051A RID: 1306 RVA: 0x000179C9 File Offset: 0x00015BC9
		public void SetVignetteInnerRadius(UIntPtr scenePointer, float vignetteInnerRadius)
		{
			ScriptingInterfaceOfIScene.call_SetVignetteInnerRadiusDelegate(scenePointer, vignetteInnerRadius);
		}

		// Token: 0x0600051B RID: 1307 RVA: 0x000179D7 File Offset: 0x00015BD7
		public void SetVignetteOpacity(UIntPtr scenePointer, float vignetteOpacity)
		{
			ScriptingInterfaceOfIScene.call_SetVignetteOpacityDelegate(scenePointer, vignetteOpacity);
		}

		// Token: 0x0600051C RID: 1308 RVA: 0x000179E5 File Offset: 0x00015BE5
		public void SetVignetteOuterRadius(UIntPtr scenePointer, float vignetteOuterRadius)
		{
			ScriptingInterfaceOfIScene.call_SetVignetteOuterRadiusDelegate(scenePointer, vignetteOuterRadius);
		}

		// Token: 0x0600051D RID: 1309 RVA: 0x000179F4 File Offset: 0x00015BF4
		public void SetWaterStrength(Scene scene, float newWaterStrength)
		{
			UIntPtr scene2 = ((scene != null) ? scene.Pointer : UIntPtr.Zero);
			ScriptingInterfaceOfIScene.call_SetWaterStrengthDelegate(scene2, newWaterStrength);
		}

		// Token: 0x0600051E RID: 1310 RVA: 0x00017A24 File Offset: 0x00015C24
		public void SetWaterWakeCameraOffset(UIntPtr scenePointer, float cameraOffset)
		{
			ScriptingInterfaceOfIScene.call_SetWaterWakeCameraOffsetDelegate(scenePointer, cameraOffset);
		}

		// Token: 0x0600051F RID: 1311 RVA: 0x00017A32 File Offset: 0x00015C32
		public void SetWaterWakeWorldSize(UIntPtr scenePointer, float worldSize, float eraseFactor)
		{
			ScriptingInterfaceOfIScene.call_SetWaterWakeWorldSizeDelegate(scenePointer, worldSize, eraseFactor);
		}

		// Token: 0x06000520 RID: 1312 RVA: 0x00017A41 File Offset: 0x00015C41
		public void SetWinterTimeFactor(UIntPtr scenePointer, float winterTimeFactor)
		{
			ScriptingInterfaceOfIScene.call_SetWinterTimeFactorDelegate(scenePointer, winterTimeFactor);
		}

		// Token: 0x06000521 RID: 1313 RVA: 0x00017A4F File Offset: 0x00015C4F
		public void StallLoadingRenderingsUntilFurtherNotice(UIntPtr scenePointer)
		{
			ScriptingInterfaceOfIScene.call_StallLoadingRenderingsUntilFurtherNoticeDelegate(scenePointer);
		}

		// Token: 0x06000522 RID: 1314 RVA: 0x00017A5C File Offset: 0x00015C5C
		public bool SwapFaceConnectionsWithId(UIntPtr scenePointer, int hubFaceGroupID, int toBeSeparatedFaceGroupId, int toBeMergedFaceGroupId, bool canFail)
		{
			return ScriptingInterfaceOfIScene.call_SwapFaceConnectionsWithIdDelegate(scenePointer, hubFaceGroupID, toBeSeparatedFaceGroupId, toBeMergedFaceGroupId, canFail);
		}

		// Token: 0x06000523 RID: 1315 RVA: 0x00017A70 File Offset: 0x00015C70
		public string TakePhotoModePicture(Scene scene, bool saveAmbientOcclusionPass, bool saveObjectIdPass, bool saveShadowPass)
		{
			UIntPtr scene2 = ((scene != null) ? scene.Pointer : UIntPtr.Zero);
			if (ScriptingInterfaceOfIScene.call_TakePhotoModePictureDelegate(scene2, saveAmbientOcclusionPass, saveObjectIdPass, saveShadowPass) != 1)
			{
				return null;
			}
			return Managed.ReturnValueFromEngine;
		}

		// Token: 0x06000524 RID: 1316 RVA: 0x00017AAD File Offset: 0x00015CAD
		public void Tick(UIntPtr scenePointer, float deltaTime)
		{
			ScriptingInterfaceOfIScene.call_TickDelegate(scenePointer, deltaTime);
		}

		// Token: 0x06000525 RID: 1317 RVA: 0x00017ABB File Offset: 0x00015CBB
		public void TickWake(UIntPtr scenePointer, float dt)
		{
			ScriptingInterfaceOfIScene.call_TickWakeDelegate(scenePointer, dt);
		}

		// Token: 0x06000526 RID: 1318 RVA: 0x00017AC9 File Offset: 0x00015CC9
		public void WaitWaterRendererCPUSimulation(UIntPtr scenePointer)
		{
			ScriptingInterfaceOfIScene.call_WaitWaterRendererCPUSimulationDelegate(scenePointer);
		}

		// Token: 0x06000527 RID: 1319 RVA: 0x00017AD6 File Offset: 0x00015CD6
		public void WorldPositionComputeNearestNavMesh(ref WorldPosition position)
		{
			ScriptingInterfaceOfIScene.call_WorldPositionComputeNearestNavMeshDelegate(ref position);
		}

		// Token: 0x06000528 RID: 1320 RVA: 0x00017AE3 File Offset: 0x00015CE3
		public void WorldPositionValidateZ(ref WorldPosition position, int minimumValidityState)
		{
			ScriptingInterfaceOfIScene.call_WorldPositionValidateZDelegate(ref position, minimumValidityState);
		}

		// Token: 0x0600052B RID: 1323 RVA: 0x00017B05 File Offset: 0x00015D05
		bool IScene.HasNavmeshFaceUnsharedEdges(UIntPtr scenePointer, in PathFaceRecord faceRecord)
		{
			return this.HasNavmeshFaceUnsharedEdges(scenePointer, faceRecord);
		}

		// Token: 0x0600052C RID: 1324 RVA: 0x00017B0F File Offset: 0x00015D0F
		Vec3 IScene.GetWaterSpeedAtPosition(UIntPtr scenePointer, in Vec2 position, bool doChoppinessCorrection)
		{
			return this.GetWaterSpeedAtPosition(scenePointer, position, doChoppinessCorrection);
		}

		// Token: 0x0600052D RID: 1325 RVA: 0x00017B1A File Offset: 0x00015D1A
		void IScene.GetBulkWaterLevelAtVolumes(UIntPtr scene, UIntPtr volumeQueryDataArray, int volumeCount, in MatrixFrame entityFrame)
		{
			this.GetBulkWaterLevelAtVolumes(scene, volumeQueryDataArray, volumeCount, entityFrame);
		}

		// Token: 0x0600052E RID: 1326 RVA: 0x00017B27 File Offset: 0x00015D27
		UIntPtr IScene.RegisterShipVisualToWaterRenderer(UIntPtr scenePointer, UIntPtr entityPointer, in Vec3 bb)
		{
			return this.RegisterShipVisualToWaterRenderer(scenePointer, entityPointer, bb);
		}

		// Token: 0x0600052F RID: 1327 RVA: 0x00017B32 File Offset: 0x00015D32
		void IScene.SetGlobalWindStrengthVector(UIntPtr scenePointer, in Vec2 strengthVector)
		{
			this.SetGlobalWindStrengthVector(scenePointer, strengthVector);
		}

		// Token: 0x06000530 RID: 1328 RVA: 0x00017B3C File Offset: 0x00015D3C
		void IScene.SetGlobalWindVelocity(UIntPtr scenePointer, in Vec2 windVelocity)
		{
			this.SetGlobalWindVelocity(scenePointer, windVelocity);
		}

		// Token: 0x06000531 RID: 1329 RVA: 0x00017B46 File Offset: 0x00015D46
		bool IScene.RayCastExcludingTwoEntities(BodyFlags flags, UIntPtr scenePointer, in Ray ray, UIntPtr entity1, UIntPtr entity2)
		{
			return this.RayCastExcludingTwoEntities(flags, scenePointer, ray, entity1, entity2);
		}

		// Token: 0x06000532 RID: 1330 RVA: 0x00017B58 File Offset: 0x00015D58
		bool IScene.RayCastForClosestEntityOrTerrain(UIntPtr scenePointer, in Vec3 sourcePoint, in Vec3 targetPoint, float rayThickness, ref float collisionDistance, ref Vec3 closestPoint, ref UIntPtr entityIndex, BodyFlags bodyExcludeFlags, bool isFixedWorld)
		{
			return this.RayCastForClosestEntityOrTerrain(scenePointer, sourcePoint, targetPoint, rayThickness, ref collisionDistance, ref closestPoint, ref entityIndex, bodyExcludeFlags, isFixedWorld);
		}

		// Token: 0x06000533 RID: 1331 RVA: 0x00017B7C File Offset: 0x00015D7C
		bool IScene.FocusRayCastForFixedPhysics(UIntPtr scenePointer, in Vec3 sourcePoint, in Vec3 targetPoint, float rayThickness, ref float collisionDistance, ref Vec3 closestPoint, ref UIntPtr entityIndex, BodyFlags bodyExcludeFlags, bool isFixedWorld)
		{
			return this.FocusRayCastForFixedPhysics(scenePointer, sourcePoint, targetPoint, rayThickness, ref collisionDistance, ref closestPoint, ref entityIndex, bodyExcludeFlags, isFixedWorld);
		}

		// Token: 0x06000534 RID: 1332 RVA: 0x00017BA0 File Offset: 0x00015DA0
		bool IScene.RayCastForRamming(UIntPtr scenePointer, in Vec3 sourcePoint, in Vec3 targetPoint, float rayThickness, ref float collisionDistance, ref Vec3 intersectionPoint, ref UIntPtr intersectedEntityIndex, BodyFlags bodyExcludeFlags, BodyFlags bodyIncludeFlags, UIntPtr ignoredEntityPointer)
		{
			return this.RayCastForRamming(scenePointer, sourcePoint, targetPoint, rayThickness, ref collisionDistance, ref intersectionPoint, ref intersectedEntityIndex, bodyExcludeFlags, bodyIncludeFlags, ignoredEntityPointer);
		}

		// Token: 0x06000535 RID: 1333 RVA: 0x00017BC4 File Offset: 0x00015DC4
		bool IScene.RayCastForClosestEntityOrTerrainIgnoreEntity(UIntPtr scenePointer, in Vec3 sourcePoint, in Vec3 targetPoint, float rayThickness, ref float collisionDistance, ref Vec3 closestPoint, ref UIntPtr entityIndex, BodyFlags bodyExcludeFlags, UIntPtr ignoredEntityPointer)
		{
			return this.RayCastForClosestEntityOrTerrainIgnoreEntity(scenePointer, sourcePoint, targetPoint, rayThickness, ref collisionDistance, ref closestPoint, ref entityIndex, bodyExcludeFlags, ignoredEntityPointer);
		}

		// Token: 0x06000536 RID: 1334 RVA: 0x00017BE8 File Offset: 0x00015DE8
		bool IScene.BoxCastOnlyForCamera(UIntPtr scenePointer, Vec3[] boxPoints, in Vec3 centerPoint, in Vec3 dir, float distance, UIntPtr ignoredEntityPointer, ref float collisionDistance, ref Vec3 closestPoint, ref UIntPtr entityPointer, BodyFlags bodyExcludeFlags)
		{
			return this.BoxCastOnlyForCamera(scenePointer, boxPoints, centerPoint, dir, distance, ignoredEntityPointer, ref collisionDistance, ref closestPoint, ref entityPointer, bodyExcludeFlags);
		}

		// Token: 0x06000537 RID: 1335 RVA: 0x00017C0C File Offset: 0x00015E0C
		UIntPtr IScene.GetNavigationMeshForPosition(UIntPtr scenePointer, in Vec3 position, ref int faceGroupId, float heightDifferenceLimit, bool excludeDynamicNavigationMeshes)
		{
			return this.GetNavigationMeshForPosition(scenePointer, position, ref faceGroupId, heightDifferenceLimit, excludeDynamicNavigationMeshes);
		}

		// Token: 0x06000538 RID: 1336 RVA: 0x00017C1B File Offset: 0x00015E1B
		UIntPtr IScene.GetNearestNavigationMeshForPosition(UIntPtr scenePointer, in Vec3 position, float heightDifferenceLimit, bool excludeDynamicNavigationMeshes)
		{
			return this.GetNearestNavigationMeshForPosition(scenePointer, position, heightDifferenceLimit, excludeDynamicNavigationMeshes);
		}

		// Token: 0x06000539 RID: 1337 RVA: 0x00017C28 File Offset: 0x00015E28
		Vec2 IScene.GetLastPositionOnNavMeshFaceForPointAndDirection(UIntPtr scenePointer, in PathFaceRecord record, Vec2 position, Vec2 direction)
		{
			return this.GetLastPositionOnNavMeshFaceForPointAndDirection(scenePointer, record, position, direction);
		}

		// Token: 0x04000365 RID: 869
		private static readonly Encoding _utf8 = Encoding.UTF8;

		// Token: 0x04000366 RID: 870
		public static ScriptingInterfaceOfIScene.AddAlwaysRenderedSkeletonDelegate call_AddAlwaysRenderedSkeletonDelegate;

		// Token: 0x04000367 RID: 871
		public static ScriptingInterfaceOfIScene.AddDecalInstanceDelegate call_AddDecalInstanceDelegate;

		// Token: 0x04000368 RID: 872
		public static ScriptingInterfaceOfIScene.AddDirectionalLightDelegate call_AddDirectionalLightDelegate;

		// Token: 0x04000369 RID: 873
		public static ScriptingInterfaceOfIScene.AddEntityWithMeshDelegate call_AddEntityWithMeshDelegate;

		// Token: 0x0400036A RID: 874
		public static ScriptingInterfaceOfIScene.AddEntityWithMultiMeshDelegate call_AddEntityWithMultiMeshDelegate;

		// Token: 0x0400036B RID: 875
		public static ScriptingInterfaceOfIScene.AddItemEntityDelegate call_AddItemEntityDelegate;

		// Token: 0x0400036C RID: 876
		public static ScriptingInterfaceOfIScene.AddPathDelegate call_AddPathDelegate;

		// Token: 0x0400036D RID: 877
		public static ScriptingInterfaceOfIScene.AddPathPointDelegate call_AddPathPointDelegate;

		// Token: 0x0400036E RID: 878
		public static ScriptingInterfaceOfIScene.AddPointLightDelegate call_AddPointLightDelegate;

		// Token: 0x0400036F RID: 879
		public static ScriptingInterfaceOfIScene.AddWaterWakeWithCapsuleDelegate call_AddWaterWakeWithCapsuleDelegate;

		// Token: 0x04000370 RID: 880
		public static ScriptingInterfaceOfIScene.AttachEntityDelegate call_AttachEntityDelegate;

		// Token: 0x04000371 RID: 881
		public static ScriptingInterfaceOfIScene.BoxCastDelegate call_BoxCastDelegate;

		// Token: 0x04000372 RID: 882
		public static ScriptingInterfaceOfIScene.BoxCastOnlyForCameraDelegate call_BoxCastOnlyForCameraDelegate;

		// Token: 0x04000373 RID: 883
		public static ScriptingInterfaceOfIScene.CalculateEffectiveLightingDelegate call_CalculateEffectiveLightingDelegate;

		// Token: 0x04000374 RID: 884
		public static ScriptingInterfaceOfIScene.CheckPathEntitiesFrameChangedDelegate call_CheckPathEntitiesFrameChangedDelegate;

		// Token: 0x04000375 RID: 885
		public static ScriptingInterfaceOfIScene.CheckPointCanSeePointDelegate call_CheckPointCanSeePointDelegate;

		// Token: 0x04000376 RID: 886
		public static ScriptingInterfaceOfIScene.CheckResourcesDelegate call_CheckResourcesDelegate;

		// Token: 0x04000377 RID: 887
		public static ScriptingInterfaceOfIScene.ClearAllDelegate call_ClearAllDelegate;

		// Token: 0x04000378 RID: 888
		public static ScriptingInterfaceOfIScene.ClearCurrentFrameTickEntitiesDelegate call_ClearCurrentFrameTickEntitiesDelegate;

		// Token: 0x04000379 RID: 889
		public static ScriptingInterfaceOfIScene.ClearDecalsDelegate call_ClearDecalsDelegate;

		// Token: 0x0400037A RID: 890
		public static ScriptingInterfaceOfIScene.ClearNavMeshDelegate call_ClearNavMeshDelegate;

		// Token: 0x0400037B RID: 891
		public static ScriptingInterfaceOfIScene.ContainsTerrainDelegate call_ContainsTerrainDelegate;

		// Token: 0x0400037C RID: 892
		public static ScriptingInterfaceOfIScene.CreateBurstParticleDelegate call_CreateBurstParticleDelegate;

		// Token: 0x0400037D RID: 893
		public static ScriptingInterfaceOfIScene.CreateDynamicRainTextureDelegate call_CreateDynamicRainTextureDelegate;

		// Token: 0x0400037E RID: 894
		public static ScriptingInterfaceOfIScene.CreateNewSceneDelegate call_CreateNewSceneDelegate;

		// Token: 0x0400037F RID: 895
		public static ScriptingInterfaceOfIScene.CreatePathMeshDelegate call_CreatePathMeshDelegate;

		// Token: 0x04000380 RID: 896
		public static ScriptingInterfaceOfIScene.CreatePathMesh2Delegate call_CreatePathMesh2Delegate;

		// Token: 0x04000381 RID: 897
		public static ScriptingInterfaceOfIScene.DeletePathWithNameDelegate call_DeletePathWithNameDelegate;

		// Token: 0x04000382 RID: 898
		public static ScriptingInterfaceOfIScene.DeleteWaterWakeRendererDelegate call_DeleteWaterWakeRendererDelegate;

		// Token: 0x04000383 RID: 899
		public static ScriptingInterfaceOfIScene.DeRegisterShipVisualDelegate call_DeRegisterShipVisualDelegate;

		// Token: 0x04000384 RID: 900
		public static ScriptingInterfaceOfIScene.DisableStaticShadowsDelegate call_DisableStaticShadowsDelegate;

		// Token: 0x04000385 RID: 901
		public static ScriptingInterfaceOfIScene.DoesPathExistBetweenFacesDelegate call_DoesPathExistBetweenFacesDelegate;

		// Token: 0x04000386 RID: 902
		public static ScriptingInterfaceOfIScene.DoesPathExistBetweenPositionsDelegate call_DoesPathExistBetweenPositionsDelegate;

		// Token: 0x04000387 RID: 903
		public static ScriptingInterfaceOfIScene.EnableFixedTickDelegate call_EnableFixedTickDelegate;

		// Token: 0x04000388 RID: 904
		public static ScriptingInterfaceOfIScene.EnableInclusiveAsyncPhysxDelegate call_EnableInclusiveAsyncPhysxDelegate;

		// Token: 0x04000389 RID: 905
		public static ScriptingInterfaceOfIScene.EnsurePostfxSystemDelegate call_EnsurePostfxSystemDelegate;

		// Token: 0x0400038A RID: 906
		public static ScriptingInterfaceOfIScene.EnsureWaterWakeRendererDelegate call_EnsureWaterWakeRendererDelegate;

		// Token: 0x0400038B RID: 907
		public static ScriptingInterfaceOfIScene.FillEntityWithHardBorderPhysicsBarrierDelegate call_FillEntityWithHardBorderPhysicsBarrierDelegate;

		// Token: 0x0400038C RID: 908
		public static ScriptingInterfaceOfIScene.FillTerrainHeightDataDelegate call_FillTerrainHeightDataDelegate;

		// Token: 0x0400038D RID: 909
		public static ScriptingInterfaceOfIScene.FillTerrainPhysicsMaterialIndexDataDelegate call_FillTerrainPhysicsMaterialIndexDataDelegate;

		// Token: 0x0400038E RID: 910
		public static ScriptingInterfaceOfIScene.FindClosestExitPositionForPositionOnABoundaryFaceDelegate call_FindClosestExitPositionForPositionOnABoundaryFaceDelegate;

		// Token: 0x0400038F RID: 911
		public static ScriptingInterfaceOfIScene.FinishSceneSoundsDelegate call_FinishSceneSoundsDelegate;

		// Token: 0x04000390 RID: 912
		public static ScriptingInterfaceOfIScene.FocusRayCastForFixedPhysicsDelegate call_FocusRayCastForFixedPhysicsDelegate;

		// Token: 0x04000391 RID: 913
		public static ScriptingInterfaceOfIScene.ForceLoadResourcesDelegate call_ForceLoadResourcesDelegate;

		// Token: 0x04000392 RID: 914
		public static ScriptingInterfaceOfIScene.GenerateContactsWithCapsuleDelegate call_GenerateContactsWithCapsuleDelegate;

		// Token: 0x04000393 RID: 915
		public static ScriptingInterfaceOfIScene.GenerateContactsWithCapsuleAgainstEntityDelegate call_GenerateContactsWithCapsuleAgainstEntityDelegate;

		// Token: 0x04000394 RID: 916
		public static ScriptingInterfaceOfIScene.GetAllColorGradeNamesDelegate call_GetAllColorGradeNamesDelegate;

		// Token: 0x04000395 RID: 917
		public static ScriptingInterfaceOfIScene.GetAllEntitiesWithScriptComponentDelegate call_GetAllEntitiesWithScriptComponentDelegate;

		// Token: 0x04000396 RID: 918
		public static ScriptingInterfaceOfIScene.GetAllFilterNamesDelegate call_GetAllFilterNamesDelegate;

		// Token: 0x04000397 RID: 919
		public static ScriptingInterfaceOfIScene.GetAllNavmeshFaceRecordsDelegate call_GetAllNavmeshFaceRecordsDelegate;

		// Token: 0x04000398 RID: 920
		public static ScriptingInterfaceOfIScene.GetBoundingBoxDelegate call_GetBoundingBoxDelegate;

		// Token: 0x04000399 RID: 921
		public static ScriptingInterfaceOfIScene.GetBulkWaterLevelAtPositionsDelegate call_GetBulkWaterLevelAtPositionsDelegate;

		// Token: 0x0400039A RID: 922
		public static ScriptingInterfaceOfIScene.GetBulkWaterLevelAtVolumesDelegate call_GetBulkWaterLevelAtVolumesDelegate;

		// Token: 0x0400039B RID: 923
		public static ScriptingInterfaceOfIScene.GetCampaignEntityWithNameDelegate call_GetCampaignEntityWithNameDelegate;

		// Token: 0x0400039C RID: 924
		public static ScriptingInterfaceOfIScene.GetEnginePhysicsEnabledDelegate call_GetEnginePhysicsEnabledDelegate;

		// Token: 0x0400039D RID: 925
		public static ScriptingInterfaceOfIScene.GetEntitiesDelegate call_GetEntitiesDelegate;

		// Token: 0x0400039E RID: 926
		public static ScriptingInterfaceOfIScene.GetEntityCountDelegate call_GetEntityCountDelegate;

		// Token: 0x0400039F RID: 927
		public static ScriptingInterfaceOfIScene.GetEntityWithGuidDelegate call_GetEntityWithGuidDelegate;

		// Token: 0x040003A0 RID: 928
		public static ScriptingInterfaceOfIScene.GetFallDensityDelegate call_GetFallDensityDelegate;

		// Token: 0x040003A1 RID: 929
		public static ScriptingInterfaceOfIScene.GetFirstEntityWithNameDelegate call_GetFirstEntityWithNameDelegate;

		// Token: 0x040003A2 RID: 930
		public static ScriptingInterfaceOfIScene.GetFirstEntityWithScriptComponentDelegate call_GetFirstEntityWithScriptComponentDelegate;

		// Token: 0x040003A3 RID: 931
		public static ScriptingInterfaceOfIScene.GetFloraInstanceCountDelegate call_GetFloraInstanceCountDelegate;

		// Token: 0x040003A4 RID: 932
		public static ScriptingInterfaceOfIScene.GetFloraRendererTextureUsageDelegate call_GetFloraRendererTextureUsageDelegate;

		// Token: 0x040003A5 RID: 933
		public static ScriptingInterfaceOfIScene.GetFogDelegate call_GetFogDelegate;

		// Token: 0x040003A6 RID: 934
		public static ScriptingInterfaceOfIScene.GetGlobalWindStrengthVectorDelegate call_GetGlobalWindStrengthVectorDelegate;

		// Token: 0x040003A7 RID: 935
		public static ScriptingInterfaceOfIScene.GetGlobalWindVelocityDelegate call_GetGlobalWindVelocityDelegate;

		// Token: 0x040003A8 RID: 936
		public static ScriptingInterfaceOfIScene.GetGroundHeightAndBodyFlagsAtPositionDelegate call_GetGroundHeightAndBodyFlagsAtPositionDelegate;

		// Token: 0x040003A9 RID: 937
		public static ScriptingInterfaceOfIScene.GetGroundHeightAndNormalAtPositionDelegate call_GetGroundHeightAndNormalAtPositionDelegate;

		// Token: 0x040003AA RID: 938
		public static ScriptingInterfaceOfIScene.GetGroundHeightAtPositionDelegate call_GetGroundHeightAtPositionDelegate;

		// Token: 0x040003AB RID: 939
		public static ScriptingInterfaceOfIScene.GetHardBoundaryVertexDelegate call_GetHardBoundaryVertexDelegate;

		// Token: 0x040003AC RID: 940
		public static ScriptingInterfaceOfIScene.GetHardBoundaryVertexCountDelegate call_GetHardBoundaryVertexCountDelegate;

		// Token: 0x040003AD RID: 941
		public static ScriptingInterfaceOfIScene.GetHeightAtPointDelegate call_GetHeightAtPointDelegate;

		// Token: 0x040003AE RID: 942
		public static ScriptingInterfaceOfIScene.GetIdOfNavMeshFaceDelegate call_GetIdOfNavMeshFaceDelegate;

		// Token: 0x040003AF RID: 943
		public static ScriptingInterfaceOfIScene.GetInterpolationFactorForBodyWorldTransformSmoothingDelegate call_GetInterpolationFactorForBodyWorldTransformSmoothingDelegate;

		// Token: 0x040003B0 RID: 944
		public static ScriptingInterfaceOfIScene.GetLastFinalRenderCameraFrameDelegate call_GetLastFinalRenderCameraFrameDelegate;

		// Token: 0x040003B1 RID: 945
		public static ScriptingInterfaceOfIScene.GetLastFinalRenderCameraPositionDelegate call_GetLastFinalRenderCameraPositionDelegate;

		// Token: 0x040003B2 RID: 946
		public static ScriptingInterfaceOfIScene.GetLastPointOnNavigationMeshFromPositionToDestinationDelegate call_GetLastPointOnNavigationMeshFromPositionToDestinationDelegate;

		// Token: 0x040003B3 RID: 947
		public static ScriptingInterfaceOfIScene.GetLastPointOnNavigationMeshFromWorldPositionToDestinationDelegate call_GetLastPointOnNavigationMeshFromWorldPositionToDestinationDelegate;

		// Token: 0x040003B4 RID: 948
		public static ScriptingInterfaceOfIScene.GetLastPositionOnNavMeshFaceForPointAndDirectionDelegate call_GetLastPositionOnNavMeshFaceForPointAndDirectionDelegate;

		// Token: 0x040003B5 RID: 949
		public static ScriptingInterfaceOfIScene.GetLoadingStateNameDelegate call_GetLoadingStateNameDelegate;

		// Token: 0x040003B6 RID: 950
		public static ScriptingInterfaceOfIScene.GetModulePathDelegate call_GetModulePathDelegate;

		// Token: 0x040003B7 RID: 951
		public static ScriptingInterfaceOfIScene.GetNameDelegate call_GetNameDelegate;

		// Token: 0x040003B8 RID: 952
		public static ScriptingInterfaceOfIScene.GetNavigationMeshCRCDelegate call_GetNavigationMeshCRCDelegate;

		// Token: 0x040003B9 RID: 953
		public static ScriptingInterfaceOfIScene.GetNavigationMeshForPositionDelegate call_GetNavigationMeshForPositionDelegate;

		// Token: 0x040003BA RID: 954
		public static ScriptingInterfaceOfIScene.GetNavMeshFaceCenterPositionDelegate call_GetNavMeshFaceCenterPositionDelegate;

		// Token: 0x040003BB RID: 955
		public static ScriptingInterfaceOfIScene.GetNavMeshFaceCountDelegate call_GetNavMeshFaceCountDelegate;

		// Token: 0x040003BC RID: 956
		public static ScriptingInterfaceOfIScene.GetNavmeshFaceCountBetweenTwoIdsDelegate call_GetNavmeshFaceCountBetweenTwoIdsDelegate;

		// Token: 0x040003BD RID: 957
		public static ScriptingInterfaceOfIScene.GetNavMeshFaceFirstVertexZDelegate call_GetNavMeshFaceFirstVertexZDelegate;

		// Token: 0x040003BE RID: 958
		public static ScriptingInterfaceOfIScene.GetNavMeshFaceIndexDelegate call_GetNavMeshFaceIndexDelegate;

		// Token: 0x040003BF RID: 959
		public static ScriptingInterfaceOfIScene.GetNavMeshFaceIndex3Delegate call_GetNavMeshFaceIndex3Delegate;

		// Token: 0x040003C0 RID: 960
		public static ScriptingInterfaceOfIScene.GetNavmeshFaceRecordsBetweenTwoIdsDelegate call_GetNavmeshFaceRecordsBetweenTwoIdsDelegate;

		// Token: 0x040003C1 RID: 961
		public static ScriptingInterfaceOfIScene.GetNavMeshPathFaceRecordDelegate call_GetNavMeshPathFaceRecordDelegate;

		// Token: 0x040003C2 RID: 962
		public static ScriptingInterfaceOfIScene.GetNearestNavigationMeshForPositionDelegate call_GetNearestNavigationMeshForPositionDelegate;

		// Token: 0x040003C3 RID: 963
		public static ScriptingInterfaceOfIScene.GetNodeDataCountDelegate call_GetNodeDataCountDelegate;

		// Token: 0x040003C4 RID: 964
		public static ScriptingInterfaceOfIScene.GetNormalAtDelegate call_GetNormalAtDelegate;

		// Token: 0x040003C5 RID: 965
		public static ScriptingInterfaceOfIScene.GetNorthAngleDelegate call_GetNorthAngleDelegate;

		// Token: 0x040003C6 RID: 966
		public static ScriptingInterfaceOfIScene.GetNumberOfPathsWithNamePrefixDelegate call_GetNumberOfPathsWithNamePrefixDelegate;

		// Token: 0x040003C7 RID: 967
		public static ScriptingInterfaceOfIScene.GetPathBetweenAIFaceIndicesDelegate call_GetPathBetweenAIFaceIndicesDelegate;

		// Token: 0x040003C8 RID: 968
		public static ScriptingInterfaceOfIScene.GetPathBetweenAIFaceIndicesWithRegionSwitchCostDelegate call_GetPathBetweenAIFaceIndicesWithRegionSwitchCostDelegate;

		// Token: 0x040003C9 RID: 969
		public static ScriptingInterfaceOfIScene.GetPathBetweenAIFacePointersDelegate call_GetPathBetweenAIFacePointersDelegate;

		// Token: 0x040003CA RID: 970
		public static ScriptingInterfaceOfIScene.GetPathBetweenAIFacePointersWithRegionSwitchCostDelegate call_GetPathBetweenAIFacePointersWithRegionSwitchCostDelegate;

		// Token: 0x040003CB RID: 971
		public static ScriptingInterfaceOfIScene.GetPathDistanceBetweenAIFacesDelegate call_GetPathDistanceBetweenAIFacesDelegate;

		// Token: 0x040003CC RID: 972
		public static ScriptingInterfaceOfIScene.GetPathDistanceBetweenPositionsDelegate call_GetPathDistanceBetweenPositionsDelegate;

		// Token: 0x040003CD RID: 973
		public static ScriptingInterfaceOfIScene.GetPathFaceRecordFromNavMeshFacePointerDelegate call_GetPathFaceRecordFromNavMeshFacePointerDelegate;

		// Token: 0x040003CE RID: 974
		public static ScriptingInterfaceOfIScene.GetPathsWithNamePrefixDelegate call_GetPathsWithNamePrefixDelegate;

		// Token: 0x040003CF RID: 975
		public static ScriptingInterfaceOfIScene.GetPathWithNameDelegate call_GetPathWithNameDelegate;

		// Token: 0x040003D0 RID: 976
		public static ScriptingInterfaceOfIScene.GetPhotoModeFocusDelegate call_GetPhotoModeFocusDelegate;

		// Token: 0x040003D1 RID: 977
		public static ScriptingInterfaceOfIScene.GetPhotoModeFovDelegate call_GetPhotoModeFovDelegate;

		// Token: 0x040003D2 RID: 978
		public static ScriptingInterfaceOfIScene.GetPhotoModeOnDelegate call_GetPhotoModeOnDelegate;

		// Token: 0x040003D3 RID: 979
		public static ScriptingInterfaceOfIScene.GetPhotoModeOrbitDelegate call_GetPhotoModeOrbitDelegate;

		// Token: 0x040003D4 RID: 980
		public static ScriptingInterfaceOfIScene.GetPhotoModeRollDelegate call_GetPhotoModeRollDelegate;

		// Token: 0x040003D5 RID: 981
		public static ScriptingInterfaceOfIScene.GetPhysicsMinMaxDelegate call_GetPhysicsMinMaxDelegate;

		// Token: 0x040003D6 RID: 982
		public static ScriptingInterfaceOfIScene.GetRainDensityDelegate call_GetRainDensityDelegate;

		// Token: 0x040003D7 RID: 983
		public static ScriptingInterfaceOfIScene.GetRootEntitiesDelegate call_GetRootEntitiesDelegate;

		// Token: 0x040003D8 RID: 984
		public static ScriptingInterfaceOfIScene.GetRootEntityCountDelegate call_GetRootEntityCountDelegate;

		// Token: 0x040003D9 RID: 985
		public static ScriptingInterfaceOfIScene.GetSceneColorGradeIndexDelegate call_GetSceneColorGradeIndexDelegate;

		// Token: 0x040003DA RID: 986
		public static ScriptingInterfaceOfIScene.GetSceneFilterIndexDelegate call_GetSceneFilterIndexDelegate;

		// Token: 0x040003DB RID: 987
		public static ScriptingInterfaceOfIScene.GetSceneLimitsDelegate call_GetSceneLimitsDelegate;

		// Token: 0x040003DC RID: 988
		public static ScriptingInterfaceOfIScene.GetSceneXMLCRCDelegate call_GetSceneXMLCRCDelegate;

		// Token: 0x040003DD RID: 989
		public static ScriptingInterfaceOfIScene.GetScriptedEntityDelegate call_GetScriptedEntityDelegate;

		// Token: 0x040003DE RID: 990
		public static ScriptingInterfaceOfIScene.GetScriptedEntityCountDelegate call_GetScriptedEntityCountDelegate;

		// Token: 0x040003DF RID: 991
		public static ScriptingInterfaceOfIScene.GetSkyboxMeshDelegate call_GetSkyboxMeshDelegate;

		// Token: 0x040003E0 RID: 992
		public static ScriptingInterfaceOfIScene.GetSnowDensityDelegate call_GetSnowDensityDelegate;

		// Token: 0x040003E1 RID: 993
		public static ScriptingInterfaceOfIScene.GetSoftBoundaryVertexDelegate call_GetSoftBoundaryVertexDelegate;

		// Token: 0x040003E2 RID: 994
		public static ScriptingInterfaceOfIScene.GetSoftBoundaryVertexCountDelegate call_GetSoftBoundaryVertexCountDelegate;

		// Token: 0x040003E3 RID: 995
		public static ScriptingInterfaceOfIScene.GetSunDirectionDelegate call_GetSunDirectionDelegate;

		// Token: 0x040003E4 RID: 996
		public static ScriptingInterfaceOfIScene.GetTerrainDataDelegate call_GetTerrainDataDelegate;

		// Token: 0x040003E5 RID: 997
		public static ScriptingInterfaceOfIScene.GetTerrainHeightDelegate call_GetTerrainHeightDelegate;

		// Token: 0x040003E6 RID: 998
		public static ScriptingInterfaceOfIScene.GetTerrainHeightAndNormalDelegate call_GetTerrainHeightAndNormalDelegate;

		// Token: 0x040003E7 RID: 999
		public static ScriptingInterfaceOfIScene.GetTerrainMemoryUsageDelegate call_GetTerrainMemoryUsageDelegate;

		// Token: 0x040003E8 RID: 1000
		public static ScriptingInterfaceOfIScene.GetTerrainMinMaxHeightDelegate call_GetTerrainMinMaxHeightDelegate;

		// Token: 0x040003E9 RID: 1001
		public static ScriptingInterfaceOfIScene.GetTerrainNodeDataDelegate call_GetTerrainNodeDataDelegate;

		// Token: 0x040003EA RID: 1002
		public static ScriptingInterfaceOfIScene.GetTerrainPhysicsMaterialIndexAtLayerDelegate call_GetTerrainPhysicsMaterialIndexAtLayerDelegate;

		// Token: 0x040003EB RID: 1003
		public static ScriptingInterfaceOfIScene.GetTimeOfDayDelegate call_GetTimeOfDayDelegate;

		// Token: 0x040003EC RID: 1004
		public static ScriptingInterfaceOfIScene.GetTimeSpeedDelegate call_GetTimeSpeedDelegate;

		// Token: 0x040003ED RID: 1005
		public static ScriptingInterfaceOfIScene.GetUpgradeLevelCountDelegate call_GetUpgradeLevelCountDelegate;

		// Token: 0x040003EE RID: 1006
		public static ScriptingInterfaceOfIScene.GetUpgradeLevelMaskDelegate call_GetUpgradeLevelMaskDelegate;

		// Token: 0x040003EF RID: 1007
		public static ScriptingInterfaceOfIScene.GetUpgradeLevelMaskOfLevelNameDelegate call_GetUpgradeLevelMaskOfLevelNameDelegate;

		// Token: 0x040003F0 RID: 1008
		public static ScriptingInterfaceOfIScene.GetUpgradeLevelNameOfIndexDelegate call_GetUpgradeLevelNameOfIndexDelegate;

		// Token: 0x040003F1 RID: 1009
		public static ScriptingInterfaceOfIScene.GetWaterLevelDelegate call_GetWaterLevelDelegate;

		// Token: 0x040003F2 RID: 1010
		public static ScriptingInterfaceOfIScene.GetWaterLevelAtPositionDelegate call_GetWaterLevelAtPositionDelegate;

		// Token: 0x040003F3 RID: 1011
		public static ScriptingInterfaceOfIScene.GetWaterSpeedAtPositionDelegate call_GetWaterSpeedAtPositionDelegate;

		// Token: 0x040003F4 RID: 1012
		public static ScriptingInterfaceOfIScene.GetWaterStrengthDelegate call_GetWaterStrengthDelegate;

		// Token: 0x040003F5 RID: 1013
		public static ScriptingInterfaceOfIScene.GetWindFlowMapDataDelegate call_GetWindFlowMapDataDelegate;

		// Token: 0x040003F6 RID: 1014
		public static ScriptingInterfaceOfIScene.GetWinterTimeFactorDelegate call_GetWinterTimeFactorDelegate;

		// Token: 0x040003F7 RID: 1015
		public static ScriptingInterfaceOfIScene.HasDecalRendererDelegate call_HasDecalRendererDelegate;

		// Token: 0x040003F8 RID: 1016
		public static ScriptingInterfaceOfIScene.HasNavmeshFaceUnsharedEdgesDelegate call_HasNavmeshFaceUnsharedEdgesDelegate;

		// Token: 0x040003F9 RID: 1017
		public static ScriptingInterfaceOfIScene.HasTerrainHeightmapDelegate call_HasTerrainHeightmapDelegate;

		// Token: 0x040003FA RID: 1018
		public static ScriptingInterfaceOfIScene.InvalidateTerrainPhysicsMaterialsDelegate call_InvalidateTerrainPhysicsMaterialsDelegate;

		// Token: 0x040003FB RID: 1019
		public static ScriptingInterfaceOfIScene.IsAnyFaceWithIdDelegate call_IsAnyFaceWithIdDelegate;

		// Token: 0x040003FC RID: 1020
		public static ScriptingInterfaceOfIScene.IsAtmosphereIndoorDelegate call_IsAtmosphereIndoorDelegate;

		// Token: 0x040003FD RID: 1021
		public static ScriptingInterfaceOfIScene.IsDefaultEditorSceneDelegate call_IsDefaultEditorSceneDelegate;

		// Token: 0x040003FE RID: 1022
		public static ScriptingInterfaceOfIScene.IsEditorSceneDelegate call_IsEditorSceneDelegate;

		// Token: 0x040003FF RID: 1023
		public static ScriptingInterfaceOfIScene.IsLineToPointClearDelegate call_IsLineToPointClearDelegate;

		// Token: 0x04000400 RID: 1024
		public static ScriptingInterfaceOfIScene.IsLineToPointClear2Delegate call_IsLineToPointClear2Delegate;

		// Token: 0x04000401 RID: 1025
		public static ScriptingInterfaceOfIScene.IsLoadingFinishedDelegate call_IsLoadingFinishedDelegate;

		// Token: 0x04000402 RID: 1026
		public static ScriptingInterfaceOfIScene.IsMultiplayerSceneDelegate call_IsMultiplayerSceneDelegate;

		// Token: 0x04000403 RID: 1027
		public static ScriptingInterfaceOfIScene.IsPositionOnADynamicNavMeshDelegate call_IsPositionOnADynamicNavMeshDelegate;

		// Token: 0x04000404 RID: 1028
		public static ScriptingInterfaceOfIScene.LoadNavMeshPrefabDelegate call_LoadNavMeshPrefabDelegate;

		// Token: 0x04000405 RID: 1029
		public static ScriptingInterfaceOfIScene.LoadNavMeshPrefabWithFrameDelegate call_LoadNavMeshPrefabWithFrameDelegate;

		// Token: 0x04000406 RID: 1030
		public static ScriptingInterfaceOfIScene.MarkFacesWithIdAsLadderDelegate call_MarkFacesWithIdAsLadderDelegate;

		// Token: 0x04000407 RID: 1031
		public static ScriptingInterfaceOfIScene.MergeFacesWithIdDelegate call_MergeFacesWithIdDelegate;

		// Token: 0x04000408 RID: 1032
		public static ScriptingInterfaceOfIScene.OptimizeSceneDelegate call_OptimizeSceneDelegate;

		// Token: 0x04000409 RID: 1033
		public static ScriptingInterfaceOfIScene.PauseSceneSoundsDelegate call_PauseSceneSoundsDelegate;

		// Token: 0x0400040A RID: 1034
		public static ScriptingInterfaceOfIScene.PreloadForRenderingDelegate call_PreloadForRenderingDelegate;

		// Token: 0x0400040B RID: 1035
		public static ScriptingInterfaceOfIScene.RayCastExcludingTwoEntitiesDelegate call_RayCastExcludingTwoEntitiesDelegate;

		// Token: 0x0400040C RID: 1036
		public static ScriptingInterfaceOfIScene.RayCastForClosestEntityOrTerrainDelegate call_RayCastForClosestEntityOrTerrainDelegate;

		// Token: 0x0400040D RID: 1037
		public static ScriptingInterfaceOfIScene.RayCastForClosestEntityOrTerrainIgnoreEntityDelegate call_RayCastForClosestEntityOrTerrainIgnoreEntityDelegate;

		// Token: 0x0400040E RID: 1038
		public static ScriptingInterfaceOfIScene.RayCastForRammingDelegate call_RayCastForRammingDelegate;

		// Token: 0x0400040F RID: 1039
		public static ScriptingInterfaceOfIScene.ReadDelegate call_ReadDelegate;

		// Token: 0x04000410 RID: 1040
		public static ScriptingInterfaceOfIScene.ReadAndCalculateInitialCameraDelegate call_ReadAndCalculateInitialCameraDelegate;

		// Token: 0x04000411 RID: 1041
		public static ScriptingInterfaceOfIScene.ReadInModuleDelegate call_ReadInModuleDelegate;

		// Token: 0x04000412 RID: 1042
		public static ScriptingInterfaceOfIScene.RegisterShipVisualToWaterRendererDelegate call_RegisterShipVisualToWaterRendererDelegate;

		// Token: 0x04000413 RID: 1043
		public static ScriptingInterfaceOfIScene.RemoveAlwaysRenderedSkeletonDelegate call_RemoveAlwaysRenderedSkeletonDelegate;

		// Token: 0x04000414 RID: 1044
		public static ScriptingInterfaceOfIScene.RemoveDecalInstanceDelegate call_RemoveDecalInstanceDelegate;

		// Token: 0x04000415 RID: 1045
		public static ScriptingInterfaceOfIScene.RemoveEntityDelegate call_RemoveEntityDelegate;

		// Token: 0x04000416 RID: 1046
		public static ScriptingInterfaceOfIScene.ResumeLoadingRenderingsDelegate call_ResumeLoadingRenderingsDelegate;

		// Token: 0x04000417 RID: 1047
		public static ScriptingInterfaceOfIScene.ResumeSceneSoundsDelegate call_ResumeSceneSoundsDelegate;

		// Token: 0x04000418 RID: 1048
		public static ScriptingInterfaceOfIScene.SaveNavMeshPrefabWithFrameDelegate call_SaveNavMeshPrefabWithFrameDelegate;

		// Token: 0x04000419 RID: 1049
		public static ScriptingInterfaceOfIScene.SceneHadWaterWakeRendererDelegate call_SceneHadWaterWakeRendererDelegate;

		// Token: 0x0400041A RID: 1050
		public static ScriptingInterfaceOfIScene.SelectEntitiesCollidedWithDelegate call_SelectEntitiesCollidedWithDelegate;

		// Token: 0x0400041B RID: 1051
		public static ScriptingInterfaceOfIScene.SelectEntitiesInBoxWithScriptComponentDelegate call_SelectEntitiesInBoxWithScriptComponentDelegate;

		// Token: 0x0400041C RID: 1052
		public static ScriptingInterfaceOfIScene.SeparateFacesWithIdDelegate call_SeparateFacesWithIdDelegate;

		// Token: 0x0400041D RID: 1053
		public static ScriptingInterfaceOfIScene.SetAberrationOffsetDelegate call_SetAberrationOffsetDelegate;

		// Token: 0x0400041E RID: 1054
		public static ScriptingInterfaceOfIScene.SetAberrationSizeDelegate call_SetAberrationSizeDelegate;

		// Token: 0x0400041F RID: 1055
		public static ScriptingInterfaceOfIScene.SetAberrationSmoothDelegate call_SetAberrationSmoothDelegate;

		// Token: 0x04000420 RID: 1056
		public static ScriptingInterfaceOfIScene.SetAbilityOfFacesWithIdDelegate call_SetAbilityOfFacesWithIdDelegate;

		// Token: 0x04000421 RID: 1057
		public static ScriptingInterfaceOfIScene.SetActiveVisibilityLevelsDelegate call_SetActiveVisibilityLevelsDelegate;

		// Token: 0x04000422 RID: 1058
		public static ScriptingInterfaceOfIScene.SetAntialiasingModeDelegate call_SetAntialiasingModeDelegate;

		// Token: 0x04000423 RID: 1059
		public static ScriptingInterfaceOfIScene.SetAtmosphereWithNameDelegate call_SetAtmosphereWithNameDelegate;

		// Token: 0x04000424 RID: 1060
		public static ScriptingInterfaceOfIScene.SetBloomDelegate call_SetBloomDelegate;

		// Token: 0x04000425 RID: 1061
		public static ScriptingInterfaceOfIScene.SetBloomAmountDelegate call_SetBloomAmountDelegate;

		// Token: 0x04000426 RID: 1062
		public static ScriptingInterfaceOfIScene.SetBloomStrengthDelegate call_SetBloomStrengthDelegate;

		// Token: 0x04000427 RID: 1063
		public static ScriptingInterfaceOfIScene.SetBrightpassTresholdDelegate call_SetBrightpassTresholdDelegate;

		// Token: 0x04000428 RID: 1064
		public static ScriptingInterfaceOfIScene.SetClothSimulationStateDelegate call_SetClothSimulationStateDelegate;

		// Token: 0x04000429 RID: 1065
		public static ScriptingInterfaceOfIScene.SetColorGradeBlendDelegate call_SetColorGradeBlendDelegate;

		// Token: 0x0400042A RID: 1066
		public static ScriptingInterfaceOfIScene.SetDLSSModeDelegate call_SetDLSSModeDelegate;

		// Token: 0x0400042B RID: 1067
		public static ScriptingInterfaceOfIScene.SetDofFocusDelegate call_SetDofFocusDelegate;

		// Token: 0x0400042C RID: 1068
		public static ScriptingInterfaceOfIScene.SetDofModeDelegate call_SetDofModeDelegate;

		// Token: 0x0400042D RID: 1069
		public static ScriptingInterfaceOfIScene.SetDofParamsDelegate call_SetDofParamsDelegate;

		// Token: 0x0400042E RID: 1070
		public static ScriptingInterfaceOfIScene.SetDoNotAddEntitiesToTickListDelegate call_SetDoNotAddEntitiesToTickListDelegate;

		// Token: 0x0400042F RID: 1071
		public static ScriptingInterfaceOfIScene.SetDoNotWaitForLoadingStatesToRenderDelegate call_SetDoNotWaitForLoadingStatesToRenderDelegate;

		// Token: 0x04000430 RID: 1072
		public static ScriptingInterfaceOfIScene.SetDontLoadInvisibleEntitiesDelegate call_SetDontLoadInvisibleEntitiesDelegate;

		// Token: 0x04000431 RID: 1073
		public static ScriptingInterfaceOfIScene.SetDrynessFactorDelegate call_SetDrynessFactorDelegate;

		// Token: 0x04000432 RID: 1074
		public static ScriptingInterfaceOfIScene.SetDynamicShadowmapCascadesRadiusMultiplierDelegate call_SetDynamicShadowmapCascadesRadiusMultiplierDelegate;

		// Token: 0x04000433 RID: 1075
		public static ScriptingInterfaceOfIScene.SetDynamicSnowTextureDelegate call_SetDynamicSnowTextureDelegate;

		// Token: 0x04000434 RID: 1076
		public static ScriptingInterfaceOfIScene.SetEnvironmentMultiplierDelegate call_SetEnvironmentMultiplierDelegate;

		// Token: 0x04000435 RID: 1077
		public static ScriptingInterfaceOfIScene.SetExternalInjectionTextureDelegate call_SetExternalInjectionTextureDelegate;

		// Token: 0x04000436 RID: 1078
		public static ScriptingInterfaceOfIScene.SetFetchCrcInfoOfSceneDelegate call_SetFetchCrcInfoOfSceneDelegate;

		// Token: 0x04000437 RID: 1079
		public static ScriptingInterfaceOfIScene.SetFixedTickCallbackActiveDelegate call_SetFixedTickCallbackActiveDelegate;

		// Token: 0x04000438 RID: 1080
		public static ScriptingInterfaceOfIScene.SetFogDelegate call_SetFogDelegate;

		// Token: 0x04000439 RID: 1081
		public static ScriptingInterfaceOfIScene.SetFogAdvancedDelegate call_SetFogAdvancedDelegate;

		// Token: 0x0400043A RID: 1082
		public static ScriptingInterfaceOfIScene.SetFogAmbientColorDelegate call_SetFogAmbientColorDelegate;

		// Token: 0x0400043B RID: 1083
		public static ScriptingInterfaceOfIScene.SetForcedSnowDelegate call_SetForcedSnowDelegate;

		// Token: 0x0400043C RID: 1084
		public static ScriptingInterfaceOfIScene.SetGlobalWindStrengthVectorDelegate call_SetGlobalWindStrengthVectorDelegate;

		// Token: 0x0400043D RID: 1085
		public static ScriptingInterfaceOfIScene.SetGlobalWindVelocityDelegate call_SetGlobalWindVelocityDelegate;

		// Token: 0x0400043E RID: 1086
		public static ScriptingInterfaceOfIScene.SetGrainAmountDelegate call_SetGrainAmountDelegate;

		// Token: 0x0400043F RID: 1087
		public static ScriptingInterfaceOfIScene.SetHexagonVignetteAlphaDelegate call_SetHexagonVignetteAlphaDelegate;

		// Token: 0x04000440 RID: 1088
		public static ScriptingInterfaceOfIScene.SetHexagonVignetteColorDelegate call_SetHexagonVignetteColorDelegate;

		// Token: 0x04000441 RID: 1089
		public static ScriptingInterfaceOfIScene.SetHumidityDelegate call_SetHumidityDelegate;

		// Token: 0x04000442 RID: 1090
		public static ScriptingInterfaceOfIScene.SetLandscapeRainMaskDataDelegate call_SetLandscapeRainMaskDataDelegate;

		// Token: 0x04000443 RID: 1091
		public static ScriptingInterfaceOfIScene.SetLensDistortionDelegate call_SetLensDistortionDelegate;

		// Token: 0x04000444 RID: 1092
		public static ScriptingInterfaceOfIScene.SetLensFlareAberrationOffsetDelegate call_SetLensFlareAberrationOffsetDelegate;

		// Token: 0x04000445 RID: 1093
		public static ScriptingInterfaceOfIScene.SetLensFlareAmountDelegate call_SetLensFlareAmountDelegate;

		// Token: 0x04000446 RID: 1094
		public static ScriptingInterfaceOfIScene.SetLensFlareBlurSigmaDelegate call_SetLensFlareBlurSigmaDelegate;

		// Token: 0x04000447 RID: 1095
		public static ScriptingInterfaceOfIScene.SetLensFlareBlurSizeDelegate call_SetLensFlareBlurSizeDelegate;

		// Token: 0x04000448 RID: 1096
		public static ScriptingInterfaceOfIScene.SetLensFlareDiffractionWeightDelegate call_SetLensFlareDiffractionWeightDelegate;

		// Token: 0x04000449 RID: 1097
		public static ScriptingInterfaceOfIScene.SetLensFlareDirtWeightDelegate call_SetLensFlareDirtWeightDelegate;

		// Token: 0x0400044A RID: 1098
		public static ScriptingInterfaceOfIScene.SetLensFlareGhostSamplesDelegate call_SetLensFlareGhostSamplesDelegate;

		// Token: 0x0400044B RID: 1099
		public static ScriptingInterfaceOfIScene.SetLensFlareGhostWeightDelegate call_SetLensFlareGhostWeightDelegate;

		// Token: 0x0400044C RID: 1100
		public static ScriptingInterfaceOfIScene.SetLensFlareHaloWeightDelegate call_SetLensFlareHaloWeightDelegate;

		// Token: 0x0400044D RID: 1101
		public static ScriptingInterfaceOfIScene.SetLensFlareHaloWidthDelegate call_SetLensFlareHaloWidthDelegate;

		// Token: 0x0400044E RID: 1102
		public static ScriptingInterfaceOfIScene.SetLensFlareStrengthDelegate call_SetLensFlareStrengthDelegate;

		// Token: 0x0400044F RID: 1103
		public static ScriptingInterfaceOfIScene.SetLensFlareThresholdDelegate call_SetLensFlareThresholdDelegate;

		// Token: 0x04000450 RID: 1104
		public static ScriptingInterfaceOfIScene.SetLightDiffuseColorDelegate call_SetLightDiffuseColorDelegate;

		// Token: 0x04000451 RID: 1105
		public static ScriptingInterfaceOfIScene.SetLightDirectionDelegate call_SetLightDirectionDelegate;

		// Token: 0x04000452 RID: 1106
		public static ScriptingInterfaceOfIScene.SetLightPositionDelegate call_SetLightPositionDelegate;

		// Token: 0x04000453 RID: 1107
		public static ScriptingInterfaceOfIScene.SetMaxExposureDelegate call_SetMaxExposureDelegate;

		// Token: 0x04000454 RID: 1108
		public static ScriptingInterfaceOfIScene.SetMiddleGrayDelegate call_SetMiddleGrayDelegate;

		// Token: 0x04000455 RID: 1109
		public static ScriptingInterfaceOfIScene.SetMieScatterFocusDelegate call_SetMieScatterFocusDelegate;

		// Token: 0x04000456 RID: 1110
		public static ScriptingInterfaceOfIScene.SetMieScatterStrengthDelegate call_SetMieScatterStrengthDelegate;

		// Token: 0x04000457 RID: 1111
		public static ScriptingInterfaceOfIScene.SetMinExposureDelegate call_SetMinExposureDelegate;

		// Token: 0x04000458 RID: 1112
		public static ScriptingInterfaceOfIScene.SetMotionBlurModeDelegate call_SetMotionBlurModeDelegate;

		// Token: 0x04000459 RID: 1113
		public static ScriptingInterfaceOfIScene.SetNameDelegate call_SetNameDelegate;

		// Token: 0x0400045A RID: 1114
		public static ScriptingInterfaceOfIScene.SetNavMeshRegionMapDelegate call_SetNavMeshRegionMapDelegate;

		// Token: 0x0400045B RID: 1115
		public static ScriptingInterfaceOfIScene.SetOcclusionModeDelegate call_SetOcclusionModeDelegate;

		// Token: 0x0400045C RID: 1116
		public static ScriptingInterfaceOfIScene.SetOnCollisionFilterCallbackActiveDelegate call_SetOnCollisionFilterCallbackActiveDelegate;

		// Token: 0x0400045D RID: 1117
		public static ScriptingInterfaceOfIScene.SetOwnerThreadDelegate call_SetOwnerThreadDelegate;

		// Token: 0x0400045E RID: 1118
		public static ScriptingInterfaceOfIScene.SetPhotoAtmosphereViaTodDelegate call_SetPhotoAtmosphereViaTodDelegate;

		// Token: 0x0400045F RID: 1119
		public static ScriptingInterfaceOfIScene.SetPhotoModeFocusDelegate call_SetPhotoModeFocusDelegate;

		// Token: 0x04000460 RID: 1120
		public static ScriptingInterfaceOfIScene.SetPhotoModeFovDelegate call_SetPhotoModeFovDelegate;

		// Token: 0x04000461 RID: 1121
		public static ScriptingInterfaceOfIScene.SetPhotoModeOnDelegate call_SetPhotoModeOnDelegate;

		// Token: 0x04000462 RID: 1122
		public static ScriptingInterfaceOfIScene.SetPhotoModeOrbitDelegate call_SetPhotoModeOrbitDelegate;

		// Token: 0x04000463 RID: 1123
		public static ScriptingInterfaceOfIScene.SetPhotoModeRollDelegate call_SetPhotoModeRollDelegate;

		// Token: 0x04000464 RID: 1124
		public static ScriptingInterfaceOfIScene.SetPhotoModeVignetteDelegate call_SetPhotoModeVignetteDelegate;

		// Token: 0x04000465 RID: 1125
		public static ScriptingInterfaceOfIScene.SetPlaySoundEventsAfterReadyToRenderDelegate call_SetPlaySoundEventsAfterReadyToRenderDelegate;

		// Token: 0x04000466 RID: 1126
		public static ScriptingInterfaceOfIScene.SetRainDensityDelegate call_SetRainDensityDelegate;

		// Token: 0x04000467 RID: 1127
		public static ScriptingInterfaceOfIScene.SetSceneColorGradeDelegate call_SetSceneColorGradeDelegate;

		// Token: 0x04000468 RID: 1128
		public static ScriptingInterfaceOfIScene.SetSceneColorGradeIndexDelegate call_SetSceneColorGradeIndexDelegate;

		// Token: 0x04000469 RID: 1129
		public static ScriptingInterfaceOfIScene.SetSceneFilterIndexDelegate call_SetSceneFilterIndexDelegate;

		// Token: 0x0400046A RID: 1130
		public static ScriptingInterfaceOfIScene.SetShadowDelegate call_SetShadowDelegate;

		// Token: 0x0400046B RID: 1131
		public static ScriptingInterfaceOfIScene.SetSkyBrightnessDelegate call_SetSkyBrightnessDelegate;

		// Token: 0x0400046C RID: 1132
		public static ScriptingInterfaceOfIScene.SetSkyRotationDelegate call_SetSkyRotationDelegate;

		// Token: 0x0400046D RID: 1133
		public static ScriptingInterfaceOfIScene.SetSnowDensityDelegate call_SetSnowDensityDelegate;

		// Token: 0x0400046E RID: 1134
		public static ScriptingInterfaceOfIScene.SetStreakAmountDelegate call_SetStreakAmountDelegate;

		// Token: 0x0400046F RID: 1135
		public static ScriptingInterfaceOfIScene.SetStreakIntensityDelegate call_SetStreakIntensityDelegate;

		// Token: 0x04000470 RID: 1136
		public static ScriptingInterfaceOfIScene.SetStreakStrengthDelegate call_SetStreakStrengthDelegate;

		// Token: 0x04000471 RID: 1137
		public static ScriptingInterfaceOfIScene.SetStreakStretchDelegate call_SetStreakStretchDelegate;

		// Token: 0x04000472 RID: 1138
		public static ScriptingInterfaceOfIScene.SetStreakThresholdDelegate call_SetStreakThresholdDelegate;

		// Token: 0x04000473 RID: 1139
		public static ScriptingInterfaceOfIScene.SetStreakTintDelegate call_SetStreakTintDelegate;

		// Token: 0x04000474 RID: 1140
		public static ScriptingInterfaceOfIScene.SetSunDelegate call_SetSunDelegate;

		// Token: 0x04000475 RID: 1141
		public static ScriptingInterfaceOfIScene.SetSunAngleAltitudeDelegate call_SetSunAngleAltitudeDelegate;

		// Token: 0x04000476 RID: 1142
		public static ScriptingInterfaceOfIScene.SetSunDirectionDelegate call_SetSunDirectionDelegate;

		// Token: 0x04000477 RID: 1143
		public static ScriptingInterfaceOfIScene.SetSunLightDelegate call_SetSunLightDelegate;

		// Token: 0x04000478 RID: 1144
		public static ScriptingInterfaceOfIScene.SetSunshaftModeDelegate call_SetSunshaftModeDelegate;

		// Token: 0x04000479 RID: 1145
		public static ScriptingInterfaceOfIScene.SetSunShaftStrengthDelegate call_SetSunShaftStrengthDelegate;

		// Token: 0x0400047A RID: 1146
		public static ScriptingInterfaceOfIScene.SetSunSizeDelegate call_SetSunSizeDelegate;

		// Token: 0x0400047B RID: 1147
		public static ScriptingInterfaceOfIScene.SetTargetExposureDelegate call_SetTargetExposureDelegate;

		// Token: 0x0400047C RID: 1148
		public static ScriptingInterfaceOfIScene.SetTemperatureDelegate call_SetTemperatureDelegate;

		// Token: 0x0400047D RID: 1149
		public static ScriptingInterfaceOfIScene.SetTerrainDynamicParamsDelegate call_SetTerrainDynamicParamsDelegate;

		// Token: 0x0400047E RID: 1150
		public static ScriptingInterfaceOfIScene.SetTimeOfDayDelegate call_SetTimeOfDayDelegate;

		// Token: 0x0400047F RID: 1151
		public static ScriptingInterfaceOfIScene.SetTimeSpeedDelegate call_SetTimeSpeedDelegate;

		// Token: 0x04000480 RID: 1152
		public static ScriptingInterfaceOfIScene.SetUpgradeLevelDelegate call_SetUpgradeLevelDelegate;

		// Token: 0x04000481 RID: 1153
		public static ScriptingInterfaceOfIScene.SetUpgradeLevelVisibilityDelegate call_SetUpgradeLevelVisibilityDelegate;

		// Token: 0x04000482 RID: 1154
		public static ScriptingInterfaceOfIScene.SetUpgradeLevelVisibilityWithMaskDelegate call_SetUpgradeLevelVisibilityWithMaskDelegate;

		// Token: 0x04000483 RID: 1155
		public static ScriptingInterfaceOfIScene.SetUseConstantTimeDelegate call_SetUseConstantTimeDelegate;

		// Token: 0x04000484 RID: 1156
		public static ScriptingInterfaceOfIScene.SetUsesDeleteLaterSystemDelegate call_SetUsesDeleteLaterSystemDelegate;

		// Token: 0x04000485 RID: 1157
		public static ScriptingInterfaceOfIScene.SetVignetteInnerRadiusDelegate call_SetVignetteInnerRadiusDelegate;

		// Token: 0x04000486 RID: 1158
		public static ScriptingInterfaceOfIScene.SetVignetteOpacityDelegate call_SetVignetteOpacityDelegate;

		// Token: 0x04000487 RID: 1159
		public static ScriptingInterfaceOfIScene.SetVignetteOuterRadiusDelegate call_SetVignetteOuterRadiusDelegate;

		// Token: 0x04000488 RID: 1160
		public static ScriptingInterfaceOfIScene.SetWaterStrengthDelegate call_SetWaterStrengthDelegate;

		// Token: 0x04000489 RID: 1161
		public static ScriptingInterfaceOfIScene.SetWaterWakeCameraOffsetDelegate call_SetWaterWakeCameraOffsetDelegate;

		// Token: 0x0400048A RID: 1162
		public static ScriptingInterfaceOfIScene.SetWaterWakeWorldSizeDelegate call_SetWaterWakeWorldSizeDelegate;

		// Token: 0x0400048B RID: 1163
		public static ScriptingInterfaceOfIScene.SetWinterTimeFactorDelegate call_SetWinterTimeFactorDelegate;

		// Token: 0x0400048C RID: 1164
		public static ScriptingInterfaceOfIScene.StallLoadingRenderingsUntilFurtherNoticeDelegate call_StallLoadingRenderingsUntilFurtherNoticeDelegate;

		// Token: 0x0400048D RID: 1165
		public static ScriptingInterfaceOfIScene.SwapFaceConnectionsWithIdDelegate call_SwapFaceConnectionsWithIdDelegate;

		// Token: 0x0400048E RID: 1166
		public static ScriptingInterfaceOfIScene.TakePhotoModePictureDelegate call_TakePhotoModePictureDelegate;

		// Token: 0x0400048F RID: 1167
		public static ScriptingInterfaceOfIScene.TickDelegate call_TickDelegate;

		// Token: 0x04000490 RID: 1168
		public static ScriptingInterfaceOfIScene.TickWakeDelegate call_TickWakeDelegate;

		// Token: 0x04000491 RID: 1169
		public static ScriptingInterfaceOfIScene.WaitWaterRendererCPUSimulationDelegate call_WaitWaterRendererCPUSimulationDelegate;

		// Token: 0x04000492 RID: 1170
		public static ScriptingInterfaceOfIScene.WorldPositionComputeNearestNavMeshDelegate call_WorldPositionComputeNearestNavMeshDelegate;

		// Token: 0x04000493 RID: 1171
		public static ScriptingInterfaceOfIScene.WorldPositionValidateZDelegate call_WorldPositionValidateZDelegate;

		// Token: 0x020003D2 RID: 978
		// (Invoke) Token: 0x0600155B RID: 5467
		[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
		[SuppressUnmanagedCodeSecurity]
		[MonoNativeFunctionWrapper]
		public delegate void AddAlwaysRenderedSkeletonDelegate(UIntPtr scenePointer, UIntPtr skeletonPointer);

		// Token: 0x020003D3 RID: 979
		// (Invoke) Token: 0x0600155F RID: 5471
		[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
		[SuppressUnmanagedCodeSecurity]
		[MonoNativeFunctionWrapper]
		public delegate void AddDecalInstanceDelegate(UIntPtr scenePointer, UIntPtr decalMeshPointer, byte[] decalSetID, [MarshalAs(UnmanagedType.U1)] bool deletable);

		// Token: 0x020003D4 RID: 980
		// (Invoke) Token: 0x06001563 RID: 5475
		[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
		[SuppressUnmanagedCodeSecurity]
		[MonoNativeFunctionWrapper]
		public delegate int AddDirectionalLightDelegate(UIntPtr scenePointer, Vec3 position, Vec3 direction, float radius);

		// Token: 0x020003D5 RID: 981
		// (Invoke) Token: 0x06001567 RID: 5479
		[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
		[SuppressUnmanagedCodeSecurity]
		[MonoNativeFunctionWrapper]
		public delegate void AddEntityWithMeshDelegate(UIntPtr scenePointer, UIntPtr meshPointer, ref MatrixFrame frame);

		// Token: 0x020003D6 RID: 982
		// (Invoke) Token: 0x0600156B RID: 5483
		[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
		[SuppressUnmanagedCodeSecurity]
		[MonoNativeFunctionWrapper]
		public delegate void AddEntityWithMultiMeshDelegate(UIntPtr scenePointer, UIntPtr multiMeshPointer, ref MatrixFrame frame);

		// Token: 0x020003D7 RID: 983
		// (Invoke) Token: 0x0600156F RID: 5487
		[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
		[SuppressUnmanagedCodeSecurity]
		[MonoNativeFunctionWrapper]
		public delegate NativeObjectPointer AddItemEntityDelegate(UIntPtr scenePointer, ref MatrixFrame frame, UIntPtr meshPointer);

		// Token: 0x020003D8 RID: 984
		// (Invoke) Token: 0x06001573 RID: 5491
		[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
		[SuppressUnmanagedCodeSecurity]
		[MonoNativeFunctionWrapper]
		public delegate void AddPathDelegate(UIntPtr scenePointer, byte[] name);

		// Token: 0x020003D9 RID: 985
		// (Invoke) Token: 0x06001577 RID: 5495
		[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
		[SuppressUnmanagedCodeSecurity]
		[MonoNativeFunctionWrapper]
		public delegate void AddPathPointDelegate(UIntPtr scenePointer, byte[] name, ref MatrixFrame frame);

		// Token: 0x020003DA RID: 986
		// (Invoke) Token: 0x0600157B RID: 5499
		[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
		[SuppressUnmanagedCodeSecurity]
		[MonoNativeFunctionWrapper]
		public delegate int AddPointLightDelegate(UIntPtr scenePointer, Vec3 position, float radius);

		// Token: 0x020003DB RID: 987
		// (Invoke) Token: 0x0600157F RID: 5503
		[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
		[SuppressUnmanagedCodeSecurity]
		[MonoNativeFunctionWrapper]
		public delegate void AddWaterWakeWithCapsuleDelegate(UIntPtr scene, Vec3 positionA, float radiusA, Vec3 positionB, float radiusB, float wakeVisibility, float foamVisibility);

		// Token: 0x020003DC RID: 988
		// (Invoke) Token: 0x06001583 RID: 5507
		[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
		[SuppressUnmanagedCodeSecurity]
		[MonoNativeFunctionWrapper]
		[return: MarshalAs(UnmanagedType.U1)]
		public delegate bool AttachEntityDelegate(UIntPtr scenePointer, UIntPtr entity, [MarshalAs(UnmanagedType.U1)] bool showWarnings);

		// Token: 0x020003DD RID: 989
		// (Invoke) Token: 0x06001587 RID: 5511
		[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
		[SuppressUnmanagedCodeSecurity]
		[MonoNativeFunctionWrapper]
		[return: MarshalAs(UnmanagedType.U1)]
		public delegate bool BoxCastDelegate(UIntPtr scenePointer, ref Vec3 boxPointBegin, ref Vec3 boxPointEnd, ref Vec3 dir, float distance, ref float collisionDistance, ref Vec3 closestPoint, ref UIntPtr entityIndex, BodyFlags bodyExcludeFlags);

		// Token: 0x020003DE RID: 990
		// (Invoke) Token: 0x0600158B RID: 5515
		[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
		[SuppressUnmanagedCodeSecurity]
		[MonoNativeFunctionWrapper]
		[return: MarshalAs(UnmanagedType.U1)]
		public delegate bool BoxCastOnlyForCameraDelegate(UIntPtr scenePointer, IntPtr boxPoints, in Vec3 centerPoint, in Vec3 dir, float distance, UIntPtr ignoredEntityPointer, ref float collisionDistance, ref Vec3 closestPoint, ref UIntPtr entityPointer, BodyFlags bodyExcludeFlags);

		// Token: 0x020003DF RID: 991
		// (Invoke) Token: 0x0600158F RID: 5519
		[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
		[SuppressUnmanagedCodeSecurity]
		[MonoNativeFunctionWrapper]
		[return: MarshalAs(UnmanagedType.U1)]
		public delegate bool CalculateEffectiveLightingDelegate(UIntPtr scenePointer);

		// Token: 0x020003E0 RID: 992
		// (Invoke) Token: 0x06001593 RID: 5523
		[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
		[SuppressUnmanagedCodeSecurity]
		[MonoNativeFunctionWrapper]
		[return: MarshalAs(UnmanagedType.U1)]
		public delegate bool CheckPathEntitiesFrameChangedDelegate(UIntPtr scenePointer, byte[] containsName);

		// Token: 0x020003E1 RID: 993
		// (Invoke) Token: 0x06001597 RID: 5527
		[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
		[SuppressUnmanagedCodeSecurity]
		[MonoNativeFunctionWrapper]
		[return: MarshalAs(UnmanagedType.U1)]
		public delegate bool CheckPointCanSeePointDelegate(UIntPtr scenePointer, Vec3 sourcePoint, Vec3 targetPoint, float distanceToCheck);

		// Token: 0x020003E2 RID: 994
		// (Invoke) Token: 0x0600159B RID: 5531
		[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
		[SuppressUnmanagedCodeSecurity]
		[MonoNativeFunctionWrapper]
		public delegate void CheckResourcesDelegate(UIntPtr scenePointer, [MarshalAs(UnmanagedType.U1)] bool checkInvisibleEntities);

		// Token: 0x020003E3 RID: 995
		// (Invoke) Token: 0x0600159F RID: 5535
		[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
		[SuppressUnmanagedCodeSecurity]
		[MonoNativeFunctionWrapper]
		public delegate void ClearAllDelegate(UIntPtr scenePointer);

		// Token: 0x020003E4 RID: 996
		// (Invoke) Token: 0x060015A3 RID: 5539
		[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
		[SuppressUnmanagedCodeSecurity]
		[MonoNativeFunctionWrapper]
		public delegate void ClearCurrentFrameTickEntitiesDelegate(UIntPtr scenePointer);

		// Token: 0x020003E5 RID: 997
		// (Invoke) Token: 0x060015A7 RID: 5543
		[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
		[SuppressUnmanagedCodeSecurity]
		[MonoNativeFunctionWrapper]
		public delegate void ClearDecalsDelegate(UIntPtr scenePointer);

		// Token: 0x020003E6 RID: 998
		// (Invoke) Token: 0x060015AB RID: 5547
		[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
		[SuppressUnmanagedCodeSecurity]
		[MonoNativeFunctionWrapper]
		public delegate void ClearNavMeshDelegate(UIntPtr scenePointer);

		// Token: 0x020003E7 RID: 999
		// (Invoke) Token: 0x060015AF RID: 5551
		[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
		[SuppressUnmanagedCodeSecurity]
		[MonoNativeFunctionWrapper]
		[return: MarshalAs(UnmanagedType.U1)]
		public delegate bool ContainsTerrainDelegate(UIntPtr scenePointer);

		// Token: 0x020003E8 RID: 1000
		// (Invoke) Token: 0x060015B3 RID: 5555
		[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
		[SuppressUnmanagedCodeSecurity]
		[MonoNativeFunctionWrapper]
		public delegate void CreateBurstParticleDelegate(UIntPtr scene, int particleId, ref MatrixFrame frame);

		// Token: 0x020003E9 RID: 1001
		// (Invoke) Token: 0x060015B7 RID: 5559
		[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
		[SuppressUnmanagedCodeSecurity]
		[MonoNativeFunctionWrapper]
		public delegate void CreateDynamicRainTextureDelegate(UIntPtr scenePointer, int w, int h);

		// Token: 0x020003EA RID: 1002
		// (Invoke) Token: 0x060015BB RID: 5563
		[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
		[SuppressUnmanagedCodeSecurity]
		[MonoNativeFunctionWrapper]
		public delegate NativeObjectPointer CreateNewSceneDelegate([MarshalAs(UnmanagedType.U1)] bool initializePhysics, [MarshalAs(UnmanagedType.U1)] bool enableDecals, int atlasGroup, byte[] sceneName);

		// Token: 0x020003EB RID: 1003
		// (Invoke) Token: 0x060015BF RID: 5567
		[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
		[SuppressUnmanagedCodeSecurity]
		[MonoNativeFunctionWrapper]
		public delegate NativeObjectPointer CreatePathMeshDelegate(UIntPtr scenePointer, byte[] baseEntityName, [MarshalAs(UnmanagedType.U1)] bool isWaterPath);

		// Token: 0x020003EC RID: 1004
		// (Invoke) Token: 0x060015C3 RID: 5571
		[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
		[SuppressUnmanagedCodeSecurity]
		[MonoNativeFunctionWrapper]
		public delegate NativeObjectPointer CreatePathMesh2Delegate(UIntPtr scenePointer, IntPtr pathNodes, int pathNodeCount, [MarshalAs(UnmanagedType.U1)] bool isWaterPath);

		// Token: 0x020003ED RID: 1005
		// (Invoke) Token: 0x060015C7 RID: 5575
		[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
		[SuppressUnmanagedCodeSecurity]
		[MonoNativeFunctionWrapper]
		public delegate void DeletePathWithNameDelegate(UIntPtr scenePointer, byte[] name);

		// Token: 0x020003EE RID: 1006
		// (Invoke) Token: 0x060015CB RID: 5579
		[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
		[SuppressUnmanagedCodeSecurity]
		[MonoNativeFunctionWrapper]
		public delegate void DeleteWaterWakeRendererDelegate(UIntPtr scenePointer);

		// Token: 0x020003EF RID: 1007
		// (Invoke) Token: 0x060015CF RID: 5583
		[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
		[SuppressUnmanagedCodeSecurity]
		[MonoNativeFunctionWrapper]
		public delegate void DeRegisterShipVisualDelegate(UIntPtr scenePointer, UIntPtr visualPointer);

		// Token: 0x020003F0 RID: 1008
		// (Invoke) Token: 0x060015D3 RID: 5587
		[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
		[SuppressUnmanagedCodeSecurity]
		[MonoNativeFunctionWrapper]
		public delegate void DisableStaticShadowsDelegate(UIntPtr ptr, [MarshalAs(UnmanagedType.U1)] bool value);

		// Token: 0x020003F1 RID: 1009
		// (Invoke) Token: 0x060015D7 RID: 5591
		[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
		[SuppressUnmanagedCodeSecurity]
		[MonoNativeFunctionWrapper]
		[return: MarshalAs(UnmanagedType.U1)]
		public delegate bool DoesPathExistBetweenFacesDelegate(UIntPtr scenePointer, int firstNavMeshFace, int secondNavMeshFace, [MarshalAs(UnmanagedType.U1)] bool ignoreDisabled);

		// Token: 0x020003F2 RID: 1010
		// (Invoke) Token: 0x060015DB RID: 5595
		[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
		[SuppressUnmanagedCodeSecurity]
		[MonoNativeFunctionWrapper]
		[return: MarshalAs(UnmanagedType.U1)]
		public delegate bool DoesPathExistBetweenPositionsDelegate(UIntPtr scenePointer, WorldPosition position, WorldPosition destination);

		// Token: 0x020003F3 RID: 1011
		// (Invoke) Token: 0x060015DF RID: 5599
		[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
		[SuppressUnmanagedCodeSecurity]
		[MonoNativeFunctionWrapper]
		public delegate void EnableFixedTickDelegate(UIntPtr scene);

		// Token: 0x020003F4 RID: 1012
		// (Invoke) Token: 0x060015E3 RID: 5603
		[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
		[SuppressUnmanagedCodeSecurity]
		[MonoNativeFunctionWrapper]
		public delegate void EnableInclusiveAsyncPhysxDelegate(UIntPtr scenePointer);

		// Token: 0x020003F5 RID: 1013
		// (Invoke) Token: 0x060015E7 RID: 5607
		[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
		[SuppressUnmanagedCodeSecurity]
		[MonoNativeFunctionWrapper]
		public delegate void EnsurePostfxSystemDelegate(UIntPtr scenePointer);

		// Token: 0x020003F6 RID: 1014
		// (Invoke) Token: 0x060015EB RID: 5611
		[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
		[SuppressUnmanagedCodeSecurity]
		[MonoNativeFunctionWrapper]
		public delegate void EnsureWaterWakeRendererDelegate(UIntPtr scenePointer);

		// Token: 0x020003F7 RID: 1015
		// (Invoke) Token: 0x060015EF RID: 5615
		[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
		[SuppressUnmanagedCodeSecurity]
		[MonoNativeFunctionWrapper]
		public delegate void FillEntityWithHardBorderPhysicsBarrierDelegate(UIntPtr scenePointer, UIntPtr entityPointer);

		// Token: 0x020003F8 RID: 1016
		// (Invoke) Token: 0x060015F3 RID: 5619
		[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
		[SuppressUnmanagedCodeSecurity]
		[MonoNativeFunctionWrapper]
		public delegate void FillTerrainHeightDataDelegate(UIntPtr scene, int xIndex, int yIndex, IntPtr heightArray);

		// Token: 0x020003F9 RID: 1017
		// (Invoke) Token: 0x060015F7 RID: 5623
		[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
		[SuppressUnmanagedCodeSecurity]
		[MonoNativeFunctionWrapper]
		public delegate void FillTerrainPhysicsMaterialIndexDataDelegate(UIntPtr scene, int xIndex, int yIndex, IntPtr materialIndexArray);

		// Token: 0x020003FA RID: 1018
		// (Invoke) Token: 0x060015FB RID: 5627
		[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
		[SuppressUnmanagedCodeSecurity]
		[MonoNativeFunctionWrapper]
		public delegate Vec2 FindClosestExitPositionForPositionOnABoundaryFaceDelegate(UIntPtr scenePointer, Vec3 position, UIntPtr boundaryNavMeshFacePointer);

		// Token: 0x020003FB RID: 1019
		// (Invoke) Token: 0x060015FF RID: 5631
		[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
		[SuppressUnmanagedCodeSecurity]
		[MonoNativeFunctionWrapper]
		public delegate void FinishSceneSoundsDelegate(UIntPtr scenePointer);

		// Token: 0x020003FC RID: 1020
		// (Invoke) Token: 0x06001603 RID: 5635
		[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
		[SuppressUnmanagedCodeSecurity]
		[MonoNativeFunctionWrapper]
		[return: MarshalAs(UnmanagedType.U1)]
		public delegate bool FocusRayCastForFixedPhysicsDelegate(UIntPtr scenePointer, in Vec3 sourcePoint, in Vec3 targetPoint, float rayThickness, ref float collisionDistance, ref Vec3 closestPoint, ref UIntPtr entityIndex, BodyFlags bodyExcludeFlags, [MarshalAs(UnmanagedType.U1)] bool isFixedWorld);

		// Token: 0x020003FD RID: 1021
		// (Invoke) Token: 0x06001607 RID: 5639
		[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
		[SuppressUnmanagedCodeSecurity]
		[MonoNativeFunctionWrapper]
		public delegate void ForceLoadResourcesDelegate(UIntPtr scenePointer, [MarshalAs(UnmanagedType.U1)] bool checkInvisibleEntities);

		// Token: 0x020003FE RID: 1022
		// (Invoke) Token: 0x0600160B RID: 5643
		[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
		[SuppressUnmanagedCodeSecurity]
		[MonoNativeFunctionWrapper]
		public delegate int GenerateContactsWithCapsuleDelegate(UIntPtr scenePointer, ref CapsuleData cap, BodyFlags excludeFlags, [MarshalAs(UnmanagedType.U1)] bool isFixedTick, IntPtr intersections, IntPtr entityIds);

		// Token: 0x020003FF RID: 1023
		// (Invoke) Token: 0x0600160F RID: 5647
		[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
		[SuppressUnmanagedCodeSecurity]
		[MonoNativeFunctionWrapper]
		public delegate int GenerateContactsWithCapsuleAgainstEntityDelegate(UIntPtr scenePointer, ref CapsuleData cap, BodyFlags excludeFlags, UIntPtr entityId, IntPtr intersections);

		// Token: 0x02000400 RID: 1024
		// (Invoke) Token: 0x06001613 RID: 5651
		[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
		[SuppressUnmanagedCodeSecurity]
		[MonoNativeFunctionWrapper]
		public delegate int GetAllColorGradeNamesDelegate(UIntPtr scene);

		// Token: 0x02000401 RID: 1025
		// (Invoke) Token: 0x06001617 RID: 5655
		[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
		[SuppressUnmanagedCodeSecurity]
		[MonoNativeFunctionWrapper]
		public delegate void GetAllEntitiesWithScriptComponentDelegate(UIntPtr scenePointer, byte[] scriptComponentName, UIntPtr output);

		// Token: 0x02000402 RID: 1026
		// (Invoke) Token: 0x0600161B RID: 5659
		[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
		[SuppressUnmanagedCodeSecurity]
		[MonoNativeFunctionWrapper]
		public delegate int GetAllFilterNamesDelegate(UIntPtr scene);

		// Token: 0x02000403 RID: 1027
		// (Invoke) Token: 0x0600161F RID: 5663
		[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
		[SuppressUnmanagedCodeSecurity]
		[MonoNativeFunctionWrapper]
		public delegate void GetAllNavmeshFaceRecordsDelegate(UIntPtr scenePointer, IntPtr faceRecords);

		// Token: 0x02000404 RID: 1028
		// (Invoke) Token: 0x06001623 RID: 5667
		[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
		[SuppressUnmanagedCodeSecurity]
		[MonoNativeFunctionWrapper]
		public delegate void GetBoundingBoxDelegate(UIntPtr scenePointer, ref Vec3 min, ref Vec3 max);

		// Token: 0x02000405 RID: 1029
		// (Invoke) Token: 0x06001627 RID: 5671
		[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
		[SuppressUnmanagedCodeSecurity]
		[MonoNativeFunctionWrapper]
		public delegate void GetBulkWaterLevelAtPositionsDelegate(UIntPtr scene, IntPtr waterHeightQueryArray, int arraySize, IntPtr waterHeightsAtVolumes, IntPtr waterSurfaceNormals);

		// Token: 0x02000406 RID: 1030
		// (Invoke) Token: 0x0600162B RID: 5675
		[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
		[SuppressUnmanagedCodeSecurity]
		[MonoNativeFunctionWrapper]
		public delegate void GetBulkWaterLevelAtVolumesDelegate(UIntPtr scene, UIntPtr volumeQueryDataArray, int volumeCount, in MatrixFrame entityFrame);

		// Token: 0x02000407 RID: 1031
		// (Invoke) Token: 0x0600162F RID: 5679
		[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
		[SuppressUnmanagedCodeSecurity]
		[MonoNativeFunctionWrapper]
		public delegate NativeObjectPointer GetCampaignEntityWithNameDelegate(UIntPtr scenePointer, byte[] entityName);

		// Token: 0x02000408 RID: 1032
		// (Invoke) Token: 0x06001633 RID: 5683
		[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
		[SuppressUnmanagedCodeSecurity]
		[MonoNativeFunctionWrapper]
		[return: MarshalAs(UnmanagedType.U1)]
		public delegate bool GetEnginePhysicsEnabledDelegate(UIntPtr scenePointer);

		// Token: 0x02000409 RID: 1033
		// (Invoke) Token: 0x06001637 RID: 5687
		[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
		[SuppressUnmanagedCodeSecurity]
		[MonoNativeFunctionWrapper]
		public delegate void GetEntitiesDelegate(UIntPtr scenePointer, UIntPtr entityObjectsArrayPointer);

		// Token: 0x0200040A RID: 1034
		// (Invoke) Token: 0x0600163B RID: 5691
		[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
		[SuppressUnmanagedCodeSecurity]
		[MonoNativeFunctionWrapper]
		public delegate int GetEntityCountDelegate(UIntPtr scenePointer);

		// Token: 0x0200040B RID: 1035
		// (Invoke) Token: 0x0600163F RID: 5695
		[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
		[SuppressUnmanagedCodeSecurity]
		[MonoNativeFunctionWrapper]
		public delegate NativeObjectPointer GetEntityWithGuidDelegate(UIntPtr scenePointer, byte[] guid);

		// Token: 0x0200040C RID: 1036
		// (Invoke) Token: 0x06001643 RID: 5699
		[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
		[SuppressUnmanagedCodeSecurity]
		[MonoNativeFunctionWrapper]
		public delegate float GetFallDensityDelegate(UIntPtr scenepTR);

		// Token: 0x0200040D RID: 1037
		// (Invoke) Token: 0x06001647 RID: 5703
		[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
		[SuppressUnmanagedCodeSecurity]
		[MonoNativeFunctionWrapper]
		public delegate NativeObjectPointer GetFirstEntityWithNameDelegate(UIntPtr scenePointer, byte[] entityName);

		// Token: 0x0200040E RID: 1038
		// (Invoke) Token: 0x0600164B RID: 5707
		[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
		[SuppressUnmanagedCodeSecurity]
		[MonoNativeFunctionWrapper]
		public delegate NativeObjectPointer GetFirstEntityWithScriptComponentDelegate(UIntPtr scenePointer, byte[] scriptComponentName);

		// Token: 0x0200040F RID: 1039
		// (Invoke) Token: 0x0600164F RID: 5711
		[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
		[SuppressUnmanagedCodeSecurity]
		[MonoNativeFunctionWrapper]
		public delegate int GetFloraInstanceCountDelegate(UIntPtr scenePointer);

		// Token: 0x02000410 RID: 1040
		// (Invoke) Token: 0x06001653 RID: 5715
		[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
		[SuppressUnmanagedCodeSecurity]
		[MonoNativeFunctionWrapper]
		public delegate int GetFloraRendererTextureUsageDelegate(UIntPtr scenePointer);

		// Token: 0x02000411 RID: 1041
		// (Invoke) Token: 0x06001657 RID: 5719
		[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
		[SuppressUnmanagedCodeSecurity]
		[MonoNativeFunctionWrapper]
		public delegate float GetFogDelegate(UIntPtr scenePointer);

		// Token: 0x02000412 RID: 1042
		// (Invoke) Token: 0x0600165B RID: 5723
		[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
		[SuppressUnmanagedCodeSecurity]
		[MonoNativeFunctionWrapper]
		public delegate Vec2 GetGlobalWindStrengthVectorDelegate(UIntPtr scenePointer);

		// Token: 0x02000413 RID: 1043
		// (Invoke) Token: 0x0600165F RID: 5727
		[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
		[SuppressUnmanagedCodeSecurity]
		[MonoNativeFunctionWrapper]
		public delegate Vec2 GetGlobalWindVelocityDelegate(UIntPtr scenePointer);

		// Token: 0x02000414 RID: 1044
		// (Invoke) Token: 0x06001663 RID: 5731
		[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
		[SuppressUnmanagedCodeSecurity]
		[MonoNativeFunctionWrapper]
		public delegate float GetGroundHeightAndBodyFlagsAtPositionDelegate(UIntPtr scenePointer, Vec3 position, out BodyFlags contactPointFlags, BodyFlags excludeFlags);

		// Token: 0x02000415 RID: 1045
		// (Invoke) Token: 0x06001667 RID: 5735
		[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
		[SuppressUnmanagedCodeSecurity]
		[MonoNativeFunctionWrapper]
		public delegate float GetGroundHeightAndNormalAtPositionDelegate(UIntPtr scenePointer, Vec3 position, ref Vec3 normal, uint excludeFlags);

		// Token: 0x02000416 RID: 1046
		// (Invoke) Token: 0x0600166B RID: 5739
		[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
		[SuppressUnmanagedCodeSecurity]
		[MonoNativeFunctionWrapper]
		public delegate float GetGroundHeightAtPositionDelegate(UIntPtr scenePointer, Vec3 position, uint excludeFlags);

		// Token: 0x02000417 RID: 1047
		// (Invoke) Token: 0x0600166F RID: 5743
		[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
		[SuppressUnmanagedCodeSecurity]
		[MonoNativeFunctionWrapper]
		public delegate Vec2 GetHardBoundaryVertexDelegate(UIntPtr scenePointer, int index);

		// Token: 0x02000418 RID: 1048
		// (Invoke) Token: 0x06001673 RID: 5747
		[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
		[SuppressUnmanagedCodeSecurity]
		[MonoNativeFunctionWrapper]
		public delegate int GetHardBoundaryVertexCountDelegate(UIntPtr scenePointer);

		// Token: 0x02000419 RID: 1049
		// (Invoke) Token: 0x06001677 RID: 5751
		[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
		[SuppressUnmanagedCodeSecurity]
		[MonoNativeFunctionWrapper]
		[return: MarshalAs(UnmanagedType.U1)]
		public delegate bool GetHeightAtPointDelegate(UIntPtr scenePointer, Vec2 point, BodyFlags excludeBodyFlags, ref float height);

		// Token: 0x0200041A RID: 1050
		// (Invoke) Token: 0x0600167B RID: 5755
		[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
		[SuppressUnmanagedCodeSecurity]
		[MonoNativeFunctionWrapper]
		public delegate int GetIdOfNavMeshFaceDelegate(UIntPtr scenePointer, int navMeshFace);

		// Token: 0x0200041B RID: 1051
		// (Invoke) Token: 0x0600167F RID: 5759
		[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
		[SuppressUnmanagedCodeSecurity]
		[MonoNativeFunctionWrapper]
		public delegate void GetInterpolationFactorForBodyWorldTransformSmoothingDelegate(UIntPtr scene, out float interpolationFactor, out float fixedDt);

		// Token: 0x0200041C RID: 1052
		// (Invoke) Token: 0x06001683 RID: 5763
		[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
		[SuppressUnmanagedCodeSecurity]
		[MonoNativeFunctionWrapper]
		public delegate void GetLastFinalRenderCameraFrameDelegate(UIntPtr scenePointer, ref MatrixFrame outFrame);

		// Token: 0x0200041D RID: 1053
		// (Invoke) Token: 0x06001687 RID: 5767
		[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
		[SuppressUnmanagedCodeSecurity]
		[MonoNativeFunctionWrapper]
		public delegate Vec3 GetLastFinalRenderCameraPositionDelegate(UIntPtr scenePointer);

		// Token: 0x0200041E RID: 1054
		// (Invoke) Token: 0x0600168B RID: 5771
		[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
		[SuppressUnmanagedCodeSecurity]
		[MonoNativeFunctionWrapper]
		public delegate Vec2 GetLastPointOnNavigationMeshFromPositionToDestinationDelegate(UIntPtr scenePointer, int startingFace, Vec2 position, Vec2 destination, IntPtr exclusionGroupIds, int exclusionGroupIdsCount);

		// Token: 0x0200041F RID: 1055
		// (Invoke) Token: 0x0600168F RID: 5775
		[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
		[SuppressUnmanagedCodeSecurity]
		[MonoNativeFunctionWrapper]
		public delegate Vec3 GetLastPointOnNavigationMeshFromWorldPositionToDestinationDelegate(UIntPtr scenePointer, ref WorldPosition position, Vec2 destination);

		// Token: 0x02000420 RID: 1056
		// (Invoke) Token: 0x06001693 RID: 5779
		[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
		[SuppressUnmanagedCodeSecurity]
		[MonoNativeFunctionWrapper]
		public delegate Vec2 GetLastPositionOnNavMeshFaceForPointAndDirectionDelegate(UIntPtr scenePointer, in PathFaceRecord record, Vec2 position, Vec2 direction);

		// Token: 0x02000421 RID: 1057
		// (Invoke) Token: 0x06001697 RID: 5783
		[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
		[SuppressUnmanagedCodeSecurity]
		[MonoNativeFunctionWrapper]
		public delegate int GetLoadingStateNameDelegate(UIntPtr scene);

		// Token: 0x02000422 RID: 1058
		// (Invoke) Token: 0x0600169B RID: 5787
		[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
		[SuppressUnmanagedCodeSecurity]
		[MonoNativeFunctionWrapper]
		public delegate int GetModulePathDelegate(UIntPtr scenePointer);

		// Token: 0x02000423 RID: 1059
		// (Invoke) Token: 0x0600169F RID: 5791
		[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
		[SuppressUnmanagedCodeSecurity]
		[MonoNativeFunctionWrapper]
		public delegate int GetNameDelegate(UIntPtr scenePointer);

		// Token: 0x02000424 RID: 1060
		// (Invoke) Token: 0x060016A3 RID: 5795
		[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
		[SuppressUnmanagedCodeSecurity]
		[MonoNativeFunctionWrapper]
		public delegate uint GetNavigationMeshCRCDelegate(UIntPtr scenePointer);

		// Token: 0x02000425 RID: 1061
		// (Invoke) Token: 0x060016A7 RID: 5799
		[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
		[SuppressUnmanagedCodeSecurity]
		[MonoNativeFunctionWrapper]
		public delegate UIntPtr GetNavigationMeshForPositionDelegate(UIntPtr scenePointer, in Vec3 position, ref int faceGroupId, float heightDifferenceLimit, [MarshalAs(UnmanagedType.U1)] bool excludeDynamicNavigationMeshes);

		// Token: 0x02000426 RID: 1062
		// (Invoke) Token: 0x060016AB RID: 5803
		[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
		[SuppressUnmanagedCodeSecurity]
		[MonoNativeFunctionWrapper]
		public delegate void GetNavMeshFaceCenterPositionDelegate(UIntPtr scenePointer, int navMeshFace, ref Vec3 centerPos);

		// Token: 0x02000427 RID: 1063
		// (Invoke) Token: 0x060016AF RID: 5807
		[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
		[SuppressUnmanagedCodeSecurity]
		[MonoNativeFunctionWrapper]
		public delegate int GetNavMeshFaceCountDelegate(UIntPtr scenePointer);

		// Token: 0x02000428 RID: 1064
		// (Invoke) Token: 0x060016B3 RID: 5811
		[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
		[SuppressUnmanagedCodeSecurity]
		[MonoNativeFunctionWrapper]
		public delegate int GetNavmeshFaceCountBetweenTwoIdsDelegate(UIntPtr scenePointer, int firstId, int secondId);

		// Token: 0x02000429 RID: 1065
		// (Invoke) Token: 0x060016B7 RID: 5815
		[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
		[SuppressUnmanagedCodeSecurity]
		[MonoNativeFunctionWrapper]
		public delegate float GetNavMeshFaceFirstVertexZDelegate(UIntPtr scenePointer, int navMeshFaceIndex);

		// Token: 0x0200042A RID: 1066
		// (Invoke) Token: 0x060016BB RID: 5819
		[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
		[SuppressUnmanagedCodeSecurity]
		[MonoNativeFunctionWrapper]
		public delegate void GetNavMeshFaceIndexDelegate(UIntPtr scenePointer, ref PathFaceRecord record, Vec2 position, [MarshalAs(UnmanagedType.U1)] bool isRegion1, [MarshalAs(UnmanagedType.U1)] bool checkIfDisabled, [MarshalAs(UnmanagedType.U1)] bool ignoreHeight);

		// Token: 0x0200042B RID: 1067
		// (Invoke) Token: 0x060016BF RID: 5823
		[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
		[SuppressUnmanagedCodeSecurity]
		[MonoNativeFunctionWrapper]
		public delegate void GetNavMeshFaceIndex3Delegate(UIntPtr scenePointer, ref PathFaceRecord record, Vec3 position, [MarshalAs(UnmanagedType.U1)] bool checkIfDisabled);

		// Token: 0x0200042C RID: 1068
		// (Invoke) Token: 0x060016C3 RID: 5827
		[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
		[SuppressUnmanagedCodeSecurity]
		[MonoNativeFunctionWrapper]
		public delegate void GetNavmeshFaceRecordsBetweenTwoIdsDelegate(UIntPtr scenePointer, int firstId, int secondId, IntPtr faceRecords);

		// Token: 0x0200042D RID: 1069
		// (Invoke) Token: 0x060016C7 RID: 5831
		[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
		[SuppressUnmanagedCodeSecurity]
		[MonoNativeFunctionWrapper]
		public delegate PathFaceRecord GetNavMeshPathFaceRecordDelegate(UIntPtr scenePointer, int navMeshFace);

		// Token: 0x0200042E RID: 1070
		// (Invoke) Token: 0x060016CB RID: 5835
		[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
		[SuppressUnmanagedCodeSecurity]
		[MonoNativeFunctionWrapper]
		public delegate UIntPtr GetNearestNavigationMeshForPositionDelegate(UIntPtr scenePointer, in Vec3 position, float heightDifferenceLimit, [MarshalAs(UnmanagedType.U1)] bool excludeDynamicNavigationMeshes);

		// Token: 0x0200042F RID: 1071
		// (Invoke) Token: 0x060016CF RID: 5839
		[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
		[SuppressUnmanagedCodeSecurity]
		[MonoNativeFunctionWrapper]
		public delegate int GetNodeDataCountDelegate(UIntPtr scene, int xIndex, int yIndex);

		// Token: 0x02000430 RID: 1072
		// (Invoke) Token: 0x060016D3 RID: 5843
		[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
		[SuppressUnmanagedCodeSecurity]
		[MonoNativeFunctionWrapper]
		public delegate Vec3 GetNormalAtDelegate(UIntPtr scenePointer, Vec2 position);

		// Token: 0x02000431 RID: 1073
		// (Invoke) Token: 0x060016D7 RID: 5847
		[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
		[SuppressUnmanagedCodeSecurity]
		[MonoNativeFunctionWrapper]
		public delegate float GetNorthAngleDelegate(UIntPtr scenePointer);

		// Token: 0x02000432 RID: 1074
		// (Invoke) Token: 0x060016DB RID: 5851
		[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
		[SuppressUnmanagedCodeSecurity]
		[MonoNativeFunctionWrapper]
		public delegate int GetNumberOfPathsWithNamePrefixDelegate(UIntPtr ptr, byte[] prefix);

		// Token: 0x02000433 RID: 1075
		// (Invoke) Token: 0x060016DF RID: 5855
		[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
		[SuppressUnmanagedCodeSecurity]
		[MonoNativeFunctionWrapper]
		[return: MarshalAs(UnmanagedType.U1)]
		public delegate bool GetPathBetweenAIFaceIndicesDelegate(UIntPtr scenePointer, int startingAiFace, int endingAiFace, Vec2 startingPosition, Vec2 endingPosition, float agentRadius, IntPtr result, ref int pathSize, IntPtr exclusionGroupIds, int exclusionGroupIdsCount, float extraCostMultiplier);

		// Token: 0x02000434 RID: 1076
		// (Invoke) Token: 0x060016E3 RID: 5859
		[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
		[SuppressUnmanagedCodeSecurity]
		[MonoNativeFunctionWrapper]
		[return: MarshalAs(UnmanagedType.U1)]
		public delegate bool GetPathBetweenAIFaceIndicesWithRegionSwitchCostDelegate(UIntPtr scenePointer, int startingAiFace, int endingAiFace, Vec2 startingPosition, Vec2 endingPosition, float agentRadius, IntPtr result, ref int pathSize, IntPtr exclusionGroupIds, int exclusionGroupIdsCount, float extraCostMultiplier, int regionSwitchCostTo0, int regionSwitchCostTo1);

		// Token: 0x02000435 RID: 1077
		// (Invoke) Token: 0x060016E7 RID: 5863
		[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
		[SuppressUnmanagedCodeSecurity]
		[MonoNativeFunctionWrapper]
		[return: MarshalAs(UnmanagedType.U1)]
		public delegate bool GetPathBetweenAIFacePointersDelegate(UIntPtr scenePointer, UIntPtr startingAiFace, UIntPtr endingAiFace, Vec2 startingPosition, Vec2 endingPosition, float agentRadius, IntPtr result, ref int pathSize, IntPtr exclusionGroupIds, int exclusionGroupIdsCount);

		// Token: 0x02000436 RID: 1078
		// (Invoke) Token: 0x060016EB RID: 5867
		[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
		[SuppressUnmanagedCodeSecurity]
		[MonoNativeFunctionWrapper]
		[return: MarshalAs(UnmanagedType.U1)]
		public delegate bool GetPathBetweenAIFacePointersWithRegionSwitchCostDelegate(UIntPtr scenePointer, UIntPtr startingAiFace, UIntPtr endingAiFace, Vec2 startingPosition, Vec2 endingPosition, float agentRadius, IntPtr result, ref int pathSize, IntPtr exclusionGroupIds, int exclusionGroupIdsCount, int regionSwitchCostTo0, int regionSwitchCostTo1);

		// Token: 0x02000437 RID: 1079
		// (Invoke) Token: 0x060016EF RID: 5871
		[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
		[SuppressUnmanagedCodeSecurity]
		[MonoNativeFunctionWrapper]
		[return: MarshalAs(UnmanagedType.U1)]
		public delegate bool GetPathDistanceBetweenAIFacesDelegate(UIntPtr scenePointer, int startingAiFace, int endingAiFace, Vec2 startingPosition, Vec2 endingPosition, float agentRadius, float distanceLimit, out float distance, IntPtr exclusionGroupIds, int exclusionGroupIdsCount, int regionSwitchCostTo0, int regionSwitchCostTo1);

		// Token: 0x02000438 RID: 1080
		// (Invoke) Token: 0x060016F3 RID: 5875
		[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
		[SuppressUnmanagedCodeSecurity]
		[MonoNativeFunctionWrapper]
		[return: MarshalAs(UnmanagedType.U1)]
		public delegate bool GetPathDistanceBetweenPositionsDelegate(UIntPtr scenePointer, ref WorldPosition position, ref WorldPosition destination, float agentRadius, ref float pathLength);

		// Token: 0x02000439 RID: 1081
		// (Invoke) Token: 0x060016F7 RID: 5879
		[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
		[SuppressUnmanagedCodeSecurity]
		[MonoNativeFunctionWrapper]
		public delegate PathFaceRecord GetPathFaceRecordFromNavMeshFacePointerDelegate(UIntPtr scenePointer, UIntPtr navMeshFacePointer);

		// Token: 0x0200043A RID: 1082
		// (Invoke) Token: 0x060016FB RID: 5883
		[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
		[SuppressUnmanagedCodeSecurity]
		[MonoNativeFunctionWrapper]
		public delegate void GetPathsWithNamePrefixDelegate(UIntPtr ptr, IntPtr points, byte[] prefix);

		// Token: 0x0200043B RID: 1083
		// (Invoke) Token: 0x060016FF RID: 5887
		[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
		[SuppressUnmanagedCodeSecurity]
		[MonoNativeFunctionWrapper]
		public delegate NativeObjectPointer GetPathWithNameDelegate(UIntPtr scenePointer, byte[] name);

		// Token: 0x0200043C RID: 1084
		// (Invoke) Token: 0x06001703 RID: 5891
		[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
		[SuppressUnmanagedCodeSecurity]
		[MonoNativeFunctionWrapper]
		public delegate void GetPhotoModeFocusDelegate(UIntPtr scene, ref float focus, ref float focusStart, ref float focusEnd, ref float exposure, [MarshalAs(UnmanagedType.U1)] ref bool vignetteOn);

		// Token: 0x0200043D RID: 1085
		// (Invoke) Token: 0x06001707 RID: 5895
		[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
		[SuppressUnmanagedCodeSecurity]
		[MonoNativeFunctionWrapper]
		public delegate float GetPhotoModeFovDelegate(UIntPtr scene);

		// Token: 0x0200043E RID: 1086
		// (Invoke) Token: 0x0600170B RID: 5899
		[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
		[SuppressUnmanagedCodeSecurity]
		[MonoNativeFunctionWrapper]
		[return: MarshalAs(UnmanagedType.U1)]
		public delegate bool GetPhotoModeOnDelegate(UIntPtr scene);

		// Token: 0x0200043F RID: 1087
		// (Invoke) Token: 0x0600170F RID: 5903
		[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
		[SuppressUnmanagedCodeSecurity]
		[MonoNativeFunctionWrapper]
		[return: MarshalAs(UnmanagedType.U1)]
		public delegate bool GetPhotoModeOrbitDelegate(UIntPtr scene);

		// Token: 0x02000440 RID: 1088
		// (Invoke) Token: 0x06001713 RID: 5907
		[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
		[SuppressUnmanagedCodeSecurity]
		[MonoNativeFunctionWrapper]
		public delegate float GetPhotoModeRollDelegate(UIntPtr scene);

		// Token: 0x02000441 RID: 1089
		// (Invoke) Token: 0x06001717 RID: 5911
		[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
		[SuppressUnmanagedCodeSecurity]
		[MonoNativeFunctionWrapper]
		public delegate void GetPhysicsMinMaxDelegate(UIntPtr scenePointer, ref Vec3 min_max);

		// Token: 0x02000442 RID: 1090
		// (Invoke) Token: 0x0600171B RID: 5915
		[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
		[SuppressUnmanagedCodeSecurity]
		[MonoNativeFunctionWrapper]
		public delegate float GetRainDensityDelegate(UIntPtr scenePointer);

		// Token: 0x02000443 RID: 1091
		// (Invoke) Token: 0x0600171F RID: 5919
		[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
		[SuppressUnmanagedCodeSecurity]
		[MonoNativeFunctionWrapper]
		public delegate void GetRootEntitiesDelegate(UIntPtr scene, UIntPtr output);

		// Token: 0x02000444 RID: 1092
		// (Invoke) Token: 0x06001723 RID: 5923
		[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
		[SuppressUnmanagedCodeSecurity]
		[MonoNativeFunctionWrapper]
		public delegate int GetRootEntityCountDelegate(UIntPtr scenePointer);

		// Token: 0x02000445 RID: 1093
		// (Invoke) Token: 0x06001727 RID: 5927
		[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
		[SuppressUnmanagedCodeSecurity]
		[MonoNativeFunctionWrapper]
		public delegate int GetSceneColorGradeIndexDelegate(UIntPtr scene);

		// Token: 0x02000446 RID: 1094
		// (Invoke) Token: 0x0600172B RID: 5931
		[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
		[SuppressUnmanagedCodeSecurity]
		[MonoNativeFunctionWrapper]
		public delegate int GetSceneFilterIndexDelegate(UIntPtr scene);

		// Token: 0x02000447 RID: 1095
		// (Invoke) Token: 0x0600172F RID: 5935
		[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
		[SuppressUnmanagedCodeSecurity]
		[MonoNativeFunctionWrapper]
		public delegate void GetSceneLimitsDelegate(UIntPtr scenePointer, ref Vec3 min, ref Vec3 max);

		// Token: 0x02000448 RID: 1096
		// (Invoke) Token: 0x06001733 RID: 5939
		[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
		[SuppressUnmanagedCodeSecurity]
		[MonoNativeFunctionWrapper]
		public delegate uint GetSceneXMLCRCDelegate(UIntPtr scenePointer);

		// Token: 0x02000449 RID: 1097
		// (Invoke) Token: 0x06001737 RID: 5943
		[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
		[SuppressUnmanagedCodeSecurity]
		[MonoNativeFunctionWrapper]
		public delegate NativeObjectPointer GetScriptedEntityDelegate(UIntPtr scenePointer, int index);

		// Token: 0x0200044A RID: 1098
		// (Invoke) Token: 0x0600173B RID: 5947
		[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
		[SuppressUnmanagedCodeSecurity]
		[MonoNativeFunctionWrapper]
		public delegate int GetScriptedEntityCountDelegate(UIntPtr scenePointer);

		// Token: 0x0200044B RID: 1099
		// (Invoke) Token: 0x0600173F RID: 5951
		[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
		[SuppressUnmanagedCodeSecurity]
		[MonoNativeFunctionWrapper]
		public delegate NativeObjectPointer GetSkyboxMeshDelegate(UIntPtr ptr);

		// Token: 0x0200044C RID: 1100
		// (Invoke) Token: 0x06001743 RID: 5955
		[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
		[SuppressUnmanagedCodeSecurity]
		[MonoNativeFunctionWrapper]
		public delegate float GetSnowDensityDelegate(UIntPtr scenePointer);

		// Token: 0x0200044D RID: 1101
		// (Invoke) Token: 0x06001747 RID: 5959
		[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
		[SuppressUnmanagedCodeSecurity]
		[MonoNativeFunctionWrapper]
		public delegate Vec2 GetSoftBoundaryVertexDelegate(UIntPtr scenePointer, int index);

		// Token: 0x0200044E RID: 1102
		// (Invoke) Token: 0x0600174B RID: 5963
		[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
		[SuppressUnmanagedCodeSecurity]
		[MonoNativeFunctionWrapper]
		public delegate int GetSoftBoundaryVertexCountDelegate(UIntPtr scenePointer);

		// Token: 0x0200044F RID: 1103
		// (Invoke) Token: 0x0600174F RID: 5967
		[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
		[SuppressUnmanagedCodeSecurity]
		[MonoNativeFunctionWrapper]
		public delegate Vec3 GetSunDirectionDelegate(UIntPtr scenePointer);

		// Token: 0x02000450 RID: 1104
		// (Invoke) Token: 0x06001753 RID: 5971
		[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
		[SuppressUnmanagedCodeSecurity]
		[MonoNativeFunctionWrapper]
		public delegate void GetTerrainDataDelegate(UIntPtr scene, out Vec2i nodeDimension, out float nodeSize, out int layerCount, out int layerVersion);

		// Token: 0x02000451 RID: 1105
		// (Invoke) Token: 0x06001757 RID: 5975
		[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
		[SuppressUnmanagedCodeSecurity]
		[MonoNativeFunctionWrapper]
		public delegate float GetTerrainHeightDelegate(UIntPtr scenePointer, Vec2 position, [MarshalAs(UnmanagedType.U1)] bool checkHoles);

		// Token: 0x02000452 RID: 1106
		// (Invoke) Token: 0x0600175B RID: 5979
		[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
		[SuppressUnmanagedCodeSecurity]
		[MonoNativeFunctionWrapper]
		public delegate void GetTerrainHeightAndNormalDelegate(UIntPtr scenePointer, Vec2 position, out float height, out Vec3 normal);

		// Token: 0x02000453 RID: 1107
		// (Invoke) Token: 0x0600175F RID: 5983
		[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
		[SuppressUnmanagedCodeSecurity]
		[MonoNativeFunctionWrapper]
		public delegate int GetTerrainMemoryUsageDelegate(UIntPtr scenePointer);

		// Token: 0x02000454 RID: 1108
		// (Invoke) Token: 0x06001763 RID: 5987
		[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
		[SuppressUnmanagedCodeSecurity]
		[MonoNativeFunctionWrapper]
		[return: MarshalAs(UnmanagedType.U1)]
		public delegate bool GetTerrainMinMaxHeightDelegate(UIntPtr scene, ref float min, ref float max);

		// Token: 0x02000455 RID: 1109
		// (Invoke) Token: 0x06001767 RID: 5991
		[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
		[SuppressUnmanagedCodeSecurity]
		[MonoNativeFunctionWrapper]
		public delegate void GetTerrainNodeDataDelegate(UIntPtr scene, int xIndex, int yIndex, out int vertexCountAlongAxis, out float quadLength, out float minHeight, out float maxHeight);

		// Token: 0x02000456 RID: 1110
		// (Invoke) Token: 0x0600176B RID: 5995
		[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
		[SuppressUnmanagedCodeSecurity]
		[MonoNativeFunctionWrapper]
		public delegate int GetTerrainPhysicsMaterialIndexAtLayerDelegate(UIntPtr scene, int layerIndex);

		// Token: 0x02000457 RID: 1111
		// (Invoke) Token: 0x0600176F RID: 5999
		[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
		[SuppressUnmanagedCodeSecurity]
		[MonoNativeFunctionWrapper]
		public delegate float GetTimeOfDayDelegate(UIntPtr scenePointer);

		// Token: 0x02000458 RID: 1112
		// (Invoke) Token: 0x06001773 RID: 6003
		[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
		[SuppressUnmanagedCodeSecurity]
		[MonoNativeFunctionWrapper]
		public delegate float GetTimeSpeedDelegate(UIntPtr scenePointer);

		// Token: 0x02000459 RID: 1113
		// (Invoke) Token: 0x06001777 RID: 6007
		[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
		[SuppressUnmanagedCodeSecurity]
		[MonoNativeFunctionWrapper]
		public delegate int GetUpgradeLevelCountDelegate(UIntPtr scenePointer);

		// Token: 0x0200045A RID: 1114
		// (Invoke) Token: 0x0600177B RID: 6011
		[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
		[SuppressUnmanagedCodeSecurity]
		[MonoNativeFunctionWrapper]
		public delegate uint GetUpgradeLevelMaskDelegate(UIntPtr scenePointer);

		// Token: 0x0200045B RID: 1115
		// (Invoke) Token: 0x0600177F RID: 6015
		[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
		[SuppressUnmanagedCodeSecurity]
		[MonoNativeFunctionWrapper]
		public delegate uint GetUpgradeLevelMaskOfLevelNameDelegate(UIntPtr scenePointer, byte[] levelName);

		// Token: 0x0200045C RID: 1116
		// (Invoke) Token: 0x06001783 RID: 6019
		[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
		[SuppressUnmanagedCodeSecurity]
		[MonoNativeFunctionWrapper]
		public delegate int GetUpgradeLevelNameOfIndexDelegate(UIntPtr scenePointer, int index);

		// Token: 0x0200045D RID: 1117
		// (Invoke) Token: 0x06001787 RID: 6023
		[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
		[SuppressUnmanagedCodeSecurity]
		[MonoNativeFunctionWrapper]
		public delegate float GetWaterLevelDelegate(UIntPtr scene);

		// Token: 0x0200045E RID: 1118
		// (Invoke) Token: 0x0600178B RID: 6027
		[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
		[SuppressUnmanagedCodeSecurity]
		[MonoNativeFunctionWrapper]
		public delegate float GetWaterLevelAtPositionDelegate(UIntPtr scene, Vec2 position, [MarshalAs(UnmanagedType.U1)] bool useWaterRenderer, [MarshalAs(UnmanagedType.U1)] bool checkWaterBodyEntities);

		// Token: 0x0200045F RID: 1119
		// (Invoke) Token: 0x0600178F RID: 6031
		[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
		[SuppressUnmanagedCodeSecurity]
		[MonoNativeFunctionWrapper]
		public delegate Vec3 GetWaterSpeedAtPositionDelegate(UIntPtr scenePointer, in Vec2 position, [MarshalAs(UnmanagedType.U1)] bool doChoppinessCorrection);

		// Token: 0x02000460 RID: 1120
		// (Invoke) Token: 0x06001793 RID: 6035
		[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
		[SuppressUnmanagedCodeSecurity]
		[MonoNativeFunctionWrapper]
		public delegate float GetWaterStrengthDelegate(UIntPtr scene);

		// Token: 0x02000461 RID: 1121
		// (Invoke) Token: 0x06001797 RID: 6039
		[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
		[SuppressUnmanagedCodeSecurity]
		[MonoNativeFunctionWrapper]
		public delegate void GetWindFlowMapDataDelegate(UIntPtr scenePointer, IntPtr flowmapData);

		// Token: 0x02000462 RID: 1122
		// (Invoke) Token: 0x0600179B RID: 6043
		[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
		[SuppressUnmanagedCodeSecurity]
		[MonoNativeFunctionWrapper]
		public delegate float GetWinterTimeFactorDelegate(UIntPtr scenePointer);

		// Token: 0x02000463 RID: 1123
		// (Invoke) Token: 0x0600179F RID: 6047
		[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
		[SuppressUnmanagedCodeSecurity]
		[MonoNativeFunctionWrapper]
		[return: MarshalAs(UnmanagedType.U1)]
		public delegate bool HasDecalRendererDelegate(UIntPtr scenePointer);

		// Token: 0x02000464 RID: 1124
		// (Invoke) Token: 0x060017A3 RID: 6051
		[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
		[SuppressUnmanagedCodeSecurity]
		[MonoNativeFunctionWrapper]
		[return: MarshalAs(UnmanagedType.U1)]
		public delegate bool HasNavmeshFaceUnsharedEdgesDelegate(UIntPtr scenePointer, in PathFaceRecord faceRecord);

		// Token: 0x02000465 RID: 1125
		// (Invoke) Token: 0x060017A7 RID: 6055
		[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
		[SuppressUnmanagedCodeSecurity]
		[MonoNativeFunctionWrapper]
		[return: MarshalAs(UnmanagedType.U1)]
		public delegate bool HasTerrainHeightmapDelegate(UIntPtr scenePointer);

		// Token: 0x02000466 RID: 1126
		// (Invoke) Token: 0x060017AB RID: 6059
		[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
		[SuppressUnmanagedCodeSecurity]
		[MonoNativeFunctionWrapper]
		public delegate void InvalidateTerrainPhysicsMaterialsDelegate(UIntPtr scenePointer);

		// Token: 0x02000467 RID: 1127
		// (Invoke) Token: 0x060017AF RID: 6063
		[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
		[SuppressUnmanagedCodeSecurity]
		[MonoNativeFunctionWrapper]
		[return: MarshalAs(UnmanagedType.U1)]
		public delegate bool IsAnyFaceWithIdDelegate(UIntPtr scenePointer, int faceGroupId);

		// Token: 0x02000468 RID: 1128
		// (Invoke) Token: 0x060017B3 RID: 6067
		[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
		[SuppressUnmanagedCodeSecurity]
		[MonoNativeFunctionWrapper]
		[return: MarshalAs(UnmanagedType.U1)]
		public delegate bool IsAtmosphereIndoorDelegate(UIntPtr scenePointer);

		// Token: 0x02000469 RID: 1129
		// (Invoke) Token: 0x060017B7 RID: 6071
		[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
		[SuppressUnmanagedCodeSecurity]
		[MonoNativeFunctionWrapper]
		[return: MarshalAs(UnmanagedType.U1)]
		public delegate bool IsDefaultEditorSceneDelegate(UIntPtr scene);

		// Token: 0x0200046A RID: 1130
		// (Invoke) Token: 0x060017BB RID: 6075
		[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
		[SuppressUnmanagedCodeSecurity]
		[MonoNativeFunctionWrapper]
		[return: MarshalAs(UnmanagedType.U1)]
		public delegate bool IsEditorSceneDelegate(UIntPtr scenePointer);

		// Token: 0x0200046B RID: 1131
		// (Invoke) Token: 0x060017BF RID: 6079
		[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
		[SuppressUnmanagedCodeSecurity]
		[MonoNativeFunctionWrapper]
		[return: MarshalAs(UnmanagedType.U1)]
		public delegate bool IsLineToPointClearDelegate(UIntPtr scenePointer, int startingFace, Vec2 position, Vec2 destination, float agentRadius);

		// Token: 0x0200046C RID: 1132
		// (Invoke) Token: 0x060017C3 RID: 6083
		[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
		[SuppressUnmanagedCodeSecurity]
		[MonoNativeFunctionWrapper]
		[return: MarshalAs(UnmanagedType.U1)]
		public delegate bool IsLineToPointClear2Delegate(UIntPtr scenePointer, UIntPtr startingFace, Vec2 position, Vec2 destination, float agentRadius);

		// Token: 0x0200046D RID: 1133
		// (Invoke) Token: 0x060017C7 RID: 6087
		[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
		[SuppressUnmanagedCodeSecurity]
		[MonoNativeFunctionWrapper]
		[return: MarshalAs(UnmanagedType.U1)]
		public delegate bool IsLoadingFinishedDelegate(UIntPtr scene);

		// Token: 0x0200046E RID: 1134
		// (Invoke) Token: 0x060017CB RID: 6091
		[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
		[SuppressUnmanagedCodeSecurity]
		[MonoNativeFunctionWrapper]
		[return: MarshalAs(UnmanagedType.U1)]
		public delegate bool IsMultiplayerSceneDelegate(UIntPtr scene);

		// Token: 0x0200046F RID: 1135
		// (Invoke) Token: 0x060017CF RID: 6095
		[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
		[SuppressUnmanagedCodeSecurity]
		[MonoNativeFunctionWrapper]
		[return: MarshalAs(UnmanagedType.U1)]
		public delegate bool IsPositionOnADynamicNavMeshDelegate(UIntPtr scenePointer, Vec3 position);

		// Token: 0x02000470 RID: 1136
		// (Invoke) Token: 0x060017D3 RID: 6099
		[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
		[SuppressUnmanagedCodeSecurity]
		[MonoNativeFunctionWrapper]
		public delegate void LoadNavMeshPrefabDelegate(UIntPtr scenePointer, byte[] navMeshPrefabName, int navMeshGroupIdShift);

		// Token: 0x02000471 RID: 1137
		// (Invoke) Token: 0x060017D7 RID: 6103
		[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
		[SuppressUnmanagedCodeSecurity]
		[MonoNativeFunctionWrapper]
		public delegate void LoadNavMeshPrefabWithFrameDelegate(UIntPtr scenePointer, byte[] navMeshPrefabName, MatrixFrame frame);

		// Token: 0x02000472 RID: 1138
		// (Invoke) Token: 0x060017DB RID: 6107
		[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
		[SuppressUnmanagedCodeSecurity]
		[MonoNativeFunctionWrapper]
		public delegate void MarkFacesWithIdAsLadderDelegate(UIntPtr scenePointer, int faceGroupId, [MarshalAs(UnmanagedType.U1)] bool isLadder);

		// Token: 0x02000473 RID: 1139
		// (Invoke) Token: 0x060017DF RID: 6111
		[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
		[SuppressUnmanagedCodeSecurity]
		[MonoNativeFunctionWrapper]
		public delegate void MergeFacesWithIdDelegate(UIntPtr scenePointer, int faceGroupId0, int faceGroupId1, int newFaceGroupId);

		// Token: 0x02000474 RID: 1140
		// (Invoke) Token: 0x060017E3 RID: 6115
		[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
		[SuppressUnmanagedCodeSecurity]
		[MonoNativeFunctionWrapper]
		public delegate void OptimizeSceneDelegate(UIntPtr scenePointer, [MarshalAs(UnmanagedType.U1)] bool optimizeFlora, [MarshalAs(UnmanagedType.U1)] bool optimizeOro);

		// Token: 0x02000475 RID: 1141
		// (Invoke) Token: 0x060017E7 RID: 6119
		[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
		[SuppressUnmanagedCodeSecurity]
		[MonoNativeFunctionWrapper]
		public delegate void PauseSceneSoundsDelegate(UIntPtr scenePointer);

		// Token: 0x02000476 RID: 1142
		// (Invoke) Token: 0x060017EB RID: 6123
		[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
		[SuppressUnmanagedCodeSecurity]
		[MonoNativeFunctionWrapper]
		public delegate void PreloadForRenderingDelegate(UIntPtr scenePointer);

		// Token: 0x02000477 RID: 1143
		// (Invoke) Token: 0x060017EF RID: 6127
		[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
		[SuppressUnmanagedCodeSecurity]
		[MonoNativeFunctionWrapper]
		[return: MarshalAs(UnmanagedType.U1)]
		public delegate bool RayCastExcludingTwoEntitiesDelegate(BodyFlags flags, UIntPtr scenePointer, in Ray ray, UIntPtr entity1, UIntPtr entity2);

		// Token: 0x02000478 RID: 1144
		// (Invoke) Token: 0x060017F3 RID: 6131
		[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
		[SuppressUnmanagedCodeSecurity]
		[MonoNativeFunctionWrapper]
		[return: MarshalAs(UnmanagedType.U1)]
		public delegate bool RayCastForClosestEntityOrTerrainDelegate(UIntPtr scenePointer, in Vec3 sourcePoint, in Vec3 targetPoint, float rayThickness, ref float collisionDistance, ref Vec3 closestPoint, ref UIntPtr entityIndex, BodyFlags bodyExcludeFlags, [MarshalAs(UnmanagedType.U1)] bool isFixedWorld);

		// Token: 0x02000479 RID: 1145
		// (Invoke) Token: 0x060017F7 RID: 6135
		[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
		[SuppressUnmanagedCodeSecurity]
		[MonoNativeFunctionWrapper]
		[return: MarshalAs(UnmanagedType.U1)]
		public delegate bool RayCastForClosestEntityOrTerrainIgnoreEntityDelegate(UIntPtr scenePointer, in Vec3 sourcePoint, in Vec3 targetPoint, float rayThickness, ref float collisionDistance, ref Vec3 closestPoint, ref UIntPtr entityIndex, BodyFlags bodyExcludeFlags, UIntPtr ignoredEntityPointer);

		// Token: 0x0200047A RID: 1146
		// (Invoke) Token: 0x060017FB RID: 6139
		[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
		[SuppressUnmanagedCodeSecurity]
		[MonoNativeFunctionWrapper]
		[return: MarshalAs(UnmanagedType.U1)]
		public delegate bool RayCastForRammingDelegate(UIntPtr scenePointer, in Vec3 sourcePoint, in Vec3 targetPoint, float rayThickness, ref float collisionDistance, ref Vec3 intersectionPoint, ref UIntPtr intersectedEntityIndex, BodyFlags bodyExcludeFlags, BodyFlags bodyIncludeFlags, UIntPtr ignoredEntityPointer);

		// Token: 0x0200047B RID: 1147
		// (Invoke) Token: 0x060017FF RID: 6143
		[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
		[SuppressUnmanagedCodeSecurity]
		[MonoNativeFunctionWrapper]
		public delegate void ReadDelegate(UIntPtr scenePointer, byte[] sceneName, ref SceneInitializationData initData, byte[] forcedAtmoName);

		// Token: 0x0200047C RID: 1148
		// (Invoke) Token: 0x06001803 RID: 6147
		[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
		[SuppressUnmanagedCodeSecurity]
		[MonoNativeFunctionWrapper]
		public delegate void ReadAndCalculateInitialCameraDelegate(UIntPtr scenePointer, ref MatrixFrame outFrame);

		// Token: 0x0200047D RID: 1149
		// (Invoke) Token: 0x06001807 RID: 6151
		[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
		[SuppressUnmanagedCodeSecurity]
		[MonoNativeFunctionWrapper]
		public delegate void ReadInModuleDelegate(UIntPtr scenePointer, byte[] sceneName, byte[] moduleId, ref SceneInitializationData initData, byte[] forcedAtmoName);

		// Token: 0x0200047E RID: 1150
		// (Invoke) Token: 0x0600180B RID: 6155
		[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
		[SuppressUnmanagedCodeSecurity]
		[MonoNativeFunctionWrapper]
		public delegate UIntPtr RegisterShipVisualToWaterRendererDelegate(UIntPtr scenePointer, UIntPtr entityPointer, in Vec3 bb);

		// Token: 0x0200047F RID: 1151
		// (Invoke) Token: 0x0600180F RID: 6159
		[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
		[SuppressUnmanagedCodeSecurity]
		[MonoNativeFunctionWrapper]
		public delegate void RemoveAlwaysRenderedSkeletonDelegate(UIntPtr scenePointer, UIntPtr skeletonPointer);

		// Token: 0x02000480 RID: 1152
		// (Invoke) Token: 0x06001813 RID: 6163
		[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
		[SuppressUnmanagedCodeSecurity]
		[MonoNativeFunctionWrapper]
		public delegate void RemoveDecalInstanceDelegate(UIntPtr scenePointer, UIntPtr decalMeshPointer, byte[] decalSetID);

		// Token: 0x02000481 RID: 1153
		// (Invoke) Token: 0x06001817 RID: 6167
		[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
		[SuppressUnmanagedCodeSecurity]
		[MonoNativeFunctionWrapper]
		public delegate void RemoveEntityDelegate(UIntPtr scenePointer, UIntPtr entityId, int removeReason);

		// Token: 0x02000482 RID: 1154
		// (Invoke) Token: 0x0600181B RID: 6171
		[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
		[SuppressUnmanagedCodeSecurity]
		[MonoNativeFunctionWrapper]
		public delegate void ResumeLoadingRenderingsDelegate(UIntPtr scenePointer);

		// Token: 0x02000483 RID: 1155
		// (Invoke) Token: 0x0600181F RID: 6175
		[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
		[SuppressUnmanagedCodeSecurity]
		[MonoNativeFunctionWrapper]
		public delegate void ResumeSceneSoundsDelegate(UIntPtr scenePointer);

		// Token: 0x02000484 RID: 1156
		// (Invoke) Token: 0x06001823 RID: 6179
		[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
		[SuppressUnmanagedCodeSecurity]
		[MonoNativeFunctionWrapper]
		public delegate void SaveNavMeshPrefabWithFrameDelegate(UIntPtr scenePointer, byte[] navMeshPrefabName, MatrixFrame frame);

		// Token: 0x02000485 RID: 1157
		// (Invoke) Token: 0x06001827 RID: 6183
		[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
		[SuppressUnmanagedCodeSecurity]
		[MonoNativeFunctionWrapper]
		[return: MarshalAs(UnmanagedType.U1)]
		public delegate bool SceneHadWaterWakeRendererDelegate(UIntPtr scenePointer);

		// Token: 0x02000486 RID: 1158
		// (Invoke) Token: 0x0600182B RID: 6187
		[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
		[SuppressUnmanagedCodeSecurity]
		[MonoNativeFunctionWrapper]
		public delegate int SelectEntitiesCollidedWithDelegate(UIntPtr scenePointer, ref Ray ray, IntPtr entityIds, IntPtr intersections);

		// Token: 0x02000487 RID: 1159
		// (Invoke) Token: 0x0600182F RID: 6191
		[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
		[SuppressUnmanagedCodeSecurity]
		[MonoNativeFunctionWrapper]
		public delegate int SelectEntitiesInBoxWithScriptComponentDelegate(UIntPtr scenePointer, ref Vec3 boundingBoxMin, ref Vec3 boundingBoxMax, IntPtr entitiesOutput, int maxCount, byte[] scriptComponentName);

		// Token: 0x02000488 RID: 1160
		// (Invoke) Token: 0x06001833 RID: 6195
		[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
		[SuppressUnmanagedCodeSecurity]
		[MonoNativeFunctionWrapper]
		public delegate void SeparateFacesWithIdDelegate(UIntPtr scenePointer, int faceGroupId0, int faceGroupId1);

		// Token: 0x02000489 RID: 1161
		// (Invoke) Token: 0x06001837 RID: 6199
		[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
		[SuppressUnmanagedCodeSecurity]
		[MonoNativeFunctionWrapper]
		public delegate void SetAberrationOffsetDelegate(UIntPtr scenePointer, float aberrationOffset);

		// Token: 0x0200048A RID: 1162
		// (Invoke) Token: 0x0600183B RID: 6203
		[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
		[SuppressUnmanagedCodeSecurity]
		[MonoNativeFunctionWrapper]
		public delegate void SetAberrationSizeDelegate(UIntPtr scenePointer, float aberrationSize);

		// Token: 0x0200048B RID: 1163
		// (Invoke) Token: 0x0600183F RID: 6207
		[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
		[SuppressUnmanagedCodeSecurity]
		[MonoNativeFunctionWrapper]
		public delegate void SetAberrationSmoothDelegate(UIntPtr scenePointer, float aberrationSmooth);

		// Token: 0x0200048C RID: 1164
		// (Invoke) Token: 0x06001843 RID: 6211
		[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
		[SuppressUnmanagedCodeSecurity]
		[MonoNativeFunctionWrapper]
		public delegate int SetAbilityOfFacesWithIdDelegate(UIntPtr scenePointer, int faceGroupId, [MarshalAs(UnmanagedType.U1)] bool isEnabled);

		// Token: 0x0200048D RID: 1165
		// (Invoke) Token: 0x06001847 RID: 6215
		[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
		[SuppressUnmanagedCodeSecurity]
		[MonoNativeFunctionWrapper]
		public delegate void SetActiveVisibilityLevelsDelegate(UIntPtr scenePointer, byte[] levelsAppended);

		// Token: 0x0200048E RID: 1166
		// (Invoke) Token: 0x0600184B RID: 6219
		[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
		[SuppressUnmanagedCodeSecurity]
		[MonoNativeFunctionWrapper]
		public delegate void SetAntialiasingModeDelegate(UIntPtr scenePointer, [MarshalAs(UnmanagedType.U1)] bool mode);

		// Token: 0x0200048F RID: 1167
		// (Invoke) Token: 0x0600184F RID: 6223
		[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
		[SuppressUnmanagedCodeSecurity]
		[MonoNativeFunctionWrapper]
		public delegate void SetAtmosphereWithNameDelegate(UIntPtr ptr, byte[] name);

		// Token: 0x02000490 RID: 1168
		// (Invoke) Token: 0x06001853 RID: 6227
		[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
		[SuppressUnmanagedCodeSecurity]
		[MonoNativeFunctionWrapper]
		public delegate void SetBloomDelegate(UIntPtr scenePointer, [MarshalAs(UnmanagedType.U1)] bool mode);

		// Token: 0x02000491 RID: 1169
		// (Invoke) Token: 0x06001857 RID: 6231
		[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
		[SuppressUnmanagedCodeSecurity]
		[MonoNativeFunctionWrapper]
		public delegate void SetBloomAmountDelegate(UIntPtr scenePointer, float bloomAmount);

		// Token: 0x02000492 RID: 1170
		// (Invoke) Token: 0x0600185B RID: 6235
		[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
		[SuppressUnmanagedCodeSecurity]
		[MonoNativeFunctionWrapper]
		public delegate void SetBloomStrengthDelegate(UIntPtr scenePointer, float bloomStrength);

		// Token: 0x02000493 RID: 1171
		// (Invoke) Token: 0x0600185F RID: 6239
		[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
		[SuppressUnmanagedCodeSecurity]
		[MonoNativeFunctionWrapper]
		public delegate void SetBrightpassTresholdDelegate(UIntPtr scenePointer, float threshold);

		// Token: 0x02000494 RID: 1172
		// (Invoke) Token: 0x06001863 RID: 6243
		[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
		[SuppressUnmanagedCodeSecurity]
		[MonoNativeFunctionWrapper]
		public delegate void SetClothSimulationStateDelegate(UIntPtr scenePointer, [MarshalAs(UnmanagedType.U1)] bool state);

		// Token: 0x02000495 RID: 1173
		// (Invoke) Token: 0x06001867 RID: 6247
		[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
		[SuppressUnmanagedCodeSecurity]
		[MonoNativeFunctionWrapper]
		public delegate void SetColorGradeBlendDelegate(UIntPtr scenePointer, byte[] texture1, byte[] texture2, float alpha);

		// Token: 0x02000496 RID: 1174
		// (Invoke) Token: 0x0600186B RID: 6251
		[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
		[SuppressUnmanagedCodeSecurity]
		[MonoNativeFunctionWrapper]
		public delegate void SetDLSSModeDelegate(UIntPtr scenePointer, [MarshalAs(UnmanagedType.U1)] bool mode);

		// Token: 0x02000497 RID: 1175
		// (Invoke) Token: 0x0600186F RID: 6255
		[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
		[SuppressUnmanagedCodeSecurity]
		[MonoNativeFunctionWrapper]
		public delegate void SetDofFocusDelegate(UIntPtr scenePointer, float dofFocus);

		// Token: 0x02000498 RID: 1176
		// (Invoke) Token: 0x06001873 RID: 6259
		[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
		[SuppressUnmanagedCodeSecurity]
		[MonoNativeFunctionWrapper]
		public delegate void SetDofModeDelegate(UIntPtr scenePointer, [MarshalAs(UnmanagedType.U1)] bool mode);

		// Token: 0x02000499 RID: 1177
		// (Invoke) Token: 0x06001877 RID: 6263
		[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
		[SuppressUnmanagedCodeSecurity]
		[MonoNativeFunctionWrapper]
		public delegate void SetDofParamsDelegate(UIntPtr scenePointer, float dofFocusStart, float dofFocusEnd, [MarshalAs(UnmanagedType.U1)] bool isVignetteOn);

		// Token: 0x0200049A RID: 1178
		// (Invoke) Token: 0x0600187B RID: 6267
		[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
		[SuppressUnmanagedCodeSecurity]
		[MonoNativeFunctionWrapper]
		public delegate void SetDoNotAddEntitiesToTickListDelegate(UIntPtr scenePointer, [MarshalAs(UnmanagedType.U1)] bool value);

		// Token: 0x0200049B RID: 1179
		// (Invoke) Token: 0x0600187F RID: 6271
		[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
		[SuppressUnmanagedCodeSecurity]
		[MonoNativeFunctionWrapper]
		public delegate void SetDoNotWaitForLoadingStatesToRenderDelegate(UIntPtr scenePointer, [MarshalAs(UnmanagedType.U1)] bool value);

		// Token: 0x0200049C RID: 1180
		// (Invoke) Token: 0x06001883 RID: 6275
		[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
		[SuppressUnmanagedCodeSecurity]
		[MonoNativeFunctionWrapper]
		public delegate void SetDontLoadInvisibleEntitiesDelegate(UIntPtr scenePointer, [MarshalAs(UnmanagedType.U1)] bool value);

		// Token: 0x0200049D RID: 1181
		// (Invoke) Token: 0x06001887 RID: 6279
		[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
		[SuppressUnmanagedCodeSecurity]
		[MonoNativeFunctionWrapper]
		public delegate void SetDrynessFactorDelegate(UIntPtr scenePointer, float drynessFactor);

		// Token: 0x0200049E RID: 1182
		// (Invoke) Token: 0x0600188B RID: 6283
		[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
		[SuppressUnmanagedCodeSecurity]
		[MonoNativeFunctionWrapper]
		public delegate void SetDynamicShadowmapCascadesRadiusMultiplierDelegate(UIntPtr scenePointer, float extraRadius);

		// Token: 0x0200049F RID: 1183
		// (Invoke) Token: 0x0600188F RID: 6287
		[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
		[SuppressUnmanagedCodeSecurity]
		[MonoNativeFunctionWrapper]
		public delegate void SetDynamicSnowTextureDelegate(UIntPtr scenePointer, UIntPtr texturePointer);

		// Token: 0x020004A0 RID: 1184
		// (Invoke) Token: 0x06001893 RID: 6291
		[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
		[SuppressUnmanagedCodeSecurity]
		[MonoNativeFunctionWrapper]
		public delegate void SetEnvironmentMultiplierDelegate(UIntPtr scenePointer, [MarshalAs(UnmanagedType.U1)] bool useMultiplier, float multiplier);

		// Token: 0x020004A1 RID: 1185
		// (Invoke) Token: 0x06001897 RID: 6295
		[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
		[SuppressUnmanagedCodeSecurity]
		[MonoNativeFunctionWrapper]
		public delegate void SetExternalInjectionTextureDelegate(UIntPtr scenePointer, UIntPtr texturePointer);

		// Token: 0x020004A2 RID: 1186
		// (Invoke) Token: 0x0600189B RID: 6299
		[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
		[SuppressUnmanagedCodeSecurity]
		[MonoNativeFunctionWrapper]
		public delegate void SetFetchCrcInfoOfSceneDelegate(UIntPtr scenePointer, [MarshalAs(UnmanagedType.U1)] bool value);

		// Token: 0x020004A3 RID: 1187
		// (Invoke) Token: 0x0600189F RID: 6303
		[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
		[SuppressUnmanagedCodeSecurity]
		[MonoNativeFunctionWrapper]
		public delegate void SetFixedTickCallbackActiveDelegate(UIntPtr scene, [MarshalAs(UnmanagedType.U1)] bool isActive);

		// Token: 0x020004A4 RID: 1188
		// (Invoke) Token: 0x060018A3 RID: 6307
		[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
		[SuppressUnmanagedCodeSecurity]
		[MonoNativeFunctionWrapper]
		public delegate void SetFogDelegate(UIntPtr scenePointer, float fogDensity, ref Vec3 fogColor, float fogFalloff);

		// Token: 0x020004A5 RID: 1189
		// (Invoke) Token: 0x060018A7 RID: 6311
		[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
		[SuppressUnmanagedCodeSecurity]
		[MonoNativeFunctionWrapper]
		public delegate void SetFogAdvancedDelegate(UIntPtr scenePointer, float fogFalloffOffset, float fogFalloffMinFog, float fogFalloffStartDist);

		// Token: 0x020004A6 RID: 1190
		// (Invoke) Token: 0x060018AB RID: 6315
		[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
		[SuppressUnmanagedCodeSecurity]
		[MonoNativeFunctionWrapper]
		public delegate void SetFogAmbientColorDelegate(UIntPtr scenePointer, ref Vec3 fogAmbientColor);

		// Token: 0x020004A7 RID: 1191
		// (Invoke) Token: 0x060018AF RID: 6319
		[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
		[SuppressUnmanagedCodeSecurity]
		[MonoNativeFunctionWrapper]
		public delegate void SetForcedSnowDelegate(UIntPtr scenePointer, [MarshalAs(UnmanagedType.U1)] bool value);

		// Token: 0x020004A8 RID: 1192
		// (Invoke) Token: 0x060018B3 RID: 6323
		[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
		[SuppressUnmanagedCodeSecurity]
		[MonoNativeFunctionWrapper]
		public delegate void SetGlobalWindStrengthVectorDelegate(UIntPtr scenePointer, in Vec2 strengthVector);

		// Token: 0x020004A9 RID: 1193
		// (Invoke) Token: 0x060018B7 RID: 6327
		[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
		[SuppressUnmanagedCodeSecurity]
		[MonoNativeFunctionWrapper]
		public delegate void SetGlobalWindVelocityDelegate(UIntPtr scenePointer, in Vec2 windVelocity);

		// Token: 0x020004AA RID: 1194
		// (Invoke) Token: 0x060018BB RID: 6331
		[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
		[SuppressUnmanagedCodeSecurity]
		[MonoNativeFunctionWrapper]
		public delegate void SetGrainAmountDelegate(UIntPtr scenePointer, float grainAmount);

		// Token: 0x020004AB RID: 1195
		// (Invoke) Token: 0x060018BF RID: 6335
		[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
		[SuppressUnmanagedCodeSecurity]
		[MonoNativeFunctionWrapper]
		public delegate void SetHexagonVignetteAlphaDelegate(UIntPtr scenePointer, float Alpha);

		// Token: 0x020004AC RID: 1196
		// (Invoke) Token: 0x060018C3 RID: 6339
		[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
		[SuppressUnmanagedCodeSecurity]
		[MonoNativeFunctionWrapper]
		public delegate void SetHexagonVignetteColorDelegate(UIntPtr scenePointer, ref Vec3 p_hexagon_vignette_color);

		// Token: 0x020004AD RID: 1197
		// (Invoke) Token: 0x060018C7 RID: 6343
		[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
		[SuppressUnmanagedCodeSecurity]
		[MonoNativeFunctionWrapper]
		public delegate void SetHumidityDelegate(UIntPtr scenePointer, float humidity);

		// Token: 0x020004AE RID: 1198
		// (Invoke) Token: 0x060018CB RID: 6347
		[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
		[SuppressUnmanagedCodeSecurity]
		[MonoNativeFunctionWrapper]
		public delegate void SetLandscapeRainMaskDataDelegate(UIntPtr scenePointer, ManagedArray data);

		// Token: 0x020004AF RID: 1199
		// (Invoke) Token: 0x060018CF RID: 6351
		[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
		[SuppressUnmanagedCodeSecurity]
		[MonoNativeFunctionWrapper]
		public delegate void SetLensDistortionDelegate(UIntPtr scenePointer, float lensDistortion);

		// Token: 0x020004B0 RID: 1200
		// (Invoke) Token: 0x060018D3 RID: 6355
		[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
		[SuppressUnmanagedCodeSecurity]
		[MonoNativeFunctionWrapper]
		public delegate void SetLensFlareAberrationOffsetDelegate(UIntPtr scenePointer, float lensFlareAberrationOffset);

		// Token: 0x020004B1 RID: 1201
		// (Invoke) Token: 0x060018D7 RID: 6359
		[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
		[SuppressUnmanagedCodeSecurity]
		[MonoNativeFunctionWrapper]
		public delegate void SetLensFlareAmountDelegate(UIntPtr scenePointer, float lensFlareAmount);

		// Token: 0x020004B2 RID: 1202
		// (Invoke) Token: 0x060018DB RID: 6363
		[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
		[SuppressUnmanagedCodeSecurity]
		[MonoNativeFunctionWrapper]
		public delegate void SetLensFlareBlurSigmaDelegate(UIntPtr scenePointer, float lensFlareBlurSigma);

		// Token: 0x020004B3 RID: 1203
		// (Invoke) Token: 0x060018DF RID: 6367
		[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
		[SuppressUnmanagedCodeSecurity]
		[MonoNativeFunctionWrapper]
		public delegate void SetLensFlareBlurSizeDelegate(UIntPtr scenePointer, int lensFlareBlurSize);

		// Token: 0x020004B4 RID: 1204
		// (Invoke) Token: 0x060018E3 RID: 6371
		[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
		[SuppressUnmanagedCodeSecurity]
		[MonoNativeFunctionWrapper]
		public delegate void SetLensFlareDiffractionWeightDelegate(UIntPtr scenePointer, float lensFlareDiffractionWeight);

		// Token: 0x020004B5 RID: 1205
		// (Invoke) Token: 0x060018E7 RID: 6375
		[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
		[SuppressUnmanagedCodeSecurity]
		[MonoNativeFunctionWrapper]
		public delegate void SetLensFlareDirtWeightDelegate(UIntPtr scenePointer, float lensFlareDirtWeight);

		// Token: 0x020004B6 RID: 1206
		// (Invoke) Token: 0x060018EB RID: 6379
		[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
		[SuppressUnmanagedCodeSecurity]
		[MonoNativeFunctionWrapper]
		public delegate void SetLensFlareGhostSamplesDelegate(UIntPtr scenePointer, int lensFlareGhostSamples);

		// Token: 0x020004B7 RID: 1207
		// (Invoke) Token: 0x060018EF RID: 6383
		[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
		[SuppressUnmanagedCodeSecurity]
		[MonoNativeFunctionWrapper]
		public delegate void SetLensFlareGhostWeightDelegate(UIntPtr scenePointer, float lensFlareGhostWeight);

		// Token: 0x020004B8 RID: 1208
		// (Invoke) Token: 0x060018F3 RID: 6387
		[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
		[SuppressUnmanagedCodeSecurity]
		[MonoNativeFunctionWrapper]
		public delegate void SetLensFlareHaloWeightDelegate(UIntPtr scenePointer, float lensFlareHaloWeight);

		// Token: 0x020004B9 RID: 1209
		// (Invoke) Token: 0x060018F7 RID: 6391
		[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
		[SuppressUnmanagedCodeSecurity]
		[MonoNativeFunctionWrapper]
		public delegate void SetLensFlareHaloWidthDelegate(UIntPtr scenePointer, float lensFlareHaloWidth);

		// Token: 0x020004BA RID: 1210
		// (Invoke) Token: 0x060018FB RID: 6395
		[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
		[SuppressUnmanagedCodeSecurity]
		[MonoNativeFunctionWrapper]
		public delegate void SetLensFlareStrengthDelegate(UIntPtr scenePointer, float lensFlareStrength);

		// Token: 0x020004BB RID: 1211
		// (Invoke) Token: 0x060018FF RID: 6399
		[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
		[SuppressUnmanagedCodeSecurity]
		[MonoNativeFunctionWrapper]
		public delegate void SetLensFlareThresholdDelegate(UIntPtr scenePointer, float lensFlareThreshold);

		// Token: 0x020004BC RID: 1212
		// (Invoke) Token: 0x06001903 RID: 6403
		[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
		[SuppressUnmanagedCodeSecurity]
		[MonoNativeFunctionWrapper]
		public delegate void SetLightDiffuseColorDelegate(UIntPtr scenePointer, int lightIndex, Vec3 diffuseColor);

		// Token: 0x020004BD RID: 1213
		// (Invoke) Token: 0x06001907 RID: 6407
		[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
		[SuppressUnmanagedCodeSecurity]
		[MonoNativeFunctionWrapper]
		public delegate void SetLightDirectionDelegate(UIntPtr scenePointer, int lightIndex, Vec3 direction);

		// Token: 0x020004BE RID: 1214
		// (Invoke) Token: 0x0600190B RID: 6411
		[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
		[SuppressUnmanagedCodeSecurity]
		[MonoNativeFunctionWrapper]
		public delegate void SetLightPositionDelegate(UIntPtr scenePointer, int lightIndex, Vec3 position);

		// Token: 0x020004BF RID: 1215
		// (Invoke) Token: 0x0600190F RID: 6415
		[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
		[SuppressUnmanagedCodeSecurity]
		[MonoNativeFunctionWrapper]
		public delegate void SetMaxExposureDelegate(UIntPtr scenePointer, float maxExposure);

		// Token: 0x020004C0 RID: 1216
		// (Invoke) Token: 0x06001913 RID: 6419
		[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
		[SuppressUnmanagedCodeSecurity]
		[MonoNativeFunctionWrapper]
		public delegate void SetMiddleGrayDelegate(UIntPtr scenePointer, float middleGray);

		// Token: 0x020004C1 RID: 1217
		// (Invoke) Token: 0x06001917 RID: 6423
		[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
		[SuppressUnmanagedCodeSecurity]
		[MonoNativeFunctionWrapper]
		public delegate void SetMieScatterFocusDelegate(UIntPtr scenePointer, float strength);

		// Token: 0x020004C2 RID: 1218
		// (Invoke) Token: 0x0600191B RID: 6427
		[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
		[SuppressUnmanagedCodeSecurity]
		[MonoNativeFunctionWrapper]
		public delegate void SetMieScatterStrengthDelegate(UIntPtr scenePointer, float strength);

		// Token: 0x020004C3 RID: 1219
		// (Invoke) Token: 0x0600191F RID: 6431
		[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
		[SuppressUnmanagedCodeSecurity]
		[MonoNativeFunctionWrapper]
		public delegate void SetMinExposureDelegate(UIntPtr scenePointer, float minExposure);

		// Token: 0x020004C4 RID: 1220
		// (Invoke) Token: 0x06001923 RID: 6435
		[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
		[SuppressUnmanagedCodeSecurity]
		[MonoNativeFunctionWrapper]
		public delegate void SetMotionBlurModeDelegate(UIntPtr scenePointer, [MarshalAs(UnmanagedType.U1)] bool mode);

		// Token: 0x020004C5 RID: 1221
		// (Invoke) Token: 0x06001927 RID: 6439
		[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
		[SuppressUnmanagedCodeSecurity]
		[MonoNativeFunctionWrapper]
		public delegate void SetNameDelegate(UIntPtr scenePointer, byte[] name);

		// Token: 0x020004C6 RID: 1222
		// (Invoke) Token: 0x0600192B RID: 6443
		[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
		[SuppressUnmanagedCodeSecurity]
		[MonoNativeFunctionWrapper]
		public delegate void SetNavMeshRegionMapDelegate(UIntPtr scenePointer, IntPtr regionMap, int regionMapSize);

		// Token: 0x020004C7 RID: 1223
		// (Invoke) Token: 0x0600192F RID: 6447
		[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
		[SuppressUnmanagedCodeSecurity]
		[MonoNativeFunctionWrapper]
		public delegate void SetOcclusionModeDelegate(UIntPtr scenePointer, [MarshalAs(UnmanagedType.U1)] bool mode);

		// Token: 0x020004C8 RID: 1224
		// (Invoke) Token: 0x06001933 RID: 6451
		[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
		[SuppressUnmanagedCodeSecurity]
		[MonoNativeFunctionWrapper]
		public delegate void SetOnCollisionFilterCallbackActiveDelegate(UIntPtr scene, [MarshalAs(UnmanagedType.U1)] bool isActive);

		// Token: 0x020004C9 RID: 1225
		// (Invoke) Token: 0x06001937 RID: 6455
		[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
		[SuppressUnmanagedCodeSecurity]
		[MonoNativeFunctionWrapper]
		public delegate void SetOwnerThreadDelegate(UIntPtr scenePointer);

		// Token: 0x020004CA RID: 1226
		// (Invoke) Token: 0x0600193B RID: 6459
		[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
		[SuppressUnmanagedCodeSecurity]
		[MonoNativeFunctionWrapper]
		public delegate void SetPhotoAtmosphereViaTodDelegate(UIntPtr scenePointer, float tod, [MarshalAs(UnmanagedType.U1)] bool withStorm);

		// Token: 0x020004CB RID: 1227
		// (Invoke) Token: 0x0600193F RID: 6463
		[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
		[SuppressUnmanagedCodeSecurity]
		[MonoNativeFunctionWrapper]
		public delegate void SetPhotoModeFocusDelegate(UIntPtr scene, float focusStart, float focusEnd, float focus, float exposure);

		// Token: 0x020004CC RID: 1228
		// (Invoke) Token: 0x06001943 RID: 6467
		[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
		[SuppressUnmanagedCodeSecurity]
		[MonoNativeFunctionWrapper]
		public delegate void SetPhotoModeFovDelegate(UIntPtr scene, float verticalFov);

		// Token: 0x020004CD RID: 1229
		// (Invoke) Token: 0x06001947 RID: 6471
		[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
		[SuppressUnmanagedCodeSecurity]
		[MonoNativeFunctionWrapper]
		public delegate void SetPhotoModeOnDelegate(UIntPtr scene, [MarshalAs(UnmanagedType.U1)] bool on);

		// Token: 0x020004CE RID: 1230
		// (Invoke) Token: 0x0600194B RID: 6475
		[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
		[SuppressUnmanagedCodeSecurity]
		[MonoNativeFunctionWrapper]
		public delegate void SetPhotoModeOrbitDelegate(UIntPtr scene, [MarshalAs(UnmanagedType.U1)] bool orbit);

		// Token: 0x020004CF RID: 1231
		// (Invoke) Token: 0x0600194F RID: 6479
		[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
		[SuppressUnmanagedCodeSecurity]
		[MonoNativeFunctionWrapper]
		public delegate void SetPhotoModeRollDelegate(UIntPtr scene, float roll);

		// Token: 0x020004D0 RID: 1232
		// (Invoke) Token: 0x06001953 RID: 6483
		[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
		[SuppressUnmanagedCodeSecurity]
		[MonoNativeFunctionWrapper]
		public delegate void SetPhotoModeVignetteDelegate(UIntPtr scene, [MarshalAs(UnmanagedType.U1)] bool vignetteOn);

		// Token: 0x020004D1 RID: 1233
		// (Invoke) Token: 0x06001957 RID: 6487
		[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
		[SuppressUnmanagedCodeSecurity]
		[MonoNativeFunctionWrapper]
		public delegate void SetPlaySoundEventsAfterReadyToRenderDelegate(UIntPtr ptr, [MarshalAs(UnmanagedType.U1)] bool value);

		// Token: 0x020004D2 RID: 1234
		// (Invoke) Token: 0x0600195B RID: 6491
		[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
		[SuppressUnmanagedCodeSecurity]
		[MonoNativeFunctionWrapper]
		public delegate void SetRainDensityDelegate(UIntPtr scenePointer, float density);

		// Token: 0x020004D3 RID: 1235
		// (Invoke) Token: 0x0600195F RID: 6495
		[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
		[SuppressUnmanagedCodeSecurity]
		[MonoNativeFunctionWrapper]
		public delegate void SetSceneColorGradeDelegate(UIntPtr scene, byte[] textureName);

		// Token: 0x020004D4 RID: 1236
		// (Invoke) Token: 0x06001963 RID: 6499
		[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
		[SuppressUnmanagedCodeSecurity]
		[MonoNativeFunctionWrapper]
		public delegate void SetSceneColorGradeIndexDelegate(UIntPtr scene, int index);

		// Token: 0x020004D5 RID: 1237
		// (Invoke) Token: 0x06001967 RID: 6503
		[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
		[SuppressUnmanagedCodeSecurity]
		[MonoNativeFunctionWrapper]
		public delegate int SetSceneFilterIndexDelegate(UIntPtr scene, int index);

		// Token: 0x020004D6 RID: 1238
		// (Invoke) Token: 0x0600196B RID: 6507
		[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
		[SuppressUnmanagedCodeSecurity]
		[MonoNativeFunctionWrapper]
		public delegate void SetShadowDelegate(UIntPtr scenePointer, [MarshalAs(UnmanagedType.U1)] bool shadowEnabled);

		// Token: 0x020004D7 RID: 1239
		// (Invoke) Token: 0x0600196F RID: 6511
		[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
		[SuppressUnmanagedCodeSecurity]
		[MonoNativeFunctionWrapper]
		public delegate void SetSkyBrightnessDelegate(UIntPtr scenePointer, float brightness);

		// Token: 0x020004D8 RID: 1240
		// (Invoke) Token: 0x06001973 RID: 6515
		[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
		[SuppressUnmanagedCodeSecurity]
		[MonoNativeFunctionWrapper]
		public delegate void SetSkyRotationDelegate(UIntPtr scenePointer, float rotation);

		// Token: 0x020004D9 RID: 1241
		// (Invoke) Token: 0x06001977 RID: 6519
		[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
		[SuppressUnmanagedCodeSecurity]
		[MonoNativeFunctionWrapper]
		public delegate void SetSnowDensityDelegate(UIntPtr scenePointer, float density);

		// Token: 0x020004DA RID: 1242
		// (Invoke) Token: 0x0600197B RID: 6523
		[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
		[SuppressUnmanagedCodeSecurity]
		[MonoNativeFunctionWrapper]
		public delegate void SetStreakAmountDelegate(UIntPtr scenePointer, float streakAmount);

		// Token: 0x020004DB RID: 1243
		// (Invoke) Token: 0x0600197F RID: 6527
		[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
		[SuppressUnmanagedCodeSecurity]
		[MonoNativeFunctionWrapper]
		public delegate void SetStreakIntensityDelegate(UIntPtr scenePointer, float stretchAmount);

		// Token: 0x020004DC RID: 1244
		// (Invoke) Token: 0x06001983 RID: 6531
		[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
		[SuppressUnmanagedCodeSecurity]
		[MonoNativeFunctionWrapper]
		public delegate void SetStreakStrengthDelegate(UIntPtr scenePointer, float strengthAmount);

		// Token: 0x020004DD RID: 1245
		// (Invoke) Token: 0x06001987 RID: 6535
		[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
		[SuppressUnmanagedCodeSecurity]
		[MonoNativeFunctionWrapper]
		public delegate void SetStreakStretchDelegate(UIntPtr scenePointer, float stretchAmount);

		// Token: 0x020004DE RID: 1246
		// (Invoke) Token: 0x0600198B RID: 6539
		[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
		[SuppressUnmanagedCodeSecurity]
		[MonoNativeFunctionWrapper]
		public delegate void SetStreakThresholdDelegate(UIntPtr scenePointer, float streakThreshold);

		// Token: 0x020004DF RID: 1247
		// (Invoke) Token: 0x0600198F RID: 6543
		[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
		[SuppressUnmanagedCodeSecurity]
		[MonoNativeFunctionWrapper]
		public delegate void SetStreakTintDelegate(UIntPtr scenePointer, ref Vec3 p_streak_tint_color);

		// Token: 0x020004E0 RID: 1248
		// (Invoke) Token: 0x06001993 RID: 6547
		[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
		[SuppressUnmanagedCodeSecurity]
		[MonoNativeFunctionWrapper]
		public delegate void SetSunDelegate(UIntPtr scenePointer, Vec3 color, float altitude, float angle, float intensity);

		// Token: 0x020004E1 RID: 1249
		// (Invoke) Token: 0x06001997 RID: 6551
		[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
		[SuppressUnmanagedCodeSecurity]
		[MonoNativeFunctionWrapper]
		public delegate void SetSunAngleAltitudeDelegate(UIntPtr scenePointer, float angle, float altitude);

		// Token: 0x020004E2 RID: 1250
		// (Invoke) Token: 0x0600199B RID: 6555
		[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
		[SuppressUnmanagedCodeSecurity]
		[MonoNativeFunctionWrapper]
		public delegate void SetSunDirectionDelegate(UIntPtr scenePointer, Vec3 direction);

		// Token: 0x020004E3 RID: 1251
		// (Invoke) Token: 0x0600199F RID: 6559
		[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
		[SuppressUnmanagedCodeSecurity]
		[MonoNativeFunctionWrapper]
		public delegate void SetSunLightDelegate(UIntPtr scenePointer, Vec3 color, Vec3 direction);

		// Token: 0x020004E4 RID: 1252
		// (Invoke) Token: 0x060019A3 RID: 6563
		[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
		[SuppressUnmanagedCodeSecurity]
		[MonoNativeFunctionWrapper]
		public delegate void SetSunshaftModeDelegate(UIntPtr scenePointer, [MarshalAs(UnmanagedType.U1)] bool mode);

		// Token: 0x020004E5 RID: 1253
		// (Invoke) Token: 0x060019A7 RID: 6567
		[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
		[SuppressUnmanagedCodeSecurity]
		[MonoNativeFunctionWrapper]
		public delegate void SetSunShaftStrengthDelegate(UIntPtr scenePointer, float strength);

		// Token: 0x020004E6 RID: 1254
		// (Invoke) Token: 0x060019AB RID: 6571
		[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
		[SuppressUnmanagedCodeSecurity]
		[MonoNativeFunctionWrapper]
		public delegate void SetSunSizeDelegate(UIntPtr scenePointer, float size);

		// Token: 0x020004E7 RID: 1255
		// (Invoke) Token: 0x060019AF RID: 6575
		[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
		[SuppressUnmanagedCodeSecurity]
		[MonoNativeFunctionWrapper]
		public delegate void SetTargetExposureDelegate(UIntPtr scenePointer, float targetExposure);

		// Token: 0x020004E8 RID: 1256
		// (Invoke) Token: 0x060019B3 RID: 6579
		[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
		[SuppressUnmanagedCodeSecurity]
		[MonoNativeFunctionWrapper]
		public delegate void SetTemperatureDelegate(UIntPtr scenePointer, float temperature);

		// Token: 0x020004E9 RID: 1257
		// (Invoke) Token: 0x060019B7 RID: 6583
		[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
		[SuppressUnmanagedCodeSecurity]
		[MonoNativeFunctionWrapper]
		public delegate void SetTerrainDynamicParamsDelegate(UIntPtr scenePointer, Vec3 dynamic_params);

		// Token: 0x020004EA RID: 1258
		// (Invoke) Token: 0x060019BB RID: 6587
		[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
		[SuppressUnmanagedCodeSecurity]
		[MonoNativeFunctionWrapper]
		public delegate void SetTimeOfDayDelegate(UIntPtr scenePointer, float value);

		// Token: 0x020004EB RID: 1259
		// (Invoke) Token: 0x060019BF RID: 6591
		[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
		[SuppressUnmanagedCodeSecurity]
		[MonoNativeFunctionWrapper]
		public delegate void SetTimeSpeedDelegate(UIntPtr scenePointer, float value);

		// Token: 0x020004EC RID: 1260
		// (Invoke) Token: 0x060019C3 RID: 6595
		[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
		[SuppressUnmanagedCodeSecurity]
		[MonoNativeFunctionWrapper]
		public delegate void SetUpgradeLevelDelegate(UIntPtr scenePointer, int level);

		// Token: 0x020004ED RID: 1261
		// (Invoke) Token: 0x060019C7 RID: 6599
		[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
		[SuppressUnmanagedCodeSecurity]
		[MonoNativeFunctionWrapper]
		public delegate void SetUpgradeLevelVisibilityDelegate(UIntPtr scenePointer, byte[] concatLevels);

		// Token: 0x020004EE RID: 1262
		// (Invoke) Token: 0x060019CB RID: 6603
		[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
		[SuppressUnmanagedCodeSecurity]
		[MonoNativeFunctionWrapper]
		public delegate void SetUpgradeLevelVisibilityWithMaskDelegate(UIntPtr scenePointer, uint mask);

		// Token: 0x020004EF RID: 1263
		// (Invoke) Token: 0x060019CF RID: 6607
		[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
		[SuppressUnmanagedCodeSecurity]
		[MonoNativeFunctionWrapper]
		public delegate void SetUseConstantTimeDelegate(UIntPtr ptr, [MarshalAs(UnmanagedType.U1)] bool value);

		// Token: 0x020004F0 RID: 1264
		// (Invoke) Token: 0x060019D3 RID: 6611
		[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
		[SuppressUnmanagedCodeSecurity]
		[MonoNativeFunctionWrapper]
		public delegate void SetUsesDeleteLaterSystemDelegate(UIntPtr scenePointer, [MarshalAs(UnmanagedType.U1)] bool value);

		// Token: 0x020004F1 RID: 1265
		// (Invoke) Token: 0x060019D7 RID: 6615
		[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
		[SuppressUnmanagedCodeSecurity]
		[MonoNativeFunctionWrapper]
		public delegate void SetVignetteInnerRadiusDelegate(UIntPtr scenePointer, float vignetteInnerRadius);

		// Token: 0x020004F2 RID: 1266
		// (Invoke) Token: 0x060019DB RID: 6619
		[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
		[SuppressUnmanagedCodeSecurity]
		[MonoNativeFunctionWrapper]
		public delegate void SetVignetteOpacityDelegate(UIntPtr scenePointer, float vignetteOpacity);

		// Token: 0x020004F3 RID: 1267
		// (Invoke) Token: 0x060019DF RID: 6623
		[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
		[SuppressUnmanagedCodeSecurity]
		[MonoNativeFunctionWrapper]
		public delegate void SetVignetteOuterRadiusDelegate(UIntPtr scenePointer, float vignetteOuterRadius);

		// Token: 0x020004F4 RID: 1268
		// (Invoke) Token: 0x060019E3 RID: 6627
		[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
		[SuppressUnmanagedCodeSecurity]
		[MonoNativeFunctionWrapper]
		public delegate void SetWaterStrengthDelegate(UIntPtr scene, float newWaterStrength);

		// Token: 0x020004F5 RID: 1269
		// (Invoke) Token: 0x060019E7 RID: 6631
		[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
		[SuppressUnmanagedCodeSecurity]
		[MonoNativeFunctionWrapper]
		public delegate void SetWaterWakeCameraOffsetDelegate(UIntPtr scenePointer, float cameraOffset);

		// Token: 0x020004F6 RID: 1270
		// (Invoke) Token: 0x060019EB RID: 6635
		[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
		[SuppressUnmanagedCodeSecurity]
		[MonoNativeFunctionWrapper]
		public delegate void SetWaterWakeWorldSizeDelegate(UIntPtr scenePointer, float worldSize, float eraseFactor);

		// Token: 0x020004F7 RID: 1271
		// (Invoke) Token: 0x060019EF RID: 6639
		[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
		[SuppressUnmanagedCodeSecurity]
		[MonoNativeFunctionWrapper]
		public delegate void SetWinterTimeFactorDelegate(UIntPtr scenePointer, float winterTimeFactor);

		// Token: 0x020004F8 RID: 1272
		// (Invoke) Token: 0x060019F3 RID: 6643
		[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
		[SuppressUnmanagedCodeSecurity]
		[MonoNativeFunctionWrapper]
		public delegate void StallLoadingRenderingsUntilFurtherNoticeDelegate(UIntPtr scenePointer);

		// Token: 0x020004F9 RID: 1273
		// (Invoke) Token: 0x060019F7 RID: 6647
		[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
		[SuppressUnmanagedCodeSecurity]
		[MonoNativeFunctionWrapper]
		[return: MarshalAs(UnmanagedType.U1)]
		public delegate bool SwapFaceConnectionsWithIdDelegate(UIntPtr scenePointer, int hubFaceGroupID, int toBeSeparatedFaceGroupId, int toBeMergedFaceGroupId, [MarshalAs(UnmanagedType.U1)] bool canFail);

		// Token: 0x020004FA RID: 1274
		// (Invoke) Token: 0x060019FB RID: 6651
		[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
		[SuppressUnmanagedCodeSecurity]
		[MonoNativeFunctionWrapper]
		public delegate int TakePhotoModePictureDelegate(UIntPtr scene, [MarshalAs(UnmanagedType.U1)] bool saveAmbientOcclusionPass, [MarshalAs(UnmanagedType.U1)] bool saveObjectIdPass, [MarshalAs(UnmanagedType.U1)] bool saveShadowPass);

		// Token: 0x020004FB RID: 1275
		// (Invoke) Token: 0x060019FF RID: 6655
		[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
		[SuppressUnmanagedCodeSecurity]
		[MonoNativeFunctionWrapper]
		public delegate void TickDelegate(UIntPtr scenePointer, float deltaTime);

		// Token: 0x020004FC RID: 1276
		// (Invoke) Token: 0x06001A03 RID: 6659
		[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
		[SuppressUnmanagedCodeSecurity]
		[MonoNativeFunctionWrapper]
		public delegate void TickWakeDelegate(UIntPtr scenePointer, float dt);

		// Token: 0x020004FD RID: 1277
		// (Invoke) Token: 0x06001A07 RID: 6663
		[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
		[SuppressUnmanagedCodeSecurity]
		[MonoNativeFunctionWrapper]
		public delegate void WaitWaterRendererCPUSimulationDelegate(UIntPtr scenePointer);

		// Token: 0x020004FE RID: 1278
		// (Invoke) Token: 0x06001A0B RID: 6667
		[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
		[SuppressUnmanagedCodeSecurity]
		[MonoNativeFunctionWrapper]
		public delegate void WorldPositionComputeNearestNavMeshDelegate(ref WorldPosition position);

		// Token: 0x020004FF RID: 1279
		// (Invoke) Token: 0x06001A0F RID: 6671
		[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
		[SuppressUnmanagedCodeSecurity]
		[MonoNativeFunctionWrapper]
		public delegate void WorldPositionValidateZDelegate(ref WorldPosition position, int minimumValidityState);
	}
}
