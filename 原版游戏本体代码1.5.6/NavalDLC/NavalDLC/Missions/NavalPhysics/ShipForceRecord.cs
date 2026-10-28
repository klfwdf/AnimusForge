using TaleWorlds.Library;

namespace NavalDLC.Missions.NavalPhysics;

public struct ShipForceRecord
{
	public readonly MBReadOnlyList<ShipForce> LeftOarForces;

	public readonly MBReadOnlyList<ShipForce> RightOarForces;

	public readonly MBReadOnlyList<ShipForce> SailForces;

	public readonly ShipForce RudderForce;

	public bool HasLeftOarForces
	{
		get
		{
			if (LeftOarForces != null)
			{
				return LeftOarForces.Count > 0;
			}
			return false;
		}
	}

	public bool HasRightOarForces
	{
		get
		{
			if (RightOarForces != null)
			{
				return RightOarForces.Count > 0;
			}
			return false;
		}
	}

	public bool HasSailForces
	{
		get
		{
			if (SailForces != null)
			{
				return SailForces.Count > 0;
			}
			return false;
		}
	}

	public ShipForceRecord(MBReadOnlyList<ShipForce> leftOarForces, MBReadOnlyList<ShipForce> rightOarForces, in MBReadOnlyList<ShipForce> sailForces, in ShipForce rudderForce)
	{
		LeftOarForces = leftOarForces;
		RightOarForces = rightOarForces;
		SailForces = sailForces;
		RudderForce = rudderForce;
	}

	public static ShipForceRecord None()
	{
		return new ShipForceRecord(null, null, (MBReadOnlyList<ShipForce>)null, ShipForce.None(ShipForce.SourceType.Rudder));
	}
}
