using System;
using System.Collections.Generic;
using System.Linq;

namespace AnimusForge;

// ============================================================================
// Civil war content catalog. Add / remove / tune content HERE.
//   - New grievance source: add a CivilWarGrievanceSourceDef, then feed it from
//     CivilWarCampaignBehavior (event -> CivilWarOwner.AddGrievance).
//   - New demand: add a CivilWarDemandDef. AcceptEffectId must be registered in
//     CivilWarEffects; WarGoal decides which outcomes can close its war.
//   - New outcome: add a CivilWarOutcomeDef with a registered EffectId.
//   - Deleting an entry is safe for saves: unknown ids in a save dissolve the
//     faction on load and are logged.
// Pure C# (no TaleWorlds types) so tests can validate the catalog.
// ============================================================================

internal enum CivilWarWarGoal
{
	None = 0,     // demand cannot justify a war on its own (escalation needs conversion)
	Coerce = 1,   // war to force the demand
	Usurp = 2,    // war for the throne
	Secede = 3    // war for independence
}

// What the demand is about. The owner resolves the concrete target; unresolvable => demand ineligible.
internal enum CivilWarDemandTarget
{
	None = 0,
	EnemyKingdom = 1,   // strongest kingdom currently at war with this kingdom
	ImposedPolicy = 2   // last policy enacted against vassal opposition, still active
}

internal sealed class CivilWarGrievanceSourceDef
{
	internal string Id = "";
	internal string Name = "";
	internal float DecayPerWeek = 0.15f;
}

internal sealed class CivilWarDemandDef
{
	internal string Id = "";
	internal string FactionNameFormat = "{leader}为首的派系"; // {leader} = clan name
	internal string Text = "";                               // {target} = resolved target name
	internal string Color = "#6B3A78FF";
	internal float BaseWeight = 0.1f;
	// grievance source id -> weight per 20 points of that source on the leader clan.
	internal Dictionary<string, float> Affinity = new Dictionary<string, float>(StringComparer.Ordinal);
	internal CivilWarDemandTarget Target = CivilWarDemandTarget.None;
	internal int MinFortifications;
	internal int MaxLeaderRelationToKing = 100;
	internal string AcceptEffectId = "";
	internal CivilWarChance Accept;
	internal CivilWarChance Escalate;
	internal float EscalationScale = 1f;
	internal CivilWarWarGoal WarGoal = CivilWarWarGoal.None;
	// War goal used when a non-war demand escalates anyway (x EscalationScale).
	internal CivilWarWarGoal FallbackWarGoal = CivilWarWarGoal.Coerce;
	internal bool CanConvertToUsurp = true;
}

internal sealed class CivilWarOutcomeDef
{
	internal string Id = "";
	internal string Name = "";
	internal CivilWarWarGoal[] Goals = Array.Empty<CivilWarWarGoal>();
	internal CivilWarChance Weight;
	internal string EffectId = "";
	internal bool RebelsWon;
}

// Effect ids implemented in CivilWarEffects.cs. Kept here so the pure test project can validate the catalog.
// Adding an effect = one constant here + one ICivilWarEffect class registered in CivilWarEffects.
internal static class CivilWarEffectIds
{
	internal const string MakePeaceWithTarget = "make_peace_with_target";
	internal const string PledgeWarOnTarget = "pledge_war_on_target";
	internal const string RevokeTargetPolicy = "revoke_target_policy";
	internal const string PayRedress = "pay_redress";
	internal const string GrantPrivileges = "grant_privileges";
	internal const string Abdicate = "abdicate";
	internal const string UsurpThrone = "usurp_throne";
	internal const string Secede = "secede";
	internal const string Concession = "concession";
	internal const string CrownVictory = "crown_victory";

	internal static readonly HashSet<string> All = new HashSet<string>(StringComparer.Ordinal)
	{
		MakePeaceWithTarget, PledgeWarOnTarget, RevokeTargetPolicy, PayRedress, GrantPrivileges, Abdicate,
		UsurpThrone, Secede, Concession, CrownVictory
	};
}

internal static class CivilWarCatalog
{
	internal const string UsurpDemandId = "usurp";
	internal const string CrownVictoryOutcomeId = "crown_victory";
	internal const string SecedeOutcomeId = "rebels_secede";
	internal const string NegotiatedOutcomeId = "negotiated";

	// ---------------------------------------------------------------- global rolls
	// Discontent -> faction formed. Rolled once per week for the most aggrieved clan.
	internal static readonly CivilWarChance FormFaction = new CivilWarChance(0.10f,
		W(CivilWarFeature.ClanGrievance, 0.45f),
		W(CivilWarFeature.Instability, 0.20f),
		W(CivilWarFeature.LeaderValor, 0.08f),
		W(CivilWarFeature.LeaderHonor, -0.08f),
		W(CivilWarFeature.KingAuthoritarian, 0.06f));

	// Middle clan joins the opposition this week.
	internal static readonly CivilWarChance JoinOpposition = new CivilWarChance(0.02f,
		W(CivilWarFeature.ClanGrievance, 0.35f),
		W(CivilWarFeature.RelationGap, 0.30f),
		W(CivilWarFeature.FactionPower, 0.15f),
		W(CivilWarFeature.BloodShy, -0.20f),
		W(CivilWarFeature.RelationToKing, -0.20f));

	// Opposition clan (not leader) leaves the faction this week.
	internal static readonly CivilWarChance LeaveOpposition = new CivilWarChance(0.06f,
		W(CivilWarFeature.RelationToKing, 0.25f),
		W(CivilWarFeature.ClanGrievance, -0.30f),
		W(CivilWarFeature.FactionPower, -0.10f));

	// Refused but not escalated: king stalls instead of refusing outright.
	internal static readonly CivilWarChance KingDefers = new CivilWarChance(0.20f,
		W(CivilWarFeature.KingHonor, -0.08f),
		W(CivilWarFeature.Refusals, -0.15f));

	// After 2+ refusals a non-war demand hardens into a claim on the throne.
	internal static readonly CivilWarChance ConvertToUsurp = new CivilWarChance(0.05f,
		W(CivilWarFeature.Refusals, 0.25f),
		W(CivilWarFeature.FactionPower, 0.20f),
		W(CivilWarFeature.KingAuthoritarian, 0.10f),
		W(CivilWarFeature.LeaderCalculating, 0.06f));

	// Open war ends this week (checked after the minimum war length).
	internal static readonly CivilWarChance WarEnds = new CivilWarChance(0.05f,
		W(CivilWarFeature.WarScoreAbs, 0.45f),
		W(CivilWarFeature.WarProgress, 0.35f));

	// ---------------------------------------------------------------- sources
	internal static readonly IReadOnlyList<CivilWarGrievanceSourceDef> Sources = new List<CivilWarGrievanceSourceDef>
	{
		new CivilWarGrievanceSourceDef { Id = "royal_execution", Name = "王室处决", DecayPerWeek = 0.08f },
		new CivilWarGrievanceSourceDef { Id = "fief_denied", Name = "封地落选", DecayPerWeek = 0.12f },
		new CivilWarGrievanceSourceDef { Id = "policy_imposed", Name = "政策强推", DecayPerWeek = 0.12f },
		new CivilWarGrievanceSourceDef { Id = "war_imposed", Name = "强行开战", DecayPerWeek = 0.15f },
		new CivilWarGrievanceSourceDef { Id = "peace_imposed", Name = "强行议和", DecayPerWeek = 0.15f },
		new CivilWarGrievanceSourceDef { Id = "lands_raided", Name = "领地遭劫", DecayPerWeek = 0.20f },
		new CivilWarGrievanceSourceDef { Id = "fief_lost", Name = "失守封地", DecayPerWeek = 0.10f },
		new CivilWarGrievanceSourceDef { Id = "broken_pledge", Name = "国王背诺", DecayPerWeek = 0.06f },
		new CivilWarGrievanceSourceDef { Id = "demand_refused", Name = "诉求被拒", DecayPerWeek = 0.10f }
	};

	// ---------------------------------------------------------------- demands
	internal static readonly IReadOnlyList<CivilWarDemandDef> Demands = new List<CivilWarDemandDef>
	{
		new CivilWarDemandDef
		{
			Id = "make_peace", FactionNameFormat = "{leader}为首的反战派", Text = "停止与{target}的战争并议和", Color = "#1E6B4AFF",
			BaseWeight = 0.05f, Target = CivilWarDemandTarget.EnemyKingdom,
			Affinity = A(("lands_raided", 0.9f), ("war_imposed", 1.0f), ("fief_lost", 0.6f)),
			AcceptEffectId = CivilWarEffectIds.MakePeaceWithTarget,
			Accept = new CivilWarChance(0.30f, W(CivilWarFeature.KingMercy, 0.15f), W(CivilWarFeature.KingValor, -0.12f), W(CivilWarFeature.FactionPower, 0.30f), W(CivilWarFeature.WarLoad, 0.20f), W(CivilWarFeature.Instability, 0.10f)),
			Escalate = new CivilWarChance(0.15f, W(CivilWarFeature.FactionGrievance, 0.35f), W(CivilWarFeature.FactionPower, 0.30f), W(CivilWarFeature.LeaderValor, 0.08f)),
			EscalationScale = 0.3f, WarGoal = CivilWarWarGoal.None
		},
		new CivilWarDemandDef
		{
			Id = "continue_war", FactionNameFormat = "{leader}为首的主战派", Text = "对{target}战事不许议和", Color = "#8C4A1EFF",
			BaseWeight = 0.05f, Target = CivilWarDemandTarget.EnemyKingdom,
			Affinity = A(("peace_imposed", 1.2f), ("fief_lost", 0.5f)),
			AcceptEffectId = CivilWarEffectIds.PledgeWarOnTarget,
			Accept = new CivilWarChance(0.40f, W(CivilWarFeature.KingValor, 0.15f), W(CivilWarFeature.KingMercy, -0.10f), W(CivilWarFeature.FactionPower, 0.25f), W(CivilWarFeature.WarLoad, -0.25f)),
			Escalate = new CivilWarChance(0.15f, W(CivilWarFeature.FactionGrievance, 0.35f), W(CivilWarFeature.FactionPower, 0.30f), W(CivilWarFeature.LeaderValor, 0.10f)),
			EscalationScale = 0.3f, WarGoal = CivilWarWarGoal.None
		},
		new CivilWarDemandDef
		{
			Id = "revoke_policy", FactionNameFormat = "{leader}为首的改革派", Text = "废除{target}", Color = "#6B3A78FF",
			BaseWeight = 0.02f, Target = CivilWarDemandTarget.ImposedPolicy,
			Affinity = A(("policy_imposed", 1.3f)),
			AcceptEffectId = CivilWarEffectIds.RevokeTargetPolicy,
			Accept = new CivilWarChance(0.35f, W(CivilWarFeature.KingEgalitarian, 0.18f), W(CivilWarFeature.KingAuthoritarian, -0.20f), W(CivilWarFeature.FactionPower, 0.30f), W(CivilWarFeature.Instability, 0.10f)),
			Escalate = new CivilWarChance(0.12f, W(CivilWarFeature.FactionGrievance, 0.35f), W(CivilWarFeature.FactionPower, 0.30f)),
			EscalationScale = 0.3f, WarGoal = CivilWarWarGoal.None
		},
		new CivilWarDemandDef
		{
			Id = "redress", FactionNameFormat = "{leader}为首的复仇派", Text = "赔偿损失并追究责任", Color = "#6B1E4AFF",
			BaseWeight = 0.03f,
			Affinity = A(("royal_execution", 1.2f), ("fief_denied", 0.8f), ("broken_pledge", 0.8f), ("fief_lost", 0.4f)),
			AcceptEffectId = CivilWarEffectIds.PayRedress,
			Accept = new CivilWarChance(0.35f, W(CivilWarFeature.KingGenerosity, 0.15f), W(CivilWarFeature.KingMercy, 0.10f), W(CivilWarFeature.KingAuthoritarian, -0.15f), W(CivilWarFeature.FactionPower, 0.25f)),
			Escalate = new CivilWarChance(0.15f, W(CivilWarFeature.FactionGrievance, 0.40f), W(CivilWarFeature.FactionPower, 0.25f)),
			EscalationScale = 0.3f, WarGoal = CivilWarWarGoal.None
		},
		new CivilWarDemandDef
		{
			Id = "autonomy", FactionNameFormat = "{leader}为首的分离派", Text = "承认本派封地自治", Color = "#8C5A12FF",
			BaseWeight = 0.02f, MinFortifications = 1, MaxLeaderRelationToKing = -5,
			Affinity = A(("fief_denied", 0.6f), ("lands_raided", 0.5f), ("demand_refused", 0.5f), ("broken_pledge", 0.5f)),
			AcceptEffectId = CivilWarEffectIds.GrantPrivileges,
			Accept = new CivilWarChance(0.25f, W(CivilWarFeature.KingMercy, 0.12f), W(CivilWarFeature.KingAuthoritarian, -0.20f), W(CivilWarFeature.FactionPower, 0.35f)),
			Escalate = new CivilWarChance(0.20f, W(CivilWarFeature.FactionGrievance, 0.40f), W(CivilWarFeature.FactionPower, 0.35f), W(CivilWarFeature.LeaderValor, 0.08f)),
			EscalationScale = 1f, WarGoal = CivilWarWarGoal.Secede, CanConvertToUsurp = false
		},
		new CivilWarDemandDef
		{
			Id = UsurpDemandId, FactionNameFormat = "{leader}为首的宣权派", Text = "国王退位，由本派领袖继位", Color = "#8C1E1EFF",
			BaseWeight = 0.01f, MinFortifications = 1, MaxLeaderRelationToKing = -10,
			Affinity = A(("royal_execution", 0.7f), ("broken_pledge", 0.6f), ("demand_refused", 0.8f)),
			AcceptEffectId = CivilWarEffectIds.Abdicate,
			Accept = new CivilWarChance(0.02f, W(CivilWarFeature.FactionPower, 0.25f), W(CivilWarFeature.Instability, 0.10f), W(CivilWarFeature.KingValor, -0.05f)),
			Escalate = new CivilWarChance(0.20f, W(CivilWarFeature.FactionGrievance, 0.40f), W(CivilWarFeature.FactionPower, 0.35f), W(CivilWarFeature.LeaderCalculating, 0.06f)),
			EscalationScale = 1f, WarGoal = CivilWarWarGoal.Usurp, CanConvertToUsurp = false
		}
	};

	// ---------------------------------------------------------------- outcomes
	internal static readonly IReadOnlyList<CivilWarOutcomeDef> Outcomes = new List<CivilWarOutcomeDef>
	{
		new CivilWarOutcomeDef
		{
			Id = "rebels_usurp", Name = "叛军夺位", Goals = G(CivilWarWarGoal.Usurp), EffectId = CivilWarEffectIds.UsurpThrone, RebelsWon = true,
			Weight = new CivilWarChance(0.35f, W(CivilWarFeature.WarScore, 0.60f))
		},
		new CivilWarOutcomeDef
		{
			Id = SecedeOutcomeId, Name = "叛军独立", Goals = G(CivilWarWarGoal.Secede), EffectId = CivilWarEffectIds.Secede, RebelsWon = true,
			Weight = new CivilWarChance(0.35f, W(CivilWarFeature.WarScore, 0.60f))
		},
		new CivilWarOutcomeDef
		{
			Id = "rebels_coerce", Name = "叛军逼宫", Goals = G(CivilWarWarGoal.Coerce), EffectId = CivilWarEffectIds.Concession, RebelsWon = true,
			Weight = new CivilWarChance(0.35f, W(CivilWarFeature.WarScore, 0.60f))
		},
		new CivilWarOutcomeDef
		{
			Id = NegotiatedOutcomeId, Name = "议和妥协", Goals = G(CivilWarWarGoal.Coerce, CivilWarWarGoal.Usurp, CivilWarWarGoal.Secede), EffectId = CivilWarEffectIds.Concession, RebelsWon = false,
			Weight = new CivilWarChance(0.25f, W(CivilWarFeature.WarScoreAbs, -0.30f), W(CivilWarFeature.WarProgress, 0.15f))
		},
		new CivilWarOutcomeDef
		{
			Id = CrownVictoryOutcomeId, Name = "王室平叛", Goals = G(CivilWarWarGoal.Coerce, CivilWarWarGoal.Usurp, CivilWarWarGoal.Secede), EffectId = CivilWarEffectIds.CrownVictory, RebelsWon = false,
			Weight = new CivilWarChance(0.35f, W(CivilWarFeature.WarScore, -0.60f))
		}
	};

	// ---------------------------------------------------------------- lookup
	private static Dictionary<string, CivilWarGrievanceSourceDef> _sourcesById;
	private static Dictionary<string, CivilWarDemandDef> _demandsById;
	private static Dictionary<string, CivilWarOutcomeDef> _outcomesById;
	private static List<CivilWarDemandDef> _validDemands;
	private static List<CivilWarOutcomeDef> _validOutcomes;

	internal static IReadOnlyList<CivilWarDemandDef> ValidDemands => _validDemands ?? (IReadOnlyList<CivilWarDemandDef>)Demands;
	internal static IReadOnlyList<CivilWarOutcomeDef> ValidOutcomes => _validOutcomes ?? (IReadOnlyList<CivilWarOutcomeDef>)Outcomes;

	// Called once at session launch with the registered effect ids. Invalid entries are dropped.
	internal static List<string> Initialize(ICollection<string> effectIds)
	{
		List<string> errors = Validate(effectIds);
		HashSet<string> bad = new HashSet<string>(errors.Select(x => x.Split(':')[0]), StringComparer.Ordinal);
		_sourcesById = Sources.Where(x => !bad.Contains("source/" + x.Id)).GroupBy(x => x.Id).ToDictionary(x => x.Key, x => x.First(), StringComparer.Ordinal);
		_validDemands = Demands.Where(x => !bad.Contains("demand/" + x.Id)).ToList();
		_validOutcomes = Outcomes.Where(x => !bad.Contains("outcome/" + x.Id)).ToList();
		_demandsById = _validDemands.GroupBy(x => x.Id).ToDictionary(x => x.Key, x => x.First(), StringComparer.Ordinal);
		_outcomesById = _validOutcomes.GroupBy(x => x.Id).ToDictionary(x => x.Key, x => x.First(), StringComparer.Ordinal);
		return errors;
	}

	internal static CivilWarGrievanceSourceDef FindSource(string id)
	{
		EnsureIndex();
		return id != null && _sourcesById.TryGetValue(id, out CivilWarGrievanceSourceDef def) ? def : null;
	}

	internal static CivilWarDemandDef FindDemand(string id)
	{
		EnsureIndex();
		return id != null && _demandsById.TryGetValue(id, out CivilWarDemandDef def) ? def : null;
	}

	internal static CivilWarOutcomeDef FindOutcome(string id)
	{
		EnsureIndex();
		return id != null && _outcomesById.TryGetValue(id, out CivilWarOutcomeDef def) ? def : null;
	}

	// Error format: "<kind>/<id>: message". Used by tests and at startup.
	internal static List<string> Validate(ICollection<string> effectIds)
	{
		List<string> errors = new List<string>();
		HashSet<string> sourceIds = new HashSet<string>(StringComparer.Ordinal);
		foreach (CivilWarGrievanceSourceDef source in Sources)
		{
			string key = "source/" + (source?.Id ?? "");
			if (source == null || string.IsNullOrWhiteSpace(source.Id)) { errors.Add(key + ": empty id"); continue; }
			if (!sourceIds.Add(source.Id)) errors.Add(key + ": duplicate id");
			if (source.DecayPerWeek < 0f || source.DecayPerWeek > 1f) errors.Add(key + ": decay must be 0..1");
		}
		HashSet<string> demandIds = new HashSet<string>(StringComparer.Ordinal);
		foreach (CivilWarDemandDef demand in Demands)
		{
			string key = "demand/" + (demand?.Id ?? "");
			if (demand == null || string.IsNullOrWhiteSpace(demand.Id)) { errors.Add(key + ": empty id"); continue; }
			if (!demandIds.Add(demand.Id)) errors.Add(key + ": duplicate id");
			if (demand.Accept == null || demand.Escalate == null) errors.Add(key + ": missing accept/escalate chance");
			if (effectIds != null && !effectIds.Contains(demand.AcceptEffectId ?? "")) errors.Add(key + ": unknown effect " + demand.AcceptEffectId);
			foreach (string sourceId in demand.Affinity?.Keys ?? Enumerable.Empty<string>())
			{
				if (!sourceIds.Contains(sourceId)) errors.Add(key + ": unknown source " + sourceId);
			}
			CheckChance(errors, key + ".accept", demand.Accept);
			CheckChance(errors, key + ".escalate", demand.Escalate);
			if (demand.EscalationScale < 0f) errors.Add(key + ": escalation scale < 0");
		}
		if (!demandIds.Contains(UsurpDemandId)) errors.Add("demand/" + UsurpDemandId + ": required for conversion");
		HashSet<string> outcomeIds = new HashSet<string>(StringComparer.Ordinal);
		HashSet<CivilWarWarGoal> coveredGoals = new HashSet<CivilWarWarGoal>();
		foreach (CivilWarOutcomeDef outcome in Outcomes)
		{
			string key = "outcome/" + (outcome?.Id ?? "");
			if (outcome == null || string.IsNullOrWhiteSpace(outcome.Id)) { errors.Add(key + ": empty id"); continue; }
			if (!outcomeIds.Add(outcome.Id)) errors.Add(key + ": duplicate id");
			if (outcome.Weight == null) errors.Add(key + ": missing weight");
			if (effectIds != null && !effectIds.Contains(outcome.EffectId ?? "")) errors.Add(key + ": unknown effect " + outcome.EffectId);
			CheckChance(errors, key + ".weight", outcome.Weight);
			foreach (CivilWarWarGoal goal in outcome.Goals ?? Array.Empty<CivilWarWarGoal>()) coveredGoals.Add(goal);
		}
		foreach (CivilWarWarGoal goal in new[] { CivilWarWarGoal.Coerce, CivilWarWarGoal.Usurp, CivilWarWarGoal.Secede })
		{
			if (!coveredGoals.Contains(goal)) errors.Add("outcome/*: no outcome closes war goal " + goal);
		}
		foreach (CivilWarChance global in new[] { FormFaction, JoinOpposition, LeaveOpposition, KingDefers, ConvertToUsurp, WarEnds })
		{
			CheckChance(errors, "global/*", global);
		}
		return errors;
	}

	internal static CivilWarWarGoal EffectiveWarGoal(CivilWarDemandDef demand)
	{
		if (demand == null) return CivilWarWarGoal.Coerce;
		return demand.WarGoal != CivilWarWarGoal.None ? demand.WarGoal : demand.FallbackWarGoal;
	}

	private static void CheckChance(List<string> errors, string key, CivilWarChance chance)
	{
		if (chance == null) return;
		if (float.IsNaN(chance.Base) || float.IsInfinity(chance.Base)) errors.Add(key + ": base not finite");
		foreach (CivilWarWeight weight in chance.Weights)
		{
			if (!CivilWarFeature.All.Contains(weight.Feature)) errors.Add(key + ": unknown feature " + weight.Feature);
			if (float.IsNaN(weight.Weight) || float.IsInfinity(weight.Weight)) errors.Add(key + ": weight not finite");
		}
	}

	private static void EnsureIndex()
	{
		if (_sourcesById == null) Initialize(null);
	}

	private static CivilWarWeight W(string feature, float weight) => new CivilWarWeight(feature, weight);

	private static CivilWarWarGoal[] G(params CivilWarWarGoal[] goals) => goals;

	private static Dictionary<string, float> A(params (string Source, float Weight)[] entries)
	{
		Dictionary<string, float> map = new Dictionary<string, float>(StringComparer.Ordinal);
		foreach ((string source, float weight) in entries) map[source] = weight;
		return map;
	}
}

// Pure decisions composed from catalog chances. The owner feeds features and applies results.
internal enum CivilWarRuling
{
	Accept = 0,
	Defer = 1,
	Refuse = 2
}

internal struct CivilWarUltimatumResult
{
	internal CivilWarRuling Ruling;
	internal bool Escalate;
	internal bool ConvertToUsurp;
	internal CivilWarRoll AcceptRoll;
	internal CivilWarRoll EscalateRoll;
	internal string Log;
}

internal static class CivilWarDecisions
{
	internal static CivilWarUltimatumResult RuleOnUltimatum(CivilWarDemandDef demand, IReadOnlyDictionary<string, float> features, int refusals, CivilWarTuning tuning, Func<float> random)
	{
		CivilWarUltimatumResult result = new CivilWarUltimatumResult();
		result.AcceptRoll = CivilWarRules.Roll("accept:" + demand?.Id, demand?.Accept, features, 1f, tuning, random);
		if (result.AcceptRoll.Passed)
		{
			result.Ruling = CivilWarRuling.Accept;
			result.Log = result.AcceptRoll.ToString();
			return result;
		}
		CivilWarRoll defer = CivilWarRules.Roll("defer", CivilWarCatalog.KingDefers, features, 1f, tuning, random);
		if (defer.Passed)
		{
			result.Ruling = CivilWarRuling.Defer;
			result.Log = result.AcceptRoll + " | " + defer;
			return result;
		}
		CivilWarUltimatumResult refusal = RollRefusal(demand, features, refusals, tuning, random);
		refusal.AcceptRoll = result.AcceptRoll;
		refusal.Log = result.AcceptRoll + " | " + defer + " | " + refusal.Log;
		return refusal;
	}

	// Consequences of an outright refusal (NPC king, or a player king answering / ignoring the ultimatum).
	internal static CivilWarUltimatumResult RollRefusal(CivilWarDemandDef demand, IReadOnlyDictionary<string, float> features, int refusals, CivilWarTuning tuning, Func<float> random)
	{
		CivilWarUltimatumResult result = new CivilWarUltimatumResult { Ruling = CivilWarRuling.Refuse };
		float scale = demand?.EscalationScale ?? 1f;
		result.EscalateRoll = CivilWarRules.Roll("escalate:" + demand?.Id, demand?.Escalate, features, scale, tuning, random);
		result.Escalate = result.EscalateRoll.Passed;
		string log = result.EscalateRoll.ToString();
		if (!result.Escalate && demand != null && demand.CanConvertToUsurp && refusals + 1 >= 2)
		{
			CivilWarRoll convert = CivilWarRules.Roll("convert", CivilWarCatalog.ConvertToUsurp, features, 1f, tuning, random);
			result.ConvertToUsurp = convert.Passed;
			log += " | " + convert;
		}
		result.Log = log;
		return result;
	}

	// Picks the outcome for a war with the given goal. Returns null only when the catalog has no candidate.
	internal static CivilWarOutcomeDef PickOutcome(CivilWarWarGoal goal, IReadOnlyDictionary<string, float> features, CivilWarTuning tuning, Func<float> random, out string log)
	{
		List<CivilWarOutcomeDef> candidates = CivilWarCatalog.ValidOutcomes.Where(x => x.Goals != null && x.Goals.Contains(goal)).ToList();
		List<float> weights = candidates.Select(x => Math.Max(0.01f, x.Weight?.Raw(features) ?? 0f)).ToList();
		// Every candidate keeps at least Floor share (scaled by randomness) so no war result is a foregone conclusion.
		float share = CivilWarRules.Clamp(tuning?.Floor ?? 0.05f, 0f, 0.49f) * CivilWarRules.Clamp(tuning?.Randomness ?? 1f, 0f, 1f);
		float total = weights.Sum();
		for (int i = 0; i < weights.Count; i++) weights[i] = Math.Max(weights[i], total * share);
		int index = CivilWarRules.PickWeighted(weights, tuning, random);
		log = string.Join(" ", candidates.Select((x, i) => x.Id + "=" + weights[i].ToString("0.00", System.Globalization.CultureInfo.InvariantCulture)));
		return index >= 0 ? candidates[index] : null;
	}

	// Weighted demand pick from the leader clan's source points. Eligibility filtered by caller.
	internal static CivilWarDemandDef PickDemand(IReadOnlyList<CivilWarDemandDef> eligible, IReadOnlyDictionary<string, float> sourcePoints, CivilWarTuning tuning, Func<float> random)
	{
		if (eligible == null || eligible.Count == 0) return null;
		List<float> weights = new List<float>(eligible.Count);
		foreach (CivilWarDemandDef demand in eligible)
		{
			float weight = demand.BaseWeight;
			foreach (KeyValuePair<string, float> affinity in demand.Affinity)
			{
				if (sourcePoints != null && sourcePoints.TryGetValue(affinity.Key, out float points)) weight += affinity.Value * Math.Max(0f, points) / 20f;
			}
			weights.Add(weight);
		}
		int index = CivilWarRules.PickWeighted(weights, tuning, random);
		return index >= 0 ? eligible[index] : null;
	}
}
