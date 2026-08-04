using System;

namespace AnimusForge;

internal static class VassalPolicyRules
{
	public const int IndependenceMinimum = 0;
	public const int IndependenceMaximum = 100;
	public const int InitialIndependence = 30;
	public const int InitialObedience = IndependenceMaximum - InitialIndependence;
	public const int RulerRelationMinimum = -100;
	public const int RulerRelationMaximum = 100;
	public const int BreakawayThresholdMinimum = 60;
	public const int BreakawayThresholdAtNeutralRelation = 80;
	public const int BreakawayThresholdMaximum = 100;
	public const int QualityDeltaMinimum = -15;
	public const int QualityDeltaMaximum = 15;
	public const int PublicationCostMinimum = 5;
	public const int PublicationCostMaximumInclusive = 10;

	public static int IndependenceFromObedience(int obedience)
	{
		return IndependenceMaximum - ClampPercent(obedience);
	}

	public static int ObedienceFromIndependence(int independence)
	{
		return IndependenceMaximum - ClampPercent(independence);
	}

	public static int NormalizeQualityDelta(float value)
	{
		if (float.IsNaN(value) || float.IsInfinity(value))
		{
			return 0;
		}
		int rounded = (int)Math.Round(value, MidpointRounding.AwayFromZero);
		return Math.Max(QualityDeltaMinimum, Math.Min(QualityDeltaMaximum, rounded));
	}

	public static int ApplyIndependenceChange(int currentIndependence, int publicationCost, int qualityDelta)
	{
		int current = ClampPercent(currentIndependence);
		int cost = Math.Max(0, publicationCost);
		int quality = Math.Max(QualityDeltaMinimum, Math.Min(QualityDeltaMaximum, qualityDelta));
		return ClampPercent(current + cost + quality);
	}

	public static int CalculateBreakawayThreshold(int rulerRelation)
	{
		int relation = Math.Max(RulerRelationMinimum, Math.Min(RulerRelationMaximum, rulerRelation));
		double threshold = BreakawayThresholdAtNeutralRelation + relation * 0.2d;
		int rounded = (int)Math.Round(threshold, MidpointRounding.AwayFromZero);
		return Math.Max(BreakawayThresholdMinimum, Math.Min(BreakawayThresholdMaximum, rounded));
	}

	public static bool ShouldBreakAway(int independence, int rulerRelation)
	{
		return ClampPercent(independence) >= CalculateBreakawayThreshold(rulerRelation);
	}

	public static bool ShouldPreserveIndependenceOnRevision(bool oldUsesSubjectIndependence, bool newUsesSubjectIndependence)
	{
		return oldUsesSubjectIndependence && newUsesSubjectIndependence;
	}

	public static int EffectiveCooldownDay(int publicationDay, int policyCooldownDay)
	{
		return Math.Max(publicationDay, policyCooldownDay);
	}

	private static int ClampPercent(int value)
	{
		return Math.Max(IndependenceMinimum, Math.Min(IndependenceMaximum, value));
	}
}
