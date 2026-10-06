using System;
using TaleWorlds.DotNet;

namespace TaleWorlds.MountAndBlade;

[Flags]
[EngineStruct("Combat_hit_result_flags", true, "chrf", false)]
public enum CombatHitResultFlags : byte
{
	NormalHit = 0,
	HitWithStartOfTheAnimation = 1,
	HitWithArm = 2,
	HitWithBackOfTheWeapon = 4
}
