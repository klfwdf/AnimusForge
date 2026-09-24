using System;

namespace AnimusForge;

internal static class KingdomStabilityPolicy
{
	internal const int KingdomStabilityMinValue = 0;
	internal const int KingdomStabilityMaxValue = 100;
	internal const int KingdomStabilityDefaultValue = 50;
	internal enum KingdomStabilityTier { ExtremelyPoor, VeryPoor, Poor, Average, FairlyHigh, High, ExtremelyHigh }

	internal static int ClampKingdomStabilityValue(int value)
	{
		return Math.Max(KingdomStabilityMinValue, Math.Min(KingdomStabilityMaxValue, value));
	}

	internal static int GetKingdomStabilityRelationTargetOffset(int stabilityValue)
	{
		return Math.Max(-25, Math.Min(25, (ClampKingdomStabilityValue(stabilityValue) - KingdomStabilityDefaultValue) / 2));
	}

	internal static KingdomStabilityTier GetKingdomStabilityTier(int value)
	{
		int num = ClampKingdomStabilityValue(value);
		if (num >= 90)
		{
			return KingdomStabilityTier.ExtremelyHigh;
		}
		if (num >= 75)
		{
			return KingdomStabilityTier.High;
		}
		if (num >= 60)
		{
			return KingdomStabilityTier.FairlyHigh;
		}
		if (num >= 40)
		{
			return KingdomStabilityTier.Average;
		}
		if (num >= 25)
		{
			return KingdomStabilityTier.Poor;
		}
		if (num >= 10)
		{
			return KingdomStabilityTier.VeryPoor;
		}
		return KingdomStabilityTier.ExtremelyPoor;
	}

	internal static int GetLowClanCountRoyalDomainLoyaltyAdjustment(int stabilityValue, int activeClanCount)
	{
		int num = Math.Max(0, activeClanCount);
		switch (GetKingdomStabilityTier(stabilityValue))
		{
		case KingdomStabilityTier.ExtremelyHigh:
			switch (num)
			{
			case 0:
				return 9;
			case 1:
				return 6;
			case 2:
				return 3;
			case 3:
				return 2;
			case 4:
				return 1;
			default:
				return 0;
			}
		case KingdomStabilityTier.High:
			switch (num)
			{
			case 0:
				return 6;
			case 1:
				return 4;
			case 2:
				return 2;
			case 3:
				return 1;
			default:
				return 0;
			}
		case KingdomStabilityTier.FairlyHigh:
			switch (num)
			{
			case 0:
				return 3;
			case 1:
				return 2;
			case 2:
				return 1;
			default:
				return 0;
			}
		case KingdomStabilityTier.Poor:
			switch (num)
			{
			case 0:
				return -3;
			case 1:
				return -2;
			case 2:
				return -1;
			default:
				return 0;
			}
		case KingdomStabilityTier.VeryPoor:
			switch (num)
			{
			case 0:
				return -6;
			case 1:
				return -4;
			case 2:
				return -2;
			case 3:
				return -1;
			default:
				return 0;
			}
		case KingdomStabilityTier.ExtremelyPoor:
			switch (num)
			{
			case 0:
				return -9;
			case 1:
				return -6;
			case 2:
				return -3;
			case 3:
				return -2;
			case 4:
				return -1;
			default:
				return 0;
			}
		default:
			return 0;
		}
	}

	internal static float GetKingdomRebellionWeeklyChance(int stabilityValue)
	{
		switch (GetKingdomStabilityTier(stabilityValue))
		{
		case KingdomStabilityTier.Poor:
			return 0.005f;
		case KingdomStabilityTier.VeryPoor:
			return 0.05f;
		case KingdomStabilityTier.ExtremelyPoor:
			return 0.25f;
		default:
			return 0f;
		}
	}

	internal static int GetKingdomStabilityWeeklyBalancingDelta(int stabilityValue)
	{
		switch (GetKingdomStabilityTier(stabilityValue))
		{
		case KingdomStabilityTier.ExtremelyHigh:
			return -5;
		case KingdomStabilityTier.High:
			return -3;
		case KingdomStabilityTier.VeryPoor:
			return 3;
		case KingdomStabilityTier.ExtremelyPoor:
			return 5;
		default:
			return 0;
		}
	}

}
