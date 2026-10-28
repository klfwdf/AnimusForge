using TaleWorlds.Core;
using TaleWorlds.Library;

namespace TaleWorlds.MountAndBlade;

public class FireMangonel : Mangonel
{
	public override SiegeEngineType GetSiegeEngineType()
	{
		if (DefaultSide != BattleSideEnum.Attacker)
		{
			return DefaultSiegeEngineTypes.FireCatapult;
		}
		return DefaultSiegeEngineTypes.FireOnager;
	}

	public override float ProcessTargetValue(float baseValue, TargetFlags flags)
	{
		if (flags.HasAnyFlag(TargetFlags.NotAThreat))
		{
			return -1000f;
		}
		if (flags.HasAllFlags(TargetFlags.IsFlammable | TargetFlags.IsSiegeEngine))
		{
			baseValue *= 1.5f;
		}
		if (flags.HasAnyFlag(TargetFlags.IsSiegeEngine))
		{
			baseValue *= 12f;
		}
		if (flags.HasAnyFlag(TargetFlags.IsStructure))
		{
			baseValue *= 1.5f;
		}
		if (flags.HasAnyFlag(TargetFlags.IsSmall))
		{
			baseValue *= 8f;
		}
		if (flags.HasAnyFlag(TargetFlags.IsMoving))
		{
			baseValue *= 12f;
		}
		if (flags.HasAnyFlag(TargetFlags.DebugThreat))
		{
			baseValue *= 10f;
		}
		if (flags.HasAnyFlag(TargetFlags.IsSiegeTower))
		{
			baseValue *= 12f;
		}
		if (flags.HasAnyFlag(TargetFlags.IsFlammable))
		{
			baseValue *= 100f;
		}
		return baseValue;
	}
}
