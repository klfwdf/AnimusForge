using System.Diagnostics;
using TaleWorlds.Library;

namespace TaleWorlds.MountAndBlade;

public class Threat
{
	public ITargetable TargetableObject;

	public Formation Formation;

	public Agent Agent;

	public float ThreatValue;

	public bool ForceTarget;

	public string Name
	{
		get
		{
			if (TargetableObject != null)
			{
				return TargetableObject.Entity().Name;
			}
			if (Agent != null)
			{
				return Agent.Name.ToString();
			}
			if (Formation != null)
			{
				return Formation.ToString();
			}
			TaleWorlds.Library.Debug.FailedAssert("Invalid threat", "C:\\BuildAgent\\work\\mb3\\Source\\Bannerlord\\TaleWorlds.MountAndBlade\\AI\\Threat.cs", "Name", 39);
			return "Invalid";
		}
	}

	public Vec3 TargetingPosition
	{
		get
		{
			if (TargetableObject != null)
			{
				(Vec3, Vec3) tuple = TargetableObject.ComputeGlobalPhysicsBoundingBoxMinMax();
				var (vec, _) = tuple;
				return (tuple.Item2 + vec) * 0.5f + TargetableObject.GetTargetingOffset();
			}
			if (Agent != null)
			{
				return Agent.CollisionCapsuleCenter;
			}
			if (Formation != null)
			{
				return Formation.GetMedianAgent(excludeDetachedUnits: false, excludePlayer: false, Formation.GetAveragePositionOfUnits(excludeDetachedUnits: false, excludePlayer: false)).Position;
			}
			TaleWorlds.Library.Debug.FailedAssert("Invalid threat", "C:\\BuildAgent\\work\\mb3\\Source\\Bannerlord\\TaleWorlds.MountAndBlade\\AI\\Threat.cs", "TargetingPosition", 64);
			return Vec3.Invalid;
		}
	}

	public override int GetHashCode()
	{
		return base.GetHashCode();
	}

	public (Vec3, Vec3) ComputeGlobalTargetingBoundingBoxMinMax()
	{
		if (TargetableObject != null)
		{
			var (vec, vec2) = TargetableObject.ComputeGlobalPhysicsBoundingBoxMinMax();
			return (vec + TargetableObject.GetTargetingOffset(), vec2 + TargetableObject.GetTargetingOffset());
		}
		if (Agent != null)
		{
			return Agent.CollisionCapsule.GetBoxMinMax();
		}
		if (Formation != null)
		{
			TaleWorlds.Library.Debug.FailedAssert("Nobody should be requesting a bounding box for a formation", "C:\\BuildAgent\\work\\mb3\\Source\\Bannerlord\\TaleWorlds.MountAndBlade\\AI\\Threat.cs", "ComputeGlobalTargetingBoundingBoxMinMax", 83);
			return (Vec3.Invalid, Vec3.Invalid);
		}
		return (Vec3.Invalid, Vec3.Invalid);
	}

	public Vec3 GetGlobalVelocity()
	{
		if (TargetableObject != null)
		{
			return TargetableObject.GetTargetGlobalVelocity();
		}
		if (Agent != null)
		{
			return new Vec3(Agent.GetAverageRealGlobalVelocity().AsVec2);
		}
		return Vec3.Zero;
	}

	public override bool Equals(object obj)
	{
		if (obj is Threat threat)
		{
			if (TargetableObject == threat.TargetableObject)
			{
				return Formation == threat.Formation;
			}
			return false;
		}
		return false;
	}

	[Conditional("DEBUG")]
	public void DisplayDebugInfo()
	{
	}
}
