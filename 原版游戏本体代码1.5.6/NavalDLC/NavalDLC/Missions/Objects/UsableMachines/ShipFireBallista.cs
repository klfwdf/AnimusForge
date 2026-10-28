using TaleWorlds.Core;
using TaleWorlds.Library;
using TaleWorlds.MountAndBlade;

namespace NavalDLC.Missions.Objects.UsableMachines;

public class ShipFireBallista : ShipBallista
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
		if (flags.HasAnyFlag(TargetFlags.IsShip))
		{
			baseValue *= 2f;
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
