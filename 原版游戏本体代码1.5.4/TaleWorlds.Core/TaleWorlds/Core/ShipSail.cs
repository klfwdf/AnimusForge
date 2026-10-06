using TaleWorlds.Library;

namespace TaleWorlds.Core;

public class ShipSail
{
	public readonly SailType Type;

	public readonly float ForceMultiplier;

	public readonly float LeftRotationLimit;

	public readonly float RightRotationLimit;

	public readonly float RotationRate;

	public ShipSail(SailType type, float forceMultiplier, float leftRotationLimit, float rightRotationLimit, float rotationRate)
	{
		Type = type;
		ForceMultiplier = forceMultiplier;
		LeftRotationLimit = leftRotationLimit;
		RightRotationLimit = rightRotationLimit;
		RotationRate = rotationRate;
	}

	public bool NearlyEquals(ShipSail otherShipSail)
	{
		if (Type == otherShipSail.Type && ForceMultiplier.ApproximatelyEqualsTo(otherShipSail.ForceMultiplier) && LeftRotationLimit.ApproximatelyEqualsTo(otherShipSail.LeftRotationLimit) && RightRotationLimit.ApproximatelyEqualsTo(otherShipSail.RightRotationLimit))
		{
			return RotationRate.ApproximatelyEqualsTo(otherShipSail.RotationRate);
		}
		return false;
	}
}
