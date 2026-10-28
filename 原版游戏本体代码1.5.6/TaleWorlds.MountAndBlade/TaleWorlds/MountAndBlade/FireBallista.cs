using TaleWorlds.Core;
using TaleWorlds.Library;

namespace TaleWorlds.MountAndBlade;

public class FireBallista : Ballista
{
	public override SiegeEngineType GetSiegeEngineType()
	{
		return DefaultSiegeEngineTypes.FireBallista;
	}

	public override float ProcessTargetValue(float baseValue, TargetFlags flags)
	{
		if (flags.HasAnyFlag(TargetFlags.NotAThreat))
		{
			return -1000f;
		}
		if (flags.HasAllFlags(TargetFlags.IsSiegeEngine | TargetFlags.IsAttacker))
		{
			baseValue *= 1.5f;
		}
		if (flags.HasAnyFlag(TargetFlags.IsSiegeEngine))
		{
			baseValue *= 2.5f;
		}
		if (flags.HasAnyFlag(TargetFlags.IsStructure))
		{
			baseValue *= 0.1f;
		}
		if (flags.HasAnyFlag(TargetFlags.IsFlammable))
		{
			baseValue *= 2f;
		}
		if (flags.HasAnyFlag(TargetFlags.DebugThreat))
		{
			baseValue *= 1000f;
		}
		return baseValue;
	}
}
