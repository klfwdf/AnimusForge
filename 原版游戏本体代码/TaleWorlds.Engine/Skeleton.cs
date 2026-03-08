using System;
using System.Collections.Generic;
using TaleWorlds.DotNet;
using TaleWorlds.Library;

namespace TaleWorlds.Engine
{
	// Token: 0x0200008C RID: 140
	[EngineClass("rglSkeleton")]
	public sealed class Skeleton : NativeObject
	{
		// Token: 0x06000C53 RID: 3155 RVA: 0x0000D998 File Offset: 0x0000BB98
		internal Skeleton(UIntPtr pointer)
		{
			base.Construct(pointer);
		}

		// Token: 0x06000C54 RID: 3156 RVA: 0x0000D9A7 File Offset: 0x0000BBA7
		public static Skeleton CreateFromModel(string modelName)
		{
			return EngineApplicationInterface.ISkeleton.CreateFromModel(modelName);
		}

		// Token: 0x06000C55 RID: 3157 RVA: 0x0000D9B4 File Offset: 0x0000BBB4
		public static Skeleton CreateFromModelWithNullAnimTree(GameEntity entity, string modelName, float boneScale = 1f)
		{
			return EngineApplicationInterface.ISkeleton.CreateFromModelWithNullAnimTree(entity.Pointer, modelName, boneScale);
		}

		// Token: 0x17000092 RID: 146
		// (get) Token: 0x06000C56 RID: 3158 RVA: 0x0000D9C8 File Offset: 0x0000BBC8
		public bool IsValid
		{
			get
			{
				return base.Pointer != UIntPtr.Zero;
			}
		}

		// Token: 0x06000C57 RID: 3159 RVA: 0x0000D9DA File Offset: 0x0000BBDA
		public string GetName()
		{
			return EngineApplicationInterface.ISkeleton.GetName(this);
		}

		// Token: 0x06000C58 RID: 3160 RVA: 0x0000D9E7 File Offset: 0x0000BBE7
		public string GetBoneName(sbyte boneIndex)
		{
			return EngineApplicationInterface.ISkeleton.GetBoneName(this, boneIndex);
		}

		// Token: 0x06000C59 RID: 3161 RVA: 0x0000D9F5 File Offset: 0x0000BBF5
		public sbyte GetBoneChildAtIndex(sbyte boneIndex, sbyte childIndex)
		{
			return EngineApplicationInterface.ISkeleton.GetBoneChildAtIndex(this, boneIndex, childIndex);
		}

		// Token: 0x06000C5A RID: 3162 RVA: 0x0000DA04 File Offset: 0x0000BC04
		public sbyte GetBoneChildCount(sbyte boneIndex)
		{
			return EngineApplicationInterface.ISkeleton.GetBoneChildCount(this, boneIndex);
		}

		// Token: 0x06000C5B RID: 3163 RVA: 0x0000DA12 File Offset: 0x0000BC12
		public sbyte GetParentBoneIndex(sbyte boneIndex)
		{
			return EngineApplicationInterface.ISkeleton.GetParentBoneIndex(this, boneIndex);
		}

		// Token: 0x06000C5C RID: 3164 RVA: 0x0000DA20 File Offset: 0x0000BC20
		public void AddMeshToBone(UIntPtr mesh, sbyte boneIndex)
		{
			EngineApplicationInterface.ISkeleton.AddMeshToBone(base.Pointer, mesh, boneIndex);
		}

		// Token: 0x06000C5D RID: 3165 RVA: 0x0000DA34 File Offset: 0x0000BC34
		public void Freeze(bool p)
		{
			EngineApplicationInterface.ISkeleton.Freeze(base.Pointer, p);
		}

		// Token: 0x06000C5E RID: 3166 RVA: 0x0000DA47 File Offset: 0x0000BC47
		public bool IsFrozen()
		{
			return EngineApplicationInterface.ISkeleton.IsFrozen(base.Pointer);
		}

		// Token: 0x06000C5F RID: 3167 RVA: 0x0000DA59 File Offset: 0x0000BC59
		public void SetBoneLocalFrame(sbyte boneIndex, MatrixFrame localFrame)
		{
			EngineApplicationInterface.ISkeleton.SetBoneLocalFrame(base.Pointer, boneIndex, ref localFrame);
		}

		// Token: 0x06000C60 RID: 3168 RVA: 0x0000DA6E File Offset: 0x0000BC6E
		public sbyte GetBoneCount()
		{
			return EngineApplicationInterface.ISkeleton.GetBoneCount(base.Pointer);
		}

		// Token: 0x06000C61 RID: 3169 RVA: 0x0000DA80 File Offset: 0x0000BC80
		public void GetBoneBody(sbyte boneIndex, ref CapsuleData data)
		{
			EngineApplicationInterface.ISkeleton.GetBoneBody(base.Pointer, boneIndex, ref data);
		}

		// Token: 0x06000C62 RID: 3170 RVA: 0x0000DA94 File Offset: 0x0000BC94
		public static bool SkeletonModelExist(string skeletonModelName)
		{
			return EngineApplicationInterface.ISkeleton.SkeletonModelExist(skeletonModelName);
		}

		// Token: 0x06000C63 RID: 3171 RVA: 0x0000DAA1 File Offset: 0x0000BCA1
		public void ForceUpdateBoneFrames()
		{
			EngineApplicationInterface.ISkeleton.ForceUpdateBoneFrames(base.Pointer);
		}

		// Token: 0x06000C64 RID: 3172 RVA: 0x0000DAB4 File Offset: 0x0000BCB4
		public MatrixFrame GetBoneEntitialFrameWithIndex(sbyte boneIndex)
		{
			MatrixFrame result = default(MatrixFrame);
			EngineApplicationInterface.ISkeleton.GetBoneEntitialFrameWithIndex(base.Pointer, boneIndex, ref result);
			return result;
		}

		// Token: 0x06000C65 RID: 3173 RVA: 0x0000DAE0 File Offset: 0x0000BCE0
		public MatrixFrame GetBoneEntitialFrameWithName(string boneName)
		{
			MatrixFrame result = default(MatrixFrame);
			EngineApplicationInterface.ISkeleton.GetBoneEntitialFrameWithName(base.Pointer, boneName, ref result);
			return result;
		}

		// Token: 0x06000C66 RID: 3174 RVA: 0x0000DB09 File Offset: 0x0000BD09
		public RagdollState GetCurrentRagdollState()
		{
			return EngineApplicationInterface.ISkeleton.GetCurrentRagdollState(base.Pointer);
		}

		// Token: 0x06000C67 RID: 3175 RVA: 0x0000DB1B File Offset: 0x0000BD1B
		public void ActivateRagdoll()
		{
			EngineApplicationInterface.ISkeleton.ActivateRagdoll(base.Pointer);
		}

		// Token: 0x06000C68 RID: 3176 RVA: 0x0000DB2D File Offset: 0x0000BD2D
		public sbyte GetSkeletonBoneMapping(sbyte boneIndex)
		{
			return EngineApplicationInterface.ISkeleton.GetSkeletonBoneMapping(base.Pointer, boneIndex);
		}

		// Token: 0x06000C69 RID: 3177 RVA: 0x0000DB40 File Offset: 0x0000BD40
		public void AddMesh(Mesh mesh)
		{
			EngineApplicationInterface.ISkeleton.AddMesh(base.Pointer, mesh.Pointer);
		}

		// Token: 0x06000C6A RID: 3178 RVA: 0x0000DB58 File Offset: 0x0000BD58
		public void ClearComponents()
		{
			EngineApplicationInterface.ISkeleton.ClearComponents(base.Pointer);
		}

		// Token: 0x06000C6B RID: 3179 RVA: 0x0000DB6A File Offset: 0x0000BD6A
		public void AddComponent(GameEntityComponent component)
		{
			EngineApplicationInterface.ISkeleton.AddComponent(base.Pointer, component.Pointer);
		}

		// Token: 0x06000C6C RID: 3180 RVA: 0x0000DB82 File Offset: 0x0000BD82
		public bool HasComponent(GameEntityComponent component)
		{
			return EngineApplicationInterface.ISkeleton.HasComponent(base.Pointer, component.Pointer);
		}

		// Token: 0x06000C6D RID: 3181 RVA: 0x0000DB9A File Offset: 0x0000BD9A
		public void RemoveComponent(GameEntityComponent component)
		{
			EngineApplicationInterface.ISkeleton.RemoveComponent(base.Pointer, component.Pointer);
		}

		// Token: 0x06000C6E RID: 3182 RVA: 0x0000DBB2 File Offset: 0x0000BDB2
		public void ClearMeshes(bool clearBoneComponents = true)
		{
			EngineApplicationInterface.ISkeleton.ClearMeshes(base.Pointer, clearBoneComponents);
		}

		// Token: 0x06000C6F RID: 3183 RVA: 0x0000DBC5 File Offset: 0x0000BDC5
		public int GetComponentCount(GameEntity.ComponentType componentType)
		{
			return EngineApplicationInterface.ISkeleton.GetComponentCount(base.Pointer, componentType);
		}

		// Token: 0x06000C70 RID: 3184 RVA: 0x0000DBD8 File Offset: 0x0000BDD8
		public void UpdateEntitialFramesFromLocalFrames()
		{
			EngineApplicationInterface.ISkeleton.UpdateEntitialFramesFromLocalFrames(base.Pointer);
		}

		// Token: 0x06000C71 RID: 3185 RVA: 0x0000DBEA File Offset: 0x0000BDEA
		public void ResetFrames()
		{
			EngineApplicationInterface.ISkeleton.ResetFrames(base.Pointer);
		}

		// Token: 0x06000C72 RID: 3186 RVA: 0x0000DBFC File Offset: 0x0000BDFC
		public GameEntityComponent GetComponentAtIndex(GameEntity.ComponentType componentType, int index)
		{
			return EngineApplicationInterface.ISkeleton.GetComponentAtIndex(base.Pointer, componentType, index);
		}

		// Token: 0x06000C73 RID: 3187 RVA: 0x0000DC10 File Offset: 0x0000BE10
		public void SetUsePreciseBoundingVolume(bool value)
		{
			EngineApplicationInterface.ISkeleton.SetUsePreciseBoundingVolume(base.Pointer, value);
		}

		// Token: 0x06000C74 RID: 3188 RVA: 0x0000DC24 File Offset: 0x0000BE24
		public MatrixFrame GetBoneEntitialRestFrame(sbyte boneIndex, bool useBoneMapping)
		{
			MatrixFrame result = default(MatrixFrame);
			EngineApplicationInterface.ISkeleton.GetBoneEntitialRestFrame(base.Pointer, boneIndex, useBoneMapping, ref result);
			return result;
		}

		// Token: 0x06000C75 RID: 3189 RVA: 0x0000DC50 File Offset: 0x0000BE50
		public MatrixFrame GetBoneLocalRestFrame(sbyte boneIndex, bool useBoneMapping = true)
		{
			MatrixFrame result = default(MatrixFrame);
			EngineApplicationInterface.ISkeleton.GetBoneLocalRestFrame(base.Pointer, boneIndex, useBoneMapping, ref result);
			return result;
		}

		// Token: 0x06000C76 RID: 3190 RVA: 0x0000DC7C File Offset: 0x0000BE7C
		public MatrixFrame GetBoneEntitialRestFrame(sbyte boneIndex)
		{
			MatrixFrame result = default(MatrixFrame);
			EngineApplicationInterface.ISkeleton.GetBoneEntitialRestFrame(base.Pointer, boneIndex, true, ref result);
			return result;
		}

		// Token: 0x06000C77 RID: 3191 RVA: 0x0000DCA8 File Offset: 0x0000BEA8
		public MatrixFrame GetBoneEntitialFrameAtChannel(int channelNo, sbyte boneIndex)
		{
			MatrixFrame result = default(MatrixFrame);
			EngineApplicationInterface.ISkeleton.GetBoneEntitialFrameAtChannel(base.Pointer, channelNo, boneIndex, ref result);
			return result;
		}

		// Token: 0x06000C78 RID: 3192 RVA: 0x0000DCD4 File Offset: 0x0000BED4
		public MatrixFrame GetBoneEntitialFrame(sbyte boneIndex)
		{
			MatrixFrame result = default(MatrixFrame);
			EngineApplicationInterface.ISkeleton.GetBoneEntitialFrame(base.Pointer, boneIndex, ref result);
			return result;
		}

		// Token: 0x06000C79 RID: 3193 RVA: 0x0000DCFD File Offset: 0x0000BEFD
		public int GetBoneComponentCount(sbyte boneIndex)
		{
			return EngineApplicationInterface.ISkeleton.GetBoneComponentCount(base.Pointer, boneIndex);
		}

		// Token: 0x06000C7A RID: 3194 RVA: 0x0000DD10 File Offset: 0x0000BF10
		public GameEntityComponent GetBoneComponentAtIndex(sbyte boneIndex, int componentIndex)
		{
			return EngineApplicationInterface.ISkeleton.GetBoneComponentAtIndex(base.Pointer, boneIndex, componentIndex);
		}

		// Token: 0x06000C7B RID: 3195 RVA: 0x0000DD24 File Offset: 0x0000BF24
		public bool HasBoneComponent(sbyte boneIndex, GameEntityComponent component)
		{
			return EngineApplicationInterface.ISkeleton.HasBoneComponent(base.Pointer, boneIndex, component);
		}

		// Token: 0x06000C7C RID: 3196 RVA: 0x0000DD38 File Offset: 0x0000BF38
		public void AddComponentToBone(sbyte boneIndex, GameEntityComponent component)
		{
			EngineApplicationInterface.ISkeleton.AddComponentToBone(base.Pointer, boneIndex, component);
		}

		// Token: 0x06000C7D RID: 3197 RVA: 0x0000DD4C File Offset: 0x0000BF4C
		public void RemoveBoneComponent(sbyte boneIndex, GameEntityComponent component)
		{
			EngineApplicationInterface.ISkeleton.RemoveBoneComponent(base.Pointer, boneIndex, component);
		}

		// Token: 0x06000C7E RID: 3198 RVA: 0x0000DD60 File Offset: 0x0000BF60
		public void ClearMeshesAtBone(sbyte boneIndex)
		{
			EngineApplicationInterface.ISkeleton.ClearMeshesAtBone(base.Pointer, boneIndex);
		}

		// Token: 0x06000C7F RID: 3199 RVA: 0x0000DD73 File Offset: 0x0000BF73
		public void TickAnimations(float dt, MatrixFrame globalFrame, bool tickAnimsForChildren)
		{
			EngineApplicationInterface.ISkeleton.TickAnimations(base.Pointer, ref globalFrame, dt, tickAnimsForChildren);
		}

		// Token: 0x06000C80 RID: 3200 RVA: 0x0000DD89 File Offset: 0x0000BF89
		public void TickAnimationsAndForceUpdate(float dt, MatrixFrame globalFrame, bool tickAnimsForChildren)
		{
			EngineApplicationInterface.ISkeleton.TickAnimationsAndForceUpdate(base.Pointer, ref globalFrame, dt, tickAnimsForChildren);
		}

		// Token: 0x06000C81 RID: 3201 RVA: 0x0000DD9F File Offset: 0x0000BF9F
		public float GetAnimationParameterAtChannel(int channelNo)
		{
			return EngineApplicationInterface.ISkeleton.GetSkeletonAnimationParameterAtChannel(base.Pointer, channelNo);
		}

		// Token: 0x06000C82 RID: 3202 RVA: 0x0000DDB2 File Offset: 0x0000BFB2
		public void SetAnimationParameterAtChannel(int channelNo, float parameter)
		{
			EngineApplicationInterface.ISkeleton.SetSkeletonAnimationParameterAtChannel(base.Pointer, channelNo, parameter);
		}

		// Token: 0x06000C83 RID: 3203 RVA: 0x0000DDC6 File Offset: 0x0000BFC6
		public float GetAnimationSpeedAtChannel(int channelNo)
		{
			return EngineApplicationInterface.ISkeleton.GetSkeletonAnimationSpeedAtChannel(base.Pointer, channelNo);
		}

		// Token: 0x06000C84 RID: 3204 RVA: 0x0000DDD9 File Offset: 0x0000BFD9
		public void SetAnimationSpeedAtChannel(int channelNo, float speed)
		{
			EngineApplicationInterface.ISkeleton.SetSkeletonAnimationSpeedAtChannel(base.Pointer, channelNo, speed);
		}

		// Token: 0x06000C85 RID: 3205 RVA: 0x0000DDED File Offset: 0x0000BFED
		public void SetUptoDate(bool value)
		{
			EngineApplicationInterface.ISkeleton.SetSkeletonUptoDate(base.Pointer, value);
		}

		// Token: 0x06000C86 RID: 3206 RVA: 0x0000DE00 File Offset: 0x0000C000
		public string GetAnimationAtChannel(int channelNo)
		{
			return EngineApplicationInterface.ISkeleton.GetAnimationAtChannel(base.Pointer, channelNo);
		}

		// Token: 0x06000C87 RID: 3207 RVA: 0x0000DE13 File Offset: 0x0000C013
		public int GetAnimationIndexAtChannel(int channelNo)
		{
			return EngineApplicationInterface.ISkeleton.GetAnimationIndexAtChannel(base.Pointer, channelNo);
		}

		// Token: 0x06000C88 RID: 3208 RVA: 0x0000DE26 File Offset: 0x0000C026
		public void EnableScriptDrivenPostIntegrateCallback()
		{
			EngineApplicationInterface.ISkeleton.EnableScriptDrivenPostIntegrateCallback(base.Pointer);
		}

		// Token: 0x06000C89 RID: 3209 RVA: 0x0000DE38 File Offset: 0x0000C038
		public void ResetCloths()
		{
			EngineApplicationInterface.ISkeleton.ResetCloths(base.Pointer);
		}

		// Token: 0x06000C8A RID: 3210 RVA: 0x0000DE4A File Offset: 0x0000C04A
		public IEnumerable<Mesh> GetAllMeshes()
		{
			NativeObjectArray nativeObjectArray = NativeObjectArray.Create();
			EngineApplicationInterface.ISkeleton.GetAllMeshes(this, nativeObjectArray);
			foreach (NativeObject nativeObject in ((IEnumerable<NativeObject>)nativeObjectArray))
			{
				Mesh mesh = (Mesh)nativeObject;
				yield return mesh;
			}
			IEnumerator<NativeObject> enumerator = null;
			yield break;
			yield break;
		}

		// Token: 0x06000C8B RID: 3211 RVA: 0x0000DE5A File Offset: 0x0000C05A
		public static sbyte GetBoneIndexFromName(string skeletonModelName, string boneName)
		{
			return EngineApplicationInterface.ISkeleton.GetBoneIndexFromName(skeletonModelName, boneName);
		}

		// Token: 0x06000C8C RID: 3212 RVA: 0x0000DE68 File Offset: 0x0000C068
		internal Transformation GetEntitialOutTransform(UIntPtr animResultPointer, sbyte boneIndex)
		{
			return EngineApplicationInterface.ISkeleton.GetEntitialOutTransform(base.Pointer, animResultPointer, boneIndex);
		}

		// Token: 0x06000C8D RID: 3213 RVA: 0x0000DE7C File Offset: 0x0000C07C
		internal void SetOutBoneDisplacement(UIntPtr animResultPointer, sbyte boneIndex, Vec3 displacement)
		{
			EngineApplicationInterface.ISkeleton.SetOutBoneDisplacement(base.Pointer, animResultPointer, boneIndex, displacement);
		}

		// Token: 0x06000C8E RID: 3214 RVA: 0x0000DE91 File Offset: 0x0000C091
		internal void SetOutQuat(UIntPtr animResultPointer, sbyte boneIndex, Mat3 rotation)
		{
			EngineApplicationInterface.ISkeleton.SetOutQuat(base.Pointer, animResultPointer, boneIndex, rotation);
		}

		// Token: 0x040001C2 RID: 450
		public const sbyte MaxBoneCount = 64;
	}
}
