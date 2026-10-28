using TaleWorlds.Library;

namespace NavalDLC.Missions.NavalPhysics;

public struct ShipForce
{
	public enum SourceType
	{
		None,
		Sail,
		Oar,
		Rudder
	}

	public readonly Vec3 LocalPosition;

	public Vec3 Force;

	public readonly SourceType Source;

	public readonly float GamifiedForceMultiplier;

	public bool IsApplicable
	{
		get
		{
			if (Force.IsValid)
			{
				return Force.IsNonZero;
			}
			return false;
		}
	}

	public ShipForce(in Vec3 localPosition, in Vec3 force, SourceType source, float gamifiedForceMultiplier)
	{
		LocalPosition = localPosition;
		Force = new Vec3(force, 0f);
		Source = source;
		GamifiedForceMultiplier = gamifiedForceMultiplier;
	}

	public ShipForce(SourceType source)
	{
		LocalPosition = Vec3.Zero;
		Force = Vec3.Zero;
		Source = source;
		GamifiedForceMultiplier = 1f;
	}

	public void ComputeRealisticAndGamifiedForceComponents(out Vec3 realisticForce, out Vec3 gamifiedForce)
	{
		realisticForce = Force / GamifiedForceMultiplier;
		gamifiedForce = realisticForce * (GamifiedForceMultiplier - 1f);
	}

	public static ShipForce None()
	{
		return new ShipForce(SourceType.None);
	}

	public static ShipForce None(SourceType source)
	{
		return new ShipForce(source);
	}
}
