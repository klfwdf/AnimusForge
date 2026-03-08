using System;
using System.Collections.Generic;
using System.Linq;
using TaleWorlds.Library;

namespace TaleWorlds.Engine
{
	// Token: 0x0200009C RID: 156
	public struct WeakGameEntity
	{
		// Token: 0x170000A1 RID: 161
		// (get) Token: 0x06000DCF RID: 3535 RVA: 0x0000F774 File Offset: 0x0000D974
		// (set) Token: 0x06000DD0 RID: 3536 RVA: 0x0000F77C File Offset: 0x0000D97C
		public UIntPtr Pointer { get; private set; }

		// Token: 0x170000A2 RID: 162
		// (get) Token: 0x06000DD1 RID: 3537 RVA: 0x0000F785 File Offset: 0x0000D985
		public bool IsValid
		{
			get
			{
				return this.Pointer != UIntPtr.Zero;
			}
		}

		// Token: 0x170000A3 RID: 163
		// (get) Token: 0x06000DD2 RID: 3538 RVA: 0x0000F797 File Offset: 0x0000D997
		public string Name
		{
			get
			{
				return EngineApplicationInterface.IGameEntity.GetName(this.Pointer);
			}
		}

		// Token: 0x170000A4 RID: 164
		// (get) Token: 0x06000DD3 RID: 3539 RVA: 0x0000F7A9 File Offset: 0x0000D9A9
		public Scene Scene
		{
			get
			{
				return EngineApplicationInterface.IGameEntity.GetScene(this.Pointer);
			}
		}

		// Token: 0x170000A5 RID: 165
		// (get) Token: 0x06000DD4 RID: 3540 RVA: 0x0000F7BB File Offset: 0x0000D9BB
		public EntityFlags EntityFlags
		{
			get
			{
				return EngineApplicationInterface.IGameEntity.GetEntityFlags(this.Pointer);
			}
		}

		// Token: 0x170000A6 RID: 166
		// (get) Token: 0x06000DD5 RID: 3541 RVA: 0x0000F7CD File Offset: 0x0000D9CD
		public EntityVisibilityFlags EntityVisibilityFlags
		{
			get
			{
				return EngineApplicationInterface.IGameEntity.GetEntityVisibilityFlags(this.Pointer);
			}
		}

		// Token: 0x170000A7 RID: 167
		// (get) Token: 0x06000DD6 RID: 3542 RVA: 0x0000F7DF File Offset: 0x0000D9DF
		public BodyFlags BodyFlag
		{
			get
			{
				return (BodyFlags)EngineApplicationInterface.IGameEntity.GetBodyFlags(this.Pointer);
			}
		}

		// Token: 0x170000A8 RID: 168
		// (get) Token: 0x06000DD7 RID: 3543 RVA: 0x0000F7F1 File Offset: 0x0000D9F1
		public BodyFlags PhysicsDescBodyFlag
		{
			get
			{
				return (BodyFlags)EngineApplicationInterface.IGameEntity.GetPhysicsDescBodyFlags(this.Pointer);
			}
		}

		// Token: 0x170000A9 RID: 169
		// (get) Token: 0x06000DD8 RID: 3544 RVA: 0x0000F803 File Offset: 0x0000DA03
		public float Mass
		{
			get
			{
				return EngineApplicationInterface.IGameEntity.GetMass(this.Pointer);
			}
		}

		// Token: 0x170000AA RID: 170
		// (get) Token: 0x06000DD9 RID: 3545 RVA: 0x0000F815 File Offset: 0x0000DA15
		public Vec3 CenterOfMass
		{
			get
			{
				return EngineApplicationInterface.IGameEntity.GetCenterOfMass(this.Pointer);
			}
		}

		// Token: 0x06000DDA RID: 3546 RVA: 0x0000F827 File Offset: 0x0000DA27
		internal WeakGameEntity(UIntPtr pointer)
		{
			this.Pointer = pointer;
		}

		// Token: 0x06000DDB RID: 3547 RVA: 0x0000F830 File Offset: 0x0000DA30
		public void Invalidate()
		{
			this.Pointer = (UIntPtr)0UL;
		}

		// Token: 0x06000DDC RID: 3548 RVA: 0x0000F83F File Offset: 0x0000DA3F
		public UIntPtr GetScenePointer()
		{
			return EngineApplicationInterface.IGameEntity.GetScenePointer(this.Pointer);
		}

		// Token: 0x06000DDD RID: 3549 RVA: 0x0000F854 File Offset: 0x0000DA54
		public override string ToString()
		{
			return this.Pointer.ToString();
		}

		// Token: 0x06000DDE RID: 3550 RVA: 0x0000F86F File Offset: 0x0000DA6F
		public void ClearEntityComponents(bool resetAll, bool removeScripts, bool deleteChildEntities)
		{
			EngineApplicationInterface.IGameEntity.ClearEntityComponents(this.Pointer, resetAll, removeScripts, deleteChildEntities);
		}

		// Token: 0x06000DDF RID: 3551 RVA: 0x0000F884 File Offset: 0x0000DA84
		public void ClearComponents()
		{
			EngineApplicationInterface.IGameEntity.ClearComponents(this.Pointer);
		}

		// Token: 0x06000DE0 RID: 3552 RVA: 0x0000F896 File Offset: 0x0000DA96
		public void ClearOnlyOwnComponents()
		{
			EngineApplicationInterface.IGameEntity.ClearOnlyOwnComponents(this.Pointer);
		}

		// Token: 0x06000DE1 RID: 3553 RVA: 0x0000F8A8 File Offset: 0x0000DAA8
		public bool CheckResources(bool addToQueue, bool checkFaceResources)
		{
			return EngineApplicationInterface.IGameEntity.CheckResources(this.Pointer, addToQueue, checkFaceResources);
		}

		// Token: 0x06000DE2 RID: 3554 RVA: 0x0000F8BC File Offset: 0x0000DABC
		public void SetMobility(GameEntity.Mobility mobility)
		{
			EngineApplicationInterface.IGameEntity.SetMobility(this.Pointer, mobility);
		}

		// Token: 0x06000DE3 RID: 3555 RVA: 0x0000F8CF File Offset: 0x0000DACF
		public GameEntity.Mobility GetMobility()
		{
			return EngineApplicationInterface.IGameEntity.GetMobility(this.Pointer);
		}

		// Token: 0x06000DE4 RID: 3556 RVA: 0x0000F8E1 File Offset: 0x0000DAE1
		public void AddMesh(Mesh mesh, bool recomputeBoundingBox = true)
		{
			EngineApplicationInterface.IGameEntity.AddMesh(this.Pointer, mesh.Pointer, recomputeBoundingBox);
		}

		// Token: 0x06000DE5 RID: 3557 RVA: 0x0000F8FA File Offset: 0x0000DAFA
		public void AddMultiMeshToSkeleton(MetaMesh metaMesh)
		{
			EngineApplicationInterface.IGameEntity.AddMultiMeshToSkeleton(this.Pointer, metaMesh.Pointer);
		}

		// Token: 0x06000DE6 RID: 3558 RVA: 0x0000F912 File Offset: 0x0000DB12
		public void AddMultiMeshToSkeletonBone(MetaMesh metaMesh, sbyte boneIndex)
		{
			EngineApplicationInterface.IGameEntity.AddMultiMeshToSkeletonBone(this.Pointer, metaMesh.Pointer, boneIndex);
		}

		// Token: 0x06000DE7 RID: 3559 RVA: 0x0000F92B File Offset: 0x0000DB2B
		public void SetColorToAllMeshesWithTagRecursive(uint color, string tag)
		{
			EngineApplicationInterface.IGameEntity.SetColorToAllMeshesWithTagRecursive(this.Pointer, color, tag);
		}

		// Token: 0x06000DE8 RID: 3560 RVA: 0x0000F93F File Offset: 0x0000DB3F
		public IEnumerable<Mesh> GetAllMeshesWithTag(string tag)
		{
			List<WeakGameEntity> list = new List<WeakGameEntity>();
			this.GetChildrenRecursive(ref list);
			list.Add(ref this);
			foreach (WeakGameEntity entity in list)
			{
				int num;
				for (int i = 0; i < entity.MultiMeshComponentCount; i = num + 1)
				{
					MetaMesh multiMesh = entity.GetMetaMesh(i);
					for (int j = 0; j < multiMesh.MeshCount; j = num + 1)
					{
						Mesh meshAtIndex = multiMesh.GetMeshAtIndex(j);
						if (meshAtIndex.HasTag(tag))
						{
							yield return meshAtIndex;
						}
						num = j;
					}
					multiMesh = null;
					num = i;
				}
				for (int i = 0; i < entity.ClothSimulatorComponentCount; i = num + 1)
				{
					ClothSimulatorComponent clothSimulator = entity.GetClothSimulator(i);
					MetaMesh multiMesh = clothSimulator.GetFirstMetaMesh();
					for (int j = 0; j < multiMesh.MeshCount; j = num + 1)
					{
						Mesh meshAtIndex2 = multiMesh.GetMeshAtIndex(j);
						if (meshAtIndex2.HasTag(tag))
						{
							yield return meshAtIndex2;
						}
						num = j;
					}
					multiMesh = null;
					num = i;
				}
			}
			List<WeakGameEntity>.Enumerator enumerator = default(List<WeakGameEntity>.Enumerator);
			yield break;
			yield break;
		}

		// Token: 0x06000DE9 RID: 3561 RVA: 0x0000F95B File Offset: 0x0000DB5B
		public void SetName(string name)
		{
			EngineApplicationInterface.IGameEntity.SetName(this.Pointer, name);
		}

		// Token: 0x06000DEA RID: 3562 RVA: 0x0000F96E File Offset: 0x0000DB6E
		public void SetEntityFlags(EntityFlags flags)
		{
			EngineApplicationInterface.IGameEntity.SetEntityFlags(this.Pointer, flags);
		}

		// Token: 0x06000DEB RID: 3563 RVA: 0x0000F981 File Offset: 0x0000DB81
		public void SetEntityVisibilityFlags(EntityVisibilityFlags flags)
		{
			EngineApplicationInterface.IGameEntity.SetEntityVisibilityFlags(this.Pointer, flags);
		}

		// Token: 0x06000DEC RID: 3564 RVA: 0x0000F994 File Offset: 0x0000DB94
		public PhysicsMaterial GetPhysicsMaterial()
		{
			return PhysicsMaterial.GetFromIndex(EngineApplicationInterface.IGameEntity.GetPhysicsMaterialIndex(this.Pointer));
		}

		// Token: 0x06000DED RID: 3565 RVA: 0x0000F9AB File Offset: 0x0000DBAB
		public void SetBodyFlags(BodyFlags flags)
		{
			EngineApplicationInterface.IGameEntity.SetBodyFlags(this.Pointer, (uint)flags);
		}

		// Token: 0x06000DEE RID: 3566 RVA: 0x0000F9BE File Offset: 0x0000DBBE
		public void SetBodyFlagsRecursive(BodyFlags bodyFlags)
		{
			EngineApplicationInterface.IGameEntity.SetBodyFlagsRecursive(this.Pointer, (uint)bodyFlags);
		}

		// Token: 0x06000DEF RID: 3567 RVA: 0x0000F9D4 File Offset: 0x0000DBD4
		public void AddBodyFlags(BodyFlags bodyFlags, bool applyToChildren = true)
		{
			this.SetBodyFlags(this.BodyFlag | bodyFlags);
			if (applyToChildren)
			{
				foreach (WeakGameEntity weakGameEntity in this.GetChildren())
				{
					weakGameEntity.AddBodyFlags(bodyFlags, true);
				}
			}
		}

		// Token: 0x06000DF0 RID: 3568 RVA: 0x0000FA34 File Offset: 0x0000DC34
		internal static WeakGameEntity GetFirstEntityWithTag(Scene scene, string tag)
		{
			return new WeakGameEntity(EngineApplicationInterface.IGameEntity.GetFirstEntityWithTag(scene.Pointer, tag));
		}

		// Token: 0x06000DF1 RID: 3569 RVA: 0x0000FA4C File Offset: 0x0000DC4C
		internal static WeakGameEntity GetNextEntityWithTag(Scene scene, WeakGameEntity startEntity, string tag)
		{
			if (!(startEntity == null))
			{
				return new WeakGameEntity(EngineApplicationInterface.IGameEntity.GetNextEntityWithTag(startEntity.Pointer, tag));
			}
			return WeakGameEntity.GetFirstEntityWithTag(scene, tag);
		}

		// Token: 0x06000DF2 RID: 3570 RVA: 0x0000FA76 File Offset: 0x0000DC76
		internal static WeakGameEntity GetFirstEntityWithTagExpression(Scene scene, string tagExpression)
		{
			return new WeakGameEntity(EngineApplicationInterface.IGameEntity.GetFirstEntityWithTagExpression(scene.Pointer, tagExpression));
		}

		// Token: 0x06000DF3 RID: 3571 RVA: 0x0000FA90 File Offset: 0x0000DC90
		internal static WeakGameEntity GetNextEntityWithTagExpression(Scene scene, WeakGameEntity startEntity, string tagExpression)
		{
			if (startEntity == null)
			{
				return WeakGameEntity.GetFirstEntityWithTagExpression(scene, tagExpression);
			}
			UIntPtr nextEntityWithTagExpression = EngineApplicationInterface.IGameEntity.GetNextEntityWithTagExpression(startEntity.Pointer, tagExpression);
			if (nextEntityWithTagExpression != UIntPtr.Zero)
			{
				return new WeakGameEntity(nextEntityWithTagExpression);
			}
			return WeakGameEntity.Invalid;
		}

		// Token: 0x06000DF4 RID: 3572 RVA: 0x0000FADA File Offset: 0x0000DCDA
		internal static IEnumerable<WeakGameEntity> GetEntitiesWithTag(Scene scene, string tag)
		{
			WeakGameEntity entity = WeakGameEntity.GetFirstEntityWithTag(scene, tag);
			while (entity != null)
			{
				yield return entity;
				entity = WeakGameEntity.GetNextEntityWithTag(scene, entity, tag);
			}
			yield break;
		}

		// Token: 0x06000DF5 RID: 3573 RVA: 0x0000FAF1 File Offset: 0x0000DCF1
		internal static IEnumerable<WeakGameEntity> GetEntitiesWithTagExpression(Scene scene, string tagExpression)
		{
			WeakGameEntity entity = WeakGameEntity.GetFirstEntityWithTagExpression(scene, tagExpression);
			while (entity != null)
			{
				yield return entity;
				entity = WeakGameEntity.GetNextEntityWithTagExpression(scene, entity, tagExpression);
			}
			yield break;
		}

		// Token: 0x06000DF6 RID: 3574 RVA: 0x0000FB08 File Offset: 0x0000DD08
		public void RemoveBodyFlags(BodyFlags bodyFlags, bool applyToChildren = true)
		{
			this.SetBodyFlags(this.BodyFlag & ~bodyFlags);
			if (applyToChildren)
			{
				foreach (WeakGameEntity weakGameEntity in this.GetChildren())
				{
					weakGameEntity.RemoveBodyFlags(bodyFlags, true);
				}
			}
		}

		// Token: 0x06000DF7 RID: 3575 RVA: 0x0000FB6C File Offset: 0x0000DD6C
		public void SetLocalPosition(Vec3 position)
		{
			EngineApplicationInterface.IGameEntity.SetLocalPosition(this.Pointer, position);
		}

		// Token: 0x06000DF8 RID: 3576 RVA: 0x0000FB7F File Offset: 0x0000DD7F
		public void SetGlobalPosition(Vec3 position)
		{
			EngineApplicationInterface.IGameEntity.SetGlobalPosition(this.Pointer, position);
		}

		// Token: 0x06000DF9 RID: 3577 RVA: 0x0000FB94 File Offset: 0x0000DD94
		public void SetColor(uint color1, uint color2, string meshTag)
		{
			foreach (Mesh mesh in this.GetAllMeshesWithTag(meshTag))
			{
				mesh.Color = color1;
				mesh.Color2 = color2;
			}
		}

		// Token: 0x06000DFA RID: 3578 RVA: 0x0000FBE8 File Offset: 0x0000DDE8
		public uint GetFactorColor()
		{
			return EngineApplicationInterface.IGameEntity.GetFactorColor(this.Pointer);
		}

		// Token: 0x06000DFB RID: 3579 RVA: 0x0000FBFA File Offset: 0x0000DDFA
		public void SetFactorColor(uint color)
		{
			EngineApplicationInterface.IGameEntity.SetFactorColor(this.Pointer, color);
		}

		// Token: 0x06000DFC RID: 3580 RVA: 0x0000FC0D File Offset: 0x0000DE0D
		public void SetAsReplayEntity()
		{
			EngineApplicationInterface.IGameEntity.SetAsReplayEntity(this.Pointer);
		}

		// Token: 0x06000DFD RID: 3581 RVA: 0x0000FC1F File Offset: 0x0000DE1F
		public void SetClothMaxDistanceMultiplier(float multiplier)
		{
			EngineApplicationInterface.IGameEntity.SetClothMaxDistanceMultiplier(this.Pointer, multiplier);
		}

		// Token: 0x06000DFE RID: 3582 RVA: 0x0000FC32 File Offset: 0x0000DE32
		public void RemoveMultiMeshFromSkeleton(MetaMesh metaMesh)
		{
			EngineApplicationInterface.IGameEntity.RemoveMultiMeshFromSkeleton(this.Pointer, metaMesh.Pointer);
		}

		// Token: 0x06000DFF RID: 3583 RVA: 0x0000FC4A File Offset: 0x0000DE4A
		public void RemoveMultiMeshFromSkeletonBone(MetaMesh metaMesh, sbyte boneIndex)
		{
			EngineApplicationInterface.IGameEntity.RemoveMultiMeshFromSkeletonBone(this.Pointer, metaMesh.Pointer, boneIndex);
		}

		// Token: 0x06000E00 RID: 3584 RVA: 0x0000FC63 File Offset: 0x0000DE63
		public bool RemoveComponentWithMesh(Mesh mesh)
		{
			return EngineApplicationInterface.IGameEntity.RemoveComponentWithMesh(this.Pointer, mesh.Pointer);
		}

		// Token: 0x06000E01 RID: 3585 RVA: 0x0000FC7B File Offset: 0x0000DE7B
		public void AddComponent(GameEntityComponent component)
		{
			EngineApplicationInterface.IGameEntity.AddComponent(this.Pointer, component.Pointer);
		}

		// Token: 0x06000E02 RID: 3586 RVA: 0x0000FC93 File Offset: 0x0000DE93
		public bool HasComponent(GameEntityComponent component)
		{
			return EngineApplicationInterface.IGameEntity.HasComponent(this.Pointer, component.Pointer);
		}

		// Token: 0x06000E03 RID: 3587 RVA: 0x0000FCAB File Offset: 0x0000DEAB
		public bool IsInEditorScene()
		{
			return false;
		}

		// Token: 0x06000E04 RID: 3588 RVA: 0x0000FCAE File Offset: 0x0000DEAE
		public bool RemoveComponent(GameEntityComponent component)
		{
			return EngineApplicationInterface.IGameEntity.RemoveComponent(this.Pointer, component.Pointer);
		}

		// Token: 0x06000E05 RID: 3589 RVA: 0x0000FCC6 File Offset: 0x0000DEC6
		public string GetGuid()
		{
			return EngineApplicationInterface.IGameEntity.GetGuid(this.Pointer);
		}

		// Token: 0x06000E06 RID: 3590 RVA: 0x0000FCD8 File Offset: 0x0000DED8
		public bool IsGuidValid()
		{
			return EngineApplicationInterface.IGameEntity.IsGuidValid(this.Pointer);
		}

		// Token: 0x06000E07 RID: 3591 RVA: 0x0000FCEA File Offset: 0x0000DEEA
		public void SetEnforcedMaximumLodLevel(int lodLevel)
		{
			EngineApplicationInterface.IGameEntity.SetEnforcedMaximumLodLevel(this.Pointer, lodLevel);
		}

		// Token: 0x06000E08 RID: 3592 RVA: 0x0000FCFD File Offset: 0x0000DEFD
		public float GetLodLevelForDistanceSq(float distSq)
		{
			return EngineApplicationInterface.IGameEntity.GetLodLevelForDistanceSq(this.Pointer, distSq);
		}

		// Token: 0x06000E09 RID: 3593 RVA: 0x0000FD10 File Offset: 0x0000DF10
		public void GetQuickBoneEntitialFrame(sbyte index, out MatrixFrame frame)
		{
			EngineApplicationInterface.IGameEntity.GetQuickBoneEntitialFrame(this.Pointer, index, out frame);
		}

		// Token: 0x06000E0A RID: 3594 RVA: 0x0000FD24 File Offset: 0x0000DF24
		public void UpdateVisibilityMask()
		{
			EngineApplicationInterface.IGameEntity.UpdateVisibilityMask(this.Pointer);
		}

		// Token: 0x06000E0B RID: 3595 RVA: 0x0000FD36 File Offset: 0x0000DF36
		public void CallScriptCallbacks(bool registerScriptComponents)
		{
			EngineApplicationInterface.IGameEntity.CallScriptCallbacks(this.Pointer, registerScriptComponents);
		}

		// Token: 0x170000AB RID: 171
		// (get) Token: 0x06000E0C RID: 3596 RVA: 0x0000FD49 File Offset: 0x0000DF49
		private int ScriptCount
		{
			get
			{
				return EngineApplicationInterface.IGameEntity.GetScriptComponentCount(this.Pointer);
			}
		}

		// Token: 0x06000E0D RID: 3597 RVA: 0x0000FD5B File Offset: 0x0000DF5B
		public bool IsGhostObject()
		{
			return EngineApplicationInterface.IGameEntity.IsGhostObject(this.Pointer);
		}

		// Token: 0x06000E0E RID: 3598 RVA: 0x0000FD6D File Offset: 0x0000DF6D
		public void CreateAndAddScriptComponent(string name, bool callScriptCallbacks)
		{
			EngineApplicationInterface.IGameEntity.CreateAndAddScriptComponent(this.Pointer, name, callScriptCallbacks);
		}

		// Token: 0x06000E0F RID: 3599 RVA: 0x0000FD81 File Offset: 0x0000DF81
		public void RemoveScriptComponent(UIntPtr scriptComponent, int removeReason)
		{
			EngineApplicationInterface.IGameEntity.RemoveScriptComponent(this.Pointer, scriptComponent, removeReason);
		}

		// Token: 0x06000E10 RID: 3600 RVA: 0x0000FD95 File Offset: 0x0000DF95
		public void SetEntityEnvMapVisibility(bool value)
		{
			EngineApplicationInterface.IGameEntity.SetEntityEnvMapVisibility(this.Pointer, value);
		}

		// Token: 0x06000E11 RID: 3601 RVA: 0x0000FDA8 File Offset: 0x0000DFA8
		internal ScriptComponentBehavior GetScriptAtIndex(int index)
		{
			return EngineApplicationInterface.IGameEntity.GetScriptComponentAtIndex(this.Pointer, index);
		}

		// Token: 0x06000E12 RID: 3602 RVA: 0x0000FDBB File Offset: 0x0000DFBB
		internal int GetScriptComponentIndex(uint nameHash)
		{
			return EngineApplicationInterface.IGameEntity.GetScriptComponentIndex(this.Pointer, nameHash);
		}

		// Token: 0x06000E13 RID: 3603 RVA: 0x0000FDCE File Offset: 0x0000DFCE
		public bool HasScene()
		{
			return EngineApplicationInterface.IGameEntity.HasScene(this.Pointer);
		}

		// Token: 0x06000E14 RID: 3604 RVA: 0x0000FDE0 File Offset: 0x0000DFE0
		public bool HasScriptComponent(string scName)
		{
			return EngineApplicationInterface.IGameEntity.HasScriptComponent(this.Pointer, scName);
		}

		// Token: 0x06000E15 RID: 3605 RVA: 0x0000FDF3 File Offset: 0x0000DFF3
		public bool HasScriptComponent(uint scNameHash)
		{
			return EngineApplicationInterface.IGameEntity.HasScriptComponentHash(this.Pointer, scNameHash);
		}

		// Token: 0x06000E16 RID: 3606 RVA: 0x0000FE06 File Offset: 0x0000E006
		public IEnumerable<ScriptComponentBehavior> GetScriptComponents()
		{
			int count = this.ScriptCount;
			int num;
			for (int i = 0; i < count; i = num + 1)
			{
				yield return this.GetScriptAtIndex(i);
				num = i;
			}
			yield break;
		}

		// Token: 0x06000E17 RID: 3607 RVA: 0x0000FE1B File Offset: 0x0000E01B
		public IEnumerable<T> GetScriptComponents<T>() where T : ScriptComponentBehavior
		{
			int count = this.ScriptCount;
			int num;
			for (int i = 0; i < count; i = num + 1)
			{
				T t;
				if ((t = this.GetScriptAtIndex(i) as T) != null)
				{
					yield return t;
				}
				num = i;
			}
			yield break;
		}

		// Token: 0x06000E18 RID: 3608 RVA: 0x0000FE30 File Offset: 0x0000E030
		public bool HasScriptOfType<T>() where T : ScriptComponentBehavior
		{
			int scriptCount = this.ScriptCount;
			for (int i = 0; i < scriptCount; i++)
			{
				if (this.GetScriptAtIndex(i) is T)
				{
					return true;
				}
			}
			return false;
		}

		// Token: 0x06000E19 RID: 3609 RVA: 0x0000FE64 File Offset: 0x0000E064
		public bool HasScriptOfType(Type t)
		{
			return this.GetScriptComponents().Any((ScriptComponentBehavior sc) => sc.GetType().IsAssignableFrom(t));
		}

		// Token: 0x06000E1A RID: 3610 RVA: 0x0000FE98 File Offset: 0x0000E098
		public T GetFirstScriptOfTypeInFamily<T>() where T : ScriptComponentBehavior
		{
			T firstScriptOfType = this.GetFirstScriptOfType<T>();
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

		// Token: 0x06000E1B RID: 3611 RVA: 0x0000FEDC File Offset: 0x0000E0DC
		public ScriptComponentBehavior GetFirstScriptWithNameHash(uint nameHash)
		{
			int scriptComponentIndex = this.GetScriptComponentIndex(nameHash);
			if (scriptComponentIndex != -1)
			{
				return this.GetScriptAtIndex(scriptComponentIndex);
			}
			return null;
		}

		// Token: 0x06000E1C RID: 3612 RVA: 0x0000FF00 File Offset: 0x0000E100
		public T GetFirstScriptOfType<T>() where T : ScriptComponentBehavior
		{
			int scriptCount = this.ScriptCount;
			for (int i = 0; i < scriptCount; i++)
			{
				T result;
				if ((result = this.GetScriptAtIndex(i) as T) != null)
				{
					return result;
				}
			}
			return default(T);
		}

		// Token: 0x06000E1D RID: 3613 RVA: 0x0000FF48 File Offset: 0x0000E148
		public T GetFirstScriptOfTypeRecursive<T>() where T : ScriptComponentBehavior
		{
			int num = this.ScriptCount;
			for (int i = 0; i < num; i++)
			{
				T result;
				if ((result = this.GetScriptAtIndex(i) as T) != null)
				{
					return result;
				}
			}
			num = this.ChildCount;
			for (int j = 0; j < num; j++)
			{
				T firstScriptOfTypeRecursive = this.GetChild(j).GetFirstScriptOfTypeRecursive<T>();
				if (firstScriptOfTypeRecursive != null)
				{
					return firstScriptOfTypeRecursive;
				}
			}
			return default(T);
		}

		// Token: 0x06000E1E RID: 3614 RVA: 0x0000FFC0 File Offset: 0x0000E1C0
		public WeakGameEntity GetFirstChildEntityWithTag(string tag)
		{
			foreach (WeakGameEntity result in this.GetChildren())
			{
				if (result.HasTag(tag))
				{
					return result;
				}
			}
			return WeakGameEntity.Invalid;
		}

		// Token: 0x06000E1F RID: 3615 RVA: 0x0001001C File Offset: 0x0000E21C
		public int GetScriptCountOfTypeRecursive<T>() where T : ScriptComponentBehavior
		{
			int num = this.ScriptCount;
			int num2 = 0;
			for (int i = 0; i < num; i++)
			{
				if (this.GetScriptAtIndex(i) is T)
				{
					num2++;
				}
			}
			num = this.ChildCount;
			for (int j = 0; j < num; j++)
			{
				num2 += this.GetChild(j).GetScriptCountOfTypeRecursive<T>();
			}
			return num2;
		}

		// Token: 0x06000E20 RID: 3616 RVA: 0x00010077 File Offset: 0x0000E277
		internal static GameEntity GetFirstEntityWithName(Scene scene, string entityName)
		{
			return EngineApplicationInterface.IGameEntity.FindWithName(scene.Pointer, entityName);
		}

		// Token: 0x06000E21 RID: 3617 RVA: 0x0001008A File Offset: 0x0000E28A
		public void SetAlpha(float alpha)
		{
			EngineApplicationInterface.IGameEntity.SetAlpha(this.Pointer, alpha);
		}

		// Token: 0x06000E22 RID: 3618 RVA: 0x0001009D File Offset: 0x0000E29D
		public void SetVisibilityExcludeParents(bool visible)
		{
			EngineApplicationInterface.IGameEntity.SetVisibilityExcludeParents(this.Pointer, visible);
		}

		// Token: 0x06000E23 RID: 3619 RVA: 0x000100B0 File Offset: 0x0000E2B0
		public void SetReadyToRender(bool ready)
		{
			EngineApplicationInterface.IGameEntity.SetReadyToRender(this.Pointer, ready);
		}

		// Token: 0x06000E24 RID: 3620 RVA: 0x000100C3 File Offset: 0x0000E2C3
		public bool GetVisibilityExcludeParents()
		{
			return EngineApplicationInterface.IGameEntity.GetVisibilityExcludeParents(this.Pointer);
		}

		// Token: 0x06000E25 RID: 3621 RVA: 0x000100D5 File Offset: 0x0000E2D5
		public bool IsVisibleIncludeParents()
		{
			return EngineApplicationInterface.IGameEntity.IsVisibleIncludeParents(this.Pointer);
		}

		// Token: 0x06000E26 RID: 3622 RVA: 0x000100E7 File Offset: 0x0000E2E7
		public uint GetVisibilityLevelMaskIncludingParents()
		{
			return EngineApplicationInterface.IGameEntity.GetVisibilityLevelMaskIncludingParents(this.Pointer);
		}

		// Token: 0x06000E27 RID: 3623 RVA: 0x000100F9 File Offset: 0x0000E2F9
		public bool GetEditModeLevelVisibility()
		{
			return EngineApplicationInterface.IGameEntity.GetEditModeLevelVisibility(this.Pointer);
		}

		// Token: 0x06000E28 RID: 3624 RVA: 0x0001010B File Offset: 0x0000E30B
		public void Remove(int removeReason)
		{
			EngineApplicationInterface.IGameEntity.Remove(this.Pointer, removeReason);
		}

		// Token: 0x06000E29 RID: 3625 RVA: 0x0001011E File Offset: 0x0000E31E
		public void SetUpgradeLevelMask(GameEntity.UpgradeLevelMask mask)
		{
			EngineApplicationInterface.IGameEntity.SetUpgradeLevelMask(this.Pointer, (uint)mask);
		}

		// Token: 0x06000E2A RID: 3626 RVA: 0x00010131 File Offset: 0x0000E331
		public GameEntity.UpgradeLevelMask GetUpgradeLevelMask()
		{
			return (GameEntity.UpgradeLevelMask)EngineApplicationInterface.IGameEntity.GetUpgradeLevelMask(this.Pointer);
		}

		// Token: 0x06000E2B RID: 3627 RVA: 0x00010143 File Offset: 0x0000E343
		public GameEntity.UpgradeLevelMask GetUpgradeLevelMaskCumulative()
		{
			return (GameEntity.UpgradeLevelMask)EngineApplicationInterface.IGameEntity.GetUpgradeLevelMaskCumulative(this.Pointer);
		}

		// Token: 0x06000E2C RID: 3628 RVA: 0x00010158 File Offset: 0x0000E358
		public int GetUpgradeLevelOfEntity()
		{
			int upgradeLevelMask = (int)this.GetUpgradeLevelMask();
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

		// Token: 0x06000E2D RID: 3629 RVA: 0x0001018D File Offset: 0x0000E38D
		public string GetOldPrefabName()
		{
			return EngineApplicationInterface.IGameEntity.GetOldPrefabName(this.Pointer);
		}

		// Token: 0x06000E2E RID: 3630 RVA: 0x0001019F File Offset: 0x0000E39F
		public string GetPrefabName()
		{
			return EngineApplicationInterface.IGameEntity.GetPrefabName(this.Pointer);
		}

		// Token: 0x06000E2F RID: 3631 RVA: 0x000101B1 File Offset: 0x0000E3B1
		public void RefreshMeshesToRenderToHullWater(UIntPtr visualRecord, string entityTag)
		{
			EngineApplicationInterface.IGameEntity.RefreshMeshesToRenderToHullWater(this.Pointer, visualRecord, entityTag);
		}

		// Token: 0x06000E30 RID: 3632 RVA: 0x000101C5 File Offset: 0x0000E3C5
		public void DeRegisterWaterMeshMaterials(UIntPtr visualRecord)
		{
			EngineApplicationInterface.IGameEntity.DeRegisterWaterMeshMaterials(this.Pointer, visualRecord);
		}

		// Token: 0x06000E31 RID: 3633 RVA: 0x000101D8 File Offset: 0x0000E3D8
		public void SetVisualRecordWakeParams(UIntPtr visualRecord, Vec3 wakeParams)
		{
			EngineApplicationInterface.IGameEntity.SetVisualRecordWakeParams(visualRecord, wakeParams);
		}

		// Token: 0x06000E32 RID: 3634 RVA: 0x000101E7 File Offset: 0x0000E3E7
		public void ChangeResolutionMultiplierOfWaterVisual(UIntPtr visualRecord, float multiplier, in Vec3 waterEffectsBB)
		{
			EngineApplicationInterface.IGameEntity.ChangeResolutionMultiplierOfWaterVisual(visualRecord, multiplier, waterEffectsBB);
		}

		// Token: 0x06000E33 RID: 3635 RVA: 0x000101F6 File Offset: 0x0000E3F6
		public void ResetHullWater(UIntPtr visualRecord)
		{
			EngineApplicationInterface.IGameEntity.ResetHullWater(visualRecord);
		}

		// Token: 0x06000E34 RID: 3636 RVA: 0x00010203 File Offset: 0x0000E403
		public void SetWaterVisualRecordFrameAndDt(UIntPtr visualRecord, MatrixFrame frame, float dt)
		{
			EngineApplicationInterface.IGameEntity.SetWaterVisualRecordFrameAndDt(this.Pointer, visualRecord, frame, dt);
		}

		// Token: 0x06000E35 RID: 3637 RVA: 0x00010219 File Offset: 0x0000E419
		public void AddSplashPositionToWaterVisualRecord(UIntPtr visualRecord, Vec3 position)
		{
			EngineApplicationInterface.IGameEntity.AddSplashPositionToWaterVisualRecord(this.Pointer, visualRecord, position);
		}

		// Token: 0x06000E36 RID: 3638 RVA: 0x0001022E File Offset: 0x0000E42E
		public void UpdateHullWaterEffectFrames(UIntPtr visualRecord)
		{
			EngineApplicationInterface.IGameEntity.UpdateHullWaterEffectFrames(this.Pointer, visualRecord);
		}

		// Token: 0x06000E37 RID: 3639 RVA: 0x00010241 File Offset: 0x0000E441
		public void CopyScriptComponentFromAnotherEntity(GameEntity otherEntity, string scriptName)
		{
			EngineApplicationInterface.IGameEntity.CopyScriptComponentFromAnotherEntity(this.Pointer, otherEntity.Pointer, scriptName);
		}

		// Token: 0x06000E38 RID: 3640 RVA: 0x0001025A File Offset: 0x0000E45A
		public void SetFrame(ref MatrixFrame frame, bool isTeleportation = true)
		{
			EngineApplicationInterface.IGameEntity.SetLocalFrame(this.Pointer, ref frame, isTeleportation);
		}

		// Token: 0x06000E39 RID: 3641 RVA: 0x0001026E File Offset: 0x0000E46E
		public void SetLocalFrame(ref MatrixFrame frame, bool isTeleportation)
		{
			EngineApplicationInterface.IGameEntity.SetLocalFrame(this.Pointer, ref frame, isTeleportation);
		}

		// Token: 0x06000E3A RID: 3642 RVA: 0x00010282 File Offset: 0x0000E482
		public void SetClothComponentKeepState(MetaMesh metaMesh, bool state)
		{
			EngineApplicationInterface.IGameEntity.SetClothComponentKeepState(this.Pointer, metaMesh.Pointer, state);
		}

		// Token: 0x06000E3B RID: 3643 RVA: 0x0001029B File Offset: 0x0000E49B
		public void SetClothComponentKeepStateOfAllMeshes(bool state)
		{
			EngineApplicationInterface.IGameEntity.SetClothComponentKeepStateOfAllMeshes(this.Pointer, state);
		}

		// Token: 0x06000E3C RID: 3644 RVA: 0x000102AE File Offset: 0x0000E4AE
		public void SetPreviousFrameInvalid()
		{
			EngineApplicationInterface.IGameEntity.SetPreviousFrameInvalid(this.Pointer);
		}

		// Token: 0x06000E3D RID: 3645 RVA: 0x000102C0 File Offset: 0x0000E4C0
		public MatrixFrame GetFrame()
		{
			MatrixFrame result;
			EngineApplicationInterface.IGameEntity.GetLocalFrame(this.Pointer, out result);
			return result;
		}

		// Token: 0x06000E3E RID: 3646 RVA: 0x000102E0 File Offset: 0x0000E4E0
		public void GetLocalFrame(out MatrixFrame frame)
		{
			EngineApplicationInterface.IGameEntity.GetLocalFrame(this.Pointer, out frame);
		}

		// Token: 0x06000E3F RID: 3647 RVA: 0x000102F3 File Offset: 0x0000E4F3
		public bool HasBatchedKinematicPhysicsFlag()
		{
			return EngineApplicationInterface.IGameEntity.HasBatchedKinematicPhysicsFlag(this.Pointer);
		}

		// Token: 0x06000E40 RID: 3648 RVA: 0x00010305 File Offset: 0x0000E505
		public bool HasBatchedRayCastPhysicsFlag()
		{
			return EngineApplicationInterface.IGameEntity.HasBatchedRayCastPhysicsFlag(this.Pointer);
		}

		// Token: 0x06000E41 RID: 3649 RVA: 0x00010318 File Offset: 0x0000E518
		public MatrixFrame GetLocalFrame()
		{
			MatrixFrame result;
			EngineApplicationInterface.IGameEntity.GetLocalFrame(this.Pointer, out result);
			return result;
		}

		// Token: 0x06000E42 RID: 3650 RVA: 0x00010338 File Offset: 0x0000E538
		public MatrixFrame GetGlobalFrame()
		{
			MatrixFrame result;
			EngineApplicationInterface.IGameEntity.GetGlobalFrame(this.Pointer, out result);
			return result;
		}

		// Token: 0x06000E43 RID: 3651 RVA: 0x00010358 File Offset: 0x0000E558
		public void SetWaterSDFClipData(int slotIndex, in MatrixFrame frame, bool visibility)
		{
			EngineApplicationInterface.IGameEntity.SetWaterSDFClipData(this.Pointer, slotIndex, frame, visibility);
		}

		// Token: 0x06000E44 RID: 3652 RVA: 0x0001036D File Offset: 0x0000E56D
		public int RegisterWaterSDFClip(Texture sdfTexture)
		{
			return EngineApplicationInterface.IGameEntity.RegisterWaterSDFClip(this.Pointer, (sdfTexture != null) ? sdfTexture.Pointer : UIntPtr.Zero);
		}

		// Token: 0x06000E45 RID: 3653 RVA: 0x00010395 File Offset: 0x0000E595
		public void DeRegisterWaterSDFClip(int slot)
		{
			EngineApplicationInterface.IGameEntity.DeRegisterWaterSDFClip(this.Pointer, slot);
		}

		// Token: 0x06000E46 RID: 3654 RVA: 0x000103A8 File Offset: 0x0000E5A8
		public MatrixFrame GetGlobalFrameImpreciseForFixedTick()
		{
			MatrixFrame result;
			EngineApplicationInterface.IGameEntity.GetGlobalFrameImpreciseForFixedTick(this.Pointer, out result);
			return result;
		}

		// Token: 0x06000E47 RID: 3655 RVA: 0x000103C8 File Offset: 0x0000E5C8
		public MatrixFrame ComputePreciseGlobalFrameForFixedTickSlow()
		{
			MatrixFrame result = this.GetLocalFrame();
			WeakGameEntity parent = this.Parent;
			while (parent.Parent != null)
			{
				result = parent.GetLocalFrame().TransformToParent(result);
				parent = parent.Parent;
			}
			result = parent.GetBodyWorldTransform().TransformToParent(result);
			return result;
		}

		// Token: 0x06000E48 RID: 3656 RVA: 0x00010421 File Offset: 0x0000E621
		public void SetGlobalFrame(in MatrixFrame frame, bool isTeleportation = true)
		{
			EngineApplicationInterface.IGameEntity.SetGlobalFrame(this.Pointer, frame, isTeleportation);
		}

		// Token: 0x06000E49 RID: 3657 RVA: 0x00010438 File Offset: 0x0000E638
		public MatrixFrame GetPreviousGlobalFrame()
		{
			MatrixFrame result;
			EngineApplicationInterface.IGameEntity.GetPreviousGlobalFrame(this.Pointer, out result);
			return result;
		}

		// Token: 0x06000E4A RID: 3658 RVA: 0x00010458 File Offset: 0x0000E658
		public MatrixFrame GetBodyWorldTransform()
		{
			MatrixFrame result;
			EngineApplicationInterface.IGameEntity.GetBodyWorldTransform(this.Pointer, out result);
			return result;
		}

		// Token: 0x06000E4B RID: 3659 RVA: 0x00010478 File Offset: 0x0000E678
		public MatrixFrame GetBodyVisualWorldTransform()
		{
			MatrixFrame result;
			EngineApplicationInterface.IGameEntity.GetBodyVisualWorldTransform(this.Pointer, out result);
			return result;
		}

		// Token: 0x06000E4C RID: 3660 RVA: 0x00010498 File Offset: 0x0000E698
		public void UpdateTriadFrameForEditor()
		{
			EngineApplicationInterface.IGameEntity.UpdateTriadFrameForEditor(this.Pointer);
		}

		// Token: 0x06000E4D RID: 3661 RVA: 0x000104AC File Offset: 0x0000E6AC
		public void UpdateTriadFrameForEditorForAllChildren()
		{
			this.UpdateTriadFrameForEditor();
			List<WeakGameEntity> list = new List<WeakGameEntity>();
			this.GetChildrenRecursive(ref list);
			foreach (WeakGameEntity weakGameEntity in list)
			{
				EngineApplicationInterface.IGameEntity.UpdateTriadFrameForEditor(weakGameEntity.Pointer);
			}
		}

		// Token: 0x06000E4E RID: 3662 RVA: 0x00010518 File Offset: 0x0000E718
		public Vec3 GetGlobalScale()
		{
			return EngineApplicationInterface.IGameEntity.GetGlobalScale(this.Pointer);
		}

		// Token: 0x06000E4F RID: 3663 RVA: 0x0001052C File Offset: 0x0000E72C
		public Vec3 GetLocalScale()
		{
			return this.GetFrame().rotation.GetScaleVector();
		}

		// Token: 0x170000AC RID: 172
		// (get) Token: 0x06000E50 RID: 3664 RVA: 0x0001054C File Offset: 0x0000E74C
		public Vec3 GlobalPosition
		{
			get
			{
				return this.GetGlobalFrame().origin;
			}
		}

		// Token: 0x06000E51 RID: 3665 RVA: 0x0001055C File Offset: 0x0000E75C
		public void SetAnimationSoundActivation(bool activate)
		{
			EngineApplicationInterface.IGameEntity.SetAnimationSoundActivation(this.Pointer, activate);
			foreach (WeakGameEntity weakGameEntity in this.GetChildren())
			{
				weakGameEntity.SetAnimationSoundActivation(activate);
			}
		}

		// Token: 0x06000E52 RID: 3666 RVA: 0x000105BC File Offset: 0x0000E7BC
		public void CopyComponentsToSkeleton()
		{
			EngineApplicationInterface.IGameEntity.CopyComponentsToSkeleton(this.Pointer);
		}

		// Token: 0x06000E53 RID: 3667 RVA: 0x000105CE File Offset: 0x0000E7CE
		public void AddMeshToBone(sbyte boneIndex, Mesh mesh)
		{
			EngineApplicationInterface.IGameEntity.AddMeshToBone(this.Pointer, mesh.Pointer, boneIndex);
		}

		// Token: 0x06000E54 RID: 3668 RVA: 0x000105E7 File Offset: 0x0000E7E7
		public void ActivateRagdoll()
		{
			EngineApplicationInterface.IGameEntity.ActivateRagdoll(this.Pointer);
		}

		// Token: 0x06000E55 RID: 3669 RVA: 0x000105F9 File Offset: 0x0000E7F9
		public void PauseSkeletonAnimation()
		{
			EngineApplicationInterface.IGameEntity.Freeze(this.Pointer, true);
		}

		// Token: 0x06000E56 RID: 3670 RVA: 0x0001060C File Offset: 0x0000E80C
		public void ResumeSkeletonAnimation()
		{
			EngineApplicationInterface.IGameEntity.Freeze(this.Pointer, false);
		}

		// Token: 0x06000E57 RID: 3671 RVA: 0x0001061F File Offset: 0x0000E81F
		public bool IsSkeletonAnimationPaused()
		{
			return EngineApplicationInterface.IGameEntity.IsFrozen(this.Pointer);
		}

		// Token: 0x06000E58 RID: 3672 RVA: 0x00010631 File Offset: 0x0000E831
		public sbyte GetBoneCount()
		{
			return EngineApplicationInterface.IGameEntity.GetBoneCount(this.Pointer);
		}

		// Token: 0x06000E59 RID: 3673 RVA: 0x00010643 File Offset: 0x0000E843
		public float GetWaterLevelAtPosition(Vec2 position, bool useWaterRenderer, bool checkWaterBodyEntities)
		{
			return EngineApplicationInterface.IGameEntity.GetWaterLevelAtPosition(this.Pointer, position, useWaterRenderer, checkWaterBodyEntities);
		}

		// Token: 0x06000E5A RID: 3674 RVA: 0x0001065C File Offset: 0x0000E85C
		public MatrixFrame GetBoneEntitialFrameWithIndex(sbyte boneIndex)
		{
			MatrixFrame result = default(MatrixFrame);
			EngineApplicationInterface.IGameEntity.GetBoneEntitialFrameWithIndex(this.Pointer, boneIndex, ref result);
			return result;
		}

		// Token: 0x06000E5B RID: 3675 RVA: 0x00010688 File Offset: 0x0000E888
		public MatrixFrame GetBoneEntitialFrameWithName(string boneName)
		{
			MatrixFrame result = default(MatrixFrame);
			EngineApplicationInterface.IGameEntity.GetBoneEntitialFrameWithName(this.Pointer, boneName, ref result);
			return result;
		}

		// Token: 0x170000AD RID: 173
		// (get) Token: 0x06000E5C RID: 3676 RVA: 0x000106B1 File Offset: 0x0000E8B1
		public string[] Tags
		{
			get
			{
				return EngineApplicationInterface.IGameEntity.GetTags(this.Pointer).Split(new char[] { ' ' });
			}
		}

		// Token: 0x06000E5D RID: 3677 RVA: 0x000106D3 File Offset: 0x0000E8D3
		public void AddTag(string tag)
		{
			EngineApplicationInterface.IGameEntity.AddTag(this.Pointer, tag);
		}

		// Token: 0x06000E5E RID: 3678 RVA: 0x000106E6 File Offset: 0x0000E8E6
		public void RemoveTag(string tag)
		{
			EngineApplicationInterface.IGameEntity.RemoveTag(this.Pointer, tag);
		}

		// Token: 0x06000E5F RID: 3679 RVA: 0x000106F9 File Offset: 0x0000E8F9
		public bool HasTag(string tag)
		{
			return EngineApplicationInterface.IGameEntity.HasTag(this.Pointer, tag);
		}

		// Token: 0x06000E60 RID: 3680 RVA: 0x0001070C File Offset: 0x0000E90C
		public void AddChild(WeakGameEntity gameEntity, bool autoLocalizeFrame = false)
		{
			EngineApplicationInterface.IGameEntity.AddChild(this.Pointer, gameEntity.Pointer, autoLocalizeFrame);
		}

		// Token: 0x06000E61 RID: 3681 RVA: 0x00010726 File Offset: 0x0000E926
		public void RemoveChild(WeakGameEntity childEntity, bool keepPhysics, bool keepScenePointer, bool callScriptCallbacks, int removeReason)
		{
			EngineApplicationInterface.IGameEntity.RemoveChild(this.Pointer, childEntity.Pointer, keepPhysics, keepScenePointer, callScriptCallbacks, removeReason);
		}

		// Token: 0x06000E62 RID: 3682 RVA: 0x00010745 File Offset: 0x0000E945
		public void BreakPrefab()
		{
			EngineApplicationInterface.IGameEntity.BreakPrefab(this.Pointer);
		}

		// Token: 0x170000AE RID: 174
		// (get) Token: 0x06000E63 RID: 3683 RVA: 0x00010757 File Offset: 0x0000E957
		public int ChildCount
		{
			get
			{
				return EngineApplicationInterface.IGameEntity.GetChildCount(this.Pointer);
			}
		}

		// Token: 0x06000E64 RID: 3684 RVA: 0x0001076C File Offset: 0x0000E96C
		public WeakGameEntity GetChild(int index)
		{
			UIntPtr childPointer = EngineApplicationInterface.IGameEntity.GetChildPointer(this.Pointer, index);
			if (!(childPointer != UIntPtr.Zero))
			{
				return new WeakGameEntity(UIntPtr.Zero);
			}
			return new WeakGameEntity(childPointer);
		}

		// Token: 0x170000AF RID: 175
		// (get) Token: 0x06000E65 RID: 3685 RVA: 0x000107AC File Offset: 0x0000E9AC
		public WeakGameEntity Parent
		{
			get
			{
				UIntPtr parentPointer = EngineApplicationInterface.IGameEntity.GetParentPointer(this.Pointer);
				if (!(parentPointer != UIntPtr.Zero))
				{
					return new WeakGameEntity(UIntPtr.Zero);
				}
				return new WeakGameEntity(parentPointer);
			}
		}

		// Token: 0x06000E66 RID: 3686 RVA: 0x000107E8 File Offset: 0x0000E9E8
		public bool HasComplexAnimTree()
		{
			return EngineApplicationInterface.IGameEntity.HasComplexAnimTree(this.Pointer);
		}

		// Token: 0x170000B0 RID: 176
		// (get) Token: 0x06000E67 RID: 3687 RVA: 0x000107FA File Offset: 0x0000E9FA
		public WeakGameEntity Root
		{
			get
			{
				return new WeakGameEntity(EngineApplicationInterface.IGameEntity.GetRootParentPointer(this.Pointer));
			}
		}

		// Token: 0x06000E68 RID: 3688 RVA: 0x00010811 File Offset: 0x0000EA11
		public void AddMultiMesh(MetaMesh metaMesh, bool updateVisMask = true)
		{
			EngineApplicationInterface.IGameEntity.AddMultiMesh(this.Pointer, metaMesh.Pointer, updateVisMask);
		}

		// Token: 0x06000E69 RID: 3689 RVA: 0x0001082A File Offset: 0x0000EA2A
		public bool RemoveMultiMesh(MetaMesh metaMesh)
		{
			return EngineApplicationInterface.IGameEntity.RemoveMultiMesh(this.Pointer, metaMesh.Pointer);
		}

		// Token: 0x170000B1 RID: 177
		// (get) Token: 0x06000E6A RID: 3690 RVA: 0x00010842 File Offset: 0x0000EA42
		public int MultiMeshComponentCount
		{
			get
			{
				return EngineApplicationInterface.IGameEntity.GetComponentCount(this.Pointer, GameEntity.ComponentType.MetaMesh);
			}
		}

		// Token: 0x170000B2 RID: 178
		// (get) Token: 0x06000E6B RID: 3691 RVA: 0x00010855 File Offset: 0x0000EA55
		public int ClothSimulatorComponentCount
		{
			get
			{
				return EngineApplicationInterface.IGameEntity.GetComponentCount(this.Pointer, GameEntity.ComponentType.ClothSimulator);
			}
		}

		// Token: 0x06000E6C RID: 3692 RVA: 0x00010868 File Offset: 0x0000EA68
		public int GetComponentCount(GameEntity.ComponentType componentType)
		{
			return EngineApplicationInterface.IGameEntity.GetComponentCount(this.Pointer, componentType);
		}

		// Token: 0x06000E6D RID: 3693 RVA: 0x0001087B File Offset: 0x0000EA7B
		public void AddAllMeshesOfGameEntity(GameEntity gameEntity)
		{
			EngineApplicationInterface.IGameEntity.AddAllMeshesOfGameEntity(this.Pointer, gameEntity.Pointer);
		}

		// Token: 0x06000E6E RID: 3694 RVA: 0x00010893 File Offset: 0x0000EA93
		public void SetFrameChanged()
		{
			EngineApplicationInterface.IGameEntity.SetFrameChanged(this.Pointer);
		}

		// Token: 0x06000E6F RID: 3695 RVA: 0x000108A5 File Offset: 0x0000EAA5
		public GameEntityComponent GetComponentAtIndex(int index, GameEntity.ComponentType componentType)
		{
			return EngineApplicationInterface.IGameEntity.GetComponentAtIndex(this.Pointer, componentType, index);
		}

		// Token: 0x06000E70 RID: 3696 RVA: 0x000108B9 File Offset: 0x0000EAB9
		public MetaMesh GetMetaMesh(int metaMeshIndex)
		{
			return (MetaMesh)EngineApplicationInterface.IGameEntity.GetComponentAtIndex(this.Pointer, GameEntity.ComponentType.MetaMesh, metaMeshIndex);
		}

		// Token: 0x06000E71 RID: 3697 RVA: 0x000108D2 File Offset: 0x0000EAD2
		public ClothSimulatorComponent GetClothSimulator(int clothSimulatorIndex)
		{
			return (ClothSimulatorComponent)EngineApplicationInterface.IGameEntity.GetComponentAtIndex(this.Pointer, GameEntity.ComponentType.ClothSimulator, clothSimulatorIndex);
		}

		// Token: 0x06000E72 RID: 3698 RVA: 0x000108EB File Offset: 0x0000EAEB
		public void SetVectorArgument(float vectorArgument0, float vectorArgument1, float vectorArgument2, float vectorArgument3)
		{
			EngineApplicationInterface.IGameEntity.SetVectorArgument(this.Pointer, vectorArgument0, vectorArgument1, vectorArgument2, vectorArgument3);
		}

		// Token: 0x06000E73 RID: 3699 RVA: 0x00010902 File Offset: 0x0000EB02
		public void SetMaterialForAllMeshes(Material material)
		{
			EngineApplicationInterface.IGameEntity.SetMaterialForAllMeshes(this.Pointer, material.Pointer);
		}

		// Token: 0x06000E74 RID: 3700 RVA: 0x0001091A File Offset: 0x0000EB1A
		public bool AddLight(Light light)
		{
			return EngineApplicationInterface.IGameEntity.AddLight(this.Pointer, light.Pointer);
		}

		// Token: 0x06000E75 RID: 3701 RVA: 0x00010932 File Offset: 0x0000EB32
		public Light GetLight()
		{
			return EngineApplicationInterface.IGameEntity.GetLight(this.Pointer);
		}

		// Token: 0x06000E76 RID: 3702 RVA: 0x00010944 File Offset: 0x0000EB44
		public void AddParticleSystemComponent(string particleid)
		{
			EngineApplicationInterface.IGameEntity.AddParticleSystemComponent(this.Pointer, particleid);
		}

		// Token: 0x06000E77 RID: 3703 RVA: 0x00010957 File Offset: 0x0000EB57
		public void RemoveAllParticleSystems()
		{
			EngineApplicationInterface.IGameEntity.RemoveAllParticleSystems(this.Pointer);
		}

		// Token: 0x06000E78 RID: 3704 RVA: 0x00010969 File Offset: 0x0000EB69
		public bool CheckPointWithOrientedBoundingBox(Vec3 point)
		{
			return EngineApplicationInterface.IGameEntity.CheckPointWithOrientedBoundingBox(this.Pointer, point);
		}

		// Token: 0x06000E79 RID: 3705 RVA: 0x0001097C File Offset: 0x0000EB7C
		public void PauseParticleSystem(bool doChildren)
		{
			EngineApplicationInterface.IGameEntity.PauseParticleSystem(this.Pointer, doChildren);
		}

		// Token: 0x06000E7A RID: 3706 RVA: 0x0001098F File Offset: 0x0000EB8F
		public void ResumeParticleSystem(bool doChildren)
		{
			EngineApplicationInterface.IGameEntity.ResumeParticleSystem(this.Pointer, doChildren);
		}

		// Token: 0x06000E7B RID: 3707 RVA: 0x000109A2 File Offset: 0x0000EBA2
		public void BurstEntityParticle(bool doChildren)
		{
			EngineApplicationInterface.IGameEntity.BurstEntityParticle(this.Pointer, doChildren);
		}

		// Token: 0x06000E7C RID: 3708 RVA: 0x000109B5 File Offset: 0x0000EBB5
		public void SetRuntimeEmissionRateMultiplier(float emissionRateMultiplier)
		{
			EngineApplicationInterface.IGameEntity.SetRuntimeEmissionRateMultiplier(this.Pointer, emissionRateMultiplier);
		}

		// Token: 0x06000E7D RID: 3709 RVA: 0x000109C8 File Offset: 0x0000EBC8
		public BoundingBox GetLocalBoundingBox()
		{
			return EngineApplicationInterface.IGameEntity.GetLocalBoundingBox(this.Pointer);
		}

		// Token: 0x06000E7E RID: 3710 RVA: 0x000109DA File Offset: 0x0000EBDA
		public BoundingBox GetGlobalBoundingBox()
		{
			return EngineApplicationInterface.IGameEntity.GetGlobalBoundingBox(this.Pointer);
		}

		// Token: 0x06000E7F RID: 3711 RVA: 0x000109EC File Offset: 0x0000EBEC
		public Vec3 GetBoundingBoxMin()
		{
			return EngineApplicationInterface.IGameEntity.GetBoundingBoxMin(this.Pointer);
		}

		// Token: 0x06000E80 RID: 3712 RVA: 0x000109FE File Offset: 0x0000EBFE
		public void SetHasCustomBoundingBoxValidationSystem(bool hasCustomBoundingBox)
		{
			EngineApplicationInterface.IGameEntity.SetHasCustomBoundingBoxValidationSystem(this.Pointer, hasCustomBoundingBox);
		}

		// Token: 0x06000E81 RID: 3713 RVA: 0x00010A11 File Offset: 0x0000EC11
		public void ValidateBoundingBox()
		{
			EngineApplicationInterface.IGameEntity.ValidateBoundingBox(this.Pointer);
		}

		// Token: 0x06000E82 RID: 3714 RVA: 0x00010A23 File Offset: 0x0000EC23
		public Vec3 GetBoundingBoxMax()
		{
			return EngineApplicationInterface.IGameEntity.GetBoundingBoxMax(this.Pointer);
		}

		// Token: 0x06000E83 RID: 3715 RVA: 0x00010A35 File Offset: 0x0000EC35
		public void UpdateGlobalBounds()
		{
			EngineApplicationInterface.IGameEntity.UpdateGlobalBounds(this.Pointer);
		}

		// Token: 0x06000E84 RID: 3716 RVA: 0x00010A47 File Offset: 0x0000EC47
		public void RecomputeBoundingBox()
		{
			EngineApplicationInterface.IGameEntity.RecomputeBoundingBox(this.Pointer);
		}

		// Token: 0x06000E85 RID: 3717 RVA: 0x00010A59 File Offset: 0x0000EC59
		public float GetBoundingBoxRadius()
		{
			return EngineApplicationInterface.IGameEntity.GetRadius(this.Pointer);
		}

		// Token: 0x06000E86 RID: 3718 RVA: 0x00010A6B File Offset: 0x0000EC6B
		public void SetBoundingboxDirty()
		{
			EngineApplicationInterface.IGameEntity.SetBoundingboxDirty(this.Pointer);
		}

		// Token: 0x170000B3 RID: 179
		// (get) Token: 0x06000E87 RID: 3719 RVA: 0x00010A7D File Offset: 0x0000EC7D
		public Vec3 GlobalBoxMax
		{
			get
			{
				return EngineApplicationInterface.IGameEntity.GetGlobalBoxMax(this.Pointer);
			}
		}

		// Token: 0x06000E88 RID: 3720 RVA: 0x00010A90 File Offset: 0x0000EC90
		public ValueTuple<Vec3, Vec3> ComputeGlobalPhysicsBoundingBoxMinMax()
		{
			MatrixFrame globalFrame = this.GetGlobalFrame();
			BoundingBox localPhysicsBoundingBox = this.GetLocalPhysicsBoundingBox(true);
			Vec3 item = globalFrame.TransformToParent(localPhysicsBoundingBox.min);
			Vec3 item2 = globalFrame.TransformToParent(localPhysicsBoundingBox.max);
			return new ValueTuple<Vec3, Vec3>(item, item2);
		}

		// Token: 0x06000E89 RID: 3721 RVA: 0x00010AD4 File Offset: 0x0000ECD4
		public Vec3 ComputeGlobalPhysicsBoundingBoxCenter()
		{
			return this.GetGlobalFrame().TransformToParent(this.GetLocalPhysicsBoundingBox(true).center);
		}

		// Token: 0x06000E8A RID: 3722 RVA: 0x00010B04 File Offset: 0x0000ED04
		public void SetContourColor(uint? color, bool alwaysVisible = true)
		{
			if (color != null)
			{
				EngineApplicationInterface.IGameEntity.SetAsContourEntity(this.Pointer, color.Value);
				EngineApplicationInterface.IGameEntity.SetContourState(this.Pointer, alwaysVisible);
				return;
			}
			EngineApplicationInterface.IGameEntity.DisableContour(this.Pointer);
		}

		// Token: 0x170000B4 RID: 180
		// (get) Token: 0x06000E8B RID: 3723 RVA: 0x00010B53 File Offset: 0x0000ED53
		public Vec3 GlobalBoxMin
		{
			get
			{
				return EngineApplicationInterface.IGameEntity.GetGlobalBoxMin(this.Pointer);
			}
		}

		// Token: 0x06000E8C RID: 3724 RVA: 0x00010B65 File Offset: 0x0000ED65
		public bool GetHasFrameChanged()
		{
			return EngineApplicationInterface.IGameEntity.HasFrameChanged(this.Pointer);
		}

		// Token: 0x06000E8D RID: 3725 RVA: 0x00010B77 File Offset: 0x0000ED77
		public Mesh GetFirstMesh()
		{
			return EngineApplicationInterface.IGameEntity.GetFirstMesh(this.Pointer);
		}

		// Token: 0x06000E8E RID: 3726 RVA: 0x00010B89 File Offset: 0x0000ED89
		public int GetAttachedNavmeshFaceCount()
		{
			return EngineApplicationInterface.IGameEntity.GetAttachedNavmeshFaceCount(this.Pointer);
		}

		// Token: 0x06000E8F RID: 3727 RVA: 0x00010B9B File Offset: 0x0000ED9B
		public void GetAttachedNavmeshFaceRecords(PathFaceRecord[] faceRecords)
		{
			EngineApplicationInterface.IGameEntity.GetAttachedNavmeshFaceRecords(this.Pointer, faceRecords);
		}

		// Token: 0x06000E90 RID: 3728 RVA: 0x00010BAE File Offset: 0x0000EDAE
		public void GetAttachedNavmeshFaceVertexIndices(in PathFaceRecord faceRecord, int[] indices)
		{
			EngineApplicationInterface.IGameEntity.GetAttachedNavmeshFaceVertexIndices(this.Pointer, faceRecord, indices);
		}

		// Token: 0x06000E91 RID: 3729 RVA: 0x00010BC2 File Offset: 0x0000EDC2
		public void SetCustomVertexPositionEnabled(bool customVertexPositionEnabled)
		{
			EngineApplicationInterface.IGameEntity.SetCustomVertexPositionEnabled(this.Pointer, customVertexPositionEnabled);
		}

		// Token: 0x06000E92 RID: 3730 RVA: 0x00010BD5 File Offset: 0x0000EDD5
		public void SetPositionsForAttachedNavmeshVertices(int[] vertices, int indexCount, Vec3[] positions)
		{
			EngineApplicationInterface.IGameEntity.SetPositionsForAttachedNavmeshVertices(this.Pointer, vertices, indexCount, positions);
		}

		// Token: 0x06000E93 RID: 3731 RVA: 0x00010BEA File Offset: 0x0000EDEA
		public void SetCostAdderForAttachedFaces(float costs)
		{
			EngineApplicationInterface.IGameEntity.SetCostAdderForAttachedFaces(this.Pointer, costs);
		}

		// Token: 0x06000E94 RID: 3732 RVA: 0x00010BFD File Offset: 0x0000EDFD
		public void SetExternalReferencesUsage(bool value)
		{
			EngineApplicationInterface.IGameEntity.SetExternalReferencesUsage(this.Pointer, value);
		}

		// Token: 0x06000E95 RID: 3733 RVA: 0x00010C10 File Offset: 0x0000EE10
		public void SetMorphFrameOfComponents(float value)
		{
			EngineApplicationInterface.IGameEntity.SetMorphFrameOfComponents(this.Pointer, value);
		}

		// Token: 0x06000E96 RID: 3734 RVA: 0x00010C23 File Offset: 0x0000EE23
		public void AddEditDataUserToAllMeshes(bool entityComponents, bool skeletonComponents)
		{
			EngineApplicationInterface.IGameEntity.AddEditDataUserToAllMeshes(this.Pointer, entityComponents, skeletonComponents);
		}

		// Token: 0x06000E97 RID: 3735 RVA: 0x00010C37 File Offset: 0x0000EE37
		public void ReleaseEditDataUserToAllMeshes(bool entityComponents, bool skeletonComponents)
		{
			EngineApplicationInterface.IGameEntity.ReleaseEditDataUserToAllMeshes(this.Pointer, entityComponents, skeletonComponents);
		}

		// Token: 0x06000E98 RID: 3736 RVA: 0x00010C4B File Offset: 0x0000EE4B
		public void GetCameraParamsFromCameraScript(Camera cam, ref Vec3 dofParams)
		{
			EngineApplicationInterface.IGameEntity.GetCameraParamsFromCameraScript(this.Pointer, cam.Pointer, ref dofParams);
		}

		// Token: 0x06000E99 RID: 3737 RVA: 0x00010C64 File Offset: 0x0000EE64
		public void GetMeshBendedFrame(MatrixFrame worldSpacePosition, ref MatrixFrame output)
		{
			EngineApplicationInterface.IGameEntity.GetMeshBendedPosition(this.Pointer, ref worldSpacePosition, ref output);
		}

		// Token: 0x06000E9A RID: 3738 RVA: 0x00010C79 File Offset: 0x0000EE79
		public void ComputeTrajectoryVolume(float missileSpeed, float verticalAngleMaxInDegrees, float verticalAngleMinInDegrees, float horizontalAngleRangeInDegrees, float airFrictionConstant)
		{
			EngineApplicationInterface.IGameEntity.ComputeTrajectoryVolume(this.Pointer, missileSpeed, verticalAngleMaxInDegrees, verticalAngleMinInDegrees, horizontalAngleRangeInDegrees, airFrictionConstant);
		}

		// Token: 0x06000E9B RID: 3739 RVA: 0x00010C92 File Offset: 0x0000EE92
		public void SetAnimTreeChannelParameterForceUpdate(float phase, int channelNo)
		{
			EngineApplicationInterface.IGameEntity.SetAnimTreeChannelParameter(this.Pointer, phase, channelNo);
		}

		// Token: 0x06000E9C RID: 3740 RVA: 0x00010CA6 File Offset: 0x0000EEA6
		public void ChangeMetaMeshOrRemoveItIfNotExists(MetaMesh entityMetaMesh, MetaMesh newMetaMesh)
		{
			EngineApplicationInterface.IGameEntity.ChangeMetaMeshOrRemoveItIfNotExists(this.Pointer, (entityMetaMesh != null) ? entityMetaMesh.Pointer : UIntPtr.Zero, (newMetaMesh != null) ? newMetaMesh.Pointer : UIntPtr.Zero);
		}

		// Token: 0x06000E9D RID: 3741 RVA: 0x00010CE4 File Offset: 0x0000EEE4
		public void SetUpdateValidtyOnFrameChangedOfFacesWithId(int faceGroupId, bool updateValidity)
		{
			EngineApplicationInterface.IGameEntity.SetUpdateValidityOnFrameChangedOfFacesWithId(this.Pointer, faceGroupId, updateValidity);
		}

		// Token: 0x06000E9E RID: 3742 RVA: 0x00010CF8 File Offset: 0x0000EEF8
		public void AttachNavigationMeshFaces(int faceGroupId, bool isConnected, bool isBlocker = false, bool autoLocalize = false, bool finalizeBlockerConvexHullComputation = false, bool updateEntityFrame = true)
		{
			EngineApplicationInterface.IGameEntity.AttachNavigationMeshFaces(this.Pointer, faceGroupId, isConnected, isBlocker, autoLocalize, finalizeBlockerConvexHullComputation, updateEntityFrame);
		}

		// Token: 0x06000E9F RID: 3743 RVA: 0x00010D13 File Offset: 0x0000EF13
		public void DetachAllAttachedNavigationMeshFaces()
		{
			EngineApplicationInterface.IGameEntity.DetachAllAttachedNavigationMeshFaces(this.Pointer);
		}

		// Token: 0x06000EA0 RID: 3744 RVA: 0x00010D25 File Offset: 0x0000EF25
		public void UpdateAttachedNavigationMeshFaces()
		{
			EngineApplicationInterface.IGameEntity.UpdateAttachedNavigationMeshFaces(this.Pointer);
		}

		// Token: 0x06000EA1 RID: 3745 RVA: 0x00010D37 File Offset: 0x0000EF37
		public void RemoveSkeleton()
		{
			this.Skeleton = null;
		}

		// Token: 0x170000B5 RID: 181
		// (get) Token: 0x06000EA2 RID: 3746 RVA: 0x00010D40 File Offset: 0x0000EF40
		// (set) Token: 0x06000EA3 RID: 3747 RVA: 0x00010D52 File Offset: 0x0000EF52
		public Skeleton Skeleton
		{
			get
			{
				return EngineApplicationInterface.IGameEntity.GetSkeleton(this.Pointer);
			}
			set
			{
				EngineApplicationInterface.IGameEntity.SetSkeleton(this.Pointer, (value != null) ? value.Pointer : UIntPtr.Zero);
			}
		}

		// Token: 0x06000EA4 RID: 3748 RVA: 0x00010D74 File Offset: 0x0000EF74
		public void RemoveAllChildren()
		{
			EngineApplicationInterface.IGameEntity.RemoveAllChildren(this.Pointer);
		}

		// Token: 0x06000EA5 RID: 3749 RVA: 0x00010D86 File Offset: 0x0000EF86
		public IEnumerable<WeakGameEntity> GetChildren()
		{
			int count = this.ChildCount;
			int num;
			for (int i = 0; i < count; i = num + 1)
			{
				yield return this.GetChild(i);
				num = i;
			}
			yield break;
		}

		// Token: 0x06000EA6 RID: 3750 RVA: 0x00010D9B File Offset: 0x0000EF9B
		public IEnumerable<WeakGameEntity> GetEntityAndChildren()
		{
			yield return ref this;
			int count = this.ChildCount;
			int num;
			for (int i = 0; i < count; i = num + 1)
			{
				yield return this.GetChild(i);
				num = i;
			}
			yield break;
		}

		// Token: 0x06000EA7 RID: 3751 RVA: 0x00010DB0 File Offset: 0x0000EFB0
		public void GetChildrenRecursive(ref List<WeakGameEntity> children)
		{
			int childCount = this.ChildCount;
			for (int i = 0; i < childCount; i++)
			{
				WeakGameEntity child = this.GetChild(i);
				children.Add(child);
				child.GetChildrenRecursive(ref children);
			}
		}

		// Token: 0x06000EA8 RID: 3752 RVA: 0x00010DE8 File Offset: 0x0000EFE8
		public void GetChildrenWithTagRecursive(List<WeakGameEntity> children, string tag)
		{
			int childCount = this.ChildCount;
			for (int i = 0; i < childCount; i++)
			{
				WeakGameEntity child = this.GetChild(i);
				if (child.HasTag(tag))
				{
					children.Add(child);
				}
				child.GetChildrenWithTagRecursive(children, tag);
			}
		}

		// Token: 0x06000EA9 RID: 3753 RVA: 0x00010E2A File Offset: 0x0000F02A
		public bool IsSelectedOnEditor()
		{
			return EngineApplicationInterface.IGameEntity.IsEntitySelectedOnEditor(this.Pointer);
		}

		// Token: 0x06000EAA RID: 3754 RVA: 0x00010E3C File Offset: 0x0000F03C
		public void SelectEntityOnEditor()
		{
			EngineApplicationInterface.IGameEntity.SelectEntityOnEditor(this.Pointer);
		}

		// Token: 0x06000EAB RID: 3755 RVA: 0x00010E4E File Offset: 0x0000F04E
		public void DeselectEntityOnEditor()
		{
			EngineApplicationInterface.IGameEntity.DeselectEntityOnEditor(this.Pointer);
		}

		// Token: 0x06000EAC RID: 3756 RVA: 0x00010E60 File Offset: 0x0000F060
		public void SetAsPredisplayEntity()
		{
			EngineApplicationInterface.IGameEntity.SetAsPredisplayEntity(this.Pointer);
		}

		// Token: 0x06000EAD RID: 3757 RVA: 0x00010E72 File Offset: 0x0000F072
		public void RemoveFromPredisplayEntity()
		{
			EngineApplicationInterface.IGameEntity.RemoveFromPredisplayEntity(this.Pointer);
		}

		// Token: 0x06000EAE RID: 3758 RVA: 0x00010E84 File Offset: 0x0000F084
		public void SetNativeScriptComponentVariable(string className, string fieldName, ref ScriptComponentFieldHolder data, RglScriptFieldType variableType)
		{
			EngineApplicationInterface.IGameEntity.SetNativeScriptComponentVariable(this.Pointer, className, fieldName, ref data, variableType);
		}

		// Token: 0x06000EAF RID: 3759 RVA: 0x00010E9B File Offset: 0x0000F09B
		public void SetManualGlobalBoundingBox(Vec3 boundingBoxStartGlobal, Vec3 boundingBoxEndGlobal)
		{
			EngineApplicationInterface.IGameEntity.SetManualGlobalBoundingBox(this.Pointer, boundingBoxStartGlobal, boundingBoxEndGlobal);
		}

		// Token: 0x06000EB0 RID: 3760 RVA: 0x00010EAF File Offset: 0x0000F0AF
		public bool RayHitEntityWithNormal(Vec3 rayOrigin, Vec3 rayDirection, float maxLength, ref Vec3 resultNormal, ref float resultLength)
		{
			return EngineApplicationInterface.IGameEntity.RayHitEntityWithNormal(this.Pointer, rayOrigin, rayDirection, maxLength, ref resultNormal, ref resultLength);
		}

		// Token: 0x06000EB1 RID: 3761 RVA: 0x00010ECA File Offset: 0x0000F0CA
		public bool RayHitEntity(Vec3 rayOrigin, Vec3 rayDirection, float maxLength, ref float resultLength)
		{
			return EngineApplicationInterface.IGameEntity.RayHitEntity(this.Pointer, rayOrigin, rayDirection, maxLength, ref resultLength);
		}

		// Token: 0x06000EB2 RID: 3762 RVA: 0x00010EE3 File Offset: 0x0000F0E3
		public void GetNativeScriptComponentVariable(string className, string fieldName, ref ScriptComponentFieldHolder data, RglScriptFieldType variableType)
		{
			EngineApplicationInterface.IGameEntity.GetNativeScriptComponentVariable(this.Pointer, className, fieldName, ref data, variableType);
		}

		// Token: 0x06000EB3 RID: 3763 RVA: 0x00010EFA File Offset: 0x0000F0FA
		public void SetCustomClipPlane(Vec3 clipPosition, Vec3 clipNormal, bool setForChildren)
		{
			EngineApplicationInterface.IGameEntity.SetCustomClipPlane(this.Pointer, clipPosition, clipNormal, setForChildren);
		}

		// Token: 0x06000EB4 RID: 3764 RVA: 0x00010F0F File Offset: 0x0000F10F
		public float GetBoundingBoxLongestHalfDimension()
		{
			return BoundingBox.GetLongestHalfDimensionOfBoundingBox(this.GetLocalBoundingBox());
		}

		// Token: 0x06000EB5 RID: 3765 RVA: 0x00010F1C File Offset: 0x0000F11C
		public BoundingBox ComputeBoundingBoxFromLongestHalfDimension(float longestHalfDimensionCoefficient)
		{
			BoundingBox localBoundingBox = this.GetLocalBoundingBox();
			BoundingBox result = default(BoundingBox);
			float num = this.GetBoundingBoxLongestHalfDimension() * longestHalfDimensionCoefficient;
			Vec3 v = new Vec3(num, num, num, -1f);
			result.min = localBoundingBox.center - v;
			result.max = localBoundingBox.center + v;
			result.RecomputeRadius();
			return result;
		}

		// Token: 0x06000EB6 RID: 3766 RVA: 0x00010F80 File Offset: 0x0000F180
		public BoundingBox ComputeBoundingBoxIncludeChildren()
		{
			BoundingBox result = default(BoundingBox);
			result.BeginRelaxation();
			foreach (WeakGameEntity weakGameEntity in this.GetChildren())
			{
				weakGameEntity.ValidateBoundingBox();
				BoundingBox localBoundingBox = weakGameEntity.GetLocalBoundingBox();
				result.RelaxWithChildBoundingBox(localBoundingBox, weakGameEntity.GetFrame());
			}
			result.RecomputeRadius();
			return result;
		}

		// Token: 0x06000EB7 RID: 3767 RVA: 0x00010FFC File Offset: 0x0000F1FC
		public void SetManualLocalBoundingBox(in BoundingBox boundingBox)
		{
			EngineApplicationInterface.IGameEntity.SetManualLocalBoundingBox(this.Pointer, boundingBox);
		}

		// Token: 0x06000EB8 RID: 3768 RVA: 0x0001100F File Offset: 0x0000F20F
		public void RelaxLocalBoundingBox(in BoundingBox boundingBox)
		{
			EngineApplicationInterface.IGameEntity.RelaxLocalBoundingBox(this.Pointer, boundingBox);
		}

		// Token: 0x06000EB9 RID: 3769 RVA: 0x00011022 File Offset: 0x0000F222
		public void SetCullMode(MBMeshCullingMode cullMode)
		{
			EngineApplicationInterface.IGameEntity.SetCullMode(this.Pointer, cullMode);
		}

		// Token: 0x06000EBA RID: 3770 RVA: 0x00011038 File Offset: 0x0000F238
		public WeakGameEntity GetFirstChildEntityWithTagRecursive(string tag)
		{
			UIntPtr firstChildWithTagRecursive = EngineApplicationInterface.IGameEntity.GetFirstChildWithTagRecursive(this.Pointer, tag);
			if (firstChildWithTagRecursive != UIntPtr.Zero)
			{
				return new WeakGameEntity(firstChildWithTagRecursive);
			}
			return WeakGameEntity.Invalid;
		}

		// Token: 0x06000EBB RID: 3771 RVA: 0x00011070 File Offset: 0x0000F270
		public override bool Equals(object obj)
		{
			return ((WeakGameEntity)obj).Pointer == this.Pointer;
		}

		// Token: 0x06000EBC RID: 3772 RVA: 0x00011098 File Offset: 0x0000F298
		public override int GetHashCode()
		{
			return this.Pointer.GetHashCode();
		}

		// Token: 0x06000EBD RID: 3773 RVA: 0x000110B3 File Offset: 0x0000F2B3
		public static bool operator ==(WeakGameEntity weakGameEntity, GameEntity entity)
		{
			return weakGameEntity.Pointer == ((entity != null) ? entity.Pointer : UIntPtr.Zero);
		}

		// Token: 0x06000EBE RID: 3774 RVA: 0x000110D1 File Offset: 0x0000F2D1
		public static bool operator !=(WeakGameEntity weakGameEntity, GameEntity entity)
		{
			return weakGameEntity.Pointer != ((entity != null) ? entity.Pointer : UIntPtr.Zero);
		}

		// Token: 0x06000EBF RID: 3775 RVA: 0x000110EF File Offset: 0x0000F2EF
		public static bool operator ==(WeakGameEntity weakGameEntity1, WeakGameEntity weakGameEntity2)
		{
			return weakGameEntity1.Pointer == weakGameEntity2.Pointer;
		}

		// Token: 0x06000EC0 RID: 3776 RVA: 0x00011104 File Offset: 0x0000F304
		public static bool operator !=(WeakGameEntity weakGameEntity1, WeakGameEntity weakGameEntity2)
		{
			return weakGameEntity1.Pointer != weakGameEntity2.Pointer;
		}

		// Token: 0x06000EC1 RID: 3777 RVA: 0x0001111C File Offset: 0x0000F31C
		public List<WeakGameEntity> CollectChildrenEntitiesWithTag(string tag)
		{
			List<WeakGameEntity> list = new List<WeakGameEntity>();
			foreach (WeakGameEntity item in this.GetChildren())
			{
				if (item.HasTag(tag))
				{
					list.Add(item);
				}
				if (item.ChildCount > 0)
				{
					list.AddRange(item.CollectChildrenEntitiesWithTag(tag));
				}
			}
			return list;
		}

		// Token: 0x06000EC2 RID: 3778 RVA: 0x00011194 File Offset: 0x0000F394
		public IEnumerable<WeakGameEntity> CollectChildrenEntitiesWithTagAsEnumarable(string tag)
		{
			foreach (WeakGameEntity child in this.GetChildren())
			{
				if (child.HasTag(tag))
				{
					yield return child;
				}
				if (child.ChildCount > 0)
				{
					foreach (WeakGameEntity weakGameEntity in child.CollectChildrenEntitiesWithTagAsEnumarable(tag))
					{
						yield return weakGameEntity;
					}
					IEnumerator<WeakGameEntity> enumerator2 = null;
				}
			}
			IEnumerator<WeakGameEntity> enumerator = null;
			yield break;
			yield break;
		}

		// Token: 0x06000EC3 RID: 3779 RVA: 0x000111B0 File Offset: 0x0000F3B0
		public void SetDoNotCheckVisibility(bool value)
		{
			EngineApplicationInterface.IGameEntity.SetDoNotCheckVisibility(this.Pointer, value);
		}

		// Token: 0x06000EC4 RID: 3780 RVA: 0x000111C3 File Offset: 0x0000F3C3
		public void SetBoneFrameToAllMeshes(int boneIndex, in MatrixFrame frame)
		{
			EngineApplicationInterface.IGameEntity.SetBoneFrameToAllMeshes(this.Pointer, boneIndex, frame);
		}

		// Token: 0x06000EC5 RID: 3781 RVA: 0x000111D7 File Offset: 0x0000F3D7
		public Vec2 GetGlobalWindStrengthVectorOfScene()
		{
			return EngineApplicationInterface.IGameEntity.GetGlobalWindStrengthVectorOfScene(this.Pointer);
		}

		// Token: 0x06000EC6 RID: 3782 RVA: 0x000111E9 File Offset: 0x0000F3E9
		public Vec2 GetGlobalWindVelocityOfScene()
		{
			return EngineApplicationInterface.IGameEntity.GetGlobalWindVelocityOfScene(this.Pointer);
		}

		// Token: 0x06000EC7 RID: 3783 RVA: 0x000111FB File Offset: 0x0000F3FB
		public Vec3 GetLastFinalRenderCameraPositionOfScene()
		{
			return EngineApplicationInterface.IGameEntity.GetLastFinalRenderCameraPositionOfScene(this.Pointer);
		}

		// Token: 0x06000EC8 RID: 3784 RVA: 0x0001120D File Offset: 0x0000F40D
		public Vec2 GetGlobalWindVelocityWithGustNoiseOfScene(float globalTime)
		{
			return EngineApplicationInterface.IGameEntity.GetGlobalWindVelocityWithGustNoiseOfScene(this.Pointer, globalTime);
		}

		// Token: 0x06000EC9 RID: 3785 RVA: 0x00011220 File Offset: 0x0000F420
		public void SetForceDecalsToRender(bool value)
		{
			EngineApplicationInterface.IGameEntity.SetForceDecalsToRender(this.Pointer, value);
		}

		// Token: 0x06000ECA RID: 3786 RVA: 0x00011233 File Offset: 0x0000F433
		public UIntPtr CreateEmptyPhysxShape(bool isVariable, int physxMaterialIndex)
		{
			return EngineApplicationInterface.IGameEntity.CreateEmptyPhysxShape(this.Pointer, isVariable, physxMaterialIndex);
		}

		// Token: 0x06000ECB RID: 3787 RVA: 0x00011247 File Offset: 0x0000F447
		public void SetForceNotAffectedBySeason(bool value)
		{
			EngineApplicationInterface.IGameEntity.SetForceNotAffectedBySeason(this.Pointer, value);
		}

		// Token: 0x06000ECC RID: 3788 RVA: 0x0001125A File Offset: 0x0000F45A
		public bool CheckIsPrefabLinkRootPrefab(int depth)
		{
			return EngineApplicationInterface.IGameEntity.CheckIsPrefabLinkRootPrefab(this.Pointer, depth);
		}

		// Token: 0x04000201 RID: 513
		public static readonly WeakGameEntity Invalid = new WeakGameEntity(UIntPtr.Zero);
	}
}
