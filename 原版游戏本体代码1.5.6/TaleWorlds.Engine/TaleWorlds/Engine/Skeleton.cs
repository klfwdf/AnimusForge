using System;
using System.Collections.Generic;
using TaleWorlds.DotNet;
using TaleWorlds.Library;

namespace TaleWorlds.Engine;

[EngineClass("rglSkeleton")]
public sealed class Skeleton : NativeObject
{
	public const sbyte MaxBoneCount = 64;

	public bool IsValid => base.Pointer != UIntPtr.Zero;

	internal Skeleton(UIntPtr pointer)
	{
		Construct(pointer);
	}

	public static Skeleton CreateFromModel(string modelName)
	{
		return EngineApplicationInterface.ISkeleton.CreateFromModel(modelName);
	}

	public static Skeleton CreateFromModelWithNullAnimTree(GameEntity entity, string modelName, float boneScale = 1f)
	{
		return EngineApplicationInterface.ISkeleton.CreateFromModelWithNullAnimTree(entity.Pointer, modelName, boneScale);
	}

	public string GetName()
	{
		return EngineApplicationInterface.ISkeleton.GetName(this);
	}

	public string GetBoneName(sbyte boneIndex)
	{
		return EngineApplicationInterface.ISkeleton.GetBoneName(this, boneIndex);
	}

	public sbyte GetBoneChildAtIndex(sbyte boneIndex, sbyte childIndex)
	{
		return EngineApplicationInterface.ISkeleton.GetBoneChildAtIndex(this, boneIndex, childIndex);
	}

	public sbyte GetBoneChildCount(sbyte boneIndex)
	{
		return EngineApplicationInterface.ISkeleton.GetBoneChildCount(this, boneIndex);
	}

	public sbyte GetParentBoneIndex(sbyte boneIndex)
	{
		return EngineApplicationInterface.ISkeleton.GetParentBoneIndex(this, boneIndex);
	}

	public void AddMeshToBone(UIntPtr mesh, sbyte boneIndex)
	{
		EngineApplicationInterface.ISkeleton.AddMeshToBone(base.Pointer, mesh, boneIndex);
	}

	public void Freeze(bool p)
	{
		EngineApplicationInterface.ISkeleton.Freeze(base.Pointer, p);
	}

	public bool IsFrozen()
	{
		return EngineApplicationInterface.ISkeleton.IsFrozen(base.Pointer);
	}

	public void SetBoneLocalFrame(sbyte boneIndex, MatrixFrame localFrame)
	{
		EngineApplicationInterface.ISkeleton.SetBoneLocalFrame(base.Pointer, boneIndex, ref localFrame);
	}

	public sbyte GetBoneCount()
	{
		return EngineApplicationInterface.ISkeleton.GetBoneCount(base.Pointer);
	}

	public void GetBoneBody(sbyte boneIndex, ref CapsuleData data)
	{
		EngineApplicationInterface.ISkeleton.GetBoneBody(base.Pointer, boneIndex, ref data);
	}

	public static bool SkeletonModelExist(string skeletonModelName)
	{
		return EngineApplicationInterface.ISkeleton.SkeletonModelExist(skeletonModelName);
	}

	public void ForceUpdateBoneFrames()
	{
		EngineApplicationInterface.ISkeleton.ForceUpdateBoneFrames(base.Pointer);
	}

	public MatrixFrame GetBoneEntitialFrameWithIndex(sbyte boneIndex)
	{
		MatrixFrame outEntitialFrame = default(MatrixFrame);
		EngineApplicationInterface.ISkeleton.GetBoneEntitialFrameWithIndex(base.Pointer, boneIndex, ref outEntitialFrame);
		return outEntitialFrame;
	}

	public MatrixFrame GetBoneEntitialFrameWithName(string boneName)
	{
		MatrixFrame outEntitialFrame = default(MatrixFrame);
		EngineApplicationInterface.ISkeleton.GetBoneEntitialFrameWithName(base.Pointer, boneName, ref outEntitialFrame);
		return outEntitialFrame;
	}

	public RagdollState GetCurrentRagdollState()
	{
		return EngineApplicationInterface.ISkeleton.GetCurrentRagdollState(base.Pointer);
	}

	public void ActivateRagdoll()
	{
		EngineApplicationInterface.ISkeleton.ActivateRagdoll(base.Pointer);
	}

	public sbyte GetSkeletonBoneMapping(sbyte boneIndex)
	{
		return EngineApplicationInterface.ISkeleton.GetSkeletonBoneMapping(base.Pointer, boneIndex);
	}

	public void AddMesh(Mesh mesh)
	{
		EngineApplicationInterface.ISkeleton.AddMesh(base.Pointer, mesh.Pointer);
	}

	public void ClearComponents()
	{
		EngineApplicationInterface.ISkeleton.ClearComponents(base.Pointer);
	}

	public void AddComponent(GameEntityComponent component)
	{
		EngineApplicationInterface.ISkeleton.AddComponent(base.Pointer, component.Pointer);
	}

	public bool HasComponent(GameEntityComponent component)
	{
		return EngineApplicationInterface.ISkeleton.HasComponent(base.Pointer, component.Pointer);
	}

	public void RemoveComponent(GameEntityComponent component)
	{
		EngineApplicationInterface.ISkeleton.RemoveComponent(base.Pointer, component.Pointer);
	}

	public void ClearMeshes(bool clearBoneComponents = true)
	{
		EngineApplicationInterface.ISkeleton.ClearMeshes(base.Pointer, clearBoneComponents);
	}

	public int GetComponentCount(GameEntity.ComponentType componentType)
	{
		return EngineApplicationInterface.ISkeleton.GetComponentCount(base.Pointer, componentType);
	}

	public void UpdateEntitialFramesFromLocalFrames()
	{
		EngineApplicationInterface.ISkeleton.UpdateEntitialFramesFromLocalFrames(base.Pointer);
	}

	public void ResetFrames()
	{
		EngineApplicationInterface.ISkeleton.ResetFrames(base.Pointer);
	}

	public GameEntityComponent GetComponentAtIndex(GameEntity.ComponentType componentType, int index)
	{
		return EngineApplicationInterface.ISkeleton.GetComponentAtIndex(base.Pointer, componentType, index);
	}

	public void SetUsePreciseBoundingVolume(bool value)
	{
		EngineApplicationInterface.ISkeleton.SetUsePreciseBoundingVolume(base.Pointer, value);
	}

	public MatrixFrame GetBoneEntitialRestFrame(sbyte boneIndex, bool useBoneMapping)
	{
		MatrixFrame outFrame = default(MatrixFrame);
		EngineApplicationInterface.ISkeleton.GetBoneEntitialRestFrame(base.Pointer, boneIndex, useBoneMapping, ref outFrame);
		return outFrame;
	}

	public MatrixFrame GetBoneLocalRestFrame(sbyte boneIndex, bool useBoneMapping = true)
	{
		MatrixFrame outFrame = default(MatrixFrame);
		EngineApplicationInterface.ISkeleton.GetBoneLocalRestFrame(base.Pointer, boneIndex, useBoneMapping, ref outFrame);
		return outFrame;
	}

	public MatrixFrame GetBoneEntitialRestFrame(sbyte boneIndex)
	{
		MatrixFrame outFrame = default(MatrixFrame);
		EngineApplicationInterface.ISkeleton.GetBoneEntitialRestFrame(base.Pointer, boneIndex, useBoneMapping: true, ref outFrame);
		return outFrame;
	}

	public MatrixFrame GetBoneEntitialFrameAtChannel(int channelNo, sbyte boneIndex)
	{
		MatrixFrame outFrame = default(MatrixFrame);
		EngineApplicationInterface.ISkeleton.GetBoneEntitialFrameAtChannel(base.Pointer, channelNo, boneIndex, ref outFrame);
		return outFrame;
	}

	public MatrixFrame GetBoneEntitialFrame(sbyte boneIndex)
	{
		MatrixFrame outFrame = default(MatrixFrame);
		EngineApplicationInterface.ISkeleton.GetBoneEntitialFrame(base.Pointer, boneIndex, ref outFrame);
		return outFrame;
	}

	public int GetBoneComponentCount(sbyte boneIndex)
	{
		return EngineApplicationInterface.ISkeleton.GetBoneComponentCount(base.Pointer, boneIndex);
	}

	public GameEntityComponent GetBoneComponentAtIndex(sbyte boneIndex, int componentIndex)
	{
		return EngineApplicationInterface.ISkeleton.GetBoneComponentAtIndex(base.Pointer, boneIndex, componentIndex);
	}

	public bool HasBoneComponent(sbyte boneIndex, GameEntityComponent component)
	{
		return EngineApplicationInterface.ISkeleton.HasBoneComponent(base.Pointer, boneIndex, component);
	}

	public void AddComponentToBone(sbyte boneIndex, GameEntityComponent component)
	{
		EngineApplicationInterface.ISkeleton.AddComponentToBone(base.Pointer, boneIndex, component);
	}

	public void RemoveBoneComponent(sbyte boneIndex, GameEntityComponent component)
	{
		EngineApplicationInterface.ISkeleton.RemoveBoneComponent(base.Pointer, boneIndex, component);
	}

	public void ClearMeshesAtBone(sbyte boneIndex)
	{
		EngineApplicationInterface.ISkeleton.ClearMeshesAtBone(base.Pointer, boneIndex);
	}

	public void TickAnimations(float dt, MatrixFrame globalFrame, bool tickAnimsForChildren)
	{
		EngineApplicationInterface.ISkeleton.TickAnimations(base.Pointer, ref globalFrame, dt, tickAnimsForChildren);
	}

	public void TickAnimationsAndForceUpdate(float dt, MatrixFrame globalFrame, bool tickAnimsForChildren)
	{
		EngineApplicationInterface.ISkeleton.TickAnimationsAndForceUpdate(base.Pointer, ref globalFrame, dt, tickAnimsForChildren);
	}

	public float GetAnimationParameterAtChannel(int channelNo)
	{
		return EngineApplicationInterface.ISkeleton.GetSkeletonAnimationParameterAtChannel(base.Pointer, channelNo);
	}

	public void SetAnimationParameterAtChannel(int channelNo, float parameter)
	{
		EngineApplicationInterface.ISkeleton.SetSkeletonAnimationParameterAtChannel(base.Pointer, channelNo, parameter);
	}

	public float GetAnimationSpeedAtChannel(int channelNo)
	{
		return EngineApplicationInterface.ISkeleton.GetSkeletonAnimationSpeedAtChannel(base.Pointer, channelNo);
	}

	public void SetAnimationSpeedAtChannel(int channelNo, float speed)
	{
		EngineApplicationInterface.ISkeleton.SetSkeletonAnimationSpeedAtChannel(base.Pointer, channelNo, speed);
	}

	public void SetUptoDate(bool value)
	{
		EngineApplicationInterface.ISkeleton.SetSkeletonUptoDate(base.Pointer, value);
	}

	public string GetAnimationAtChannel(int channelNo)
	{
		return EngineApplicationInterface.ISkeleton.GetAnimationAtChannel(base.Pointer, channelNo);
	}

	public int GetAnimationIndexAtChannel(int channelNo)
	{
		return EngineApplicationInterface.ISkeleton.GetAnimationIndexAtChannel(base.Pointer, channelNo);
	}

	public void EnableScriptDrivenPostIntegrateCallback()
	{
		EngineApplicationInterface.ISkeleton.EnableScriptDrivenPostIntegrateCallback(base.Pointer);
	}

	public void ResetCloths()
	{
		EngineApplicationInterface.ISkeleton.ResetCloths(base.Pointer);
	}

	public IEnumerable<Mesh> GetAllMeshes()
	{
		NativeObjectArray nativeObjectArray = NativeObjectArray.Create();
		EngineApplicationInterface.ISkeleton.GetAllMeshes(this, nativeObjectArray);
		foreach (Mesh item in (IEnumerable<NativeObject>)nativeObjectArray)
		{
			yield return item;
		}
	}

	public static sbyte GetBoneIndexFromName(string skeletonModelName, string boneName)
	{
		return EngineApplicationInterface.ISkeleton.GetBoneIndexFromName(skeletonModelName, boneName);
	}

	internal Transformation GetEntitialOutTransform(UIntPtr animResultPointer, sbyte boneIndex)
	{
		return EngineApplicationInterface.ISkeleton.GetEntitialOutTransform(base.Pointer, animResultPointer, boneIndex);
	}

	internal void SetOutBoneDisplacement(UIntPtr animResultPointer, sbyte boneIndex, Vec3 displacement)
	{
		EngineApplicationInterface.ISkeleton.SetOutBoneDisplacement(base.Pointer, animResultPointer, boneIndex, displacement);
	}

	internal void SetOutQuat(UIntPtr animResultPointer, sbyte boneIndex, Mat3 rotation)
	{
		EngineApplicationInterface.ISkeleton.SetOutQuat(base.Pointer, animResultPointer, boneIndex, rotation);
	}
}
