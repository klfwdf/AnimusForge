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
	// Kingdom war weariness accumulated from individual battles, 0..1.
	internal const string Weariness = "weariness";
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
		Instability, WarLoad, KingdomGrievance, Weariness,
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
	internal int PlayerDetonationStrengthPercent = 20;
	internal int UltimatumDelayWeeks = 4;
	internal int MinWarWeeks = 3;
	internal int MaxWarWeeks = 12;
	internal int CooldownWeeks = 8;
	internal int MaxRefusals = 4;
	// A faction must have been refused this many times before an automatic escalation may open a war (<= MaxRefusals).
	internal int MinRefusalsBeforeWar = 2;
	internal int PlayerAnswerWeeks = 2;
	internal int SideLockWeeks = 2;
	internal int WarRequestTimeoutWeeks = 3;
	// Concurrent factions per kingdom (MCM 1..4).
	internal int MaxFactions = 3;
	// false = only one faction may be at open war, the others wait; true = each faction may rebel on its own.
	internal bool AllowConcurrentWars;
}

// Pure multi-faction rules (no TaleWorlds types) so the smoke tests can cover them.
internal static class CivilWarFactionRules
{
	// A new faction may form while below the cap and outside the cooldown.
	internal static bool CanFormFaction(int activeFactions, int week, int cooldownUntilWeek, CivilWarTuning tuning)
	{
		int cap = Math.Max(1, Math.Min(4, tuning?.MaxFactions ?? 3));
		return activeFactions < cap && week >= cooldownUntilWeek;
	}

	// Single-war mode lets a faction open war only while no other faction of the kingdom is at war.
	internal static bool CanOpenWar(bool otherFactionAtWar, CivilWarTuning tuning)
	{
		return !otherFactionAtWar || (tuning?.AllowConcurrentWars ?? false);
	}

	// Automatic escalation into open war needs enough refusals first (Refusals counts the one just given).
	internal static bool HasEnoughRefusals(int refusals, CivilWarTuning tuning)
	{
		int required = Math.Max(1, Math.Min(tuning?.MaxRefusals ?? 4, tuning?.MinRefusalsBeforeWar ?? 2));
		return refusals >= required;
	}

	// A kingdom just shaken by a civil war (or one faction already in arms) is quicker to give in to the others.
	internal const float CooldownAcceptScale = 1.5f;

	internal static float AcceptScale(bool inKingdomCooldown) => inKingdomCooldown ? CooldownAcceptScale : 1f;

	// The concession window outlasts the kingdom cooldown by the truce length, so factions held back until the
	// cooldown ends still meet a king inclined to give in.
	internal static bool InConcessionWindow(int day, int cooldownUntilDay, CivilWarTuning tuning)
	{
		return cooldownUntilDay > 0 && day < cooldownUntilDay + CivilWarAftermathRules.TruceWeeks(tuning) * 7;
	}

	// A faction may form from grievance or from war weariness; either condition is enough.
	internal static bool MeetsFormationCondition(float grievance, float weariness, CivilWarTuning tuning)
	{
		return grievance >= (tuning?.DiscontentThreshold ?? 35) || CivilWarWearinessRules.MeetsFormation(weariness);
	}

	// Relation to the king at which the join chance stops falling and stays at the floor below.
	internal const int JoinRelationCeiling = 60;
	// Even a clan on excellent terms with the king can still side against him; good relations only make it unlikely.
	internal const float JoinRelationMinFactor = 0.1f;

	// Clans on good terms with the king rarely side against him: full chance at relation <= 0, linear down to the floor at the ceiling.
	internal static float JoinRelationFactor(int relationToKing)
	{
		return CivilWarRules.Clamp(1f - Math.Max(0, relationToKing) / (float)JoinRelationCeiling, JoinRelationMinFactor, 1f);
	}

	// Second gate applied after a passed join roll; loyal clans pass rarely but never with zero chance.
	// Randomness 0 is the deterministic mode: pass only when the factor is at least one half.
	internal static bool PassesJoinRelationGate(float factor, CivilWarTuning tuning, Func<float> random)
	{
		if (factor >= 1f) return true;
		if (factor <= 0f) return false;
		if (CivilWarRules.Clamp(tuning?.Randomness ?? 1f, 0f, 1f) <= 0f) return factor >= 0.5f;
		return CivilWarRules.Clamp(random?.Invoke() ?? 0.5f, 0f, 0.99999f) < factor;
	}

	// Two factions never share a demand; the second one would just duplicate the first.
	internal static bool IsDemandTaken(IEnumerable<string> activeDemandIds, string demandId)
	{
		if (activeDemandIds == null || string.IsNullOrWhiteSpace(demandId)) return false;
		foreach (string id in activeDemandIds)
		{
			if (string.Equals(id, demandId, StringComparison.Ordinal)) return true;
		}
		return false;
	}

	// Faction grievance = mean of its member clans' totals, 0..100.
	internal static float FactionGrievance(IEnumerable<float> memberTotals)
	{
		float sum = 0f;
		int count = 0;
		foreach (float value in memberTotals ?? Array.Empty<float>())
		{
			sum += Clamp01x100(value);
			count++;
		}
		return count == 0 ? 0f : sum / count;
	}

	// A middle clan joining the opposition picks the faction whose demand matches its own grievance best.
	// scores[i] = affinity of the clan's grievance to faction i. Returns -1 when there is no faction.
	internal static int PickFactionForClan(IReadOnlyList<float> scores, CivilWarTuning tuning, Func<float> random)
	{
		if (scores == null || scores.Count == 0) return -1;
		List<float> weights = new List<float>(scores.Count);
		foreach (float score in scores) weights.Add(Math.Max(0.05f, score));
		return CivilWarRules.PickWeighted(weights, tuning, random);
	}

	private static float Clamp01x100(float value) => CivilWarRules.Clamp(value, 0f, 100f);
}

// Kingdom war weariness (0..100), built battle by battle and fading with time and peace.
// Stored per kingdom; decay is applied lazily from the last update day, so nothing ticks.
internal static class CivilWarWearinessRules
{
	internal const float Max = 100f;
	// Either this weariness or the grievance threshold lets a faction form.
	internal const float FormationThreshold = 40f;
	internal const float WeeklyDecay = 0.12f;
	// Share of the current weariness removed by each peace treaty.
	internal const float PeaceRelief = 0.35f;
	private static readonly double DailyRetention = Math.Pow(1d - WeeklyDecay, 1d / 7d);

	internal static bool MeetsFormation(float weariness) => weariness >= FormationThreshold;

	// One battle: casualties relative to the side's committed troops, plus a flat cost for losing.
	// The winner still tires, at half the casualty cost.
	internal static float BattlePoints(int casualties, int committed, bool lost)
	{
		float losses = Math.Max(0, casualties);
		float share = losses / Math.Max(50f, committed);
		float points = Math.Min(4f, losses / 150f) + Math.Min(3f, share * 6f);
		if (!lost) return points * 0.5f;
		return points + 2f;
	}

	internal static float Decayed(float value, int fromDay, int toDay)
	{
		if (value <= 0f) return 0f;
		if (fromDay < 0 || toDay <= fromDay) return CivilWarRules.Clamp(value, 0f, Max);
		return CivilWarRules.Clamp((float)(value * Math.Pow(DailyRetention, toDay - fromDay)), 0f, Max);
	}

	internal static float AfterPeace(float value) => CivilWarRules.Clamp(value * (1f - PeaceRelief), 0f, Max);

	// Weekly stability drain of a war-weary kingdom (battle swings themselves are kept small).
	internal static int WeeklyStability(float weariness) => weariness >= 70f ? -2 : weariness >= FormationThreshold ? -1 : 0;
}

// How the other factions of a kingdom react when one civil war ends.
internal enum CivilWarAftermath
{
	None = 0,
	Suppressed = 1,  // crown won: remaining factions are cowed
	Emboldened = 2,  // rebels won: remaining factions push harder
	Settled = 3      // negotiated / accepted: only the truce applies
}

internal static class CivilWarAftermathRules
{
	// Weeks every remaining faction waits before its next ultimatum after a war ends.
	internal static int TruceWeeks(CivilWarTuning tuning)
	{
		return Math.Max(2, (tuning?.CooldownWeeks ?? 8) / 2);
	}

	// Remaining factions' grievance multiplier (applied once to every source of their members).
	internal static float GrievanceFactor(CivilWarAftermath aftermath)
	{
		return aftermath == CivilWarAftermath.Suppressed ? 0.6f : aftermath == CivilWarAftermath.Emboldened ? 1.1f : 1f;
	}

	// Multiplier on the chance that a member leaves its faction, while the aftermath lasts.
	internal static float LeaveFactor(CivilWarAftermath aftermath)
	{
		return aftermath == CivilWarAftermath.Suppressed ? 2.5f : aftermath == CivilWarAftermath.Emboldened ? 0.5f : 1f;
	}

	// Multiplier on the escalation roll of a refused demand, while the aftermath lasts.
	internal static float EscalationFactor(CivilWarAftermath aftermath)
	{
		return aftermath == CivilWarAftermath.Suppressed ? 0.5f : aftermath == CivilWarAftermath.Emboldened ? 1.5f : 1f;
	}

	// The mood lasts twice as long as the truce, so it still shapes the first ultimatum after it.
	internal static int MoodWeeks(CivilWarTuning tuning) => TruceWeeks(tuning) * 2;

	internal static CivilWarAftermath Current(CivilWarAftermath aftermath, int week, int untilWeek)
	{
		return week <= untilWeek ? aftermath : CivilWarAftermath.None;
	}
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
