using TaleWorlds.DotNet;

namespace TaleWorlds.MountAndBlade;

[EngineStruct("Melee_collision_reaction", true, "mcr", false)]
public enum MeleeCollisionReaction
{
	Invalid = -1,
	SlicedThrough,
	ContinueChecking,
	Stuck,
	Bounced,
	Staggered
}
