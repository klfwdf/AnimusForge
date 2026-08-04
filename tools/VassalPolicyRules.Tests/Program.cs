using System;
using System.Collections.Generic;
using AnimusForge;

static void Equal<T>(T expected, T actual, string name)
{
	if (!EqualityComparer<T>.Default.Equals(expected, actual))
	{
		throw new InvalidOperationException($"{name}: expected={expected}, actual={actual}");
	}
}

Equal(0, VassalPolicyRules.IndependenceFromObedience(100), "full obedience");
Equal(30, VassalPolicyRules.InitialIndependence, "initial independence");
Equal(70, VassalPolicyRules.InitialObedience, "initial obedience");
Equal(60, VassalPolicyRules.CalculateBreakawayThreshold(-100), "hostile ruler threshold");
Equal(80, VassalPolicyRules.CalculateBreakawayThreshold(0), "neutral ruler threshold");
Equal(100, VassalPolicyRules.CalculateBreakawayThreshold(100), "friendly ruler threshold");
Equal(70, VassalPolicyRules.CalculateBreakawayThreshold(-50), "negative midpoint threshold");
Equal(90, VassalPolicyRules.CalculateBreakawayThreshold(50), "positive midpoint threshold");
Equal(80, VassalPolicyRules.CalculateBreakawayThreshold(2), "positive rounding below half");
Equal(81, VassalPolicyRules.CalculateBreakawayThreshold(3), "positive rounding above half");
Equal(80, VassalPolicyRules.CalculateBreakawayThreshold(-2), "negative rounding below half");
Equal(79, VassalPolicyRules.CalculateBreakawayThreshold(-3), "negative rounding above half");
Equal(60, VassalPolicyRules.CalculateBreakawayThreshold(-999), "relation lower clamp");
Equal(100, VassalPolicyRules.CalculateBreakawayThreshold(999), "relation upper clamp");
Equal(-15, VassalPolicyRules.NormalizeQualityDelta(-99f), "quality lower clamp");
Equal(15, VassalPolicyRules.NormalizeQualityDelta(99f), "quality upper clamp");
Equal(6, VassalPolicyRules.NormalizeQualityDelta(5.5f), "quality rounding");
Equal(61, VassalPolicyRules.ApplyIndependenceChange(60, 8, -7), "atomic net change");
Equal(100, VassalPolicyRules.ApplyIndependenceChange(98, 10, 15), "independence upper clamp");
Equal(0, VassalPolicyRules.ApplyIndependenceChange(2, 5, -15), "independence lower clamp");
Equal(false, VassalPolicyRules.ShouldBreakAway(59, -100), "below hostile threshold");
Equal(true, VassalPolicyRules.ShouldBreakAway(60, -100), "at hostile threshold");
Equal(false, VassalPolicyRules.ShouldBreakAway(79, 0), "below neutral threshold");
Equal(true, VassalPolicyRules.ShouldBreakAway(80, 0), "at neutral threshold");
Equal(false, VassalPolicyRules.ShouldBreakAway(99, 100), "below friendly threshold");
Equal(true, VassalPolicyRules.ShouldBreakAway(100, 100), "at friendly threshold");
Equal(true, VassalPolicyRules.ShouldPreserveIndependenceOnRevision(true, true), "preserve independence type revision");
Equal(false, VassalPolicyRules.ShouldPreserveIndependenceOnRevision(false, true), "initialize independence when entering tracked type");
Equal(false, VassalPolicyRules.ShouldPreserveIndependenceOnRevision(true, false), "discard independence when leaving tracked type");
Equal(42, VassalPolicyRules.EffectiveCooldownDay(42, -1), "publication cooldown");
Equal(57, VassalPolicyRules.EffectiveCooldownDay(42, 57), "renewal cooldown");

Console.WriteLine("VassalPolicyRules regression checks passed.");
