using System;
using TaleWorlds.DotNet;
using TaleWorlds.Library;

namespace TaleWorlds.MountAndBlade;

[Serializable]
[EngineStruct("Animation_system_data_quadruped", false, null)]
public struct AnimationSystemDataQuadruped
{
	public Vec3 ReinHandleLeftLocalPosition;

	public Vec3 ReinHandleRightLocalPosition;

	public string ReinSkeleton;

	public string ReinCollisionBody;

	public sbyte IndexOfBoneToDetectGroundSlopeFront;

	public sbyte IndexOfBoneToDetectGroundSlopeBack;

	public AnimationSystemBoneDataQuadruped Bones;
}
