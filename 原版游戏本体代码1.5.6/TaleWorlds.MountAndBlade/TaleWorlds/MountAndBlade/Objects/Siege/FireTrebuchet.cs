using TaleWorlds.Core;
using TaleWorlds.Library;

namespace TaleWorlds.MountAndBlade.Objects.Siege;

public class FireTrebuchet : Trebuchet
{
	public override SiegeEngineType GetSiegeEngineType()
	{
		return DefaultSiegeEngineTypes.FireTrebuchet;
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
			baseValue *= 2f;
		}
		if (flags.HasAnyFlag(TargetFlags.IsStructure))
		{
			baseValue *= 1.5f;
		}
		if (flags.HasAnyFlag(TargetFlags.IsSmall))
		{
			baseValue *= 0.5f;
		}
		if (flags.HasAnyFlag(TargetFlags.IsMoving))
		{
			baseValue *= 0.8f;
		}
		if (flags.HasAnyFlag(TargetFlags.DebugThreat))
		{
			baseValue *= 10000f;
		}
		return baseValue;
	}
}
