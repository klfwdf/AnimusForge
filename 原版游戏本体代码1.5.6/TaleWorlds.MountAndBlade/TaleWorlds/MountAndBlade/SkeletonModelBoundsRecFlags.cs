using TaleWorlds.DotNet;

namespace TaleWorlds.MountAndBlade;

[EngineStruct("Skeleton_model_bounds_rec_flags", true, "smbrf", false)]
public enum SkeletonModelBoundsRecFlags : sbyte
{
	None = 0,
	UseSmallerRadiusMultWhileHoldingShield = 1,
	Sweep = 2,
	DoNotScaleAccordingToAgentScale = 4
}
