using System;
using System.Runtime.InteropServices;
using System.Security;
using System.Text;
using TaleWorlds.DotNet;
using TaleWorlds.Engine;
using TaleWorlds.Library;

namespace ManagedCallbacks;

internal class ScriptingInterfaceOfIScene : IScene
{
	[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
	[SuppressUnmanagedCodeSecurity]
	[MonoNativeFunctionWrapper]
	public delegate void AddAlwaysRenderedSkeletonDelegate(UIntPtr scenePointer, UIntPtr skeletonPointer);

	[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
	[SuppressUnmanagedCodeSecurity]
	[MonoNativeFunctionWrapper]
	public delegate void AddDecalInstanceDelegate(UIntPtr scenePointer, UIntPtr decalMeshPointer, byte[] decalSetID, [MarshalAs(UnmanagedType.U1)] bool deletable);

	[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
	[SuppressUnmanagedCodeSecurity]
	[MonoNativeFunctionWrapper]
	public delegate int AddDirectionalLightDelegate(UIntPtr scenePointer, Vec3 position, Vec3 direction, float radius);

	[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
	[SuppressUnmanagedCodeSecurity]
	[MonoNativeFunctionWrapper]
	public delegate void AddEntityWithMeshDelegate(UIntPtr scenePointer, UIntPtr meshPointer, ref MatrixFrame frame);

	[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
	[SuppressUnmanagedCodeSecurity]
	[MonoNativeFunctionWrapper]
	public delegate void AddEntityWithMultiMeshDelegate(UIntPtr scenePointer, UIntPtr multiMeshPointer, ref MatrixFrame frame);

	[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
	[SuppressUnmanagedCodeSecurity]
	[MonoNativeFunctionWrapper]
	public delegate NativeObjectPointer AddItemEntityDelegate(UIntPtr scenePointer, ref MatrixFrame frame, UIntPtr meshPointer);

	[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
	[SuppressUnmanagedCodeSecurity]
	[MonoNativeFunctionWrapper]
	public delegate void AddPathDelegate(UIntPtr scenePointer, byte[] name);

	[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
	[SuppressUnmanagedCodeSecurity]
	[MonoNativeFunctionWrapper]
	public delegate void AddPathPointDelegate(UIntPtr scenePointer, byte[] name, ref MatrixFrame frame);

	[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
	[SuppressUnmanagedCodeSecurity]
	[MonoNativeFunctionWrapper]
	public delegate int AddPointLightDelegate(UIntPtr scenePointer, Vec3 position, float radius);

	[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
	[SuppressUnmanagedCodeSecurity]
	[MonoNativeFunctionWrapper]
	public delegate void AddWaterWakeWithCapsuleDelegate(UIntPtr scene, Vec3 positionA, float radiusA, Vec3 positionB, float radiusB, float wakeVisibility, float foamVisibility);

	[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
	[SuppressUnmanagedCodeSecurity]
	[MonoNativeFunctionWrapper]
	[return: MarshalAs(UnmanagedType.U1)]
	public delegate bool AttachEntityDelegate(UIntPtr scenePointer, UIntPtr entity, [MarshalAs(UnmanagedType.U1)] bool showWarnings);

	[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
	[SuppressUnmanagedCodeSecurity]
	[MonoNativeFunctionWrapper]
	[return: MarshalAs(UnmanagedType.U1)]
	public delegate bool BoxCastDelegate(UIntPtr scenePointer, ref Vec3 boxPointBegin, ref Vec3 boxPointEnd, ref Vec3 dir, float distance, ref float collisionDistance, ref Vec3 closestPoint, ref UIntPtr entityIndex, BodyFlags bodyExcludeFlags);

	[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
	[SuppressUnmanagedCodeSecurity]
	[MonoNativeFunctionWrapper]
	[return: MarshalAs(UnmanagedType.U1)]
	public delegate bool BoxCastOnlyForCameraDelegate(UIntPtr scenePointer, IntPtr boxPoints, in Vec3 centerPoint, in Vec3 dir, float distance, UIntPtr ignoredEntityPointer, ref float collisionDistance, ref Vec3 closestPoint, ref UIntPtr entityPointer, BodyFlags bodyExcludeFlags);

	[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
	[SuppressUnmanagedCodeSecurity]
	[MonoNativeFunctionWrapper]
	[return: MarshalAs(UnmanagedType.U1)]
	public delegate bool CalculateEffectiveLightingDelegate(UIntPtr scenePointer);

	[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
	[SuppressUnmanagedCodeSecurity]
	[MonoNativeFunctionWrapper]
	[return: MarshalAs(UnmanagedType.U1)]
	public delegate bool CheckPathEntitiesFrameChangedDelegate(UIntPtr scenePointer, byte[] containsName);

	[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
	[SuppressUnmanagedCodeSecurity]
	[MonoNativeFunctionWrapper]
	[return: MarshalAs(UnmanagedType.U1)]
	public delegate bool CheckPointCanSeePointDelegate(UIntPtr scenePointer, Vec3 sourcePoint, Vec3 targetPoint, float distanceToCheck);

	[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
	[SuppressUnmanagedCodeSecurity]
	[MonoNativeFunctionWrapper]
	public delegate void CheckResourcesDelegate(UIntPtr scenePointer, [MarshalAs(UnmanagedType.U1)] bool checkInvisibleEntities);

	[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
	[SuppressUnmanagedCodeSecurity]
	[MonoNativeFunctionWrapper]
	public delegate void ClearAllDelegate(UIntPtr scenePointer);

	[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
	[SuppressUnmanagedCodeSecurity]
	[MonoNativeFunctionWrapper]
	public delegate void ClearCurrentFrameTickEntitiesDelegate(UIntPtr scenePointer);

	[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
	[SuppressUnmanagedCodeSecurity]
	[MonoNativeFunctionWrapper]
	public delegate void ClearDecalsDelegate(UIntPtr scenePointer);

	[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
	[SuppressUnmanagedCodeSecurity]
	[MonoNativeFunctionWrapper]
	public delegate void ClearNavMeshDelegate(UIntPtr scenePointer);

	[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
	[SuppressUnmanagedCodeSecurity]
	[MonoNativeFunctionWrapper]
	[return: MarshalAs(UnmanagedType.U1)]
	public delegate bool ContainsTerrainDelegate(UIntPtr scenePointer);

	[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
	[SuppressUnmanagedCodeSecurity]
	[MonoNativeFunctionWrapper]
	public delegate void CreateBurstParticleDelegate(UIntPtr scene, int particleId, ref MatrixFrame frame);

	[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
	[SuppressUnmanagedCodeSecurity]
	[MonoNativeFunctionWrapper]
	public delegate void CreateDynamicRainTextureDelegate(UIntPtr scenePointer, int w, int h);

	[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
	[SuppressUnmanagedCodeSecurity]
	[MonoNativeFunctionWrapper]
	public delegate NativeObjectPointer CreateNewSceneDelegate([MarshalAs(UnmanagedType.U1)] bool initializePhysics, [MarshalAs(UnmanagedType.U1)] bool enableDecals, int atlasGroup, byte[] sceneName);

	[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
	[SuppressUnmanagedCodeSecurity]
	[MonoNativeFunctionWrapper]
	public delegate NativeObjectPointer CreatePathMeshDelegate(UIntPtr scenePointer, byte[] baseEntityName, [MarshalAs(UnmanagedType.U1)] bool isWaterPath);

	[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
	[SuppressUnmanagedCodeSecurity]
	[MonoNativeFunctionWrapper]
	public delegate NativeObjectPointer CreatePathMesh2Delegate(UIntPtr scenePointer, IntPtr pathNodes, int pathNodeCount, [MarshalAs(UnmanagedType.U1)] bool isWaterPath);

	[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
	[SuppressUnmanagedCodeSecurity]
	[MonoNativeFunctionWrapper]
	public delegate void DeletePathWithNameDelegate(UIntPtr scenePointer, byte[] name);

	[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
	[SuppressUnmanagedCodeSecurity]
	[MonoNativeFunctionWrapper]
	public delegate void DeleteWaterWakeRendererDelegate(UIntPtr scenePointer);

	[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
	[SuppressUnmanagedCodeSecurity]
	[MonoNativeFunctionWrapper]
	public delegate void DeRegisterShipVisualDelegate(UIntPtr scenePointer, UIntPtr visualPointer);

	[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
	[SuppressUnmanagedCodeSecurity]
	[MonoNativeFunctionWrapper]
	public delegate void DisableStaticShadowsDelegate(UIntPtr ptr, [MarshalAs(UnmanagedType.U1)] bool value);

	[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
	[SuppressUnmanagedCodeSecurity]
	[MonoNativeFunctionWrapper]
	[return: MarshalAs(UnmanagedType.U1)]
	public delegate bool DoesPathExistBetweenFacesDelegate(UIntPtr scenePointer, int firstNavMeshFace, int secondNavMeshFace, [MarshalAs(UnmanagedType.U1)] bool ignoreDisabled);

	[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
	[SuppressUnmanagedCodeSecurity]
	[MonoNativeFunctionWrapper]
	[return: MarshalAs(UnmanagedType.U1)]
	public delegate bool DoesPathExistBetweenPositionsDelegate(UIntPtr scenePointer, WorldPosition position, WorldPosition destination);

	[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
	[SuppressUnmanagedCodeSecurity]
	[MonoNativeFunctionWrapper]
	public delegate void EnableFixedTickDelegate(UIntPtr scene);

	[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
	[SuppressUnmanagedCodeSecurity]
	[MonoNativeFunctionWrapper]
	public delegate void EnableInclusiveAsyncPhysxDelegate(UIntPtr scenePointer);

	[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
	[SuppressUnmanagedCodeSecurity]
	[MonoNativeFunctionWrapper]
	public delegate void EnsurePostfxSystemDelegate(UIntPtr scenePointer);

	[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
	[SuppressUnmanagedCodeSecurity]
	[MonoNativeFunctionWrapper]
	public delegate void EnsureWaterWakeRendererDelegate(UIntPtr scenePointer);

	[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
	[SuppressUnmanagedCodeSecurity]
	[MonoNativeFunctionWrapper]
	public delegate void FillEntityWithHardBorderPhysicsBarrierDelegate(UIntPtr scenePointer, UIntPtr entityPointer);

	[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
	[SuppressUnmanagedCodeSecurity]
	[MonoNativeFunctionWrapper]
	public delegate void FillTerrainHeightDataDelegate(UIntPtr scene, int xIndex, int yIndex, IntPtr heightArray);

	[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
	[SuppressUnmanagedCodeSecurity]
	[MonoNativeFunctionWrapper]
	public delegate void FillTerrainPhysicsMaterialIndexDataDelegate(UIntPtr scene, int xIndex, int yIndex, IntPtr materialIndexArray);

	[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
	[SuppressUnmanagedCodeSecurity]
	[MonoNativeFunctionWrapper]
	public delegate Vec2 FindClosestExitPositionForPositionOnABoundaryFaceDelegate(UIntPtr scenePointer, Vec3 position, UIntPtr boundaryNavMeshFacePointer);

	[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
	[SuppressUnmanagedCodeSecurity]
	[MonoNativeFunctionWrapper]
	public delegate void FinishSceneSoundsDelegate(UIntPtr scenePointer);

	[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
	[SuppressUnmanagedCodeSecurity]
	[MonoNativeFunctionWrapper]
	[return: MarshalAs(UnmanagedType.U1)]
	public delegate bool FocusRayCastForFixedPhysicsDelegate(UIntPtr scenePointer, in Vec3 sourcePoint, in Vec3 targetPoint, float rayThickness, ref float collisionDistance, ref Vec3 closestPoint, ref UIntPtr entityIndex, BodyFlags bodyExcludeFlags, [MarshalAs(UnmanagedType.U1)] bool isFixedWorld);

	[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
	[SuppressUnmanagedCodeSecurity]
	[MonoNativeFunctionWrapper]
	public delegate void ForceLoadResourcesDelegate(UIntPtr scenePointer, [MarshalAs(UnmanagedType.U1)] bool checkInvisibleEntities);

	[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
	[SuppressUnmanagedCodeSecurity]
	[MonoNativeFunctionWrapper]
	public delegate int GenerateContactsWithCapsuleDelegate(UIntPtr scenePointer, ref CapsuleData cap, BodyFlags excludeFlags, [MarshalAs(UnmanagedType.U1)] bool isFixedTick, IntPtr intersections, IntPtr entityIds);

	[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
	[SuppressUnmanagedCodeSecurity]
	[MonoNativeFunctionWrapper]
	public delegate int GenerateContactsWithCapsuleAgainstEntityDelegate(UIntPtr scenePointer, ref CapsuleData cap, BodyFlags excludeFlags, UIntPtr entityId, IntPtr intersections);

	[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
	[SuppressUnmanagedCodeSecurity]
	[MonoNativeFunctionWrapper]
	public delegate int GetAllColorGradeNamesDelegate(UIntPtr scene);

	[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
	[SuppressUnmanagedCodeSecurity]
	[MonoNativeFunctionWrapper]
	public delegate void GetAllEntitiesWithScriptComponentDelegate(UIntPtr scenePointer, byte[] scriptComponentName, UIntPtr output);

	[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
	[SuppressUnmanagedCodeSecurity]
	[MonoNativeFunctionWrapper]
	public delegate int GetAllFilterNamesDelegate(UIntPtr scene);

	[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
	[SuppressUnmanagedCodeSecurity]
	[MonoNativeFunctionWrapper]
	public delegate void GetAllNavmeshFaceRecordsDelegate(UIntPtr scenePointer, IntPtr faceRecords);

	[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
	[SuppressUnmanagedCodeSecurity]
	[MonoNativeFunctionWrapper]
	public delegate void GetBoundingBoxDelegate(UIntPtr scenePointer, ref Vec3 min, ref Vec3 max);

	[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
	[SuppressUnmanagedCodeSecurity]
	[MonoNativeFunctionWrapper]
	public delegate void GetBulkWaterLevelAtPositionsDelegate(UIntPtr scene, IntPtr waterHeightQueryArray, int arraySize, IntPtr waterHeightsAtVolumes, IntPtr waterSurfaceNormals);

	[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
	[SuppressUnmanagedCodeSecurity]
	[MonoNativeFunctionWrapper]
	public delegate void GetBulkWaterLevelAtVolumesDelegate(UIntPtr scene, UIntPtr volumeQueryDataArray, int volumeCount, in MatrixFrame entityFrame);

	[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
	[SuppressUnmanagedCodeSecurity]
	[MonoNativeFunctionWrapper]
	public delegate NativeObjectPointer GetCampaignEntityWithNameDelegate(UIntPtr scenePointer, byte[] entityName);

	[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
	[SuppressUnmanagedCodeSecurity]
	[MonoNativeFunctionWrapper]
	[return: MarshalAs(UnmanagedType.U1)]
	public delegate bool GetEnginePhysicsEnabledDelegate(UIntPtr scenePointer);

	[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
	[SuppressUnmanagedCodeSecurity]
	[MonoNativeFunctionWrapper]
	public delegate void GetEntitiesDelegate(UIntPtr scenePointer, UIntPtr entityObjectsArrayPointer);

	[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
	[SuppressUnmanagedCodeSecurity]
	[MonoNativeFunctionWrapper]
	public delegate int GetEntityCountDelegate(UIntPtr scenePointer);

	[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
	[SuppressUnmanagedCodeSecurity]
	[MonoNativeFunctionWrapper]
	public delegate NativeObjectPointer GetEntityWithGuidDelegate(UIntPtr scenePointer, byte[] guid);

	[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
	[SuppressUnmanagedCodeSecurity]
	[MonoNativeFunctionWrapper]
	public delegate float GetFallDensityDelegate(UIntPtr scenepTR);

	[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
	[SuppressUnmanagedCodeSecurity]
	[MonoNativeFunctionWrapper]
	public delegate NativeObjectPointer GetFirstEntityWithNameDelegate(UIntPtr scenePointer, byte[] entityName);

	[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
	[SuppressUnmanagedCodeSecurity]
	[MonoNativeFunctionWrapper]
	public delegate NativeObjectPointer GetFirstEntityWithScriptComponentDelegate(UIntPtr scenePointer, byte[] scriptComponentName);

	[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
	[SuppressUnmanagedCodeSecurity]
	[MonoNativeFunctionWrapper]
	public delegate int GetFloraInstanceCountDelegate(UIntPtr scenePointer);

	[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
	[SuppressUnmanagedCodeSecurity]
	[MonoNativeFunctionWrapper]
	public delegate int GetFloraRendererTextureUsageDelegate(UIntPtr scenePointer);

	[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
	[SuppressUnmanagedCodeSecurity]
	[MonoNativeFunctionWrapper]
	public delegate float GetFogDelegate(UIntPtr scenePointer);

	[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
	[SuppressUnmanagedCodeSecurity]
	[MonoNativeFunctionWrapper]
	public delegate Vec2 GetGlobalWindStrengthVectorDelegate(UIntPtr scenePointer);

	[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
	[SuppressUnmanagedCodeSecurity]
	[MonoNativeFunctionWrapper]
	public delegate Vec2 GetGlobalWindVelocityDelegate(UIntPtr scenePointer);

	[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
	[SuppressUnmanagedCodeSecurity]
	[MonoNativeFunctionWrapper]
	public delegate float GetGroundHeightAndBodyFlagsAtPositionDelegate(UIntPtr scenePointer, Vec3 position, out BodyFlags contactPointFlags, BodyFlags excludeFlags);

	[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
	[SuppressUnmanagedCodeSecurity]
	[MonoNativeFunctionWrapper]
	public delegate float GetGroundHeightAndNormalAtPositionDelegate(UIntPtr scenePointer, Vec3 position, ref Vec3 normal, uint excludeFlags);

	[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
	[SuppressUnmanagedCodeSecurity]
	[MonoNativeFunctionWrapper]
	public delegate float GetGroundHeightAtPositionDelegate(UIntPtr scenePointer, Vec3 position, uint excludeFlags);

	[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
	[SuppressUnmanagedCodeSecurity]
	[MonoNativeFunctionWrapper]
	public delegate Vec2 GetHardBoundaryVertexDelegate(UIntPtr scenePointer, int index);

	[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
	[SuppressUnmanagedCodeSecurity]
	[MonoNativeFunctionWrapper]
	public delegate int GetHardBoundaryVertexCountDelegate(UIntPtr scenePointer);

	[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
	[SuppressUnmanagedCodeSecurity]
	[MonoNativeFunctionWrapper]
	[return: MarshalAs(UnmanagedType.U1)]
	public delegate bool GetHeightAtPointDelegate(UIntPtr scenePointer, Vec2 point, BodyFlags excludeBodyFlags, ref float height);

	[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
	[SuppressUnmanagedCodeSecurity]
	[MonoNativeFunctionWrapper]
	public delegate int GetIdOfNavMeshFaceDelegate(UIntPtr scenePointer, int navMeshFace);

	[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
	[SuppressUnmanagedCodeSecurity]
	[MonoNativeFunctionWrapper]
	public delegate void GetInterpolationFactorForBodyWorldTransformSmoothingDelegate(UIntPtr scene, out float interpolationFactor, out float fixedDt);

	[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
	[SuppressUnmanagedCodeSecurity]
	[MonoNativeFunctionWrapper]
	public delegate void GetLastFinalRenderCameraFrameDelegate(UIntPtr scenePointer, ref MatrixFrame outFrame);

	[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
	[SuppressUnmanagedCodeSecurity]
	[MonoNativeFunctionWrapper]
	public delegate Vec3 GetLastFinalRenderCameraPositionDelegate(UIntPtr scenePointer);

	[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
	[SuppressUnmanagedCodeSecurity]
	[MonoNativeFunctionWrapper]
	public delegate Vec2 GetLastPointOnNavigationMeshFromPositionToDestinationDelegate(UIntPtr scenePointer, int startingFace, Vec2 position, Vec2 destination, IntPtr exclusionGroupIds, int exclusionGroupIdsCount);

	[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
	[SuppressUnmanagedCodeSecurity]
	[MonoNativeFunctionWrapper]
	public delegate Vec3 GetLastPointOnNavigationMeshFromWorldPositionToDestinationDelegate(UIntPtr scenePointer, ref WorldPosition position, Vec2 destination);

	[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
	[SuppressUnmanagedCodeSecurity]
	[MonoNativeFunctionWrapper]
	public delegate Vec2 GetLastPositionOnNavMeshFaceForPointAndDirectionDelegate(UIntPtr scenePointer, in PathFaceRecord record, Vec2 position, Vec2 direction);

	[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
	[SuppressUnmanagedCodeSecurity]
	[MonoNativeFunctionWrapper]
	public delegate int GetLoadingStateNameDelegate(UIntPtr scene);

	[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
	[SuppressUnmanagedCodeSecurity]
	[MonoNativeFunctionWrapper]
	public delegate int GetModulePathDelegate(UIntPtr scenePointer);

	[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
	[SuppressUnmanagedCodeSecurity]
	[MonoNativeFunctionWrapper]
	public delegate int GetNameDelegate(UIntPtr scenePointer);

	[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
	[SuppressUnmanagedCodeSecurity]
	[MonoNativeFunctionWrapper]
	public delegate uint GetNavigationMeshCRCDelegate(UIntPtr scenePointer);

	[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
	[SuppressUnmanagedCodeSecurity]
	[MonoNativeFunctionWrapper]
	public delegate UIntPtr GetNavigationMeshForPositionDelegate(UIntPtr scenePointer, in Vec3 position, ref int faceGroupId, float heightDifferenceLimit, [MarshalAs(UnmanagedType.U1)] bool excludeDynamicNavigationMeshes);

	[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
	[SuppressUnmanagedCodeSecurity]
	[MonoNativeFunctionWrapper]
	public delegate void GetNavMeshFaceCenterPositionDelegate(UIntPtr scenePointer, int navMeshFace, ref Vec3 centerPos);

	[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
	[SuppressUnmanagedCodeSecurity]
	[MonoNativeFunctionWrapper]
	public delegate int GetNavMeshFaceCountDelegate(UIntPtr scenePointer);

	[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
	[SuppressUnmanagedCodeSecurity]
	[MonoNativeFunctionWrapper]
	public delegate int GetNavmeshFaceCountBetweenTwoIdsDelegate(UIntPtr scenePointer, int firstId, int secondId);

	[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
	[SuppressUnmanagedCodeSecurity]
	[MonoNativeFunctionWrapper]
	public delegate float GetNavMeshFaceFirstVertexZDelegate(UIntPtr scenePointer, int navMeshFaceIndex);

	[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
	[SuppressUnmanagedCodeSecurity]
	[MonoNativeFunctionWrapper]
	public delegate void GetNavMeshFaceIndexDelegate(UIntPtr scenePointer, ref PathFaceRecord record, Vec2 position, [MarshalAs(UnmanagedType.U1)] bool isRegion1, [MarshalAs(UnmanagedType.U1)] bool checkIfDisabled, [MarshalAs(UnmanagedType.U1)] bool ignoreHeight);

	[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
	[SuppressUnmanagedCodeSecurity]
	[MonoNativeFunctionWrapper]
	public delegate void GetNavMeshFaceIndex3Delegate(UIntPtr scenePointer, ref PathFaceRecord record, Vec3 position, [MarshalAs(UnmanagedType.U1)] bool checkIfDisabled);

	[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
	[SuppressUnmanagedCodeSecurity]
	[MonoNativeFunctionWrapper]
	public delegate void GetNavmeshFaceRecordsBetweenTwoIdsDelegate(UIntPtr scenePointer, int firstId, int secondId, IntPtr faceRecords);

	[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
	[SuppressUnmanagedCodeSecurity]
	[MonoNativeFunctionWrapper]
	public delegate PathFaceRecord GetNavMeshPathFaceRecordDelegate(UIntPtr scenePointer, int navMeshFace);

	[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
	[SuppressUnmanagedCodeSecurity]
	[MonoNativeFunctionWrapper]
	public delegate UIntPtr GetNearestNavigationMeshForPositionDelegate(UIntPtr scenePointer, in Vec3 position, float heightDifferenceLimit, [MarshalAs(UnmanagedType.U1)] bool excludeDynamicNavigationMeshes);

	[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
	[SuppressUnmanagedCodeSecurity]
	[MonoNativeFunctionWrapper]
	public delegate int GetNodeDataCountDelegate(UIntPtr scene, int xIndex, int yIndex);

	[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
	[SuppressUnmanagedCodeSecurity]
	[MonoNativeFunctionWrapper]
	public delegate Vec3 GetNormalAtDelegate(UIntPtr scenePointer, Vec2 position);

	[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
	[SuppressUnmanagedCodeSecurity]
	[MonoNativeFunctionWrapper]
	public delegate float GetNorthAngleDelegate(UIntPtr scenePointer);

	[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
	[SuppressUnmanagedCodeSecurity]
	[MonoNativeFunctionWrapper]
	public delegate int GetNumberOfPathsWithNamePrefixDelegate(UIntPtr ptr, byte[] prefix);

	[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
	[SuppressUnmanagedCodeSecurity]
	[MonoNativeFunctionWrapper]
	[return: MarshalAs(UnmanagedType.U1)]
	public delegate bool GetPathBetweenAIFaceIndicesDelegate(UIntPtr scenePointer, int startingAiFace, int endingAiFace, Vec2 startingPosition, Vec2 endingPosition, float agentRadius, IntPtr result, ref int pathSize, IntPtr exclusionGroupIds, int exclusionGroupIdsCount, float extraCostMultiplier);

	[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
	[SuppressUnmanagedCodeSecurity]
	[MonoNativeFunctionWrapper]
	[return: MarshalAs(UnmanagedType.U1)]
	public delegate bool GetPathBetweenAIFaceIndicesWithRegionSwitchCostDelegate(UIntPtr scenePointer, int startingAiFace, int endingAiFace, Vec2 startingPosition, Vec2 endingPosition, float agentRadius, IntPtr result, ref int pathSize, IntPtr exclusionGroupIds, int exclusionGroupIdsCount, float extraCostMultiplier, int regionSwitchCostTo0, int regionSwitchCostTo1);

	[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
	[SuppressUnmanagedCodeSecurity]
	[MonoNativeFunctionWrapper]
	[return: MarshalAs(UnmanagedType.U1)]
	public delegate bool GetPathBetweenAIFacePointersDelegate(UIntPtr scenePointer, UIntPtr startingAiFace, UIntPtr endingAiFace, Vec2 startingPosition, Vec2 endingPosition, float agentRadius, IntPtr result, ref int pathSize, IntPtr exclusionGroupIds, int exclusionGroupIdsCount);

	[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
	[SuppressUnmanagedCodeSecurity]
	[MonoNativeFunctionWrapper]
	[return: MarshalAs(UnmanagedType.U1)]
	public delegate bool GetPathBetweenAIFacePointersWithRegionSwitchCostDelegate(UIntPtr scenePointer, UIntPtr startingAiFace, UIntPtr endingAiFace, Vec2 startingPosition, Vec2 endingPosition, float agentRadius, IntPtr result, ref int pathSize, IntPtr exclusionGroupIds, int exclusionGroupIdsCount, int regionSwitchCostTo0, int regionSwitchCostTo1);

	[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
	[SuppressUnmanagedCodeSecurity]
	[MonoNativeFunctionWrapper]
	[return: MarshalAs(UnmanagedType.U1)]
	public delegate bool GetPathDistanceBetweenAIFacesDelegate(UIntPtr scenePointer, int startingAiFace, int endingAiFace, Vec2 startingPosition, Vec2 endingPosition, float agentRadius, float distanceLimit, out float distance, IntPtr exclusionGroupIds, int exclusionGroupIdsCount, int regionSwitchCostTo0, int regionSwitchCostTo1);

	[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
	[SuppressUnmanagedCodeSecurity]
	[MonoNativeFunctionWrapper]
	[return: MarshalAs(UnmanagedType.U1)]
	public delegate bool GetPathDistanceBetweenPositionsDelegate(UIntPtr scenePointer, ref WorldPosition position, ref WorldPosition destination, float agentRadius, ref float pathLength);

	[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
	[SuppressUnmanagedCodeSecurity]
	[MonoNativeFunctionWrapper]
	public delegate PathFaceRecord GetPathFaceRecordFromNavMeshFacePointerDelegate(UIntPtr scenePointer, UIntPtr navMeshFacePointer);

	[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
	[SuppressUnmanagedCodeSecurity]
	[MonoNativeFunctionWrapper]
	public delegate void GetPathsWithNamePrefixDelegate(UIntPtr ptr, IntPtr points, byte[] prefix);

	[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
	[SuppressUnmanagedCodeSecurity]
	[MonoNativeFunctionWrapper]
	public delegate NativeObjectPointer GetPathWithNameDelegate(UIntPtr scenePointer, byte[] name);

	[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
	[SuppressUnmanagedCodeSecurity]
	[MonoNativeFunctionWrapper]
	public delegate void GetPhotoModeFocusDelegate(UIntPtr scene, ref float focus, ref float focusStart, ref float focusEnd, ref float exposure, [MarshalAs(UnmanagedType.U1)] ref bool vignetteOn);

	[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
	[SuppressUnmanagedCodeSecurity]
	[MonoNativeFunctionWrapper]
	public delegate float GetPhotoModeFovDelegate(UIntPtr scene);

	[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
	[SuppressUnmanagedCodeSecurity]
	[MonoNativeFunctionWrapper]
	[return: MarshalAs(UnmanagedType.U1)]
	public delegate bool GetPhotoModeOnDelegate(UIntPtr scene);

	[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
	[SuppressUnmanagedCodeSecurity]
	[MonoNativeFunctionWrapper]
	[return: MarshalAs(UnmanagedType.U1)]
	public delegate bool GetPhotoModeOrbitDelegate(UIntPtr scene);

	[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
	[SuppressUnmanagedCodeSecurity]
	[MonoNativeFunctionWrapper]
	public delegate float GetPhotoModeRollDelegate(UIntPtr scene);

	[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
	[SuppressUnmanagedCodeSecurity]
	[MonoNativeFunctionWrapper]
	public delegate void GetPhysicsMinMaxDelegate(UIntPtr scenePointer, ref Vec3 min_max);

	[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
	[SuppressUnmanagedCodeSecurity]
	[MonoNativeFunctionWrapper]
	public delegate float GetRainDensityDelegate(UIntPtr scenePointer);

	[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
	[SuppressUnmanagedCodeSecurity]
	[MonoNativeFunctionWrapper]
	public delegate void GetRootEntitiesDelegate(UIntPtr scene, UIntPtr output);

	[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
	[SuppressUnmanagedCodeSecurity]
	[MonoNativeFunctionWrapper]
	public delegate int GetRootEntityCountDelegate(UIntPtr scenePointer);

	[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
	[SuppressUnmanagedCodeSecurity]
	[MonoNativeFunctionWrapper]
	public delegate int GetSceneColorGradeIndexDelegate(UIntPtr scene);

	[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
	[SuppressUnmanagedCodeSecurity]
	[MonoNativeFunctionWrapper]
	public delegate int GetSceneFilterIndexDelegate(UIntPtr scene);

	[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
	[SuppressUnmanagedCodeSecurity]
	[MonoNativeFunctionWrapper]
	public delegate void GetSceneLimitsDelegate(UIntPtr scenePointer, ref Vec3 min, ref Vec3 max);

	[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
	[SuppressUnmanagedCodeSecurity]
	[MonoNativeFunctionWrapper]
	public delegate uint GetSceneXMLCRCDelegate(UIntPtr scenePointer);

	[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
	[SuppressUnmanagedCodeSecurity]
	[MonoNativeFunctionWrapper]
	public delegate NativeObjectPointer GetScriptedEntityDelegate(UIntPtr scenePointer, int index);

	[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
	[SuppressUnmanagedCodeSecurity]
	[MonoNativeFunctionWrapper]
	public delegate int GetScriptedEntityCountDelegate(UIntPtr scenePointer);

	[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
	[SuppressUnmanagedCodeSecurity]
	[MonoNativeFunctionWrapper]
	public delegate NativeObjectPointer GetSkyboxMeshDelegate(UIntPtr ptr);

	[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
	[SuppressUnmanagedCodeSecurity]
	[MonoNativeFunctionWrapper]
	public delegate float GetSnowDensityDelegate(UIntPtr scenePointer);

	[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
	[SuppressUnmanagedCodeSecurity]
	[MonoNativeFunctionWrapper]
	public delegate Vec2 GetSoftBoundaryVertexDelegate(UIntPtr scenePointer, int index);

	[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
	[SuppressUnmanagedCodeSecurity]
	[MonoNativeFunctionWrapper]
	public delegate int GetSoftBoundaryVertexCountDelegate(UIntPtr scenePointer);

	[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
	[SuppressUnmanagedCodeSecurity]
	[MonoNativeFunctionWrapper]
	public delegate Vec3 GetSunDirectionDelegate(UIntPtr scenePointer);

	[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
	[SuppressUnmanagedCodeSecurity]
	[MonoNativeFunctionWrapper]
	public delegate void GetTerrainDataDelegate(UIntPtr scene, out Vec2i nodeDimension, out float nodeSize, out int layerCount, out int layerVersion);

	[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
	[SuppressUnmanagedCodeSecurity]
	[MonoNativeFunctionWrapper]
	public delegate float GetTerrainHeightDelegate(UIntPtr scenePointer, Vec2 position, [MarshalAs(UnmanagedType.U1)] bool checkHoles);

	[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
	[SuppressUnmanagedCodeSecurity]
	[MonoNativeFunctionWrapper]
	public delegate void GetTerrainHeightAndNormalDelegate(UIntPtr scenePointer, Vec2 position, out float height, out Vec3 normal);

	[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
	[SuppressUnmanagedCodeSecurity]
	[MonoNativeFunctionWrapper]
	public delegate int GetTerrainMemoryUsageDelegate(UIntPtr scenePointer);

	[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
	[SuppressUnmanagedCodeSecurity]
	[MonoNativeFunctionWrapper]
	[return: MarshalAs(UnmanagedType.U1)]
	public delegate bool GetTerrainMinMaxHeightDelegate(UIntPtr scene, ref float min, ref float max);

	[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
	[SuppressUnmanagedCodeSecurity]
	[MonoNativeFunctionWrapper]
	public delegate void GetTerrainNodeDataDelegate(UIntPtr scene, int xIndex, int yIndex, out int vertexCountAlongAxis, out float quadLength, out float minHeight, out float maxHeight);

	[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
	[SuppressUnmanagedCodeSecurity]
	[MonoNativeFunctionWrapper]
	public delegate int GetTerrainPhysicsMaterialIndexAtLayerDelegate(UIntPtr scene, int layerIndex);

	[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
	[SuppressUnmanagedCodeSecurity]
	[MonoNativeFunctionWrapper]
	public delegate float GetTimeOfDayDelegate(UIntPtr scenePointer);

	[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
	[SuppressUnmanagedCodeSecurity]
	[MonoNativeFunctionWrapper]
	public delegate float GetTimeSpeedDelegate(UIntPtr scenePointer);

	[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
	[SuppressUnmanagedCodeSecurity]
	[MonoNativeFunctionWrapper]
	public delegate int GetUpgradeLevelCountDelegate(UIntPtr scenePointer);

	[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
	[SuppressUnmanagedCodeSecurity]
	[MonoNativeFunctionWrapper]
	public delegate uint GetUpgradeLevelMaskDelegate(UIntPtr scenePointer);

	[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
	[SuppressUnmanagedCodeSecurity]
	[MonoNativeFunctionWrapper]
	public delegate uint GetUpgradeLevelMaskOfLevelNameDelegate(UIntPtr scenePointer, byte[] levelName);

	[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
	[SuppressUnmanagedCodeSecurity]
	[MonoNativeFunctionWrapper]
	public delegate int GetUpgradeLevelNameOfIndexDelegate(UIntPtr scenePointer, int index);

	[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
	[SuppressUnmanagedCodeSecurity]
	[MonoNativeFunctionWrapper]
	public delegate float GetWaterLevelDelegate(UIntPtr scene);

	[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
	[SuppressUnmanagedCodeSecurity]
	[MonoNativeFunctionWrapper]
	public delegate float GetWaterLevelAtPositionDelegate(UIntPtr scene, Vec2 position, [MarshalAs(UnmanagedType.U1)] bool useWaterRenderer, [MarshalAs(UnmanagedType.U1)] bool checkWaterBodyEntities);

	[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
	[SuppressUnmanagedCodeSecurity]
	[MonoNativeFunctionWrapper]
	public delegate Vec3 GetWaterSpeedAtPositionDelegate(UIntPtr scenePointer, in Vec2 position, [MarshalAs(UnmanagedType.U1)] bool doChoppinessCorrection);

	[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
	[SuppressUnmanagedCodeSecurity]
	[MonoNativeFunctionWrapper]
	public delegate float GetWaterStrengthDelegate(UIntPtr scene);

	[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
	[SuppressUnmanagedCodeSecurity]
	[MonoNativeFunctionWrapper]
	public delegate void GetWindFlowMapDataDelegate(UIntPtr scenePointer, IntPtr flowmapData);

	[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
	[SuppressUnmanagedCodeSecurity]
	[MonoNativeFunctionWrapper]
	public delegate float GetWinterTimeFactorDelegate(UIntPtr scenePointer);

	[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
	[SuppressUnmanagedCodeSecurity]
	[MonoNativeFunctionWrapper]
	public delegate void HandleCurrentFrameTickEntitiesDelegate(UIntPtr scenePointer);

	[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
	[SuppressUnmanagedCodeSecurity]
	[MonoNativeFunctionWrapper]
	[return: MarshalAs(UnmanagedType.U1)]
	public delegate bool HasDecalRendererDelegate(UIntPtr scenePointer);

	[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
	[SuppressUnmanagedCodeSecurity]
	[MonoNativeFunctionWrapper]
	[return: MarshalAs(UnmanagedType.U1)]
	public delegate bool HasNavmeshFaceUnsharedEdgesDelegate(UIntPtr scenePointer, in PathFaceRecord faceRecord);

	[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
	[SuppressUnmanagedCodeSecurity]
	[MonoNativeFunctionWrapper]
	[return: MarshalAs(UnmanagedType.U1)]
	public delegate bool HasTerrainHeightmapDelegate(UIntPtr scenePointer);

	[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
	[SuppressUnmanagedCodeSecurity]
	[MonoNativeFunctionWrapper]
	public delegate void InvalidateTerrainPhysicsMaterialsDelegate(UIntPtr scenePointer);

	[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
	[SuppressUnmanagedCodeSecurity]
	[MonoNativeFunctionWrapper]
	[return: MarshalAs(UnmanagedType.U1)]
	public delegate bool IsAnyFaceWithIdDelegate(UIntPtr scenePointer, int faceGroupId);

	[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
	[SuppressUnmanagedCodeSecurity]
	[MonoNativeFunctionWrapper]
	[return: MarshalAs(UnmanagedType.U1)]
	public delegate bool IsAtmosphereIndoorDelegate(UIntPtr scenePointer);

	[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
	[SuppressUnmanagedCodeSecurity]
	[MonoNativeFunctionWrapper]
	[return: MarshalAs(UnmanagedType.U1)]
	public delegate bool IsDefaultEditorSceneDelegate(UIntPtr scene);

	[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
	[SuppressUnmanagedCodeSecurity]
	[MonoNativeFunctionWrapper]
	[return: MarshalAs(UnmanagedType.U1)]
	public delegate bool IsEditorSceneDelegate(UIntPtr scenePointer);

	[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
	[SuppressUnmanagedCodeSecurity]
	[MonoNativeFunctionWrapper]
	[return: MarshalAs(UnmanagedType.U1)]
	public delegate bool IsLineToPointClearDelegate(UIntPtr scenePointer, int startingFace, Vec2 position, Vec2 destination, float agentRadius);

	[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
	[SuppressUnmanagedCodeSecurity]
	[MonoNativeFunctionWrapper]
	[return: MarshalAs(UnmanagedType.U1)]
	public delegate bool IsLineToPointClear2Delegate(UIntPtr scenePointer, UIntPtr startingFace, Vec2 position, Vec2 destination, float agentRadius);

	[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
	[SuppressUnmanagedCodeSecurity]
	[MonoNativeFunctionWrapper]
	[return: MarshalAs(UnmanagedType.U1)]
	public delegate bool IsLoadingFinishedDelegate(UIntPtr scene);

	[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
	[SuppressUnmanagedCodeSecurity]
	[MonoNativeFunctionWrapper]
	[return: MarshalAs(UnmanagedType.U1)]
	public delegate bool IsMultiplayerSceneDelegate(UIntPtr scene);

	[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
	[SuppressUnmanagedCodeSecurity]
	[MonoNativeFunctionWrapper]
	[return: MarshalAs(UnmanagedType.U1)]
	public delegate bool IsPositionOnADynamicNavMeshDelegate(UIntPtr scenePointer, Vec3 position);

	[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
	[SuppressUnmanagedCodeSecurity]
	[MonoNativeFunctionWrapper]
	public delegate void LoadNavMeshPrefabDelegate(UIntPtr scenePointer, byte[] navMeshPrefabName, int navMeshGroupIdShift);

	[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
	[SuppressUnmanagedCodeSecurity]
	[MonoNativeFunctionWrapper]
	public delegate void LoadNavMeshPrefabWithFrameDelegate(UIntPtr scenePointer, byte[] navMeshPrefabName, MatrixFrame frame);

	[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
	[SuppressUnmanagedCodeSecurity]
	[MonoNativeFunctionWrapper]
	public delegate void MarkFacesWithIdAsLadderDelegate(UIntPtr scenePointer, int faceGroupId, [MarshalAs(UnmanagedType.U1)] bool isLadder);

	[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
	[SuppressUnmanagedCodeSecurity]
	[MonoNativeFunctionWrapper]
	public delegate void MergeFacesWithIdDelegate(UIntPtr scenePointer, int faceGroupId0, int faceGroupId1, int newFaceGroupId);

	[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
	[SuppressUnmanagedCodeSecurity]
	[MonoNativeFunctionWrapper]
	public delegate void OptimizeSceneDelegate(UIntPtr scenePointer, [MarshalAs(UnmanagedType.U1)] bool optimizeFlora, [MarshalAs(UnmanagedType.U1)] bool optimizeOro);

	[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
	[SuppressUnmanagedCodeSecurity]
	[MonoNativeFunctionWrapper]
	public delegate void PauseSceneSoundsDelegate(UIntPtr scenePointer);

	[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
	[SuppressUnmanagedCodeSecurity]
	[MonoNativeFunctionWrapper]
	public delegate void PreloadForRenderingDelegate(UIntPtr scenePointer);

	[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
	[SuppressUnmanagedCodeSecurity]
	[MonoNativeFunctionWrapper]
	[return: MarshalAs(UnmanagedType.U1)]
	public delegate bool RayCastExcludingTwoEntitiesDelegate(BodyFlags flags, UIntPtr scenePointer, in Ray ray, UIntPtr entity1, UIntPtr entity2);

	[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
	[SuppressUnmanagedCodeSecurity]
	[MonoNativeFunctionWrapper]
	[return: MarshalAs(UnmanagedType.U1)]
	public delegate bool RayCastForClosestEntityOrTerrainDelegate(UIntPtr scenePointer, in Vec3 sourcePoint, in Vec3 targetPoint, float rayThickness, ref float collisionDistance, ref Vec3 closestPoint, ref UIntPtr entityIndex, BodyFlags bodyExcludeFlags, [MarshalAs(UnmanagedType.U1)] bool isFixedWorld);

	[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
	[SuppressUnmanagedCodeSecurity]
	[MonoNativeFunctionWrapper]
	[return: MarshalAs(UnmanagedType.U1)]
	public delegate bool RayCastForClosestEntityOrTerrainIgnoreEntityDelegate(UIntPtr scenePointer, in Vec3 sourcePoint, in Vec3 targetPoint, float rayThickness, ref float collisionDistance, ref Vec3 closestPoint, ref UIntPtr entityIndex, BodyFlags bodyExcludeFlags, UIntPtr ignoredEntityPointer);

	[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
	[SuppressUnmanagedCodeSecurity]
	[MonoNativeFunctionWrapper]
	[return: MarshalAs(UnmanagedType.U1)]
	public delegate bool RayCastForRammingDelegate(UIntPtr scenePointer, in Vec3 sourcePoint, in Vec3 targetPoint, float rayThickness, ref float collisionDistance, ref Vec3 intersectionPoint, ref UIntPtr intersectedEntityIndex, BodyFlags bodyExcludeFlags, BodyFlags bodyIncludeFlags, UIntPtr ignoredEntityPointer);

	[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
	[SuppressUnmanagedCodeSecurity]
	[MonoNativeFunctionWrapper]
	public delegate void ReadDelegate(UIntPtr scenePointer, byte[] sceneName, ref SceneInitializationData initData, byte[] forcedAtmoName);

	[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
	[SuppressUnmanagedCodeSecurity]
	[MonoNativeFunctionWrapper]
	public delegate void ReadAndCalculateInitialCameraDelegate(UIntPtr scenePointer, ref MatrixFrame outFrame);

	[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
	[SuppressUnmanagedCodeSecurity]
	[MonoNativeFunctionWrapper]
	public delegate void ReadInModuleDelegate(UIntPtr scenePointer, byte[] sceneName, byte[] moduleId, ref SceneInitializationData initData, byte[] forcedAtmoName);

	[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
	[SuppressUnmanagedCodeSecurity]
	[MonoNativeFunctionWrapper]
	public delegate UIntPtr RegisterShipVisualToWaterRendererDelegate(UIntPtr scenePointer, UIntPtr entityPointer, in Vec3 bb);

	[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
	[SuppressUnmanagedCodeSecurity]
	[MonoNativeFunctionWrapper]
	public delegate void RemoveAlwaysRenderedSkeletonDelegate(UIntPtr scenePointer, UIntPtr skeletonPointer);

	[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
	[SuppressUnmanagedCodeSecurity]
	[MonoNativeFunctionWrapper]
	public delegate void RemoveDecalInstanceDelegate(UIntPtr scenePointer, UIntPtr decalMeshPointer, byte[] decalSetID);

	[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
	[SuppressUnmanagedCodeSecurity]
	[MonoNativeFunctionWrapper]
	public delegate void RemoveEntityDelegate(UIntPtr scenePointer, UIntPtr entityId, int removeReason);

	[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
	[SuppressUnmanagedCodeSecurity]
	[MonoNativeFunctionWrapper]
	public delegate void ResumeLoadingRenderingsDelegate(UIntPtr scenePointer);

	[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
	[SuppressUnmanagedCodeSecurity]
	[MonoNativeFunctionWrapper]
	public delegate void ResumeSceneSoundsDelegate(UIntPtr scenePointer);

	[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
	[SuppressUnmanagedCodeSecurity]
	[MonoNativeFunctionWrapper]
	public delegate void SaveNavMeshPrefabWithFrameDelegate(UIntPtr scenePointer, byte[] navMeshPrefabName, MatrixFrame frame);

	[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
	[SuppressUnmanagedCodeSecurity]
	[MonoNativeFunctionWrapper]
	[return: MarshalAs(UnmanagedType.U1)]
	public delegate bool SceneHadWaterWakeRendererDelegate(UIntPtr scenePointer);

	[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
	[SuppressUnmanagedCodeSecurity]
	[MonoNativeFunctionWrapper]
	public delegate int SelectEntitiesCollidedWithDelegate(UIntPtr scenePointer, ref Ray ray, IntPtr entityIds, IntPtr intersections);

	[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
	[SuppressUnmanagedCodeSecurity]
	[MonoNativeFunctionWrapper]
	public delegate int SelectEntitiesInBoxWithScriptComponentDelegate(UIntPtr scenePointer, ref Vec3 boundingBoxMin, ref Vec3 boundingBoxMax, IntPtr entitiesOutput, int maxCount, byte[] scriptComponentName, [MarshalAs(UnmanagedType.U1)] bool isFixedTick);

	[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
	[SuppressUnmanagedCodeSecurity]
	[MonoNativeFunctionWrapper]
	public delegate void SeparateFacesWithIdDelegate(UIntPtr scenePointer, int faceGroupId0, int faceGroupId1);

	[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
	[SuppressUnmanagedCodeSecurity]
	[MonoNativeFunctionWrapper]
	public delegate void SetAberrationOffsetDelegate(UIntPtr scenePointer, float aberrationOffset);

	[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
	[SuppressUnmanagedCodeSecurity]
	[MonoNativeFunctionWrapper]
	public delegate void SetAberrationSizeDelegate(UIntPtr scenePointer, float aberrationSize);

	[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
	[SuppressUnmanagedCodeSecurity]
	[MonoNativeFunctionWrapper]
	public delegate void SetAberrationSmoothDelegate(UIntPtr scenePointer, float aberrationSmooth);

	[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
	[SuppressUnmanagedCodeSecurity]
	[MonoNativeFunctionWrapper]
	public delegate int SetAbilityOfFacesWithIdDelegate(UIntPtr scenePointer, int faceGroupId, [MarshalAs(UnmanagedType.U1)] bool isEnabled);

	[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
	[SuppressUnmanagedCodeSecurity]
	[MonoNativeFunctionWrapper]
	public delegate void SetActiveVisibilityLevelsDelegate(UIntPtr scenePointer, byte[] levelsAppended);

	[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
	[SuppressUnmanagedCodeSecurity]
	[MonoNativeFunctionWrapper]
	public delegate void SetAntialiasingModeDelegate(UIntPtr scenePointer, [MarshalAs(UnmanagedType.U1)] bool mode);

	[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
	[SuppressUnmanagedCodeSecurity]
	[MonoNativeFunctionWrapper]
	public delegate void SetAtmosphereWithNameDelegate(UIntPtr ptr, byte[] name);

	[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
	[SuppressUnmanagedCodeSecurity]
	[MonoNativeFunctionWrapper]
	public delegate void SetBlockerDirectionForFacesWithIdDelegate(UIntPtr scenePointer, int faceGroupId, float rotation);

	[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
	[SuppressUnmanagedCodeSecurity]
	[MonoNativeFunctionWrapper]
	public delegate void SetBloomDelegate(UIntPtr scenePointer, [MarshalAs(UnmanagedType.U1)] bool mode);

	[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
	[SuppressUnmanagedCodeSecurity]
	[MonoNativeFunctionWrapper]
	public delegate void SetBloomAmountDelegate(UIntPtr scenePointer, float bloomAmount);

	[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
	[SuppressUnmanagedCodeSecurity]
	[MonoNativeFunctionWrapper]
	public delegate void SetBloomStrengthDelegate(UIntPtr scenePointer, float bloomStrength);

	[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
	[SuppressUnmanagedCodeSecurity]
	[MonoNativeFunctionWrapper]
	public delegate void SetBrightpassTresholdDelegate(UIntPtr scenePointer, float threshold);

	[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
	[SuppressUnmanagedCodeSecurity]
	[MonoNativeFunctionWrapper]
	public delegate void SetClothSimulationStateDelegate(UIntPtr scenePointer, [MarshalAs(UnmanagedType.U1)] bool state);

	[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
	[SuppressUnmanagedCodeSecurity]
	[MonoNativeFunctionWrapper]
	public delegate void SetColorGradeBlendDelegate(UIntPtr scenePointer, byte[] texture1, byte[] texture2, float alpha);

	[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
	[SuppressUnmanagedCodeSecurity]
	[MonoNativeFunctionWrapper]
	public delegate void SetDLSSModeDelegate(UIntPtr scenePointer, [MarshalAs(UnmanagedType.U1)] bool mode);

	[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
	[SuppressUnmanagedCodeSecurity]
	[MonoNativeFunctionWrapper]
	public delegate void SetDofFocusDelegate(UIntPtr scenePointer, float dofFocus);

	[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
	[SuppressUnmanagedCodeSecurity]
	[MonoNativeFunctionWrapper]
	public delegate void SetDofModeDelegate(UIntPtr scenePointer, [MarshalAs(UnmanagedType.U1)] bool mode);

	[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
	[SuppressUnmanagedCodeSecurity]
	[MonoNativeFunctionWrapper]
	public delegate void SetDofParamsDelegate(UIntPtr scenePointer, float dofFocusStart, float dofFocusEnd, [MarshalAs(UnmanagedType.U1)] bool isVignetteOn);

	[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
	[SuppressUnmanagedCodeSecurity]
	[MonoNativeFunctionWrapper]
	public delegate void SetDoNotAddEntitiesToTickListDelegate(UIntPtr scenePointer, [MarshalAs(UnmanagedType.U1)] bool value);

	[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
	[SuppressUnmanagedCodeSecurity]
	[MonoNativeFunctionWrapper]
	public delegate void SetDoNotWaitForLoadingStatesToRenderDelegate(UIntPtr scenePointer, [MarshalAs(UnmanagedType.U1)] bool value);

	[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
	[SuppressUnmanagedCodeSecurity]
	[MonoNativeFunctionWrapper]
	public delegate void SetDontLoadInvisibleEntitiesDelegate(UIntPtr scenePointer, [MarshalAs(UnmanagedType.U1)] bool value);

	[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
	[SuppressUnmanagedCodeSecurity]
	[MonoNativeFunctionWrapper]
	public delegate void SetDrynessFactorDelegate(UIntPtr scenePointer, float drynessFactor);

	[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
	[SuppressUnmanagedCodeSecurity]
	[MonoNativeFunctionWrapper]
	public delegate void SetDynamicShadowmapCascadesRadiusMultiplierDelegate(UIntPtr scenePointer, float extraRadius);

	[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
	[SuppressUnmanagedCodeSecurity]
	[MonoNativeFunctionWrapper]
	public delegate void SetDynamicSnowTextureDelegate(UIntPtr scenePointer, UIntPtr texturePointer);

	[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
	[SuppressUnmanagedCodeSecurity]
	[MonoNativeFunctionWrapper]
	public delegate void SetEnvironmentMultiplierDelegate(UIntPtr scenePointer, [MarshalAs(UnmanagedType.U1)] bool useMultiplier, float multiplier);

	[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
	[SuppressUnmanagedCodeSecurity]
	[MonoNativeFunctionWrapper]
	public delegate void SetExternalInjectionTextureDelegate(UIntPtr scenePointer, UIntPtr texturePointer);

	[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
	[SuppressUnmanagedCodeSecurity]
	[MonoNativeFunctionWrapper]
	public delegate void SetFetchCrcInfoOfSceneDelegate(UIntPtr scenePointer, [MarshalAs(UnmanagedType.U1)] bool value);

	[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
	[SuppressUnmanagedCodeSecurity]
	[MonoNativeFunctionWrapper]
	public delegate void SetFixedTickCallbackActiveDelegate(UIntPtr scene, [MarshalAs(UnmanagedType.U1)] bool isActive);

	[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
	[SuppressUnmanagedCodeSecurity]
	[MonoNativeFunctionWrapper]
	public delegate void SetFogDelegate(UIntPtr scenePointer, float fogDensity, ref Vec3 fogColor, float fogFalloff);

	[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
	[SuppressUnmanagedCodeSecurity]
	[MonoNativeFunctionWrapper]
	public delegate void SetFogAdvancedDelegate(UIntPtr scenePointer, float fogFalloffOffset, float fogFalloffMinFog, float fogFalloffStartDist);

	[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
	[SuppressUnmanagedCodeSecurity]
	[MonoNativeFunctionWrapper]
	public delegate void SetFogAmbientColorDelegate(UIntPtr scenePointer, ref Vec3 fogAmbientColor);

	[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
	[SuppressUnmanagedCodeSecurity]
	[MonoNativeFunctionWrapper]
	public delegate void SetForcedSnowDelegate(UIntPtr scenePointer, [MarshalAs(UnmanagedType.U1)] bool value);

	[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
	[SuppressUnmanagedCodeSecurity]
	[MonoNativeFunctionWrapper]
	public delegate void SetGlobalWindStrengthVectorDelegate(UIntPtr scenePointer, in Vec2 strengthVector);

	[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
	[SuppressUnmanagedCodeSecurity]
	[MonoNativeFunctionWrapper]
	public delegate void SetGlobalWindVelocityDelegate(UIntPtr scenePointer, in Vec2 windVelocity);

	[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
	[SuppressUnmanagedCodeSecurity]
	[MonoNativeFunctionWrapper]
	public delegate void SetGrainAmountDelegate(UIntPtr scenePointer, float grainAmount);

	[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
	[SuppressUnmanagedCodeSecurity]
	[MonoNativeFunctionWrapper]
	public delegate void SetHexagonVignetteAlphaDelegate(UIntPtr scenePointer, float Alpha);

	[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
	[SuppressUnmanagedCodeSecurity]
	[MonoNativeFunctionWrapper]
	public delegate void SetHexagonVignetteColorDelegate(UIntPtr scenePointer, ref Vec3 p_hexagon_vignette_color);

	[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
	[SuppressUnmanagedCodeSecurity]
	[MonoNativeFunctionWrapper]
	public delegate void SetHumidityDelegate(UIntPtr scenePointer, float humidity);

	[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
	[SuppressUnmanagedCodeSecurity]
	[MonoNativeFunctionWrapper]
	public delegate void SetLandscapeRainMaskDataDelegate(UIntPtr scenePointer, ManagedArray data);

	[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
	[SuppressUnmanagedCodeSecurity]
	[MonoNativeFunctionWrapper]
	public delegate void SetLensDistortionDelegate(UIntPtr scenePointer, float lensDistortion);

	[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
	[SuppressUnmanagedCodeSecurity]
	[MonoNativeFunctionWrapper]
	public delegate void SetLensFlareAberrationOffsetDelegate(UIntPtr scenePointer, float lensFlareAberrationOffset);

	[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
	[SuppressUnmanagedCodeSecurity]
	[MonoNativeFunctionWrapper]
	public delegate void SetLensFlareAmountDelegate(UIntPtr scenePointer, float lensFlareAmount);

	[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
	[SuppressUnmanagedCodeSecurity]
	[MonoNativeFunctionWrapper]
	public delegate void SetLensFlareBlurSigmaDelegate(UIntPtr scenePointer, float lensFlareBlurSigma);

	[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
	[SuppressUnmanagedCodeSecurity]
	[MonoNativeFunctionWrapper]
	public delegate void SetLensFlareBlurSizeDelegate(UIntPtr scenePointer, int lensFlareBlurSize);

	[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
	[SuppressUnmanagedCodeSecurity]
	[MonoNativeFunctionWrapper]
	public delegate void SetLensFlareDiffractionWeightDelegate(UIntPtr scenePointer, float lensFlareDiffractionWeight);

	[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
	[SuppressUnmanagedCodeSecurity]
	[MonoNativeFunctionWrapper]
	public delegate void SetLensFlareDirtWeightDelegate(UIntPtr scenePointer, float lensFlareDirtWeight);

	[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
	[SuppressUnmanagedCodeSecurity]
	[MonoNativeFunctionWrapper]
	public delegate void SetLensFlareGhostSamplesDelegate(UIntPtr scenePointer, int lensFlareGhostSamples);

	[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
	[SuppressUnmanagedCodeSecurity]
	[MonoNativeFunctionWrapper]
	public delegate void SetLensFlareGhostWeightDelegate(UIntPtr scenePointer, float lensFlareGhostWeight);

	[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
	[SuppressUnmanagedCodeSecurity]
	[MonoNativeFunctionWrapper]
	public delegate void SetLensFlareHaloWeightDelegate(UIntPtr scenePointer, float lensFlareHaloWeight);

	[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
	[SuppressUnmanagedCodeSecurity]
	[MonoNativeFunctionWrapper]
	public delegate void SetLensFlareHaloWidthDelegate(UIntPtr scenePointer, float lensFlareHaloWidth);

	[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
	[SuppressUnmanagedCodeSecurity]
	[MonoNativeFunctionWrapper]
	public delegate void SetLensFlareStrengthDelegate(UIntPtr scenePointer, float lensFlareStrength);

	[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
	[SuppressUnmanagedCodeSecurity]
	[MonoNativeFunctionWrapper]
	public delegate void SetLensFlareThresholdDelegate(UIntPtr scenePointer, float lensFlareThreshold);

	[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
	[SuppressUnmanagedCodeSecurity]
	[MonoNativeFunctionWrapper]
	public delegate void SetLightDiffuseColorDelegate(UIntPtr scenePointer, int lightIndex, Vec3 diffuseColor);

	[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
	[SuppressUnmanagedCodeSecurity]
	[MonoNativeFunctionWrapper]
	public delegate void SetLightDirectionDelegate(UIntPtr scenePointer, int lightIndex, Vec3 direction);

	[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
	[SuppressUnmanagedCodeSecurity]
	[MonoNativeFunctionWrapper]
	public delegate void SetLightPositionDelegate(UIntPtr scenePointer, int lightIndex, Vec3 position);

	[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
	[SuppressUnmanagedCodeSecurity]
	[MonoNativeFunctionWrapper]
	public delegate void SetMaxExposureDelegate(UIntPtr scenePointer, float maxExposure);

	[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
	[SuppressUnmanagedCodeSecurity]
	[MonoNativeFunctionWrapper]
	public delegate void SetMiddleGrayDelegate(UIntPtr scenePointer, float middleGray);

	[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
	[SuppressUnmanagedCodeSecurity]
	[MonoNativeFunctionWrapper]
	public delegate void SetMieScatterFocusDelegate(UIntPtr scenePointer, float strength);

	[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
	[SuppressUnmanagedCodeSecurity]
	[MonoNativeFunctionWrapper]
	public delegate void SetMieScatterStrengthDelegate(UIntPtr scenePointer, float strength);

	[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
	[SuppressUnmanagedCodeSecurity]
	[MonoNativeFunctionWrapper]
	public delegate void SetMinExposureDelegate(UIntPtr scenePointer, float minExposure);

	[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
	[SuppressUnmanagedCodeSecurity]
	[MonoNativeFunctionWrapper]
	public delegate void SetMotionBlurModeDelegate(UIntPtr scenePointer, [MarshalAs(UnmanagedType.U1)] bool mode);

	[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
	[SuppressUnmanagedCodeSecurity]
	[MonoNativeFunctionWrapper]
	public delegate void SetNameDelegate(UIntPtr scenePointer, byte[] name);

	[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
	[SuppressUnmanagedCodeSecurity]
	[MonoNativeFunctionWrapper]
	public delegate void SetNavMeshRegionMapDelegate(UIntPtr scenePointer, IntPtr regionMap, int regionMapSize);

	[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
	[SuppressUnmanagedCodeSecurity]
	[MonoNativeFunctionWrapper]
	public delegate void SetOcclusionModeDelegate(UIntPtr scenePointer, [MarshalAs(UnmanagedType.U1)] bool mode);

	[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
	[SuppressUnmanagedCodeSecurity]
	[MonoNativeFunctionWrapper]
	public delegate void SetOnCollisionFilterCallbackActiveDelegate(UIntPtr scene, [MarshalAs(UnmanagedType.U1)] bool isActive);

	[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
	[SuppressUnmanagedCodeSecurity]
	[MonoNativeFunctionWrapper]
	public delegate void SetOwnerThreadDelegate(UIntPtr scenePointer);

	[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
	[SuppressUnmanagedCodeSecurity]
	[MonoNativeFunctionWrapper]
	public delegate void SetPhotoAtmosphereViaTodDelegate(UIntPtr scenePointer, float tod, [MarshalAs(UnmanagedType.U1)] bool withStorm);

	[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
	[SuppressUnmanagedCodeSecurity]
	[MonoNativeFunctionWrapper]
	public delegate void SetPhotoModeFocusDelegate(UIntPtr scene, float focusStart, float focusEnd, float focus, float exposure);

	[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
	[SuppressUnmanagedCodeSecurity]
	[MonoNativeFunctionWrapper]
	public delegate void SetPhotoModeFovDelegate(UIntPtr scene, float verticalFov);

	[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
	[SuppressUnmanagedCodeSecurity]
	[MonoNativeFunctionWrapper]
	public delegate void SetPhotoModeOnDelegate(UIntPtr scene, [MarshalAs(UnmanagedType.U1)] bool on);

	[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
	[SuppressUnmanagedCodeSecurity]
	[MonoNativeFunctionWrapper]
	public delegate void SetPhotoModeOrbitDelegate(UIntPtr scene, [MarshalAs(UnmanagedType.U1)] bool orbit);

	[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
	[SuppressUnmanagedCodeSecurity]
	[MonoNativeFunctionWrapper]
	public delegate void SetPhotoModeRollDelegate(UIntPtr scene, float roll);

	[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
	[SuppressUnmanagedCodeSecurity]
	[MonoNativeFunctionWrapper]
	public delegate void SetPhotoModeVignetteDelegate(UIntPtr scene, [MarshalAs(UnmanagedType.U1)] bool vignetteOn);

	[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
	[SuppressUnmanagedCodeSecurity]
	[MonoNativeFunctionWrapper]
	public delegate void SetPlaySoundEventsAfterReadyToRenderDelegate(UIntPtr ptr, [MarshalAs(UnmanagedType.U1)] bool value);

	[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
	[SuppressUnmanagedCodeSecurity]
	[MonoNativeFunctionWrapper]
	public delegate void SetRainDensityDelegate(UIntPtr scenePointer, float density);

	[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
	[SuppressUnmanagedCodeSecurity]
	[MonoNativeFunctionWrapper]
	public delegate void SetSceneColorGradeDelegate(UIntPtr scene, byte[] textureName);

	[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
	[SuppressUnmanagedCodeSecurity]
	[MonoNativeFunctionWrapper]
	public delegate void SetSceneColorGradeIndexDelegate(UIntPtr scene, int index);

	[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
	[SuppressUnmanagedCodeSecurity]
	[MonoNativeFunctionWrapper]
	public delegate int SetSceneFilterIndexDelegate(UIntPtr scene, int index);

	[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
	[SuppressUnmanagedCodeSecurity]
	[MonoNativeFunctionWrapper]
	public delegate void SetShadowDelegate(UIntPtr scenePointer, [MarshalAs(UnmanagedType.U1)] bool shadowEnabled);

	[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
	[SuppressUnmanagedCodeSecurity]
	[MonoNativeFunctionWrapper]
	public delegate void SetSkyBrightnessDelegate(UIntPtr scenePointer, float brightness);

	[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
	[SuppressUnmanagedCodeSecurity]
	[MonoNativeFunctionWrapper]
	public delegate void SetSkyRotationDelegate(UIntPtr scenePointer, float rotation);

	[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
	[SuppressUnmanagedCodeSecurity]
	[MonoNativeFunctionWrapper]
	public delegate void SetSnowDensityDelegate(UIntPtr scenePointer, float density);

	[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
	[SuppressUnmanagedCodeSecurity]
	[MonoNativeFunctionWrapper]
	public delegate void SetStreakAmountDelegate(UIntPtr scenePointer, float streakAmount);

	[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
	[SuppressUnmanagedCodeSecurity]
	[MonoNativeFunctionWrapper]
	public delegate void SetStreakIntensityDelegate(UIntPtr scenePointer, float stretchAmount);

	[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
	[SuppressUnmanagedCodeSecurity]
	[MonoNativeFunctionWrapper]
	public delegate void SetStreakStrengthDelegate(UIntPtr scenePointer, float strengthAmount);

	[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
	[SuppressUnmanagedCodeSecurity]
	[MonoNativeFunctionWrapper]
	public delegate void SetStreakStretchDelegate(UIntPtr scenePointer, float stretchAmount);

	[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
	[SuppressUnmanagedCodeSecurity]
	[MonoNativeFunctionWrapper]
	public delegate void SetStreakThresholdDelegate(UIntPtr scenePointer, float streakThreshold);

	[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
	[SuppressUnmanagedCodeSecurity]
	[MonoNativeFunctionWrapper]
	public delegate void SetStreakTintDelegate(UIntPtr scenePointer, ref Vec3 p_streak_tint_color);

	[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
	[SuppressUnmanagedCodeSecurity]
	[MonoNativeFunctionWrapper]
	public delegate void SetSunDelegate(UIntPtr scenePointer, Vec3 color, float altitude, float angle, float intensity);

	[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
	[SuppressUnmanagedCodeSecurity]
	[MonoNativeFunctionWrapper]
	public delegate void SetSunAngleAltitudeDelegate(UIntPtr scenePointer, float angle, float altitude);

	[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
	[SuppressUnmanagedCodeSecurity]
	[MonoNativeFunctionWrapper]
	public delegate void SetSunDirectionDelegate(UIntPtr scenePointer, Vec3 direction);

	[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
	[SuppressUnmanagedCodeSecurity]
	[MonoNativeFunctionWrapper]
	public delegate void SetSunLightDelegate(UIntPtr scenePointer, Vec3 color, Vec3 direction);

	[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
	[SuppressUnmanagedCodeSecurity]
	[MonoNativeFunctionWrapper]
	public delegate void SetSunshaftModeDelegate(UIntPtr scenePointer, [MarshalAs(UnmanagedType.U1)] bool mode);

	[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
	[SuppressUnmanagedCodeSecurity]
	[MonoNativeFunctionWrapper]
	public delegate void SetSunShaftStrengthDelegate(UIntPtr scenePointer, float strength);

	[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
	[SuppressUnmanagedCodeSecurity]
	[MonoNativeFunctionWrapper]
	public delegate void SetSunSizeDelegate(UIntPtr scenePointer, float size);

	[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
	[SuppressUnmanagedCodeSecurity]
	[MonoNativeFunctionWrapper]
	public delegate void SetTargetExposureDelegate(UIntPtr scenePointer, float targetExposure);

	[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
	[SuppressUnmanagedCodeSecurity]
	[MonoNativeFunctionWrapper]
	public delegate void SetTemperatureDelegate(UIntPtr scenePointer, float temperature);

	[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
	[SuppressUnmanagedCodeSecurity]
	[MonoNativeFunctionWrapper]
	public delegate void SetTerrainDynamicParamsDelegate(UIntPtr scenePointer, Vec3 dynamic_params);

	[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
	[SuppressUnmanagedCodeSecurity]
	[MonoNativeFunctionWrapper]
	public delegate void SetTimeOfDayDelegate(UIntPtr scenePointer, float value);

	[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
	[SuppressUnmanagedCodeSecurity]
	[MonoNativeFunctionWrapper]
	public delegate void SetTimeSpeedDelegate(UIntPtr scenePointer, float value);

	[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
	[SuppressUnmanagedCodeSecurity]
	[MonoNativeFunctionWrapper]
	public delegate void SetUpgradeLevelDelegate(UIntPtr scenePointer, int level);

	[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
	[SuppressUnmanagedCodeSecurity]
	[MonoNativeFunctionWrapper]
	public delegate void SetUpgradeLevelVisibilityDelegate(UIntPtr scenePointer, byte[] concatLevels);

	[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
	[SuppressUnmanagedCodeSecurity]
	[MonoNativeFunctionWrapper]
	public delegate void SetUpgradeLevelVisibilityWithMaskDelegate(UIntPtr scenePointer, uint mask);

	[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
	[SuppressUnmanagedCodeSecurity]
	[MonoNativeFunctionWrapper]
	public delegate void SetUseAdvancedWaterRenderingDelegate(UIntPtr scenePointer, [MarshalAs(UnmanagedType.U1)] bool value);

	[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
	[SuppressUnmanagedCodeSecurity]
	[MonoNativeFunctionWrapper]
	public delegate void SetUseConstantTimeDelegate(UIntPtr ptr, [MarshalAs(UnmanagedType.U1)] bool value);

	[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
	[SuppressUnmanagedCodeSecurity]
	[MonoNativeFunctionWrapper]
	public delegate void SetUsesDeleteLaterSystemDelegate(UIntPtr scenePointer, [MarshalAs(UnmanagedType.U1)] bool value);

	[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
	[SuppressUnmanagedCodeSecurity]
	[MonoNativeFunctionWrapper]
	public delegate void SetVignetteInnerRadiusDelegate(UIntPtr scenePointer, float vignetteInnerRadius);

	[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
	[SuppressUnmanagedCodeSecurity]
	[MonoNativeFunctionWrapper]
	public delegate void SetVignetteOpacityDelegate(UIntPtr scenePointer, float vignetteOpacity);

	[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
	[SuppressUnmanagedCodeSecurity]
	[MonoNativeFunctionWrapper]
	public delegate void SetVignetteOuterRadiusDelegate(UIntPtr scenePointer, float vignetteOuterRadius);

	[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
	[SuppressUnmanagedCodeSecurity]
	[MonoNativeFunctionWrapper]
	public delegate void SetWaterStrengthDelegate(UIntPtr scene, float newWaterStrength);

	[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
	[SuppressUnmanagedCodeSecurity]
	[MonoNativeFunctionWrapper]
	public delegate void SetWaterWakeCameraOffsetDelegate(UIntPtr scenePointer, float cameraOffset);

	[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
	[SuppressUnmanagedCodeSecurity]
	[MonoNativeFunctionWrapper]
	public delegate void SetWaterWakeWorldSizeDelegate(UIntPtr scenePointer, float worldSize, float eraseFactor);

	[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
	[SuppressUnmanagedCodeSecurity]
	[MonoNativeFunctionWrapper]
	public delegate void SetWinterTimeFactorDelegate(UIntPtr scenePointer, float winterTimeFactor);

	[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
	[SuppressUnmanagedCodeSecurity]
	[MonoNativeFunctionWrapper]
	public delegate void StallLoadingRenderingsUntilFurtherNoticeDelegate(UIntPtr scenePointer);

	[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
	[SuppressUnmanagedCodeSecurity]
	[MonoNativeFunctionWrapper]
	[return: MarshalAs(UnmanagedType.U1)]
	public delegate bool SwapFaceConnectionsWithIdDelegate(UIntPtr scenePointer, int hubFaceGroupID, int toBeSeparatedFaceGroupId, int toBeMergedFaceGroupId, [MarshalAs(UnmanagedType.U1)] bool canFail);

	[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
	[SuppressUnmanagedCodeSecurity]
	[MonoNativeFunctionWrapper]
	public delegate int TakePhotoModePictureDelegate(UIntPtr scene, [MarshalAs(UnmanagedType.U1)] bool saveAmbientOcclusionPass, [MarshalAs(UnmanagedType.U1)] bool saveObjectIdPass, [MarshalAs(UnmanagedType.U1)] bool saveShadowPass);

	[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
	[SuppressUnmanagedCodeSecurity]
	[MonoNativeFunctionWrapper]
	public delegate void TickDelegate(UIntPtr scenePointer, float deltaTime);

	[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
	[SuppressUnmanagedCodeSecurity]
	[MonoNativeFunctionWrapper]
	public delegate void TickWakeDelegate(UIntPtr scenePointer, float dt);

	[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
	[SuppressUnmanagedCodeSecurity]
	[MonoNativeFunctionWrapper]
	public delegate void WaitWaterRendererCPUSimulationDelegate(UIntPtr scenePointer);

	[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
	[SuppressUnmanagedCodeSecurity]
	[MonoNativeFunctionWrapper]
	public delegate void WorldPositionComputeNearestNavMeshDelegate(ref WorldPosition position);

	[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
	[SuppressUnmanagedCodeSecurity]
	[MonoNativeFunctionWrapper]
	public delegate void WorldPositionValidateZDelegate(ref WorldPosition position, int minimumValidityState);

	private static readonly Encoding _utf8 = Encoding.UTF8;

	public static AddAlwaysRenderedSkeletonDelegate call_AddAlwaysRenderedSkeletonDelegate;

	public static AddDecalInstanceDelegate call_AddDecalInstanceDelegate;

	public static AddDirectionalLightDelegate call_AddDirectionalLightDelegate;

	public static AddEntityWithMeshDelegate call_AddEntityWithMeshDelegate;

	public static AddEntityWithMultiMeshDelegate call_AddEntityWithMultiMeshDelegate;

	public static AddItemEntityDelegate call_AddItemEntityDelegate;

	public static AddPathDelegate call_AddPathDelegate;

	public static AddPathPointDelegate call_AddPathPointDelegate;

	public static AddPointLightDelegate call_AddPointLightDelegate;

	public static AddWaterWakeWithCapsuleDelegate call_AddWaterWakeWithCapsuleDelegate;

	public static AttachEntityDelegate call_AttachEntityDelegate;

	public static BoxCastDelegate call_BoxCastDelegate;

	public static BoxCastOnlyForCameraDelegate call_BoxCastOnlyForCameraDelegate;

	public static CalculateEffectiveLightingDelegate call_CalculateEffectiveLightingDelegate;

	public static CheckPathEntitiesFrameChangedDelegate call_CheckPathEntitiesFrameChangedDelegate;

	public static CheckPointCanSeePointDelegate call_CheckPointCanSeePointDelegate;

	public static CheckResourcesDelegate call_CheckResourcesDelegate;

	public static ClearAllDelegate call_ClearAllDelegate;

	public static ClearCurrentFrameTickEntitiesDelegate call_ClearCurrentFrameTickEntitiesDelegate;

	public static ClearDecalsDelegate call_ClearDecalsDelegate;

	public static ClearNavMeshDelegate call_ClearNavMeshDelegate;

	public static ContainsTerrainDelegate call_ContainsTerrainDelegate;

	public static CreateBurstParticleDelegate call_CreateBurstParticleDelegate;

	public static CreateDynamicRainTextureDelegate call_CreateDynamicRainTextureDelegate;

	public static CreateNewSceneDelegate call_CreateNewSceneDelegate;

	public static CreatePathMeshDelegate call_CreatePathMeshDelegate;

	public static CreatePathMesh2Delegate call_CreatePathMesh2Delegate;

	public static DeletePathWithNameDelegate call_DeletePathWithNameDelegate;

	public static DeleteWaterWakeRendererDelegate call_DeleteWaterWakeRendererDelegate;

	public static DeRegisterShipVisualDelegate call_DeRegisterShipVisualDelegate;

	public static DisableStaticShadowsDelegate call_DisableStaticShadowsDelegate;

	public static DoesPathExistBetweenFacesDelegate call_DoesPathExistBetweenFacesDelegate;

	public static DoesPathExistBetweenPositionsDelegate call_DoesPathExistBetweenPositionsDelegate;

	public static EnableFixedTickDelegate call_EnableFixedTickDelegate;

	public static EnableInclusiveAsyncPhysxDelegate call_EnableInclusiveAsyncPhysxDelegate;

	public static EnsurePostfxSystemDelegate call_EnsurePostfxSystemDelegate;

	public static EnsureWaterWakeRendererDelegate call_EnsureWaterWakeRendererDelegate;

	public static FillEntityWithHardBorderPhysicsBarrierDelegate call_FillEntityWithHardBorderPhysicsBarrierDelegate;

	public static FillTerrainHeightDataDelegate call_FillTerrainHeightDataDelegate;

	public static FillTerrainPhysicsMaterialIndexDataDelegate call_FillTerrainPhysicsMaterialIndexDataDelegate;

	public static FindClosestExitPositionForPositionOnABoundaryFaceDelegate call_FindClosestExitPositionForPositionOnABoundaryFaceDelegate;

	public static FinishSceneSoundsDelegate call_FinishSceneSoundsDelegate;

	public static FocusRayCastForFixedPhysicsDelegate call_FocusRayCastForFixedPhysicsDelegate;

	public static ForceLoadResourcesDelegate call_ForceLoadResourcesDelegate;

	public static GenerateContactsWithCapsuleDelegate call_GenerateContactsWithCapsuleDelegate;

	public static GenerateContactsWithCapsuleAgainstEntityDelegate call_GenerateContactsWithCapsuleAgainstEntityDelegate;

	public static GetAllColorGradeNamesDelegate call_GetAllColorGradeNamesDelegate;

	public static GetAllEntitiesWithScriptComponentDelegate call_GetAllEntitiesWithScriptComponentDelegate;

	public static GetAllFilterNamesDelegate call_GetAllFilterNamesDelegate;

	public static GetAllNavmeshFaceRecordsDelegate call_GetAllNavmeshFaceRecordsDelegate;

	public static GetBoundingBoxDelegate call_GetBoundingBoxDelegate;

	public static GetBulkWaterLevelAtPositionsDelegate call_GetBulkWaterLevelAtPositionsDelegate;

	public static GetBulkWaterLevelAtVolumesDelegate call_GetBulkWaterLevelAtVolumesDelegate;

	public static GetCampaignEntityWithNameDelegate call_GetCampaignEntityWithNameDelegate;

	public static GetEnginePhysicsEnabledDelegate call_GetEnginePhysicsEnabledDelegate;

	public static GetEntitiesDelegate call_GetEntitiesDelegate;

	public static GetEntityCountDelegate call_GetEntityCountDelegate;

	public static GetEntityWithGuidDelegate call_GetEntityWithGuidDelegate;

	public static GetFallDensityDelegate call_GetFallDensityDelegate;

	public static GetFirstEntityWithNameDelegate call_GetFirstEntityWithNameDelegate;

	public static GetFirstEntityWithScriptComponentDelegate call_GetFirstEntityWithScriptComponentDelegate;

	public static GetFloraInstanceCountDelegate call_GetFloraInstanceCountDelegate;

	public static GetFloraRendererTextureUsageDelegate call_GetFloraRendererTextureUsageDelegate;

	public static GetFogDelegate call_GetFogDelegate;

	public static GetGlobalWindStrengthVectorDelegate call_GetGlobalWindStrengthVectorDelegate;

	public static GetGlobalWindVelocityDelegate call_GetGlobalWindVelocityDelegate;

	public static GetGroundHeightAndBodyFlagsAtPositionDelegate call_GetGroundHeightAndBodyFlagsAtPositionDelegate;

	public static GetGroundHeightAndNormalAtPositionDelegate call_GetGroundHeightAndNormalAtPositionDelegate;

	public static GetGroundHeightAtPositionDelegate call_GetGroundHeightAtPositionDelegate;

	public static GetHardBoundaryVertexDelegate call_GetHardBoundaryVertexDelegate;

	public static GetHardBoundaryVertexCountDelegate call_GetHardBoundaryVertexCountDelegate;

	public static GetHeightAtPointDelegate call_GetHeightAtPointDelegate;

	public static GetIdOfNavMeshFaceDelegate call_GetIdOfNavMeshFaceDelegate;

	public static GetInterpolationFactorForBodyWorldTransformSmoothingDelegate call_GetInterpolationFactorForBodyWorldTransformSmoothingDelegate;

	public static GetLastFinalRenderCameraFrameDelegate call_GetLastFinalRenderCameraFrameDelegate;

	public static GetLastFinalRenderCameraPositionDelegate call_GetLastFinalRenderCameraPositionDelegate;

	public static GetLastPointOnNavigationMeshFromPositionToDestinationDelegate call_GetLastPointOnNavigationMeshFromPositionToDestinationDelegate;

	public static GetLastPointOnNavigationMeshFromWorldPositionToDestinationDelegate call_GetLastPointOnNavigationMeshFromWorldPositionToDestinationDelegate;

	public static GetLastPositionOnNavMeshFaceForPointAndDirectionDelegate call_GetLastPositionOnNavMeshFaceForPointAndDirectionDelegate;

	public static GetLoadingStateNameDelegate call_GetLoadingStateNameDelegate;

	public static GetModulePathDelegate call_GetModulePathDelegate;

	public static GetNameDelegate call_GetNameDelegate;

	public static GetNavigationMeshCRCDelegate call_GetNavigationMeshCRCDelegate;

	public static GetNavigationMeshForPositionDelegate call_GetNavigationMeshForPositionDelegate;

	public static GetNavMeshFaceCenterPositionDelegate call_GetNavMeshFaceCenterPositionDelegate;

	public static GetNavMeshFaceCountDelegate call_GetNavMeshFaceCountDelegate;

	public static GetNavmeshFaceCountBetweenTwoIdsDelegate call_GetNavmeshFaceCountBetweenTwoIdsDelegate;

	public static GetNavMeshFaceFirstVertexZDelegate call_GetNavMeshFaceFirstVertexZDelegate;

	public static GetNavMeshFaceIndexDelegate call_GetNavMeshFaceIndexDelegate;

	public static GetNavMeshFaceIndex3Delegate call_GetNavMeshFaceIndex3Delegate;

	public static GetNavmeshFaceRecordsBetweenTwoIdsDelegate call_GetNavmeshFaceRecordsBetweenTwoIdsDelegate;

	public static GetNavMeshPathFaceRecordDelegate call_GetNavMeshPathFaceRecordDelegate;

	public static GetNearestNavigationMeshForPositionDelegate call_GetNearestNavigationMeshForPositionDelegate;

	public static GetNodeDataCountDelegate call_GetNodeDataCountDelegate;

	public static GetNormalAtDelegate call_GetNormalAtDelegate;

	public static GetNorthAngleDelegate call_GetNorthAngleDelegate;

	public static GetNumberOfPathsWithNamePrefixDelegate call_GetNumberOfPathsWithNamePrefixDelegate;

	public static GetPathBetweenAIFaceIndicesDelegate call_GetPathBetweenAIFaceIndicesDelegate;

	public static GetPathBetweenAIFaceIndicesWithRegionSwitchCostDelegate call_GetPathBetweenAIFaceIndicesWithRegionSwitchCostDelegate;

	public static GetPathBetweenAIFacePointersDelegate call_GetPathBetweenAIFacePointersDelegate;

	public static GetPathBetweenAIFacePointersWithRegionSwitchCostDelegate call_GetPathBetweenAIFacePointersWithRegionSwitchCostDelegate;

	public static GetPathDistanceBetweenAIFacesDelegate call_GetPathDistanceBetweenAIFacesDelegate;

	public static GetPathDistanceBetweenPositionsDelegate call_GetPathDistanceBetweenPositionsDelegate;

	public static GetPathFaceRecordFromNavMeshFacePointerDelegate call_GetPathFaceRecordFromNavMeshFacePointerDelegate;

	public static GetPathsWithNamePrefixDelegate call_GetPathsWithNamePrefixDelegate;

	public static GetPathWithNameDelegate call_GetPathWithNameDelegate;

	public static GetPhotoModeFocusDelegate call_GetPhotoModeFocusDelegate;

	public static GetPhotoModeFovDelegate call_GetPhotoModeFovDelegate;

	public static GetPhotoModeOnDelegate call_GetPhotoModeOnDelegate;

	public static GetPhotoModeOrbitDelegate call_GetPhotoModeOrbitDelegate;

	public static GetPhotoModeRollDelegate call_GetPhotoModeRollDelegate;

	public static GetPhysicsMinMaxDelegate call_GetPhysicsMinMaxDelegate;

	public static GetRainDensityDelegate call_GetRainDensityDelegate;

	public static GetRootEntitiesDelegate call_GetRootEntitiesDelegate;

	public static GetRootEntityCountDelegate call_GetRootEntityCountDelegate;

	public static GetSceneColorGradeIndexDelegate call_GetSceneColorGradeIndexDelegate;

	public static GetSceneFilterIndexDelegate call_GetSceneFilterIndexDelegate;

	public static GetSceneLimitsDelegate call_GetSceneLimitsDelegate;

	public static GetSceneXMLCRCDelegate call_GetSceneXMLCRCDelegate;

	public static GetScriptedEntityDelegate call_GetScriptedEntityDelegate;

	public static GetScriptedEntityCountDelegate call_GetScriptedEntityCountDelegate;

	public static GetSkyboxMeshDelegate call_GetSkyboxMeshDelegate;

	public static GetSnowDensityDelegate call_GetSnowDensityDelegate;

	public static GetSoftBoundaryVertexDelegate call_GetSoftBoundaryVertexDelegate;

	public static GetSoftBoundaryVertexCountDelegate call_GetSoftBoundaryVertexCountDelegate;

	public static GetSunDirectionDelegate call_GetSunDirectionDelegate;

	public static GetTerrainDataDelegate call_GetTerrainDataDelegate;

	public static GetTerrainHeightDelegate call_GetTerrainHeightDelegate;

	public static GetTerrainHeightAndNormalDelegate call_GetTerrainHeightAndNormalDelegate;

	public static GetTerrainMemoryUsageDelegate call_GetTerrainMemoryUsageDelegate;

	public static GetTerrainMinMaxHeightDelegate call_GetTerrainMinMaxHeightDelegate;

	public static GetTerrainNodeDataDelegate call_GetTerrainNodeDataDelegate;

	public static GetTerrainPhysicsMaterialIndexAtLayerDelegate call_GetTerrainPhysicsMaterialIndexAtLayerDelegate;

	public static GetTimeOfDayDelegate call_GetTimeOfDayDelegate;

	public static GetTimeSpeedDelegate call_GetTimeSpeedDelegate;

	public static GetUpgradeLevelCountDelegate call_GetUpgradeLevelCountDelegate;

	public static GetUpgradeLevelMaskDelegate call_GetUpgradeLevelMaskDelegate;

	public static GetUpgradeLevelMaskOfLevelNameDelegate call_GetUpgradeLevelMaskOfLevelNameDelegate;

	public static GetUpgradeLevelNameOfIndexDelegate call_GetUpgradeLevelNameOfIndexDelegate;

	public static GetWaterLevelDelegate call_GetWaterLevelDelegate;

	public static GetWaterLevelAtPositionDelegate call_GetWaterLevelAtPositionDelegate;

	public static GetWaterSpeedAtPositionDelegate call_GetWaterSpeedAtPositionDelegate;

	public static GetWaterStrengthDelegate call_GetWaterStrengthDelegate;

	public static GetWindFlowMapDataDelegate call_GetWindFlowMapDataDelegate;

	public static GetWinterTimeFactorDelegate call_GetWinterTimeFactorDelegate;

	public static HandleCurrentFrameTickEntitiesDelegate call_HandleCurrentFrameTickEntitiesDelegate;

	public static HasDecalRendererDelegate call_HasDecalRendererDelegate;

	public static HasNavmeshFaceUnsharedEdgesDelegate call_HasNavmeshFaceUnsharedEdgesDelegate;

	public static HasTerrainHeightmapDelegate call_HasTerrainHeightmapDelegate;

	public static InvalidateTerrainPhysicsMaterialsDelegate call_InvalidateTerrainPhysicsMaterialsDelegate;

	public static IsAnyFaceWithIdDelegate call_IsAnyFaceWithIdDelegate;

	public static IsAtmosphereIndoorDelegate call_IsAtmosphereIndoorDelegate;

	public static IsDefaultEditorSceneDelegate call_IsDefaultEditorSceneDelegate;

	public static IsEditorSceneDelegate call_IsEditorSceneDelegate;

	public static IsLineToPointClearDelegate call_IsLineToPointClearDelegate;

	public static IsLineToPointClear2Delegate call_IsLineToPointClear2Delegate;

	public static IsLoadingFinishedDelegate call_IsLoadingFinishedDelegate;

	public static IsMultiplayerSceneDelegate call_IsMultiplayerSceneDelegate;

	public static IsPositionOnADynamicNavMeshDelegate call_IsPositionOnADynamicNavMeshDelegate;

	public static LoadNavMeshPrefabDelegate call_LoadNavMeshPrefabDelegate;

	public static LoadNavMeshPrefabWithFrameDelegate call_LoadNavMeshPrefabWithFrameDelegate;

	public static MarkFacesWithIdAsLadderDelegate call_MarkFacesWithIdAsLadderDelegate;

	public static MergeFacesWithIdDelegate call_MergeFacesWithIdDelegate;

	public static OptimizeSceneDelegate call_OptimizeSceneDelegate;

	public static PauseSceneSoundsDelegate call_PauseSceneSoundsDelegate;

	public static PreloadForRenderingDelegate call_PreloadForRenderingDelegate;

	public static RayCastExcludingTwoEntitiesDelegate call_RayCastExcludingTwoEntitiesDelegate;

	public static RayCastForClosestEntityOrTerrainDelegate call_RayCastForClosestEntityOrTerrainDelegate;

	public static RayCastForClosestEntityOrTerrainIgnoreEntityDelegate call_RayCastForClosestEntityOrTerrainIgnoreEntityDelegate;

	public static RayCastForRammingDelegate call_RayCastForRammingDelegate;

	public static ReadDelegate call_ReadDelegate;

	public static ReadAndCalculateInitialCameraDelegate call_ReadAndCalculateInitialCameraDelegate;

	public static ReadInModuleDelegate call_ReadInModuleDelegate;

	public static RegisterShipVisualToWaterRendererDelegate call_RegisterShipVisualToWaterRendererDelegate;

	public static RemoveAlwaysRenderedSkeletonDelegate call_RemoveAlwaysRenderedSkeletonDelegate;

	public static RemoveDecalInstanceDelegate call_RemoveDecalInstanceDelegate;

	public static RemoveEntityDelegate call_RemoveEntityDelegate;

	public static ResumeLoadingRenderingsDelegate call_ResumeLoadingRenderingsDelegate;

	public static ResumeSceneSoundsDelegate call_ResumeSceneSoundsDelegate;

	public static SaveNavMeshPrefabWithFrameDelegate call_SaveNavMeshPrefabWithFrameDelegate;

	public static SceneHadWaterWakeRendererDelegate call_SceneHadWaterWakeRendererDelegate;

	public static SelectEntitiesCollidedWithDelegate call_SelectEntitiesCollidedWithDelegate;

	public static SelectEntitiesInBoxWithScriptComponentDelegate call_SelectEntitiesInBoxWithScriptComponentDelegate;

	public static SeparateFacesWithIdDelegate call_SeparateFacesWithIdDelegate;

	public static SetAberrationOffsetDelegate call_SetAberrationOffsetDelegate;

	public static SetAberrationSizeDelegate call_SetAberrationSizeDelegate;

	public static SetAberrationSmoothDelegate call_SetAberrationSmoothDelegate;

	public static SetAbilityOfFacesWithIdDelegate call_SetAbilityOfFacesWithIdDelegate;

	public static SetActiveVisibilityLevelsDelegate call_SetActiveVisibilityLevelsDelegate;

	public static SetAntialiasingModeDelegate call_SetAntialiasingModeDelegate;

	public static SetAtmosphereWithNameDelegate call_SetAtmosphereWithNameDelegate;

	public static SetBlockerDirectionForFacesWithIdDelegate call_SetBlockerDirectionForFacesWithIdDelegate;

	public static SetBloomDelegate call_SetBloomDelegate;

	public static SetBloomAmountDelegate call_SetBloomAmountDelegate;

	public static SetBloomStrengthDelegate call_SetBloomStrengthDelegate;

	public static SetBrightpassTresholdDelegate call_SetBrightpassTresholdDelegate;

	public static SetClothSimulationStateDelegate call_SetClothSimulationStateDelegate;

	public static SetColorGradeBlendDelegate call_SetColorGradeBlendDelegate;

	public static SetDLSSModeDelegate call_SetDLSSModeDelegate;

	public static SetDofFocusDelegate call_SetDofFocusDelegate;

	public static SetDofModeDelegate call_SetDofModeDelegate;

	public static SetDofParamsDelegate call_SetDofParamsDelegate;

	public static SetDoNotAddEntitiesToTickListDelegate call_SetDoNotAddEntitiesToTickListDelegate;

	public static SetDoNotWaitForLoadingStatesToRenderDelegate call_SetDoNotWaitForLoadingStatesToRenderDelegate;

	public static SetDontLoadInvisibleEntitiesDelegate call_SetDontLoadInvisibleEntitiesDelegate;

	public static SetDrynessFactorDelegate call_SetDrynessFactorDelegate;

	public static SetDynamicShadowmapCascadesRadiusMultiplierDelegate call_SetDynamicShadowmapCascadesRadiusMultiplierDelegate;

	public static SetDynamicSnowTextureDelegate call_SetDynamicSnowTextureDelegate;

	public static SetEnvironmentMultiplierDelegate call_SetEnvironmentMultiplierDelegate;

	public static SetExternalInjectionTextureDelegate call_SetExternalInjectionTextureDelegate;

	public static SetFetchCrcInfoOfSceneDelegate call_SetFetchCrcInfoOfSceneDelegate;

	public static SetFixedTickCallbackActiveDelegate call_SetFixedTickCallbackActiveDelegate;

	public static SetFogDelegate call_SetFogDelegate;

	public static SetFogAdvancedDelegate call_SetFogAdvancedDelegate;

	public static SetFogAmbientColorDelegate call_SetFogAmbientColorDelegate;

	public static SetForcedSnowDelegate call_SetForcedSnowDelegate;

	public static SetGlobalWindStrengthVectorDelegate call_SetGlobalWindStrengthVectorDelegate;

	public static SetGlobalWindVelocityDelegate call_SetGlobalWindVelocityDelegate;

	public static SetGrainAmountDelegate call_SetGrainAmountDelegate;

	public static SetHexagonVignetteAlphaDelegate call_SetHexagonVignetteAlphaDelegate;

	public static SetHexagonVignetteColorDelegate call_SetHexagonVignetteColorDelegate;

	public static SetHumidityDelegate call_SetHumidityDelegate;

	public static SetLandscapeRainMaskDataDelegate call_SetLandscapeRainMaskDataDelegate;

	public static SetLensDistortionDelegate call_SetLensDistortionDelegate;

	public static SetLensFlareAberrationOffsetDelegate call_SetLensFlareAberrationOffsetDelegate;

	public static SetLensFlareAmountDelegate call_SetLensFlareAmountDelegate;

	public static SetLensFlareBlurSigmaDelegate call_SetLensFlareBlurSigmaDelegate;

	public static SetLensFlareBlurSizeDelegate call_SetLensFlareBlurSizeDelegate;

	public static SetLensFlareDiffractionWeightDelegate call_SetLensFlareDiffractionWeightDelegate;

	public static SetLensFlareDirtWeightDelegate call_SetLensFlareDirtWeightDelegate;

	public static SetLensFlareGhostSamplesDelegate call_SetLensFlareGhostSamplesDelegate;

	public static SetLensFlareGhostWeightDelegate call_SetLensFlareGhostWeightDelegate;

	public static SetLensFlareHaloWeightDelegate call_SetLensFlareHaloWeightDelegate;

	public static SetLensFlareHaloWidthDelegate call_SetLensFlareHaloWidthDelegate;

	public static SetLensFlareStrengthDelegate call_SetLensFlareStrengthDelegate;

	public static SetLensFlareThresholdDelegate call_SetLensFlareThresholdDelegate;

	public static SetLightDiffuseColorDelegate call_SetLightDiffuseColorDelegate;

	public static SetLightDirectionDelegate call_SetLightDirectionDelegate;

	public static SetLightPositionDelegate call_SetLightPositionDelegate;

	public static SetMaxExposureDelegate call_SetMaxExposureDelegate;

	public static SetMiddleGrayDelegate call_SetMiddleGrayDelegate;

	public static SetMieScatterFocusDelegate call_SetMieScatterFocusDelegate;

	public static SetMieScatterStrengthDelegate call_SetMieScatterStrengthDelegate;

	public static SetMinExposureDelegate call_SetMinExposureDelegate;

	public static SetMotionBlurModeDelegate call_SetMotionBlurModeDelegate;

	public static SetNameDelegate call_SetNameDelegate;

	public static SetNavMeshRegionMapDelegate call_SetNavMeshRegionMapDelegate;

	public static SetOcclusionModeDelegate call_SetOcclusionModeDelegate;

	public static SetOnCollisionFilterCallbackActiveDelegate call_SetOnCollisionFilterCallbackActiveDelegate;

	public static SetOwnerThreadDelegate call_SetOwnerThreadDelegate;

	public static SetPhotoAtmosphereViaTodDelegate call_SetPhotoAtmosphereViaTodDelegate;

	public static SetPhotoModeFocusDelegate call_SetPhotoModeFocusDelegate;

	public static SetPhotoModeFovDelegate call_SetPhotoModeFovDelegate;

	public static SetPhotoModeOnDelegate call_SetPhotoModeOnDelegate;

	public static SetPhotoModeOrbitDelegate call_SetPhotoModeOrbitDelegate;

	public static SetPhotoModeRollDelegate call_SetPhotoModeRollDelegate;

	public static SetPhotoModeVignetteDelegate call_SetPhotoModeVignetteDelegate;

	public static SetPlaySoundEventsAfterReadyToRenderDelegate call_SetPlaySoundEventsAfterReadyToRenderDelegate;

	public static SetRainDensityDelegate call_SetRainDensityDelegate;

	public static SetSceneColorGradeDelegate call_SetSceneColorGradeDelegate;

	public static SetSceneColorGradeIndexDelegate call_SetSceneColorGradeIndexDelegate;

	public static SetSceneFilterIndexDelegate call_SetSceneFilterIndexDelegate;

	public static SetShadowDelegate call_SetShadowDelegate;

	public static SetSkyBrightnessDelegate call_SetSkyBrightnessDelegate;

	public static SetSkyRotationDelegate call_SetSkyRotationDelegate;

	public static SetSnowDensityDelegate call_SetSnowDensityDelegate;

	public static SetStreakAmountDelegate call_SetStreakAmountDelegate;

	public static SetStreakIntensityDelegate call_SetStreakIntensityDelegate;

	public static SetStreakStrengthDelegate call_SetStreakStrengthDelegate;

	public static SetStreakStretchDelegate call_SetStreakStretchDelegate;

	public static SetStreakThresholdDelegate call_SetStreakThresholdDelegate;

	public static SetStreakTintDelegate call_SetStreakTintDelegate;

	public static SetSunDelegate call_SetSunDelegate;

	public static SetSunAngleAltitudeDelegate call_SetSunAngleAltitudeDelegate;

	public static SetSunDirectionDelegate call_SetSunDirectionDelegate;

	public static SetSunLightDelegate call_SetSunLightDelegate;

	public static SetSunshaftModeDelegate call_SetSunshaftModeDelegate;

	public static SetSunShaftStrengthDelegate call_SetSunShaftStrengthDelegate;

	public static SetSunSizeDelegate call_SetSunSizeDelegate;

	public static SetTargetExposureDelegate call_SetTargetExposureDelegate;

	public static SetTemperatureDelegate call_SetTemperatureDelegate;

	public static SetTerrainDynamicParamsDelegate call_SetTerrainDynamicParamsDelegate;

	public static SetTimeOfDayDelegate call_SetTimeOfDayDelegate;

	public static SetTimeSpeedDelegate call_SetTimeSpeedDelegate;

	public static SetUpgradeLevelDelegate call_SetUpgradeLevelDelegate;

	public static SetUpgradeLevelVisibilityDelegate call_SetUpgradeLevelVisibilityDelegate;

	public static SetUpgradeLevelVisibilityWithMaskDelegate call_SetUpgradeLevelVisibilityWithMaskDelegate;

	public static SetUseAdvancedWaterRenderingDelegate call_SetUseAdvancedWaterRenderingDelegate;

	public static SetUseConstantTimeDelegate call_SetUseConstantTimeDelegate;

	public static SetUsesDeleteLaterSystemDelegate call_SetUsesDeleteLaterSystemDelegate;

	public static SetVignetteInnerRadiusDelegate call_SetVignetteInnerRadiusDelegate;

	public static SetVignetteOpacityDelegate call_SetVignetteOpacityDelegate;

	public static SetVignetteOuterRadiusDelegate call_SetVignetteOuterRadiusDelegate;

	public static SetWaterStrengthDelegate call_SetWaterStrengthDelegate;

	public static SetWaterWakeCameraOffsetDelegate call_SetWaterWakeCameraOffsetDelegate;

	public static SetWaterWakeWorldSizeDelegate call_SetWaterWakeWorldSizeDelegate;

	public static SetWinterTimeFactorDelegate call_SetWinterTimeFactorDelegate;

	public static StallLoadingRenderingsUntilFurtherNoticeDelegate call_StallLoadingRenderingsUntilFurtherNoticeDelegate;

	public static SwapFaceConnectionsWithIdDelegate call_SwapFaceConnectionsWithIdDelegate;

	public static TakePhotoModePictureDelegate call_TakePhotoModePictureDelegate;

	public static TickDelegate call_TickDelegate;

	public static TickWakeDelegate call_TickWakeDelegate;

	public static WaitWaterRendererCPUSimulationDelegate call_WaitWaterRendererCPUSimulationDelegate;

	public static WorldPositionComputeNearestNavMeshDelegate call_WorldPositionComputeNearestNavMeshDelegate;

	public static WorldPositionValidateZDelegate call_WorldPositionValidateZDelegate;

	public void AddAlwaysRenderedSkeleton(UIntPtr scenePointer, UIntPtr skeletonPointer)
	{
		call_AddAlwaysRenderedSkeletonDelegate(scenePointer, skeletonPointer);
	}

	public void AddDecalInstance(UIntPtr scenePointer, UIntPtr decalMeshPointer, string decalSetID, bool deletable)
	{
		byte[] array = null;
		if (decalSetID != null)
		{
			int byteCount = _utf8.GetByteCount(decalSetID);
			array = ((byteCount < 1024) ? CallbackStringBufferManager.StringBuffer0 : new byte[byteCount + 1]);
			_utf8.GetBytes(decalSetID, 0, decalSetID.Length, array, 0);
			array[byteCount] = 0;
		}
		call_AddDecalInstanceDelegate(scenePointer, decalMeshPointer, array, deletable);
	}

	public int AddDirectionalLight(UIntPtr scenePointer, Vec3 position, Vec3 direction, float radius)
	{
		return call_AddDirectionalLightDelegate(scenePointer, position, direction, radius);
	}

	public void AddEntityWithMesh(UIntPtr scenePointer, UIntPtr meshPointer, ref MatrixFrame frame)
	{
		call_AddEntityWithMeshDelegate(scenePointer, meshPointer, ref frame);
	}

	public void AddEntityWithMultiMesh(UIntPtr scenePointer, UIntPtr multiMeshPointer, ref MatrixFrame frame)
	{
		call_AddEntityWithMultiMeshDelegate(scenePointer, multiMeshPointer, ref frame);
	}

	public GameEntity AddItemEntity(UIntPtr scenePointer, ref MatrixFrame frame, UIntPtr meshPointer)
	{
		NativeObjectPointer nativeObjectPointer = call_AddItemEntityDelegate(scenePointer, ref frame, meshPointer);
		GameEntity result = null;
		if (nativeObjectPointer.Pointer != UIntPtr.Zero)
		{
			result = new GameEntity(nativeObjectPointer.Pointer);
			LibraryApplicationInterface.IManaged.DecreaseReferenceCount(nativeObjectPointer.Pointer);
		}
		return result;
	}

	public void AddPath(UIntPtr scenePointer, string name)
	{
		byte[] array = null;
		if (name != null)
		{
			int byteCount = _utf8.GetByteCount(name);
			array = ((byteCount < 1024) ? CallbackStringBufferManager.StringBuffer0 : new byte[byteCount + 1]);
			_utf8.GetBytes(name, 0, name.Length, array, 0);
			array[byteCount] = 0;
		}
		call_AddPathDelegate(scenePointer, array);
	}

	public void AddPathPoint(UIntPtr scenePointer, string name, ref MatrixFrame frame)
	{
		byte[] array = null;
		if (name != null)
		{
			int byteCount = _utf8.GetByteCount(name);
			array = ((byteCount < 1024) ? CallbackStringBufferManager.StringBuffer0 : new byte[byteCount + 1]);
			_utf8.GetBytes(name, 0, name.Length, array, 0);
			array[byteCount] = 0;
		}
		call_AddPathPointDelegate(scenePointer, array, ref frame);
	}

	public int AddPointLight(UIntPtr scenePointer, Vec3 position, float radius)
	{
		return call_AddPointLightDelegate(scenePointer, position, radius);
	}

	public void AddWaterWakeWithCapsule(Scene scene, Vec3 positionA, float radiusA, Vec3 positionB, float radiusB, float wakeVisibility, float foamVisibility)
	{
		UIntPtr scene2 = ((scene != null) ? scene.Pointer : UIntPtr.Zero);
		call_AddWaterWakeWithCapsuleDelegate(scene2, positionA, radiusA, positionB, radiusB, wakeVisibility, foamVisibility);
	}

	public bool AttachEntity(UIntPtr scenePointer, UIntPtr entity, bool showWarnings)
	{
		return call_AttachEntityDelegate(scenePointer, entity, showWarnings);
	}

	public bool BoxCast(UIntPtr scenePointer, ref Vec3 boxPointBegin, ref Vec3 boxPointEnd, ref Vec3 dir, float distance, ref float collisionDistance, ref Vec3 closestPoint, ref UIntPtr entityIndex, BodyFlags bodyExcludeFlags)
	{
		return call_BoxCastDelegate(scenePointer, ref boxPointBegin, ref boxPointEnd, ref dir, distance, ref collisionDistance, ref closestPoint, ref entityIndex, bodyExcludeFlags);
	}

	public bool BoxCastOnlyForCamera(UIntPtr scenePointer, Vec3[] boxPoints, in Vec3 centerPoint, in Vec3 dir, float distance, UIntPtr ignoredEntityPointer, ref float collisionDistance, ref Vec3 closestPoint, ref UIntPtr entityPointer, BodyFlags bodyExcludeFlags)
	{
		PinnedArrayData<Vec3> pinnedArrayData = new PinnedArrayData<Vec3>(boxPoints);
		IntPtr pointer = pinnedArrayData.Pointer;
		bool result = call_BoxCastOnlyForCameraDelegate(scenePointer, pointer, in centerPoint, in dir, distance, ignoredEntityPointer, ref collisionDistance, ref closestPoint, ref entityPointer, bodyExcludeFlags);
		pinnedArrayData.Dispose();
		return result;
	}

	public bool CalculateEffectiveLighting(UIntPtr scenePointer)
	{
		return call_CalculateEffectiveLightingDelegate(scenePointer);
	}

	public bool CheckPathEntitiesFrameChanged(UIntPtr scenePointer, string containsName)
	{
		byte[] array = null;
		if (containsName != null)
		{
			int byteCount = _utf8.GetByteCount(containsName);
			array = ((byteCount < 1024) ? CallbackStringBufferManager.StringBuffer0 : new byte[byteCount + 1]);
			_utf8.GetBytes(containsName, 0, containsName.Length, array, 0);
			array[byteCount] = 0;
		}
		return call_CheckPathEntitiesFrameChangedDelegate(scenePointer, array);
	}

	public bool CheckPointCanSeePoint(UIntPtr scenePointer, Vec3 sourcePoint, Vec3 targetPoint, float distanceToCheck)
	{
		return call_CheckPointCanSeePointDelegate(scenePointer, sourcePoint, targetPoint, distanceToCheck);
	}

	public void CheckResources(UIntPtr scenePointer, bool checkInvisibleEntities)
	{
		call_CheckResourcesDelegate(scenePointer, checkInvisibleEntities);
	}

	public void ClearAll(UIntPtr scenePointer)
	{
		call_ClearAllDelegate(scenePointer);
	}

	public void ClearCurrentFrameTickEntities(UIntPtr scenePointer)
	{
		call_ClearCurrentFrameTickEntitiesDelegate(scenePointer);
	}

	public void ClearDecals(UIntPtr scenePointer)
	{
		call_ClearDecalsDelegate(scenePointer);
	}

	public void ClearNavMesh(UIntPtr scenePointer)
	{
		call_ClearNavMeshDelegate(scenePointer);
	}

	public bool ContainsTerrain(UIntPtr scenePointer)
	{
		return call_ContainsTerrainDelegate(scenePointer);
	}

	public void CreateBurstParticle(Scene scene, int particleId, ref MatrixFrame frame)
	{
		UIntPtr scene2 = ((scene != null) ? scene.Pointer : UIntPtr.Zero);
		call_CreateBurstParticleDelegate(scene2, particleId, ref frame);
	}

	public void CreateDynamicRainTexture(UIntPtr scenePointer, int w, int h)
	{
		call_CreateDynamicRainTextureDelegate(scenePointer, w, h);
	}

	public Scene CreateNewScene(bool initializePhysics, bool enableDecals, int atlasGroup, string sceneName)
	{
		byte[] array = null;
		if (sceneName != null)
		{
			int byteCount = _utf8.GetByteCount(sceneName);
			array = ((byteCount < 1024) ? CallbackStringBufferManager.StringBuffer0 : new byte[byteCount + 1]);
			_utf8.GetBytes(sceneName, 0, sceneName.Length, array, 0);
			array[byteCount] = 0;
		}
		NativeObjectPointer nativeObjectPointer = call_CreateNewSceneDelegate(initializePhysics, enableDecals, atlasGroup, array);
		Scene result = null;
		if (nativeObjectPointer.Pointer != UIntPtr.Zero)
		{
			result = new Scene(nativeObjectPointer.Pointer);
			LibraryApplicationInterface.IManaged.DecreaseReferenceCount(nativeObjectPointer.Pointer);
		}
		return result;
	}

	public MetaMesh CreatePathMesh(UIntPtr scenePointer, string baseEntityName, bool isWaterPath)
	{
		byte[] array = null;
		if (baseEntityName != null)
		{
			int byteCount = _utf8.GetByteCount(baseEntityName);
			array = ((byteCount < 1024) ? CallbackStringBufferManager.StringBuffer0 : new byte[byteCount + 1]);
			_utf8.GetBytes(baseEntityName, 0, baseEntityName.Length, array, 0);
			array[byteCount] = 0;
		}
		NativeObjectPointer nativeObjectPointer = call_CreatePathMeshDelegate(scenePointer, array, isWaterPath);
		MetaMesh result = null;
		if (nativeObjectPointer.Pointer != UIntPtr.Zero)
		{
			result = new MetaMesh(nativeObjectPointer.Pointer);
			LibraryApplicationInterface.IManaged.DecreaseReferenceCount(nativeObjectPointer.Pointer);
		}
		return result;
	}

	public MetaMesh CreatePathMesh2(UIntPtr scenePointer, UIntPtr[] pathNodes, int pathNodeCount, bool isWaterPath)
	{
		PinnedArrayData<UIntPtr> pinnedArrayData = new PinnedArrayData<UIntPtr>(pathNodes);
		IntPtr pointer = pinnedArrayData.Pointer;
		NativeObjectPointer nativeObjectPointer = call_CreatePathMesh2Delegate(scenePointer, pointer, pathNodeCount, isWaterPath);
		pinnedArrayData.Dispose();
		MetaMesh result = null;
		if (nativeObjectPointer.Pointer != UIntPtr.Zero)
		{
			result = new MetaMesh(nativeObjectPointer.Pointer);
			LibraryApplicationInterface.IManaged.DecreaseReferenceCount(nativeObjectPointer.Pointer);
		}
		return result;
	}

	public void DeletePathWithName(UIntPtr scenePointer, string name)
	{
		byte[] array = null;
		if (name != null)
		{
			int byteCount = _utf8.GetByteCount(name);
			array = ((byteCount < 1024) ? CallbackStringBufferManager.StringBuffer0 : new byte[byteCount + 1]);
			_utf8.GetBytes(name, 0, name.Length, array, 0);
			array[byteCount] = 0;
		}
		call_DeletePathWithNameDelegate(scenePointer, array);
	}

	public void DeleteWaterWakeRenderer(UIntPtr scenePointer)
	{
		call_DeleteWaterWakeRendererDelegate(scenePointer);
	}

	public void DeRegisterShipVisual(UIntPtr scenePointer, UIntPtr visualPointer)
	{
		call_DeRegisterShipVisualDelegate(scenePointer, visualPointer);
	}

	public void DisableStaticShadows(UIntPtr ptr, bool value)
	{
		call_DisableStaticShadowsDelegate(ptr, value);
	}

	public bool DoesPathExistBetweenFaces(UIntPtr scenePointer, int firstNavMeshFace, int secondNavMeshFace, bool ignoreDisabled)
	{
		return call_DoesPathExistBetweenFacesDelegate(scenePointer, firstNavMeshFace, secondNavMeshFace, ignoreDisabled);
	}

	public bool DoesPathExistBetweenPositions(UIntPtr scenePointer, WorldPosition position, WorldPosition destination)
	{
		return call_DoesPathExistBetweenPositionsDelegate(scenePointer, position, destination);
	}

	public void EnableFixedTick(Scene scene)
	{
		UIntPtr scene2 = ((scene != null) ? scene.Pointer : UIntPtr.Zero);
		call_EnableFixedTickDelegate(scene2);
	}

	public void EnableInclusiveAsyncPhysx(UIntPtr scenePointer)
	{
		call_EnableInclusiveAsyncPhysxDelegate(scenePointer);
	}

	public void EnsurePostfxSystem(UIntPtr scenePointer)
	{
		call_EnsurePostfxSystemDelegate(scenePointer);
	}

	public void EnsureWaterWakeRenderer(UIntPtr scenePointer)
	{
		call_EnsureWaterWakeRendererDelegate(scenePointer);
	}

	public void FillEntityWithHardBorderPhysicsBarrier(UIntPtr scenePointer, UIntPtr entityPointer)
	{
		call_FillEntityWithHardBorderPhysicsBarrierDelegate(scenePointer, entityPointer);
	}

	public void FillTerrainHeightData(Scene scene, int xIndex, int yIndex, float[] heightArray)
	{
		UIntPtr scene2 = ((scene != null) ? scene.Pointer : UIntPtr.Zero);
		PinnedArrayData<float> pinnedArrayData = new PinnedArrayData<float>(heightArray);
		IntPtr pointer = pinnedArrayData.Pointer;
		call_FillTerrainHeightDataDelegate(scene2, xIndex, yIndex, pointer);
		pinnedArrayData.Dispose();
	}

	public void FillTerrainPhysicsMaterialIndexData(Scene scene, int xIndex, int yIndex, short[] materialIndexArray)
	{
		UIntPtr scene2 = ((scene != null) ? scene.Pointer : UIntPtr.Zero);
		PinnedArrayData<short> pinnedArrayData = new PinnedArrayData<short>(materialIndexArray);
		IntPtr pointer = pinnedArrayData.Pointer;
		call_FillTerrainPhysicsMaterialIndexDataDelegate(scene2, xIndex, yIndex, pointer);
		pinnedArrayData.Dispose();
	}

	public Vec2 FindClosestExitPositionForPositionOnABoundaryFace(UIntPtr scenePointer, Vec3 position, UIntPtr boundaryNavMeshFacePointer)
	{
		return call_FindClosestExitPositionForPositionOnABoundaryFaceDelegate(scenePointer, position, boundaryNavMeshFacePointer);
	}

	public void FinishSceneSounds(UIntPtr scenePointer)
	{
		call_FinishSceneSoundsDelegate(scenePointer);
	}

	public bool FocusRayCastForFixedPhysics(UIntPtr scenePointer, in Vec3 sourcePoint, in Vec3 targetPoint, float rayThickness, ref float collisionDistance, ref Vec3 closestPoint, ref UIntPtr entityIndex, BodyFlags bodyExcludeFlags, bool isFixedWorld)
	{
		return call_FocusRayCastForFixedPhysicsDelegate(scenePointer, in sourcePoint, in targetPoint, rayThickness, ref collisionDistance, ref closestPoint, ref entityIndex, bodyExcludeFlags, isFixedWorld);
	}

	public void ForceLoadResources(UIntPtr scenePointer, bool checkInvisibleEntities)
	{
		call_ForceLoadResourcesDelegate(scenePointer, checkInvisibleEntities);
	}

	public int GenerateContactsWithCapsule(UIntPtr scenePointer, ref CapsuleData cap, BodyFlags excludeFlags, bool isFixedTick, Intersection[] intersections, UIntPtr[] entityIds)
	{
		PinnedArrayData<Intersection> pinnedArrayData = new PinnedArrayData<Intersection>(intersections);
		IntPtr pointer = pinnedArrayData.Pointer;
		PinnedArrayData<UIntPtr> pinnedArrayData2 = new PinnedArrayData<UIntPtr>(entityIds);
		IntPtr pointer2 = pinnedArrayData2.Pointer;
		int result = call_GenerateContactsWithCapsuleDelegate(scenePointer, ref cap, excludeFlags, isFixedTick, pointer, pointer2);
		pinnedArrayData.Dispose();
		pinnedArrayData2.Dispose();
		return result;
	}

	public int GenerateContactsWithCapsuleAgainstEntity(UIntPtr scenePointer, ref CapsuleData cap, BodyFlags excludeFlags, UIntPtr entityId, Intersection[] intersections)
	{
		PinnedArrayData<Intersection> pinnedArrayData = new PinnedArrayData<Intersection>(intersections);
		IntPtr pointer = pinnedArrayData.Pointer;
		int result = call_GenerateContactsWithCapsuleAgainstEntityDelegate(scenePointer, ref cap, excludeFlags, entityId, pointer);
		pinnedArrayData.Dispose();
		return result;
	}

	public string GetAllColorGradeNames(Scene scene)
	{
		UIntPtr scene2 = ((scene != null) ? scene.Pointer : UIntPtr.Zero);
		if (call_GetAllColorGradeNamesDelegate(scene2) != 1)
		{
			return null;
		}
		return Managed.ReturnValueFromEngine;
	}

	public void GetAllEntitiesWithScriptComponent(UIntPtr scenePointer, string scriptComponentName, UIntPtr output)
	{
		byte[] array = null;
		if (scriptComponentName != null)
		{
			int byteCount = _utf8.GetByteCount(scriptComponentName);
			array = ((byteCount < 1024) ? CallbackStringBufferManager.StringBuffer0 : new byte[byteCount + 1]);
			_utf8.GetBytes(scriptComponentName, 0, scriptComponentName.Length, array, 0);
			array[byteCount] = 0;
		}
		call_GetAllEntitiesWithScriptComponentDelegate(scenePointer, array, output);
	}

	public string GetAllFilterNames(Scene scene)
	{
		UIntPtr scene2 = ((scene != null) ? scene.Pointer : UIntPtr.Zero);
		if (call_GetAllFilterNamesDelegate(scene2) != 1)
		{
			return null;
		}
		return Managed.ReturnValueFromEngine;
	}

	public void GetAllNavmeshFaceRecords(UIntPtr scenePointer, PathFaceRecord[] faceRecords)
	{
		PinnedArrayData<PathFaceRecord> pinnedArrayData = new PinnedArrayData<PathFaceRecord>(faceRecords);
		IntPtr pointer = pinnedArrayData.Pointer;
		call_GetAllNavmeshFaceRecordsDelegate(scenePointer, pointer);
		pinnedArrayData.Dispose();
	}

	public void GetBoundingBox(UIntPtr scenePointer, ref Vec3 min, ref Vec3 max)
	{
		call_GetBoundingBoxDelegate(scenePointer, ref min, ref max);
	}

	public void GetBulkWaterLevelAtPositions(Scene scene, Vec2[] waterHeightQueryArray, int arraySize, float[] waterHeightsAtVolumes, Vec3[] waterSurfaceNormals)
	{
		UIntPtr scene2 = ((scene != null) ? scene.Pointer : UIntPtr.Zero);
		PinnedArrayData<Vec2> pinnedArrayData = new PinnedArrayData<Vec2>(waterHeightQueryArray);
		IntPtr pointer = pinnedArrayData.Pointer;
		PinnedArrayData<float> pinnedArrayData2 = new PinnedArrayData<float>(waterHeightsAtVolumes);
		IntPtr pointer2 = pinnedArrayData2.Pointer;
		PinnedArrayData<Vec3> pinnedArrayData3 = new PinnedArrayData<Vec3>(waterSurfaceNormals);
		IntPtr pointer3 = pinnedArrayData3.Pointer;
		call_GetBulkWaterLevelAtPositionsDelegate(scene2, pointer, arraySize, pointer2, pointer3);
		pinnedArrayData.Dispose();
		pinnedArrayData2.Dispose();
		pinnedArrayData3.Dispose();
	}

	public void GetBulkWaterLevelAtVolumes(UIntPtr scene, UIntPtr volumeQueryDataArray, int volumeCount, in MatrixFrame entityFrame)
	{
		call_GetBulkWaterLevelAtVolumesDelegate(scene, volumeQueryDataArray, volumeCount, in entityFrame);
	}

	public GameEntity GetCampaignEntityWithName(UIntPtr scenePointer, string entityName)
	{
		byte[] array = null;
		if (entityName != null)
		{
			int byteCount = _utf8.GetByteCount(entityName);
			array = ((byteCount < 1024) ? CallbackStringBufferManager.StringBuffer0 : new byte[byteCount + 1]);
			_utf8.GetBytes(entityName, 0, entityName.Length, array, 0);
			array[byteCount] = 0;
		}
		NativeObjectPointer nativeObjectPointer = call_GetCampaignEntityWithNameDelegate(scenePointer, array);
		GameEntity result = null;
		if (nativeObjectPointer.Pointer != UIntPtr.Zero)
		{
			result = new GameEntity(nativeObjectPointer.Pointer);
			LibraryApplicationInterface.IManaged.DecreaseReferenceCount(nativeObjectPointer.Pointer);
		}
		return result;
	}

	public bool GetEnginePhysicsEnabled(UIntPtr scenePointer)
	{
		return call_GetEnginePhysicsEnabledDelegate(scenePointer);
	}

	public void GetEntities(UIntPtr scenePointer, UIntPtr entityObjectsArrayPointer)
	{
		call_GetEntitiesDelegate(scenePointer, entityObjectsArrayPointer);
	}

	public int GetEntityCount(UIntPtr scenePointer)
	{
		return call_GetEntityCountDelegate(scenePointer);
	}

	public GameEntity GetEntityWithGuid(UIntPtr scenePointer, string guid)
	{
		byte[] array = null;
		if (guid != null)
		{
			int byteCount = _utf8.GetByteCount(guid);
			array = ((byteCount < 1024) ? CallbackStringBufferManager.StringBuffer0 : new byte[byteCount + 1]);
			_utf8.GetBytes(guid, 0, guid.Length, array, 0);
			array[byteCount] = 0;
		}
		NativeObjectPointer nativeObjectPointer = call_GetEntityWithGuidDelegate(scenePointer, array);
		GameEntity result = null;
		if (nativeObjectPointer.Pointer != UIntPtr.Zero)
		{
			result = new GameEntity(nativeObjectPointer.Pointer);
			LibraryApplicationInterface.IManaged.DecreaseReferenceCount(nativeObjectPointer.Pointer);
		}
		return result;
	}

	public float GetFallDensity(UIntPtr scenepTR)
	{
		return call_GetFallDensityDelegate(scenepTR);
	}

	public GameEntity GetFirstEntityWithName(UIntPtr scenePointer, string entityName)
	{
		byte[] array = null;
		if (entityName != null)
		{
			int byteCount = _utf8.GetByteCount(entityName);
			array = ((byteCount < 1024) ? CallbackStringBufferManager.StringBuffer0 : new byte[byteCount + 1]);
			_utf8.GetBytes(entityName, 0, entityName.Length, array, 0);
			array[byteCount] = 0;
		}
		NativeObjectPointer nativeObjectPointer = call_GetFirstEntityWithNameDelegate(scenePointer, array);
		GameEntity result = null;
		if (nativeObjectPointer.Pointer != UIntPtr.Zero)
		{
			result = new GameEntity(nativeObjectPointer.Pointer);
			LibraryApplicationInterface.IManaged.DecreaseReferenceCount(nativeObjectPointer.Pointer);
		}
		return result;
	}

	public GameEntity GetFirstEntityWithScriptComponent(UIntPtr scenePointer, string scriptComponentName)
	{
		byte[] array = null;
		if (scriptComponentName != null)
		{
			int byteCount = _utf8.GetByteCount(scriptComponentName);
			array = ((byteCount < 1024) ? CallbackStringBufferManager.StringBuffer0 : new byte[byteCount + 1]);
			_utf8.GetBytes(scriptComponentName, 0, scriptComponentName.Length, array, 0);
			array[byteCount] = 0;
		}
		NativeObjectPointer nativeObjectPointer = call_GetFirstEntityWithScriptComponentDelegate(scenePointer, array);
		GameEntity result = null;
		if (nativeObjectPointer.Pointer != UIntPtr.Zero)
		{
			result = new GameEntity(nativeObjectPointer.Pointer);
			LibraryApplicationInterface.IManaged.DecreaseReferenceCount(nativeObjectPointer.Pointer);
		}
		return result;
	}

	public int GetFloraInstanceCount(UIntPtr scenePointer)
	{
		return call_GetFloraInstanceCountDelegate(scenePointer);
	}

	public int GetFloraRendererTextureUsage(UIntPtr scenePointer)
	{
		return call_GetFloraRendererTextureUsageDelegate(scenePointer);
	}

	public float GetFog(UIntPtr scenePointer)
	{
		return call_GetFogDelegate(scenePointer);
	}

	public Vec2 GetGlobalWindStrengthVector(UIntPtr scenePointer)
	{
		return call_GetGlobalWindStrengthVectorDelegate(scenePointer);
	}

	public Vec2 GetGlobalWindVelocity(UIntPtr scenePointer)
	{
		return call_GetGlobalWindVelocityDelegate(scenePointer);
	}

	public float GetGroundHeightAndBodyFlagsAtPosition(UIntPtr scenePointer, Vec3 position, out BodyFlags contactPointFlags, BodyFlags excludeFlags)
	{
		return call_GetGroundHeightAndBodyFlagsAtPositionDelegate(scenePointer, position, out contactPointFlags, excludeFlags);
	}

	public float GetGroundHeightAndNormalAtPosition(UIntPtr scenePointer, Vec3 position, ref Vec3 normal, uint excludeFlags)
	{
		return call_GetGroundHeightAndNormalAtPositionDelegate(scenePointer, position, ref normal, excludeFlags);
	}

	public float GetGroundHeightAtPosition(UIntPtr scenePointer, Vec3 position, uint excludeFlags)
	{
		return call_GetGroundHeightAtPositionDelegate(scenePointer, position, excludeFlags);
	}

	public Vec2 GetHardBoundaryVertex(UIntPtr scenePointer, int index)
	{
		return call_GetHardBoundaryVertexDelegate(scenePointer, index);
	}

	public int GetHardBoundaryVertexCount(UIntPtr scenePointer)
	{
		return call_GetHardBoundaryVertexCountDelegate(scenePointer);
	}

	public bool GetHeightAtPoint(UIntPtr scenePointer, Vec2 point, BodyFlags excludeBodyFlags, ref float height)
	{
		return call_GetHeightAtPointDelegate(scenePointer, point, excludeBodyFlags, ref height);
	}

	public int GetIdOfNavMeshFace(UIntPtr scenePointer, int navMeshFace)
	{
		return call_GetIdOfNavMeshFaceDelegate(scenePointer, navMeshFace);
	}

	public void GetInterpolationFactorForBodyWorldTransformSmoothing(Scene scene, out float interpolationFactor, out float fixedDt)
	{
		UIntPtr scene2 = ((scene != null) ? scene.Pointer : UIntPtr.Zero);
		call_GetInterpolationFactorForBodyWorldTransformSmoothingDelegate(scene2, out interpolationFactor, out fixedDt);
	}

	public void GetLastFinalRenderCameraFrame(UIntPtr scenePointer, ref MatrixFrame outFrame)
	{
		call_GetLastFinalRenderCameraFrameDelegate(scenePointer, ref outFrame);
	}

	public Vec3 GetLastFinalRenderCameraPosition(UIntPtr scenePointer)
	{
		return call_GetLastFinalRenderCameraPositionDelegate(scenePointer);
	}

	public Vec2 GetLastPointOnNavigationMeshFromPositionToDestination(UIntPtr scenePointer, int startingFace, Vec2 position, Vec2 destination, int[] exclusionGroupIds, int exclusionGroupIdsCount)
	{
		PinnedArrayData<int> pinnedArrayData = new PinnedArrayData<int>(exclusionGroupIds);
		IntPtr pointer = pinnedArrayData.Pointer;
		Vec2 result = call_GetLastPointOnNavigationMeshFromPositionToDestinationDelegate(scenePointer, startingFace, position, destination, pointer, exclusionGroupIdsCount);
		pinnedArrayData.Dispose();
		return result;
	}

	public Vec3 GetLastPointOnNavigationMeshFromWorldPositionToDestination(UIntPtr scenePointer, ref WorldPosition position, Vec2 destination)
	{
		return call_GetLastPointOnNavigationMeshFromWorldPositionToDestinationDelegate(scenePointer, ref position, destination);
	}

	public Vec2 GetLastPositionOnNavMeshFaceForPointAndDirection(UIntPtr scenePointer, in PathFaceRecord record, Vec2 position, Vec2 direction)
	{
		return call_GetLastPositionOnNavMeshFaceForPointAndDirectionDelegate(scenePointer, in record, position, direction);
	}

	public string GetLoadingStateName(Scene scene)
	{
		UIntPtr scene2 = ((scene != null) ? scene.Pointer : UIntPtr.Zero);
		if (call_GetLoadingStateNameDelegate(scene2) != 1)
		{
			return null;
		}
		return Managed.ReturnValueFromEngine;
	}

	public string GetModulePath(UIntPtr scenePointer)
	{
		if (call_GetModulePathDelegate(scenePointer) != 1)
		{
			return null;
		}
		return Managed.ReturnValueFromEngine;
	}

	public string GetName(UIntPtr scenePointer)
	{
		if (call_GetNameDelegate(scenePointer) != 1)
		{
			return null;
		}
		return Managed.ReturnValueFromEngine;
	}

	public uint GetNavigationMeshCRC(UIntPtr scenePointer)
	{
		return call_GetNavigationMeshCRCDelegate(scenePointer);
	}

	public UIntPtr GetNavigationMeshForPosition(UIntPtr scenePointer, in Vec3 position, ref int faceGroupId, float heightDifferenceLimit, bool excludeDynamicNavigationMeshes)
	{
		return call_GetNavigationMeshForPositionDelegate(scenePointer, in position, ref faceGroupId, heightDifferenceLimit, excludeDynamicNavigationMeshes);
	}

	public void GetNavMeshFaceCenterPosition(UIntPtr scenePointer, int navMeshFace, ref Vec3 centerPos)
	{
		call_GetNavMeshFaceCenterPositionDelegate(scenePointer, navMeshFace, ref centerPos);
	}

	public int GetNavMeshFaceCount(UIntPtr scenePointer)
	{
		return call_GetNavMeshFaceCountDelegate(scenePointer);
	}

	public int GetNavmeshFaceCountBetweenTwoIds(UIntPtr scenePointer, int firstId, int secondId)
	{
		return call_GetNavmeshFaceCountBetweenTwoIdsDelegate(scenePointer, firstId, secondId);
	}

	public float GetNavMeshFaceFirstVertexZ(UIntPtr scenePointer, int navMeshFaceIndex)
	{
		return call_GetNavMeshFaceFirstVertexZDelegate(scenePointer, navMeshFaceIndex);
	}

	public void GetNavMeshFaceIndex(UIntPtr scenePointer, ref PathFaceRecord record, Vec2 position, bool isRegion1, bool checkIfDisabled, bool ignoreHeight)
	{
		call_GetNavMeshFaceIndexDelegate(scenePointer, ref record, position, isRegion1, checkIfDisabled, ignoreHeight);
	}

	public void GetNavMeshFaceIndex3(UIntPtr scenePointer, ref PathFaceRecord record, Vec3 position, bool checkIfDisabled)
	{
		call_GetNavMeshFaceIndex3Delegate(scenePointer, ref record, position, checkIfDisabled);
	}

	public void GetNavmeshFaceRecordsBetweenTwoIds(UIntPtr scenePointer, int firstId, int secondId, PathFaceRecord[] faceRecords)
	{
		PinnedArrayData<PathFaceRecord> pinnedArrayData = new PinnedArrayData<PathFaceRecord>(faceRecords);
		IntPtr pointer = pinnedArrayData.Pointer;
		call_GetNavmeshFaceRecordsBetweenTwoIdsDelegate(scenePointer, firstId, secondId, pointer);
		pinnedArrayData.Dispose();
	}

	public PathFaceRecord GetNavMeshPathFaceRecord(UIntPtr scenePointer, int navMeshFace)
	{
		return call_GetNavMeshPathFaceRecordDelegate(scenePointer, navMeshFace);
	}

	public UIntPtr GetNearestNavigationMeshForPosition(UIntPtr scenePointer, in Vec3 position, float heightDifferenceLimit, bool excludeDynamicNavigationMeshes)
	{
		return call_GetNearestNavigationMeshForPositionDelegate(scenePointer, in position, heightDifferenceLimit, excludeDynamicNavigationMeshes);
	}

	public int GetNodeDataCount(Scene scene, int xIndex, int yIndex)
	{
		UIntPtr scene2 = ((scene != null) ? scene.Pointer : UIntPtr.Zero);
		return call_GetNodeDataCountDelegate(scene2, xIndex, yIndex);
	}

	public Vec3 GetNormalAt(UIntPtr scenePointer, Vec2 position)
	{
		return call_GetNormalAtDelegate(scenePointer, position);
	}

	public float GetNorthAngle(UIntPtr scenePointer)
	{
		return call_GetNorthAngleDelegate(scenePointer);
	}

	public int GetNumberOfPathsWithNamePrefix(UIntPtr ptr, string prefix)
	{
		byte[] array = null;
		if (prefix != null)
		{
			int byteCount = _utf8.GetByteCount(prefix);
			array = ((byteCount < 1024) ? CallbackStringBufferManager.StringBuffer0 : new byte[byteCount + 1]);
			_utf8.GetBytes(prefix, 0, prefix.Length, array, 0);
			array[byteCount] = 0;
		}
		return call_GetNumberOfPathsWithNamePrefixDelegate(ptr, array);
	}

	public bool GetPathBetweenAIFaceIndices(UIntPtr scenePointer, int startingAiFace, int endingAiFace, Vec2 startingPosition, Vec2 endingPosition, float agentRadius, Vec2[] result, ref int pathSize, int[] exclusionGroupIds, int exclusionGroupIdsCount, float extraCostMultiplier)
	{
		PinnedArrayData<Vec2> pinnedArrayData = new PinnedArrayData<Vec2>(result);
		IntPtr pointer = pinnedArrayData.Pointer;
		PinnedArrayData<int> pinnedArrayData2 = new PinnedArrayData<int>(exclusionGroupIds);
		IntPtr pointer2 = pinnedArrayData2.Pointer;
		bool result2 = call_GetPathBetweenAIFaceIndicesDelegate(scenePointer, startingAiFace, endingAiFace, startingPosition, endingPosition, agentRadius, pointer, ref pathSize, pointer2, exclusionGroupIdsCount, extraCostMultiplier);
		pinnedArrayData.Dispose();
		pinnedArrayData2.Dispose();
		return result2;
	}

	public bool GetPathBetweenAIFaceIndicesWithRegionSwitchCost(UIntPtr scenePointer, int startingAiFace, int endingAiFace, Vec2 startingPosition, Vec2 endingPosition, float agentRadius, Vec2[] result, ref int pathSize, int[] exclusionGroupIds, int exclusionGroupIdsCount, float extraCostMultiplier, int regionSwitchCostTo0, int regionSwitchCostTo1)
	{
		PinnedArrayData<Vec2> pinnedArrayData = new PinnedArrayData<Vec2>(result);
		IntPtr pointer = pinnedArrayData.Pointer;
		PinnedArrayData<int> pinnedArrayData2 = new PinnedArrayData<int>(exclusionGroupIds);
		IntPtr pointer2 = pinnedArrayData2.Pointer;
		bool result2 = call_GetPathBetweenAIFaceIndicesWithRegionSwitchCostDelegate(scenePointer, startingAiFace, endingAiFace, startingPosition, endingPosition, agentRadius, pointer, ref pathSize, pointer2, exclusionGroupIdsCount, extraCostMultiplier, regionSwitchCostTo0, regionSwitchCostTo1);
		pinnedArrayData.Dispose();
		pinnedArrayData2.Dispose();
		return result2;
	}

	public bool GetPathBetweenAIFacePointers(UIntPtr scenePointer, UIntPtr startingAiFace, UIntPtr endingAiFace, Vec2 startingPosition, Vec2 endingPosition, float agentRadius, Vec2[] result, ref int pathSize, int[] exclusionGroupIds, int exclusionGroupIdsCount)
	{
		PinnedArrayData<Vec2> pinnedArrayData = new PinnedArrayData<Vec2>(result);
		IntPtr pointer = pinnedArrayData.Pointer;
		PinnedArrayData<int> pinnedArrayData2 = new PinnedArrayData<int>(exclusionGroupIds);
		IntPtr pointer2 = pinnedArrayData2.Pointer;
		bool result2 = call_GetPathBetweenAIFacePointersDelegate(scenePointer, startingAiFace, endingAiFace, startingPosition, endingPosition, agentRadius, pointer, ref pathSize, pointer2, exclusionGroupIdsCount);
		pinnedArrayData.Dispose();
		pinnedArrayData2.Dispose();
		return result2;
	}

	public bool GetPathBetweenAIFacePointersWithRegionSwitchCost(UIntPtr scenePointer, UIntPtr startingAiFace, UIntPtr endingAiFace, Vec2 startingPosition, Vec2 endingPosition, float agentRadius, Vec2[] result, ref int pathSize, int[] exclusionGroupIds, int exclusionGroupIdsCount, int regionSwitchCostTo0, int regionSwitchCostTo1)
	{
		PinnedArrayData<Vec2> pinnedArrayData = new PinnedArrayData<Vec2>(result);
		IntPtr pointer = pinnedArrayData.Pointer;
		PinnedArrayData<int> pinnedArrayData2 = new PinnedArrayData<int>(exclusionGroupIds);
		IntPtr pointer2 = pinnedArrayData2.Pointer;
		bool result2 = call_GetPathBetweenAIFacePointersWithRegionSwitchCostDelegate(scenePointer, startingAiFace, endingAiFace, startingPosition, endingPosition, agentRadius, pointer, ref pathSize, pointer2, exclusionGroupIdsCount, regionSwitchCostTo0, regionSwitchCostTo1);
		pinnedArrayData.Dispose();
		pinnedArrayData2.Dispose();
		return result2;
	}

	public bool GetPathDistanceBetweenAIFaces(UIntPtr scenePointer, int startingAiFace, int endingAiFace, Vec2 startingPosition, Vec2 endingPosition, float agentRadius, float distanceLimit, out float distance, int[] exclusionGroupIds, int exclusionGroupIdsCount, int regionSwitchCostTo0, int regionSwitchCostTo1)
	{
		PinnedArrayData<int> pinnedArrayData = new PinnedArrayData<int>(exclusionGroupIds);
		IntPtr pointer = pinnedArrayData.Pointer;
		bool result = call_GetPathDistanceBetweenAIFacesDelegate(scenePointer, startingAiFace, endingAiFace, startingPosition, endingPosition, agentRadius, distanceLimit, out distance, pointer, exclusionGroupIdsCount, regionSwitchCostTo0, regionSwitchCostTo1);
		pinnedArrayData.Dispose();
		return result;
	}

	public bool GetPathDistanceBetweenPositions(UIntPtr scenePointer, ref WorldPosition position, ref WorldPosition destination, float agentRadius, ref float pathLength)
	{
		return call_GetPathDistanceBetweenPositionsDelegate(scenePointer, ref position, ref destination, agentRadius, ref pathLength);
	}

	public PathFaceRecord GetPathFaceRecordFromNavMeshFacePointer(UIntPtr scenePointer, UIntPtr navMeshFacePointer)
	{
		return call_GetPathFaceRecordFromNavMeshFacePointerDelegate(scenePointer, navMeshFacePointer);
	}

	public void GetPathsWithNamePrefix(UIntPtr ptr, UIntPtr[] points, string prefix)
	{
		PinnedArrayData<UIntPtr> pinnedArrayData = new PinnedArrayData<UIntPtr>(points);
		IntPtr pointer = pinnedArrayData.Pointer;
		byte[] array = null;
		if (prefix != null)
		{
			int byteCount = _utf8.GetByteCount(prefix);
			array = ((byteCount < 1024) ? CallbackStringBufferManager.StringBuffer0 : new byte[byteCount + 1]);
			_utf8.GetBytes(prefix, 0, prefix.Length, array, 0);
			array[byteCount] = 0;
		}
		call_GetPathsWithNamePrefixDelegate(ptr, pointer, array);
		pinnedArrayData.Dispose();
	}

	public Path GetPathWithName(UIntPtr scenePointer, string name)
	{
		byte[] array = null;
		if (name != null)
		{
			int byteCount = _utf8.GetByteCount(name);
			array = ((byteCount < 1024) ? CallbackStringBufferManager.StringBuffer0 : new byte[byteCount + 1]);
			_utf8.GetBytes(name, 0, name.Length, array, 0);
			array[byteCount] = 0;
		}
		NativeObjectPointer nativeObjectPointer = call_GetPathWithNameDelegate(scenePointer, array);
		Path result = null;
		if (nativeObjectPointer.Pointer != UIntPtr.Zero)
		{
			result = new Path(nativeObjectPointer.Pointer);
			LibraryApplicationInterface.IManaged.DecreaseReferenceCount(nativeObjectPointer.Pointer);
		}
		return result;
	}

	public void GetPhotoModeFocus(Scene scene, ref float focus, ref float focusStart, ref float focusEnd, ref float exposure, ref bool vignetteOn)
	{
		UIntPtr scene2 = ((scene != null) ? scene.Pointer : UIntPtr.Zero);
		call_GetPhotoModeFocusDelegate(scene2, ref focus, ref focusStart, ref focusEnd, ref exposure, ref vignetteOn);
	}

	public float GetPhotoModeFov(Scene scene)
	{
		UIntPtr scene2 = ((scene != null) ? scene.Pointer : UIntPtr.Zero);
		return call_GetPhotoModeFovDelegate(scene2);
	}

	public bool GetPhotoModeOn(Scene scene)
	{
		UIntPtr scene2 = ((scene != null) ? scene.Pointer : UIntPtr.Zero);
		return call_GetPhotoModeOnDelegate(scene2);
	}

	public bool GetPhotoModeOrbit(Scene scene)
	{
		UIntPtr scene2 = ((scene != null) ? scene.Pointer : UIntPtr.Zero);
		return call_GetPhotoModeOrbitDelegate(scene2);
	}

	public float GetPhotoModeRoll(Scene scene)
	{
		UIntPtr scene2 = ((scene != null) ? scene.Pointer : UIntPtr.Zero);
		return call_GetPhotoModeRollDelegate(scene2);
	}

	public void GetPhysicsMinMax(UIntPtr scenePointer, ref Vec3 min_max)
	{
		call_GetPhysicsMinMaxDelegate(scenePointer, ref min_max);
	}

	public float GetRainDensity(UIntPtr scenePointer)
	{
		return call_GetRainDensityDelegate(scenePointer);
	}

	public void GetRootEntities(Scene scene, NativeObjectArray output)
	{
		UIntPtr scene2 = ((scene != null) ? scene.Pointer : UIntPtr.Zero);
		UIntPtr output2 = ((output != null) ? output.Pointer : UIntPtr.Zero);
		call_GetRootEntitiesDelegate(scene2, output2);
	}

	public int GetRootEntityCount(UIntPtr scenePointer)
	{
		return call_GetRootEntityCountDelegate(scenePointer);
	}

	public int GetSceneColorGradeIndex(Scene scene)
	{
		UIntPtr scene2 = ((scene != null) ? scene.Pointer : UIntPtr.Zero);
		return call_GetSceneColorGradeIndexDelegate(scene2);
	}

	public int GetSceneFilterIndex(Scene scene)
	{
		UIntPtr scene2 = ((scene != null) ? scene.Pointer : UIntPtr.Zero);
		return call_GetSceneFilterIndexDelegate(scene2);
	}

	public void GetSceneLimits(UIntPtr scenePointer, ref Vec3 min, ref Vec3 max)
	{
		call_GetSceneLimitsDelegate(scenePointer, ref min, ref max);
	}

	public uint GetSceneXMLCRC(UIntPtr scenePointer)
	{
		return call_GetSceneXMLCRCDelegate(scenePointer);
	}

	public GameEntity GetScriptedEntity(UIntPtr scenePointer, int index)
	{
		NativeObjectPointer nativeObjectPointer = call_GetScriptedEntityDelegate(scenePointer, index);
		GameEntity result = null;
		if (nativeObjectPointer.Pointer != UIntPtr.Zero)
		{
			result = new GameEntity(nativeObjectPointer.Pointer);
			LibraryApplicationInterface.IManaged.DecreaseReferenceCount(nativeObjectPointer.Pointer);
		}
		return result;
	}

	public int GetScriptedEntityCount(UIntPtr scenePointer)
	{
		return call_GetScriptedEntityCountDelegate(scenePointer);
	}

	public Mesh GetSkyboxMesh(UIntPtr ptr)
	{
		NativeObjectPointer nativeObjectPointer = call_GetSkyboxMeshDelegate(ptr);
		Mesh result = null;
		if (nativeObjectPointer.Pointer != UIntPtr.Zero)
		{
			result = new Mesh(nativeObjectPointer.Pointer);
			LibraryApplicationInterface.IManaged.DecreaseReferenceCount(nativeObjectPointer.Pointer);
		}
		return result;
	}

	public float GetSnowDensity(UIntPtr scenePointer)
	{
		return call_GetSnowDensityDelegate(scenePointer);
	}

	public Vec2 GetSoftBoundaryVertex(UIntPtr scenePointer, int index)
	{
		return call_GetSoftBoundaryVertexDelegate(scenePointer, index);
	}

	public int GetSoftBoundaryVertexCount(UIntPtr scenePointer)
	{
		return call_GetSoftBoundaryVertexCountDelegate(scenePointer);
	}

	public Vec3 GetSunDirection(UIntPtr scenePointer)
	{
		return call_GetSunDirectionDelegate(scenePointer);
	}

	public void GetTerrainData(Scene scene, out Vec2i nodeDimension, out float nodeSize, out int layerCount, out int layerVersion)
	{
		UIntPtr scene2 = ((scene != null) ? scene.Pointer : UIntPtr.Zero);
		call_GetTerrainDataDelegate(scene2, out nodeDimension, out nodeSize, out layerCount, out layerVersion);
	}

	public float GetTerrainHeight(UIntPtr scenePointer, Vec2 position, bool checkHoles)
	{
		return call_GetTerrainHeightDelegate(scenePointer, position, checkHoles);
	}

	public void GetTerrainHeightAndNormal(UIntPtr scenePointer, Vec2 position, out float height, out Vec3 normal)
	{
		call_GetTerrainHeightAndNormalDelegate(scenePointer, position, out height, out normal);
	}

	public int GetTerrainMemoryUsage(UIntPtr scenePointer)
	{
		return call_GetTerrainMemoryUsageDelegate(scenePointer);
	}

	public bool GetTerrainMinMaxHeight(Scene scene, ref float min, ref float max)
	{
		UIntPtr scene2 = ((scene != null) ? scene.Pointer : UIntPtr.Zero);
		return call_GetTerrainMinMaxHeightDelegate(scene2, ref min, ref max);
	}

	public void GetTerrainNodeData(Scene scene, int xIndex, int yIndex, out int vertexCountAlongAxis, out float quadLength, out float minHeight, out float maxHeight)
	{
		UIntPtr scene2 = ((scene != null) ? scene.Pointer : UIntPtr.Zero);
		call_GetTerrainNodeDataDelegate(scene2, xIndex, yIndex, out vertexCountAlongAxis, out quadLength, out minHeight, out maxHeight);
	}

	public int GetTerrainPhysicsMaterialIndexAtLayer(Scene scene, int layerIndex)
	{
		UIntPtr scene2 = ((scene != null) ? scene.Pointer : UIntPtr.Zero);
		return call_GetTerrainPhysicsMaterialIndexAtLayerDelegate(scene2, layerIndex);
	}

	public float GetTimeOfDay(UIntPtr scenePointer)
	{
		return call_GetTimeOfDayDelegate(scenePointer);
	}

	public float GetTimeSpeed(UIntPtr scenePointer)
	{
		return call_GetTimeSpeedDelegate(scenePointer);
	}

	public int GetUpgradeLevelCount(UIntPtr scenePointer)
	{
		return call_GetUpgradeLevelCountDelegate(scenePointer);
	}

	public uint GetUpgradeLevelMask(UIntPtr scenePointer)
	{
		return call_GetUpgradeLevelMaskDelegate(scenePointer);
	}

	public uint GetUpgradeLevelMaskOfLevelName(UIntPtr scenePointer, string levelName)
	{
		byte[] array = null;
		if (levelName != null)
		{
			int byteCount = _utf8.GetByteCount(levelName);
			array = ((byteCount < 1024) ? CallbackStringBufferManager.StringBuffer0 : new byte[byteCount + 1]);
			_utf8.GetBytes(levelName, 0, levelName.Length, array, 0);
			array[byteCount] = 0;
		}
		return call_GetUpgradeLevelMaskOfLevelNameDelegate(scenePointer, array);
	}

	public string GetUpgradeLevelNameOfIndex(UIntPtr scenePointer, int index)
	{
		if (call_GetUpgradeLevelNameOfIndexDelegate(scenePointer, index) != 1)
		{
			return null;
		}
		return Managed.ReturnValueFromEngine;
	}

	public float GetWaterLevel(Scene scene)
	{
		UIntPtr scene2 = ((scene != null) ? scene.Pointer : UIntPtr.Zero);
		return call_GetWaterLevelDelegate(scene2);
	}

	public float GetWaterLevelAtPosition(Scene scene, Vec2 position, bool useWaterRenderer, bool checkWaterBodyEntities)
	{
		UIntPtr scene2 = ((scene != null) ? scene.Pointer : UIntPtr.Zero);
		return call_GetWaterLevelAtPositionDelegate(scene2, position, useWaterRenderer, checkWaterBodyEntities);
	}

	public Vec3 GetWaterSpeedAtPosition(UIntPtr scenePointer, in Vec2 position, bool doChoppinessCorrection)
	{
		return call_GetWaterSpeedAtPositionDelegate(scenePointer, in position, doChoppinessCorrection);
	}

	public float GetWaterStrength(Scene scene)
	{
		UIntPtr scene2 = ((scene != null) ? scene.Pointer : UIntPtr.Zero);
		return call_GetWaterStrengthDelegate(scene2);
	}

	public void GetWindFlowMapData(UIntPtr scenePointer, float[] flowmapData)
	{
		PinnedArrayData<float> pinnedArrayData = new PinnedArrayData<float>(flowmapData);
		IntPtr pointer = pinnedArrayData.Pointer;
		call_GetWindFlowMapDataDelegate(scenePointer, pointer);
		pinnedArrayData.Dispose();
	}

	public float GetWinterTimeFactor(UIntPtr scenePointer)
	{
		return call_GetWinterTimeFactorDelegate(scenePointer);
	}

	public void HandleCurrentFrameTickEntities(UIntPtr scenePointer)
	{
		call_HandleCurrentFrameTickEntitiesDelegate(scenePointer);
	}

	public bool HasDecalRenderer(UIntPtr scenePointer)
	{
		return call_HasDecalRendererDelegate(scenePointer);
	}

	public bool HasNavmeshFaceUnsharedEdges(UIntPtr scenePointer, in PathFaceRecord faceRecord)
	{
		return call_HasNavmeshFaceUnsharedEdgesDelegate(scenePointer, in faceRecord);
	}

	public bool HasTerrainHeightmap(UIntPtr scenePointer)
	{
		return call_HasTerrainHeightmapDelegate(scenePointer);
	}

	public void InvalidateTerrainPhysicsMaterials(UIntPtr scenePointer)
	{
		call_InvalidateTerrainPhysicsMaterialsDelegate(scenePointer);
	}

	public bool IsAnyFaceWithId(UIntPtr scenePointer, int faceGroupId)
	{
		return call_IsAnyFaceWithIdDelegate(scenePointer, faceGroupId);
	}

	public bool IsAtmosphereIndoor(UIntPtr scenePointer)
	{
		return call_IsAtmosphereIndoorDelegate(scenePointer);
	}

	public bool IsDefaultEditorScene(Scene scene)
	{
		UIntPtr scene2 = ((scene != null) ? scene.Pointer : UIntPtr.Zero);
		return call_IsDefaultEditorSceneDelegate(scene2);
	}

	public bool IsEditorScene(UIntPtr scenePointer)
	{
		return call_IsEditorSceneDelegate(scenePointer);
	}

	public bool IsLineToPointClear(UIntPtr scenePointer, int startingFace, Vec2 position, Vec2 destination, float agentRadius)
	{
		return call_IsLineToPointClearDelegate(scenePointer, startingFace, position, destination, agentRadius);
	}

	public bool IsLineToPointClear2(UIntPtr scenePointer, UIntPtr startingFace, Vec2 position, Vec2 destination, float agentRadius)
	{
		return call_IsLineToPointClear2Delegate(scenePointer, startingFace, position, destination, agentRadius);
	}

	public bool IsLoadingFinished(Scene scene)
	{
		UIntPtr scene2 = ((scene != null) ? scene.Pointer : UIntPtr.Zero);
		return call_IsLoadingFinishedDelegate(scene2);
	}

	public bool IsMultiplayerScene(Scene scene)
	{
		UIntPtr scene2 = ((scene != null) ? scene.Pointer : UIntPtr.Zero);
		return call_IsMultiplayerSceneDelegate(scene2);
	}

	public bool IsPositionOnADynamicNavMesh(UIntPtr scenePointer, Vec3 position)
	{
		return call_IsPositionOnADynamicNavMeshDelegate(scenePointer, position);
	}

	public void LoadNavMeshPrefab(UIntPtr scenePointer, string navMeshPrefabName, int navMeshGroupIdShift)
	{
		byte[] array = null;
		if (navMeshPrefabName != null)
		{
			int byteCount = _utf8.GetByteCount(navMeshPrefabName);
			array = ((byteCount < 1024) ? CallbackStringBufferManager.StringBuffer0 : new byte[byteCount + 1]);
			_utf8.GetBytes(navMeshPrefabName, 0, navMeshPrefabName.Length, array, 0);
			array[byteCount] = 0;
		}
		call_LoadNavMeshPrefabDelegate(scenePointer, array, navMeshGroupIdShift);
	}

	public void LoadNavMeshPrefabWithFrame(UIntPtr scenePointer, string navMeshPrefabName, MatrixFrame frame)
	{
		byte[] array = null;
		if (navMeshPrefabName != null)
		{
			int byteCount = _utf8.GetByteCount(navMeshPrefabName);
			array = ((byteCount < 1024) ? CallbackStringBufferManager.StringBuffer0 : new byte[byteCount + 1]);
			_utf8.GetBytes(navMeshPrefabName, 0, navMeshPrefabName.Length, array, 0);
			array[byteCount] = 0;
		}
		call_LoadNavMeshPrefabWithFrameDelegate(scenePointer, array, frame);
	}

	public void MarkFacesWithIdAsLadder(UIntPtr scenePointer, int faceGroupId, bool isLadder)
	{
		call_MarkFacesWithIdAsLadderDelegate(scenePointer, faceGroupId, isLadder);
	}

	public void MergeFacesWithId(UIntPtr scenePointer, int faceGroupId0, int faceGroupId1, int newFaceGroupId)
	{
		call_MergeFacesWithIdDelegate(scenePointer, faceGroupId0, faceGroupId1, newFaceGroupId);
	}

	public void OptimizeScene(UIntPtr scenePointer, bool optimizeFlora, bool optimizeOro)
	{
		call_OptimizeSceneDelegate(scenePointer, optimizeFlora, optimizeOro);
	}

	public void PauseSceneSounds(UIntPtr scenePointer)
	{
		call_PauseSceneSoundsDelegate(scenePointer);
	}

	public void PreloadForRendering(UIntPtr scenePointer)
	{
		call_PreloadForRenderingDelegate(scenePointer);
	}

	public bool RayCastExcludingTwoEntities(BodyFlags flags, UIntPtr scenePointer, in Ray ray, UIntPtr entity1, UIntPtr entity2)
	{
		return call_RayCastExcludingTwoEntitiesDelegate(flags, scenePointer, in ray, entity1, entity2);
	}

	public bool RayCastForClosestEntityOrTerrain(UIntPtr scenePointer, in Vec3 sourcePoint, in Vec3 targetPoint, float rayThickness, ref float collisionDistance, ref Vec3 closestPoint, ref UIntPtr entityIndex, BodyFlags bodyExcludeFlags, bool isFixedWorld)
	{
		return call_RayCastForClosestEntityOrTerrainDelegate(scenePointer, in sourcePoint, in targetPoint, rayThickness, ref collisionDistance, ref closestPoint, ref entityIndex, bodyExcludeFlags, isFixedWorld);
	}

	public bool RayCastForClosestEntityOrTerrainIgnoreEntity(UIntPtr scenePointer, in Vec3 sourcePoint, in Vec3 targetPoint, float rayThickness, ref float collisionDistance, ref Vec3 closestPoint, ref UIntPtr entityIndex, BodyFlags bodyExcludeFlags, UIntPtr ignoredEntityPointer)
	{
		return call_RayCastForClosestEntityOrTerrainIgnoreEntityDelegate(scenePointer, in sourcePoint, in targetPoint, rayThickness, ref collisionDistance, ref closestPoint, ref entityIndex, bodyExcludeFlags, ignoredEntityPointer);
	}

	public bool RayCastForRamming(UIntPtr scenePointer, in Vec3 sourcePoint, in Vec3 targetPoint, float rayThickness, ref float collisionDistance, ref Vec3 intersectionPoint, ref UIntPtr intersectedEntityIndex, BodyFlags bodyExcludeFlags, BodyFlags bodyIncludeFlags, UIntPtr ignoredEntityPointer)
	{
		return call_RayCastForRammingDelegate(scenePointer, in sourcePoint, in targetPoint, rayThickness, ref collisionDistance, ref intersectionPoint, ref intersectedEntityIndex, bodyExcludeFlags, bodyIncludeFlags, ignoredEntityPointer);
	}

	public void Read(UIntPtr scenePointer, string sceneName, ref SceneInitializationData initData, string forcedAtmoName)
	{
		byte[] array = null;
		if (sceneName != null)
		{
			int byteCount = _utf8.GetByteCount(sceneName);
			array = ((byteCount < 1024) ? CallbackStringBufferManager.StringBuffer0 : new byte[byteCount + 1]);
			_utf8.GetBytes(sceneName, 0, sceneName.Length, array, 0);
			array[byteCount] = 0;
		}
		byte[] array2 = null;
		if (forcedAtmoName != null)
		{
			int byteCount2 = _utf8.GetByteCount(forcedAtmoName);
			array2 = ((byteCount2 < 1024) ? CallbackStringBufferManager.StringBuffer1 : new byte[byteCount2 + 1]);
			_utf8.GetBytes(forcedAtmoName, 0, forcedAtmoName.Length, array2, 0);
			array2[byteCount2] = 0;
		}
		call_ReadDelegate(scenePointer, array, ref initData, array2);
	}

	public void ReadAndCalculateInitialCamera(UIntPtr scenePointer, ref MatrixFrame outFrame)
	{
		call_ReadAndCalculateInitialCameraDelegate(scenePointer, ref outFrame);
	}

	public void ReadInModule(UIntPtr scenePointer, string sceneName, string moduleId, ref SceneInitializationData initData, string forcedAtmoName)
	{
		byte[] array = null;
		if (sceneName != null)
		{
			int byteCount = _utf8.GetByteCount(sceneName);
			array = ((byteCount < 1024) ? CallbackStringBufferManager.StringBuffer0 : new byte[byteCount + 1]);
			_utf8.GetBytes(sceneName, 0, sceneName.Length, array, 0);
			array[byteCount] = 0;
		}
		byte[] array2 = null;
		if (moduleId != null)
		{
			int byteCount2 = _utf8.GetByteCount(moduleId);
			array2 = ((byteCount2 < 1024) ? CallbackStringBufferManager.StringBuffer1 : new byte[byteCount2 + 1]);
			_utf8.GetBytes(moduleId, 0, moduleId.Length, array2, 0);
			array2[byteCount2] = 0;
		}
		byte[] array3 = null;
		if (forcedAtmoName != null)
		{
			int byteCount3 = _utf8.GetByteCount(forcedAtmoName);
			array3 = ((byteCount3 < 1024) ? CallbackStringBufferManager.StringBuffer2 : new byte[byteCount3 + 1]);
			_utf8.GetBytes(forcedAtmoName, 0, forcedAtmoName.Length, array3, 0);
			array3[byteCount3] = 0;
		}
		call_ReadInModuleDelegate(scenePointer, array, array2, ref initData, array3);
	}

	public UIntPtr RegisterShipVisualToWaterRenderer(UIntPtr scenePointer, UIntPtr entityPointer, in Vec3 bb)
	{
		return call_RegisterShipVisualToWaterRendererDelegate(scenePointer, entityPointer, in bb);
	}

	public void RemoveAlwaysRenderedSkeleton(UIntPtr scenePointer, UIntPtr skeletonPointer)
	{
		call_RemoveAlwaysRenderedSkeletonDelegate(scenePointer, skeletonPointer);
	}

	public void RemoveDecalInstance(UIntPtr scenePointer, UIntPtr decalMeshPointer, string decalSetID)
	{
		byte[] array = null;
		if (decalSetID != null)
		{
			int byteCount = _utf8.GetByteCount(decalSetID);
			array = ((byteCount < 1024) ? CallbackStringBufferManager.StringBuffer0 : new byte[byteCount + 1]);
			_utf8.GetBytes(decalSetID, 0, decalSetID.Length, array, 0);
			array[byteCount] = 0;
		}
		call_RemoveDecalInstanceDelegate(scenePointer, decalMeshPointer, array);
	}

	public void RemoveEntity(UIntPtr scenePointer, UIntPtr entityId, int removeReason)
	{
		call_RemoveEntityDelegate(scenePointer, entityId, removeReason);
	}

	public void ResumeLoadingRenderings(UIntPtr scenePointer)
	{
		call_ResumeLoadingRenderingsDelegate(scenePointer);
	}

	public void ResumeSceneSounds(UIntPtr scenePointer)
	{
		call_ResumeSceneSoundsDelegate(scenePointer);
	}

	public void SaveNavMeshPrefabWithFrame(UIntPtr scenePointer, string navMeshPrefabName, MatrixFrame frame)
	{
		byte[] array = null;
		if (navMeshPrefabName != null)
		{
			int byteCount = _utf8.GetByteCount(navMeshPrefabName);
			array = ((byteCount < 1024) ? CallbackStringBufferManager.StringBuffer0 : new byte[byteCount + 1]);
			_utf8.GetBytes(navMeshPrefabName, 0, navMeshPrefabName.Length, array, 0);
			array[byteCount] = 0;
		}
		call_SaveNavMeshPrefabWithFrameDelegate(scenePointer, array, frame);
	}

	public bool SceneHadWaterWakeRenderer(UIntPtr scenePointer)
	{
		return call_SceneHadWaterWakeRendererDelegate(scenePointer);
	}

	public int SelectEntitiesCollidedWith(UIntPtr scenePointer, ref Ray ray, UIntPtr[] entityIds, Intersection[] intersections)
	{
		PinnedArrayData<UIntPtr> pinnedArrayData = new PinnedArrayData<UIntPtr>(entityIds);
		IntPtr pointer = pinnedArrayData.Pointer;
		PinnedArrayData<Intersection> pinnedArrayData2 = new PinnedArrayData<Intersection>(intersections);
		IntPtr pointer2 = pinnedArrayData2.Pointer;
		int result = call_SelectEntitiesCollidedWithDelegate(scenePointer, ref ray, pointer, pointer2);
		pinnedArrayData.Dispose();
		pinnedArrayData2.Dispose();
		return result;
	}

	public int SelectEntitiesInBoxWithScriptComponent(UIntPtr scenePointer, ref Vec3 boundingBoxMin, ref Vec3 boundingBoxMax, UIntPtr[] entitiesOutput, int maxCount, string scriptComponentName, bool isFixedTick)
	{
		PinnedArrayData<UIntPtr> pinnedArrayData = new PinnedArrayData<UIntPtr>(entitiesOutput);
		IntPtr pointer = pinnedArrayData.Pointer;
		byte[] array = null;
		if (scriptComponentName != null)
		{
			int byteCount = _utf8.GetByteCount(scriptComponentName);
			array = ((byteCount < 1024) ? CallbackStringBufferManager.StringBuffer0 : new byte[byteCount + 1]);
			_utf8.GetBytes(scriptComponentName, 0, scriptComponentName.Length, array, 0);
			array[byteCount] = 0;
		}
		int result = call_SelectEntitiesInBoxWithScriptComponentDelegate(scenePointer, ref boundingBoxMin, ref boundingBoxMax, pointer, maxCount, array, isFixedTick);
		pinnedArrayData.Dispose();
		return result;
	}

	public void SeparateFacesWithId(UIntPtr scenePointer, int faceGroupId0, int faceGroupId1)
	{
		call_SeparateFacesWithIdDelegate(scenePointer, faceGroupId0, faceGroupId1);
	}

	public void SetAberrationOffset(UIntPtr scenePointer, float aberrationOffset)
	{
		call_SetAberrationOffsetDelegate(scenePointer, aberrationOffset);
	}

	public void SetAberrationSize(UIntPtr scenePointer, float aberrationSize)
	{
		call_SetAberrationSizeDelegate(scenePointer, aberrationSize);
	}

	public void SetAberrationSmooth(UIntPtr scenePointer, float aberrationSmooth)
	{
		call_SetAberrationSmoothDelegate(scenePointer, aberrationSmooth);
	}

	public int SetAbilityOfFacesWithId(UIntPtr scenePointer, int faceGroupId, bool isEnabled)
	{
		return call_SetAbilityOfFacesWithIdDelegate(scenePointer, faceGroupId, isEnabled);
	}

	public void SetActiveVisibilityLevels(UIntPtr scenePointer, string levelsAppended)
	{
		byte[] array = null;
		if (levelsAppended != null)
		{
			int byteCount = _utf8.GetByteCount(levelsAppended);
			array = ((byteCount < 1024) ? CallbackStringBufferManager.StringBuffer0 : new byte[byteCount + 1]);
			_utf8.GetBytes(levelsAppended, 0, levelsAppended.Length, array, 0);
			array[byteCount] = 0;
		}
		call_SetActiveVisibilityLevelsDelegate(scenePointer, array);
	}

	public void SetAntialiasingMode(UIntPtr scenePointer, bool mode)
	{
		call_SetAntialiasingModeDelegate(scenePointer, mode);
	}

	public void SetAtmosphereWithName(UIntPtr ptr, string name)
	{
		byte[] array = null;
		if (name != null)
		{
			int byteCount = _utf8.GetByteCount(name);
			array = ((byteCount < 1024) ? CallbackStringBufferManager.StringBuffer0 : new byte[byteCount + 1]);
			_utf8.GetBytes(name, 0, name.Length, array, 0);
			array[byteCount] = 0;
		}
		call_SetAtmosphereWithNameDelegate(ptr, array);
	}

	public void SetBlockerDirectionForFacesWithId(UIntPtr scenePointer, int faceGroupId, float rotation)
	{
		call_SetBlockerDirectionForFacesWithIdDelegate(scenePointer, faceGroupId, rotation);
	}

	public void SetBloom(UIntPtr scenePointer, bool mode)
	{
		call_SetBloomDelegate(scenePointer, mode);
	}

	public void SetBloomAmount(UIntPtr scenePointer, float bloomAmount)
	{
		call_SetBloomAmountDelegate(scenePointer, bloomAmount);
	}

	public void SetBloomStrength(UIntPtr scenePointer, float bloomStrength)
	{
		call_SetBloomStrengthDelegate(scenePointer, bloomStrength);
	}

	public void SetBrightpassTreshold(UIntPtr scenePointer, float threshold)
	{
		call_SetBrightpassTresholdDelegate(scenePointer, threshold);
	}

	public void SetClothSimulationState(UIntPtr scenePointer, bool state)
	{
		call_SetClothSimulationStateDelegate(scenePointer, state);
	}

	public void SetColorGradeBlend(UIntPtr scenePointer, string texture1, string texture2, float alpha)
	{
		byte[] array = null;
		if (texture1 != null)
		{
			int byteCount = _utf8.GetByteCount(texture1);
			array = ((byteCount < 1024) ? CallbackStringBufferManager.StringBuffer0 : new byte[byteCount + 1]);
			_utf8.GetBytes(texture1, 0, texture1.Length, array, 0);
			array[byteCount] = 0;
		}
		byte[] array2 = null;
		if (texture2 != null)
		{
			int byteCount2 = _utf8.GetByteCount(texture2);
			array2 = ((byteCount2 < 1024) ? CallbackStringBufferManager.StringBuffer1 : new byte[byteCount2 + 1]);
			_utf8.GetBytes(texture2, 0, texture2.Length, array2, 0);
			array2[byteCount2] = 0;
		}
		call_SetColorGradeBlendDelegate(scenePointer, array, array2, alpha);
	}

	public void SetDLSSMode(UIntPtr scenePointer, bool mode)
	{
		call_SetDLSSModeDelegate(scenePointer, mode);
	}

	public void SetDofFocus(UIntPtr scenePointer, float dofFocus)
	{
		call_SetDofFocusDelegate(scenePointer, dofFocus);
	}

	public void SetDofMode(UIntPtr scenePointer, bool mode)
	{
		call_SetDofModeDelegate(scenePointer, mode);
	}

	public void SetDofParams(UIntPtr scenePointer, float dofFocusStart, float dofFocusEnd, bool isVignetteOn)
	{
		call_SetDofParamsDelegate(scenePointer, dofFocusStart, dofFocusEnd, isVignetteOn);
	}

	public void SetDoNotAddEntitiesToTickList(UIntPtr scenePointer, bool value)
	{
		call_SetDoNotAddEntitiesToTickListDelegate(scenePointer, value);
	}

	public void SetDoNotWaitForLoadingStatesToRender(UIntPtr scenePointer, bool value)
	{
		call_SetDoNotWaitForLoadingStatesToRenderDelegate(scenePointer, value);
	}

	public void SetDontLoadInvisibleEntities(UIntPtr scenePointer, bool value)
	{
		call_SetDontLoadInvisibleEntitiesDelegate(scenePointer, value);
	}

	public void SetDrynessFactor(UIntPtr scenePointer, float drynessFactor)
	{
		call_SetDrynessFactorDelegate(scenePointer, drynessFactor);
	}

	public void SetDynamicShadowmapCascadesRadiusMultiplier(UIntPtr scenePointer, float extraRadius)
	{
		call_SetDynamicShadowmapCascadesRadiusMultiplierDelegate(scenePointer, extraRadius);
	}

	public void SetDynamicSnowTexture(UIntPtr scenePointer, UIntPtr texturePointer)
	{
		call_SetDynamicSnowTextureDelegate(scenePointer, texturePointer);
	}

	public void SetEnvironmentMultiplier(UIntPtr scenePointer, bool useMultiplier, float multiplier)
	{
		call_SetEnvironmentMultiplierDelegate(scenePointer, useMultiplier, multiplier);
	}

	public void SetExternalInjectionTexture(UIntPtr scenePointer, UIntPtr texturePointer)
	{
		call_SetExternalInjectionTextureDelegate(scenePointer, texturePointer);
	}

	public void SetFetchCrcInfoOfScene(UIntPtr scenePointer, bool value)
	{
		call_SetFetchCrcInfoOfSceneDelegate(scenePointer, value);
	}

	public void SetFixedTickCallbackActive(Scene scene, bool isActive)
	{
		UIntPtr scene2 = ((scene != null) ? scene.Pointer : UIntPtr.Zero);
		call_SetFixedTickCallbackActiveDelegate(scene2, isActive);
	}

	public void SetFog(UIntPtr scenePointer, float fogDensity, ref Vec3 fogColor, float fogFalloff)
	{
		call_SetFogDelegate(scenePointer, fogDensity, ref fogColor, fogFalloff);
	}

	public void SetFogAdvanced(UIntPtr scenePointer, float fogFalloffOffset, float fogFalloffMinFog, float fogFalloffStartDist)
	{
		call_SetFogAdvancedDelegate(scenePointer, fogFalloffOffset, fogFalloffMinFog, fogFalloffStartDist);
	}

	public void SetFogAmbientColor(UIntPtr scenePointer, ref Vec3 fogAmbientColor)
	{
		call_SetFogAmbientColorDelegate(scenePointer, ref fogAmbientColor);
	}

	public void SetForcedSnow(UIntPtr scenePointer, bool value)
	{
		call_SetForcedSnowDelegate(scenePointer, value);
	}

	public void SetGlobalWindStrengthVector(UIntPtr scenePointer, in Vec2 strengthVector)
	{
		call_SetGlobalWindStrengthVectorDelegate(scenePointer, in strengthVector);
	}

	public void SetGlobalWindVelocity(UIntPtr scenePointer, in Vec2 windVelocity)
	{
		call_SetGlobalWindVelocityDelegate(scenePointer, in windVelocity);
	}

	public void SetGrainAmount(UIntPtr scenePointer, float grainAmount)
	{
		call_SetGrainAmountDelegate(scenePointer, grainAmount);
	}

	public void SetHexagonVignetteAlpha(UIntPtr scenePointer, float Alpha)
	{
		call_SetHexagonVignetteAlphaDelegate(scenePointer, Alpha);
	}

	public void SetHexagonVignetteColor(UIntPtr scenePointer, ref Vec3 p_hexagon_vignette_color)
	{
		call_SetHexagonVignetteColorDelegate(scenePointer, ref p_hexagon_vignette_color);
	}

	public void SetHumidity(UIntPtr scenePointer, float humidity)
	{
		call_SetHumidityDelegate(scenePointer, humidity);
	}

	public void SetLandscapeRainMaskData(UIntPtr scenePointer, byte[] data)
	{
		PinnedArrayData<byte> pinnedArrayData = new PinnedArrayData<byte>(data);
		IntPtr pointer = pinnedArrayData.Pointer;
		ManagedArray data2 = new ManagedArray(pointer, (data != null) ? data.Length : 0);
		call_SetLandscapeRainMaskDataDelegate(scenePointer, data2);
		pinnedArrayData.Dispose();
	}

	public void SetLensDistortion(UIntPtr scenePointer, float lensDistortion)
	{
		call_SetLensDistortionDelegate(scenePointer, lensDistortion);
	}

	public void SetLensFlareAberrationOffset(UIntPtr scenePointer, float lensFlareAberrationOffset)
	{
		call_SetLensFlareAberrationOffsetDelegate(scenePointer, lensFlareAberrationOffset);
	}

	public void SetLensFlareAmount(UIntPtr scenePointer, float lensFlareAmount)
	{
		call_SetLensFlareAmountDelegate(scenePointer, lensFlareAmount);
	}

	public void SetLensFlareBlurSigma(UIntPtr scenePointer, float lensFlareBlurSigma)
	{
		call_SetLensFlareBlurSigmaDelegate(scenePointer, lensFlareBlurSigma);
	}

	public void SetLensFlareBlurSize(UIntPtr scenePointer, int lensFlareBlurSize)
	{
		call_SetLensFlareBlurSizeDelegate(scenePointer, lensFlareBlurSize);
	}

	public void SetLensFlareDiffractionWeight(UIntPtr scenePointer, float lensFlareDiffractionWeight)
	{
		call_SetLensFlareDiffractionWeightDelegate(scenePointer, lensFlareDiffractionWeight);
	}

	public void SetLensFlareDirtWeight(UIntPtr scenePointer, float lensFlareDirtWeight)
	{
		call_SetLensFlareDirtWeightDelegate(scenePointer, lensFlareDirtWeight);
	}

	public void SetLensFlareGhostSamples(UIntPtr scenePointer, int lensFlareGhostSamples)
	{
		call_SetLensFlareGhostSamplesDelegate(scenePointer, lensFlareGhostSamples);
	}

	public void SetLensFlareGhostWeight(UIntPtr scenePointer, float lensFlareGhostWeight)
	{
		call_SetLensFlareGhostWeightDelegate(scenePointer, lensFlareGhostWeight);
	}

	public void SetLensFlareHaloWeight(UIntPtr scenePointer, float lensFlareHaloWeight)
	{
		call_SetLensFlareHaloWeightDelegate(scenePointer, lensFlareHaloWeight);
	}

	public void SetLensFlareHaloWidth(UIntPtr scenePointer, float lensFlareHaloWidth)
	{
		call_SetLensFlareHaloWidthDelegate(scenePointer, lensFlareHaloWidth);
	}

	public void SetLensFlareStrength(UIntPtr scenePointer, float lensFlareStrength)
	{
		call_SetLensFlareStrengthDelegate(scenePointer, lensFlareStrength);
	}

	public void SetLensFlareThreshold(UIntPtr scenePointer, float lensFlareThreshold)
	{
		call_SetLensFlareThresholdDelegate(scenePointer, lensFlareThreshold);
	}

	public void SetLightDiffuseColor(UIntPtr scenePointer, int lightIndex, Vec3 diffuseColor)
	{
		call_SetLightDiffuseColorDelegate(scenePointer, lightIndex, diffuseColor);
	}

	public void SetLightDirection(UIntPtr scenePointer, int lightIndex, Vec3 direction)
	{
		call_SetLightDirectionDelegate(scenePointer, lightIndex, direction);
	}

	public void SetLightPosition(UIntPtr scenePointer, int lightIndex, Vec3 position)
	{
		call_SetLightPositionDelegate(scenePointer, lightIndex, position);
	}

	public void SetMaxExposure(UIntPtr scenePointer, float maxExposure)
	{
		call_SetMaxExposureDelegate(scenePointer, maxExposure);
	}

	public void SetMiddleGray(UIntPtr scenePointer, float middleGray)
	{
		call_SetMiddleGrayDelegate(scenePointer, middleGray);
	}

	public void SetMieScatterFocus(UIntPtr scenePointer, float strength)
	{
		call_SetMieScatterFocusDelegate(scenePointer, strength);
	}

	public void SetMieScatterStrength(UIntPtr scenePointer, float strength)
	{
		call_SetMieScatterStrengthDelegate(scenePointer, strength);
	}

	public void SetMinExposure(UIntPtr scenePointer, float minExposure)
	{
		call_SetMinExposureDelegate(scenePointer, minExposure);
	}

	public void SetMotionBlurMode(UIntPtr scenePointer, bool mode)
	{
		call_SetMotionBlurModeDelegate(scenePointer, mode);
	}

	public void SetName(UIntPtr scenePointer, string name)
	{
		byte[] array = null;
		if (name != null)
		{
			int byteCount = _utf8.GetByteCount(name);
			array = ((byteCount < 1024) ? CallbackStringBufferManager.StringBuffer0 : new byte[byteCount + 1]);
			_utf8.GetBytes(name, 0, name.Length, array, 0);
			array[byteCount] = 0;
		}
		call_SetNameDelegate(scenePointer, array);
	}

	public void SetNavMeshRegionMap(UIntPtr scenePointer, bool[] regionMap, int regionMapSize)
	{
		PinnedArrayData<bool> pinnedArrayData = new PinnedArrayData<bool>(regionMap);
		IntPtr pointer = pinnedArrayData.Pointer;
		call_SetNavMeshRegionMapDelegate(scenePointer, pointer, regionMapSize);
		pinnedArrayData.Dispose();
	}

	public void SetOcclusionMode(UIntPtr scenePointer, bool mode)
	{
		call_SetOcclusionModeDelegate(scenePointer, mode);
	}

	public void SetOnCollisionFilterCallbackActive(Scene scene, bool isActive)
	{
		UIntPtr scene2 = ((scene != null) ? scene.Pointer : UIntPtr.Zero);
		call_SetOnCollisionFilterCallbackActiveDelegate(scene2, isActive);
	}

	public void SetOwnerThread(UIntPtr scenePointer)
	{
		call_SetOwnerThreadDelegate(scenePointer);
	}

	public void SetPhotoAtmosphereViaTod(UIntPtr scenePointer, float tod, bool withStorm)
	{
		call_SetPhotoAtmosphereViaTodDelegate(scenePointer, tod, withStorm);
	}

	public void SetPhotoModeFocus(Scene scene, float focusStart, float focusEnd, float focus, float exposure)
	{
		UIntPtr scene2 = ((scene != null) ? scene.Pointer : UIntPtr.Zero);
		call_SetPhotoModeFocusDelegate(scene2, focusStart, focusEnd, focus, exposure);
	}

	public void SetPhotoModeFov(Scene scene, float verticalFov)
	{
		UIntPtr scene2 = ((scene != null) ? scene.Pointer : UIntPtr.Zero);
		call_SetPhotoModeFovDelegate(scene2, verticalFov);
	}

	public void SetPhotoModeOn(Scene scene, bool on)
	{
		UIntPtr scene2 = ((scene != null) ? scene.Pointer : UIntPtr.Zero);
		call_SetPhotoModeOnDelegate(scene2, on);
	}

	public void SetPhotoModeOrbit(Scene scene, bool orbit)
	{
		UIntPtr scene2 = ((scene != null) ? scene.Pointer : UIntPtr.Zero);
		call_SetPhotoModeOrbitDelegate(scene2, orbit);
	}

	public void SetPhotoModeRoll(Scene scene, float roll)
	{
		UIntPtr scene2 = ((scene != null) ? scene.Pointer : UIntPtr.Zero);
		call_SetPhotoModeRollDelegate(scene2, roll);
	}

	public void SetPhotoModeVignette(Scene scene, bool vignetteOn)
	{
		UIntPtr scene2 = ((scene != null) ? scene.Pointer : UIntPtr.Zero);
		call_SetPhotoModeVignetteDelegate(scene2, vignetteOn);
	}

	public void SetPlaySoundEventsAfterReadyToRender(UIntPtr ptr, bool value)
	{
		call_SetPlaySoundEventsAfterReadyToRenderDelegate(ptr, value);
	}

	public void SetRainDensity(UIntPtr scenePointer, float density)
	{
		call_SetRainDensityDelegate(scenePointer, density);
	}

	public void SetSceneColorGrade(Scene scene, string textureName)
	{
		UIntPtr scene2 = ((scene != null) ? scene.Pointer : UIntPtr.Zero);
		byte[] array = null;
		if (textureName != null)
		{
			int byteCount = _utf8.GetByteCount(textureName);
			array = ((byteCount < 1024) ? CallbackStringBufferManager.StringBuffer0 : new byte[byteCount + 1]);
			_utf8.GetBytes(textureName, 0, textureName.Length, array, 0);
			array[byteCount] = 0;
		}
		call_SetSceneColorGradeDelegate(scene2, array);
	}

	public void SetSceneColorGradeIndex(Scene scene, int index)
	{
		UIntPtr scene2 = ((scene != null) ? scene.Pointer : UIntPtr.Zero);
		call_SetSceneColorGradeIndexDelegate(scene2, index);
	}

	public int SetSceneFilterIndex(Scene scene, int index)
	{
		UIntPtr scene2 = ((scene != null) ? scene.Pointer : UIntPtr.Zero);
		return call_SetSceneFilterIndexDelegate(scene2, index);
	}

	public void SetShadow(UIntPtr scenePointer, bool shadowEnabled)
	{
		call_SetShadowDelegate(scenePointer, shadowEnabled);
	}

	public void SetSkyBrightness(UIntPtr scenePointer, float brightness)
	{
		call_SetSkyBrightnessDelegate(scenePointer, brightness);
	}

	public void SetSkyRotation(UIntPtr scenePointer, float rotation)
	{
		call_SetSkyRotationDelegate(scenePointer, rotation);
	}

	public void SetSnowDensity(UIntPtr scenePointer, float density)
	{
		call_SetSnowDensityDelegate(scenePointer, density);
	}

	public void SetStreakAmount(UIntPtr scenePointer, float streakAmount)
	{
		call_SetStreakAmountDelegate(scenePointer, streakAmount);
	}

	public void SetStreakIntensity(UIntPtr scenePointer, float stretchAmount)
	{
		call_SetStreakIntensityDelegate(scenePointer, stretchAmount);
	}

	public void SetStreakStrength(UIntPtr scenePointer, float strengthAmount)
	{
		call_SetStreakStrengthDelegate(scenePointer, strengthAmount);
	}

	public void SetStreakStretch(UIntPtr scenePointer, float stretchAmount)
	{
		call_SetStreakStretchDelegate(scenePointer, stretchAmount);
	}

	public void SetStreakThreshold(UIntPtr scenePointer, float streakThreshold)
	{
		call_SetStreakThresholdDelegate(scenePointer, streakThreshold);
	}

	public void SetStreakTint(UIntPtr scenePointer, ref Vec3 p_streak_tint_color)
	{
		call_SetStreakTintDelegate(scenePointer, ref p_streak_tint_color);
	}

	public void SetSun(UIntPtr scenePointer, Vec3 color, float altitude, float angle, float intensity)
	{
		call_SetSunDelegate(scenePointer, color, altitude, angle, intensity);
	}

	public void SetSunAngleAltitude(UIntPtr scenePointer, float angle, float altitude)
	{
		call_SetSunAngleAltitudeDelegate(scenePointer, angle, altitude);
	}

	public void SetSunDirection(UIntPtr scenePointer, Vec3 direction)
	{
		call_SetSunDirectionDelegate(scenePointer, direction);
	}

	public void SetSunLight(UIntPtr scenePointer, Vec3 color, Vec3 direction)
	{
		call_SetSunLightDelegate(scenePointer, color, direction);
	}

	public void SetSunshaftMode(UIntPtr scenePointer, bool mode)
	{
		call_SetSunshaftModeDelegate(scenePointer, mode);
	}

	public void SetSunShaftStrength(UIntPtr scenePointer, float strength)
	{
		call_SetSunShaftStrengthDelegate(scenePointer, strength);
	}

	public void SetSunSize(UIntPtr scenePointer, float size)
	{
		call_SetSunSizeDelegate(scenePointer, size);
	}

	public void SetTargetExposure(UIntPtr scenePointer, float targetExposure)
	{
		call_SetTargetExposureDelegate(scenePointer, targetExposure);
	}

	public void SetTemperature(UIntPtr scenePointer, float temperature)
	{
		call_SetTemperatureDelegate(scenePointer, temperature);
	}

	public void SetTerrainDynamicParams(UIntPtr scenePointer, Vec3 dynamic_params)
	{
		call_SetTerrainDynamicParamsDelegate(scenePointer, dynamic_params);
	}

	public void SetTimeOfDay(UIntPtr scenePointer, float value)
	{
		call_SetTimeOfDayDelegate(scenePointer, value);
	}

	public void SetTimeSpeed(UIntPtr scenePointer, float value)
	{
		call_SetTimeSpeedDelegate(scenePointer, value);
	}

	public void SetUpgradeLevel(UIntPtr scenePointer, int level)
	{
		call_SetUpgradeLevelDelegate(scenePointer, level);
	}

	public void SetUpgradeLevelVisibility(UIntPtr scenePointer, string concatLevels)
	{
		byte[] array = null;
		if (concatLevels != null)
		{
			int byteCount = _utf8.GetByteCount(concatLevels);
			array = ((byteCount < 1024) ? CallbackStringBufferManager.StringBuffer0 : new byte[byteCount + 1]);
			_utf8.GetBytes(concatLevels, 0, concatLevels.Length, array, 0);
			array[byteCount] = 0;
		}
		call_SetUpgradeLevelVisibilityDelegate(scenePointer, array);
	}

	public void SetUpgradeLevelVisibilityWithMask(UIntPtr scenePointer, uint mask)
	{
		call_SetUpgradeLevelVisibilityWithMaskDelegate(scenePointer, mask);
	}

	public void SetUseAdvancedWaterRendering(UIntPtr scenePointer, bool value)
	{
		call_SetUseAdvancedWaterRenderingDelegate(scenePointer, value);
	}

	public void SetUseConstantTime(UIntPtr ptr, bool value)
	{
		call_SetUseConstantTimeDelegate(ptr, value);
	}

	public void SetUsesDeleteLaterSystem(UIntPtr scenePointer, bool value)
	{
		call_SetUsesDeleteLaterSystemDelegate(scenePointer, value);
	}

	public void SetVignetteInnerRadius(UIntPtr scenePointer, float vignetteInnerRadius)
	{
		call_SetVignetteInnerRadiusDelegate(scenePointer, vignetteInnerRadius);
	}

	public void SetVignetteOpacity(UIntPtr scenePointer, float vignetteOpacity)
	{
		call_SetVignetteOpacityDelegate(scenePointer, vignetteOpacity);
	}

	public void SetVignetteOuterRadius(UIntPtr scenePointer, float vignetteOuterRadius)
	{
		call_SetVignetteOuterRadiusDelegate(scenePointer, vignetteOuterRadius);
	}

	public void SetWaterStrength(Scene scene, float newWaterStrength)
	{
		UIntPtr scene2 = ((scene != null) ? scene.Pointer : UIntPtr.Zero);
		call_SetWaterStrengthDelegate(scene2, newWaterStrength);
	}

	public void SetWaterWakeCameraOffset(UIntPtr scenePointer, float cameraOffset)
	{
		call_SetWaterWakeCameraOffsetDelegate(scenePointer, cameraOffset);
	}

	public void SetWaterWakeWorldSize(UIntPtr scenePointer, float worldSize, float eraseFactor)
	{
		call_SetWaterWakeWorldSizeDelegate(scenePointer, worldSize, eraseFactor);
	}

	public void SetWinterTimeFactor(UIntPtr scenePointer, float winterTimeFactor)
	{
		call_SetWinterTimeFactorDelegate(scenePointer, winterTimeFactor);
	}

	public void StallLoadingRenderingsUntilFurtherNotice(UIntPtr scenePointer)
	{
		call_StallLoadingRenderingsUntilFurtherNoticeDelegate(scenePointer);
	}

	public bool SwapFaceConnectionsWithId(UIntPtr scenePointer, int hubFaceGroupID, int toBeSeparatedFaceGroupId, int toBeMergedFaceGroupId, bool canFail)
	{
		return call_SwapFaceConnectionsWithIdDelegate(scenePointer, hubFaceGroupID, toBeSeparatedFaceGroupId, toBeMergedFaceGroupId, canFail);
	}

	public string TakePhotoModePicture(Scene scene, bool saveAmbientOcclusionPass, bool saveObjectIdPass, bool saveShadowPass)
	{
		UIntPtr scene2 = ((scene != null) ? scene.Pointer : UIntPtr.Zero);
		if (call_TakePhotoModePictureDelegate(scene2, saveAmbientOcclusionPass, saveObjectIdPass, saveShadowPass) != 1)
		{
			return null;
		}
		return Managed.ReturnValueFromEngine;
	}

	public void Tick(UIntPtr scenePointer, float deltaTime)
	{
		call_TickDelegate(scenePointer, deltaTime);
	}

	public void TickWake(UIntPtr scenePointer, float dt)
	{
		call_TickWakeDelegate(scenePointer, dt);
	}

	public void WaitWaterRendererCPUSimulation(UIntPtr scenePointer)
	{
		call_WaitWaterRendererCPUSimulationDelegate(scenePointer);
	}

	public void WorldPositionComputeNearestNavMesh(ref WorldPosition position)
	{
		call_WorldPositionComputeNearestNavMeshDelegate(ref position);
	}

	public void WorldPositionValidateZ(ref WorldPosition position, int minimumValidityState)
	{
		call_WorldPositionValidateZDelegate(ref position, minimumValidityState);
	}

	bool IScene.HasNavmeshFaceUnsharedEdges(UIntPtr scenePointer, in PathFaceRecord faceRecord)
	{
		return HasNavmeshFaceUnsharedEdges(scenePointer, in faceRecord);
	}

	Vec3 IScene.GetWaterSpeedAtPosition(UIntPtr scenePointer, in Vec2 position, bool doChoppinessCorrection)
	{
		return GetWaterSpeedAtPosition(scenePointer, in position, doChoppinessCorrection);
	}

	void IScene.GetBulkWaterLevelAtVolumes(UIntPtr scene, UIntPtr volumeQueryDataArray, int volumeCount, in MatrixFrame entityFrame)
	{
		GetBulkWaterLevelAtVolumes(scene, volumeQueryDataArray, volumeCount, in entityFrame);
	}

	UIntPtr IScene.RegisterShipVisualToWaterRenderer(UIntPtr scenePointer, UIntPtr entityPointer, in Vec3 bb)
	{
		return RegisterShipVisualToWaterRenderer(scenePointer, entityPointer, in bb);
	}

	void IScene.SetGlobalWindStrengthVector(UIntPtr scenePointer, in Vec2 strengthVector)
	{
		SetGlobalWindStrengthVector(scenePointer, in strengthVector);
	}

	void IScene.SetGlobalWindVelocity(UIntPtr scenePointer, in Vec2 windVelocity)
	{
		SetGlobalWindVelocity(scenePointer, in windVelocity);
	}

	bool IScene.RayCastExcludingTwoEntities(BodyFlags flags, UIntPtr scenePointer, in Ray ray, UIntPtr entity1, UIntPtr entity2)
	{
		return RayCastExcludingTwoEntities(flags, scenePointer, in ray, entity1, entity2);
	}

	bool IScene.RayCastForClosestEntityOrTerrain(UIntPtr scenePointer, in Vec3 sourcePoint, in Vec3 targetPoint, float rayThickness, ref float collisionDistance, ref Vec3 closestPoint, ref UIntPtr entityIndex, BodyFlags bodyExcludeFlags, bool isFixedWorld)
	{
		return RayCastForClosestEntityOrTerrain(scenePointer, in sourcePoint, in targetPoint, rayThickness, ref collisionDistance, ref closestPoint, ref entityIndex, bodyExcludeFlags, isFixedWorld);
	}

	bool IScene.FocusRayCastForFixedPhysics(UIntPtr scenePointer, in Vec3 sourcePoint, in Vec3 targetPoint, float rayThickness, ref float collisionDistance, ref Vec3 closestPoint, ref UIntPtr entityIndex, BodyFlags bodyExcludeFlags, bool isFixedWorld)
	{
		return FocusRayCastForFixedPhysics(scenePointer, in sourcePoint, in targetPoint, rayThickness, ref collisionDistance, ref closestPoint, ref entityIndex, bodyExcludeFlags, isFixedWorld);
	}

	bool IScene.RayCastForRamming(UIntPtr scenePointer, in Vec3 sourcePoint, in Vec3 targetPoint, float rayThickness, ref float collisionDistance, ref Vec3 intersectionPoint, ref UIntPtr intersectedEntityIndex, BodyFlags bodyExcludeFlags, BodyFlags bodyIncludeFlags, UIntPtr ignoredEntityPointer)
	{
		return RayCastForRamming(scenePointer, in sourcePoint, in targetPoint, rayThickness, ref collisionDistance, ref intersectionPoint, ref intersectedEntityIndex, bodyExcludeFlags, bodyIncludeFlags, ignoredEntityPointer);
	}

	bool IScene.RayCastForClosestEntityOrTerrainIgnoreEntity(UIntPtr scenePointer, in Vec3 sourcePoint, in Vec3 targetPoint, float rayThickness, ref float collisionDistance, ref Vec3 closestPoint, ref UIntPtr entityIndex, BodyFlags bodyExcludeFlags, UIntPtr ignoredEntityPointer)
	{
		return RayCastForClosestEntityOrTerrainIgnoreEntity(scenePointer, in sourcePoint, in targetPoint, rayThickness, ref collisionDistance, ref closestPoint, ref entityIndex, bodyExcludeFlags, ignoredEntityPointer);
	}

	bool IScene.BoxCastOnlyForCamera(UIntPtr scenePointer, Vec3[] boxPoints, in Vec3 centerPoint, in Vec3 dir, float distance, UIntPtr ignoredEntityPointer, ref float collisionDistance, ref Vec3 closestPoint, ref UIntPtr entityPointer, BodyFlags bodyExcludeFlags)
	{
		return BoxCastOnlyForCamera(scenePointer, boxPoints, in centerPoint, in dir, distance, ignoredEntityPointer, ref collisionDistance, ref closestPoint, ref entityPointer, bodyExcludeFlags);
	}

	UIntPtr IScene.GetNavigationMeshForPosition(UIntPtr scenePointer, in Vec3 position, ref int faceGroupId, float heightDifferenceLimit, bool excludeDynamicNavigationMeshes)
	{
		return GetNavigationMeshForPosition(scenePointer, in position, ref faceGroupId, heightDifferenceLimit, excludeDynamicNavigationMeshes);
	}

	UIntPtr IScene.GetNearestNavigationMeshForPosition(UIntPtr scenePointer, in Vec3 position, float heightDifferenceLimit, bool excludeDynamicNavigationMeshes)
	{
		return GetNearestNavigationMeshForPosition(scenePointer, in position, heightDifferenceLimit, excludeDynamicNavigationMeshes);
	}

	Vec2 IScene.GetLastPositionOnNavMeshFaceForPointAndDirection(UIntPtr scenePointer, in PathFaceRecord record, Vec2 position, Vec2 direction)
	{
		return GetLastPositionOnNavMeshFaceForPointAndDirection(scenePointer, in record, position, direction);
	}
}
