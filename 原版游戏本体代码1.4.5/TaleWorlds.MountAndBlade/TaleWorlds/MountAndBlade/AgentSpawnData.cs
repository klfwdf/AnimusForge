using TaleWorlds.DotNet;
using TaleWorlds.Library;

namespace TaleWorlds.MountAndBlade;

[EngineStruct("Agent_spawn_data", false, null)]
public struct AgentSpawnData
{
	public int HitPoints;

	public int MonsterUsageIndex;

	public int Weight;

	public float StandingChestHeight;

	public float StandingPelvisHeight;

	public float StandingEyeHeight;

	public float CrouchEyeHeight;

	public float MountedEyeHeight;

	public float RiderEyeHeightAdder;

	public float JumpAcceleration;

	public Vec3 EyeOffsetWrtHead;

	public Vec3 FirstPersonCameraOffsetWrtHead;

	public float RiderCameraHeightAdder;

	public float RiderBodyCapsuleHeightAdder;

	public float RiderBodyCapsuleForwardAdder;

	public float ArmLength;

	public float ArmWeight;

	public float JumpSpeedLimit;

	public float RelativeSpeedLimitForCharge;
}
