using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;

namespace AnimusForge;

// Pure civil war probability math. No TaleWorlds types: linked into the smoke test project.
// Every roll is "weights x features -> raw chance -> randomness shaping -> clamp -> compare with random".

// Feature names shared by the catalog (weights) and the owner (feature extraction).
// Adding a feature = one constant here + one line in KingdomCivilWarOwner.BuildFeatures.
internal static class CivilWarFeature
{
	// King traits, trait level / 2 => -1..1.
	internal const string KingMercy = "king_mercy";
	internal const string KingAuthoritarian = "king_authoritarian";
	internal const string KingEgalitarian = "king_egalitarian";
	internal const string KingValor = "king_valor";
	internal const string KingGenerosity = "king_generosity";
	internal const string KingHonor = "king_honor";
	// Faction leader / evaluated clan leader traits, -1..1.
	internal const string LeaderValor = "leader_valor";
	internal const string LeaderCalculating = "leader_calculating";
	internal const string LeaderHonor = "leader_honor";
	internal const string LeaderMercy = "leader_mercy";
	// Kingdom state, 0..1.
	internal const string Instability = "instability";
	internal const string WarLoad = "war_load";
	internal const string KingdomGrievance = "kingdom_grievance";
	// Faction state, 0..1.
	internal const string FactionPower = "faction_power";
	internal const string FactionGrievance = "faction_grievance";
	internal const string Refusals = "refusals";
	// Evaluated clan, -1..1 or 0..1.
	internal const string ClanGrievance = "clan_grievance";
	internal const string RelationGap = "relation_gap";
	internal const string RelationToKing = "relation_to_king";
	internal const string BloodShy = "blood_shy";
	// Open war, -1..1 / 0..1.
	internal const string WarScore = "war_score";
	internal const string WarScoreAbs = "war_score_abs";
	internal const string WarProgress = "war_progress";
	// Always 1; lets a catalog entry scale its base through a weight if desired.
	internal const string One = "one";

	internal static readonly HashSet<string> All = new HashSet<string>(StringComparer.Ordinal)
	{
		KingMercy, KingAuthoritarian, KingEgalitarian, KingValor, KingGenerosity, KingHonor,
		LeaderValor, LeaderCalculating, LeaderHonor, LeaderMercy,
		Instability, WarLoad, KingdomGrievance,
		FactionPower, FactionGrievance, Refusals,
		ClanGrievance, RelationGap, RelationToKing, BloodShy,
		WarScore, WarScoreAbs, WarProgress, One
	};
}

internal readonly struct CivilWarWeight
{
	internal readonly string Feature;
	internal readonly float Weight;

	internal CivilWarWeight(string feature, float weight)
	{
		Feature = feature ?? "";
		Weight = weight;
	}
}

internal sealed class CivilWarChance
{
	internal readonly float Base;
	internal readonly CivilWarWeight[] Weights;

	internal CivilWarChance(float baseChance, params CivilWarWeight[] weights)
	{
		Base = baseChance;
		Weights = weights ?? Array.Empty<CivilWarWeight>();
	}

	internal float Raw(IReadOnlyDictionary<string, float> features)
	{
		float value = Base;
		foreach (CivilWarWeight weight in Weights)
		{
			if (features != null && features.TryGetValue(weight.Feature, out float feature)) value += weight.Weight * feature;
		}
		return value;
	}
}

// Runtime tuning. Built once per weekly advance from MCM; tests construct it directly.
internal sealed class CivilWarTuning
{
	internal float Randomness = 1f;
	internal float Floor = 0.05f;
	internal int DiscontentThreshold = 35;
	internal int UltimatumDelayWeeks = 2;
	internal int MinWarWeeks = 3;
	internal int MaxWarWeeks = 12;
	internal int CooldownWeeks = 8;
	internal int MaxRefusals = 4;
	internal int PlayerAnswerWeeks = 2;
	internal int SideLockWeeks = 2;
	internal int WarRequestTimeoutWeeks = 3;
}

internal struct CivilWarRoll
{
	internal string Label;
	internal float Raw;
	internal float Chance;
	internal float Roll;
	internal bool Passed;

	public override string ToString()
	{
		return Label + " raw=" + Raw.ToString("0.000", CultureInfo.InvariantCulture)
			+ " p=" + Chance.ToString("0.000", CultureInfo.InvariantCulture)
			+ " roll=" + Roll.ToString("0.000", CultureInfo.InvariantCulture)
			+ " pass=" + Passed;
	}
}

internal static class CivilWarRules
{
	// randomness 1: clamp raw into [floor, 1-floor]. randomness 0: expected-value decision (0 or 1).
	internal static float Shape(float raw, CivilWarTuning tuning)
	{
		float randomness = Clamp(tuning?.Randomness ?? 1f, 0f, 1f);
		float floor = Clamp(tuning?.Floor ?? 0.05f, 0f, 0.49f) * randomness;
		float p = Clamp(raw, 0f, 1f);
		float step = p >= 0.5f ? 1f : 0f;
		float shaped = step + (p - step) * randomness;
		return Clamp(shaped, floor, 1f - floor);
	}

	internal static CivilWarRoll Roll(string label, CivilWarChance chance, IReadOnlyDictionary<string, float> features, float scale, CivilWarTuning tuning, Func<float> random)
	{
		float raw = (chance?.Raw(features) ?? 0f) * scale;
		float p = Shape(raw, tuning);
		float roll = Clamp(random?.Invoke() ?? 0.5f, 0f, 0.99999f);
		return new CivilWarRoll { Label = label ?? "", Raw = raw, Chance = p, Roll = roll, Passed = roll < p };
	}

	// Weighted pick; randomness 0 returns the heaviest entry. Non-positive weights never win unless all are.
	internal static int PickWeighted(IReadOnlyList<float> weights, CivilWarTuning tuning, Func<float> random)
	{
		if (weights == null || weights.Count == 0) return -1;
		int best = 0;
		float total = 0f;
		for (int i = 0; i < weights.Count; i++)
		{
			float w = Math.Max(0f, weights[i]);
			total += w;
			if (w > Math.Max(0f, weights[best])) best = i;
		}
		if (total <= 0f || Clamp(tuning?.Randomness ?? 1f, 0f, 1f) <= 0f) return best;
		float target = Clamp(random?.Invoke() ?? 0.5f, 0f, 0.99999f) * total;
		for (int i = 0; i < weights.Count; i++)
		{
			target -= Math.Max(0f, weights[i]);
			if (target < 0f) return i;
		}
		return best;
	}

	internal static string Describe(IReadOnlyDictionary<string, float> features)
	{
		if (features == null || features.Count == 0) return "";
		StringBuilder builder = new StringBuilder();
		foreach (KeyValuePair<string, float> pair in features)
		{
			if (pair.Key == CivilWarFeature.One || Math.Abs(pair.Value) < 0.001f) continue;
			if (builder.Length > 0) builder.Append(' ');
			builder.Append(pair.Key).Append('=').Append(pair.Value.ToString("0.00", CultureInfo.InvariantCulture));
		}
		return builder.ToString();
	}

	internal static float Clamp(float value, float min, float max)
	{
		if (float.IsNaN(value)) return min;
		return value < min ? min : value > max ? max : value;
	}

	internal static float Trait(int level) => Clamp(level / 2f, -1f, 1f);
}
