using System;
using System.Runtime.InteropServices;
using System.Security;
using System.Text;
using TaleWorlds.DotNet;
using TaleWorlds.Engine;
using TaleWorlds.Library;

namespace ManagedCallbacks
{
	// Token: 0x02000011 RID: 17
	internal class ScriptingInterfaceOfIGameEntity : IGameEntity
	{
		// Token: 0x06000127 RID: 295 RVA: 0x0000FFCA File Offset: 0x0000E1CA
		public void ActivateRagdoll(UIntPtr entityId)
		{
			ScriptingInterfaceOfIGameEntity.call_ActivateRagdollDelegate(entityId);
		}

		// Token: 0x06000128 RID: 296 RVA: 0x0000FFD7 File Offset: 0x0000E1D7
		public void AddAllMeshesOfGameEntity(UIntPtr entityId, UIntPtr copiedEntityId)
		{
			ScriptingInterfaceOfIGameEntity.call_AddAllMeshesOfGameEntityDelegate(entityId, copiedEntityId);
		}

		// Token: 0x06000129 RID: 297 RVA: 0x0000FFE8 File Offset: 0x0000E1E8
		public void AddCapsuleAsBody(UIntPtr entityId, Vec3 p1, Vec3 p2, float radius, uint bodyFlags, string physicsMaterialName)
		{
			byte[] array = null;
			if (physicsMaterialName != null)
			{
				int byteCount = ScriptingInterfaceOfIGameEntity._utf8.GetByteCount(physicsMaterialName);
				array = ((byteCount < 1024) ? CallbackStringBufferManager.StringBuffer0 : new byte[byteCount + 1]);
				ScriptingInterfaceOfIGameEntity._utf8.GetBytes(physicsMaterialName, 0, physicsMaterialName.Length, array, 0);
				array[byteCount] = 0;
			}
			ScriptingInterfaceOfIGameEntity.call_AddCapsuleAsBodyDelegate(entityId, p1, p2, radius, bodyFlags, array);
		}

		// Token: 0x0600012A RID: 298 RVA: 0x0001004D File Offset: 0x0000E24D
		public void AddChild(UIntPtr parententity, UIntPtr childentity, bool autoLocalizeFrame)
		{
			ScriptingInterfaceOfIGameEntity.call_AddChildDelegate(parententity, childentity, autoLocalizeFrame);
		}

		// Token: 0x0600012B RID: 299 RVA: 0x0001005C File Offset: 0x0000E25C
		public void AddComponent(UIntPtr pointer, UIntPtr componentPointer)
		{
			ScriptingInterfaceOfIGameEntity.call_AddComponentDelegate(pointer, componentPointer);
		}

		// Token: 0x0600012C RID: 300 RVA: 0x0001006A File Offset: 0x0000E26A
		public UIntPtr AddDistanceJoint(UIntPtr entityId, UIntPtr otherEntityId, float minDistance, float maxDistance)
		{
			return ScriptingInterfaceOfIGameEntity.call_AddDistanceJointDelegate(entityId, otherEntityId, minDistance, maxDistance);
		}

		// Token: 0x0600012D RID: 301 RVA: 0x0001007B File Offset: 0x0000E27B
		public UIntPtr AddDistanceJointWithFrames(UIntPtr entityId, UIntPtr otherEntityId, MatrixFrame globalFrameOnA, MatrixFrame globalFrameOnB, float minDistance, float maxDistance)
		{
			return ScriptingInterfaceOfIGameEntity.call_AddDistanceJointWithFramesDelegate(entityId, otherEntityId, globalFrameOnA, globalFrameOnB, minDistance, maxDistance);
		}

		// Token: 0x0600012E RID: 302 RVA: 0x00010090 File Offset: 0x0000E290
		public void AddEditDataUserToAllMeshes(UIntPtr entityId, bool entity_components, bool skeleton_components)
		{
			ScriptingInterfaceOfIGameEntity.call_AddEditDataUserToAllMeshesDelegate(entityId, entity_components, skeleton_components);
		}

		// Token: 0x0600012F RID: 303 RVA: 0x0001009F File Offset: 0x0000E29F
		public bool AddLight(UIntPtr entityId, UIntPtr lightPointer)
		{
			return ScriptingInterfaceOfIGameEntity.call_AddLightDelegate(entityId, lightPointer);
		}

		// Token: 0x06000130 RID: 304 RVA: 0x000100AD File Offset: 0x0000E2AD
		public void AddMesh(UIntPtr entityId, UIntPtr mesh, bool recomputeBoundingBox)
		{
			ScriptingInterfaceOfIGameEntity.call_AddMeshDelegate(entityId, mesh, recomputeBoundingBox);
		}

		// Token: 0x06000131 RID: 305 RVA: 0x000100BC File Offset: 0x0000E2BC
		public void AddMeshToBone(UIntPtr entityId, UIntPtr multiMeshPointer, sbyte boneIndex)
		{
			ScriptingInterfaceOfIGameEntity.call_AddMeshToBoneDelegate(entityId, multiMeshPointer, boneIndex);
		}

		// Token: 0x06000132 RID: 306 RVA: 0x000100CB File Offset: 0x0000E2CB
		public void AddMultiMesh(UIntPtr entityId, UIntPtr multiMeshPtr, bool updateVisMask)
		{
			ScriptingInterfaceOfIGameEntity.call_AddMultiMeshDelegate(entityId, multiMeshPtr, updateVisMask);
		}

		// Token: 0x06000133 RID: 307 RVA: 0x000100DA File Offset: 0x0000E2DA
		public void AddMultiMeshToSkeleton(UIntPtr gameEntity, UIntPtr multiMesh)
		{
			ScriptingInterfaceOfIGameEntity.call_AddMultiMeshToSkeletonDelegate(gameEntity, multiMesh);
		}

		// Token: 0x06000134 RID: 308 RVA: 0x000100E8 File Offset: 0x0000E2E8
		public void AddMultiMeshToSkeletonBone(UIntPtr gameEntity, UIntPtr multiMesh, sbyte boneIndex)
		{
			ScriptingInterfaceOfIGameEntity.call_AddMultiMeshToSkeletonBoneDelegate(gameEntity, multiMesh, boneIndex);
		}

		// Token: 0x06000135 RID: 309 RVA: 0x000100F8 File Offset: 0x0000E2F8
		public void AddParticleSystemComponent(UIntPtr entityId, string particleid)
		{
			byte[] array = null;
			if (particleid != null)
			{
				int byteCount = ScriptingInterfaceOfIGameEntity._utf8.GetByteCount(particleid);
				array = ((byteCount < 1024) ? CallbackStringBufferManager.StringBuffer0 : new byte[byteCount + 1]);
				ScriptingInterfaceOfIGameEntity._utf8.GetBytes(particleid, 0, particleid.Length, array, 0);
				array[byteCount] = 0;
			}
			ScriptingInterfaceOfIGameEntity.call_AddParticleSystemComponentDelegate(entityId, array);
		}

		// Token: 0x06000136 RID: 310 RVA: 0x00010154 File Offset: 0x0000E354
		public void AddPhysics(UIntPtr entityId, UIntPtr body, float mass, ref Vec3 localCenterOfMass, ref Vec3 initialGlobalVelocity, ref Vec3 initialAngularGlobalVelocity, int physicsMaterial, bool isStatic, int collisionGroupID)
		{
			ScriptingInterfaceOfIGameEntity.call_AddPhysicsDelegate(entityId, body, mass, ref localCenterOfMass, ref initialGlobalVelocity, ref initialAngularGlobalVelocity, physicsMaterial, isStatic, collisionGroupID);
		}

		// Token: 0x06000137 RID: 311 RVA: 0x0001017A File Offset: 0x0000E37A
		public void AddSphereAsBody(UIntPtr entityId, Vec3 center, float radius, uint bodyFlags)
		{
			ScriptingInterfaceOfIGameEntity.call_AddSphereAsBodyDelegate(entityId, center, radius, bodyFlags);
		}

		// Token: 0x06000138 RID: 312 RVA: 0x0001018B File Offset: 0x0000E38B
		public void AddSplashPositionToWaterVisualRecord(UIntPtr entityPointer, UIntPtr visualPrefab, in Vec3 position)
		{
			ScriptingInterfaceOfIGameEntity.call_AddSplashPositionToWaterVisualRecordDelegate(entityPointer, visualPrefab, position);
		}

		// Token: 0x06000139 RID: 313 RVA: 0x0001019C File Offset: 0x0000E39C
		public void AddTag(UIntPtr entityId, string tag)
		{
			byte[] array = null;
			if (tag != null)
			{
				int byteCount = ScriptingInterfaceOfIGameEntity._utf8.GetByteCount(tag);
				array = ((byteCount < 1024) ? CallbackStringBufferManager.StringBuffer0 : new byte[byteCount + 1]);
				ScriptingInterfaceOfIGameEntity._utf8.GetBytes(tag, 0, tag.Length, array, 0);
				array[byteCount] = 0;
			}
			ScriptingInterfaceOfIGameEntity.call_AddTagDelegate(entityId, array);
		}

		// Token: 0x0600013A RID: 314 RVA: 0x000101F7 File Offset: 0x0000E3F7
		public void ApplyAccelerationToDynamicBody(UIntPtr entityId, ref Vec3 acceleration)
		{
			ScriptingInterfaceOfIGameEntity.call_ApplyAccelerationToDynamicBodyDelegate(entityId, ref acceleration);
		}

		// Token: 0x0600013B RID: 315 RVA: 0x00010205 File Offset: 0x0000E405
		public void ApplyForceToDynamicBody(UIntPtr entityId, ref Vec3 force, GameEntityPhysicsExtensions.ForceMode forceMode)
		{
			ScriptingInterfaceOfIGameEntity.call_ApplyForceToDynamicBodyDelegate(entityId, ref force, forceMode);
		}

		// Token: 0x0600013C RID: 316 RVA: 0x00010214 File Offset: 0x0000E414
		public void ApplyGlobalForceAtLocalPosToDynamicBody(UIntPtr entityId, ref Vec3 localPosition, ref Vec3 globalForce, GameEntityPhysicsExtensions.ForceMode forceMode)
		{
			ScriptingInterfaceOfIGameEntity.call_ApplyGlobalForceAtLocalPosToDynamicBodyDelegate(entityId, ref localPosition, ref globalForce, forceMode);
		}

		// Token: 0x0600013D RID: 317 RVA: 0x00010225 File Offset: 0x0000E425
		public void ApplyLocalForceAtLocalPosToDynamicBody(UIntPtr entityId, ref Vec3 localPosition, ref Vec3 localForce, GameEntityPhysicsExtensions.ForceMode forceMode)
		{
			ScriptingInterfaceOfIGameEntity.call_ApplyLocalForceAtLocalPosToDynamicBodyDelegate(entityId, ref localPosition, ref localForce, forceMode);
		}

		// Token: 0x0600013E RID: 318 RVA: 0x00010236 File Offset: 0x0000E436
		public void ApplyLocalImpulseToDynamicBody(UIntPtr entityId, ref Vec3 localPosition, ref Vec3 impulse)
		{
			ScriptingInterfaceOfIGameEntity.call_ApplyLocalImpulseToDynamicBodyDelegate(entityId, ref localPosition, ref impulse);
		}

		// Token: 0x0600013F RID: 319 RVA: 0x00010245 File Offset: 0x0000E445
		public void ApplyTorqueToDynamicBody(UIntPtr entityId, ref Vec3 torque, GameEntityPhysicsExtensions.ForceMode forceMode)
		{
			ScriptingInterfaceOfIGameEntity.call_ApplyTorqueToDynamicBodyDelegate(entityId, ref torque, forceMode);
		}

		// Token: 0x06000140 RID: 320 RVA: 0x00010254 File Offset: 0x0000E454
		public void AttachNavigationMeshFaces(UIntPtr entityId, int faceGroupId, bool isConnected, bool isBlocker, bool autoLocalize, bool finalizeBlockerConvexHullComputation, bool updateEntityFrame)
		{
			ScriptingInterfaceOfIGameEntity.call_AttachNavigationMeshFacesDelegate(entityId, faceGroupId, isConnected, isBlocker, autoLocalize, finalizeBlockerConvexHullComputation, updateEntityFrame);
		}

		// Token: 0x06000141 RID: 321 RVA: 0x0001026B File Offset: 0x0000E46B
		public void BreakPrefab(UIntPtr entityId)
		{
			ScriptingInterfaceOfIGameEntity.call_BreakPrefabDelegate(entityId);
		}

		// Token: 0x06000142 RID: 322 RVA: 0x00010278 File Offset: 0x0000E478
		public void BurstEntityParticle(UIntPtr entityId, bool doChildren)
		{
			ScriptingInterfaceOfIGameEntity.call_BurstEntityParticleDelegate(entityId, doChildren);
		}

		// Token: 0x06000143 RID: 323 RVA: 0x00010286 File Offset: 0x0000E486
		public void CallScriptCallbacks(UIntPtr entityPointer, bool registerScriptComponents)
		{
			ScriptingInterfaceOfIGameEntity.call_CallScriptCallbacksDelegate(entityPointer, registerScriptComponents);
		}

		// Token: 0x06000144 RID: 324 RVA: 0x00010294 File Offset: 0x0000E494
		public void ChangeMetaMeshOrRemoveItIfNotExists(UIntPtr entityId, UIntPtr entityMetaMeshPointer, UIntPtr newMetaMeshPointer)
		{
			ScriptingInterfaceOfIGameEntity.call_ChangeMetaMeshOrRemoveItIfNotExistsDelegate(entityId, entityMetaMeshPointer, newMetaMeshPointer);
		}

		// Token: 0x06000145 RID: 325 RVA: 0x000102A3 File Offset: 0x0000E4A3
		public void ChangeResolutionMultiplierOfWaterVisual(UIntPtr visualPrefab, float multiplier, in Vec3 waterEffectsBB)
		{
			ScriptingInterfaceOfIGameEntity.call_ChangeResolutionMultiplierOfWaterVisualDelegate(visualPrefab, multiplier, waterEffectsBB);
		}

		// Token: 0x06000146 RID: 326 RVA: 0x000102B2 File Offset: 0x0000E4B2
		public bool CheckIsPrefabLinkRootPrefab(UIntPtr entityPtr, int depth)
		{
			return ScriptingInterfaceOfIGameEntity.call_CheckIsPrefabLinkRootPrefabDelegate(entityPtr, depth);
		}

		// Token: 0x06000147 RID: 327 RVA: 0x000102C0 File Offset: 0x0000E4C0
		public bool CheckPointWithOrientedBoundingBox(UIntPtr entityId, Vec3 point)
		{
			return ScriptingInterfaceOfIGameEntity.call_CheckPointWithOrientedBoundingBoxDelegate(entityId, point);
		}

		// Token: 0x06000148 RID: 328 RVA: 0x000102CE File Offset: 0x0000E4CE
		public bool CheckResources(UIntPtr entityId, bool addToQueue, bool checkFaceResources)
		{
			return ScriptingInterfaceOfIGameEntity.call_CheckResourcesDelegate(entityId, addToQueue, checkFaceResources);
		}

		// Token: 0x06000149 RID: 329 RVA: 0x000102DD File Offset: 0x0000E4DD
		public void ClearComponents(UIntPtr entityId)
		{
			ScriptingInterfaceOfIGameEntity.call_ClearComponentsDelegate(entityId);
		}

		// Token: 0x0600014A RID: 330 RVA: 0x000102EA File Offset: 0x0000E4EA
		public void ClearEntityComponents(UIntPtr entityId, bool resetAll, bool removeScripts, bool deleteChildEntities)
		{
			ScriptingInterfaceOfIGameEntity.call_ClearEntityComponentsDelegate(entityId, resetAll, removeScripts, deleteChildEntities);
		}

		// Token: 0x0600014B RID: 331 RVA: 0x000102FB File Offset: 0x0000E4FB
		public void ClearOnlyOwnComponents(UIntPtr entityId)
		{
			ScriptingInterfaceOfIGameEntity.call_ClearOnlyOwnComponentsDelegate(entityId);
		}

		// Token: 0x0600014C RID: 332 RVA: 0x00010308 File Offset: 0x0000E508
		public void ComputeTrajectoryVolume(UIntPtr gameEntity, float missileSpeed, float verticalAngleMaxInDegrees, float verticalAngleMinInDegrees, float horizontalAngleRangeInDegrees, float airFrictionConstant)
		{
			ScriptingInterfaceOfIGameEntity.call_ComputeTrajectoryVolumeDelegate(gameEntity, missileSpeed, verticalAngleMaxInDegrees, verticalAngleMinInDegrees, horizontalAngleRangeInDegrees, airFrictionConstant);
		}

		// Token: 0x0600014D RID: 333 RVA: 0x0001031D File Offset: 0x0000E51D
		public void ComputeVelocityDeltaFromImpulse(UIntPtr entityPtr, in Vec3 impulsiveForce, in Vec3 impulsiveTorque, out Vec3 deltaLinearVelocity, out Vec3 deltaAngularVelocity)
		{
			ScriptingInterfaceOfIGameEntity.call_ComputeVelocityDeltaFromImpulseDelegate(entityPtr, impulsiveForce, impulsiveTorque, out deltaLinearVelocity, out deltaAngularVelocity);
		}

		// Token: 0x0600014E RID: 334 RVA: 0x00010330 File Offset: 0x0000E530
		public void ConvertDynamicBodyToRayCast(UIntPtr entityId)
		{
			ScriptingInterfaceOfIGameEntity.call_ConvertDynamicBodyToRayCastDelegate(entityId);
		}

		// Token: 0x0600014F RID: 335 RVA: 0x0001033D File Offset: 0x0000E53D
		public void CookTrianglePhysxMesh(UIntPtr cookingInstancePointer, UIntPtr shapePointer, UIntPtr quadPinnedPointer, int physicsMaterial, int numberOfVertices, UIntPtr indicesPinnedPointer, int numberOfIndices)
		{
			ScriptingInterfaceOfIGameEntity.call_CookTrianglePhysxMeshDelegate(cookingInstancePointer, shapePointer, quadPinnedPointer, physicsMaterial, numberOfVertices, indicesPinnedPointer, numberOfIndices);
		}

		// Token: 0x06000150 RID: 336 RVA: 0x00010354 File Offset: 0x0000E554
		public void CopyComponentsToSkeleton(UIntPtr entityId)
		{
			ScriptingInterfaceOfIGameEntity.call_CopyComponentsToSkeletonDelegate(entityId);
		}

		// Token: 0x06000151 RID: 337 RVA: 0x00010364 File Offset: 0x0000E564
		public GameEntity CopyFromPrefab(UIntPtr prefab)
		{
			NativeObjectPointer nativeObjectPointer = ScriptingInterfaceOfIGameEntity.call_CopyFromPrefabDelegate(prefab);
			GameEntity result = null;
			if (nativeObjectPointer.Pointer != UIntPtr.Zero)
			{
				result = new GameEntity(nativeObjectPointer.Pointer);
				LibraryApplicationInterface.IManaged.DecreaseReferenceCount(nativeObjectPointer.Pointer);
			}
			return result;
		}

		// Token: 0x06000152 RID: 338 RVA: 0x000103B0 File Offset: 0x0000E5B0
		public void CopyScriptComponentFromAnotherEntity(UIntPtr prefab, UIntPtr other_prefab, string script_name)
		{
			byte[] array = null;
			if (script_name != null)
			{
				int byteCount = ScriptingInterfaceOfIGameEntity._utf8.GetByteCount(script_name);
				array = ((byteCount < 1024) ? CallbackStringBufferManager.StringBuffer0 : new byte[byteCount + 1]);
				ScriptingInterfaceOfIGameEntity._utf8.GetBytes(script_name, 0, script_name.Length, array, 0);
				array[byteCount] = 0;
			}
			ScriptingInterfaceOfIGameEntity.call_CopyScriptComponentFromAnotherEntityDelegate(prefab, other_prefab, array);
		}

		// Token: 0x06000153 RID: 339 RVA: 0x0001040C File Offset: 0x0000E60C
		public void CreateAndAddScriptComponent(UIntPtr entityId, string name, bool callScriptCallbacks)
		{
			byte[] array = null;
			if (name != null)
			{
				int byteCount = ScriptingInterfaceOfIGameEntity._utf8.GetByteCount(name);
				array = ((byteCount < 1024) ? CallbackStringBufferManager.StringBuffer0 : new byte[byteCount + 1]);
				ScriptingInterfaceOfIGameEntity._utf8.GetBytes(name, 0, name.Length, array, 0);
				array[byteCount] = 0;
			}
			ScriptingInterfaceOfIGameEntity.call_CreateAndAddScriptComponentDelegate(entityId, array, callScriptCallbacks);
		}

		// Token: 0x06000154 RID: 340 RVA: 0x00010468 File Offset: 0x0000E668
		public GameEntity CreateEmpty(UIntPtr scenePointer, bool isModifiableFromEditor, UIntPtr entityId, bool createPhysics, bool callScriptCallbacks)
		{
			NativeObjectPointer nativeObjectPointer = ScriptingInterfaceOfIGameEntity.call_CreateEmptyDelegate(scenePointer, isModifiableFromEditor, entityId, createPhysics, callScriptCallbacks);
			GameEntity result = null;
			if (nativeObjectPointer.Pointer != UIntPtr.Zero)
			{
				result = new GameEntity(nativeObjectPointer.Pointer);
				LibraryApplicationInterface.IManaged.DecreaseReferenceCount(nativeObjectPointer.Pointer);
			}
			return result;
		}

		// Token: 0x06000155 RID: 341 RVA: 0x000104B8 File Offset: 0x0000E6B8
		public UIntPtr CreateEmptyPhysxShape(UIntPtr entityPointer, bool isVariable, int physxMaterialIndex)
		{
			return ScriptingInterfaceOfIGameEntity.call_CreateEmptyPhysxShapeDelegate(entityPointer, isVariable, physxMaterialIndex);
		}

		// Token: 0x06000156 RID: 342 RVA: 0x000104C8 File Offset: 0x0000E6C8
		public GameEntity CreateEmptyWithoutScene()
		{
			NativeObjectPointer nativeObjectPointer = ScriptingInterfaceOfIGameEntity.call_CreateEmptyWithoutSceneDelegate();
			GameEntity result = null;
			if (nativeObjectPointer.Pointer != UIntPtr.Zero)
			{
				result = new GameEntity(nativeObjectPointer.Pointer);
				LibraryApplicationInterface.IManaged.DecreaseReferenceCount(nativeObjectPointer.Pointer);
			}
			return result;
		}

		// Token: 0x06000157 RID: 343 RVA: 0x00010514 File Offset: 0x0000E714
		public GameEntity CreateFromPrefab(UIntPtr scenePointer, string prefabid, bool callScriptCallbacks, bool createPhysics, uint scriptInclusionHashTag)
		{
			byte[] array = null;
			if (prefabid != null)
			{
				int byteCount = ScriptingInterfaceOfIGameEntity._utf8.GetByteCount(prefabid);
				array = ((byteCount < 1024) ? CallbackStringBufferManager.StringBuffer0 : new byte[byteCount + 1]);
				ScriptingInterfaceOfIGameEntity._utf8.GetBytes(prefabid, 0, prefabid.Length, array, 0);
				array[byteCount] = 0;
			}
			NativeObjectPointer nativeObjectPointer = ScriptingInterfaceOfIGameEntity.call_CreateFromPrefabDelegate(scenePointer, array, callScriptCallbacks, createPhysics, scriptInclusionHashTag);
			GameEntity result = null;
			if (nativeObjectPointer.Pointer != UIntPtr.Zero)
			{
				result = new GameEntity(nativeObjectPointer.Pointer);
				LibraryApplicationInterface.IManaged.DecreaseReferenceCount(nativeObjectPointer.Pointer);
			}
			return result;
		}

		// Token: 0x06000158 RID: 344 RVA: 0x000105A8 File Offset: 0x0000E7A8
		public GameEntity CreateFromPrefabWithInitialFrame(UIntPtr scenePointer, string prefabid, ref MatrixFrame frame, bool callScriptCallbacks)
		{
			byte[] array = null;
			if (prefabid != null)
			{
				int byteCount = ScriptingInterfaceOfIGameEntity._utf8.GetByteCount(prefabid);
				array = ((byteCount < 1024) ? CallbackStringBufferManager.StringBuffer0 : new byte[byteCount + 1]);
				ScriptingInterfaceOfIGameEntity._utf8.GetBytes(prefabid, 0, prefabid.Length, array, 0);
				array[byteCount] = 0;
			}
			NativeObjectPointer nativeObjectPointer = ScriptingInterfaceOfIGameEntity.call_CreateFromPrefabWithInitialFrameDelegate(scenePointer, array, ref frame, callScriptCallbacks);
			GameEntity result = null;
			if (nativeObjectPointer.Pointer != UIntPtr.Zero)
			{
				result = new GameEntity(nativeObjectPointer.Pointer);
				LibraryApplicationInterface.IManaged.DecreaseReferenceCount(nativeObjectPointer.Pointer);
			}
			return result;
		}

		// Token: 0x06000159 RID: 345 RVA: 0x00010638 File Offset: 0x0000E838
		public UIntPtr CreatePhysxCookingInstance()
		{
			return ScriptingInterfaceOfIGameEntity.call_CreatePhysxCookingInstanceDelegate();
		}

		// Token: 0x0600015A RID: 346 RVA: 0x00010644 File Offset: 0x0000E844
		public void CreateVariableRatePhysics(UIntPtr entityId, bool forChildren)
		{
			ScriptingInterfaceOfIGameEntity.call_CreateVariableRatePhysicsDelegate(entityId, forChildren);
		}

		// Token: 0x0600015B RID: 347 RVA: 0x00010652 File Offset: 0x0000E852
		public void DeleteEmptyShape(UIntPtr entity, UIntPtr shape1, UIntPtr shape2)
		{
			ScriptingInterfaceOfIGameEntity.call_DeleteEmptyShapeDelegate(entity, shape1, shape2);
		}

		// Token: 0x0600015C RID: 348 RVA: 0x00010661 File Offset: 0x0000E861
		public void DeletePhysxCookingInstance(UIntPtr pointer)
		{
			ScriptingInterfaceOfIGameEntity.call_DeletePhysxCookingInstanceDelegate(pointer);
		}

		// Token: 0x0600015D RID: 349 RVA: 0x0001066E File Offset: 0x0000E86E
		public void DeRegisterWaterMeshMaterials(UIntPtr entityPointer, UIntPtr visualPrefab)
		{
			ScriptingInterfaceOfIGameEntity.call_DeRegisterWaterMeshMaterialsDelegate(entityPointer, visualPrefab);
		}

		// Token: 0x0600015E RID: 350 RVA: 0x0001067C File Offset: 0x0000E87C
		public void DeRegisterWaterSDFClip(UIntPtr entityId, int slot)
		{
			ScriptingInterfaceOfIGameEntity.call_DeRegisterWaterSDFClipDelegate(entityId, slot);
		}

		// Token: 0x0600015F RID: 351 RVA: 0x0001068A File Offset: 0x0000E88A
		public void DeselectEntityOnEditor(UIntPtr entityId)
		{
			ScriptingInterfaceOfIGameEntity.call_DeselectEntityOnEditorDelegate(entityId);
		}

		// Token: 0x06000160 RID: 352 RVA: 0x00010697 File Offset: 0x0000E897
		public void DetachAllAttachedNavigationMeshFaces(UIntPtr entityId)
		{
			ScriptingInterfaceOfIGameEntity.call_DetachAllAttachedNavigationMeshFacesDelegate(entityId);
		}

		// Token: 0x06000161 RID: 353 RVA: 0x000106A4 File Offset: 0x0000E8A4
		public void DisableContour(UIntPtr entityId)
		{
			ScriptingInterfaceOfIGameEntity.call_DisableContourDelegate(entityId);
		}

		// Token: 0x06000162 RID: 354 RVA: 0x000106B1 File Offset: 0x0000E8B1
		public void DisableDynamicBodySimulation(UIntPtr entityId)
		{
			ScriptingInterfaceOfIGameEntity.call_DisableDynamicBodySimulationDelegate(entityId);
		}

		// Token: 0x06000163 RID: 355 RVA: 0x000106BE File Offset: 0x0000E8BE
		public void DisableGravity(UIntPtr entityId)
		{
			ScriptingInterfaceOfIGameEntity.call_DisableGravityDelegate(entityId);
		}

		// Token: 0x06000164 RID: 356 RVA: 0x000106CB File Offset: 0x0000E8CB
		public void EnableDynamicBody(UIntPtr entityId)
		{
			ScriptingInterfaceOfIGameEntity.call_EnableDynamicBodyDelegate(entityId);
		}

		// Token: 0x06000165 RID: 357 RVA: 0x000106D8 File Offset: 0x0000E8D8
		public GameEntity FindWithName(UIntPtr scenePointer, string name)
		{
			byte[] array = null;
			if (name != null)
			{
				int byteCount = ScriptingInterfaceOfIGameEntity._utf8.GetByteCount(name);
				array = ((byteCount < 1024) ? CallbackStringBufferManager.StringBuffer0 : new byte[byteCount + 1]);
				ScriptingInterfaceOfIGameEntity._utf8.GetBytes(name, 0, name.Length, array, 0);
				array[byteCount] = 0;
			}
			NativeObjectPointer nativeObjectPointer = ScriptingInterfaceOfIGameEntity.call_FindWithNameDelegate(scenePointer, array);
			GameEntity result = null;
			if (nativeObjectPointer.Pointer != UIntPtr.Zero)
			{
				result = new GameEntity(nativeObjectPointer.Pointer);
				LibraryApplicationInterface.IManaged.DecreaseReferenceCount(nativeObjectPointer.Pointer);
			}
			return result;
		}

		// Token: 0x06000166 RID: 358 RVA: 0x00010765 File Offset: 0x0000E965
		public void Freeze(UIntPtr entityId, bool isFrozen)
		{
			ScriptingInterfaceOfIGameEntity.call_FreezeDelegate(entityId, isFrozen);
		}

		// Token: 0x06000167 RID: 359 RVA: 0x00010773 File Offset: 0x0000E973
		public Vec3 GetAngularVelocity(UIntPtr entityPtr)
		{
			return ScriptingInterfaceOfIGameEntity.call_GetAngularVelocityDelegate(entityPtr);
		}

		// Token: 0x06000168 RID: 360 RVA: 0x00010780 File Offset: 0x0000E980
		public int GetAttachedNavmeshFaceCount(UIntPtr entityId)
		{
			return ScriptingInterfaceOfIGameEntity.call_GetAttachedNavmeshFaceCountDelegate(entityId);
		}

		// Token: 0x06000169 RID: 361 RVA: 0x00010790 File Offset: 0x0000E990
		public void GetAttachedNavmeshFaceRecords(UIntPtr entityId, PathFaceRecord[] faceRecords)
		{
			PinnedArrayData<PathFaceRecord> pinnedArrayData = new PinnedArrayData<PathFaceRecord>(faceRecords, false);
			IntPtr pointer = pinnedArrayData.Pointer;
			ScriptingInterfaceOfIGameEntity.call_GetAttachedNavmeshFaceRecordsDelegate(entityId, pointer);
			pinnedArrayData.Dispose();
		}

		// Token: 0x0600016A RID: 362 RVA: 0x000107C4 File Offset: 0x0000E9C4
		public void GetAttachedNavmeshFaceVertexIndices(UIntPtr entityId, in PathFaceRecord faceRecord, int[] indices)
		{
			PinnedArrayData<int> pinnedArrayData = new PinnedArrayData<int>(indices, false);
			IntPtr pointer = pinnedArrayData.Pointer;
			ScriptingInterfaceOfIGameEntity.call_GetAttachedNavmeshFaceVertexIndicesDelegate(entityId, faceRecord, pointer);
			pinnedArrayData.Dispose();
		}

		// Token: 0x0600016B RID: 363 RVA: 0x000107F6 File Offset: 0x0000E9F6
		public uint GetBodyFlags(UIntPtr entityId)
		{
			return ScriptingInterfaceOfIGameEntity.call_GetBodyFlagsDelegate(entityId);
		}

		// Token: 0x0600016C RID: 364 RVA: 0x00010804 File Offset: 0x0000EA04
		public PhysicsShape GetBodyShape(UIntPtr entityId)
		{
			NativeObjectPointer nativeObjectPointer = ScriptingInterfaceOfIGameEntity.call_GetBodyShapeDelegate(entityId);
			PhysicsShape result = null;
			if (nativeObjectPointer.Pointer != UIntPtr.Zero)
			{
				result = new PhysicsShape(nativeObjectPointer.Pointer);
				LibraryApplicationInterface.IManaged.DecreaseReferenceCount(nativeObjectPointer.Pointer);
			}
			return result;
		}

		// Token: 0x0600016D RID: 365 RVA: 0x0001084E File Offset: 0x0000EA4E
		public void GetBodyVisualWorldTransform(UIntPtr entityPtr, out MatrixFrame frame)
		{
			ScriptingInterfaceOfIGameEntity.call_GetBodyVisualWorldTransformDelegate(entityPtr, out frame);
		}

		// Token: 0x0600016E RID: 366 RVA: 0x0001085C File Offset: 0x0000EA5C
		public void GetBodyWorldTransform(UIntPtr entityPtr, out MatrixFrame frame)
		{
			ScriptingInterfaceOfIGameEntity.call_GetBodyWorldTransformDelegate(entityPtr, out frame);
		}

		// Token: 0x0600016F RID: 367 RVA: 0x0001086A File Offset: 0x0000EA6A
		public sbyte GetBoneCount(UIntPtr entityId)
		{
			return ScriptingInterfaceOfIGameEntity.call_GetBoneCountDelegate(entityId);
		}

		// Token: 0x06000170 RID: 368 RVA: 0x00010877 File Offset: 0x0000EA77
		public void GetBoneEntitialFrameWithIndex(UIntPtr entityId, sbyte boneIndex, ref MatrixFrame outEntitialFrame)
		{
			ScriptingInterfaceOfIGameEntity.call_GetBoneEntitialFrameWithIndexDelegate(entityId, boneIndex, ref outEntitialFrame);
		}

		// Token: 0x06000171 RID: 369 RVA: 0x00010888 File Offset: 0x0000EA88
		public void GetBoneEntitialFrameWithName(UIntPtr entityId, string boneName, ref MatrixFrame outEntitialFrame)
		{
			byte[] array = null;
			if (boneName != null)
			{
				int byteCount = ScriptingInterfaceOfIGameEntity._utf8.GetByteCount(boneName);
				array = ((byteCount < 1024) ? CallbackStringBufferManager.StringBuffer0 : new byte[byteCount + 1]);
				ScriptingInterfaceOfIGameEntity._utf8.GetBytes(boneName, 0, boneName.Length, array, 0);
				array[byteCount] = 0;
			}
			ScriptingInterfaceOfIGameEntity.call_GetBoneEntitialFrameWithNameDelegate(entityId, array, ref outEntitialFrame);
		}

		// Token: 0x06000172 RID: 370 RVA: 0x000108E4 File Offset: 0x0000EAE4
		public Vec3 GetBoundingBoxMax(UIntPtr entityId)
		{
			return ScriptingInterfaceOfIGameEntity.call_GetBoundingBoxMaxDelegate(entityId);
		}

		// Token: 0x06000173 RID: 371 RVA: 0x000108F1 File Offset: 0x0000EAF1
		public Vec3 GetBoundingBoxMin(UIntPtr entityId)
		{
			return ScriptingInterfaceOfIGameEntity.call_GetBoundingBoxMinDelegate(entityId);
		}

		// Token: 0x06000174 RID: 372 RVA: 0x000108FE File Offset: 0x0000EAFE
		public void GetCameraParamsFromCameraScript(UIntPtr entityId, UIntPtr camPtr, ref Vec3 dof_params)
		{
			ScriptingInterfaceOfIGameEntity.call_GetCameraParamsFromCameraScriptDelegate(entityId, camPtr, ref dof_params);
		}

		// Token: 0x06000175 RID: 373 RVA: 0x0001090D File Offset: 0x0000EB0D
		public Vec3 GetCenterOfMass(UIntPtr entityId)
		{
			return ScriptingInterfaceOfIGameEntity.call_GetCenterOfMassDelegate(entityId);
		}

		// Token: 0x06000176 RID: 374 RVA: 0x0001091C File Offset: 0x0000EB1C
		public GameEntity GetChild(UIntPtr entityId, int childIndex)
		{
			NativeObjectPointer nativeObjectPointer = ScriptingInterfaceOfIGameEntity.call_GetChildDelegate(entityId, childIndex);
			GameEntity result = null;
			if (nativeObjectPointer.Pointer != UIntPtr.Zero)
			{
				result = new GameEntity(nativeObjectPointer.Pointer);
				LibraryApplicationInterface.IManaged.DecreaseReferenceCount(nativeObjectPointer.Pointer);
			}
			return result;
		}

		// Token: 0x06000177 RID: 375 RVA: 0x00010967 File Offset: 0x0000EB67
		public int GetChildCount(UIntPtr entityId)
		{
			return ScriptingInterfaceOfIGameEntity.call_GetChildCountDelegate(entityId);
		}

		// Token: 0x06000178 RID: 376 RVA: 0x00010974 File Offset: 0x0000EB74
		public UIntPtr GetChildPointer(UIntPtr entityId, int childIndex)
		{
			return ScriptingInterfaceOfIGameEntity.call_GetChildPointerDelegate(entityId, childIndex);
		}

		// Token: 0x06000179 RID: 377 RVA: 0x00010984 File Offset: 0x0000EB84
		public GameEntityComponent GetComponentAtIndex(UIntPtr entityId, GameEntity.ComponentType componentType, int index)
		{
			NativeObjectPointer nativeObjectPointer = ScriptingInterfaceOfIGameEntity.call_GetComponentAtIndexDelegate(entityId, componentType, index);
			GameEntityComponent result = NativeObject.CreateNativeObjectWrapper<GameEntityComponent>(nativeObjectPointer);
			if (nativeObjectPointer.Pointer != UIntPtr.Zero)
			{
				LibraryApplicationInterface.IManaged.DecreaseReferenceCount(nativeObjectPointer.Pointer);
			}
			return result;
		}

		// Token: 0x0600017A RID: 378 RVA: 0x000109C7 File Offset: 0x0000EBC7
		public int GetComponentCount(UIntPtr entityId, GameEntity.ComponentType componentType)
		{
			return ScriptingInterfaceOfIGameEntity.call_GetComponentCountDelegate(entityId, componentType);
		}

		// Token: 0x0600017B RID: 379 RVA: 0x000109D5 File Offset: 0x0000EBD5
		public bool GetEditModeLevelVisibility(UIntPtr entityId)
		{
			return ScriptingInterfaceOfIGameEntity.call_GetEditModeLevelVisibilityDelegate(entityId);
		}

		// Token: 0x0600017C RID: 380 RVA: 0x000109E2 File Offset: 0x0000EBE2
		public EntityFlags GetEntityFlags(UIntPtr entityId)
		{
			return ScriptingInterfaceOfIGameEntity.call_GetEntityFlagsDelegate(entityId);
		}

		// Token: 0x0600017D RID: 381 RVA: 0x000109EF File Offset: 0x0000EBEF
		public EntityVisibilityFlags GetEntityVisibilityFlags(UIntPtr entityId)
		{
			return ScriptingInterfaceOfIGameEntity.call_GetEntityVisibilityFlagsDelegate(entityId);
		}

		// Token: 0x0600017E RID: 382 RVA: 0x000109FC File Offset: 0x0000EBFC
		public uint GetFactorColor(UIntPtr entityId)
		{
			return ScriptingInterfaceOfIGameEntity.call_GetFactorColorDelegate(entityId);
		}

		// Token: 0x0600017F RID: 383 RVA: 0x00010A0C File Offset: 0x0000EC0C
		public UIntPtr GetFirstChildWithTagRecursive(UIntPtr entityPtr, string tag)
		{
			byte[] array = null;
			if (tag != null)
			{
				int byteCount = ScriptingInterfaceOfIGameEntity._utf8.GetByteCount(tag);
				array = ((byteCount < 1024) ? CallbackStringBufferManager.StringBuffer0 : new byte[byteCount + 1]);
				ScriptingInterfaceOfIGameEntity._utf8.GetBytes(tag, 0, tag.Length, array, 0);
				array[byteCount] = 0;
			}
			return ScriptingInterfaceOfIGameEntity.call_GetFirstChildWithTagRecursiveDelegate(entityPtr, array);
		}

		// Token: 0x06000180 RID: 384 RVA: 0x00010A68 File Offset: 0x0000EC68
		public UIntPtr GetFirstEntityWithTag(UIntPtr scenePointer, string tag)
		{
			byte[] array = null;
			if (tag != null)
			{
				int byteCount = ScriptingInterfaceOfIGameEntity._utf8.GetByteCount(tag);
				array = ((byteCount < 1024) ? CallbackStringBufferManager.StringBuffer0 : new byte[byteCount + 1]);
				ScriptingInterfaceOfIGameEntity._utf8.GetBytes(tag, 0, tag.Length, array, 0);
				array[byteCount] = 0;
			}
			return ScriptingInterfaceOfIGameEntity.call_GetFirstEntityWithTagDelegate(scenePointer, array);
		}

		// Token: 0x06000181 RID: 385 RVA: 0x00010AC4 File Offset: 0x0000ECC4
		public UIntPtr GetFirstEntityWithTagExpression(UIntPtr scenePointer, string tagExpression)
		{
			byte[] array = null;
			if (tagExpression != null)
			{
				int byteCount = ScriptingInterfaceOfIGameEntity._utf8.GetByteCount(tagExpression);
				array = ((byteCount < 1024) ? CallbackStringBufferManager.StringBuffer0 : new byte[byteCount + 1]);
				ScriptingInterfaceOfIGameEntity._utf8.GetBytes(tagExpression, 0, tagExpression.Length, array, 0);
				array[byteCount] = 0;
			}
			return ScriptingInterfaceOfIGameEntity.call_GetFirstEntityWithTagExpressionDelegate(scenePointer, array);
		}

		// Token: 0x06000182 RID: 386 RVA: 0x00010B20 File Offset: 0x0000ED20
		public Mesh GetFirstMesh(UIntPtr entityId)
		{
			NativeObjectPointer nativeObjectPointer = ScriptingInterfaceOfIGameEntity.call_GetFirstMeshDelegate(entityId);
			Mesh result = null;
			if (nativeObjectPointer.Pointer != UIntPtr.Zero)
			{
				result = new Mesh(nativeObjectPointer.Pointer);
				LibraryApplicationInterface.IManaged.DecreaseReferenceCount(nativeObjectPointer.Pointer);
			}
			return result;
		}

		// Token: 0x06000183 RID: 387 RVA: 0x00010B6A File Offset: 0x0000ED6A
		public BoundingBox GetGlobalBoundingBox(UIntPtr entityId)
		{
			return ScriptingInterfaceOfIGameEntity.call_GetGlobalBoundingBoxDelegate(entityId);
		}

		// Token: 0x06000184 RID: 388 RVA: 0x00010B77 File Offset: 0x0000ED77
		public Vec3 GetGlobalBoxMax(UIntPtr entityId)
		{
			return ScriptingInterfaceOfIGameEntity.call_GetGlobalBoxMaxDelegate(entityId);
		}

		// Token: 0x06000185 RID: 389 RVA: 0x00010B84 File Offset: 0x0000ED84
		public Vec3 GetGlobalBoxMin(UIntPtr entityId)
		{
			return ScriptingInterfaceOfIGameEntity.call_GetGlobalBoxMinDelegate(entityId);
		}

		// Token: 0x06000186 RID: 390 RVA: 0x00010B91 File Offset: 0x0000ED91
		public void GetGlobalFrame(UIntPtr meshPointer, out MatrixFrame outFrame)
		{
			ScriptingInterfaceOfIGameEntity.call_GetGlobalFrameDelegate(meshPointer, out outFrame);
		}

		// Token: 0x06000187 RID: 391 RVA: 0x00010B9F File Offset: 0x0000ED9F
		public void GetGlobalFrameImpreciseForFixedTick(UIntPtr entityId, out MatrixFrame outFrame)
		{
			ScriptingInterfaceOfIGameEntity.call_GetGlobalFrameImpreciseForFixedTickDelegate(entityId, out outFrame);
		}

		// Token: 0x06000188 RID: 392 RVA: 0x00010BAD File Offset: 0x0000EDAD
		public Vec3 GetGlobalScale(UIntPtr pointer)
		{
			return ScriptingInterfaceOfIGameEntity.call_GetGlobalScaleDelegate(pointer);
		}

		// Token: 0x06000189 RID: 393 RVA: 0x00010BBA File Offset: 0x0000EDBA
		public Vec2 GetGlobalWindStrengthVectorOfScene(UIntPtr entityPtr)
		{
			return ScriptingInterfaceOfIGameEntity.call_GetGlobalWindStrengthVectorOfSceneDelegate(entityPtr);
		}

		// Token: 0x0600018A RID: 394 RVA: 0x00010BC7 File Offset: 0x0000EDC7
		public Vec2 GetGlobalWindVelocityOfScene(UIntPtr entityPtr)
		{
			return ScriptingInterfaceOfIGameEntity.call_GetGlobalWindVelocityOfSceneDelegate(entityPtr);
		}

		// Token: 0x0600018B RID: 395 RVA: 0x00010BD4 File Offset: 0x0000EDD4
		public Vec2 GetGlobalWindVelocityWithGustNoiseOfScene(UIntPtr entityPtr, float globalTime)
		{
			return ScriptingInterfaceOfIGameEntity.call_GetGlobalWindVelocityWithGustNoiseOfSceneDelegate(entityPtr, globalTime);
		}

		// Token: 0x0600018C RID: 396 RVA: 0x00010BE2 File Offset: 0x0000EDE2
		public string GetGuid(UIntPtr entityId)
		{
			if (ScriptingInterfaceOfIGameEntity.call_GetGuidDelegate(entityId) != 1)
			{
				return null;
			}
			return Managed.ReturnValueFromEngine;
		}

		// Token: 0x0600018D RID: 397 RVA: 0x00010BF9 File Offset: 0x0000EDF9
		public Vec3 GetLastFinalRenderCameraPositionOfScene(UIntPtr entityPtr)
		{
			return ScriptingInterfaceOfIGameEntity.call_GetLastFinalRenderCameraPositionOfSceneDelegate(entityPtr);
		}

		// Token: 0x0600018E RID: 398 RVA: 0x00010C08 File Offset: 0x0000EE08
		public Light GetLight(UIntPtr entityId)
		{
			NativeObjectPointer nativeObjectPointer = ScriptingInterfaceOfIGameEntity.call_GetLightDelegate(entityId);
			Light result = null;
			if (nativeObjectPointer.Pointer != UIntPtr.Zero)
			{
				result = new Light(nativeObjectPointer.Pointer);
				LibraryApplicationInterface.IManaged.DecreaseReferenceCount(nativeObjectPointer.Pointer);
			}
			return result;
		}

		// Token: 0x0600018F RID: 399 RVA: 0x00010C52 File Offset: 0x0000EE52
		public Vec3 GetLinearVelocity(UIntPtr entityPtr)
		{
			return ScriptingInterfaceOfIGameEntity.call_GetLinearVelocityDelegate(entityPtr);
		}

		// Token: 0x06000190 RID: 400 RVA: 0x00010C5F File Offset: 0x0000EE5F
		public BoundingBox GetLocalBoundingBox(UIntPtr entityId)
		{
			return ScriptingInterfaceOfIGameEntity.call_GetLocalBoundingBoxDelegate(entityId);
		}

		// Token: 0x06000191 RID: 401 RVA: 0x00010C6C File Offset: 0x0000EE6C
		public void GetLocalFrame(UIntPtr entityId, out MatrixFrame outFrame)
		{
			ScriptingInterfaceOfIGameEntity.call_GetLocalFrameDelegate(entityId, out outFrame);
		}

		// Token: 0x06000192 RID: 402 RVA: 0x00010C7A File Offset: 0x0000EE7A
		public void GetLocalPhysicsBoundingBox(UIntPtr entityId, bool includeChildren, out BoundingBox outBoundingBox)
		{
			ScriptingInterfaceOfIGameEntity.call_GetLocalPhysicsBoundingBoxDelegate(entityId, includeChildren, out outBoundingBox);
		}

		// Token: 0x06000193 RID: 403 RVA: 0x00010C89 File Offset: 0x0000EE89
		public float GetLodLevelForDistanceSq(UIntPtr entityId, float distanceSquared)
		{
			return ScriptingInterfaceOfIGameEntity.call_GetLodLevelForDistanceSqDelegate(entityId, distanceSquared);
		}

		// Token: 0x06000194 RID: 404 RVA: 0x00010C97 File Offset: 0x0000EE97
		public float GetMass(UIntPtr entityId)
		{
			return ScriptingInterfaceOfIGameEntity.call_GetMassDelegate(entityId);
		}

		// Token: 0x06000195 RID: 405 RVA: 0x00010CA4 File Offset: 0x0000EEA4
		public Vec3 GetMassSpaceInertia(UIntPtr entityId)
		{
			return ScriptingInterfaceOfIGameEntity.call_GetMassSpaceInertiaDelegate(entityId);
		}

		// Token: 0x06000196 RID: 406 RVA: 0x00010CB1 File Offset: 0x0000EEB1
		public Vec3 GetMassSpaceInverseInertia(UIntPtr entityId)
		{
			return ScriptingInterfaceOfIGameEntity.call_GetMassSpaceInverseInertiaDelegate(entityId);
		}

		// Token: 0x06000197 RID: 407 RVA: 0x00010CBE File Offset: 0x0000EEBE
		public void GetMeshBendedPosition(UIntPtr entityId, ref MatrixFrame worldSpacePosition, ref MatrixFrame output)
		{
			ScriptingInterfaceOfIGameEntity.call_GetMeshBendedPositionDelegate(entityId, ref worldSpacePosition, ref output);
		}

		// Token: 0x06000198 RID: 408 RVA: 0x00010CCD File Offset: 0x0000EECD
		public GameEntity.Mobility GetMobility(UIntPtr entityId)
		{
			return ScriptingInterfaceOfIGameEntity.call_GetMobilityDelegate(entityId);
		}

		// Token: 0x06000199 RID: 409 RVA: 0x00010CDA File Offset: 0x0000EEDA
		public string GetName(UIntPtr entityId)
		{
			if (ScriptingInterfaceOfIGameEntity.call_GetNameDelegate(entityId) != 1)
			{
				return null;
			}
			return Managed.ReturnValueFromEngine;
		}

		// Token: 0x0600019A RID: 410 RVA: 0x00010CF4 File Offset: 0x0000EEF4
		public void GetNativeScriptComponentVariable(UIntPtr entityPtr, string className, string fieldName, ref ScriptComponentFieldHolder data, RglScriptFieldType variableType)
		{
			byte[] array = null;
			if (className != null)
			{
				int byteCount = ScriptingInterfaceOfIGameEntity._utf8.GetByteCount(className);
				array = ((byteCount < 1024) ? CallbackStringBufferManager.StringBuffer0 : new byte[byteCount + 1]);
				ScriptingInterfaceOfIGameEntity._utf8.GetBytes(className, 0, className.Length, array, 0);
				array[byteCount] = 0;
			}
			byte[] array2 = null;
			if (fieldName != null)
			{
				int byteCount2 = ScriptingInterfaceOfIGameEntity._utf8.GetByteCount(fieldName);
				array2 = ((byteCount2 < 1024) ? CallbackStringBufferManager.StringBuffer1 : new byte[byteCount2 + 1]);
				ScriptingInterfaceOfIGameEntity._utf8.GetBytes(fieldName, 0, fieldName.Length, array2, 0);
				array2[byteCount2] = 0;
			}
			ScriptingInterfaceOfIGameEntity.call_GetNativeScriptComponentVariableDelegate(entityPtr, array, array2, ref data, variableType);
		}

		// Token: 0x0600019B RID: 411 RVA: 0x00010D98 File Offset: 0x0000EF98
		public UIntPtr GetNextEntityWithTag(UIntPtr currententityId, string tag)
		{
			byte[] array = null;
			if (tag != null)
			{
				int byteCount = ScriptingInterfaceOfIGameEntity._utf8.GetByteCount(tag);
				array = ((byteCount < 1024) ? CallbackStringBufferManager.StringBuffer0 : new byte[byteCount + 1]);
				ScriptingInterfaceOfIGameEntity._utf8.GetBytes(tag, 0, tag.Length, array, 0);
				array[byteCount] = 0;
			}
			return ScriptingInterfaceOfIGameEntity.call_GetNextEntityWithTagDelegate(currententityId, array);
		}

		// Token: 0x0600019C RID: 412 RVA: 0x00010DF4 File Offset: 0x0000EFF4
		public UIntPtr GetNextEntityWithTagExpression(UIntPtr currententityId, string tagExpression)
		{
			byte[] array = null;
			if (tagExpression != null)
			{
				int byteCount = ScriptingInterfaceOfIGameEntity._utf8.GetByteCount(tagExpression);
				array = ((byteCount < 1024) ? CallbackStringBufferManager.StringBuffer0 : new byte[byteCount + 1]);
				ScriptingInterfaceOfIGameEntity._utf8.GetBytes(tagExpression, 0, tagExpression.Length, array, 0);
				array[byteCount] = 0;
			}
			return ScriptingInterfaceOfIGameEntity.call_GetNextEntityWithTagExpressionDelegate(currententityId, array);
		}

		// Token: 0x0600019D RID: 413 RVA: 0x00010E50 File Offset: 0x0000F050
		public GameEntity GetNextPrefab(UIntPtr currentPrefab)
		{
			NativeObjectPointer nativeObjectPointer = ScriptingInterfaceOfIGameEntity.call_GetNextPrefabDelegate(currentPrefab);
			GameEntity result = null;
			if (nativeObjectPointer.Pointer != UIntPtr.Zero)
			{
				result = new GameEntity(nativeObjectPointer.Pointer);
				LibraryApplicationInterface.IManaged.DecreaseReferenceCount(nativeObjectPointer.Pointer);
			}
			return result;
		}

		// Token: 0x0600019E RID: 414 RVA: 0x00010E9A File Offset: 0x0000F09A
		public string GetOldPrefabName(UIntPtr prefab)
		{
			if (ScriptingInterfaceOfIGameEntity.call_GetOldPrefabNameDelegate(prefab) != 1)
			{
				return null;
			}
			return Managed.ReturnValueFromEngine;
		}

		// Token: 0x0600019F RID: 415 RVA: 0x00010EB4 File Offset: 0x0000F0B4
		public GameEntity GetParent(UIntPtr entityId)
		{
			NativeObjectPointer nativeObjectPointer = ScriptingInterfaceOfIGameEntity.call_GetParentDelegate(entityId);
			GameEntity result = null;
			if (nativeObjectPointer.Pointer != UIntPtr.Zero)
			{
				result = new GameEntity(nativeObjectPointer.Pointer);
				LibraryApplicationInterface.IManaged.DecreaseReferenceCount(nativeObjectPointer.Pointer);
			}
			return result;
		}

		// Token: 0x060001A0 RID: 416 RVA: 0x00010EFE File Offset: 0x0000F0FE
		public UIntPtr GetParentPointer(UIntPtr entityId)
		{
			return ScriptingInterfaceOfIGameEntity.call_GetParentPointerDelegate(entityId);
		}

		// Token: 0x060001A1 RID: 417 RVA: 0x00010F0B File Offset: 0x0000F10B
		public uint GetPhysicsDescBodyFlags(UIntPtr entityId)
		{
			return ScriptingInterfaceOfIGameEntity.call_GetPhysicsDescBodyFlagsDelegate(entityId);
		}

		// Token: 0x060001A2 RID: 418 RVA: 0x00010F18 File Offset: 0x0000F118
		public int GetPhysicsMaterialIndex(UIntPtr entityId)
		{
			return ScriptingInterfaceOfIGameEntity.call_GetPhysicsMaterialIndexDelegate(entityId);
		}

		// Token: 0x060001A3 RID: 419 RVA: 0x00010F25 File Offset: 0x0000F125
		public void GetPhysicsMinMax(UIntPtr entityId, bool includeChildren, ref Vec3 bbmin, ref Vec3 bbmax, bool returnLocal)
		{
			ScriptingInterfaceOfIGameEntity.call_GetPhysicsMinMaxDelegate(entityId, includeChildren, ref bbmin, ref bbmax, returnLocal);
		}

		// Token: 0x060001A4 RID: 420 RVA: 0x00010F38 File Offset: 0x0000F138
		public bool GetPhysicsState(UIntPtr entityId)
		{
			return ScriptingInterfaceOfIGameEntity.call_GetPhysicsStateDelegate(entityId);
		}

		// Token: 0x060001A5 RID: 421 RVA: 0x00010F45 File Offset: 0x0000F145
		public int GetPhysicsTriangleCount(UIntPtr entityId)
		{
			return ScriptingInterfaceOfIGameEntity.call_GetPhysicsTriangleCountDelegate(entityId);
		}

		// Token: 0x060001A6 RID: 422 RVA: 0x00010F52 File Offset: 0x0000F152
		public string GetPrefabName(UIntPtr prefab)
		{
			if (ScriptingInterfaceOfIGameEntity.call_GetPrefabNameDelegate(prefab) != 1)
			{
				return null;
			}
			return Managed.ReturnValueFromEngine;
		}

		// Token: 0x060001A7 RID: 423 RVA: 0x00010F69 File Offset: 0x0000F169
		public void GetPreviousGlobalFrame(UIntPtr entityPtr, out MatrixFrame frame)
		{
			ScriptingInterfaceOfIGameEntity.call_GetPreviousGlobalFrameDelegate(entityPtr, out frame);
		}

		// Token: 0x060001A8 RID: 424 RVA: 0x00010F77 File Offset: 0x0000F177
		public void GetQuickBoneEntitialFrame(UIntPtr entityId, sbyte index, out MatrixFrame frame)
		{
			ScriptingInterfaceOfIGameEntity.call_GetQuickBoneEntitialFrameDelegate(entityId, index, out frame);
		}

		// Token: 0x060001A9 RID: 425 RVA: 0x00010F86 File Offset: 0x0000F186
		public float GetRadius(UIntPtr entityId)
		{
			return ScriptingInterfaceOfIGameEntity.call_GetRadiusDelegate(entityId);
		}

		// Token: 0x060001AA RID: 426 RVA: 0x00010F93 File Offset: 0x0000F193
		public UIntPtr GetRootParentPointer(UIntPtr entityId)
		{
			return ScriptingInterfaceOfIGameEntity.call_GetRootParentPointerDelegate(entityId);
		}

		// Token: 0x060001AB RID: 427 RVA: 0x00010FA0 File Offset: 0x0000F1A0
		public Scene GetScene(UIntPtr entityId)
		{
			NativeObjectPointer nativeObjectPointer = ScriptingInterfaceOfIGameEntity.call_GetSceneDelegate(entityId);
			Scene result = null;
			if (nativeObjectPointer.Pointer != UIntPtr.Zero)
			{
				result = new Scene(nativeObjectPointer.Pointer);
				LibraryApplicationInterface.IManaged.DecreaseReferenceCount(nativeObjectPointer.Pointer);
			}
			return result;
		}

		// Token: 0x060001AC RID: 428 RVA: 0x00010FEA File Offset: 0x0000F1EA
		public UIntPtr GetScenePointer(UIntPtr entityId)
		{
			return ScriptingInterfaceOfIGameEntity.call_GetScenePointerDelegate(entityId);
		}

		// Token: 0x060001AD RID: 429 RVA: 0x00010FF7 File Offset: 0x0000F1F7
		public ScriptComponentBehavior GetScriptComponent(UIntPtr entityId)
		{
			return DotNetObject.GetManagedObjectWithId(ScriptingInterfaceOfIGameEntity.call_GetScriptComponentDelegate(entityId)) as ScriptComponentBehavior;
		}

		// Token: 0x060001AE RID: 430 RVA: 0x0001100E File Offset: 0x0000F20E
		public ScriptComponentBehavior GetScriptComponentAtIndex(UIntPtr entityId, int index)
		{
			return DotNetObject.GetManagedObjectWithId(ScriptingInterfaceOfIGameEntity.call_GetScriptComponentAtIndexDelegate(entityId, index)) as ScriptComponentBehavior;
		}

		// Token: 0x060001AF RID: 431 RVA: 0x00011026 File Offset: 0x0000F226
		public int GetScriptComponentCount(UIntPtr entityId)
		{
			return ScriptingInterfaceOfIGameEntity.call_GetScriptComponentCountDelegate(entityId);
		}

		// Token: 0x060001B0 RID: 432 RVA: 0x00011033 File Offset: 0x0000F233
		public int GetScriptComponentIndex(UIntPtr entityId, uint nameHash)
		{
			return ScriptingInterfaceOfIGameEntity.call_GetScriptComponentIndexDelegate(entityId, nameHash);
		}

		// Token: 0x060001B1 RID: 433 RVA: 0x00011044 File Offset: 0x0000F244
		public Skeleton GetSkeleton(UIntPtr entityId)
		{
			NativeObjectPointer nativeObjectPointer = ScriptingInterfaceOfIGameEntity.call_GetSkeletonDelegate(entityId);
			Skeleton result = null;
			if (nativeObjectPointer.Pointer != UIntPtr.Zero)
			{
				result = new Skeleton(nativeObjectPointer.Pointer);
				LibraryApplicationInterface.IManaged.DecreaseReferenceCount(nativeObjectPointer.Pointer);
			}
			return result;
		}

		// Token: 0x060001B2 RID: 434 RVA: 0x0001108E File Offset: 0x0000F28E
		public string GetTags(UIntPtr entityId)
		{
			if (ScriptingInterfaceOfIGameEntity.call_GetTagsDelegate(entityId) != 1)
			{
				return null;
			}
			return Managed.ReturnValueFromEngine;
		}

		// Token: 0x060001B3 RID: 435 RVA: 0x000110A5 File Offset: 0x0000F2A5
		public uint GetUpgradeLevelMask(UIntPtr prefab)
		{
			return ScriptingInterfaceOfIGameEntity.call_GetUpgradeLevelMaskDelegate(prefab);
		}

		// Token: 0x060001B4 RID: 436 RVA: 0x000110B2 File Offset: 0x0000F2B2
		public uint GetUpgradeLevelMaskCumulative(UIntPtr prefab)
		{
			return ScriptingInterfaceOfIGameEntity.call_GetUpgradeLevelMaskCumulativeDelegate(prefab);
		}

		// Token: 0x060001B5 RID: 437 RVA: 0x000110BF File Offset: 0x0000F2BF
		public bool GetVisibilityExcludeParents(UIntPtr entityId)
		{
			return ScriptingInterfaceOfIGameEntity.call_GetVisibilityExcludeParentsDelegate(entityId);
		}

		// Token: 0x060001B6 RID: 438 RVA: 0x000110CC File Offset: 0x0000F2CC
		public uint GetVisibilityLevelMaskIncludingParents(UIntPtr entityId)
		{
			return ScriptingInterfaceOfIGameEntity.call_GetVisibilityLevelMaskIncludingParentsDelegate(entityId);
		}

		// Token: 0x060001B7 RID: 439 RVA: 0x000110D9 File Offset: 0x0000F2D9
		public float GetWaterLevelAtPosition(UIntPtr entityId, in Vec2 position, bool useWaterRenderer, bool checkWaterBodyEntities)
		{
			return ScriptingInterfaceOfIGameEntity.call_GetWaterLevelAtPositionDelegate(entityId, position, useWaterRenderer, checkWaterBodyEntities);
		}

		// Token: 0x060001B8 RID: 440 RVA: 0x000110EA File Offset: 0x0000F2EA
		public bool HasBatchedKinematicPhysicsFlag(UIntPtr entityId)
		{
			return ScriptingInterfaceOfIGameEntity.call_HasBatchedKinematicPhysicsFlagDelegate(entityId);
		}

		// Token: 0x060001B9 RID: 441 RVA: 0x000110F7 File Offset: 0x0000F2F7
		public bool HasBatchedRayCastPhysicsFlag(UIntPtr entityId)
		{
			return ScriptingInterfaceOfIGameEntity.call_HasBatchedRayCastPhysicsFlagDelegate(entityId);
		}

		// Token: 0x060001BA RID: 442 RVA: 0x00011104 File Offset: 0x0000F304
		public bool HasBody(UIntPtr entityId)
		{
			return ScriptingInterfaceOfIGameEntity.call_HasBodyDelegate(entityId);
		}

		// Token: 0x060001BB RID: 443 RVA: 0x00011111 File Offset: 0x0000F311
		public bool HasComplexAnimTree(UIntPtr entityId)
		{
			return ScriptingInterfaceOfIGameEntity.call_HasComplexAnimTreeDelegate(entityId);
		}

		// Token: 0x060001BC RID: 444 RVA: 0x0001111E File Offset: 0x0000F31E
		public bool HasComponent(UIntPtr pointer, UIntPtr componentPointer)
		{
			return ScriptingInterfaceOfIGameEntity.call_HasComponentDelegate(pointer, componentPointer);
		}

		// Token: 0x060001BD RID: 445 RVA: 0x0001112C File Offset: 0x0000F32C
		public bool HasDynamicRigidBody(UIntPtr entityId)
		{
			return ScriptingInterfaceOfIGameEntity.call_HasDynamicRigidBodyDelegate(entityId);
		}

		// Token: 0x060001BE RID: 446 RVA: 0x00011139 File Offset: 0x0000F339
		public bool HasDynamicRigidBodyAndActiveSimulation(UIntPtr entityId)
		{
			return ScriptingInterfaceOfIGameEntity.call_HasDynamicRigidBodyAndActiveSimulationDelegate(entityId);
		}

		// Token: 0x060001BF RID: 447 RVA: 0x00011146 File Offset: 0x0000F346
		public bool HasFrameChanged(UIntPtr entityId)
		{
			return ScriptingInterfaceOfIGameEntity.call_HasFrameChangedDelegate(entityId);
		}

		// Token: 0x060001C0 RID: 448 RVA: 0x00011153 File Offset: 0x0000F353
		public bool HasKinematicRigidBody(UIntPtr entityId)
		{
			return ScriptingInterfaceOfIGameEntity.call_HasKinematicRigidBodyDelegate(entityId);
		}

		// Token: 0x060001C1 RID: 449 RVA: 0x00011160 File Offset: 0x0000F360
		public bool HasPhysicsBody(UIntPtr entityId)
		{
			return ScriptingInterfaceOfIGameEntity.call_HasPhysicsBodyDelegate(entityId);
		}

		// Token: 0x060001C2 RID: 450 RVA: 0x0001116D File Offset: 0x0000F36D
		public bool HasPhysicsDefinition(UIntPtr entityId, int excludeFlags)
		{
			return ScriptingInterfaceOfIGameEntity.call_HasPhysicsDefinitionDelegate(entityId, excludeFlags);
		}

		// Token: 0x060001C3 RID: 451 RVA: 0x0001117B File Offset: 0x0000F37B
		public bool HasScene(UIntPtr entityId)
		{
			return ScriptingInterfaceOfIGameEntity.call_HasSceneDelegate(entityId);
		}

		// Token: 0x060001C4 RID: 452 RVA: 0x00011188 File Offset: 0x0000F388
		public bool HasScriptComponent(UIntPtr entityId, string scName)
		{
			byte[] array = null;
			if (scName != null)
			{
				int byteCount = ScriptingInterfaceOfIGameEntity._utf8.GetByteCount(scName);
				array = ((byteCount < 1024) ? CallbackStringBufferManager.StringBuffer0 : new byte[byteCount + 1]);
				ScriptingInterfaceOfIGameEntity._utf8.GetBytes(scName, 0, scName.Length, array, 0);
				array[byteCount] = 0;
			}
			return ScriptingInterfaceOfIGameEntity.call_HasScriptComponentDelegate(entityId, array);
		}

		// Token: 0x060001C5 RID: 453 RVA: 0x000111E3 File Offset: 0x0000F3E3
		public bool HasScriptComponentHash(UIntPtr entityId, uint scNameHash)
		{
			return ScriptingInterfaceOfIGameEntity.call_HasScriptComponentHashDelegate(entityId, scNameHash);
		}

		// Token: 0x060001C6 RID: 454 RVA: 0x000111F1 File Offset: 0x0000F3F1
		public bool HasStaticPhysicsBody(UIntPtr entityId)
		{
			return ScriptingInterfaceOfIGameEntity.call_HasStaticPhysicsBodyDelegate(entityId);
		}

		// Token: 0x060001C7 RID: 455 RVA: 0x00011200 File Offset: 0x0000F400
		public bool HasTag(UIntPtr entityId, string tag)
		{
			byte[] array = null;
			if (tag != null)
			{
				int byteCount = ScriptingInterfaceOfIGameEntity._utf8.GetByteCount(tag);
				array = ((byteCount < 1024) ? CallbackStringBufferManager.StringBuffer0 : new byte[byteCount + 1]);
				ScriptingInterfaceOfIGameEntity._utf8.GetBytes(tag, 0, tag.Length, array, 0);
				array[byteCount] = 0;
			}
			return ScriptingInterfaceOfIGameEntity.call_HasTagDelegate(entityId, array);
		}

		// Token: 0x060001C8 RID: 456 RVA: 0x0001125B File Offset: 0x0000F45B
		public bool IsDynamicBodyStationary(UIntPtr entityId)
		{
			return ScriptingInterfaceOfIGameEntity.call_IsDynamicBodyStationaryDelegate(entityId);
		}

		// Token: 0x060001C9 RID: 457 RVA: 0x00011268 File Offset: 0x0000F468
		public bool IsEngineBodySleeping(UIntPtr entityId)
		{
			return ScriptingInterfaceOfIGameEntity.call_IsEngineBodySleepingDelegate(entityId);
		}

		// Token: 0x060001CA RID: 458 RVA: 0x00011275 File Offset: 0x0000F475
		public bool IsEntitySelectedOnEditor(UIntPtr entityId)
		{
			return ScriptingInterfaceOfIGameEntity.call_IsEntitySelectedOnEditorDelegate(entityId);
		}

		// Token: 0x060001CB RID: 459 RVA: 0x00011282 File Offset: 0x0000F482
		public bool IsFrozen(UIntPtr entityId)
		{
			return ScriptingInterfaceOfIGameEntity.call_IsFrozenDelegate(entityId);
		}

		// Token: 0x060001CC RID: 460 RVA: 0x0001128F File Offset: 0x0000F48F
		public bool IsGhostObject(UIntPtr entityId)
		{
			return ScriptingInterfaceOfIGameEntity.call_IsGhostObjectDelegate(entityId);
		}

		// Token: 0x060001CD RID: 461 RVA: 0x0001129C File Offset: 0x0000F49C
		public bool IsGravityDisabled(UIntPtr entityId)
		{
			return ScriptingInterfaceOfIGameEntity.call_IsGravityDisabledDelegate(entityId);
		}

		// Token: 0x060001CE RID: 462 RVA: 0x000112A9 File Offset: 0x0000F4A9
		public bool IsGuidValid(UIntPtr entityId)
		{
			return ScriptingInterfaceOfIGameEntity.call_IsGuidValidDelegate(entityId);
		}

		// Token: 0x060001CF RID: 463 RVA: 0x000112B6 File Offset: 0x0000F4B6
		public bool IsInEditorScene(UIntPtr pointer)
		{
			return ScriptingInterfaceOfIGameEntity.call_IsInEditorSceneDelegate(pointer);
		}

		// Token: 0x060001D0 RID: 464 RVA: 0x000112C3 File Offset: 0x0000F4C3
		public bool IsVisibleIncludeParents(UIntPtr entityId)
		{
			return ScriptingInterfaceOfIGameEntity.call_IsVisibleIncludeParentsDelegate(entityId);
		}

		// Token: 0x060001D1 RID: 465 RVA: 0x000112D0 File Offset: 0x0000F4D0
		public void PauseParticleSystem(UIntPtr entityId, bool doChildren)
		{
			ScriptingInterfaceOfIGameEntity.call_PauseParticleSystemDelegate(entityId, doChildren);
		}

		// Token: 0x060001D2 RID: 466 RVA: 0x000112DE File Offset: 0x0000F4DE
		public void PopCapsuleShapeFromEntityBody(UIntPtr entityId)
		{
			ScriptingInterfaceOfIGameEntity.call_PopCapsuleShapeFromEntityBodyDelegate(entityId);
		}

		// Token: 0x060001D3 RID: 467 RVA: 0x000112EC File Offset: 0x0000F4EC
		public bool PrefabExists(string prefabName)
		{
			byte[] array = null;
			if (prefabName != null)
			{
				int byteCount = ScriptingInterfaceOfIGameEntity._utf8.GetByteCount(prefabName);
				array = ((byteCount < 1024) ? CallbackStringBufferManager.StringBuffer0 : new byte[byteCount + 1]);
				ScriptingInterfaceOfIGameEntity._utf8.GetBytes(prefabName, 0, prefabName.Length, array, 0);
				array[byteCount] = 0;
			}
			return ScriptingInterfaceOfIGameEntity.call_PrefabExistsDelegate(array);
		}

		// Token: 0x060001D4 RID: 468 RVA: 0x00011348 File Offset: 0x0000F548
		public void PushCapsuleShapeToEntityBody(UIntPtr entityId, Vec3 p1, Vec3 p2, float radius, string physicsMaterialName)
		{
			byte[] array = null;
			if (physicsMaterialName != null)
			{
				int byteCount = ScriptingInterfaceOfIGameEntity._utf8.GetByteCount(physicsMaterialName);
				array = ((byteCount < 1024) ? CallbackStringBufferManager.StringBuffer0 : new byte[byteCount + 1]);
				ScriptingInterfaceOfIGameEntity._utf8.GetBytes(physicsMaterialName, 0, physicsMaterialName.Length, array, 0);
				array[byteCount] = 0;
			}
			ScriptingInterfaceOfIGameEntity.call_PushCapsuleShapeToEntityBodyDelegate(entityId, p1, p2, radius, array);
		}

		// Token: 0x060001D5 RID: 469 RVA: 0x000113AB File Offset: 0x0000F5AB
		public bool RayHitEntity(UIntPtr entityId, in Vec3 rayOrigin, in Vec3 rayDirection, float maxLength, ref float resultLength)
		{
			return ScriptingInterfaceOfIGameEntity.call_RayHitEntityDelegate(entityId, rayOrigin, rayDirection, maxLength, ref resultLength);
		}

		// Token: 0x060001D6 RID: 470 RVA: 0x000113BE File Offset: 0x0000F5BE
		public bool RayHitEntityWithNormal(UIntPtr entityId, in Vec3 rayOrigin, in Vec3 rayDirection, float maxLength, ref Vec3 resultNormal, ref float resultLength)
		{
			return ScriptingInterfaceOfIGameEntity.call_RayHitEntityWithNormalDelegate(entityId, rayOrigin, rayDirection, maxLength, ref resultNormal, ref resultLength);
		}

		// Token: 0x060001D7 RID: 471 RVA: 0x000113D3 File Offset: 0x0000F5D3
		public void RecomputeBoundingBox(UIntPtr pointer)
		{
			ScriptingInterfaceOfIGameEntity.call_RecomputeBoundingBoxDelegate(pointer);
		}

		// Token: 0x060001D8 RID: 472 RVA: 0x000113E0 File Offset: 0x0000F5E0
		public void RefreshMeshesToRenderToHullWater(UIntPtr entityPointer, UIntPtr visualPrefab, string tag)
		{
			byte[] array = null;
			if (tag != null)
			{
				int byteCount = ScriptingInterfaceOfIGameEntity._utf8.GetByteCount(tag);
				array = ((byteCount < 1024) ? CallbackStringBufferManager.StringBuffer0 : new byte[byteCount + 1]);
				ScriptingInterfaceOfIGameEntity._utf8.GetBytes(tag, 0, tag.Length, array, 0);
				array[byteCount] = 0;
			}
			ScriptingInterfaceOfIGameEntity.call_RefreshMeshesToRenderToHullWaterDelegate(entityPointer, visualPrefab, array);
		}

		// Token: 0x060001D9 RID: 473 RVA: 0x0001143C File Offset: 0x0000F63C
		public int RegisterWaterSDFClip(UIntPtr entityId, UIntPtr textureID)
		{
			return ScriptingInterfaceOfIGameEntity.call_RegisterWaterSDFClipDelegate(entityId, textureID);
		}

		// Token: 0x060001DA RID: 474 RVA: 0x0001144A File Offset: 0x0000F64A
		public void RelaxLocalBoundingBox(UIntPtr entityId, in BoundingBox boundingBox)
		{
			ScriptingInterfaceOfIGameEntity.call_RelaxLocalBoundingBoxDelegate(entityId, boundingBox);
		}

		// Token: 0x060001DB RID: 475 RVA: 0x00011458 File Offset: 0x0000F658
		public void ReleaseEditDataUserToAllMeshes(UIntPtr entityId, bool entity_components, bool skeleton_components)
		{
			ScriptingInterfaceOfIGameEntity.call_ReleaseEditDataUserToAllMeshesDelegate(entityId, entity_components, skeleton_components);
		}

		// Token: 0x060001DC RID: 476 RVA: 0x00011467 File Offset: 0x0000F667
		public void Remove(UIntPtr entityId, int removeReason)
		{
			ScriptingInterfaceOfIGameEntity.call_RemoveDelegate(entityId, removeReason);
		}

		// Token: 0x060001DD RID: 477 RVA: 0x00011475 File Offset: 0x0000F675
		public void RemoveAllChildren(UIntPtr entityId)
		{
			ScriptingInterfaceOfIGameEntity.call_RemoveAllChildrenDelegate(entityId);
		}

		// Token: 0x060001DE RID: 478 RVA: 0x00011482 File Offset: 0x0000F682
		public void RemoveAllParticleSystems(UIntPtr entityId)
		{
			ScriptingInterfaceOfIGameEntity.call_RemoveAllParticleSystemsDelegate(entityId);
		}

		// Token: 0x060001DF RID: 479 RVA: 0x0001148F File Offset: 0x0000F68F
		public void RemoveChild(UIntPtr parentEntity, UIntPtr childEntity, bool keepPhysics, bool keepScenePointer, bool callScriptCallbacks, int removeReason)
		{
			ScriptingInterfaceOfIGameEntity.call_RemoveChildDelegate(parentEntity, childEntity, keepPhysics, keepScenePointer, callScriptCallbacks, removeReason);
		}

		// Token: 0x060001E0 RID: 480 RVA: 0x000114A4 File Offset: 0x0000F6A4
		public bool RemoveComponent(UIntPtr pointer, UIntPtr componentPointer)
		{
			return ScriptingInterfaceOfIGameEntity.call_RemoveComponentDelegate(pointer, componentPointer);
		}

		// Token: 0x060001E1 RID: 481 RVA: 0x000114B2 File Offset: 0x0000F6B2
		public bool RemoveComponentWithMesh(UIntPtr entityId, UIntPtr mesh)
		{
			return ScriptingInterfaceOfIGameEntity.call_RemoveComponentWithMeshDelegate(entityId, mesh);
		}

		// Token: 0x060001E2 RID: 482 RVA: 0x000114C0 File Offset: 0x0000F6C0
		public void RemoveEnginePhysics(UIntPtr entityId)
		{
			ScriptingInterfaceOfIGameEntity.call_RemoveEnginePhysicsDelegate(entityId);
		}

		// Token: 0x060001E3 RID: 483 RVA: 0x000114CD File Offset: 0x0000F6CD
		public void RemoveFromPredisplayEntity(UIntPtr entityId)
		{
			ScriptingInterfaceOfIGameEntity.call_RemoveFromPredisplayEntityDelegate(entityId);
		}

		// Token: 0x060001E4 RID: 484 RVA: 0x000114DA File Offset: 0x0000F6DA
		public void RemoveJoint(UIntPtr jointId, UIntPtr entityId)
		{
			ScriptingInterfaceOfIGameEntity.call_RemoveJointDelegate(jointId, entityId);
		}

		// Token: 0x060001E5 RID: 485 RVA: 0x000114E8 File Offset: 0x0000F6E8
		public bool RemoveMultiMesh(UIntPtr entityId, UIntPtr multiMeshPtr)
		{
			return ScriptingInterfaceOfIGameEntity.call_RemoveMultiMeshDelegate(entityId, multiMeshPtr);
		}

		// Token: 0x060001E6 RID: 486 RVA: 0x000114F6 File Offset: 0x0000F6F6
		public void RemoveMultiMeshFromSkeleton(UIntPtr gameEntity, UIntPtr multiMesh)
		{
			ScriptingInterfaceOfIGameEntity.call_RemoveMultiMeshFromSkeletonDelegate(gameEntity, multiMesh);
		}

		// Token: 0x060001E7 RID: 487 RVA: 0x00011504 File Offset: 0x0000F704
		public void RemoveMultiMeshFromSkeletonBone(UIntPtr gameEntity, UIntPtr multiMesh, sbyte boneIndex)
		{
			ScriptingInterfaceOfIGameEntity.call_RemoveMultiMeshFromSkeletonBoneDelegate(gameEntity, multiMesh, boneIndex);
		}

		// Token: 0x060001E8 RID: 488 RVA: 0x00011513 File Offset: 0x0000F713
		public void RemovePhysics(UIntPtr entityId, bool clearingTheScene)
		{
			ScriptingInterfaceOfIGameEntity.call_RemovePhysicsDelegate(entityId, clearingTheScene);
		}

		// Token: 0x060001E9 RID: 489 RVA: 0x00011521 File Offset: 0x0000F721
		public void RemoveScriptComponent(UIntPtr entityId, UIntPtr scriptComponentPtr, int removeReason)
		{
			ScriptingInterfaceOfIGameEntity.call_RemoveScriptComponentDelegate(entityId, scriptComponentPtr, removeReason);
		}

		// Token: 0x060001EA RID: 490 RVA: 0x00011530 File Offset: 0x0000F730
		public void RemoveTag(UIntPtr entityId, string tag)
		{
			byte[] array = null;
			if (tag != null)
			{
				int byteCount = ScriptingInterfaceOfIGameEntity._utf8.GetByteCount(tag);
				array = ((byteCount < 1024) ? CallbackStringBufferManager.StringBuffer0 : new byte[byteCount + 1]);
				ScriptingInterfaceOfIGameEntity._utf8.GetBytes(tag, 0, tag.Length, array, 0);
				array[byteCount] = 0;
			}
			ScriptingInterfaceOfIGameEntity.call_RemoveTagDelegate(entityId, array);
		}

		// Token: 0x060001EB RID: 491 RVA: 0x0001158B File Offset: 0x0000F78B
		public void ReplacePhysicsBodyWithQuadPhysicsBody(UIntPtr pointer, UIntPtr quad, int physicsMaterial, BodyFlags bodyFlags, int numberOfVertices, UIntPtr indices, int numberOfIndices)
		{
			ScriptingInterfaceOfIGameEntity.call_ReplacePhysicsBodyWithQuadPhysicsBodyDelegate(pointer, quad, physicsMaterial, bodyFlags, numberOfVertices, indices, numberOfIndices);
		}

		// Token: 0x060001EC RID: 492 RVA: 0x000115A2 File Offset: 0x0000F7A2
		public void ResetHullWater(UIntPtr visualPrefab)
		{
			ScriptingInterfaceOfIGameEntity.call_ResetHullWaterDelegate(visualPrefab);
		}

		// Token: 0x060001ED RID: 493 RVA: 0x000115AF File Offset: 0x0000F7AF
		public void ResumeParticleSystem(UIntPtr entityId, bool doChildren)
		{
			ScriptingInterfaceOfIGameEntity.call_ResumeParticleSystemDelegate(entityId, doChildren);
		}

		// Token: 0x060001EE RID: 494 RVA: 0x000115BD File Offset: 0x0000F7BD
		public void SelectEntityOnEditor(UIntPtr entityId)
		{
			ScriptingInterfaceOfIGameEntity.call_SelectEntityOnEditorDelegate(entityId);
		}

		// Token: 0x060001EF RID: 495 RVA: 0x000115CA File Offset: 0x0000F7CA
		public void SetAlpha(UIntPtr entityId, float alpha)
		{
			ScriptingInterfaceOfIGameEntity.call_SetAlphaDelegate(entityId, alpha);
		}

		// Token: 0x060001F0 RID: 496 RVA: 0x000115D8 File Offset: 0x0000F7D8
		public void SetAngularVelocity(UIntPtr entityPtr, in Vec3 newAngularVelocity)
		{
			ScriptingInterfaceOfIGameEntity.call_SetAngularVelocityDelegate(entityPtr, newAngularVelocity);
		}

		// Token: 0x060001F1 RID: 497 RVA: 0x000115E6 File Offset: 0x0000F7E6
		public void SetAnimationSoundActivation(UIntPtr entityId, bool activate)
		{
			ScriptingInterfaceOfIGameEntity.call_SetAnimationSoundActivationDelegate(entityId, activate);
		}

		// Token: 0x060001F2 RID: 498 RVA: 0x000115F4 File Offset: 0x0000F7F4
		public void SetAnimTreeChannelParameter(UIntPtr entityId, float phase, int channel_no)
		{
			ScriptingInterfaceOfIGameEntity.call_SetAnimTreeChannelParameterDelegate(entityId, phase, channel_no);
		}

		// Token: 0x060001F3 RID: 499 RVA: 0x00011603 File Offset: 0x0000F803
		public void SetAsContourEntity(UIntPtr entityId, uint color)
		{
			ScriptingInterfaceOfIGameEntity.call_SetAsContourEntityDelegate(entityId, color);
		}

		// Token: 0x060001F4 RID: 500 RVA: 0x00011611 File Offset: 0x0000F811
		public void SetAsPredisplayEntity(UIntPtr entityId)
		{
			ScriptingInterfaceOfIGameEntity.call_SetAsPredisplayEntityDelegate(entityId);
		}

		// Token: 0x060001F5 RID: 501 RVA: 0x0001161E File Offset: 0x0000F81E
		public void SetAsReplayEntity(UIntPtr gameEntity)
		{
			ScriptingInterfaceOfIGameEntity.call_SetAsReplayEntityDelegate(gameEntity);
		}

		// Token: 0x060001F6 RID: 502 RVA: 0x0001162B File Offset: 0x0000F82B
		public void SetBodyFlags(UIntPtr entityId, uint bodyFlags)
		{
			ScriptingInterfaceOfIGameEntity.call_SetBodyFlagsDelegate(entityId, bodyFlags);
		}

		// Token: 0x060001F7 RID: 503 RVA: 0x00011639 File Offset: 0x0000F839
		public void SetBodyFlagsRecursive(UIntPtr entityId, uint bodyFlags)
		{
			ScriptingInterfaceOfIGameEntity.call_SetBodyFlagsRecursiveDelegate(entityId, bodyFlags);
		}

		// Token: 0x060001F8 RID: 504 RVA: 0x00011647 File Offset: 0x0000F847
		public void SetBodyShape(UIntPtr entityId, UIntPtr shape)
		{
			ScriptingInterfaceOfIGameEntity.call_SetBodyShapeDelegate(entityId, shape);
		}

		// Token: 0x060001F9 RID: 505 RVA: 0x00011655 File Offset: 0x0000F855
		public void SetBoneFrameToAllMeshes(UIntPtr entityPtr, int boneIndex, in MatrixFrame frame)
		{
			ScriptingInterfaceOfIGameEntity.call_SetBoneFrameToAllMeshesDelegate(entityPtr, boneIndex, frame);
		}

		// Token: 0x060001FA RID: 506 RVA: 0x00011664 File Offset: 0x0000F864
		public void SetBoundingboxDirty(UIntPtr entityId)
		{
			ScriptingInterfaceOfIGameEntity.call_SetBoundingboxDirtyDelegate(entityId);
		}

		// Token: 0x060001FB RID: 507 RVA: 0x00011671 File Offset: 0x0000F871
		public void SetCenterOfMass(UIntPtr entityId, ref Vec3 localCenterOfMass)
		{
			ScriptingInterfaceOfIGameEntity.call_SetCenterOfMassDelegate(entityId, ref localCenterOfMass);
		}

		// Token: 0x060001FC RID: 508 RVA: 0x0001167F File Offset: 0x0000F87F
		public void SetClothComponentKeepState(UIntPtr entityId, UIntPtr metaMesh, bool keepState)
		{
			ScriptingInterfaceOfIGameEntity.call_SetClothComponentKeepStateDelegate(entityId, metaMesh, keepState);
		}

		// Token: 0x060001FD RID: 509 RVA: 0x0001168E File Offset: 0x0000F88E
		public void SetClothComponentKeepStateOfAllMeshes(UIntPtr entityId, bool keepState)
		{
			ScriptingInterfaceOfIGameEntity.call_SetClothComponentKeepStateOfAllMeshesDelegate(entityId, keepState);
		}

		// Token: 0x060001FE RID: 510 RVA: 0x0001169C File Offset: 0x0000F89C
		public void SetClothMaxDistanceMultiplier(UIntPtr gameEntity, float multiplier)
		{
			ScriptingInterfaceOfIGameEntity.call_SetClothMaxDistanceMultiplierDelegate(gameEntity, multiplier);
		}

		// Token: 0x060001FF RID: 511 RVA: 0x000116AC File Offset: 0x0000F8AC
		public void SetColorToAllMeshesWithTagRecursive(UIntPtr gameEntity, uint color, string tag)
		{
			byte[] array = null;
			if (tag != null)
			{
				int byteCount = ScriptingInterfaceOfIGameEntity._utf8.GetByteCount(tag);
				array = ((byteCount < 1024) ? CallbackStringBufferManager.StringBuffer0 : new byte[byteCount + 1]);
				ScriptingInterfaceOfIGameEntity._utf8.GetBytes(tag, 0, tag.Length, array, 0);
				array[byteCount] = 0;
			}
			ScriptingInterfaceOfIGameEntity.call_SetColorToAllMeshesWithTagRecursiveDelegate(gameEntity, color, array);
		}

		// Token: 0x06000200 RID: 512 RVA: 0x00011708 File Offset: 0x0000F908
		public void SetContourState(UIntPtr entityId, bool alwaysVisible)
		{
			ScriptingInterfaceOfIGameEntity.call_SetContourStateDelegate(entityId, alwaysVisible);
		}

		// Token: 0x06000201 RID: 513 RVA: 0x00011716 File Offset: 0x0000F916
		public void SetCostAdderForAttachedFaces(UIntPtr entityId, float cost)
		{
			ScriptingInterfaceOfIGameEntity.call_SetCostAdderForAttachedFacesDelegate(entityId, cost);
		}

		// Token: 0x06000202 RID: 514 RVA: 0x00011724 File Offset: 0x0000F924
		public void SetCullMode(UIntPtr entityPtr, MBMeshCullingMode cullMode)
		{
			ScriptingInterfaceOfIGameEntity.call_SetCullModeDelegate(entityPtr, cullMode);
		}

		// Token: 0x06000203 RID: 515 RVA: 0x00011732 File Offset: 0x0000F932
		public void SetCustomClipPlane(UIntPtr entityId, Vec3 position, Vec3 normal, bool setForChildren)
		{
			ScriptingInterfaceOfIGameEntity.call_SetCustomClipPlaneDelegate(entityId, position, normal, setForChildren);
		}

		// Token: 0x06000204 RID: 516 RVA: 0x00011743 File Offset: 0x0000F943
		public void SetCustomVertexPositionEnabled(UIntPtr entityId, bool customVertexPositionEnabled)
		{
			ScriptingInterfaceOfIGameEntity.call_SetCustomVertexPositionEnabledDelegate(entityId, customVertexPositionEnabled);
		}

		// Token: 0x06000205 RID: 517 RVA: 0x00011751 File Offset: 0x0000F951
		public void SetDamping(UIntPtr entityId, float linearDamping, float angularDamping)
		{
			ScriptingInterfaceOfIGameEntity.call_SetDampingDelegate(entityId, linearDamping, angularDamping);
		}

		// Token: 0x06000206 RID: 518 RVA: 0x00011760 File Offset: 0x0000F960
		public void SetDoNotCheckVisibility(UIntPtr entityPtr, bool value)
		{
			ScriptingInterfaceOfIGameEntity.call_SetDoNotCheckVisibilityDelegate(entityPtr, value);
		}

		// Token: 0x06000207 RID: 519 RVA: 0x0001176E File Offset: 0x0000F96E
		public void SetEnforcedMaximumLodLevel(UIntPtr entityId, int lodLevel)
		{
			ScriptingInterfaceOfIGameEntity.call_SetEnforcedMaximumLodLevelDelegate(entityId, lodLevel);
		}

		// Token: 0x06000208 RID: 520 RVA: 0x0001177C File Offset: 0x0000F97C
		public void SetEntityEnvMapVisibility(UIntPtr entityId, bool value)
		{
			ScriptingInterfaceOfIGameEntity.call_SetEntityEnvMapVisibilityDelegate(entityId, value);
		}

		// Token: 0x06000209 RID: 521 RVA: 0x0001178A File Offset: 0x0000F98A
		public void SetEntityFlags(UIntPtr entityId, EntityFlags entityFlags)
		{
			ScriptingInterfaceOfIGameEntity.call_SetEntityFlagsDelegate(entityId, entityFlags);
		}

		// Token: 0x0600020A RID: 522 RVA: 0x00011798 File Offset: 0x0000F998
		public void SetEntityVisibilityFlags(UIntPtr entityId, EntityVisibilityFlags entityVisibilityFlags)
		{
			ScriptingInterfaceOfIGameEntity.call_SetEntityVisibilityFlagsDelegate(entityId, entityVisibilityFlags);
		}

		// Token: 0x0600020B RID: 523 RVA: 0x000117A6 File Offset: 0x0000F9A6
		public void SetExternalReferencesUsage(UIntPtr entityId, bool value)
		{
			ScriptingInterfaceOfIGameEntity.call_SetExternalReferencesUsageDelegate(entityId, value);
		}

		// Token: 0x0600020C RID: 524 RVA: 0x000117B4 File Offset: 0x0000F9B4
		public void SetFactor2Color(UIntPtr entityId, uint factor2Color)
		{
			ScriptingInterfaceOfIGameEntity.call_SetFactor2ColorDelegate(entityId, factor2Color);
		}

		// Token: 0x0600020D RID: 525 RVA: 0x000117C2 File Offset: 0x0000F9C2
		public void SetFactorColor(UIntPtr entityId, uint factorColor)
		{
			ScriptingInterfaceOfIGameEntity.call_SetFactorColorDelegate(entityId, factorColor);
		}

		// Token: 0x0600020E RID: 526 RVA: 0x000117D0 File Offset: 0x0000F9D0
		public void SetForceDecalsToRender(UIntPtr entityPtr, bool value)
		{
			ScriptingInterfaceOfIGameEntity.call_SetForceDecalsToRenderDelegate(entityPtr, value);
		}

		// Token: 0x0600020F RID: 527 RVA: 0x000117DE File Offset: 0x0000F9DE
		public void SetForceNotAffectedBySeason(UIntPtr entityPtr, bool value)
		{
			ScriptingInterfaceOfIGameEntity.call_SetForceNotAffectedBySeasonDelegate(entityPtr, value);
		}

		// Token: 0x06000210 RID: 528 RVA: 0x000117EC File Offset: 0x0000F9EC
		public void SetFrameChanged(UIntPtr entityId)
		{
			ScriptingInterfaceOfIGameEntity.call_SetFrameChangedDelegate(entityId);
		}

		// Token: 0x06000211 RID: 529 RVA: 0x000117F9 File Offset: 0x0000F9F9
		public void SetGlobalFrame(UIntPtr entityId, in MatrixFrame frame, bool isTeleportation)
		{
			ScriptingInterfaceOfIGameEntity.call_SetGlobalFrameDelegate(entityId, frame, isTeleportation);
		}

		// Token: 0x06000212 RID: 530 RVA: 0x00011808 File Offset: 0x0000FA08
		public void SetGlobalPosition(UIntPtr entityId, in Vec3 position)
		{
			ScriptingInterfaceOfIGameEntity.call_SetGlobalPositionDelegate(entityId, position);
		}

		// Token: 0x06000213 RID: 531 RVA: 0x00011816 File Offset: 0x0000FA16
		public void SetHasCustomBoundingBoxValidationSystem(UIntPtr entityId, bool hasCustomBoundingBox)
		{
			ScriptingInterfaceOfIGameEntity.call_SetHasCustomBoundingBoxValidationSystemDelegate(entityId, hasCustomBoundingBox);
		}

		// Token: 0x06000214 RID: 532 RVA: 0x00011824 File Offset: 0x0000FA24
		public void SetLinearVelocity(UIntPtr entityPtr, Vec3 newLinearVelocity)
		{
			ScriptingInterfaceOfIGameEntity.call_SetLinearVelocityDelegate(entityPtr, newLinearVelocity);
		}

		// Token: 0x06000215 RID: 533 RVA: 0x00011832 File Offset: 0x0000FA32
		public void SetLocalFrame(UIntPtr entityId, ref MatrixFrame frame, bool isTeleportation)
		{
			ScriptingInterfaceOfIGameEntity.call_SetLocalFrameDelegate(entityId, ref frame, isTeleportation);
		}

		// Token: 0x06000216 RID: 534 RVA: 0x00011841 File Offset: 0x0000FA41
		public void SetLocalPosition(UIntPtr entityId, Vec3 position)
		{
			ScriptingInterfaceOfIGameEntity.call_SetLocalPositionDelegate(entityId, position);
		}

		// Token: 0x06000217 RID: 535 RVA: 0x0001184F File Offset: 0x0000FA4F
		public void SetManualGlobalBoundingBox(UIntPtr entityId, Vec3 boundingBoxStartGlobal, Vec3 boundingBoxEndGlobal)
		{
			ScriptingInterfaceOfIGameEntity.call_SetManualGlobalBoundingBoxDelegate(entityId, boundingBoxStartGlobal, boundingBoxEndGlobal);
		}

		// Token: 0x06000218 RID: 536 RVA: 0x0001185E File Offset: 0x0000FA5E
		public void SetManualLocalBoundingBox(UIntPtr entityId, in BoundingBox boundingBox)
		{
			ScriptingInterfaceOfIGameEntity.call_SetManualLocalBoundingBoxDelegate(entityId, boundingBox);
		}

		// Token: 0x06000219 RID: 537 RVA: 0x0001186C File Offset: 0x0000FA6C
		public void SetMassAndUpdateInertiaAndCenterOfMass(UIntPtr entityId, float mass)
		{
			ScriptingInterfaceOfIGameEntity.call_SetMassAndUpdateInertiaAndCenterOfMassDelegate(entityId, mass);
		}

		// Token: 0x0600021A RID: 538 RVA: 0x0001187A File Offset: 0x0000FA7A
		public void SetMassSpaceInertia(UIntPtr entityId, ref Vec3 inertia)
		{
			ScriptingInterfaceOfIGameEntity.call_SetMassSpaceInertiaDelegate(entityId, ref inertia);
		}

		// Token: 0x0600021B RID: 539 RVA: 0x00011888 File Offset: 0x0000FA88
		public void SetMaterialForAllMeshes(UIntPtr entityId, UIntPtr materialPointer)
		{
			ScriptingInterfaceOfIGameEntity.call_SetMaterialForAllMeshesDelegate(entityId, materialPointer);
		}

		// Token: 0x0600021C RID: 540 RVA: 0x00011896 File Offset: 0x0000FA96
		public void SetMaxDepenetrationVelocity(UIntPtr entityId, float maxDepenetrationVelocity)
		{
			ScriptingInterfaceOfIGameEntity.call_SetMaxDepenetrationVelocityDelegate(entityId, maxDepenetrationVelocity);
		}

		// Token: 0x0600021D RID: 541 RVA: 0x000118A4 File Offset: 0x0000FAA4
		public void SetMobility(UIntPtr entityId, GameEntity.Mobility mobility)
		{
			ScriptingInterfaceOfIGameEntity.call_SetMobilityDelegate(entityId, mobility);
		}

		// Token: 0x0600021E RID: 542 RVA: 0x000118B2 File Offset: 0x0000FAB2
		public void SetMorphFrameOfComponents(UIntPtr entityId, float value)
		{
			ScriptingInterfaceOfIGameEntity.call_SetMorphFrameOfComponentsDelegate(entityId, value);
		}

		// Token: 0x0600021F RID: 543 RVA: 0x000118C0 File Offset: 0x0000FAC0
		public void SetName(UIntPtr entityId, string name)
		{
			byte[] array = null;
			if (name != null)
			{
				int byteCount = ScriptingInterfaceOfIGameEntity._utf8.GetByteCount(name);
				array = ((byteCount < 1024) ? CallbackStringBufferManager.StringBuffer0 : new byte[byteCount + 1]);
				ScriptingInterfaceOfIGameEntity._utf8.GetBytes(name, 0, name.Length, array, 0);
				array[byteCount] = 0;
			}
			ScriptingInterfaceOfIGameEntity.call_SetNameDelegate(entityId, array);
		}

		// Token: 0x06000220 RID: 544 RVA: 0x0001191C File Offset: 0x0000FB1C
		public void SetNativeScriptComponentVariable(UIntPtr entityPtr, string className, string fieldName, ref ScriptComponentFieldHolder data, RglScriptFieldType variableType)
		{
			byte[] array = null;
			if (className != null)
			{
				int byteCount = ScriptingInterfaceOfIGameEntity._utf8.GetByteCount(className);
				array = ((byteCount < 1024) ? CallbackStringBufferManager.StringBuffer0 : new byte[byteCount + 1]);
				ScriptingInterfaceOfIGameEntity._utf8.GetBytes(className, 0, className.Length, array, 0);
				array[byteCount] = 0;
			}
			byte[] array2 = null;
			if (fieldName != null)
			{
				int byteCount2 = ScriptingInterfaceOfIGameEntity._utf8.GetByteCount(fieldName);
				array2 = ((byteCount2 < 1024) ? CallbackStringBufferManager.StringBuffer1 : new byte[byteCount2 + 1]);
				ScriptingInterfaceOfIGameEntity._utf8.GetBytes(fieldName, 0, fieldName.Length, array2, 0);
				array2[byteCount2] = 0;
			}
			ScriptingInterfaceOfIGameEntity.call_SetNativeScriptComponentVariableDelegate(entityPtr, array, array2, ref data, variableType);
		}

		// Token: 0x06000221 RID: 545 RVA: 0x000119BE File Offset: 0x0000FBBE
		public void SetPhysicsMoveToBatched(UIntPtr entityId, bool value)
		{
			ScriptingInterfaceOfIGameEntity.call_SetPhysicsMoveToBatchedDelegate(entityId, value);
		}

		// Token: 0x06000222 RID: 546 RVA: 0x000119CC File Offset: 0x0000FBCC
		public void SetPhysicsState(UIntPtr entityId, bool isEnabled, bool setChildren)
		{
			ScriptingInterfaceOfIGameEntity.call_SetPhysicsStateDelegate(entityId, isEnabled, setChildren);
		}

		// Token: 0x06000223 RID: 547 RVA: 0x000119DB File Offset: 0x0000FBDB
		public void SetPhysicsStateOnlyVariable(UIntPtr entityId, bool isEnabled, bool setChildren)
		{
			ScriptingInterfaceOfIGameEntity.call_SetPhysicsStateOnlyVariableDelegate(entityId, isEnabled, setChildren);
		}

		// Token: 0x06000224 RID: 548 RVA: 0x000119EC File Offset: 0x0000FBEC
		public void SetPositionsForAttachedNavmeshVertices(UIntPtr entityId, int[] indices, int indexCount, Vec3[] positions)
		{
			PinnedArrayData<int> pinnedArrayData = new PinnedArrayData<int>(indices, false);
			IntPtr pointer = pinnedArrayData.Pointer;
			PinnedArrayData<Vec3> pinnedArrayData2 = new PinnedArrayData<Vec3>(positions, false);
			IntPtr pointer2 = pinnedArrayData2.Pointer;
			ScriptingInterfaceOfIGameEntity.call_SetPositionsForAttachedNavmeshVerticesDelegate(entityId, pointer, indexCount, pointer2);
			pinnedArrayData.Dispose();
			pinnedArrayData2.Dispose();
		}

		// Token: 0x06000225 RID: 549 RVA: 0x00011A38 File Offset: 0x0000FC38
		public void SetPreviousFrameInvalid(UIntPtr gameEntity)
		{
			ScriptingInterfaceOfIGameEntity.call_SetPreviousFrameInvalidDelegate(gameEntity);
		}

		// Token: 0x06000226 RID: 550 RVA: 0x00011A45 File Offset: 0x0000FC45
		public void SetReadyToRender(UIntPtr entityId, bool ready)
		{
			ScriptingInterfaceOfIGameEntity.call_SetReadyToRenderDelegate(entityId, ready);
		}

		// Token: 0x06000227 RID: 551 RVA: 0x00011A53 File Offset: 0x0000FC53
		public void SetRuntimeEmissionRateMultiplier(UIntPtr entityId, float emission_rate_multiplier)
		{
			ScriptingInterfaceOfIGameEntity.call_SetRuntimeEmissionRateMultiplierDelegate(entityId, emission_rate_multiplier);
		}

		// Token: 0x06000228 RID: 552 RVA: 0x00011A61 File Offset: 0x0000FC61
		public void SetSkeleton(UIntPtr entityId, UIntPtr skeletonPointer)
		{
			ScriptingInterfaceOfIGameEntity.call_SetSkeletonDelegate(entityId, skeletonPointer);
		}

		// Token: 0x06000229 RID: 553 RVA: 0x00011A6F File Offset: 0x0000FC6F
		public void SetSolverIterationCounts(UIntPtr entityId, int positionIterationCount, int velocityIterationCount)
		{
			ScriptingInterfaceOfIGameEntity.call_SetSolverIterationCountsDelegate(entityId, positionIterationCount, velocityIterationCount);
		}

		// Token: 0x0600022A RID: 554 RVA: 0x00011A7E File Offset: 0x0000FC7E
		public void SetupAdditionalBoneBufferForMeshes(UIntPtr entityPtr, int boneCount)
		{
			ScriptingInterfaceOfIGameEntity.call_SetupAdditionalBoneBufferForMeshesDelegate(entityPtr, boneCount);
		}

		// Token: 0x0600022B RID: 555 RVA: 0x00011A8C File Offset: 0x0000FC8C
		public void SetUpdateValidityOnFrameChangedOfFacesWithId(UIntPtr entityId, int faceGroupId, bool updateValidity)
		{
			ScriptingInterfaceOfIGameEntity.call_SetUpdateValidityOnFrameChangedOfFacesWithIdDelegate(entityId, faceGroupId, updateValidity);
		}

		// Token: 0x0600022C RID: 556 RVA: 0x00011A9B File Offset: 0x0000FC9B
		public void SetUpgradeLevelMask(UIntPtr prefab, uint mask)
		{
			ScriptingInterfaceOfIGameEntity.call_SetUpgradeLevelMaskDelegate(prefab, mask);
		}

		// Token: 0x0600022D RID: 557 RVA: 0x00011AA9 File Offset: 0x0000FCA9
		public void SetVectorArgument(UIntPtr entityId, float vectorArgument0, float vectorArgument1, float vectorArgument2, float vectorArgument3)
		{
			ScriptingInterfaceOfIGameEntity.call_SetVectorArgumentDelegate(entityId, vectorArgument0, vectorArgument1, vectorArgument2, vectorArgument3);
		}

		// Token: 0x0600022E RID: 558 RVA: 0x00011ABC File Offset: 0x0000FCBC
		public void SetVelocityLimits(UIntPtr entityId, float maxLinearVelocity, float maxAngularVelocity)
		{
			ScriptingInterfaceOfIGameEntity.call_SetVelocityLimitsDelegate(entityId, maxLinearVelocity, maxAngularVelocity);
		}

		// Token: 0x0600022F RID: 559 RVA: 0x00011ACB File Offset: 0x0000FCCB
		public void SetVisibilityExcludeParents(UIntPtr entityId, bool visibility)
		{
			ScriptingInterfaceOfIGameEntity.call_SetVisibilityExcludeParentsDelegate(entityId, visibility);
		}

		// Token: 0x06000230 RID: 560 RVA: 0x00011AD9 File Offset: 0x0000FCD9
		public void SetVisualRecordWakeParams(UIntPtr visualRecord, in Vec3 wakeParams)
		{
			ScriptingInterfaceOfIGameEntity.call_SetVisualRecordWakeParamsDelegate(visualRecord, wakeParams);
		}

		// Token: 0x06000231 RID: 561 RVA: 0x00011AE7 File Offset: 0x0000FCE7
		public void SetWaterSDFClipData(UIntPtr entityId, int slotIndex, in MatrixFrame frame, bool visibility)
		{
			ScriptingInterfaceOfIGameEntity.call_SetWaterSDFClipDataDelegate(entityId, slotIndex, frame, visibility);
		}

		// Token: 0x06000232 RID: 562 RVA: 0x00011AF8 File Offset: 0x0000FCF8
		public void SetWaterVisualRecordFrameAndDt(UIntPtr entityPointer, UIntPtr visualPrefab, in MatrixFrame frame, float dt)
		{
			ScriptingInterfaceOfIGameEntity.call_SetWaterVisualRecordFrameAndDtDelegate(entityPointer, visualPrefab, frame, dt);
		}

		// Token: 0x06000233 RID: 563 RVA: 0x00011B09 File Offset: 0x0000FD09
		public void SwapPhysxShapeInEntity(UIntPtr entityPtr, UIntPtr oldShape, UIntPtr newShape, bool isVariable)
		{
			ScriptingInterfaceOfIGameEntity.call_SwapPhysxShapeInEntityDelegate(entityPtr, oldShape, newShape, isVariable);
		}

		// Token: 0x06000234 RID: 564 RVA: 0x00011B1A File Offset: 0x0000FD1A
		public void UpdateAttachedNavigationMeshFaces(UIntPtr entityId)
		{
			ScriptingInterfaceOfIGameEntity.call_UpdateAttachedNavigationMeshFacesDelegate(entityId);
		}

		// Token: 0x06000235 RID: 565 RVA: 0x00011B27 File Offset: 0x0000FD27
		public void UpdateGlobalBounds(UIntPtr entityPointer)
		{
			ScriptingInterfaceOfIGameEntity.call_UpdateGlobalBoundsDelegate(entityPointer);
		}

		// Token: 0x06000236 RID: 566 RVA: 0x00011B34 File Offset: 0x0000FD34
		public void UpdateHullWaterEffectFrames(UIntPtr entityPointer, UIntPtr visualPrefab)
		{
			ScriptingInterfaceOfIGameEntity.call_UpdateHullWaterEffectFramesDelegate(entityPointer, visualPrefab);
		}

		// Token: 0x06000237 RID: 567 RVA: 0x00011B42 File Offset: 0x0000FD42
		public void UpdateTriadFrameForEditor(UIntPtr meshPointer)
		{
			ScriptingInterfaceOfIGameEntity.call_UpdateTriadFrameForEditorDelegate(meshPointer);
		}

		// Token: 0x06000238 RID: 568 RVA: 0x00011B4F File Offset: 0x0000FD4F
		public void UpdateVisibilityMask(UIntPtr entityPtr)
		{
			ScriptingInterfaceOfIGameEntity.call_UpdateVisibilityMaskDelegate(entityPtr);
		}

		// Token: 0x06000239 RID: 569 RVA: 0x00011B5C File Offset: 0x0000FD5C
		public void ValidateBoundingBox(UIntPtr entityPointer)
		{
			ScriptingInterfaceOfIGameEntity.call_ValidateBoundingBoxDelegate(entityPointer);
		}

		// Token: 0x0600023C RID: 572 RVA: 0x00011B7D File Offset: 0x0000FD7D
		void IGameEntity.SetWaterSDFClipData(UIntPtr entityId, int slotIndex, in MatrixFrame frame, bool visibility)
		{
			this.SetWaterSDFClipData(entityId, slotIndex, frame, visibility);
		}

		// Token: 0x0600023D RID: 573 RVA: 0x00011B8A File Offset: 0x0000FD8A
		void IGameEntity.SetGlobalFrame(UIntPtr entityId, in MatrixFrame frame, bool isTeleportation)
		{
			this.SetGlobalFrame(entityId, frame, isTeleportation);
		}

		// Token: 0x0600023E RID: 574 RVA: 0x00011B95 File Offset: 0x0000FD95
		void IGameEntity.ComputeVelocityDeltaFromImpulse(UIntPtr entityPtr, in Vec3 impulsiveForce, in Vec3 impulsiveTorque, out Vec3 deltaLinearVelocity, out Vec3 deltaAngularVelocity)
		{
			this.ComputeVelocityDeltaFromImpulse(entityPtr, impulsiveForce, impulsiveTorque, out deltaLinearVelocity, out deltaAngularVelocity);
		}

		// Token: 0x0600023F RID: 575 RVA: 0x00011BA4 File Offset: 0x0000FDA4
		void IGameEntity.SetGlobalPosition(UIntPtr entityId, in Vec3 position)
		{
			this.SetGlobalPosition(entityId, position);
		}

		// Token: 0x06000240 RID: 576 RVA: 0x00011BAE File Offset: 0x0000FDAE
		void IGameEntity.SetVisualRecordWakeParams(UIntPtr visualRecord, in Vec3 wakeParams)
		{
			this.SetVisualRecordWakeParams(visualRecord, wakeParams);
		}

		// Token: 0x06000241 RID: 577 RVA: 0x00011BB8 File Offset: 0x0000FDB8
		void IGameEntity.ChangeResolutionMultiplierOfWaterVisual(UIntPtr visualPrefab, float multiplier, in Vec3 waterEffectsBB)
		{
			this.ChangeResolutionMultiplierOfWaterVisual(visualPrefab, multiplier, waterEffectsBB);
		}

		// Token: 0x06000242 RID: 578 RVA: 0x00011BC3 File Offset: 0x0000FDC3
		void IGameEntity.SetWaterVisualRecordFrameAndDt(UIntPtr entityPointer, UIntPtr visualPrefab, in MatrixFrame frame, float dt)
		{
			this.SetWaterVisualRecordFrameAndDt(entityPointer, visualPrefab, frame, dt);
		}

		// Token: 0x06000243 RID: 579 RVA: 0x00011BD0 File Offset: 0x0000FDD0
		void IGameEntity.AddSplashPositionToWaterVisualRecord(UIntPtr entityPointer, UIntPtr visualPrefab, in Vec3 position)
		{
			this.AddSplashPositionToWaterVisualRecord(entityPointer, visualPrefab, position);
		}

		// Token: 0x06000244 RID: 580 RVA: 0x00011BDB File Offset: 0x0000FDDB
		void IGameEntity.GetAttachedNavmeshFaceVertexIndices(UIntPtr entityId, in PathFaceRecord faceRecord, int[] indices)
		{
			this.GetAttachedNavmeshFaceVertexIndices(entityId, faceRecord, indices);
		}

		// Token: 0x06000245 RID: 581 RVA: 0x00011BE6 File Offset: 0x0000FDE6
		float IGameEntity.GetWaterLevelAtPosition(UIntPtr entityId, in Vec2 position, bool useWaterRenderer, bool checkWaterBodyEntities)
		{
			return this.GetWaterLevelAtPosition(entityId, position, useWaterRenderer, checkWaterBodyEntities);
		}

		// Token: 0x06000246 RID: 582 RVA: 0x00011BF3 File Offset: 0x0000FDF3
		bool IGameEntity.RayHitEntity(UIntPtr entityId, in Vec3 rayOrigin, in Vec3 rayDirection, float maxLength, ref float resultLength)
		{
			return this.RayHitEntity(entityId, rayOrigin, rayDirection, maxLength, ref resultLength);
		}

		// Token: 0x06000247 RID: 583 RVA: 0x00011C02 File Offset: 0x0000FE02
		bool IGameEntity.RayHitEntityWithNormal(UIntPtr entityId, in Vec3 rayOrigin, in Vec3 rayDirection, float maxLength, ref Vec3 resultNormal, ref float resultLength)
		{
			return this.RayHitEntityWithNormal(entityId, rayOrigin, rayDirection, maxLength, ref resultNormal, ref resultLength);
		}

		// Token: 0x06000248 RID: 584 RVA: 0x00011C13 File Offset: 0x0000FE13
		void IGameEntity.SetManualLocalBoundingBox(UIntPtr entityId, in BoundingBox boundingBox)
		{
			this.SetManualLocalBoundingBox(entityId, boundingBox);
		}

		// Token: 0x06000249 RID: 585 RVA: 0x00011C1D File Offset: 0x0000FE1D
		void IGameEntity.RelaxLocalBoundingBox(UIntPtr entityId, in BoundingBox boundingBox)
		{
			this.RelaxLocalBoundingBox(entityId, boundingBox);
		}

		// Token: 0x0600024A RID: 586 RVA: 0x00011C27 File Offset: 0x0000FE27
		void IGameEntity.SetAngularVelocity(UIntPtr entityPtr, in Vec3 newAngularVelocity)
		{
			this.SetAngularVelocity(entityPtr, newAngularVelocity);
		}

		// Token: 0x0600024B RID: 587 RVA: 0x00011C31 File Offset: 0x0000FE31
		void IGameEntity.SetBoneFrameToAllMeshes(UIntPtr entityPtr, int boneIndex, in MatrixFrame frame)
		{
			this.SetBoneFrameToAllMeshes(entityPtr, boneIndex, frame);
		}

		// Token: 0x040000B5 RID: 181
		private static readonly Encoding _utf8 = Encoding.UTF8;

		// Token: 0x040000B6 RID: 182
		public static ScriptingInterfaceOfIGameEntity.ActivateRagdollDelegate call_ActivateRagdollDelegate;

		// Token: 0x040000B7 RID: 183
		public static ScriptingInterfaceOfIGameEntity.AddAllMeshesOfGameEntityDelegate call_AddAllMeshesOfGameEntityDelegate;

		// Token: 0x040000B8 RID: 184
		public static ScriptingInterfaceOfIGameEntity.AddCapsuleAsBodyDelegate call_AddCapsuleAsBodyDelegate;

		// Token: 0x040000B9 RID: 185
		public static ScriptingInterfaceOfIGameEntity.AddChildDelegate call_AddChildDelegate;

		// Token: 0x040000BA RID: 186
		public static ScriptingInterfaceOfIGameEntity.AddComponentDelegate call_AddComponentDelegate;

		// Token: 0x040000BB RID: 187
		public static ScriptingInterfaceOfIGameEntity.AddDistanceJointDelegate call_AddDistanceJointDelegate;

		// Token: 0x040000BC RID: 188
		public static ScriptingInterfaceOfIGameEntity.AddDistanceJointWithFramesDelegate call_AddDistanceJointWithFramesDelegate;

		// Token: 0x040000BD RID: 189
		public static ScriptingInterfaceOfIGameEntity.AddEditDataUserToAllMeshesDelegate call_AddEditDataUserToAllMeshesDelegate;

		// Token: 0x040000BE RID: 190
		public static ScriptingInterfaceOfIGameEntity.AddLightDelegate call_AddLightDelegate;

		// Token: 0x040000BF RID: 191
		public static ScriptingInterfaceOfIGameEntity.AddMeshDelegate call_AddMeshDelegate;

		// Token: 0x040000C0 RID: 192
		public static ScriptingInterfaceOfIGameEntity.AddMeshToBoneDelegate call_AddMeshToBoneDelegate;

		// Token: 0x040000C1 RID: 193
		public static ScriptingInterfaceOfIGameEntity.AddMultiMeshDelegate call_AddMultiMeshDelegate;

		// Token: 0x040000C2 RID: 194
		public static ScriptingInterfaceOfIGameEntity.AddMultiMeshToSkeletonDelegate call_AddMultiMeshToSkeletonDelegate;

		// Token: 0x040000C3 RID: 195
		public static ScriptingInterfaceOfIGameEntity.AddMultiMeshToSkeletonBoneDelegate call_AddMultiMeshToSkeletonBoneDelegate;

		// Token: 0x040000C4 RID: 196
		public static ScriptingInterfaceOfIGameEntity.AddParticleSystemComponentDelegate call_AddParticleSystemComponentDelegate;

		// Token: 0x040000C5 RID: 197
		public static ScriptingInterfaceOfIGameEntity.AddPhysicsDelegate call_AddPhysicsDelegate;

		// Token: 0x040000C6 RID: 198
		public static ScriptingInterfaceOfIGameEntity.AddSphereAsBodyDelegate call_AddSphereAsBodyDelegate;

		// Token: 0x040000C7 RID: 199
		public static ScriptingInterfaceOfIGameEntity.AddSplashPositionToWaterVisualRecordDelegate call_AddSplashPositionToWaterVisualRecordDelegate;

		// Token: 0x040000C8 RID: 200
		public static ScriptingInterfaceOfIGameEntity.AddTagDelegate call_AddTagDelegate;

		// Token: 0x040000C9 RID: 201
		public static ScriptingInterfaceOfIGameEntity.ApplyAccelerationToDynamicBodyDelegate call_ApplyAccelerationToDynamicBodyDelegate;

		// Token: 0x040000CA RID: 202
		public static ScriptingInterfaceOfIGameEntity.ApplyForceToDynamicBodyDelegate call_ApplyForceToDynamicBodyDelegate;

		// Token: 0x040000CB RID: 203
		public static ScriptingInterfaceOfIGameEntity.ApplyGlobalForceAtLocalPosToDynamicBodyDelegate call_ApplyGlobalForceAtLocalPosToDynamicBodyDelegate;

		// Token: 0x040000CC RID: 204
		public static ScriptingInterfaceOfIGameEntity.ApplyLocalForceAtLocalPosToDynamicBodyDelegate call_ApplyLocalForceAtLocalPosToDynamicBodyDelegate;

		// Token: 0x040000CD RID: 205
		public static ScriptingInterfaceOfIGameEntity.ApplyLocalImpulseToDynamicBodyDelegate call_ApplyLocalImpulseToDynamicBodyDelegate;

		// Token: 0x040000CE RID: 206
		public static ScriptingInterfaceOfIGameEntity.ApplyTorqueToDynamicBodyDelegate call_ApplyTorqueToDynamicBodyDelegate;

		// Token: 0x040000CF RID: 207
		public static ScriptingInterfaceOfIGameEntity.AttachNavigationMeshFacesDelegate call_AttachNavigationMeshFacesDelegate;

		// Token: 0x040000D0 RID: 208
		public static ScriptingInterfaceOfIGameEntity.BreakPrefabDelegate call_BreakPrefabDelegate;

		// Token: 0x040000D1 RID: 209
		public static ScriptingInterfaceOfIGameEntity.BurstEntityParticleDelegate call_BurstEntityParticleDelegate;

		// Token: 0x040000D2 RID: 210
		public static ScriptingInterfaceOfIGameEntity.CallScriptCallbacksDelegate call_CallScriptCallbacksDelegate;

		// Token: 0x040000D3 RID: 211
		public static ScriptingInterfaceOfIGameEntity.ChangeMetaMeshOrRemoveItIfNotExistsDelegate call_ChangeMetaMeshOrRemoveItIfNotExistsDelegate;

		// Token: 0x040000D4 RID: 212
		public static ScriptingInterfaceOfIGameEntity.ChangeResolutionMultiplierOfWaterVisualDelegate call_ChangeResolutionMultiplierOfWaterVisualDelegate;

		// Token: 0x040000D5 RID: 213
		public static ScriptingInterfaceOfIGameEntity.CheckIsPrefabLinkRootPrefabDelegate call_CheckIsPrefabLinkRootPrefabDelegate;

		// Token: 0x040000D6 RID: 214
		public static ScriptingInterfaceOfIGameEntity.CheckPointWithOrientedBoundingBoxDelegate call_CheckPointWithOrientedBoundingBoxDelegate;

		// Token: 0x040000D7 RID: 215
		public static ScriptingInterfaceOfIGameEntity.CheckResourcesDelegate call_CheckResourcesDelegate;

		// Token: 0x040000D8 RID: 216
		public static ScriptingInterfaceOfIGameEntity.ClearComponentsDelegate call_ClearComponentsDelegate;

		// Token: 0x040000D9 RID: 217
		public static ScriptingInterfaceOfIGameEntity.ClearEntityComponentsDelegate call_ClearEntityComponentsDelegate;

		// Token: 0x040000DA RID: 218
		public static ScriptingInterfaceOfIGameEntity.ClearOnlyOwnComponentsDelegate call_ClearOnlyOwnComponentsDelegate;

		// Token: 0x040000DB RID: 219
		public static ScriptingInterfaceOfIGameEntity.ComputeTrajectoryVolumeDelegate call_ComputeTrajectoryVolumeDelegate;

		// Token: 0x040000DC RID: 220
		public static ScriptingInterfaceOfIGameEntity.ComputeVelocityDeltaFromImpulseDelegate call_ComputeVelocityDeltaFromImpulseDelegate;

		// Token: 0x040000DD RID: 221
		public static ScriptingInterfaceOfIGameEntity.ConvertDynamicBodyToRayCastDelegate call_ConvertDynamicBodyToRayCastDelegate;

		// Token: 0x040000DE RID: 222
		public static ScriptingInterfaceOfIGameEntity.CookTrianglePhysxMeshDelegate call_CookTrianglePhysxMeshDelegate;

		// Token: 0x040000DF RID: 223
		public static ScriptingInterfaceOfIGameEntity.CopyComponentsToSkeletonDelegate call_CopyComponentsToSkeletonDelegate;

		// Token: 0x040000E0 RID: 224
		public static ScriptingInterfaceOfIGameEntity.CopyFromPrefabDelegate call_CopyFromPrefabDelegate;

		// Token: 0x040000E1 RID: 225
		public static ScriptingInterfaceOfIGameEntity.CopyScriptComponentFromAnotherEntityDelegate call_CopyScriptComponentFromAnotherEntityDelegate;

		// Token: 0x040000E2 RID: 226
		public static ScriptingInterfaceOfIGameEntity.CreateAndAddScriptComponentDelegate call_CreateAndAddScriptComponentDelegate;

		// Token: 0x040000E3 RID: 227
		public static ScriptingInterfaceOfIGameEntity.CreateEmptyDelegate call_CreateEmptyDelegate;

		// Token: 0x040000E4 RID: 228
		public static ScriptingInterfaceOfIGameEntity.CreateEmptyPhysxShapeDelegate call_CreateEmptyPhysxShapeDelegate;

		// Token: 0x040000E5 RID: 229
		public static ScriptingInterfaceOfIGameEntity.CreateEmptyWithoutSceneDelegate call_CreateEmptyWithoutSceneDelegate;

		// Token: 0x040000E6 RID: 230
		public static ScriptingInterfaceOfIGameEntity.CreateFromPrefabDelegate call_CreateFromPrefabDelegate;

		// Token: 0x040000E7 RID: 231
		public static ScriptingInterfaceOfIGameEntity.CreateFromPrefabWithInitialFrameDelegate call_CreateFromPrefabWithInitialFrameDelegate;

		// Token: 0x040000E8 RID: 232
		public static ScriptingInterfaceOfIGameEntity.CreatePhysxCookingInstanceDelegate call_CreatePhysxCookingInstanceDelegate;

		// Token: 0x040000E9 RID: 233
		public static ScriptingInterfaceOfIGameEntity.CreateVariableRatePhysicsDelegate call_CreateVariableRatePhysicsDelegate;

		// Token: 0x040000EA RID: 234
		public static ScriptingInterfaceOfIGameEntity.DeleteEmptyShapeDelegate call_DeleteEmptyShapeDelegate;

		// Token: 0x040000EB RID: 235
		public static ScriptingInterfaceOfIGameEntity.DeletePhysxCookingInstanceDelegate call_DeletePhysxCookingInstanceDelegate;

		// Token: 0x040000EC RID: 236
		public static ScriptingInterfaceOfIGameEntity.DeRegisterWaterMeshMaterialsDelegate call_DeRegisterWaterMeshMaterialsDelegate;

		// Token: 0x040000ED RID: 237
		public static ScriptingInterfaceOfIGameEntity.DeRegisterWaterSDFClipDelegate call_DeRegisterWaterSDFClipDelegate;

		// Token: 0x040000EE RID: 238
		public static ScriptingInterfaceOfIGameEntity.DeselectEntityOnEditorDelegate call_DeselectEntityOnEditorDelegate;

		// Token: 0x040000EF RID: 239
		public static ScriptingInterfaceOfIGameEntity.DetachAllAttachedNavigationMeshFacesDelegate call_DetachAllAttachedNavigationMeshFacesDelegate;

		// Token: 0x040000F0 RID: 240
		public static ScriptingInterfaceOfIGameEntity.DisableContourDelegate call_DisableContourDelegate;

		// Token: 0x040000F1 RID: 241
		public static ScriptingInterfaceOfIGameEntity.DisableDynamicBodySimulationDelegate call_DisableDynamicBodySimulationDelegate;

		// Token: 0x040000F2 RID: 242
		public static ScriptingInterfaceOfIGameEntity.DisableGravityDelegate call_DisableGravityDelegate;

		// Token: 0x040000F3 RID: 243
		public static ScriptingInterfaceOfIGameEntity.EnableDynamicBodyDelegate call_EnableDynamicBodyDelegate;

		// Token: 0x040000F4 RID: 244
		public static ScriptingInterfaceOfIGameEntity.FindWithNameDelegate call_FindWithNameDelegate;

		// Token: 0x040000F5 RID: 245
		public static ScriptingInterfaceOfIGameEntity.FreezeDelegate call_FreezeDelegate;

		// Token: 0x040000F6 RID: 246
		public static ScriptingInterfaceOfIGameEntity.GetAngularVelocityDelegate call_GetAngularVelocityDelegate;

		// Token: 0x040000F7 RID: 247
		public static ScriptingInterfaceOfIGameEntity.GetAttachedNavmeshFaceCountDelegate call_GetAttachedNavmeshFaceCountDelegate;

		// Token: 0x040000F8 RID: 248
		public static ScriptingInterfaceOfIGameEntity.GetAttachedNavmeshFaceRecordsDelegate call_GetAttachedNavmeshFaceRecordsDelegate;

		// Token: 0x040000F9 RID: 249
		public static ScriptingInterfaceOfIGameEntity.GetAttachedNavmeshFaceVertexIndicesDelegate call_GetAttachedNavmeshFaceVertexIndicesDelegate;

		// Token: 0x040000FA RID: 250
		public static ScriptingInterfaceOfIGameEntity.GetBodyFlagsDelegate call_GetBodyFlagsDelegate;

		// Token: 0x040000FB RID: 251
		public static ScriptingInterfaceOfIGameEntity.GetBodyShapeDelegate call_GetBodyShapeDelegate;

		// Token: 0x040000FC RID: 252
		public static ScriptingInterfaceOfIGameEntity.GetBodyVisualWorldTransformDelegate call_GetBodyVisualWorldTransformDelegate;

		// Token: 0x040000FD RID: 253
		public static ScriptingInterfaceOfIGameEntity.GetBodyWorldTransformDelegate call_GetBodyWorldTransformDelegate;

		// Token: 0x040000FE RID: 254
		public static ScriptingInterfaceOfIGameEntity.GetBoneCountDelegate call_GetBoneCountDelegate;

		// Token: 0x040000FF RID: 255
		public static ScriptingInterfaceOfIGameEntity.GetBoneEntitialFrameWithIndexDelegate call_GetBoneEntitialFrameWithIndexDelegate;

		// Token: 0x04000100 RID: 256
		public static ScriptingInterfaceOfIGameEntity.GetBoneEntitialFrameWithNameDelegate call_GetBoneEntitialFrameWithNameDelegate;

		// Token: 0x04000101 RID: 257
		public static ScriptingInterfaceOfIGameEntity.GetBoundingBoxMaxDelegate call_GetBoundingBoxMaxDelegate;

		// Token: 0x04000102 RID: 258
		public static ScriptingInterfaceOfIGameEntity.GetBoundingBoxMinDelegate call_GetBoundingBoxMinDelegate;

		// Token: 0x04000103 RID: 259
		public static ScriptingInterfaceOfIGameEntity.GetCameraParamsFromCameraScriptDelegate call_GetCameraParamsFromCameraScriptDelegate;

		// Token: 0x04000104 RID: 260
		public static ScriptingInterfaceOfIGameEntity.GetCenterOfMassDelegate call_GetCenterOfMassDelegate;

		// Token: 0x04000105 RID: 261
		public static ScriptingInterfaceOfIGameEntity.GetChildDelegate call_GetChildDelegate;

		// Token: 0x04000106 RID: 262
		public static ScriptingInterfaceOfIGameEntity.GetChildCountDelegate call_GetChildCountDelegate;

		// Token: 0x04000107 RID: 263
		public static ScriptingInterfaceOfIGameEntity.GetChildPointerDelegate call_GetChildPointerDelegate;

		// Token: 0x04000108 RID: 264
		public static ScriptingInterfaceOfIGameEntity.GetComponentAtIndexDelegate call_GetComponentAtIndexDelegate;

		// Token: 0x04000109 RID: 265
		public static ScriptingInterfaceOfIGameEntity.GetComponentCountDelegate call_GetComponentCountDelegate;

		// Token: 0x0400010A RID: 266
		public static ScriptingInterfaceOfIGameEntity.GetEditModeLevelVisibilityDelegate call_GetEditModeLevelVisibilityDelegate;

		// Token: 0x0400010B RID: 267
		public static ScriptingInterfaceOfIGameEntity.GetEntityFlagsDelegate call_GetEntityFlagsDelegate;

		// Token: 0x0400010C RID: 268
		public static ScriptingInterfaceOfIGameEntity.GetEntityVisibilityFlagsDelegate call_GetEntityVisibilityFlagsDelegate;

		// Token: 0x0400010D RID: 269
		public static ScriptingInterfaceOfIGameEntity.GetFactorColorDelegate call_GetFactorColorDelegate;

		// Token: 0x0400010E RID: 270
		public static ScriptingInterfaceOfIGameEntity.GetFirstChildWithTagRecursiveDelegate call_GetFirstChildWithTagRecursiveDelegate;

		// Token: 0x0400010F RID: 271
		public static ScriptingInterfaceOfIGameEntity.GetFirstEntityWithTagDelegate call_GetFirstEntityWithTagDelegate;

		// Token: 0x04000110 RID: 272
		public static ScriptingInterfaceOfIGameEntity.GetFirstEntityWithTagExpressionDelegate call_GetFirstEntityWithTagExpressionDelegate;

		// Token: 0x04000111 RID: 273
		public static ScriptingInterfaceOfIGameEntity.GetFirstMeshDelegate call_GetFirstMeshDelegate;

		// Token: 0x04000112 RID: 274
		public static ScriptingInterfaceOfIGameEntity.GetGlobalBoundingBoxDelegate call_GetGlobalBoundingBoxDelegate;

		// Token: 0x04000113 RID: 275
		public static ScriptingInterfaceOfIGameEntity.GetGlobalBoxMaxDelegate call_GetGlobalBoxMaxDelegate;

		// Token: 0x04000114 RID: 276
		public static ScriptingInterfaceOfIGameEntity.GetGlobalBoxMinDelegate call_GetGlobalBoxMinDelegate;

		// Token: 0x04000115 RID: 277
		public static ScriptingInterfaceOfIGameEntity.GetGlobalFrameDelegate call_GetGlobalFrameDelegate;

		// Token: 0x04000116 RID: 278
		public static ScriptingInterfaceOfIGameEntity.GetGlobalFrameImpreciseForFixedTickDelegate call_GetGlobalFrameImpreciseForFixedTickDelegate;

		// Token: 0x04000117 RID: 279
		public static ScriptingInterfaceOfIGameEntity.GetGlobalScaleDelegate call_GetGlobalScaleDelegate;

		// Token: 0x04000118 RID: 280
		public static ScriptingInterfaceOfIGameEntity.GetGlobalWindStrengthVectorOfSceneDelegate call_GetGlobalWindStrengthVectorOfSceneDelegate;

		// Token: 0x04000119 RID: 281
		public static ScriptingInterfaceOfIGameEntity.GetGlobalWindVelocityOfSceneDelegate call_GetGlobalWindVelocityOfSceneDelegate;

		// Token: 0x0400011A RID: 282
		public static ScriptingInterfaceOfIGameEntity.GetGlobalWindVelocityWithGustNoiseOfSceneDelegate call_GetGlobalWindVelocityWithGustNoiseOfSceneDelegate;

		// Token: 0x0400011B RID: 283
		public static ScriptingInterfaceOfIGameEntity.GetGuidDelegate call_GetGuidDelegate;

		// Token: 0x0400011C RID: 284
		public static ScriptingInterfaceOfIGameEntity.GetLastFinalRenderCameraPositionOfSceneDelegate call_GetLastFinalRenderCameraPositionOfSceneDelegate;

		// Token: 0x0400011D RID: 285
		public static ScriptingInterfaceOfIGameEntity.GetLightDelegate call_GetLightDelegate;

		// Token: 0x0400011E RID: 286
		public static ScriptingInterfaceOfIGameEntity.GetLinearVelocityDelegate call_GetLinearVelocityDelegate;

		// Token: 0x0400011F RID: 287
		public static ScriptingInterfaceOfIGameEntity.GetLocalBoundingBoxDelegate call_GetLocalBoundingBoxDelegate;

		// Token: 0x04000120 RID: 288
		public static ScriptingInterfaceOfIGameEntity.GetLocalFrameDelegate call_GetLocalFrameDelegate;

		// Token: 0x04000121 RID: 289
		public static ScriptingInterfaceOfIGameEntity.GetLocalPhysicsBoundingBoxDelegate call_GetLocalPhysicsBoundingBoxDelegate;

		// Token: 0x04000122 RID: 290
		public static ScriptingInterfaceOfIGameEntity.GetLodLevelForDistanceSqDelegate call_GetLodLevelForDistanceSqDelegate;

		// Token: 0x04000123 RID: 291
		public static ScriptingInterfaceOfIGameEntity.GetMassDelegate call_GetMassDelegate;

		// Token: 0x04000124 RID: 292
		public static ScriptingInterfaceOfIGameEntity.GetMassSpaceInertiaDelegate call_GetMassSpaceInertiaDelegate;

		// Token: 0x04000125 RID: 293
		public static ScriptingInterfaceOfIGameEntity.GetMassSpaceInverseInertiaDelegate call_GetMassSpaceInverseInertiaDelegate;

		// Token: 0x04000126 RID: 294
		public static ScriptingInterfaceOfIGameEntity.GetMeshBendedPositionDelegate call_GetMeshBendedPositionDelegate;

		// Token: 0x04000127 RID: 295
		public static ScriptingInterfaceOfIGameEntity.GetMobilityDelegate call_GetMobilityDelegate;

		// Token: 0x04000128 RID: 296
		public static ScriptingInterfaceOfIGameEntity.GetNameDelegate call_GetNameDelegate;

		// Token: 0x04000129 RID: 297
		public static ScriptingInterfaceOfIGameEntity.GetNativeScriptComponentVariableDelegate call_GetNativeScriptComponentVariableDelegate;

		// Token: 0x0400012A RID: 298
		public static ScriptingInterfaceOfIGameEntity.GetNextEntityWithTagDelegate call_GetNextEntityWithTagDelegate;

		// Token: 0x0400012B RID: 299
		public static ScriptingInterfaceOfIGameEntity.GetNextEntityWithTagExpressionDelegate call_GetNextEntityWithTagExpressionDelegate;

		// Token: 0x0400012C RID: 300
		public static ScriptingInterfaceOfIGameEntity.GetNextPrefabDelegate call_GetNextPrefabDelegate;

		// Token: 0x0400012D RID: 301
		public static ScriptingInterfaceOfIGameEntity.GetOldPrefabNameDelegate call_GetOldPrefabNameDelegate;

		// Token: 0x0400012E RID: 302
		public static ScriptingInterfaceOfIGameEntity.GetParentDelegate call_GetParentDelegate;

		// Token: 0x0400012F RID: 303
		public static ScriptingInterfaceOfIGameEntity.GetParentPointerDelegate call_GetParentPointerDelegate;

		// Token: 0x04000130 RID: 304
		public static ScriptingInterfaceOfIGameEntity.GetPhysicsDescBodyFlagsDelegate call_GetPhysicsDescBodyFlagsDelegate;

		// Token: 0x04000131 RID: 305
		public static ScriptingInterfaceOfIGameEntity.GetPhysicsMaterialIndexDelegate call_GetPhysicsMaterialIndexDelegate;

		// Token: 0x04000132 RID: 306
		public static ScriptingInterfaceOfIGameEntity.GetPhysicsMinMaxDelegate call_GetPhysicsMinMaxDelegate;

		// Token: 0x04000133 RID: 307
		public static ScriptingInterfaceOfIGameEntity.GetPhysicsStateDelegate call_GetPhysicsStateDelegate;

		// Token: 0x04000134 RID: 308
		public static ScriptingInterfaceOfIGameEntity.GetPhysicsTriangleCountDelegate call_GetPhysicsTriangleCountDelegate;

		// Token: 0x04000135 RID: 309
		public static ScriptingInterfaceOfIGameEntity.GetPrefabNameDelegate call_GetPrefabNameDelegate;

		// Token: 0x04000136 RID: 310
		public static ScriptingInterfaceOfIGameEntity.GetPreviousGlobalFrameDelegate call_GetPreviousGlobalFrameDelegate;

		// Token: 0x04000137 RID: 311
		public static ScriptingInterfaceOfIGameEntity.GetQuickBoneEntitialFrameDelegate call_GetQuickBoneEntitialFrameDelegate;

		// Token: 0x04000138 RID: 312
		public static ScriptingInterfaceOfIGameEntity.GetRadiusDelegate call_GetRadiusDelegate;

		// Token: 0x04000139 RID: 313
		public static ScriptingInterfaceOfIGameEntity.GetRootParentPointerDelegate call_GetRootParentPointerDelegate;

		// Token: 0x0400013A RID: 314
		public static ScriptingInterfaceOfIGameEntity.GetSceneDelegate call_GetSceneDelegate;

		// Token: 0x0400013B RID: 315
		public static ScriptingInterfaceOfIGameEntity.GetScenePointerDelegate call_GetScenePointerDelegate;

		// Token: 0x0400013C RID: 316
		public static ScriptingInterfaceOfIGameEntity.GetScriptComponentDelegate call_GetScriptComponentDelegate;

		// Token: 0x0400013D RID: 317
		public static ScriptingInterfaceOfIGameEntity.GetScriptComponentAtIndexDelegate call_GetScriptComponentAtIndexDelegate;

		// Token: 0x0400013E RID: 318
		public static ScriptingInterfaceOfIGameEntity.GetScriptComponentCountDelegate call_GetScriptComponentCountDelegate;

		// Token: 0x0400013F RID: 319
		public static ScriptingInterfaceOfIGameEntity.GetScriptComponentIndexDelegate call_GetScriptComponentIndexDelegate;

		// Token: 0x04000140 RID: 320
		public static ScriptingInterfaceOfIGameEntity.GetSkeletonDelegate call_GetSkeletonDelegate;

		// Token: 0x04000141 RID: 321
		public static ScriptingInterfaceOfIGameEntity.GetTagsDelegate call_GetTagsDelegate;

		// Token: 0x04000142 RID: 322
		public static ScriptingInterfaceOfIGameEntity.GetUpgradeLevelMaskDelegate call_GetUpgradeLevelMaskDelegate;

		// Token: 0x04000143 RID: 323
		public static ScriptingInterfaceOfIGameEntity.GetUpgradeLevelMaskCumulativeDelegate call_GetUpgradeLevelMaskCumulativeDelegate;

		// Token: 0x04000144 RID: 324
		public static ScriptingInterfaceOfIGameEntity.GetVisibilityExcludeParentsDelegate call_GetVisibilityExcludeParentsDelegate;

		// Token: 0x04000145 RID: 325
		public static ScriptingInterfaceOfIGameEntity.GetVisibilityLevelMaskIncludingParentsDelegate call_GetVisibilityLevelMaskIncludingParentsDelegate;

		// Token: 0x04000146 RID: 326
		public static ScriptingInterfaceOfIGameEntity.GetWaterLevelAtPositionDelegate call_GetWaterLevelAtPositionDelegate;

		// Token: 0x04000147 RID: 327
		public static ScriptingInterfaceOfIGameEntity.HasBatchedKinematicPhysicsFlagDelegate call_HasBatchedKinematicPhysicsFlagDelegate;

		// Token: 0x04000148 RID: 328
		public static ScriptingInterfaceOfIGameEntity.HasBatchedRayCastPhysicsFlagDelegate call_HasBatchedRayCastPhysicsFlagDelegate;

		// Token: 0x04000149 RID: 329
		public static ScriptingInterfaceOfIGameEntity.HasBodyDelegate call_HasBodyDelegate;

		// Token: 0x0400014A RID: 330
		public static ScriptingInterfaceOfIGameEntity.HasComplexAnimTreeDelegate call_HasComplexAnimTreeDelegate;

		// Token: 0x0400014B RID: 331
		public static ScriptingInterfaceOfIGameEntity.HasComponentDelegate call_HasComponentDelegate;

		// Token: 0x0400014C RID: 332
		public static ScriptingInterfaceOfIGameEntity.HasDynamicRigidBodyDelegate call_HasDynamicRigidBodyDelegate;

		// Token: 0x0400014D RID: 333
		public static ScriptingInterfaceOfIGameEntity.HasDynamicRigidBodyAndActiveSimulationDelegate call_HasDynamicRigidBodyAndActiveSimulationDelegate;

		// Token: 0x0400014E RID: 334
		public static ScriptingInterfaceOfIGameEntity.HasFrameChangedDelegate call_HasFrameChangedDelegate;

		// Token: 0x0400014F RID: 335
		public static ScriptingInterfaceOfIGameEntity.HasKinematicRigidBodyDelegate call_HasKinematicRigidBodyDelegate;

		// Token: 0x04000150 RID: 336
		public static ScriptingInterfaceOfIGameEntity.HasPhysicsBodyDelegate call_HasPhysicsBodyDelegate;

		// Token: 0x04000151 RID: 337
		public static ScriptingInterfaceOfIGameEntity.HasPhysicsDefinitionDelegate call_HasPhysicsDefinitionDelegate;

		// Token: 0x04000152 RID: 338
		public static ScriptingInterfaceOfIGameEntity.HasSceneDelegate call_HasSceneDelegate;

		// Token: 0x04000153 RID: 339
		public static ScriptingInterfaceOfIGameEntity.HasScriptComponentDelegate call_HasScriptComponentDelegate;

		// Token: 0x04000154 RID: 340
		public static ScriptingInterfaceOfIGameEntity.HasScriptComponentHashDelegate call_HasScriptComponentHashDelegate;

		// Token: 0x04000155 RID: 341
		public static ScriptingInterfaceOfIGameEntity.HasStaticPhysicsBodyDelegate call_HasStaticPhysicsBodyDelegate;

		// Token: 0x04000156 RID: 342
		public static ScriptingInterfaceOfIGameEntity.HasTagDelegate call_HasTagDelegate;

		// Token: 0x04000157 RID: 343
		public static ScriptingInterfaceOfIGameEntity.IsDynamicBodyStationaryDelegate call_IsDynamicBodyStationaryDelegate;

		// Token: 0x04000158 RID: 344
		public static ScriptingInterfaceOfIGameEntity.IsEngineBodySleepingDelegate call_IsEngineBodySleepingDelegate;

		// Token: 0x04000159 RID: 345
		public static ScriptingInterfaceOfIGameEntity.IsEntitySelectedOnEditorDelegate call_IsEntitySelectedOnEditorDelegate;

		// Token: 0x0400015A RID: 346
		public static ScriptingInterfaceOfIGameEntity.IsFrozenDelegate call_IsFrozenDelegate;

		// Token: 0x0400015B RID: 347
		public static ScriptingInterfaceOfIGameEntity.IsGhostObjectDelegate call_IsGhostObjectDelegate;

		// Token: 0x0400015C RID: 348
		public static ScriptingInterfaceOfIGameEntity.IsGravityDisabledDelegate call_IsGravityDisabledDelegate;

		// Token: 0x0400015D RID: 349
		public static ScriptingInterfaceOfIGameEntity.IsGuidValidDelegate call_IsGuidValidDelegate;

		// Token: 0x0400015E RID: 350
		public static ScriptingInterfaceOfIGameEntity.IsInEditorSceneDelegate call_IsInEditorSceneDelegate;

		// Token: 0x0400015F RID: 351
		public static ScriptingInterfaceOfIGameEntity.IsVisibleIncludeParentsDelegate call_IsVisibleIncludeParentsDelegate;

		// Token: 0x04000160 RID: 352
		public static ScriptingInterfaceOfIGameEntity.PauseParticleSystemDelegate call_PauseParticleSystemDelegate;

		// Token: 0x04000161 RID: 353
		public static ScriptingInterfaceOfIGameEntity.PopCapsuleShapeFromEntityBodyDelegate call_PopCapsuleShapeFromEntityBodyDelegate;

		// Token: 0x04000162 RID: 354
		public static ScriptingInterfaceOfIGameEntity.PrefabExistsDelegate call_PrefabExistsDelegate;

		// Token: 0x04000163 RID: 355
		public static ScriptingInterfaceOfIGameEntity.PushCapsuleShapeToEntityBodyDelegate call_PushCapsuleShapeToEntityBodyDelegate;

		// Token: 0x04000164 RID: 356
		public static ScriptingInterfaceOfIGameEntity.RayHitEntityDelegate call_RayHitEntityDelegate;

		// Token: 0x04000165 RID: 357
		public static ScriptingInterfaceOfIGameEntity.RayHitEntityWithNormalDelegate call_RayHitEntityWithNormalDelegate;

		// Token: 0x04000166 RID: 358
		public static ScriptingInterfaceOfIGameEntity.RecomputeBoundingBoxDelegate call_RecomputeBoundingBoxDelegate;

		// Token: 0x04000167 RID: 359
		public static ScriptingInterfaceOfIGameEntity.RefreshMeshesToRenderToHullWaterDelegate call_RefreshMeshesToRenderToHullWaterDelegate;

		// Token: 0x04000168 RID: 360
		public static ScriptingInterfaceOfIGameEntity.RegisterWaterSDFClipDelegate call_RegisterWaterSDFClipDelegate;

		// Token: 0x04000169 RID: 361
		public static ScriptingInterfaceOfIGameEntity.RelaxLocalBoundingBoxDelegate call_RelaxLocalBoundingBoxDelegate;

		// Token: 0x0400016A RID: 362
		public static ScriptingInterfaceOfIGameEntity.ReleaseEditDataUserToAllMeshesDelegate call_ReleaseEditDataUserToAllMeshesDelegate;

		// Token: 0x0400016B RID: 363
		public static ScriptingInterfaceOfIGameEntity.RemoveDelegate call_RemoveDelegate;

		// Token: 0x0400016C RID: 364
		public static ScriptingInterfaceOfIGameEntity.RemoveAllChildrenDelegate call_RemoveAllChildrenDelegate;

		// Token: 0x0400016D RID: 365
		public static ScriptingInterfaceOfIGameEntity.RemoveAllParticleSystemsDelegate call_RemoveAllParticleSystemsDelegate;

		// Token: 0x0400016E RID: 366
		public static ScriptingInterfaceOfIGameEntity.RemoveChildDelegate call_RemoveChildDelegate;

		// Token: 0x0400016F RID: 367
		public static ScriptingInterfaceOfIGameEntity.RemoveComponentDelegate call_RemoveComponentDelegate;

		// Token: 0x04000170 RID: 368
		public static ScriptingInterfaceOfIGameEntity.RemoveComponentWithMeshDelegate call_RemoveComponentWithMeshDelegate;

		// Token: 0x04000171 RID: 369
		public static ScriptingInterfaceOfIGameEntity.RemoveEnginePhysicsDelegate call_RemoveEnginePhysicsDelegate;

		// Token: 0x04000172 RID: 370
		public static ScriptingInterfaceOfIGameEntity.RemoveFromPredisplayEntityDelegate call_RemoveFromPredisplayEntityDelegate;

		// Token: 0x04000173 RID: 371
		public static ScriptingInterfaceOfIGameEntity.RemoveJointDelegate call_RemoveJointDelegate;

		// Token: 0x04000174 RID: 372
		public static ScriptingInterfaceOfIGameEntity.RemoveMultiMeshDelegate call_RemoveMultiMeshDelegate;

		// Token: 0x04000175 RID: 373
		public static ScriptingInterfaceOfIGameEntity.RemoveMultiMeshFromSkeletonDelegate call_RemoveMultiMeshFromSkeletonDelegate;

		// Token: 0x04000176 RID: 374
		public static ScriptingInterfaceOfIGameEntity.RemoveMultiMeshFromSkeletonBoneDelegate call_RemoveMultiMeshFromSkeletonBoneDelegate;

		// Token: 0x04000177 RID: 375
		public static ScriptingInterfaceOfIGameEntity.RemovePhysicsDelegate call_RemovePhysicsDelegate;

		// Token: 0x04000178 RID: 376
		public static ScriptingInterfaceOfIGameEntity.RemoveScriptComponentDelegate call_RemoveScriptComponentDelegate;

		// Token: 0x04000179 RID: 377
		public static ScriptingInterfaceOfIGameEntity.RemoveTagDelegate call_RemoveTagDelegate;

		// Token: 0x0400017A RID: 378
		public static ScriptingInterfaceOfIGameEntity.ReplacePhysicsBodyWithQuadPhysicsBodyDelegate call_ReplacePhysicsBodyWithQuadPhysicsBodyDelegate;

		// Token: 0x0400017B RID: 379
		public static ScriptingInterfaceOfIGameEntity.ResetHullWaterDelegate call_ResetHullWaterDelegate;

		// Token: 0x0400017C RID: 380
		public static ScriptingInterfaceOfIGameEntity.ResumeParticleSystemDelegate call_ResumeParticleSystemDelegate;

		// Token: 0x0400017D RID: 381
		public static ScriptingInterfaceOfIGameEntity.SelectEntityOnEditorDelegate call_SelectEntityOnEditorDelegate;

		// Token: 0x0400017E RID: 382
		public static ScriptingInterfaceOfIGameEntity.SetAlphaDelegate call_SetAlphaDelegate;

		// Token: 0x0400017F RID: 383
		public static ScriptingInterfaceOfIGameEntity.SetAngularVelocityDelegate call_SetAngularVelocityDelegate;

		// Token: 0x04000180 RID: 384
		public static ScriptingInterfaceOfIGameEntity.SetAnimationSoundActivationDelegate call_SetAnimationSoundActivationDelegate;

		// Token: 0x04000181 RID: 385
		public static ScriptingInterfaceOfIGameEntity.SetAnimTreeChannelParameterDelegate call_SetAnimTreeChannelParameterDelegate;

		// Token: 0x04000182 RID: 386
		public static ScriptingInterfaceOfIGameEntity.SetAsContourEntityDelegate call_SetAsContourEntityDelegate;

		// Token: 0x04000183 RID: 387
		public static ScriptingInterfaceOfIGameEntity.SetAsPredisplayEntityDelegate call_SetAsPredisplayEntityDelegate;

		// Token: 0x04000184 RID: 388
		public static ScriptingInterfaceOfIGameEntity.SetAsReplayEntityDelegate call_SetAsReplayEntityDelegate;

		// Token: 0x04000185 RID: 389
		public static ScriptingInterfaceOfIGameEntity.SetBodyFlagsDelegate call_SetBodyFlagsDelegate;

		// Token: 0x04000186 RID: 390
		public static ScriptingInterfaceOfIGameEntity.SetBodyFlagsRecursiveDelegate call_SetBodyFlagsRecursiveDelegate;

		// Token: 0x04000187 RID: 391
		public static ScriptingInterfaceOfIGameEntity.SetBodyShapeDelegate call_SetBodyShapeDelegate;

		// Token: 0x04000188 RID: 392
		public static ScriptingInterfaceOfIGameEntity.SetBoneFrameToAllMeshesDelegate call_SetBoneFrameToAllMeshesDelegate;

		// Token: 0x04000189 RID: 393
		public static ScriptingInterfaceOfIGameEntity.SetBoundingboxDirtyDelegate call_SetBoundingboxDirtyDelegate;

		// Token: 0x0400018A RID: 394
		public static ScriptingInterfaceOfIGameEntity.SetCenterOfMassDelegate call_SetCenterOfMassDelegate;

		// Token: 0x0400018B RID: 395
		public static ScriptingInterfaceOfIGameEntity.SetClothComponentKeepStateDelegate call_SetClothComponentKeepStateDelegate;

		// Token: 0x0400018C RID: 396
		public static ScriptingInterfaceOfIGameEntity.SetClothComponentKeepStateOfAllMeshesDelegate call_SetClothComponentKeepStateOfAllMeshesDelegate;

		// Token: 0x0400018D RID: 397
		public static ScriptingInterfaceOfIGameEntity.SetClothMaxDistanceMultiplierDelegate call_SetClothMaxDistanceMultiplierDelegate;

		// Token: 0x0400018E RID: 398
		public static ScriptingInterfaceOfIGameEntity.SetColorToAllMeshesWithTagRecursiveDelegate call_SetColorToAllMeshesWithTagRecursiveDelegate;

		// Token: 0x0400018F RID: 399
		public static ScriptingInterfaceOfIGameEntity.SetContourStateDelegate call_SetContourStateDelegate;

		// Token: 0x04000190 RID: 400
		public static ScriptingInterfaceOfIGameEntity.SetCostAdderForAttachedFacesDelegate call_SetCostAdderForAttachedFacesDelegate;

		// Token: 0x04000191 RID: 401
		public static ScriptingInterfaceOfIGameEntity.SetCullModeDelegate call_SetCullModeDelegate;

		// Token: 0x04000192 RID: 402
		public static ScriptingInterfaceOfIGameEntity.SetCustomClipPlaneDelegate call_SetCustomClipPlaneDelegate;

		// Token: 0x04000193 RID: 403
		public static ScriptingInterfaceOfIGameEntity.SetCustomVertexPositionEnabledDelegate call_SetCustomVertexPositionEnabledDelegate;

		// Token: 0x04000194 RID: 404
		public static ScriptingInterfaceOfIGameEntity.SetDampingDelegate call_SetDampingDelegate;

		// Token: 0x04000195 RID: 405
		public static ScriptingInterfaceOfIGameEntity.SetDoNotCheckVisibilityDelegate call_SetDoNotCheckVisibilityDelegate;

		// Token: 0x04000196 RID: 406
		public static ScriptingInterfaceOfIGameEntity.SetEnforcedMaximumLodLevelDelegate call_SetEnforcedMaximumLodLevelDelegate;

		// Token: 0x04000197 RID: 407
		public static ScriptingInterfaceOfIGameEntity.SetEntityEnvMapVisibilityDelegate call_SetEntityEnvMapVisibilityDelegate;

		// Token: 0x04000198 RID: 408
		public static ScriptingInterfaceOfIGameEntity.SetEntityFlagsDelegate call_SetEntityFlagsDelegate;

		// Token: 0x04000199 RID: 409
		public static ScriptingInterfaceOfIGameEntity.SetEntityVisibilityFlagsDelegate call_SetEntityVisibilityFlagsDelegate;

		// Token: 0x0400019A RID: 410
		public static ScriptingInterfaceOfIGameEntity.SetExternalReferencesUsageDelegate call_SetExternalReferencesUsageDelegate;

		// Token: 0x0400019B RID: 411
		public static ScriptingInterfaceOfIGameEntity.SetFactor2ColorDelegate call_SetFactor2ColorDelegate;

		// Token: 0x0400019C RID: 412
		public static ScriptingInterfaceOfIGameEntity.SetFactorColorDelegate call_SetFactorColorDelegate;

		// Token: 0x0400019D RID: 413
		public static ScriptingInterfaceOfIGameEntity.SetForceDecalsToRenderDelegate call_SetForceDecalsToRenderDelegate;

		// Token: 0x0400019E RID: 414
		public static ScriptingInterfaceOfIGameEntity.SetForceNotAffectedBySeasonDelegate call_SetForceNotAffectedBySeasonDelegate;

		// Token: 0x0400019F RID: 415
		public static ScriptingInterfaceOfIGameEntity.SetFrameChangedDelegate call_SetFrameChangedDelegate;

		// Token: 0x040001A0 RID: 416
		public static ScriptingInterfaceOfIGameEntity.SetGlobalFrameDelegate call_SetGlobalFrameDelegate;

		// Token: 0x040001A1 RID: 417
		public static ScriptingInterfaceOfIGameEntity.SetGlobalPositionDelegate call_SetGlobalPositionDelegate;

		// Token: 0x040001A2 RID: 418
		public static ScriptingInterfaceOfIGameEntity.SetHasCustomBoundingBoxValidationSystemDelegate call_SetHasCustomBoundingBoxValidationSystemDelegate;

		// Token: 0x040001A3 RID: 419
		public static ScriptingInterfaceOfIGameEntity.SetLinearVelocityDelegate call_SetLinearVelocityDelegate;

		// Token: 0x040001A4 RID: 420
		public static ScriptingInterfaceOfIGameEntity.SetLocalFrameDelegate call_SetLocalFrameDelegate;

		// Token: 0x040001A5 RID: 421
		public static ScriptingInterfaceOfIGameEntity.SetLocalPositionDelegate call_SetLocalPositionDelegate;

		// Token: 0x040001A6 RID: 422
		public static ScriptingInterfaceOfIGameEntity.SetManualGlobalBoundingBoxDelegate call_SetManualGlobalBoundingBoxDelegate;

		// Token: 0x040001A7 RID: 423
		public static ScriptingInterfaceOfIGameEntity.SetManualLocalBoundingBoxDelegate call_SetManualLocalBoundingBoxDelegate;

		// Token: 0x040001A8 RID: 424
		public static ScriptingInterfaceOfIGameEntity.SetMassAndUpdateInertiaAndCenterOfMassDelegate call_SetMassAndUpdateInertiaAndCenterOfMassDelegate;

		// Token: 0x040001A9 RID: 425
		public static ScriptingInterfaceOfIGameEntity.SetMassSpaceInertiaDelegate call_SetMassSpaceInertiaDelegate;

		// Token: 0x040001AA RID: 426
		public static ScriptingInterfaceOfIGameEntity.SetMaterialForAllMeshesDelegate call_SetMaterialForAllMeshesDelegate;

		// Token: 0x040001AB RID: 427
		public static ScriptingInterfaceOfIGameEntity.SetMaxDepenetrationVelocityDelegate call_SetMaxDepenetrationVelocityDelegate;

		// Token: 0x040001AC RID: 428
		public static ScriptingInterfaceOfIGameEntity.SetMobilityDelegate call_SetMobilityDelegate;

		// Token: 0x040001AD RID: 429
		public static ScriptingInterfaceOfIGameEntity.SetMorphFrameOfComponentsDelegate call_SetMorphFrameOfComponentsDelegate;

		// Token: 0x040001AE RID: 430
		public static ScriptingInterfaceOfIGameEntity.SetNameDelegate call_SetNameDelegate;

		// Token: 0x040001AF RID: 431
		public static ScriptingInterfaceOfIGameEntity.SetNativeScriptComponentVariableDelegate call_SetNativeScriptComponentVariableDelegate;

		// Token: 0x040001B0 RID: 432
		public static ScriptingInterfaceOfIGameEntity.SetPhysicsMoveToBatchedDelegate call_SetPhysicsMoveToBatchedDelegate;

		// Token: 0x040001B1 RID: 433
		public static ScriptingInterfaceOfIGameEntity.SetPhysicsStateDelegate call_SetPhysicsStateDelegate;

		// Token: 0x040001B2 RID: 434
		public static ScriptingInterfaceOfIGameEntity.SetPhysicsStateOnlyVariableDelegate call_SetPhysicsStateOnlyVariableDelegate;

		// Token: 0x040001B3 RID: 435
		public static ScriptingInterfaceOfIGameEntity.SetPositionsForAttachedNavmeshVerticesDelegate call_SetPositionsForAttachedNavmeshVerticesDelegate;

		// Token: 0x040001B4 RID: 436
		public static ScriptingInterfaceOfIGameEntity.SetPreviousFrameInvalidDelegate call_SetPreviousFrameInvalidDelegate;

		// Token: 0x040001B5 RID: 437
		public static ScriptingInterfaceOfIGameEntity.SetReadyToRenderDelegate call_SetReadyToRenderDelegate;

		// Token: 0x040001B6 RID: 438
		public static ScriptingInterfaceOfIGameEntity.SetRuntimeEmissionRateMultiplierDelegate call_SetRuntimeEmissionRateMultiplierDelegate;

		// Token: 0x040001B7 RID: 439
		public static ScriptingInterfaceOfIGameEntity.SetSkeletonDelegate call_SetSkeletonDelegate;

		// Token: 0x040001B8 RID: 440
		public static ScriptingInterfaceOfIGameEntity.SetSolverIterationCountsDelegate call_SetSolverIterationCountsDelegate;

		// Token: 0x040001B9 RID: 441
		public static ScriptingInterfaceOfIGameEntity.SetupAdditionalBoneBufferForMeshesDelegate call_SetupAdditionalBoneBufferForMeshesDelegate;

		// Token: 0x040001BA RID: 442
		public static ScriptingInterfaceOfIGameEntity.SetUpdateValidityOnFrameChangedOfFacesWithIdDelegate call_SetUpdateValidityOnFrameChangedOfFacesWithIdDelegate;

		// Token: 0x040001BB RID: 443
		public static ScriptingInterfaceOfIGameEntity.SetUpgradeLevelMaskDelegate call_SetUpgradeLevelMaskDelegate;

		// Token: 0x040001BC RID: 444
		public static ScriptingInterfaceOfIGameEntity.SetVectorArgumentDelegate call_SetVectorArgumentDelegate;

		// Token: 0x040001BD RID: 445
		public static ScriptingInterfaceOfIGameEntity.SetVelocityLimitsDelegate call_SetVelocityLimitsDelegate;

		// Token: 0x040001BE RID: 446
		public static ScriptingInterfaceOfIGameEntity.SetVisibilityExcludeParentsDelegate call_SetVisibilityExcludeParentsDelegate;

		// Token: 0x040001BF RID: 447
		public static ScriptingInterfaceOfIGameEntity.SetVisualRecordWakeParamsDelegate call_SetVisualRecordWakeParamsDelegate;

		// Token: 0x040001C0 RID: 448
		public static ScriptingInterfaceOfIGameEntity.SetWaterSDFClipDataDelegate call_SetWaterSDFClipDataDelegate;

		// Token: 0x040001C1 RID: 449
		public static ScriptingInterfaceOfIGameEntity.SetWaterVisualRecordFrameAndDtDelegate call_SetWaterVisualRecordFrameAndDtDelegate;

		// Token: 0x040001C2 RID: 450
		public static ScriptingInterfaceOfIGameEntity.SwapPhysxShapeInEntityDelegate call_SwapPhysxShapeInEntityDelegate;

		// Token: 0x040001C3 RID: 451
		public static ScriptingInterfaceOfIGameEntity.UpdateAttachedNavigationMeshFacesDelegate call_UpdateAttachedNavigationMeshFacesDelegate;

		// Token: 0x040001C4 RID: 452
		public static ScriptingInterfaceOfIGameEntity.UpdateGlobalBoundsDelegate call_UpdateGlobalBoundsDelegate;

		// Token: 0x040001C5 RID: 453
		public static ScriptingInterfaceOfIGameEntity.UpdateHullWaterEffectFramesDelegate call_UpdateHullWaterEffectFramesDelegate;

		// Token: 0x040001C6 RID: 454
		public static ScriptingInterfaceOfIGameEntity.UpdateTriadFrameForEditorDelegate call_UpdateTriadFrameForEditorDelegate;

		// Token: 0x040001C7 RID: 455
		public static ScriptingInterfaceOfIGameEntity.UpdateVisibilityMaskDelegate call_UpdateVisibilityMaskDelegate;

		// Token: 0x040001C8 RID: 456
		public static ScriptingInterfaceOfIGameEntity.ValidateBoundingBoxDelegate call_ValidateBoundingBoxDelegate;

		// Token: 0x02000133 RID: 307
		// (Invoke) Token: 0x06000ADF RID: 2783
		[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
		[SuppressUnmanagedCodeSecurity]
		[MonoNativeFunctionWrapper]
		public delegate void ActivateRagdollDelegate(UIntPtr entityId);

		// Token: 0x02000134 RID: 308
		// (Invoke) Token: 0x06000AE3 RID: 2787
		[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
		[SuppressUnmanagedCodeSecurity]
		[MonoNativeFunctionWrapper]
		public delegate void AddAllMeshesOfGameEntityDelegate(UIntPtr entityId, UIntPtr copiedEntityId);

		// Token: 0x02000135 RID: 309
		// (Invoke) Token: 0x06000AE7 RID: 2791
		[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
		[SuppressUnmanagedCodeSecurity]
		[MonoNativeFunctionWrapper]
		public delegate void AddCapsuleAsBodyDelegate(UIntPtr entityId, Vec3 p1, Vec3 p2, float radius, uint bodyFlags, byte[] physicsMaterialName);

		// Token: 0x02000136 RID: 310
		// (Invoke) Token: 0x06000AEB RID: 2795
		[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
		[SuppressUnmanagedCodeSecurity]
		[MonoNativeFunctionWrapper]
		public delegate void AddChildDelegate(UIntPtr parententity, UIntPtr childentity, [MarshalAs(UnmanagedType.U1)] bool autoLocalizeFrame);

		// Token: 0x02000137 RID: 311
		// (Invoke) Token: 0x06000AEF RID: 2799
		[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
		[SuppressUnmanagedCodeSecurity]
		[MonoNativeFunctionWrapper]
		public delegate void AddComponentDelegate(UIntPtr pointer, UIntPtr componentPointer);

		// Token: 0x02000138 RID: 312
		// (Invoke) Token: 0x06000AF3 RID: 2803
		[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
		[SuppressUnmanagedCodeSecurity]
		[MonoNativeFunctionWrapper]
		public delegate UIntPtr AddDistanceJointDelegate(UIntPtr entityId, UIntPtr otherEntityId, float minDistance, float maxDistance);

		// Token: 0x02000139 RID: 313
		// (Invoke) Token: 0x06000AF7 RID: 2807
		[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
		[SuppressUnmanagedCodeSecurity]
		[MonoNativeFunctionWrapper]
		public delegate UIntPtr AddDistanceJointWithFramesDelegate(UIntPtr entityId, UIntPtr otherEntityId, MatrixFrame globalFrameOnA, MatrixFrame globalFrameOnB, float minDistance, float maxDistance);

		// Token: 0x0200013A RID: 314
		// (Invoke) Token: 0x06000AFB RID: 2811
		[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
		[SuppressUnmanagedCodeSecurity]
		[MonoNativeFunctionWrapper]
		public delegate void AddEditDataUserToAllMeshesDelegate(UIntPtr entityId, [MarshalAs(UnmanagedType.U1)] bool entity_components, [MarshalAs(UnmanagedType.U1)] bool skeleton_components);

		// Token: 0x0200013B RID: 315
		// (Invoke) Token: 0x06000AFF RID: 2815
		[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
		[SuppressUnmanagedCodeSecurity]
		[MonoNativeFunctionWrapper]
		[return: MarshalAs(UnmanagedType.U1)]
		public delegate bool AddLightDelegate(UIntPtr entityId, UIntPtr lightPointer);

		// Token: 0x0200013C RID: 316
		// (Invoke) Token: 0x06000B03 RID: 2819
		[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
		[SuppressUnmanagedCodeSecurity]
		[MonoNativeFunctionWrapper]
		public delegate void AddMeshDelegate(UIntPtr entityId, UIntPtr mesh, [MarshalAs(UnmanagedType.U1)] bool recomputeBoundingBox);

		// Token: 0x0200013D RID: 317
		// (Invoke) Token: 0x06000B07 RID: 2823
		[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
		[SuppressUnmanagedCodeSecurity]
		[MonoNativeFunctionWrapper]
		public delegate void AddMeshToBoneDelegate(UIntPtr entityId, UIntPtr multiMeshPointer, sbyte boneIndex);

		// Token: 0x0200013E RID: 318
		// (Invoke) Token: 0x06000B0B RID: 2827
		[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
		[SuppressUnmanagedCodeSecurity]
		[MonoNativeFunctionWrapper]
		public delegate void AddMultiMeshDelegate(UIntPtr entityId, UIntPtr multiMeshPtr, [MarshalAs(UnmanagedType.U1)] bool updateVisMask);

		// Token: 0x0200013F RID: 319
		// (Invoke) Token: 0x06000B0F RID: 2831
		[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
		[SuppressUnmanagedCodeSecurity]
		[MonoNativeFunctionWrapper]
		public delegate void AddMultiMeshToSkeletonDelegate(UIntPtr gameEntity, UIntPtr multiMesh);

		// Token: 0x02000140 RID: 320
		// (Invoke) Token: 0x06000B13 RID: 2835
		[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
		[SuppressUnmanagedCodeSecurity]
		[MonoNativeFunctionWrapper]
		public delegate void AddMultiMeshToSkeletonBoneDelegate(UIntPtr gameEntity, UIntPtr multiMesh, sbyte boneIndex);

		// Token: 0x02000141 RID: 321
		// (Invoke) Token: 0x06000B17 RID: 2839
		[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
		[SuppressUnmanagedCodeSecurity]
		[MonoNativeFunctionWrapper]
		public delegate void AddParticleSystemComponentDelegate(UIntPtr entityId, byte[] particleid);

		// Token: 0x02000142 RID: 322
		// (Invoke) Token: 0x06000B1B RID: 2843
		[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
		[SuppressUnmanagedCodeSecurity]
		[MonoNativeFunctionWrapper]
		public delegate void AddPhysicsDelegate(UIntPtr entityId, UIntPtr body, float mass, ref Vec3 localCenterOfMass, ref Vec3 initialGlobalVelocity, ref Vec3 initialAngularGlobalVelocity, int physicsMaterial, [MarshalAs(UnmanagedType.U1)] bool isStatic, int collisionGroupID);

		// Token: 0x02000143 RID: 323
		// (Invoke) Token: 0x06000B1F RID: 2847
		[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
		[SuppressUnmanagedCodeSecurity]
		[MonoNativeFunctionWrapper]
		public delegate void AddSphereAsBodyDelegate(UIntPtr entityId, Vec3 center, float radius, uint bodyFlags);

		// Token: 0x02000144 RID: 324
		// (Invoke) Token: 0x06000B23 RID: 2851
		[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
		[SuppressUnmanagedCodeSecurity]
		[MonoNativeFunctionWrapper]
		public delegate void AddSplashPositionToWaterVisualRecordDelegate(UIntPtr entityPointer, UIntPtr visualPrefab, in Vec3 position);

		// Token: 0x02000145 RID: 325
		// (Invoke) Token: 0x06000B27 RID: 2855
		[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
		[SuppressUnmanagedCodeSecurity]
		[MonoNativeFunctionWrapper]
		public delegate void AddTagDelegate(UIntPtr entityId, byte[] tag);

		// Token: 0x02000146 RID: 326
		// (Invoke) Token: 0x06000B2B RID: 2859
		[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
		[SuppressUnmanagedCodeSecurity]
		[MonoNativeFunctionWrapper]
		public delegate void ApplyAccelerationToDynamicBodyDelegate(UIntPtr entityId, ref Vec3 acceleration);

		// Token: 0x02000147 RID: 327
		// (Invoke) Token: 0x06000B2F RID: 2863
		[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
		[SuppressUnmanagedCodeSecurity]
		[MonoNativeFunctionWrapper]
		public delegate void ApplyForceToDynamicBodyDelegate(UIntPtr entityId, ref Vec3 force, GameEntityPhysicsExtensions.ForceMode forceMode);

		// Token: 0x02000148 RID: 328
		// (Invoke) Token: 0x06000B33 RID: 2867
		[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
		[SuppressUnmanagedCodeSecurity]
		[MonoNativeFunctionWrapper]
		public delegate void ApplyGlobalForceAtLocalPosToDynamicBodyDelegate(UIntPtr entityId, ref Vec3 localPosition, ref Vec3 globalForce, GameEntityPhysicsExtensions.ForceMode forceMode);

		// Token: 0x02000149 RID: 329
		// (Invoke) Token: 0x06000B37 RID: 2871
		[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
		[SuppressUnmanagedCodeSecurity]
		[MonoNativeFunctionWrapper]
		public delegate void ApplyLocalForceAtLocalPosToDynamicBodyDelegate(UIntPtr entityId, ref Vec3 localPosition, ref Vec3 localForce, GameEntityPhysicsExtensions.ForceMode forceMode);

		// Token: 0x0200014A RID: 330
		// (Invoke) Token: 0x06000B3B RID: 2875
		[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
		[SuppressUnmanagedCodeSecurity]
		[MonoNativeFunctionWrapper]
		public delegate void ApplyLocalImpulseToDynamicBodyDelegate(UIntPtr entityId, ref Vec3 localPosition, ref Vec3 impulse);

		// Token: 0x0200014B RID: 331
		// (Invoke) Token: 0x06000B3F RID: 2879
		[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
		[SuppressUnmanagedCodeSecurity]
		[MonoNativeFunctionWrapper]
		public delegate void ApplyTorqueToDynamicBodyDelegate(UIntPtr entityId, ref Vec3 torque, GameEntityPhysicsExtensions.ForceMode forceMode);

		// Token: 0x0200014C RID: 332
		// (Invoke) Token: 0x06000B43 RID: 2883
		[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
		[SuppressUnmanagedCodeSecurity]
		[MonoNativeFunctionWrapper]
		public delegate void AttachNavigationMeshFacesDelegate(UIntPtr entityId, int faceGroupId, [MarshalAs(UnmanagedType.U1)] bool isConnected, [MarshalAs(UnmanagedType.U1)] bool isBlocker, [MarshalAs(UnmanagedType.U1)] bool autoLocalize, [MarshalAs(UnmanagedType.U1)] bool finalizeBlockerConvexHullComputation, [MarshalAs(UnmanagedType.U1)] bool updateEntityFrame);

		// Token: 0x0200014D RID: 333
		// (Invoke) Token: 0x06000B47 RID: 2887
		[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
		[SuppressUnmanagedCodeSecurity]
		[MonoNativeFunctionWrapper]
		public delegate void BreakPrefabDelegate(UIntPtr entityId);

		// Token: 0x0200014E RID: 334
		// (Invoke) Token: 0x06000B4B RID: 2891
		[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
		[SuppressUnmanagedCodeSecurity]
		[MonoNativeFunctionWrapper]
		public delegate void BurstEntityParticleDelegate(UIntPtr entityId, [MarshalAs(UnmanagedType.U1)] bool doChildren);

		// Token: 0x0200014F RID: 335
		// (Invoke) Token: 0x06000B4F RID: 2895
		[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
		[SuppressUnmanagedCodeSecurity]
		[MonoNativeFunctionWrapper]
		public delegate void CallScriptCallbacksDelegate(UIntPtr entityPointer, [MarshalAs(UnmanagedType.U1)] bool registerScriptComponents);

		// Token: 0x02000150 RID: 336
		// (Invoke) Token: 0x06000B53 RID: 2899
		[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
		[SuppressUnmanagedCodeSecurity]
		[MonoNativeFunctionWrapper]
		public delegate void ChangeMetaMeshOrRemoveItIfNotExistsDelegate(UIntPtr entityId, UIntPtr entityMetaMeshPointer, UIntPtr newMetaMeshPointer);

		// Token: 0x02000151 RID: 337
		// (Invoke) Token: 0x06000B57 RID: 2903
		[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
		[SuppressUnmanagedCodeSecurity]
		[MonoNativeFunctionWrapper]
		public delegate void ChangeResolutionMultiplierOfWaterVisualDelegate(UIntPtr visualPrefab, float multiplier, in Vec3 waterEffectsBB);

		// Token: 0x02000152 RID: 338
		// (Invoke) Token: 0x06000B5B RID: 2907
		[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
		[SuppressUnmanagedCodeSecurity]
		[MonoNativeFunctionWrapper]
		[return: MarshalAs(UnmanagedType.U1)]
		public delegate bool CheckIsPrefabLinkRootPrefabDelegate(UIntPtr entityPtr, int depth);

		// Token: 0x02000153 RID: 339
		// (Invoke) Token: 0x06000B5F RID: 2911
		[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
		[SuppressUnmanagedCodeSecurity]
		[MonoNativeFunctionWrapper]
		[return: MarshalAs(UnmanagedType.U1)]
		public delegate bool CheckPointWithOrientedBoundingBoxDelegate(UIntPtr entityId, Vec3 point);

		// Token: 0x02000154 RID: 340
		// (Invoke) Token: 0x06000B63 RID: 2915
		[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
		[SuppressUnmanagedCodeSecurity]
		[MonoNativeFunctionWrapper]
		[return: MarshalAs(UnmanagedType.U1)]
		public delegate bool CheckResourcesDelegate(UIntPtr entityId, [MarshalAs(UnmanagedType.U1)] bool addToQueue, [MarshalAs(UnmanagedType.U1)] bool checkFaceResources);

		// Token: 0x02000155 RID: 341
		// (Invoke) Token: 0x06000B67 RID: 2919
		[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
		[SuppressUnmanagedCodeSecurity]
		[MonoNativeFunctionWrapper]
		public delegate void ClearComponentsDelegate(UIntPtr entityId);

		// Token: 0x02000156 RID: 342
		// (Invoke) Token: 0x06000B6B RID: 2923
		[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
		[SuppressUnmanagedCodeSecurity]
		[MonoNativeFunctionWrapper]
		public delegate void ClearEntityComponentsDelegate(UIntPtr entityId, [MarshalAs(UnmanagedType.U1)] bool resetAll, [MarshalAs(UnmanagedType.U1)] bool removeScripts, [MarshalAs(UnmanagedType.U1)] bool deleteChildEntities);

		// Token: 0x02000157 RID: 343
		// (Invoke) Token: 0x06000B6F RID: 2927
		[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
		[SuppressUnmanagedCodeSecurity]
		[MonoNativeFunctionWrapper]
		public delegate void ClearOnlyOwnComponentsDelegate(UIntPtr entityId);

		// Token: 0x02000158 RID: 344
		// (Invoke) Token: 0x06000B73 RID: 2931
		[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
		[SuppressUnmanagedCodeSecurity]
		[MonoNativeFunctionWrapper]
		public delegate void ComputeTrajectoryVolumeDelegate(UIntPtr gameEntity, float missileSpeed, float verticalAngleMaxInDegrees, float verticalAngleMinInDegrees, float horizontalAngleRangeInDegrees, float airFrictionConstant);

		// Token: 0x02000159 RID: 345
		// (Invoke) Token: 0x06000B77 RID: 2935
		[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
		[SuppressUnmanagedCodeSecurity]
		[MonoNativeFunctionWrapper]
		public delegate void ComputeVelocityDeltaFromImpulseDelegate(UIntPtr entityPtr, in Vec3 impulsiveForce, in Vec3 impulsiveTorque, out Vec3 deltaLinearVelocity, out Vec3 deltaAngularVelocity);

		// Token: 0x0200015A RID: 346
		// (Invoke) Token: 0x06000B7B RID: 2939
		[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
		[SuppressUnmanagedCodeSecurity]
		[MonoNativeFunctionWrapper]
		public delegate void ConvertDynamicBodyToRayCastDelegate(UIntPtr entityId);

		// Token: 0x0200015B RID: 347
		// (Invoke) Token: 0x06000B7F RID: 2943
		[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
		[SuppressUnmanagedCodeSecurity]
		[MonoNativeFunctionWrapper]
		public delegate void CookTrianglePhysxMeshDelegate(UIntPtr cookingInstancePointer, UIntPtr shapePointer, UIntPtr quadPinnedPointer, int physicsMaterial, int numberOfVertices, UIntPtr indicesPinnedPointer, int numberOfIndices);

		// Token: 0x0200015C RID: 348
		// (Invoke) Token: 0x06000B83 RID: 2947
		[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
		[SuppressUnmanagedCodeSecurity]
		[MonoNativeFunctionWrapper]
		public delegate void CopyComponentsToSkeletonDelegate(UIntPtr entityId);

		// Token: 0x0200015D RID: 349
		// (Invoke) Token: 0x06000B87 RID: 2951
		[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
		[SuppressUnmanagedCodeSecurity]
		[MonoNativeFunctionWrapper]
		public delegate NativeObjectPointer CopyFromPrefabDelegate(UIntPtr prefab);

		// Token: 0x0200015E RID: 350
		// (Invoke) Token: 0x06000B8B RID: 2955
		[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
		[SuppressUnmanagedCodeSecurity]
		[MonoNativeFunctionWrapper]
		public delegate void CopyScriptComponentFromAnotherEntityDelegate(UIntPtr prefab, UIntPtr other_prefab, byte[] script_name);

		// Token: 0x0200015F RID: 351
		// (Invoke) Token: 0x06000B8F RID: 2959
		[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
		[SuppressUnmanagedCodeSecurity]
		[MonoNativeFunctionWrapper]
		public delegate void CreateAndAddScriptComponentDelegate(UIntPtr entityId, byte[] name, [MarshalAs(UnmanagedType.U1)] bool callScriptCallbacks);

		// Token: 0x02000160 RID: 352
		// (Invoke) Token: 0x06000B93 RID: 2963
		[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
		[SuppressUnmanagedCodeSecurity]
		[MonoNativeFunctionWrapper]
		public delegate NativeObjectPointer CreateEmptyDelegate(UIntPtr scenePointer, [MarshalAs(UnmanagedType.U1)] bool isModifiableFromEditor, UIntPtr entityId, [MarshalAs(UnmanagedType.U1)] bool createPhysics, [MarshalAs(UnmanagedType.U1)] bool callScriptCallbacks);

		// Token: 0x02000161 RID: 353
		// (Invoke) Token: 0x06000B97 RID: 2967
		[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
		[SuppressUnmanagedCodeSecurity]
		[MonoNativeFunctionWrapper]
		public delegate UIntPtr CreateEmptyPhysxShapeDelegate(UIntPtr entityPointer, [MarshalAs(UnmanagedType.U1)] bool isVariable, int physxMaterialIndex);

		// Token: 0x02000162 RID: 354
		// (Invoke) Token: 0x06000B9B RID: 2971
		[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
		[SuppressUnmanagedCodeSecurity]
		[MonoNativeFunctionWrapper]
		public delegate NativeObjectPointer CreateEmptyWithoutSceneDelegate();

		// Token: 0x02000163 RID: 355
		// (Invoke) Token: 0x06000B9F RID: 2975
		[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
		[SuppressUnmanagedCodeSecurity]
		[MonoNativeFunctionWrapper]
		public delegate NativeObjectPointer CreateFromPrefabDelegate(UIntPtr scenePointer, byte[] prefabid, [MarshalAs(UnmanagedType.U1)] bool callScriptCallbacks, [MarshalAs(UnmanagedType.U1)] bool createPhysics, uint scriptInclusionHashTag);

		// Token: 0x02000164 RID: 356
		// (Invoke) Token: 0x06000BA3 RID: 2979
		[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
		[SuppressUnmanagedCodeSecurity]
		[MonoNativeFunctionWrapper]
		public delegate NativeObjectPointer CreateFromPrefabWithInitialFrameDelegate(UIntPtr scenePointer, byte[] prefabid, ref MatrixFrame frame, [MarshalAs(UnmanagedType.U1)] bool callScriptCallbacks);

		// Token: 0x02000165 RID: 357
		// (Invoke) Token: 0x06000BA7 RID: 2983
		[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
		[SuppressUnmanagedCodeSecurity]
		[MonoNativeFunctionWrapper]
		public delegate UIntPtr CreatePhysxCookingInstanceDelegate();

		// Token: 0x02000166 RID: 358
		// (Invoke) Token: 0x06000BAB RID: 2987
		[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
		[SuppressUnmanagedCodeSecurity]
		[MonoNativeFunctionWrapper]
		public delegate void CreateVariableRatePhysicsDelegate(UIntPtr entityId, [MarshalAs(UnmanagedType.U1)] bool forChildren);

		// Token: 0x02000167 RID: 359
		// (Invoke) Token: 0x06000BAF RID: 2991
		[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
		[SuppressUnmanagedCodeSecurity]
		[MonoNativeFunctionWrapper]
		public delegate void DeleteEmptyShapeDelegate(UIntPtr entity, UIntPtr shape1, UIntPtr shape2);

		// Token: 0x02000168 RID: 360
		// (Invoke) Token: 0x06000BB3 RID: 2995
		[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
		[SuppressUnmanagedCodeSecurity]
		[MonoNativeFunctionWrapper]
		public delegate void DeletePhysxCookingInstanceDelegate(UIntPtr pointer);

		// Token: 0x02000169 RID: 361
		// (Invoke) Token: 0x06000BB7 RID: 2999
		[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
		[SuppressUnmanagedCodeSecurity]
		[MonoNativeFunctionWrapper]
		public delegate void DeRegisterWaterMeshMaterialsDelegate(UIntPtr entityPointer, UIntPtr visualPrefab);

		// Token: 0x0200016A RID: 362
		// (Invoke) Token: 0x06000BBB RID: 3003
		[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
		[SuppressUnmanagedCodeSecurity]
		[MonoNativeFunctionWrapper]
		public delegate void DeRegisterWaterSDFClipDelegate(UIntPtr entityId, int slot);

		// Token: 0x0200016B RID: 363
		// (Invoke) Token: 0x06000BBF RID: 3007
		[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
		[SuppressUnmanagedCodeSecurity]
		[MonoNativeFunctionWrapper]
		public delegate void DeselectEntityOnEditorDelegate(UIntPtr entityId);

		// Token: 0x0200016C RID: 364
		// (Invoke) Token: 0x06000BC3 RID: 3011
		[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
		[SuppressUnmanagedCodeSecurity]
		[MonoNativeFunctionWrapper]
		public delegate void DetachAllAttachedNavigationMeshFacesDelegate(UIntPtr entityId);

		// Token: 0x0200016D RID: 365
		// (Invoke) Token: 0x06000BC7 RID: 3015
		[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
		[SuppressUnmanagedCodeSecurity]
		[MonoNativeFunctionWrapper]
		public delegate void DisableContourDelegate(UIntPtr entityId);

		// Token: 0x0200016E RID: 366
		// (Invoke) Token: 0x06000BCB RID: 3019
		[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
		[SuppressUnmanagedCodeSecurity]
		[MonoNativeFunctionWrapper]
		public delegate void DisableDynamicBodySimulationDelegate(UIntPtr entityId);

		// Token: 0x0200016F RID: 367
		// (Invoke) Token: 0x06000BCF RID: 3023
		[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
		[SuppressUnmanagedCodeSecurity]
		[MonoNativeFunctionWrapper]
		public delegate void DisableGravityDelegate(UIntPtr entityId);

		// Token: 0x02000170 RID: 368
		// (Invoke) Token: 0x06000BD3 RID: 3027
		[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
		[SuppressUnmanagedCodeSecurity]
		[MonoNativeFunctionWrapper]
		public delegate void EnableDynamicBodyDelegate(UIntPtr entityId);

		// Token: 0x02000171 RID: 369
		// (Invoke) Token: 0x06000BD7 RID: 3031
		[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
		[SuppressUnmanagedCodeSecurity]
		[MonoNativeFunctionWrapper]
		public delegate NativeObjectPointer FindWithNameDelegate(UIntPtr scenePointer, byte[] name);

		// Token: 0x02000172 RID: 370
		// (Invoke) Token: 0x06000BDB RID: 3035
		[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
		[SuppressUnmanagedCodeSecurity]
		[MonoNativeFunctionWrapper]
		public delegate void FreezeDelegate(UIntPtr entityId, [MarshalAs(UnmanagedType.U1)] bool isFrozen);

		// Token: 0x02000173 RID: 371
		// (Invoke) Token: 0x06000BDF RID: 3039
		[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
		[SuppressUnmanagedCodeSecurity]
		[MonoNativeFunctionWrapper]
		public delegate Vec3 GetAngularVelocityDelegate(UIntPtr entityPtr);

		// Token: 0x02000174 RID: 372
		// (Invoke) Token: 0x06000BE3 RID: 3043
		[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
		[SuppressUnmanagedCodeSecurity]
		[MonoNativeFunctionWrapper]
		public delegate int GetAttachedNavmeshFaceCountDelegate(UIntPtr entityId);

		// Token: 0x02000175 RID: 373
		// (Invoke) Token: 0x06000BE7 RID: 3047
		[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
		[SuppressUnmanagedCodeSecurity]
		[MonoNativeFunctionWrapper]
		public delegate void GetAttachedNavmeshFaceRecordsDelegate(UIntPtr entityId, IntPtr faceRecords);

		// Token: 0x02000176 RID: 374
		// (Invoke) Token: 0x06000BEB RID: 3051
		[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
		[SuppressUnmanagedCodeSecurity]
		[MonoNativeFunctionWrapper]
		public delegate void GetAttachedNavmeshFaceVertexIndicesDelegate(UIntPtr entityId, in PathFaceRecord faceRecord, IntPtr indices);

		// Token: 0x02000177 RID: 375
		// (Invoke) Token: 0x06000BEF RID: 3055
		[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
		[SuppressUnmanagedCodeSecurity]
		[MonoNativeFunctionWrapper]
		public delegate uint GetBodyFlagsDelegate(UIntPtr entityId);

		// Token: 0x02000178 RID: 376
		// (Invoke) Token: 0x06000BF3 RID: 3059
		[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
		[SuppressUnmanagedCodeSecurity]
		[MonoNativeFunctionWrapper]
		public delegate NativeObjectPointer GetBodyShapeDelegate(UIntPtr entityId);

		// Token: 0x02000179 RID: 377
		// (Invoke) Token: 0x06000BF7 RID: 3063
		[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
		[SuppressUnmanagedCodeSecurity]
		[MonoNativeFunctionWrapper]
		public delegate void GetBodyVisualWorldTransformDelegate(UIntPtr entityPtr, out MatrixFrame frame);

		// Token: 0x0200017A RID: 378
		// (Invoke) Token: 0x06000BFB RID: 3067
		[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
		[SuppressUnmanagedCodeSecurity]
		[MonoNativeFunctionWrapper]
		public delegate void GetBodyWorldTransformDelegate(UIntPtr entityPtr, out MatrixFrame frame);

		// Token: 0x0200017B RID: 379
		// (Invoke) Token: 0x06000BFF RID: 3071
		[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
		[SuppressUnmanagedCodeSecurity]
		[MonoNativeFunctionWrapper]
		public delegate sbyte GetBoneCountDelegate(UIntPtr entityId);

		// Token: 0x0200017C RID: 380
		// (Invoke) Token: 0x06000C03 RID: 3075
		[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
		[SuppressUnmanagedCodeSecurity]
		[MonoNativeFunctionWrapper]
		public delegate void GetBoneEntitialFrameWithIndexDelegate(UIntPtr entityId, sbyte boneIndex, ref MatrixFrame outEntitialFrame);

		// Token: 0x0200017D RID: 381
		// (Invoke) Token: 0x06000C07 RID: 3079
		[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
		[SuppressUnmanagedCodeSecurity]
		[MonoNativeFunctionWrapper]
		public delegate void GetBoneEntitialFrameWithNameDelegate(UIntPtr entityId, byte[] boneName, ref MatrixFrame outEntitialFrame);

		// Token: 0x0200017E RID: 382
		// (Invoke) Token: 0x06000C0B RID: 3083
		[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
		[SuppressUnmanagedCodeSecurity]
		[MonoNativeFunctionWrapper]
		public delegate Vec3 GetBoundingBoxMaxDelegate(UIntPtr entityId);

		// Token: 0x0200017F RID: 383
		// (Invoke) Token: 0x06000C0F RID: 3087
		[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
		[SuppressUnmanagedCodeSecurity]
		[MonoNativeFunctionWrapper]
		public delegate Vec3 GetBoundingBoxMinDelegate(UIntPtr entityId);

		// Token: 0x02000180 RID: 384
		// (Invoke) Token: 0x06000C13 RID: 3091
		[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
		[SuppressUnmanagedCodeSecurity]
		[MonoNativeFunctionWrapper]
		public delegate void GetCameraParamsFromCameraScriptDelegate(UIntPtr entityId, UIntPtr camPtr, ref Vec3 dof_params);

		// Token: 0x02000181 RID: 385
		// (Invoke) Token: 0x06000C17 RID: 3095
		[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
		[SuppressUnmanagedCodeSecurity]
		[MonoNativeFunctionWrapper]
		public delegate Vec3 GetCenterOfMassDelegate(UIntPtr entityId);

		// Token: 0x02000182 RID: 386
		// (Invoke) Token: 0x06000C1B RID: 3099
		[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
		[SuppressUnmanagedCodeSecurity]
		[MonoNativeFunctionWrapper]
		public delegate NativeObjectPointer GetChildDelegate(UIntPtr entityId, int childIndex);

		// Token: 0x02000183 RID: 387
		// (Invoke) Token: 0x06000C1F RID: 3103
		[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
		[SuppressUnmanagedCodeSecurity]
		[MonoNativeFunctionWrapper]
		public delegate int GetChildCountDelegate(UIntPtr entityId);

		// Token: 0x02000184 RID: 388
		// (Invoke) Token: 0x06000C23 RID: 3107
		[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
		[SuppressUnmanagedCodeSecurity]
		[MonoNativeFunctionWrapper]
		public delegate UIntPtr GetChildPointerDelegate(UIntPtr entityId, int childIndex);

		// Token: 0x02000185 RID: 389
		// (Invoke) Token: 0x06000C27 RID: 3111
		[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
		[SuppressUnmanagedCodeSecurity]
		[MonoNativeFunctionWrapper]
		public delegate NativeObjectPointer GetComponentAtIndexDelegate(UIntPtr entityId, GameEntity.ComponentType componentType, int index);

		// Token: 0x02000186 RID: 390
		// (Invoke) Token: 0x06000C2B RID: 3115
		[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
		[SuppressUnmanagedCodeSecurity]
		[MonoNativeFunctionWrapper]
		public delegate int GetComponentCountDelegate(UIntPtr entityId, GameEntity.ComponentType componentType);

		// Token: 0x02000187 RID: 391
		// (Invoke) Token: 0x06000C2F RID: 3119
		[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
		[SuppressUnmanagedCodeSecurity]
		[MonoNativeFunctionWrapper]
		[return: MarshalAs(UnmanagedType.U1)]
		public delegate bool GetEditModeLevelVisibilityDelegate(UIntPtr entityId);

		// Token: 0x02000188 RID: 392
		// (Invoke) Token: 0x06000C33 RID: 3123
		[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
		[SuppressUnmanagedCodeSecurity]
		[MonoNativeFunctionWrapper]
		public delegate EntityFlags GetEntityFlagsDelegate(UIntPtr entityId);

		// Token: 0x02000189 RID: 393
		// (Invoke) Token: 0x06000C37 RID: 3127
		[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
		[SuppressUnmanagedCodeSecurity]
		[MonoNativeFunctionWrapper]
		public delegate EntityVisibilityFlags GetEntityVisibilityFlagsDelegate(UIntPtr entityId);

		// Token: 0x0200018A RID: 394
		// (Invoke) Token: 0x06000C3B RID: 3131
		[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
		[SuppressUnmanagedCodeSecurity]
		[MonoNativeFunctionWrapper]
		public delegate uint GetFactorColorDelegate(UIntPtr entityId);

		// Token: 0x0200018B RID: 395
		// (Invoke) Token: 0x06000C3F RID: 3135
		[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
		[SuppressUnmanagedCodeSecurity]
		[MonoNativeFunctionWrapper]
		public delegate UIntPtr GetFirstChildWithTagRecursiveDelegate(UIntPtr entityPtr, byte[] tag);

		// Token: 0x0200018C RID: 396
		// (Invoke) Token: 0x06000C43 RID: 3139
		[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
		[SuppressUnmanagedCodeSecurity]
		[MonoNativeFunctionWrapper]
		public delegate UIntPtr GetFirstEntityWithTagDelegate(UIntPtr scenePointer, byte[] tag);

		// Token: 0x0200018D RID: 397
		// (Invoke) Token: 0x06000C47 RID: 3143
		[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
		[SuppressUnmanagedCodeSecurity]
		[MonoNativeFunctionWrapper]
		public delegate UIntPtr GetFirstEntityWithTagExpressionDelegate(UIntPtr scenePointer, byte[] tagExpression);

		// Token: 0x0200018E RID: 398
		// (Invoke) Token: 0x06000C4B RID: 3147
		[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
		[SuppressUnmanagedCodeSecurity]
		[MonoNativeFunctionWrapper]
		public delegate NativeObjectPointer GetFirstMeshDelegate(UIntPtr entityId);

		// Token: 0x0200018F RID: 399
		// (Invoke) Token: 0x06000C4F RID: 3151
		[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
		[SuppressUnmanagedCodeSecurity]
		[MonoNativeFunctionWrapper]
		public delegate BoundingBox GetGlobalBoundingBoxDelegate(UIntPtr entityId);

		// Token: 0x02000190 RID: 400
		// (Invoke) Token: 0x06000C53 RID: 3155
		[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
		[SuppressUnmanagedCodeSecurity]
		[MonoNativeFunctionWrapper]
		public delegate Vec3 GetGlobalBoxMaxDelegate(UIntPtr entityId);

		// Token: 0x02000191 RID: 401
		// (Invoke) Token: 0x06000C57 RID: 3159
		[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
		[SuppressUnmanagedCodeSecurity]
		[MonoNativeFunctionWrapper]
		public delegate Vec3 GetGlobalBoxMinDelegate(UIntPtr entityId);

		// Token: 0x02000192 RID: 402
		// (Invoke) Token: 0x06000C5B RID: 3163
		[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
		[SuppressUnmanagedCodeSecurity]
		[MonoNativeFunctionWrapper]
		public delegate void GetGlobalFrameDelegate(UIntPtr meshPointer, out MatrixFrame outFrame);

		// Token: 0x02000193 RID: 403
		// (Invoke) Token: 0x06000C5F RID: 3167
		[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
		[SuppressUnmanagedCodeSecurity]
		[MonoNativeFunctionWrapper]
		public delegate void GetGlobalFrameImpreciseForFixedTickDelegate(UIntPtr entityId, out MatrixFrame outFrame);

		// Token: 0x02000194 RID: 404
		// (Invoke) Token: 0x06000C63 RID: 3171
		[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
		[SuppressUnmanagedCodeSecurity]
		[MonoNativeFunctionWrapper]
		public delegate Vec3 GetGlobalScaleDelegate(UIntPtr pointer);

		// Token: 0x02000195 RID: 405
		// (Invoke) Token: 0x06000C67 RID: 3175
		[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
		[SuppressUnmanagedCodeSecurity]
		[MonoNativeFunctionWrapper]
		public delegate Vec2 GetGlobalWindStrengthVectorOfSceneDelegate(UIntPtr entityPtr);

		// Token: 0x02000196 RID: 406
		// (Invoke) Token: 0x06000C6B RID: 3179
		[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
		[SuppressUnmanagedCodeSecurity]
		[MonoNativeFunctionWrapper]
		public delegate Vec2 GetGlobalWindVelocityOfSceneDelegate(UIntPtr entityPtr);

		// Token: 0x02000197 RID: 407
		// (Invoke) Token: 0x06000C6F RID: 3183
		[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
		[SuppressUnmanagedCodeSecurity]
		[MonoNativeFunctionWrapper]
		public delegate Vec2 GetGlobalWindVelocityWithGustNoiseOfSceneDelegate(UIntPtr entityPtr, float globalTime);

		// Token: 0x02000198 RID: 408
		// (Invoke) Token: 0x06000C73 RID: 3187
		[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
		[SuppressUnmanagedCodeSecurity]
		[MonoNativeFunctionWrapper]
		public delegate int GetGuidDelegate(UIntPtr entityId);

		// Token: 0x02000199 RID: 409
		// (Invoke) Token: 0x06000C77 RID: 3191
		[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
		[SuppressUnmanagedCodeSecurity]
		[MonoNativeFunctionWrapper]
		public delegate Vec3 GetLastFinalRenderCameraPositionOfSceneDelegate(UIntPtr entityPtr);

		// Token: 0x0200019A RID: 410
		// (Invoke) Token: 0x06000C7B RID: 3195
		[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
		[SuppressUnmanagedCodeSecurity]
		[MonoNativeFunctionWrapper]
		public delegate NativeObjectPointer GetLightDelegate(UIntPtr entityId);

		// Token: 0x0200019B RID: 411
		// (Invoke) Token: 0x06000C7F RID: 3199
		[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
		[SuppressUnmanagedCodeSecurity]
		[MonoNativeFunctionWrapper]
		public delegate Vec3 GetLinearVelocityDelegate(UIntPtr entityPtr);

		// Token: 0x0200019C RID: 412
		// (Invoke) Token: 0x06000C83 RID: 3203
		[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
		[SuppressUnmanagedCodeSecurity]
		[MonoNativeFunctionWrapper]
		public delegate BoundingBox GetLocalBoundingBoxDelegate(UIntPtr entityId);

		// Token: 0x0200019D RID: 413
		// (Invoke) Token: 0x06000C87 RID: 3207
		[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
		[SuppressUnmanagedCodeSecurity]
		[MonoNativeFunctionWrapper]
		public delegate void GetLocalFrameDelegate(UIntPtr entityId, out MatrixFrame outFrame);

		// Token: 0x0200019E RID: 414
		// (Invoke) Token: 0x06000C8B RID: 3211
		[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
		[SuppressUnmanagedCodeSecurity]
		[MonoNativeFunctionWrapper]
		public delegate void GetLocalPhysicsBoundingBoxDelegate(UIntPtr entityId, [MarshalAs(UnmanagedType.U1)] bool includeChildren, out BoundingBox outBoundingBox);

		// Token: 0x0200019F RID: 415
		// (Invoke) Token: 0x06000C8F RID: 3215
		[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
		[SuppressUnmanagedCodeSecurity]
		[MonoNativeFunctionWrapper]
		public delegate float GetLodLevelForDistanceSqDelegate(UIntPtr entityId, float distanceSquared);

		// Token: 0x020001A0 RID: 416
		// (Invoke) Token: 0x06000C93 RID: 3219
		[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
		[SuppressUnmanagedCodeSecurity]
		[MonoNativeFunctionWrapper]
		public delegate float GetMassDelegate(UIntPtr entityId);

		// Token: 0x020001A1 RID: 417
		// (Invoke) Token: 0x06000C97 RID: 3223
		[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
		[SuppressUnmanagedCodeSecurity]
		[MonoNativeFunctionWrapper]
		public delegate Vec3 GetMassSpaceInertiaDelegate(UIntPtr entityId);

		// Token: 0x020001A2 RID: 418
		// (Invoke) Token: 0x06000C9B RID: 3227
		[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
		[SuppressUnmanagedCodeSecurity]
		[MonoNativeFunctionWrapper]
		public delegate Vec3 GetMassSpaceInverseInertiaDelegate(UIntPtr entityId);

		// Token: 0x020001A3 RID: 419
		// (Invoke) Token: 0x06000C9F RID: 3231
		[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
		[SuppressUnmanagedCodeSecurity]
		[MonoNativeFunctionWrapper]
		public delegate void GetMeshBendedPositionDelegate(UIntPtr entityId, ref MatrixFrame worldSpacePosition, ref MatrixFrame output);

		// Token: 0x020001A4 RID: 420
		// (Invoke) Token: 0x06000CA3 RID: 3235
		[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
		[SuppressUnmanagedCodeSecurity]
		[MonoNativeFunctionWrapper]
		public delegate GameEntity.Mobility GetMobilityDelegate(UIntPtr entityId);

		// Token: 0x020001A5 RID: 421
		// (Invoke) Token: 0x06000CA7 RID: 3239
		[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
		[SuppressUnmanagedCodeSecurity]
		[MonoNativeFunctionWrapper]
		public delegate int GetNameDelegate(UIntPtr entityId);

		// Token: 0x020001A6 RID: 422
		// (Invoke) Token: 0x06000CAB RID: 3243
		[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
		[SuppressUnmanagedCodeSecurity]
		[MonoNativeFunctionWrapper]
		public delegate void GetNativeScriptComponentVariableDelegate(UIntPtr entityPtr, byte[] className, byte[] fieldName, ref ScriptComponentFieldHolder data, RglScriptFieldType variableType);

		// Token: 0x020001A7 RID: 423
		// (Invoke) Token: 0x06000CAF RID: 3247
		[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
		[SuppressUnmanagedCodeSecurity]
		[MonoNativeFunctionWrapper]
		public delegate UIntPtr GetNextEntityWithTagDelegate(UIntPtr currententityId, byte[] tag);

		// Token: 0x020001A8 RID: 424
		// (Invoke) Token: 0x06000CB3 RID: 3251
		[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
		[SuppressUnmanagedCodeSecurity]
		[MonoNativeFunctionWrapper]
		public delegate UIntPtr GetNextEntityWithTagExpressionDelegate(UIntPtr currententityId, byte[] tagExpression);

		// Token: 0x020001A9 RID: 425
		// (Invoke) Token: 0x06000CB7 RID: 3255
		[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
		[SuppressUnmanagedCodeSecurity]
		[MonoNativeFunctionWrapper]
		public delegate NativeObjectPointer GetNextPrefabDelegate(UIntPtr currentPrefab);

		// Token: 0x020001AA RID: 426
		// (Invoke) Token: 0x06000CBB RID: 3259
		[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
		[SuppressUnmanagedCodeSecurity]
		[MonoNativeFunctionWrapper]
		public delegate int GetOldPrefabNameDelegate(UIntPtr prefab);

		// Token: 0x020001AB RID: 427
		// (Invoke) Token: 0x06000CBF RID: 3263
		[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
		[SuppressUnmanagedCodeSecurity]
		[MonoNativeFunctionWrapper]
		public delegate NativeObjectPointer GetParentDelegate(UIntPtr entityId);

		// Token: 0x020001AC RID: 428
		// (Invoke) Token: 0x06000CC3 RID: 3267
		[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
		[SuppressUnmanagedCodeSecurity]
		[MonoNativeFunctionWrapper]
		public delegate UIntPtr GetParentPointerDelegate(UIntPtr entityId);

		// Token: 0x020001AD RID: 429
		// (Invoke) Token: 0x06000CC7 RID: 3271
		[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
		[SuppressUnmanagedCodeSecurity]
		[MonoNativeFunctionWrapper]
		public delegate uint GetPhysicsDescBodyFlagsDelegate(UIntPtr entityId);

		// Token: 0x020001AE RID: 430
		// (Invoke) Token: 0x06000CCB RID: 3275
		[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
		[SuppressUnmanagedCodeSecurity]
		[MonoNativeFunctionWrapper]
		public delegate int GetPhysicsMaterialIndexDelegate(UIntPtr entityId);

		// Token: 0x020001AF RID: 431
		// (Invoke) Token: 0x06000CCF RID: 3279
		[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
		[SuppressUnmanagedCodeSecurity]
		[MonoNativeFunctionWrapper]
		public delegate void GetPhysicsMinMaxDelegate(UIntPtr entityId, [MarshalAs(UnmanagedType.U1)] bool includeChildren, ref Vec3 bbmin, ref Vec3 bbmax, [MarshalAs(UnmanagedType.U1)] bool returnLocal);

		// Token: 0x020001B0 RID: 432
		// (Invoke) Token: 0x06000CD3 RID: 3283
		[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
		[SuppressUnmanagedCodeSecurity]
		[MonoNativeFunctionWrapper]
		[return: MarshalAs(UnmanagedType.U1)]
		public delegate bool GetPhysicsStateDelegate(UIntPtr entityId);

		// Token: 0x020001B1 RID: 433
		// (Invoke) Token: 0x06000CD7 RID: 3287
		[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
		[SuppressUnmanagedCodeSecurity]
		[MonoNativeFunctionWrapper]
		public delegate int GetPhysicsTriangleCountDelegate(UIntPtr entityId);

		// Token: 0x020001B2 RID: 434
		// (Invoke) Token: 0x06000CDB RID: 3291
		[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
		[SuppressUnmanagedCodeSecurity]
		[MonoNativeFunctionWrapper]
		public delegate int GetPrefabNameDelegate(UIntPtr prefab);

		// Token: 0x020001B3 RID: 435
		// (Invoke) Token: 0x06000CDF RID: 3295
		[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
		[SuppressUnmanagedCodeSecurity]
		[MonoNativeFunctionWrapper]
		public delegate void GetPreviousGlobalFrameDelegate(UIntPtr entityPtr, out MatrixFrame frame);

		// Token: 0x020001B4 RID: 436
		// (Invoke) Token: 0x06000CE3 RID: 3299
		[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
		[SuppressUnmanagedCodeSecurity]
		[MonoNativeFunctionWrapper]
		public delegate void GetQuickBoneEntitialFrameDelegate(UIntPtr entityId, sbyte index, out MatrixFrame frame);

		// Token: 0x020001B5 RID: 437
		// (Invoke) Token: 0x06000CE7 RID: 3303
		[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
		[SuppressUnmanagedCodeSecurity]
		[MonoNativeFunctionWrapper]
		public delegate float GetRadiusDelegate(UIntPtr entityId);

		// Token: 0x020001B6 RID: 438
		// (Invoke) Token: 0x06000CEB RID: 3307
		[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
		[SuppressUnmanagedCodeSecurity]
		[MonoNativeFunctionWrapper]
		public delegate UIntPtr GetRootParentPointerDelegate(UIntPtr entityId);

		// Token: 0x020001B7 RID: 439
		// (Invoke) Token: 0x06000CEF RID: 3311
		[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
		[SuppressUnmanagedCodeSecurity]
		[MonoNativeFunctionWrapper]
		public delegate NativeObjectPointer GetSceneDelegate(UIntPtr entityId);

		// Token: 0x020001B8 RID: 440
		// (Invoke) Token: 0x06000CF3 RID: 3315
		[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
		[SuppressUnmanagedCodeSecurity]
		[MonoNativeFunctionWrapper]
		public delegate UIntPtr GetScenePointerDelegate(UIntPtr entityId);

		// Token: 0x020001B9 RID: 441
		// (Invoke) Token: 0x06000CF7 RID: 3319
		[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
		[SuppressUnmanagedCodeSecurity]
		[MonoNativeFunctionWrapper]
		public delegate int GetScriptComponentDelegate(UIntPtr entityId);

		// Token: 0x020001BA RID: 442
		// (Invoke) Token: 0x06000CFB RID: 3323
		[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
		[SuppressUnmanagedCodeSecurity]
		[MonoNativeFunctionWrapper]
		public delegate int GetScriptComponentAtIndexDelegate(UIntPtr entityId, int index);

		// Token: 0x020001BB RID: 443
		// (Invoke) Token: 0x06000CFF RID: 3327
		[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
		[SuppressUnmanagedCodeSecurity]
		[MonoNativeFunctionWrapper]
		public delegate int GetScriptComponentCountDelegate(UIntPtr entityId);

		// Token: 0x020001BC RID: 444
		// (Invoke) Token: 0x06000D03 RID: 3331
		[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
		[SuppressUnmanagedCodeSecurity]
		[MonoNativeFunctionWrapper]
		public delegate int GetScriptComponentIndexDelegate(UIntPtr entityId, uint nameHash);

		// Token: 0x020001BD RID: 445
		// (Invoke) Token: 0x06000D07 RID: 3335
		[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
		[SuppressUnmanagedCodeSecurity]
		[MonoNativeFunctionWrapper]
		public delegate NativeObjectPointer GetSkeletonDelegate(UIntPtr entityId);

		// Token: 0x020001BE RID: 446
		// (Invoke) Token: 0x06000D0B RID: 3339
		[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
		[SuppressUnmanagedCodeSecurity]
		[MonoNativeFunctionWrapper]
		public delegate int GetTagsDelegate(UIntPtr entityId);

		// Token: 0x020001BF RID: 447
		// (Invoke) Token: 0x06000D0F RID: 3343
		[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
		[SuppressUnmanagedCodeSecurity]
		[MonoNativeFunctionWrapper]
		public delegate uint GetUpgradeLevelMaskDelegate(UIntPtr prefab);

		// Token: 0x020001C0 RID: 448
		// (Invoke) Token: 0x06000D13 RID: 3347
		[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
		[SuppressUnmanagedCodeSecurity]
		[MonoNativeFunctionWrapper]
		public delegate uint GetUpgradeLevelMaskCumulativeDelegate(UIntPtr prefab);

		// Token: 0x020001C1 RID: 449
		// (Invoke) Token: 0x06000D17 RID: 3351
		[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
		[SuppressUnmanagedCodeSecurity]
		[MonoNativeFunctionWrapper]
		[return: MarshalAs(UnmanagedType.U1)]
		public delegate bool GetVisibilityExcludeParentsDelegate(UIntPtr entityId);

		// Token: 0x020001C2 RID: 450
		// (Invoke) Token: 0x06000D1B RID: 3355
		[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
		[SuppressUnmanagedCodeSecurity]
		[MonoNativeFunctionWrapper]
		public delegate uint GetVisibilityLevelMaskIncludingParentsDelegate(UIntPtr entityId);

		// Token: 0x020001C3 RID: 451
		// (Invoke) Token: 0x06000D1F RID: 3359
		[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
		[SuppressUnmanagedCodeSecurity]
		[MonoNativeFunctionWrapper]
		public delegate float GetWaterLevelAtPositionDelegate(UIntPtr entityId, in Vec2 position, [MarshalAs(UnmanagedType.U1)] bool useWaterRenderer, [MarshalAs(UnmanagedType.U1)] bool checkWaterBodyEntities);

		// Token: 0x020001C4 RID: 452
		// (Invoke) Token: 0x06000D23 RID: 3363
		[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
		[SuppressUnmanagedCodeSecurity]
		[MonoNativeFunctionWrapper]
		[return: MarshalAs(UnmanagedType.U1)]
		public delegate bool HasBatchedKinematicPhysicsFlagDelegate(UIntPtr entityId);

		// Token: 0x020001C5 RID: 453
		// (Invoke) Token: 0x06000D27 RID: 3367
		[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
		[SuppressUnmanagedCodeSecurity]
		[MonoNativeFunctionWrapper]
		[return: MarshalAs(UnmanagedType.U1)]
		public delegate bool HasBatchedRayCastPhysicsFlagDelegate(UIntPtr entityId);

		// Token: 0x020001C6 RID: 454
		// (Invoke) Token: 0x06000D2B RID: 3371
		[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
		[SuppressUnmanagedCodeSecurity]
		[MonoNativeFunctionWrapper]
		[return: MarshalAs(UnmanagedType.U1)]
		public delegate bool HasBodyDelegate(UIntPtr entityId);

		// Token: 0x020001C7 RID: 455
		// (Invoke) Token: 0x06000D2F RID: 3375
		[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
		[SuppressUnmanagedCodeSecurity]
		[MonoNativeFunctionWrapper]
		[return: MarshalAs(UnmanagedType.U1)]
		public delegate bool HasComplexAnimTreeDelegate(UIntPtr entityId);

		// Token: 0x020001C8 RID: 456
		// (Invoke) Token: 0x06000D33 RID: 3379
		[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
		[SuppressUnmanagedCodeSecurity]
		[MonoNativeFunctionWrapper]
		[return: MarshalAs(UnmanagedType.U1)]
		public delegate bool HasComponentDelegate(UIntPtr pointer, UIntPtr componentPointer);

		// Token: 0x020001C9 RID: 457
		// (Invoke) Token: 0x06000D37 RID: 3383
		[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
		[SuppressUnmanagedCodeSecurity]
		[MonoNativeFunctionWrapper]
		[return: MarshalAs(UnmanagedType.U1)]
		public delegate bool HasDynamicRigidBodyDelegate(UIntPtr entityId);

		// Token: 0x020001CA RID: 458
		// (Invoke) Token: 0x06000D3B RID: 3387
		[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
		[SuppressUnmanagedCodeSecurity]
		[MonoNativeFunctionWrapper]
		[return: MarshalAs(UnmanagedType.U1)]
		public delegate bool HasDynamicRigidBodyAndActiveSimulationDelegate(UIntPtr entityId);

		// Token: 0x020001CB RID: 459
		// (Invoke) Token: 0x06000D3F RID: 3391
		[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
		[SuppressUnmanagedCodeSecurity]
		[MonoNativeFunctionWrapper]
		[return: MarshalAs(UnmanagedType.U1)]
		public delegate bool HasFrameChangedDelegate(UIntPtr entityId);

		// Token: 0x020001CC RID: 460
		// (Invoke) Token: 0x06000D43 RID: 3395
		[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
		[SuppressUnmanagedCodeSecurity]
		[MonoNativeFunctionWrapper]
		[return: MarshalAs(UnmanagedType.U1)]
		public delegate bool HasKinematicRigidBodyDelegate(UIntPtr entityId);

		// Token: 0x020001CD RID: 461
		// (Invoke) Token: 0x06000D47 RID: 3399
		[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
		[SuppressUnmanagedCodeSecurity]
		[MonoNativeFunctionWrapper]
		[return: MarshalAs(UnmanagedType.U1)]
		public delegate bool HasPhysicsBodyDelegate(UIntPtr entityId);

		// Token: 0x020001CE RID: 462
		// (Invoke) Token: 0x06000D4B RID: 3403
		[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
		[SuppressUnmanagedCodeSecurity]
		[MonoNativeFunctionWrapper]
		[return: MarshalAs(UnmanagedType.U1)]
		public delegate bool HasPhysicsDefinitionDelegate(UIntPtr entityId, int excludeFlags);

		// Token: 0x020001CF RID: 463
		// (Invoke) Token: 0x06000D4F RID: 3407
		[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
		[SuppressUnmanagedCodeSecurity]
		[MonoNativeFunctionWrapper]
		[return: MarshalAs(UnmanagedType.U1)]
		public delegate bool HasSceneDelegate(UIntPtr entityId);

		// Token: 0x020001D0 RID: 464
		// (Invoke) Token: 0x06000D53 RID: 3411
		[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
		[SuppressUnmanagedCodeSecurity]
		[MonoNativeFunctionWrapper]
		[return: MarshalAs(UnmanagedType.U1)]
		public delegate bool HasScriptComponentDelegate(UIntPtr entityId, byte[] scName);

		// Token: 0x020001D1 RID: 465
		// (Invoke) Token: 0x06000D57 RID: 3415
		[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
		[SuppressUnmanagedCodeSecurity]
		[MonoNativeFunctionWrapper]
		[return: MarshalAs(UnmanagedType.U1)]
		public delegate bool HasScriptComponentHashDelegate(UIntPtr entityId, uint scNameHash);

		// Token: 0x020001D2 RID: 466
		// (Invoke) Token: 0x06000D5B RID: 3419
		[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
		[SuppressUnmanagedCodeSecurity]
		[MonoNativeFunctionWrapper]
		[return: MarshalAs(UnmanagedType.U1)]
		public delegate bool HasStaticPhysicsBodyDelegate(UIntPtr entityId);

		// Token: 0x020001D3 RID: 467
		// (Invoke) Token: 0x06000D5F RID: 3423
		[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
		[SuppressUnmanagedCodeSecurity]
		[MonoNativeFunctionWrapper]
		[return: MarshalAs(UnmanagedType.U1)]
		public delegate bool HasTagDelegate(UIntPtr entityId, byte[] tag);

		// Token: 0x020001D4 RID: 468
		// (Invoke) Token: 0x06000D63 RID: 3427
		[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
		[SuppressUnmanagedCodeSecurity]
		[MonoNativeFunctionWrapper]
		[return: MarshalAs(UnmanagedType.U1)]
		public delegate bool IsDynamicBodyStationaryDelegate(UIntPtr entityId);

		// Token: 0x020001D5 RID: 469
		// (Invoke) Token: 0x06000D67 RID: 3431
		[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
		[SuppressUnmanagedCodeSecurity]
		[MonoNativeFunctionWrapper]
		[return: MarshalAs(UnmanagedType.U1)]
		public delegate bool IsEngineBodySleepingDelegate(UIntPtr entityId);

		// Token: 0x020001D6 RID: 470
		// (Invoke) Token: 0x06000D6B RID: 3435
		[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
		[SuppressUnmanagedCodeSecurity]
		[MonoNativeFunctionWrapper]
		[return: MarshalAs(UnmanagedType.U1)]
		public delegate bool IsEntitySelectedOnEditorDelegate(UIntPtr entityId);

		// Token: 0x020001D7 RID: 471
		// (Invoke) Token: 0x06000D6F RID: 3439
		[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
		[SuppressUnmanagedCodeSecurity]
		[MonoNativeFunctionWrapper]
		[return: MarshalAs(UnmanagedType.U1)]
		public delegate bool IsFrozenDelegate(UIntPtr entityId);

		// Token: 0x020001D8 RID: 472
		// (Invoke) Token: 0x06000D73 RID: 3443
		[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
		[SuppressUnmanagedCodeSecurity]
		[MonoNativeFunctionWrapper]
		[return: MarshalAs(UnmanagedType.U1)]
		public delegate bool IsGhostObjectDelegate(UIntPtr entityId);

		// Token: 0x020001D9 RID: 473
		// (Invoke) Token: 0x06000D77 RID: 3447
		[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
		[SuppressUnmanagedCodeSecurity]
		[MonoNativeFunctionWrapper]
		[return: MarshalAs(UnmanagedType.U1)]
		public delegate bool IsGravityDisabledDelegate(UIntPtr entityId);

		// Token: 0x020001DA RID: 474
		// (Invoke) Token: 0x06000D7B RID: 3451
		[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
		[SuppressUnmanagedCodeSecurity]
		[MonoNativeFunctionWrapper]
		[return: MarshalAs(UnmanagedType.U1)]
		public delegate bool IsGuidValidDelegate(UIntPtr entityId);

		// Token: 0x020001DB RID: 475
		// (Invoke) Token: 0x06000D7F RID: 3455
		[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
		[SuppressUnmanagedCodeSecurity]
		[MonoNativeFunctionWrapper]
		[return: MarshalAs(UnmanagedType.U1)]
		public delegate bool IsInEditorSceneDelegate(UIntPtr pointer);

		// Token: 0x020001DC RID: 476
		// (Invoke) Token: 0x06000D83 RID: 3459
		[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
		[SuppressUnmanagedCodeSecurity]
		[MonoNativeFunctionWrapper]
		[return: MarshalAs(UnmanagedType.U1)]
		public delegate bool IsVisibleIncludeParentsDelegate(UIntPtr entityId);

		// Token: 0x020001DD RID: 477
		// (Invoke) Token: 0x06000D87 RID: 3463
		[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
		[SuppressUnmanagedCodeSecurity]
		[MonoNativeFunctionWrapper]
		public delegate void PauseParticleSystemDelegate(UIntPtr entityId, [MarshalAs(UnmanagedType.U1)] bool doChildren);

		// Token: 0x020001DE RID: 478
		// (Invoke) Token: 0x06000D8B RID: 3467
		[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
		[SuppressUnmanagedCodeSecurity]
		[MonoNativeFunctionWrapper]
		public delegate void PopCapsuleShapeFromEntityBodyDelegate(UIntPtr entityId);

		// Token: 0x020001DF RID: 479
		// (Invoke) Token: 0x06000D8F RID: 3471
		[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
		[SuppressUnmanagedCodeSecurity]
		[MonoNativeFunctionWrapper]
		[return: MarshalAs(UnmanagedType.U1)]
		public delegate bool PrefabExistsDelegate(byte[] prefabName);

		// Token: 0x020001E0 RID: 480
		// (Invoke) Token: 0x06000D93 RID: 3475
		[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
		[SuppressUnmanagedCodeSecurity]
		[MonoNativeFunctionWrapper]
		public delegate void PushCapsuleShapeToEntityBodyDelegate(UIntPtr entityId, Vec3 p1, Vec3 p2, float radius, byte[] physicsMaterialName);

		// Token: 0x020001E1 RID: 481
		// (Invoke) Token: 0x06000D97 RID: 3479
		[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
		[SuppressUnmanagedCodeSecurity]
		[MonoNativeFunctionWrapper]
		[return: MarshalAs(UnmanagedType.U1)]
		public delegate bool RayHitEntityDelegate(UIntPtr entityId, in Vec3 rayOrigin, in Vec3 rayDirection, float maxLength, ref float resultLength);

		// Token: 0x020001E2 RID: 482
		// (Invoke) Token: 0x06000D9B RID: 3483
		[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
		[SuppressUnmanagedCodeSecurity]
		[MonoNativeFunctionWrapper]
		[return: MarshalAs(UnmanagedType.U1)]
		public delegate bool RayHitEntityWithNormalDelegate(UIntPtr entityId, in Vec3 rayOrigin, in Vec3 rayDirection, float maxLength, ref Vec3 resultNormal, ref float resultLength);

		// Token: 0x020001E3 RID: 483
		// (Invoke) Token: 0x06000D9F RID: 3487
		[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
		[SuppressUnmanagedCodeSecurity]
		[MonoNativeFunctionWrapper]
		public delegate void RecomputeBoundingBoxDelegate(UIntPtr pointer);

		// Token: 0x020001E4 RID: 484
		// (Invoke) Token: 0x06000DA3 RID: 3491
		[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
		[SuppressUnmanagedCodeSecurity]
		[MonoNativeFunctionWrapper]
		public delegate void RefreshMeshesToRenderToHullWaterDelegate(UIntPtr entityPointer, UIntPtr visualPrefab, byte[] tag);

		// Token: 0x020001E5 RID: 485
		// (Invoke) Token: 0x06000DA7 RID: 3495
		[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
		[SuppressUnmanagedCodeSecurity]
		[MonoNativeFunctionWrapper]
		public delegate int RegisterWaterSDFClipDelegate(UIntPtr entityId, UIntPtr textureID);

		// Token: 0x020001E6 RID: 486
		// (Invoke) Token: 0x06000DAB RID: 3499
		[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
		[SuppressUnmanagedCodeSecurity]
		[MonoNativeFunctionWrapper]
		public delegate void RelaxLocalBoundingBoxDelegate(UIntPtr entityId, in BoundingBox boundingBox);

		// Token: 0x020001E7 RID: 487
		// (Invoke) Token: 0x06000DAF RID: 3503
		[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
		[SuppressUnmanagedCodeSecurity]
		[MonoNativeFunctionWrapper]
		public delegate void ReleaseEditDataUserToAllMeshesDelegate(UIntPtr entityId, [MarshalAs(UnmanagedType.U1)] bool entity_components, [MarshalAs(UnmanagedType.U1)] bool skeleton_components);

		// Token: 0x020001E8 RID: 488
		// (Invoke) Token: 0x06000DB3 RID: 3507
		[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
		[SuppressUnmanagedCodeSecurity]
		[MonoNativeFunctionWrapper]
		public delegate void RemoveDelegate(UIntPtr entityId, int removeReason);

		// Token: 0x020001E9 RID: 489
		// (Invoke) Token: 0x06000DB7 RID: 3511
		[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
		[SuppressUnmanagedCodeSecurity]
		[MonoNativeFunctionWrapper]
		public delegate void RemoveAllChildrenDelegate(UIntPtr entityId);

		// Token: 0x020001EA RID: 490
		// (Invoke) Token: 0x06000DBB RID: 3515
		[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
		[SuppressUnmanagedCodeSecurity]
		[MonoNativeFunctionWrapper]
		public delegate void RemoveAllParticleSystemsDelegate(UIntPtr entityId);

		// Token: 0x020001EB RID: 491
		// (Invoke) Token: 0x06000DBF RID: 3519
		[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
		[SuppressUnmanagedCodeSecurity]
		[MonoNativeFunctionWrapper]
		public delegate void RemoveChildDelegate(UIntPtr parentEntity, UIntPtr childEntity, [MarshalAs(UnmanagedType.U1)] bool keepPhysics, [MarshalAs(UnmanagedType.U1)] bool keepScenePointer, [MarshalAs(UnmanagedType.U1)] bool callScriptCallbacks, int removeReason);

		// Token: 0x020001EC RID: 492
		// (Invoke) Token: 0x06000DC3 RID: 3523
		[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
		[SuppressUnmanagedCodeSecurity]
		[MonoNativeFunctionWrapper]
		[return: MarshalAs(UnmanagedType.U1)]
		public delegate bool RemoveComponentDelegate(UIntPtr pointer, UIntPtr componentPointer);

		// Token: 0x020001ED RID: 493
		// (Invoke) Token: 0x06000DC7 RID: 3527
		[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
		[SuppressUnmanagedCodeSecurity]
		[MonoNativeFunctionWrapper]
		[return: MarshalAs(UnmanagedType.U1)]
		public delegate bool RemoveComponentWithMeshDelegate(UIntPtr entityId, UIntPtr mesh);

		// Token: 0x020001EE RID: 494
		// (Invoke) Token: 0x06000DCB RID: 3531
		[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
		[SuppressUnmanagedCodeSecurity]
		[MonoNativeFunctionWrapper]
		public delegate void RemoveEnginePhysicsDelegate(UIntPtr entityId);

		// Token: 0x020001EF RID: 495
		// (Invoke) Token: 0x06000DCF RID: 3535
		[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
		[SuppressUnmanagedCodeSecurity]
		[MonoNativeFunctionWrapper]
		public delegate void RemoveFromPredisplayEntityDelegate(UIntPtr entityId);

		// Token: 0x020001F0 RID: 496
		// (Invoke) Token: 0x06000DD3 RID: 3539
		[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
		[SuppressUnmanagedCodeSecurity]
		[MonoNativeFunctionWrapper]
		public delegate void RemoveJointDelegate(UIntPtr jointId, UIntPtr entityId);

		// Token: 0x020001F1 RID: 497
		// (Invoke) Token: 0x06000DD7 RID: 3543
		[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
		[SuppressUnmanagedCodeSecurity]
		[MonoNativeFunctionWrapper]
		[return: MarshalAs(UnmanagedType.U1)]
		public delegate bool RemoveMultiMeshDelegate(UIntPtr entityId, UIntPtr multiMeshPtr);

		// Token: 0x020001F2 RID: 498
		// (Invoke) Token: 0x06000DDB RID: 3547
		[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
		[SuppressUnmanagedCodeSecurity]
		[MonoNativeFunctionWrapper]
		public delegate void RemoveMultiMeshFromSkeletonDelegate(UIntPtr gameEntity, UIntPtr multiMesh);

		// Token: 0x020001F3 RID: 499
		// (Invoke) Token: 0x06000DDF RID: 3551
		[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
		[SuppressUnmanagedCodeSecurity]
		[MonoNativeFunctionWrapper]
		public delegate void RemoveMultiMeshFromSkeletonBoneDelegate(UIntPtr gameEntity, UIntPtr multiMesh, sbyte boneIndex);

		// Token: 0x020001F4 RID: 500
		// (Invoke) Token: 0x06000DE3 RID: 3555
		[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
		[SuppressUnmanagedCodeSecurity]
		[MonoNativeFunctionWrapper]
		public delegate void RemovePhysicsDelegate(UIntPtr entityId, [MarshalAs(UnmanagedType.U1)] bool clearingTheScene);

		// Token: 0x020001F5 RID: 501
		// (Invoke) Token: 0x06000DE7 RID: 3559
		[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
		[SuppressUnmanagedCodeSecurity]
		[MonoNativeFunctionWrapper]
		public delegate void RemoveScriptComponentDelegate(UIntPtr entityId, UIntPtr scriptComponentPtr, int removeReason);

		// Token: 0x020001F6 RID: 502
		// (Invoke) Token: 0x06000DEB RID: 3563
		[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
		[SuppressUnmanagedCodeSecurity]
		[MonoNativeFunctionWrapper]
		public delegate void RemoveTagDelegate(UIntPtr entityId, byte[] tag);

		// Token: 0x020001F7 RID: 503
		// (Invoke) Token: 0x06000DEF RID: 3567
		[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
		[SuppressUnmanagedCodeSecurity]
		[MonoNativeFunctionWrapper]
		public delegate void ReplacePhysicsBodyWithQuadPhysicsBodyDelegate(UIntPtr pointer, UIntPtr quad, int physicsMaterial, BodyFlags bodyFlags, int numberOfVertices, UIntPtr indices, int numberOfIndices);

		// Token: 0x020001F8 RID: 504
		// (Invoke) Token: 0x06000DF3 RID: 3571
		[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
		[SuppressUnmanagedCodeSecurity]
		[MonoNativeFunctionWrapper]
		public delegate void ResetHullWaterDelegate(UIntPtr visualPrefab);

		// Token: 0x020001F9 RID: 505
		// (Invoke) Token: 0x06000DF7 RID: 3575
		[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
		[SuppressUnmanagedCodeSecurity]
		[MonoNativeFunctionWrapper]
		public delegate void ResumeParticleSystemDelegate(UIntPtr entityId, [MarshalAs(UnmanagedType.U1)] bool doChildren);

		// Token: 0x020001FA RID: 506
		// (Invoke) Token: 0x06000DFB RID: 3579
		[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
		[SuppressUnmanagedCodeSecurity]
		[MonoNativeFunctionWrapper]
		public delegate void SelectEntityOnEditorDelegate(UIntPtr entityId);

		// Token: 0x020001FB RID: 507
		// (Invoke) Token: 0x06000DFF RID: 3583
		[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
		[SuppressUnmanagedCodeSecurity]
		[MonoNativeFunctionWrapper]
		public delegate void SetAlphaDelegate(UIntPtr entityId, float alpha);

		// Token: 0x020001FC RID: 508
		// (Invoke) Token: 0x06000E03 RID: 3587
		[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
		[SuppressUnmanagedCodeSecurity]
		[MonoNativeFunctionWrapper]
		public delegate void SetAngularVelocityDelegate(UIntPtr entityPtr, in Vec3 newAngularVelocity);

		// Token: 0x020001FD RID: 509
		// (Invoke) Token: 0x06000E07 RID: 3591
		[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
		[SuppressUnmanagedCodeSecurity]
		[MonoNativeFunctionWrapper]
		public delegate void SetAnimationSoundActivationDelegate(UIntPtr entityId, [MarshalAs(UnmanagedType.U1)] bool activate);

		// Token: 0x020001FE RID: 510
		// (Invoke) Token: 0x06000E0B RID: 3595
		[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
		[SuppressUnmanagedCodeSecurity]
		[MonoNativeFunctionWrapper]
		public delegate void SetAnimTreeChannelParameterDelegate(UIntPtr entityId, float phase, int channel_no);

		// Token: 0x020001FF RID: 511
		// (Invoke) Token: 0x06000E0F RID: 3599
		[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
		[SuppressUnmanagedCodeSecurity]
		[MonoNativeFunctionWrapper]
		public delegate void SetAsContourEntityDelegate(UIntPtr entityId, uint color);

		// Token: 0x02000200 RID: 512
		// (Invoke) Token: 0x06000E13 RID: 3603
		[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
		[SuppressUnmanagedCodeSecurity]
		[MonoNativeFunctionWrapper]
		public delegate void SetAsPredisplayEntityDelegate(UIntPtr entityId);

		// Token: 0x02000201 RID: 513
		// (Invoke) Token: 0x06000E17 RID: 3607
		[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
		[SuppressUnmanagedCodeSecurity]
		[MonoNativeFunctionWrapper]
		public delegate void SetAsReplayEntityDelegate(UIntPtr gameEntity);

		// Token: 0x02000202 RID: 514
		// (Invoke) Token: 0x06000E1B RID: 3611
		[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
		[SuppressUnmanagedCodeSecurity]
		[MonoNativeFunctionWrapper]
		public delegate void SetBodyFlagsDelegate(UIntPtr entityId, uint bodyFlags);

		// Token: 0x02000203 RID: 515
		// (Invoke) Token: 0x06000E1F RID: 3615
		[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
		[SuppressUnmanagedCodeSecurity]
		[MonoNativeFunctionWrapper]
		public delegate void SetBodyFlagsRecursiveDelegate(UIntPtr entityId, uint bodyFlags);

		// Token: 0x02000204 RID: 516
		// (Invoke) Token: 0x06000E23 RID: 3619
		[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
		[SuppressUnmanagedCodeSecurity]
		[MonoNativeFunctionWrapper]
		public delegate void SetBodyShapeDelegate(UIntPtr entityId, UIntPtr shape);

		// Token: 0x02000205 RID: 517
		// (Invoke) Token: 0x06000E27 RID: 3623
		[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
		[SuppressUnmanagedCodeSecurity]
		[MonoNativeFunctionWrapper]
		public delegate void SetBoneFrameToAllMeshesDelegate(UIntPtr entityPtr, int boneIndex, in MatrixFrame frame);

		// Token: 0x02000206 RID: 518
		// (Invoke) Token: 0x06000E2B RID: 3627
		[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
		[SuppressUnmanagedCodeSecurity]
		[MonoNativeFunctionWrapper]
		public delegate void SetBoundingboxDirtyDelegate(UIntPtr entityId);

		// Token: 0x02000207 RID: 519
		// (Invoke) Token: 0x06000E2F RID: 3631
		[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
		[SuppressUnmanagedCodeSecurity]
		[MonoNativeFunctionWrapper]
		public delegate void SetCenterOfMassDelegate(UIntPtr entityId, ref Vec3 localCenterOfMass);

		// Token: 0x02000208 RID: 520
		// (Invoke) Token: 0x06000E33 RID: 3635
		[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
		[SuppressUnmanagedCodeSecurity]
		[MonoNativeFunctionWrapper]
		public delegate void SetClothComponentKeepStateDelegate(UIntPtr entityId, UIntPtr metaMesh, [MarshalAs(UnmanagedType.U1)] bool keepState);

		// Token: 0x02000209 RID: 521
		// (Invoke) Token: 0x06000E37 RID: 3639
		[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
		[SuppressUnmanagedCodeSecurity]
		[MonoNativeFunctionWrapper]
		public delegate void SetClothComponentKeepStateOfAllMeshesDelegate(UIntPtr entityId, [MarshalAs(UnmanagedType.U1)] bool keepState);

		// Token: 0x0200020A RID: 522
		// (Invoke) Token: 0x06000E3B RID: 3643
		[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
		[SuppressUnmanagedCodeSecurity]
		[MonoNativeFunctionWrapper]
		public delegate void SetClothMaxDistanceMultiplierDelegate(UIntPtr gameEntity, float multiplier);

		// Token: 0x0200020B RID: 523
		// (Invoke) Token: 0x06000E3F RID: 3647
		[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
		[SuppressUnmanagedCodeSecurity]
		[MonoNativeFunctionWrapper]
		public delegate void SetColorToAllMeshesWithTagRecursiveDelegate(UIntPtr gameEntity, uint color, byte[] tag);

		// Token: 0x0200020C RID: 524
		// (Invoke) Token: 0x06000E43 RID: 3651
		[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
		[SuppressUnmanagedCodeSecurity]
		[MonoNativeFunctionWrapper]
		public delegate void SetContourStateDelegate(UIntPtr entityId, [MarshalAs(UnmanagedType.U1)] bool alwaysVisible);

		// Token: 0x0200020D RID: 525
		// (Invoke) Token: 0x06000E47 RID: 3655
		[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
		[SuppressUnmanagedCodeSecurity]
		[MonoNativeFunctionWrapper]
		public delegate void SetCostAdderForAttachedFacesDelegate(UIntPtr entityId, float cost);

		// Token: 0x0200020E RID: 526
		// (Invoke) Token: 0x06000E4B RID: 3659
		[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
		[SuppressUnmanagedCodeSecurity]
		[MonoNativeFunctionWrapper]
		public delegate void SetCullModeDelegate(UIntPtr entityPtr, MBMeshCullingMode cullMode);

		// Token: 0x0200020F RID: 527
		// (Invoke) Token: 0x06000E4F RID: 3663
		[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
		[SuppressUnmanagedCodeSecurity]
		[MonoNativeFunctionWrapper]
		public delegate void SetCustomClipPlaneDelegate(UIntPtr entityId, Vec3 position, Vec3 normal, [MarshalAs(UnmanagedType.U1)] bool setForChildren);

		// Token: 0x02000210 RID: 528
		// (Invoke) Token: 0x06000E53 RID: 3667
		[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
		[SuppressUnmanagedCodeSecurity]
		[MonoNativeFunctionWrapper]
		public delegate void SetCustomVertexPositionEnabledDelegate(UIntPtr entityId, [MarshalAs(UnmanagedType.U1)] bool customVertexPositionEnabled);

		// Token: 0x02000211 RID: 529
		// (Invoke) Token: 0x06000E57 RID: 3671
		[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
		[SuppressUnmanagedCodeSecurity]
		[MonoNativeFunctionWrapper]
		public delegate void SetDampingDelegate(UIntPtr entityId, float linearDamping, float angularDamping);

		// Token: 0x02000212 RID: 530
		// (Invoke) Token: 0x06000E5B RID: 3675
		[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
		[SuppressUnmanagedCodeSecurity]
		[MonoNativeFunctionWrapper]
		public delegate void SetDoNotCheckVisibilityDelegate(UIntPtr entityPtr, [MarshalAs(UnmanagedType.U1)] bool value);

		// Token: 0x02000213 RID: 531
		// (Invoke) Token: 0x06000E5F RID: 3679
		[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
		[SuppressUnmanagedCodeSecurity]
		[MonoNativeFunctionWrapper]
		public delegate void SetEnforcedMaximumLodLevelDelegate(UIntPtr entityId, int lodLevel);

		// Token: 0x02000214 RID: 532
		// (Invoke) Token: 0x06000E63 RID: 3683
		[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
		[SuppressUnmanagedCodeSecurity]
		[MonoNativeFunctionWrapper]
		public delegate void SetEntityEnvMapVisibilityDelegate(UIntPtr entityId, [MarshalAs(UnmanagedType.U1)] bool value);

		// Token: 0x02000215 RID: 533
		// (Invoke) Token: 0x06000E67 RID: 3687
		[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
		[SuppressUnmanagedCodeSecurity]
		[MonoNativeFunctionWrapper]
		public delegate void SetEntityFlagsDelegate(UIntPtr entityId, EntityFlags entityFlags);

		// Token: 0x02000216 RID: 534
		// (Invoke) Token: 0x06000E6B RID: 3691
		[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
		[SuppressUnmanagedCodeSecurity]
		[MonoNativeFunctionWrapper]
		public delegate void SetEntityVisibilityFlagsDelegate(UIntPtr entityId, EntityVisibilityFlags entityVisibilityFlags);

		// Token: 0x02000217 RID: 535
		// (Invoke) Token: 0x06000E6F RID: 3695
		[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
		[SuppressUnmanagedCodeSecurity]
		[MonoNativeFunctionWrapper]
		public delegate void SetExternalReferencesUsageDelegate(UIntPtr entityId, [MarshalAs(UnmanagedType.U1)] bool value);

		// Token: 0x02000218 RID: 536
		// (Invoke) Token: 0x06000E73 RID: 3699
		[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
		[SuppressUnmanagedCodeSecurity]
		[MonoNativeFunctionWrapper]
		public delegate void SetFactor2ColorDelegate(UIntPtr entityId, uint factor2Color);

		// Token: 0x02000219 RID: 537
		// (Invoke) Token: 0x06000E77 RID: 3703
		[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
		[SuppressUnmanagedCodeSecurity]
		[MonoNativeFunctionWrapper]
		public delegate void SetFactorColorDelegate(UIntPtr entityId, uint factorColor);

		// Token: 0x0200021A RID: 538
		// (Invoke) Token: 0x06000E7B RID: 3707
		[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
		[SuppressUnmanagedCodeSecurity]
		[MonoNativeFunctionWrapper]
		public delegate void SetForceDecalsToRenderDelegate(UIntPtr entityPtr, [MarshalAs(UnmanagedType.U1)] bool value);

		// Token: 0x0200021B RID: 539
		// (Invoke) Token: 0x06000E7F RID: 3711
		[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
		[SuppressUnmanagedCodeSecurity]
		[MonoNativeFunctionWrapper]
		public delegate void SetForceNotAffectedBySeasonDelegate(UIntPtr entityPtr, [MarshalAs(UnmanagedType.U1)] bool value);

		// Token: 0x0200021C RID: 540
		// (Invoke) Token: 0x06000E83 RID: 3715
		[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
		[SuppressUnmanagedCodeSecurity]
		[MonoNativeFunctionWrapper]
		public delegate void SetFrameChangedDelegate(UIntPtr entityId);

		// Token: 0x0200021D RID: 541
		// (Invoke) Token: 0x06000E87 RID: 3719
		[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
		[SuppressUnmanagedCodeSecurity]
		[MonoNativeFunctionWrapper]
		public delegate void SetGlobalFrameDelegate(UIntPtr entityId, in MatrixFrame frame, [MarshalAs(UnmanagedType.U1)] bool isTeleportation);

		// Token: 0x0200021E RID: 542
		// (Invoke) Token: 0x06000E8B RID: 3723
		[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
		[SuppressUnmanagedCodeSecurity]
		[MonoNativeFunctionWrapper]
		public delegate void SetGlobalPositionDelegate(UIntPtr entityId, in Vec3 position);

		// Token: 0x0200021F RID: 543
		// (Invoke) Token: 0x06000E8F RID: 3727
		[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
		[SuppressUnmanagedCodeSecurity]
		[MonoNativeFunctionWrapper]
		public delegate void SetHasCustomBoundingBoxValidationSystemDelegate(UIntPtr entityId, [MarshalAs(UnmanagedType.U1)] bool hasCustomBoundingBox);

		// Token: 0x02000220 RID: 544
		// (Invoke) Token: 0x06000E93 RID: 3731
		[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
		[SuppressUnmanagedCodeSecurity]
		[MonoNativeFunctionWrapper]
		public delegate void SetLinearVelocityDelegate(UIntPtr entityPtr, Vec3 newLinearVelocity);

		// Token: 0x02000221 RID: 545
		// (Invoke) Token: 0x06000E97 RID: 3735
		[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
		[SuppressUnmanagedCodeSecurity]
		[MonoNativeFunctionWrapper]
		public delegate void SetLocalFrameDelegate(UIntPtr entityId, ref MatrixFrame frame, [MarshalAs(UnmanagedType.U1)] bool isTeleportation);

		// Token: 0x02000222 RID: 546
		// (Invoke) Token: 0x06000E9B RID: 3739
		[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
		[SuppressUnmanagedCodeSecurity]
		[MonoNativeFunctionWrapper]
		public delegate void SetLocalPositionDelegate(UIntPtr entityId, Vec3 position);

		// Token: 0x02000223 RID: 547
		// (Invoke) Token: 0x06000E9F RID: 3743
		[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
		[SuppressUnmanagedCodeSecurity]
		[MonoNativeFunctionWrapper]
		public delegate void SetManualGlobalBoundingBoxDelegate(UIntPtr entityId, Vec3 boundingBoxStartGlobal, Vec3 boundingBoxEndGlobal);

		// Token: 0x02000224 RID: 548
		// (Invoke) Token: 0x06000EA3 RID: 3747
		[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
		[SuppressUnmanagedCodeSecurity]
		[MonoNativeFunctionWrapper]
		public delegate void SetManualLocalBoundingBoxDelegate(UIntPtr entityId, in BoundingBox boundingBox);

		// Token: 0x02000225 RID: 549
		// (Invoke) Token: 0x06000EA7 RID: 3751
		[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
		[SuppressUnmanagedCodeSecurity]
		[MonoNativeFunctionWrapper]
		public delegate void SetMassAndUpdateInertiaAndCenterOfMassDelegate(UIntPtr entityId, float mass);

		// Token: 0x02000226 RID: 550
		// (Invoke) Token: 0x06000EAB RID: 3755
		[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
		[SuppressUnmanagedCodeSecurity]
		[MonoNativeFunctionWrapper]
		public delegate void SetMassSpaceInertiaDelegate(UIntPtr entityId, ref Vec3 inertia);

		// Token: 0x02000227 RID: 551
		// (Invoke) Token: 0x06000EAF RID: 3759
		[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
		[SuppressUnmanagedCodeSecurity]
		[MonoNativeFunctionWrapper]
		public delegate void SetMaterialForAllMeshesDelegate(UIntPtr entityId, UIntPtr materialPointer);

		// Token: 0x02000228 RID: 552
		// (Invoke) Token: 0x06000EB3 RID: 3763
		[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
		[SuppressUnmanagedCodeSecurity]
		[MonoNativeFunctionWrapper]
		public delegate void SetMaxDepenetrationVelocityDelegate(UIntPtr entityId, float maxDepenetrationVelocity);

		// Token: 0x02000229 RID: 553
		// (Invoke) Token: 0x06000EB7 RID: 3767
		[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
		[SuppressUnmanagedCodeSecurity]
		[MonoNativeFunctionWrapper]
		public delegate void SetMobilityDelegate(UIntPtr entityId, GameEntity.Mobility mobility);

		// Token: 0x0200022A RID: 554
		// (Invoke) Token: 0x06000EBB RID: 3771
		[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
		[SuppressUnmanagedCodeSecurity]
		[MonoNativeFunctionWrapper]
		public delegate void SetMorphFrameOfComponentsDelegate(UIntPtr entityId, float value);

		// Token: 0x0200022B RID: 555
		// (Invoke) Token: 0x06000EBF RID: 3775
		[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
		[SuppressUnmanagedCodeSecurity]
		[MonoNativeFunctionWrapper]
		public delegate void SetNameDelegate(UIntPtr entityId, byte[] name);

		// Token: 0x0200022C RID: 556
		// (Invoke) Token: 0x06000EC3 RID: 3779
		[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
		[SuppressUnmanagedCodeSecurity]
		[MonoNativeFunctionWrapper]
		public delegate void SetNativeScriptComponentVariableDelegate(UIntPtr entityPtr, byte[] className, byte[] fieldName, ref ScriptComponentFieldHolder data, RglScriptFieldType variableType);

		// Token: 0x0200022D RID: 557
		// (Invoke) Token: 0x06000EC7 RID: 3783
		[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
		[SuppressUnmanagedCodeSecurity]
		[MonoNativeFunctionWrapper]
		public delegate void SetPhysicsMoveToBatchedDelegate(UIntPtr entityId, [MarshalAs(UnmanagedType.U1)] bool value);

		// Token: 0x0200022E RID: 558
		// (Invoke) Token: 0x06000ECB RID: 3787
		[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
		[SuppressUnmanagedCodeSecurity]
		[MonoNativeFunctionWrapper]
		public delegate void SetPhysicsStateDelegate(UIntPtr entityId, [MarshalAs(UnmanagedType.U1)] bool isEnabled, [MarshalAs(UnmanagedType.U1)] bool setChildren);

		// Token: 0x0200022F RID: 559
		// (Invoke) Token: 0x06000ECF RID: 3791
		[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
		[SuppressUnmanagedCodeSecurity]
		[MonoNativeFunctionWrapper]
		public delegate void SetPhysicsStateOnlyVariableDelegate(UIntPtr entityId, [MarshalAs(UnmanagedType.U1)] bool isEnabled, [MarshalAs(UnmanagedType.U1)] bool setChildren);

		// Token: 0x02000230 RID: 560
		// (Invoke) Token: 0x06000ED3 RID: 3795
		[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
		[SuppressUnmanagedCodeSecurity]
		[MonoNativeFunctionWrapper]
		public delegate void SetPositionsForAttachedNavmeshVerticesDelegate(UIntPtr entityId, IntPtr indices, int indexCount, IntPtr positions);

		// Token: 0x02000231 RID: 561
		// (Invoke) Token: 0x06000ED7 RID: 3799
		[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
		[SuppressUnmanagedCodeSecurity]
		[MonoNativeFunctionWrapper]
		public delegate void SetPreviousFrameInvalidDelegate(UIntPtr gameEntity);

		// Token: 0x02000232 RID: 562
		// (Invoke) Token: 0x06000EDB RID: 3803
		[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
		[SuppressUnmanagedCodeSecurity]
		[MonoNativeFunctionWrapper]
		public delegate void SetReadyToRenderDelegate(UIntPtr entityId, [MarshalAs(UnmanagedType.U1)] bool ready);

		// Token: 0x02000233 RID: 563
		// (Invoke) Token: 0x06000EDF RID: 3807
		[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
		[SuppressUnmanagedCodeSecurity]
		[MonoNativeFunctionWrapper]
		public delegate void SetRuntimeEmissionRateMultiplierDelegate(UIntPtr entityId, float emission_rate_multiplier);

		// Token: 0x02000234 RID: 564
		// (Invoke) Token: 0x06000EE3 RID: 3811
		[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
		[SuppressUnmanagedCodeSecurity]
		[MonoNativeFunctionWrapper]
		public delegate void SetSkeletonDelegate(UIntPtr entityId, UIntPtr skeletonPointer);

		// Token: 0x02000235 RID: 565
		// (Invoke) Token: 0x06000EE7 RID: 3815
		[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
		[SuppressUnmanagedCodeSecurity]
		[MonoNativeFunctionWrapper]
		public delegate void SetSolverIterationCountsDelegate(UIntPtr entityId, int positionIterationCount, int velocityIterationCount);

		// Token: 0x02000236 RID: 566
		// (Invoke) Token: 0x06000EEB RID: 3819
		[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
		[SuppressUnmanagedCodeSecurity]
		[MonoNativeFunctionWrapper]
		public delegate void SetupAdditionalBoneBufferForMeshesDelegate(UIntPtr entityPtr, int boneCount);

		// Token: 0x02000237 RID: 567
		// (Invoke) Token: 0x06000EEF RID: 3823
		[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
		[SuppressUnmanagedCodeSecurity]
		[MonoNativeFunctionWrapper]
		public delegate void SetUpdateValidityOnFrameChangedOfFacesWithIdDelegate(UIntPtr entityId, int faceGroupId, [MarshalAs(UnmanagedType.U1)] bool updateValidity);

		// Token: 0x02000238 RID: 568
		// (Invoke) Token: 0x06000EF3 RID: 3827
		[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
		[SuppressUnmanagedCodeSecurity]
		[MonoNativeFunctionWrapper]
		public delegate void SetUpgradeLevelMaskDelegate(UIntPtr prefab, uint mask);

		// Token: 0x02000239 RID: 569
		// (Invoke) Token: 0x06000EF7 RID: 3831
		[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
		[SuppressUnmanagedCodeSecurity]
		[MonoNativeFunctionWrapper]
		public delegate void SetVectorArgumentDelegate(UIntPtr entityId, float vectorArgument0, float vectorArgument1, float vectorArgument2, float vectorArgument3);

		// Token: 0x0200023A RID: 570
		// (Invoke) Token: 0x06000EFB RID: 3835
		[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
		[SuppressUnmanagedCodeSecurity]
		[MonoNativeFunctionWrapper]
		public delegate void SetVelocityLimitsDelegate(UIntPtr entityId, float maxLinearVelocity, float maxAngularVelocity);

		// Token: 0x0200023B RID: 571
		// (Invoke) Token: 0x06000EFF RID: 3839
		[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
		[SuppressUnmanagedCodeSecurity]
		[MonoNativeFunctionWrapper]
		public delegate void SetVisibilityExcludeParentsDelegate(UIntPtr entityId, [MarshalAs(UnmanagedType.U1)] bool visibility);

		// Token: 0x0200023C RID: 572
		// (Invoke) Token: 0x06000F03 RID: 3843
		[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
		[SuppressUnmanagedCodeSecurity]
		[MonoNativeFunctionWrapper]
		public delegate void SetVisualRecordWakeParamsDelegate(UIntPtr visualRecord, in Vec3 wakeParams);

		// Token: 0x0200023D RID: 573
		// (Invoke) Token: 0x06000F07 RID: 3847
		[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
		[SuppressUnmanagedCodeSecurity]
		[MonoNativeFunctionWrapper]
		public delegate void SetWaterSDFClipDataDelegate(UIntPtr entityId, int slotIndex, in MatrixFrame frame, [MarshalAs(UnmanagedType.U1)] bool visibility);

		// Token: 0x0200023E RID: 574
		// (Invoke) Token: 0x06000F0B RID: 3851
		[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
		[SuppressUnmanagedCodeSecurity]
		[MonoNativeFunctionWrapper]
		public delegate void SetWaterVisualRecordFrameAndDtDelegate(UIntPtr entityPointer, UIntPtr visualPrefab, in MatrixFrame frame, float dt);

		// Token: 0x0200023F RID: 575
		// (Invoke) Token: 0x06000F0F RID: 3855
		[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
		[SuppressUnmanagedCodeSecurity]
		[MonoNativeFunctionWrapper]
		public delegate void SwapPhysxShapeInEntityDelegate(UIntPtr entityPtr, UIntPtr oldShape, UIntPtr newShape, [MarshalAs(UnmanagedType.U1)] bool isVariable);

		// Token: 0x02000240 RID: 576
		// (Invoke) Token: 0x06000F13 RID: 3859
		[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
		[SuppressUnmanagedCodeSecurity]
		[MonoNativeFunctionWrapper]
		public delegate void UpdateAttachedNavigationMeshFacesDelegate(UIntPtr entityId);

		// Token: 0x02000241 RID: 577
		// (Invoke) Token: 0x06000F17 RID: 3863
		[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
		[SuppressUnmanagedCodeSecurity]
		[MonoNativeFunctionWrapper]
		public delegate void UpdateGlobalBoundsDelegate(UIntPtr entityPointer);

		// Token: 0x02000242 RID: 578
		// (Invoke) Token: 0x06000F1B RID: 3867
		[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
		[SuppressUnmanagedCodeSecurity]
		[MonoNativeFunctionWrapper]
		public delegate void UpdateHullWaterEffectFramesDelegate(UIntPtr entityPointer, UIntPtr visualPrefab);

		// Token: 0x02000243 RID: 579
		// (Invoke) Token: 0x06000F1F RID: 3871
		[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
		[SuppressUnmanagedCodeSecurity]
		[MonoNativeFunctionWrapper]
		public delegate void UpdateTriadFrameForEditorDelegate(UIntPtr meshPointer);

		// Token: 0x02000244 RID: 580
		// (Invoke) Token: 0x06000F23 RID: 3875
		[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
		[SuppressUnmanagedCodeSecurity]
		[MonoNativeFunctionWrapper]
		public delegate void UpdateVisibilityMaskDelegate(UIntPtr entityPtr);

		// Token: 0x02000245 RID: 581
		// (Invoke) Token: 0x06000F27 RID: 3879
		[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
		[SuppressUnmanagedCodeSecurity]
		[MonoNativeFunctionWrapper]
		public delegate void ValidateBoundingBoxDelegate(UIntPtr entityPointer);
	}
}
