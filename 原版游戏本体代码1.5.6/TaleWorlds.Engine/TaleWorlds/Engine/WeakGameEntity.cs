using System;
using System.Collections.Generic;
using TaleWorlds.Library;

namespace TaleWorlds.Engine;

public struct WeakGameEntity
{
	public static readonly WeakGameEntity Invalid = new WeakGameEntity(UIntPtr.Zero);

	public UIntPtr Pointer { get; private set; }

	public bool IsValid => Pointer != UIntPtr.Zero;

	public string Name => EngineApplicationInterface.IGameEntity.GetName(Pointer);

	public Scene Scene => EngineApplicationInterface.IGameEntity.GetScene(Pointer);

	public EntityFlags EntityFlags => EngineApplicationInterface.IGameEntity.GetEntityFlags(Pointer);

	public EntityVisibilityFlags EntityVisibilityFlags => EngineApplicationInterface.IGameEntity.GetEntityVisibilityFlags(Pointer);

	public BodyFlags BodyFlag => (BodyFlags)EngineApplicationInterface.IGameEntity.GetBodyFlags(Pointer);

	public BodyFlags PhysicsDescBodyFlag => (BodyFlags)EngineApplicationInterface.IGameEntity.GetPhysicsDescBodyFlags(Pointer);

	public float Mass => EngineApplicationInterface.IGameEntity.GetMass(Pointer);

	public Vec3 CenterOfMass => EngineApplicationInterface.IGameEntity.GetCenterOfMass(Pointer);

	public Vec3 GlobalPosition => GetGlobalFrame().origin;

	public string[] Tags => EngineApplicationInterface.IGameEntity.GetTags(Pointer).Split(new char[1] { ' ' });

	public int ChildCount => EngineApplicationInterface.IGameEntity.GetChildCount(Pointer);

	public WeakGameEntity Parent
	{
		get
		{
			UIntPtr parentPointer = EngineApplicationInterface.IGameEntity.GetParentPointer(Pointer);
			if (!(parentPointer != UIntPtr.Zero))
			{
				return new WeakGameEntity(UIntPtr.Zero);
			}
			return new WeakGameEntity(parentPointer);
		}
	}

	public WeakGameEntity Root => new WeakGameEntity(EngineApplicationInterface.IGameEntity.GetRootParentPointer(Pointer));

	public int MultiMeshComponentCount => EngineApplicationInterface.IGameEntity.GetComponentCount(Pointer, GameEntity.ComponentType.MetaMesh);

	public int ClothSimulatorComponentCount => EngineApplicationInterface.IGameEntity.GetComponentCount(Pointer, GameEntity.ComponentType.ClothSimulator);

	public Vec3 GlobalBoxMax => EngineApplicationInterface.IGameEntity.GetGlobalBoxMax(Pointer);

	public Vec3 GlobalBoxMin => EngineApplicationInterface.IGameEntity.GetGlobalBoxMin(Pointer);

	public Skeleton Skeleton
	{
		get
		{
			return EngineApplicationInterface.IGameEntity.GetSkeleton(Pointer);
		}
		set
		{
			EngineApplicationInterface.IGameEntity.SetSkeleton(Pointer, value?.Pointer ?? UIntPtr.Zero);
		}
	}

	internal WeakGameEntity(UIntPtr pointer)
	{
		Pointer = pointer;
	}

	public void Invalidate()
	{
		Pointer = (UIntPtr)0uL;
	}

	public UIntPtr GetScenePointer()
	{
		return EngineApplicationInterface.IGameEntity.GetScenePointer(Pointer);
	}

	public override string ToString()
	{
		return Pointer.ToString();
	}

	public void ClearEntityComponents(bool resetAll, bool removeScripts, bool deleteChildEntities)
	{
		EngineApplicationInterface.IGameEntity.ClearEntityComponents(Pointer, resetAll, removeScripts, deleteChildEntities);
	}

	public void ClearComponents()
	{
		EngineApplicationInterface.IGameEntity.ClearComponents(Pointer);
	}

	public void ClearOnlyOwnComponents()
	{
		EngineApplicationInterface.IGameEntity.ClearOnlyOwnComponents(Pointer);
	}

	public bool CheckResources(bool addToQueue, bool checkFaceResources)
	{
		return EngineApplicationInterface.IGameEntity.CheckResources(Pointer, addToQueue, checkFaceResources);
	}

	public void SetMobility(GameEntity.Mobility mobility)
	{
		EngineApplicationInterface.IGameEntity.SetMobility(Pointer, mobility);
	}

	public GameEntity.Mobility GetMobility()
	{
		return EngineApplicationInterface.IGameEntity.GetMobility(Pointer);
	}

	public void AddMesh(Mesh mesh, bool recomputeBoundingBox = true)
	{
		EngineApplicationInterface.IGameEntity.AddMesh(Pointer, mesh.Pointer, recomputeBoundingBox);
	}

	public void AddMultiMeshToSkeleton(MetaMesh metaMesh)
	{
		EngineApplicationInterface.IGameEntity.AddMultiMeshToSkeleton(Pointer, metaMesh.Pointer);
	}

	public void AddMultiMeshToSkeletonBone(MetaMesh metaMesh, sbyte boneIndex)
	{
		EngineApplicationInterface.IGameEntity.AddMultiMeshToSkeletonBone(Pointer, metaMesh.Pointer, boneIndex);
	}

	public void SetColorToAllMeshesWithTagRecursive(uint color, string tag)
	{
		EngineApplicationInterface.IGameEntity.SetColorToAllMeshesWithTagRecursive(Pointer, color, tag);
	}

	public IEnumerable<Mesh> GetAllMeshesWithTag(string tag)
	{
		List<WeakGameEntity> children = new List<WeakGameEntity>();
		GetChildrenRecursive(ref children);
		children.Add(this);
		foreach (WeakGameEntity entity in children)
		{
			for (int i = 0; i < entity.MultiMeshComponentCount; i++)
			{
				MetaMesh multiMesh = entity.GetMetaMesh(i);
				for (int j = 0; j < multiMesh.MeshCount; j++)
				{
					Mesh meshAtIndex = multiMesh.GetMeshAtIndex(j);
					if (meshAtIndex.HasTag(tag))
					{
						yield return meshAtIndex;
					}
				}
			}
			for (int i = 0; i < entity.ClothSimulatorComponentCount; i++)
			{
				ClothSimulatorComponent clothSimulator = entity.GetClothSimulator(i);
				MetaMesh multiMesh = clothSimulator.GetFirstMetaMesh();
				for (int j = 0; j < multiMesh.MeshCount; j++)
				{
					Mesh meshAtIndex2 = multiMesh.GetMeshAtIndex(j);
					if (meshAtIndex2.HasTag(tag))
					{
						yield return meshAtIndex2;
					}
				}
			}
		}
	}

	public void SetName(string name)
	{
		EngineApplicationInterface.IGameEntity.SetName(Pointer, name);
	}

	public void SetEntityFlags(EntityFlags flags)
	{
		EngineApplicationInterface.IGameEntity.SetEntityFlags(Pointer, flags);
	}

	public void SetEntityVisibilityFlags(EntityVisibilityFlags flags)
	{
		EngineApplicationInterface.IGameEntity.SetEntityVisibilityFlags(Pointer, flags);
	}

	public PhysicsMaterial GetPhysicsMaterial()
	{
		return PhysicsMaterial.GetFromIndex(EngineApplicationInterface.IGameEntity.GetPhysicsMaterialIndex(Pointer));
	}

	public void SetBodyFlags(BodyFlags flags)
	{
		EngineApplicationInterface.IGameEntity.SetBodyFlags(Pointer, (uint)flags);
	}

	public void SetBodyFlagsRecursive(BodyFlags bodyFlags)
	{
		EngineApplicationInterface.IGameEntity.SetBodyFlagsRecursive(Pointer, (uint)bodyFlags);
	}

	public void AddBodyFlags(BodyFlags bodyFlags, bool applyToChildren = true)
	{
		SetBodyFlags(BodyFlag | bodyFlags);
		if (!applyToChildren)
		{
			return;
		}
		foreach (WeakGameEntity child in GetChildren())
		{
			child.AddBodyFlags(bodyFlags);
		}
	}

	internal static WeakGameEntity GetFirstEntityWithTag(Scene scene, string tag)
	{
		return new WeakGameEntity(EngineApplicationInterface.IGameEntity.GetFirstEntityWithTag(scene.Pointer, tag));
	}

	internal static WeakGameEntity GetNextEntityWithTag(Scene scene, WeakGameEntity startEntity, string tag)
	{
		if (!(startEntity == null))
		{
			return new WeakGameEntity(EngineApplicationInterface.IGameEntity.GetNextEntityWithTag(startEntity.Pointer, tag));
		}
		return GetFirstEntityWithTag(scene, tag);
	}

	internal static WeakGameEntity GetFirstEntityWithTagExpression(Scene scene, string tagExpression)
	{
		return new WeakGameEntity(EngineApplicationInterface.IGameEntity.GetFirstEntityWithTagExpression(scene.Pointer, tagExpression));
	}

	internal static WeakGameEntity GetNextEntityWithTagExpression(Scene scene, WeakGameEntity startEntity, string tagExpression)
	{
		if (startEntity == null)
		{
			return GetFirstEntityWithTagExpression(scene, tagExpression);
		}
		UIntPtr nextEntityWithTagExpression = EngineApplicationInterface.IGameEntity.GetNextEntityWithTagExpression(startEntity.Pointer, tagExpression);
		if (nextEntityWithTagExpression != UIntPtr.Zero)
		{
			return new WeakGameEntity(nextEntityWithTagExpression);
		}
		return Invalid;
	}

	internal static IEnumerable<WeakGameEntity> GetEntitiesWithTag(Scene scene, string tag)
	{
		WeakGameEntity entity = GetFirstEntityWithTag(scene, tag);
		while (entity != null)
		{
			yield return entity;
			entity = GetNextEntityWithTag(scene, entity, tag);
		}
	}

	internal static IEnumerable<WeakGameEntity> GetEntitiesWithTagExpression(Scene scene, string tagExpression)
	{
		WeakGameEntity entity = GetFirstEntityWithTagExpression(scene, tagExpression);
		while (entity != null)
		{
			yield return entity;
			entity = GetNextEntityWithTagExpression(scene, entity, tagExpression);
		}
	}

	public void RemoveBodyFlags(BodyFlags bodyFlags, bool applyToChildren = true)
	{
		SetBodyFlags(BodyFlag & ~bodyFlags);
		if (!applyToChildren)
		{
			return;
		}
		foreach (WeakGameEntity child in GetChildren())
		{
			child.RemoveBodyFlags(bodyFlags);
		}
	}

	public void SetLocalPosition(Vec3 position)
	{
		EngineApplicationInterface.IGameEntity.SetLocalPosition(Pointer, position);
	}

	public void SetGlobalPosition(Vec3 position)
	{
		EngineApplicationInterface.IGameEntity.SetGlobalPosition(Pointer, in position);
	}

	public void SetColor(uint color1, uint color2, string meshTag)
	{
		foreach (Mesh item in GetAllMeshesWithTag(meshTag))
		{
			item.Color = color1;
			item.Color2 = color2;
		}
	}

	public uint GetFactorColor()
	{
		return EngineApplicationInterface.IGameEntity.GetFactorColor(Pointer);
	}

	public void SetFactorColor(uint color)
	{
		EngineApplicationInterface.IGameEntity.SetFactorColor(Pointer, color);
	}

	public void SetAsReplayEntity()
	{
		EngineApplicationInterface.IGameEntity.SetAsReplayEntity(Pointer);
	}

	public void SetClothMaxDistanceMultiplier(float multiplier)
	{
		EngineApplicationInterface.IGameEntity.SetClothMaxDistanceMultiplier(Pointer, multiplier);
	}

	public void RemoveMultiMeshFromSkeleton(MetaMesh metaMesh)
	{
		EngineApplicationInterface.IGameEntity.RemoveMultiMeshFromSkeleton(Pointer, metaMesh.Pointer);
	}

	public void RemoveMultiMeshFromSkeletonBone(MetaMesh metaMesh, sbyte boneIndex)
	{
		EngineApplicationInterface.IGameEntity.RemoveMultiMeshFromSkeletonBone(Pointer, metaMesh.Pointer, boneIndex);
	}

	public bool RemoveComponentWithMesh(Mesh mesh)
	{
		return EngineApplicationInterface.IGameEntity.RemoveComponentWithMesh(Pointer, mesh.Pointer);
	}

	public void AddComponent(GameEntityComponent component)
	{
		EngineApplicationInterface.IGameEntity.AddComponent(Pointer, component.Pointer);
	}

	public bool HasComponent(GameEntityComponent component)
	{
		return EngineApplicationInterface.IGameEntity.HasComponent(Pointer, component.Pointer);
	}

	public bool IsInEditorScene()
	{
		return false;
	}

	public bool RemoveComponent(GameEntityComponent component)
	{
		return EngineApplicationInterface.IGameEntity.RemoveComponent(Pointer, component.Pointer);
	}

	public string GetGuid()
	{
		return EngineApplicationInterface.IGameEntity.GetGuid(Pointer);
	}

	public bool IsGuidValid()
	{
		return EngineApplicationInterface.IGameEntity.IsGuidValid(Pointer);
	}

	public void SetEnforcedMaximumLodLevel(int lodLevel)
	{
		EngineApplicationInterface.IGameEntity.SetEnforcedMaximumLodLevel(Pointer, lodLevel);
	}

	public float GetLodLevelForDistanceSq(float distSq)
	{
		return EngineApplicationInterface.IGameEntity.GetLodLevelForDistanceSq(Pointer, distSq);
	}

	public void GetQuickBoneEntitialFrame(sbyte index, out MatrixFrame frame)
	{
		EngineApplicationInterface.IGameEntity.GetQuickBoneEntitialFrame(Pointer, index, out frame);
	}

	public void UpdateVisibilityMask()
	{
		EngineApplicationInterface.IGameEntity.UpdateVisibilityMask(Pointer);
	}

	public void CallScriptCallbacks(bool registerScriptComponents)
	{
		EngineApplicationInterface.IGameEntity.CallScriptCallbacks(Pointer, registerScriptComponents);
	}

	public int GetScriptCount()
	{
		return EngineApplicationInterface.IGameEntity.GetScriptComponentCount(Pointer);
	}

	public bool IsGhostObject()
	{
		return EngineApplicationInterface.IGameEntity.IsGhostObject(Pointer);
	}

	public void CreateAndAddScriptComponent(string name, bool callScriptCallbacks)
	{
		EngineApplicationInterface.IGameEntity.CreateAndAddScriptComponent(Pointer, name, callScriptCallbacks);
	}

	public void RemoveScriptComponent(UIntPtr scriptComponent, int removeReason)
	{
		EngineApplicationInterface.IGameEntity.RemoveScriptComponent(Pointer, scriptComponent, removeReason);
	}

	public void SetEntityEnvMapVisibility(bool value)
	{
		EngineApplicationInterface.IGameEntity.SetEntityEnvMapVisibility(Pointer, value);
	}

	public ScriptComponentBehavior GetScriptAtIndex(int index)
	{
		return EngineApplicationInterface.IGameEntity.GetScriptComponentAtIndex(Pointer, index);
	}

	internal int GetScriptComponentIndex(uint nameHash)
	{
		return EngineApplicationInterface.IGameEntity.GetScriptComponentIndex(Pointer, nameHash);
	}

	public bool HasScene()
	{
		return EngineApplicationInterface.IGameEntity.HasScene(Pointer);
	}

	public bool HasScriptComponent(string scName)
	{
		return EngineApplicationInterface.IGameEntity.HasScriptComponent(Pointer, scName);
	}

	public bool HasScriptComponent(uint scNameHash)
	{
		return EngineApplicationInterface.IGameEntity.HasScriptComponentHash(Pointer, scNameHash);
	}

	public IEnumerable<ScriptComponentBehavior> GetScriptComponents()
	{
		int count = GetScriptCount();
		for (int i = 0; i < count; i++)
		{
			yield return GetScriptAtIndex(i);
		}
	}

	public IEnumerable<T> GetScriptComponents<T>() where T : ScriptComponentBehavior
	{
		int count = GetScriptCount();
		for (int i = 0; i < count; i++)
		{
			if (GetScriptAtIndex(i) is T val)
			{
				yield return val;
			}
		}
	}

	public bool HasScriptOfType<T>() where T : ScriptComponentBehavior
	{
		int scriptCount = GetScriptCount();
		for (int i = 0; i < scriptCount; i++)
		{
			if (GetScriptAtIndex(i) is T)
			{
				return true;
			}
		}
		return false;
	}

	public bool HasScriptWithInterfaceOfType<T>()
	{
		int scriptCount = GetScriptCount();
		for (int i = 0; i < scriptCount; i++)
		{
			if (GetScriptAtIndex(i) is T)
			{
				return true;
			}
		}
		return false;
	}

	public T GetFirstScriptOfTypeInFamily<T>() where T : ScriptComponentBehavior
	{
		T firstScriptOfType = GetFirstScriptOfType<T>();
		WeakGameEntity weakGameEntity = this;
		while (firstScriptOfType == null)
		{
			WeakGameEntity parent = weakGameEntity.Parent;
			if (!parent.IsValid)
			{
				break;
			}
			weakGameEntity = parent;
			firstScriptOfType = weakGameEntity.GetFirstScriptOfType<T>();
		}
		return firstScriptOfType;
	}

	public ScriptComponentBehavior GetFirstScriptWithNameHash(uint nameHash)
	{
		int scriptComponentIndex = GetScriptComponentIndex(nameHash);
		if (scriptComponentIndex != -1)
		{
			return GetScriptAtIndex(scriptComponentIndex);
		}
		return null;
	}

	public T GetFirstScriptOfType<T>() where T : ScriptComponentBehavior
	{
		int scriptCount = GetScriptCount();
		for (int i = 0; i < scriptCount; i++)
		{
			if (GetScriptAtIndex(i) is T result)
			{
				return result;
			}
		}
		return null;
	}

	public T GetFirstScriptWithInterfaceOfType<T>() where T : class
	{
		int scriptCount = GetScriptCount();
		for (int i = 0; i < scriptCount; i++)
		{
			if (GetScriptAtIndex(i) is T result)
			{
				return result;
			}
		}
		return null;
	}

	public T GetFirstScriptOfTypeRecursive<T>() where T : ScriptComponentBehavior
	{
		int scriptCount = GetScriptCount();
		for (int i = 0; i < scriptCount; i++)
		{
			if (GetScriptAtIndex(i) is T result)
			{
				return result;
			}
		}
		scriptCount = ChildCount;
		for (int j = 0; j < scriptCount; j++)
		{
			T firstScriptOfTypeRecursive = GetChild(j).GetFirstScriptOfTypeRecursive<T>();
			if (firstScriptOfTypeRecursive != null)
			{
				return firstScriptOfTypeRecursive;
			}
		}
		return null;
	}

	public WeakGameEntity GetFirstChildEntityWithTag(string tag)
	{
		foreach (WeakGameEntity child in GetChildren())
		{
			if (child.HasTag(tag))
			{
				return child;
			}
		}
		return Invalid;
	}

	public int GetScriptCountOfType<T>() where T : ScriptComponentBehavior
	{
		int scriptCount = GetScriptCount();
		int num = 0;
		for (int i = 0; i < scriptCount; i++)
		{
			if (GetScriptAtIndex(i) is T)
			{
				num++;
			}
		}
		return num;
	}

	public int GetScriptCountOfTypeRecursive<T>() where T : ScriptComponentBehavior
	{
		int scriptCount = GetScriptCount();
		int num = 0;
		for (int i = 0; i < scriptCount; i++)
		{
			if (GetScriptAtIndex(i) is T)
			{
				num++;
			}
		}
		scriptCount = ChildCount;
		for (int j = 0; j < scriptCount; j++)
		{
			num += GetChild(j).GetScriptCountOfTypeRecursive<T>();
		}
		return num;
	}

	internal static GameEntity GetFirstEntityWithName(Scene scene, string entityName)
	{
		return EngineApplicationInterface.IGameEntity.FindWithName(scene.Pointer, entityName);
	}

	public void SetAlpha(float alpha)
	{
		EngineApplicationInterface.IGameEntity.SetAlpha(Pointer, alpha);
	}

	public void SetVisibilityExcludeParents(bool visible)
	{
		EngineApplicationInterface.IGameEntity.SetVisibilityExcludeParents(Pointer, visible);
	}

	public void SetReadyToRender(bool ready)
	{
		EngineApplicationInterface.IGameEntity.SetReadyToRender(Pointer, ready);
	}

	public bool GetVisibilityExcludeParents()
	{
		return EngineApplicationInterface.IGameEntity.GetVisibilityExcludeParents(Pointer);
	}

	public bool IsVisibleIncludeParents()
	{
		return EngineApplicationInterface.IGameEntity.IsVisibleIncludeParents(Pointer);
	}

	public uint GetVisibilityLevelMaskIncludingParents()
	{
		return EngineApplicationInterface.IGameEntity.GetVisibilityLevelMaskIncludingParents(Pointer);
	}

	public bool GetEditModeLevelVisibility()
	{
		return EngineApplicationInterface.IGameEntity.GetEditModeLevelVisibility(Pointer);
	}

	public void Remove(int removeReason)
	{
		EngineApplicationInterface.IGameEntity.Remove(Pointer, removeReason);
	}

	public void SetUpgradeLevelMask(GameEntity.UpgradeLevelMask mask)
	{
		EngineApplicationInterface.IGameEntity.SetUpgradeLevelMask(Pointer, (uint)mask);
	}

	public GameEntity.UpgradeLevelMask GetUpgradeLevelMask()
	{
		return (GameEntity.UpgradeLevelMask)EngineApplicationInterface.IGameEntity.GetUpgradeLevelMask(Pointer);
	}

	public GameEntity.UpgradeLevelMask GetUpgradeLevelMaskCumulative()
	{
		return (GameEntity.UpgradeLevelMask)EngineApplicationInterface.IGameEntity.GetUpgradeLevelMaskCumulative(Pointer);
	}

	public int GetUpgradeLevelOfEntity()
	{
		int upgradeLevelMask = (int)GetUpgradeLevelMask();
		if ((upgradeLevelMask & 1) > 0)
		{
			return 0;
		}
		if ((upgradeLevelMask & 2) > 0)
		{
			return 1;
		}
		if ((upgradeLevelMask & 4) > 0)
		{
			return 2;
		}
		if ((upgradeLevelMask & 8) > 0)
		{
			return 3;
		}
		return -1;
	}

	public string GetOldPrefabName()
	{
		return EngineApplicationInterface.IGameEntity.GetOldPrefabName(Pointer);
	}

	public string GetPrefabName()
	{
		return EngineApplicationInterface.IGameEntity.GetPrefabName(Pointer);
	}

	public void RefreshMeshesToRenderToHullWater(UIntPtr visualRecord, string entityTag)
	{
		EngineApplicationInterface.IGameEntity.RefreshMeshesToRenderToHullWater(Pointer, visualRecord, entityTag);
	}

	public void DeRegisterWaterMeshMaterials(UIntPtr visualRecord)
	{
		EngineApplicationInterface.IGameEntity.DeRegisterWaterMeshMaterials(Pointer, visualRecord);
	}

	public void SetVisualRecordWakeParams(UIntPtr visualRecord, Vec3 wakeParams)
	{
		EngineApplicationInterface.IGameEntity.SetVisualRecordWakeParams(visualRecord, in wakeParams);
	}

	public void ChangeResolutionMultiplierOfWaterVisual(UIntPtr visualRecord, float multiplier, in Vec3 waterEffectsBB)
	{
		EngineApplicationInterface.IGameEntity.ChangeResolutionMultiplierOfWaterVisual(visualRecord, multiplier, in waterEffectsBB);
	}

	public void ResetHullWater(UIntPtr visualRecord)
	{
		EngineApplicationInterface.IGameEntity.ResetHullWater(visualRecord);
	}

	public void SetWaterVisualRecordFrameAndDt(UIntPtr visualRecord, MatrixFrame frame, float dt)
	{
		EngineApplicationInterface.IGameEntity.SetWaterVisualRecordFrameAndDt(Pointer, visualRecord, in frame, dt);
	}

	public void AddSplashPositionToWaterVisualRecord(UIntPtr visualRecord, Vec3 position)
	{
		EngineApplicationInterface.IGameEntity.AddSplashPositionToWaterVisualRecord(Pointer, visualRecord, in position);
	}

	public void UpdateHullWaterEffectFrames(UIntPtr visualRecord)
	{
		EngineApplicationInterface.IGameEntity.UpdateHullWaterEffectFrames(Pointer, visualRecord);
	}

	public void CopyScriptComponentFromAnotherEntity(GameEntity otherEntity, string scriptName)
	{
		EngineApplicationInterface.IGameEntity.CopyScriptComponentFromAnotherEntity(Pointer, otherEntity.Pointer, scriptName);
	}

	public void SetFrame(ref MatrixFrame frame, bool isTeleportation = true)
	{
		EngineApplicationInterface.IGameEntity.SetLocalFrame(Pointer, ref frame, isTeleportation);
	}

	public void SetLocalFrame(ref MatrixFrame frame, bool isTeleportation)
	{
		EngineApplicationInterface.IGameEntity.SetLocalFrame(Pointer, ref frame, isTeleportation);
	}

	public void SetClothComponentKeepState(MetaMesh metaMesh, bool state)
	{
		EngineApplicationInterface.IGameEntity.SetClothComponentKeepState(Pointer, metaMesh.Pointer, state);
	}

	public void SetClothComponentKeepStateOfAllMeshes(bool state)
	{
		EngineApplicationInterface.IGameEntity.SetClothComponentKeepStateOfAllMeshes(Pointer, state);
	}

	public void SetPreviousFrameInvalid()
	{
		EngineApplicationInterface.IGameEntity.SetPreviousFrameInvalid(Pointer);
	}

	public MatrixFrame GetFrame()
	{
		EngineApplicationInterface.IGameEntity.GetLocalFrame(Pointer, out var outFrame);
		return outFrame;
	}

	public void GetLocalFrame(out MatrixFrame frame)
	{
		EngineApplicationInterface.IGameEntity.GetLocalFrame(Pointer, out frame);
	}

	public bool HasBatchedKinematicPhysicsFlag()
	{
		return EngineApplicationInterface.IGameEntity.HasBatchedKinematicPhysicsFlag(Pointer);
	}

	public bool HasBatchedRayCastPhysicsFlag()
	{
		return EngineApplicationInterface.IGameEntity.HasBatchedRayCastPhysicsFlag(Pointer);
	}

	public MatrixFrame GetLocalFrame()
	{
		EngineApplicationInterface.IGameEntity.GetLocalFrame(Pointer, out var outFrame);
		return outFrame;
	}

	public MatrixFrame GetGlobalFrame()
	{
		EngineApplicationInterface.IGameEntity.GetGlobalFrame(Pointer, out var outFrame);
		return outFrame;
	}

	public void SetWaterSDFClipData(int slotIndex, in MatrixFrame frame, bool visibility)
	{
		EngineApplicationInterface.IGameEntity.SetWaterSDFClipData(Pointer, slotIndex, in frame, visibility);
	}

	public int RegisterWaterSDFClip(Texture sdfTexture)
	{
		return EngineApplicationInterface.IGameEntity.RegisterWaterSDFClip(Pointer, (sdfTexture != null) ? sdfTexture.Pointer : UIntPtr.Zero);
	}

	public void DeRegisterWaterSDFClip(int slot)
	{
		EngineApplicationInterface.IGameEntity.DeRegisterWaterSDFClip(Pointer, slot);
	}

	public MatrixFrame GetGlobalFrameImpreciseForFixedTick()
	{
		EngineApplicationInterface.IGameEntity.GetGlobalFrameImpreciseForFixedTick(Pointer, out var outFrame);
		return outFrame;
	}

	public MatrixFrame ComputePreciseGlobalFrameForFixedTickSlow()
	{
		MatrixFrame m = GetLocalFrame();
		WeakGameEntity parent = Parent;
		while (parent.Parent != null)
		{
			m = parent.GetLocalFrame().TransformToParent(in m);
			parent = parent.Parent;
		}
		return parent.GetBodyWorldTransform().TransformToParent(in m);
	}

	public void SetGlobalFrame(in MatrixFrame frame, bool isTeleportation = true)
	{
		EngineApplicationInterface.IGameEntity.SetGlobalFrame(Pointer, in frame, isTeleportation);
	}

	public MatrixFrame GetPreviousGlobalFrame()
	{
		EngineApplicationInterface.IGameEntity.GetPreviousGlobalFrame(Pointer, out var frame);
		return frame;
	}

	public MatrixFrame GetBodyWorldTransform()
	{
		EngineApplicationInterface.IGameEntity.GetBodyWorldTransform(Pointer, out var frame);
		return frame;
	}

	public MatrixFrame GetBodyVisualWorldTransform()
	{
		EngineApplicationInterface.IGameEntity.GetBodyVisualWorldTransform(Pointer, out var frame);
		return frame;
	}

	public void UpdateTriadFrameForEditor()
	{
		EngineApplicationInterface.IGameEntity.UpdateTriadFrameForEditor(Pointer);
	}

	public void UpdateTriadFrameForEditorForAllChildren()
	{
		UpdateTriadFrameForEditor();
		List<WeakGameEntity> children = new List<WeakGameEntity>();
		GetChildrenRecursive(ref children);
		foreach (WeakGameEntity item in children)
		{
			EngineApplicationInterface.IGameEntity.UpdateTriadFrameForEditor(item.Pointer);
		}
	}

	public Vec3 GetGlobalScale()
	{
		return EngineApplicationInterface.IGameEntity.GetGlobalScale(Pointer);
	}

	public Vec3 GetLocalScale()
	{
		return GetFrame().rotation.GetScaleVector();
	}

	public void SetAnimationSoundActivation(bool activate)
	{
		EngineApplicationInterface.IGameEntity.SetAnimationSoundActivation(Pointer, activate);
		foreach (WeakGameEntity child in GetChildren())
		{
			child.SetAnimationSoundActivation(activate);
		}
	}

	public void CopyComponentsToSkeleton()
	{
		EngineApplicationInterface.IGameEntity.CopyComponentsToSkeleton(Pointer);
	}

	public void AddMeshToBone(sbyte boneIndex, Mesh mesh)
	{
		EngineApplicationInterface.IGameEntity.AddMeshToBone(Pointer, mesh.Pointer, boneIndex);
	}

	public void ActivateRagdoll()
	{
		EngineApplicationInterface.IGameEntity.ActivateRagdoll(Pointer);
	}

	public void PauseSkeletonAnimation()
	{
		EngineApplicationInterface.IGameEntity.Freeze(Pointer, isFrozen: true);
	}

	public void ResumeSkeletonAnimation()
	{
		EngineApplicationInterface.IGameEntity.Freeze(Pointer, isFrozen: false);
	}

	public bool IsSkeletonAnimationPaused()
	{
		return EngineApplicationInterface.IGameEntity.IsFrozen(Pointer);
	}

	public sbyte GetBoneCount()
	{
		return EngineApplicationInterface.IGameEntity.GetBoneCount(Pointer);
	}

	public float GetWaterLevelAtPosition(Vec2 position, bool useWaterRenderer, bool checkWaterBodyEntities)
	{
		return EngineApplicationInterface.IGameEntity.GetWaterLevelAtPosition(Pointer, in position, useWaterRenderer, checkWaterBodyEntities);
	}

	public MatrixFrame GetBoneEntitialFrameWithIndex(sbyte boneIndex)
	{
		MatrixFrame outEntitialFrame = default(MatrixFrame);
		EngineApplicationInterface.IGameEntity.GetBoneEntitialFrameWithIndex(Pointer, boneIndex, ref outEntitialFrame);
		return outEntitialFrame;
	}

	public MatrixFrame GetBoneEntitialFrameWithName(string boneName)
	{
		MatrixFrame outEntitialFrame = default(MatrixFrame);
		EngineApplicationInterface.IGameEntity.GetBoneEntitialFrameWithName(Pointer, boneName, ref outEntitialFrame);
		return outEntitialFrame;
	}

	public void AddTag(string tag)
	{
		EngineApplicationInterface.IGameEntity.AddTag(Pointer, tag);
	}

	public void RemoveTag(string tag)
	{
		EngineApplicationInterface.IGameEntity.RemoveTag(Pointer, tag);
	}

	public bool HasTag(string tag)
	{
		return EngineApplicationInterface.IGameEntity.HasTag(Pointer, tag);
	}

	public void AddChild(WeakGameEntity gameEntity, bool autoLocalizeFrame = false)
	{
		EngineApplicationInterface.IGameEntity.AddChild(Pointer, gameEntity.Pointer, autoLocalizeFrame);
	}

	public void RemoveChild(WeakGameEntity childEntity, bool keepPhysics, bool keepScenePointer, bool callScriptCallbacks, int removeReason)
	{
		EngineApplicationInterface.IGameEntity.RemoveChild(Pointer, childEntity.Pointer, keepPhysics, keepScenePointer, callScriptCallbacks, removeReason);
	}

	public void BreakPrefab()
	{
		EngineApplicationInterface.IGameEntity.BreakPrefab(Pointer);
	}

	public WeakGameEntity GetChild(int index)
	{
		UIntPtr childPointer = EngineApplicationInterface.IGameEntity.GetChildPointer(Pointer, index);
		if (!(childPointer != UIntPtr.Zero))
		{
			return new WeakGameEntity(UIntPtr.Zero);
		}
		return new WeakGameEntity(childPointer);
	}

	public bool HasComplexAnimTree()
	{
		return EngineApplicationInterface.IGameEntity.HasComplexAnimTree(Pointer);
	}

	public void AddMultiMesh(MetaMesh metaMesh, bool updateVisMask = true)
	{
		EngineApplicationInterface.IGameEntity.AddMultiMesh(Pointer, metaMesh.Pointer, updateVisMask);
	}

	public bool RemoveMultiMesh(MetaMesh metaMesh)
	{
		return EngineApplicationInterface.IGameEntity.RemoveMultiMesh(Pointer, metaMesh.Pointer);
	}

	public int GetComponentCount(GameEntity.ComponentType componentType)
	{
		return EngineApplicationInterface.IGameEntity.GetComponentCount(Pointer, componentType);
	}

	public void AddAllMeshesOfGameEntity(GameEntity gameEntity)
	{
		EngineApplicationInterface.IGameEntity.AddAllMeshesOfGameEntity(Pointer, gameEntity.Pointer);
	}

	public void SetFrameChanged()
	{
		EngineApplicationInterface.IGameEntity.SetFrameChanged(Pointer);
	}

	public GameEntityComponent GetComponentAtIndex(int index, GameEntity.ComponentType componentType)
	{
		return EngineApplicationInterface.IGameEntity.GetComponentAtIndex(Pointer, componentType, index);
	}

	public MetaMesh GetMetaMesh(int metaMeshIndex)
	{
		return (MetaMesh)EngineApplicationInterface.IGameEntity.GetComponentAtIndex(Pointer, GameEntity.ComponentType.MetaMesh, metaMeshIndex);
	}

	public ClothSimulatorComponent GetClothSimulator(int clothSimulatorIndex)
	{
		return (ClothSimulatorComponent)EngineApplicationInterface.IGameEntity.GetComponentAtIndex(Pointer, GameEntity.ComponentType.ClothSimulator, clothSimulatorIndex);
	}

	public void SetVectorArgument(float vectorArgument0, float vectorArgument1, float vectorArgument2, float vectorArgument3)
	{
		EngineApplicationInterface.IGameEntity.SetVectorArgument(Pointer, vectorArgument0, vectorArgument1, vectorArgument2, vectorArgument3);
	}

	public void SetMaterialForAllMeshes(Material material)
	{
		EngineApplicationInterface.IGameEntity.SetMaterialForAllMeshes(Pointer, material.Pointer);
	}

	public bool AddLight(Light light)
	{
		return EngineApplicationInterface.IGameEntity.AddLight(Pointer, light.Pointer);
	}

	public Light GetLight()
	{
		return EngineApplicationInterface.IGameEntity.GetLight(Pointer);
	}

	public void AddParticleSystemComponent(string particleid)
	{
		EngineApplicationInterface.IGameEntity.AddParticleSystemComponent(Pointer, particleid);
	}

	public void RemoveAllParticleSystems()
	{
		EngineApplicationInterface.IGameEntity.RemoveAllParticleSystems(Pointer);
	}

	public bool CheckPointWithOrientedBoundingBox(Vec3 point)
	{
		return EngineApplicationInterface.IGameEntity.CheckPointWithOrientedBoundingBox(Pointer, point);
	}

	public void PauseParticleSystem(bool doChildren)
	{
		EngineApplicationInterface.IGameEntity.PauseParticleSystem(Pointer, doChildren);
	}

	public void ResumeParticleSystem(bool doChildren)
	{
		EngineApplicationInterface.IGameEntity.ResumeParticleSystem(Pointer, doChildren);
	}

	public void BurstEntityParticle(bool doChildren)
	{
		EngineApplicationInterface.IGameEntity.BurstEntityParticle(Pointer, doChildren);
	}

	public void SetRuntimeEmissionRateMultiplier(float emissionRateMultiplier)
	{
		EngineApplicationInterface.IGameEntity.SetRuntimeEmissionRateMultiplier(Pointer, emissionRateMultiplier);
	}

	public BoundingBox GetLocalBoundingBox()
	{
		return EngineApplicationInterface.IGameEntity.GetLocalBoundingBox(Pointer);
	}

	public BoundingBox GetGlobalBoundingBox()
	{
		return EngineApplicationInterface.IGameEntity.GetGlobalBoundingBox(Pointer);
	}

	public Vec3 GetBoundingBoxMin()
	{
		return EngineApplicationInterface.IGameEntity.GetBoundingBoxMin(Pointer);
	}

	public void SetHasCustomBoundingBoxValidationSystem(bool hasCustomBoundingBox)
	{
		EngineApplicationInterface.IGameEntity.SetHasCustomBoundingBoxValidationSystem(Pointer, hasCustomBoundingBox);
	}

	public void ValidateBoundingBox()
	{
		EngineApplicationInterface.IGameEntity.ValidateBoundingBox(Pointer);
	}

	public Vec3 GetBoundingBoxMax()
	{
		return EngineApplicationInterface.IGameEntity.GetBoundingBoxMax(Pointer);
	}

	public void UpdateGlobalBounds()
	{
		EngineApplicationInterface.IGameEntity.UpdateGlobalBounds(Pointer);
	}

	public void RecomputeBoundingBox()
	{
		EngineApplicationInterface.IGameEntity.RecomputeBoundingBox(Pointer);
	}

	public float GetBoundingBoxRadius()
	{
		return EngineApplicationInterface.IGameEntity.GetRadius(Pointer);
	}

	public void SetBoundingboxDirty()
	{
		EngineApplicationInterface.IGameEntity.SetBoundingboxDirty(Pointer);
	}

	public (Vec3, Vec3) ComputeGlobalPhysicsBoundingBoxMinMax()
	{
		MatrixFrame globalFrame = GetGlobalFrame();
		BoundingBox localPhysicsBoundingBox = this.GetLocalPhysicsBoundingBox(includeChildren: true);
		Vec3 item = globalFrame.TransformToParent(in localPhysicsBoundingBox.min);
		Vec3 item2 = globalFrame.TransformToParent(in localPhysicsBoundingBox.max);
		return (item, item2);
	}

	public Vec3 ComputeGlobalPhysicsBoundingBoxCenter()
	{
		MatrixFrame globalFrame = GetGlobalFrame();
		BoundingBox localPhysicsBoundingBox = this.GetLocalPhysicsBoundingBox(includeChildren: true);
		return globalFrame.TransformToParent(in localPhysicsBoundingBox.center);
	}

	public void SetContourColor(uint? color, bool alwaysVisible = true)
	{
		if (color.HasValue)
		{
			EngineApplicationInterface.IGameEntity.SetAsContourEntity(Pointer, color.Value);
			EngineApplicationInterface.IGameEntity.SetContourState(Pointer, alwaysVisible);
		}
		else
		{
			EngineApplicationInterface.IGameEntity.DisableContour(Pointer);
		}
	}

	public bool GetHasFrameChanged()
	{
		return EngineApplicationInterface.IGameEntity.HasFrameChanged(Pointer);
	}

	public Mesh GetFirstMesh()
	{
		return EngineApplicationInterface.IGameEntity.GetFirstMesh(Pointer);
	}

	public int GetAttachedNavmeshFaceCount()
	{
		return EngineApplicationInterface.IGameEntity.GetAttachedNavmeshFaceCount(Pointer);
	}

	public void GetAttachedNavmeshFaceRecords(PathFaceRecord[] faceRecords)
	{
		EngineApplicationInterface.IGameEntity.GetAttachedNavmeshFaceRecords(Pointer, faceRecords);
	}

	public void GetAttachedNavmeshFaceVertexIndices(in PathFaceRecord faceRecord, int[] indices)
	{
		EngineApplicationInterface.IGameEntity.GetAttachedNavmeshFaceVertexIndices(Pointer, in faceRecord, indices);
	}

	public void SetCustomVertexPositionEnabled(bool customVertexPositionEnabled)
	{
		EngineApplicationInterface.IGameEntity.SetCustomVertexPositionEnabled(Pointer, customVertexPositionEnabled);
	}

	public void SetPositionsForAttachedNavmeshVertices(int[] vertices, int indexCount, Vec3[] positions)
	{
		EngineApplicationInterface.IGameEntity.SetPositionsForAttachedNavmeshVertices(Pointer, vertices, indexCount, positions);
	}

	public void SetCostAdderForAttachedFaces(float costs)
	{
		EngineApplicationInterface.IGameEntity.SetCostAdderForAttachedFaces(Pointer, costs);
	}

	public void SetExternalReferencesUsage(bool value)
	{
		EngineApplicationInterface.IGameEntity.SetExternalReferencesUsage(Pointer, value);
	}

	public void SetMorphFrameOfComponents(float value)
	{
		EngineApplicationInterface.IGameEntity.SetMorphFrameOfComponents(Pointer, value);
	}

	public void AddEditDataUserToAllMeshes(bool entityComponents, bool skeletonComponents)
	{
		EngineApplicationInterface.IGameEntity.AddEditDataUserToAllMeshes(Pointer, entityComponents, skeletonComponents);
	}

	public void ReleaseEditDataUserToAllMeshes(bool entityComponents, bool skeletonComponents)
	{
		EngineApplicationInterface.IGameEntity.ReleaseEditDataUserToAllMeshes(Pointer, entityComponents, skeletonComponents);
	}

	public void GetCameraParamsFromCameraScript(Camera cam, ref Vec3 dofParams)
	{
		EngineApplicationInterface.IGameEntity.GetCameraParamsFromCameraScript(Pointer, cam.Pointer, ref dofParams);
	}

	public void GetMeshBendedFrame(MatrixFrame worldSpacePosition, ref MatrixFrame output)
	{
		EngineApplicationInterface.IGameEntity.GetMeshBendedPosition(Pointer, ref worldSpacePosition, ref output);
	}

	public void ComputeTrajectoryVolume(float missileSpeed, float verticalAngleMaxInDegrees, float verticalAngleMinInDegrees, float horizontalAngleRangeInDegrees, float airFrictionConstant)
	{
		EngineApplicationInterface.IGameEntity.ComputeTrajectoryVolume(Pointer, missileSpeed, verticalAngleMaxInDegrees, verticalAngleMinInDegrees, horizontalAngleRangeInDegrees, airFrictionConstant);
	}

	public void SetAnimTreeChannelParameterForceUpdate(float phase, int channelNo)
	{
		EngineApplicationInterface.IGameEntity.SetAnimTreeChannelParameter(Pointer, phase, channelNo);
	}

	public void ChangeMetaMeshOrRemoveItIfNotExists(MetaMesh entityMetaMesh, MetaMesh newMetaMesh)
	{
		EngineApplicationInterface.IGameEntity.ChangeMetaMeshOrRemoveItIfNotExists(Pointer, (entityMetaMesh != null) ? entityMetaMesh.Pointer : UIntPtr.Zero, (newMetaMesh != null) ? newMetaMesh.Pointer : UIntPtr.Zero);
	}

	public void SetUpdateValidtyOnFrameChangedOfFacesWithId(int faceGroupId, bool updateValidity)
	{
		EngineApplicationInterface.IGameEntity.SetUpdateValidityOnFrameChangedOfFacesWithId(Pointer, faceGroupId, updateValidity);
	}

	public void AttachNavigationMeshFaces(int faceGroupId, bool isConnected, bool isBlocker = false, bool autoLocalize = false, bool finalizeBlockerConvexHullComputation = false, bool updateEntityFrame = true)
	{
		EngineApplicationInterface.IGameEntity.AttachNavigationMeshFaces(Pointer, faceGroupId, isConnected, isBlocker, autoLocalize, finalizeBlockerConvexHullComputation, updateEntityFrame);
	}

	public void DetachAllAttachedNavigationMeshFaces()
	{
		EngineApplicationInterface.IGameEntity.DetachAllAttachedNavigationMeshFaces(Pointer);
	}

	public void UpdateAttachedNavigationMeshFaces()
	{
		EngineApplicationInterface.IGameEntity.UpdateAttachedNavigationMeshFaces(Pointer);
	}

	public void RemoveSkeleton()
	{
		Skeleton = null;
	}

	public void RemoveAllChildren()
	{
		EngineApplicationInterface.IGameEntity.RemoveAllChildren(Pointer);
	}

	public IEnumerable<WeakGameEntity> GetChildren()
	{
		int count = ChildCount;
		for (int i = 0; i < count; i++)
		{
			yield return GetChild(i);
		}
	}

	public IEnumerable<WeakGameEntity> GetEntityAndChildren()
	{
		yield return this;
		int count = ChildCount;
		for (int i = 0; i < count; i++)
		{
			yield return GetChild(i);
		}
	}

	public void GetChildrenRecursive(ref List<WeakGameEntity> children)
	{
		int childCount = ChildCount;
		for (int i = 0; i < childCount; i++)
		{
			WeakGameEntity child = GetChild(i);
			children.Add(child);
			child.GetChildrenRecursive(ref children);
		}
	}

	public void GetChildrenWithTagRecursive(List<WeakGameEntity> children, string tag)
	{
		int childCount = ChildCount;
		for (int i = 0; i < childCount; i++)
		{
			WeakGameEntity child = GetChild(i);
			if (child.HasTag(tag))
			{
				children.Add(child);
			}
			child.GetChildrenWithTagRecursive(children, tag);
		}
	}

	public bool IsSelectedOnEditor()
	{
		return EngineApplicationInterface.IGameEntity.IsEntitySelectedOnEditor(Pointer);
	}

	public void SelectEntityOnEditor()
	{
		EngineApplicationInterface.IGameEntity.SelectEntityOnEditor(Pointer);
	}

	public void DeselectEntityOnEditor()
	{
		EngineApplicationInterface.IGameEntity.DeselectEntityOnEditor(Pointer);
	}

	public void SetAsPredisplayEntity()
	{
		EngineApplicationInterface.IGameEntity.SetAsPredisplayEntity(Pointer);
	}

	public void RemoveFromPredisplayEntity()
	{
		EngineApplicationInterface.IGameEntity.RemoveFromPredisplayEntity(Pointer);
	}

	public void SetNativeScriptComponentVariable(string className, string fieldName, ref ScriptComponentFieldHolder data, RglScriptFieldType variableType)
	{
		EngineApplicationInterface.IGameEntity.SetNativeScriptComponentVariable(Pointer, className, fieldName, ref data, variableType);
	}

	public void SetManualGlobalBoundingBox(Vec3 boundingBoxStartGlobal, Vec3 boundingBoxEndGlobal)
	{
		EngineApplicationInterface.IGameEntity.SetManualGlobalBoundingBox(Pointer, boundingBoxStartGlobal, boundingBoxEndGlobal);
	}

	public bool RayHitEntityWithNormal(Vec3 rayOrigin, Vec3 rayDirection, float maxLength, ref Vec3 resultNormal, ref float resultLength)
	{
		return EngineApplicationInterface.IGameEntity.RayHitEntityWithNormal(Pointer, in rayOrigin, in rayDirection, maxLength, ref resultNormal, ref resultLength);
	}

	public bool RayHitEntity(Vec3 rayOrigin, Vec3 rayDirection, float maxLength, ref float resultLength)
	{
		return EngineApplicationInterface.IGameEntity.RayHitEntity(Pointer, in rayOrigin, in rayDirection, maxLength, ref resultLength);
	}

	public void GetNativeScriptComponentVariable(string className, string fieldName, ref ScriptComponentFieldHolder data, RglScriptFieldType variableType)
	{
		EngineApplicationInterface.IGameEntity.GetNativeScriptComponentVariable(Pointer, className, fieldName, ref data, variableType);
	}

	public void SetCustomClipPlane(Vec3 clipPosition, Vec3 clipNormal, bool setForChildren)
	{
		EngineApplicationInterface.IGameEntity.SetCustomClipPlane(Pointer, clipPosition, clipNormal, setForChildren);
	}

	public float GetBoundingBoxLongestHalfDimension()
	{
		return BoundingBox.GetLongestHalfDimensionOfBoundingBox(GetLocalBoundingBox());
	}

	public BoundingBox ComputeBoundingBoxFromLongestHalfDimension(float longestHalfDimensionCoefficient)
	{
		BoundingBox localBoundingBox = GetLocalBoundingBox();
		BoundingBox result = default(BoundingBox);
		float num = GetBoundingBoxLongestHalfDimension() * longestHalfDimensionCoefficient;
		Vec3 vec = new Vec3(num, num, num);
		result.min = localBoundingBox.center - vec;
		result.max = localBoundingBox.center + vec;
		result.RecomputeRadius();
		return result;
	}

	public BoundingBox ComputeBoundingBoxIncludeChildren()
	{
		BoundingBox result = default(BoundingBox);
		result.BeginRelaxation();
		foreach (WeakGameEntity child in GetChildren())
		{
			child.ValidateBoundingBox();
			BoundingBox localBoundingBox = child.GetLocalBoundingBox();
			result.RelaxWithChildBoundingBox(localBoundingBox, child.GetFrame());
		}
		result.RecomputeRadius();
		return result;
	}

	public void SetManualLocalBoundingBox(in BoundingBox boundingBox)
	{
		EngineApplicationInterface.IGameEntity.SetManualLocalBoundingBox(Pointer, in boundingBox);
	}

	public void RelaxLocalBoundingBox(in BoundingBox boundingBox)
	{
		EngineApplicationInterface.IGameEntity.RelaxLocalBoundingBox(Pointer, in boundingBox);
	}

	public void SetCullMode(MBMeshCullingMode cullMode)
	{
		EngineApplicationInterface.IGameEntity.SetCullMode(Pointer, cullMode);
	}

	public WeakGameEntity GetFirstChildEntityWithTagRecursive(string tag)
	{
		UIntPtr firstChildWithTagRecursive = EngineApplicationInterface.IGameEntity.GetFirstChildWithTagRecursive(Pointer, tag);
		if (firstChildWithTagRecursive != UIntPtr.Zero)
		{
			return new WeakGameEntity(firstChildWithTagRecursive);
		}
		return Invalid;
	}

	public override bool Equals(object obj)
	{
		return ((WeakGameEntity)obj).Pointer == Pointer;
	}

	public override int GetHashCode()
	{
		return Pointer.GetHashCode();
	}

	public static bool operator ==(WeakGameEntity weakGameEntity, GameEntity entity)
	{
		return weakGameEntity.Pointer == (entity?.Pointer ?? UIntPtr.Zero);
	}

	public static bool operator !=(WeakGameEntity weakGameEntity, GameEntity entity)
	{
		return weakGameEntity.Pointer != (entity?.Pointer ?? UIntPtr.Zero);
	}

	public static bool operator ==(WeakGameEntity weakGameEntity1, WeakGameEntity weakGameEntity2)
	{
		return weakGameEntity1.Pointer == weakGameEntity2.Pointer;
	}

	public static bool operator !=(WeakGameEntity weakGameEntity1, WeakGameEntity weakGameEntity2)
	{
		return weakGameEntity1.Pointer != weakGameEntity2.Pointer;
	}

	public List<WeakGameEntity> CollectChildrenEntitiesWithTag(string tag)
	{
		List<WeakGameEntity> list = new List<WeakGameEntity>();
		foreach (WeakGameEntity child in GetChildren())
		{
			if (child.HasTag(tag))
			{
				list.Add(child);
			}
			if (child.ChildCount > 0)
			{
				list.AddRange(child.CollectChildrenEntitiesWithTag(tag));
			}
		}
		return list;
	}

	public IEnumerable<WeakGameEntity> CollectChildrenEntitiesWithTagAsEnumarable(string tag)
	{
		foreach (WeakGameEntity child in GetChildren())
		{
			if (child.HasTag(tag))
			{
				yield return child;
			}
			if (child.ChildCount <= 0)
			{
				continue;
			}
			foreach (WeakGameEntity item in child.CollectChildrenEntitiesWithTagAsEnumarable(tag))
			{
				yield return item;
			}
		}
	}

	public void SetDoNotCheckVisibility(bool value)
	{
		EngineApplicationInterface.IGameEntity.SetDoNotCheckVisibility(Pointer, value);
	}

	public void SetBoneFrameToAllMeshes(int boneIndex, in MatrixFrame frame)
	{
		EngineApplicationInterface.IGameEntity.SetBoneFrameToAllMeshes(Pointer, boneIndex, in frame);
	}

	public Vec2 GetGlobalWindStrengthVectorOfScene()
	{
		return EngineApplicationInterface.IGameEntity.GetGlobalWindStrengthVectorOfScene(Pointer);
	}

	public Vec2 GetGlobalWindVelocityOfScene()
	{
		return EngineApplicationInterface.IGameEntity.GetGlobalWindVelocityOfScene(Pointer);
	}

	public Vec3 GetLastFinalRenderCameraPositionOfScene()
	{
		return EngineApplicationInterface.IGameEntity.GetLastFinalRenderCameraPositionOfScene(Pointer);
	}

	public Vec2 GetGlobalWindVelocityWithGustNoiseOfScene(float globalTime)
	{
		return EngineApplicationInterface.IGameEntity.GetGlobalWindVelocityWithGustNoiseOfScene(Pointer, globalTime);
	}

	public void SetForceDecalsToRender(bool value)
	{
		EngineApplicationInterface.IGameEntity.SetForceDecalsToRender(Pointer, value);
	}

	public UIntPtr CreateEmptyPhysxShape(bool isVariable, int physxMaterialIndex)
	{
		return EngineApplicationInterface.IGameEntity.CreateEmptyPhysxShape(Pointer, isVariable, physxMaterialIndex);
	}

	public void SetForceNotAffectedBySeason(bool value)
	{
		EngineApplicationInterface.IGameEntity.SetForceNotAffectedBySeason(Pointer, value);
	}

	public bool CheckIsPrefabLinkRootPrefab(int depth)
	{
		return EngineApplicationInterface.IGameEntity.CheckIsPrefabLinkRootPrefab(Pointer, depth);
	}
}
