using AnimusForge;

int failures = 0;
void Check(bool condition, string scenario)
{
	if (condition) return;
	failures++;
	Console.WriteLine("FAIL: " + scenario);
}

Func<float> Fixed(float value) => () => value;
Func<float> Sequence(params float[] values)
{
	int i = 0;
	return () => values[Math.Min(i++, values.Length - 1)];
}

var tuning = new CivilWarTuning();
var expectedValue = new CivilWarTuning { Randomness = 0f };

// ---- catalog integrity
List<string> errors = CivilWarCatalog.Validate(CivilWarEffectIds.All);
foreach (string error in errors) Console.WriteLine("catalog: " + error);
Check(errors.Count == 0, "shipped catalog validates against registered effect ids");
CivilWarCatalog.Initialize(CivilWarEffectIds.All);
Check(CivilWarCatalog.FindDemand(CivilWarCatalog.UsurpDemandId) != null, "usurp demand indexed");
Check(CivilWarCatalog.FindDemand("missing_demand") == null, "removed demand id resolves to null (save dissolves faction)");
Check(CivilWarCatalog.Validate(new HashSet<string>()).Any(x => x.Contains("unknown effect")), "unregistered effect is reported");

// ---- shaping: clamp to [5%, 95%] with randomness, step with randomness 0
Check(Math.Abs(CivilWarRules.Shape(-3f, tuning) - 0.05f) < 1e-5f, "floor 5%");
Check(Math.Abs(CivilWarRules.Shape(3f, tuning) - 0.95f) < 1e-5f, "ceiling 95%");
Check(Math.Abs(CivilWarRules.Shape(0.4f, tuning) - 0.4f) < 1e-5f, "mid value unchanged");
Check(CivilWarRules.Shape(0.4f, expectedValue) == 0f && CivilWarRules.Shape(0.6f, expectedValue) == 1f, "randomness 0 is expected-value");
Check(Math.Abs(CivilWarRules.Shape(0.8f, new CivilWarTuning { Randomness = 0.5f }) - 0.9f) < 1e-5f, "randomness 0.5 halves distance to step");
Check(CivilWarRules.Shape(float.NaN, tuning) >= 0.05f, "NaN safe");

// ---- weighted pick
Check(CivilWarRules.PickWeighted(new[] { 1f, 3f }, tuning, Fixed(0.1f)) == 0, "low roll picks first bucket");
Check(CivilWarRules.PickWeighted(new[] { 1f, 3f }, tuning, Fixed(0.9f)) == 1, "high roll picks second bucket");
Check(CivilWarRules.PickWeighted(new[] { 1f, 3f }, expectedValue, Fixed(0.1f)) == 1, "randomness 0 picks heaviest");
Check(CivilWarRules.PickWeighted(new[] { 0f, -1f }, tuning, Fixed(0.5f)) == 0, "all non-positive falls back");
Check(CivilWarRules.PickWeighted(Array.Empty<float>(), tuning, Fixed(0.5f)) == -1, "empty returns -1");

// ---- ultimatum: same inputs, different rolls => different rulings (randomness requirement)
CivilWarDemandDef peace = CivilWarCatalog.FindDemand("make_peace");
var weakFaction = new Dictionary<string, float> { [CivilWarFeature.FactionPower] = 0.1f, [CivilWarFeature.KingAuthoritarian] = 1f, [CivilWarFeature.FactionGrievance] = 0.3f };
CivilWarUltimatumResult accepted = CivilWarDecisions.RuleOnUltimatum(peace, weakFaction, 0, tuning, Fixed(0.01f));
CivilWarUltimatumResult refused = CivilWarDecisions.RuleOnUltimatum(peace, weakFaction, 0, tuning, Fixed(0.99f));
Check(accepted.Ruling == CivilWarRuling.Accept, "low roll accepts even for a hostile king");
Check(refused.Ruling == CivilWarRuling.Refuse && !refused.Escalate, "high roll refuses without escalation");
Check(!refused.ConvertToUsurp, "first refusal never converts");
Check(refused.EscalateRoll.Chance <= 0.95f && refused.EscalateRoll.Chance >= 0.05f, "escalation chance clamped");

// accept fails, defer fails, escalate passes
CivilWarUltimatumResult escalated = CivilWarDecisions.RuleOnUltimatum(peace, weakFaction, 0, tuning, Sequence(0.99f, 0.99f, 0.01f));
Check(escalated.Ruling == CivilWarRuling.Refuse && escalated.Escalate, "low escalation roll escalates a non-war demand");
// accept fails, defer passes
CivilWarUltimatumResult deferred = CivilWarDecisions.RuleOnUltimatum(peace, weakFaction, 0, tuning, Sequence(0.99f, 0.01f));
Check(deferred.Ruling == CivilWarRuling.Defer, "defer branch reachable");
// second refusal: escalate fails, convert passes
CivilWarUltimatumResult converted = CivilWarDecisions.RollRefusal(peace, weakFaction, 1, tuning, Sequence(0.99f, 0.01f));
Check(!converted.Escalate && converted.ConvertToUsurp, "second refusal may convert to usurp");
CivilWarUltimatumResult usurpNoConvert = CivilWarDecisions.RollRefusal(CivilWarCatalog.FindDemand("usurp"), weakFaction, 5, tuning, Sequence(0.99f, 0.01f));
Check(!usurpNoConvert.ConvertToUsurp, "usurp demand never converts");

// non-war demand escalation is scaled by 0.3 relative to war demand with same weights
var strong = new Dictionary<string, float> { [CivilWarFeature.FactionPower] = 1f, [CivilWarFeature.FactionGrievance] = 1f };
CivilWarRoll peaceEsc = CivilWarDecisions.RollRefusal(peace, strong, 0, tuning, Fixed(0.99f)).EscalateRoll;
CivilWarRoll autonomyEsc = CivilWarDecisions.RollRefusal(CivilWarCatalog.FindDemand("autonomy"), strong, 0, tuning, Fixed(0.99f)).EscalateRoll;
Check(peaceEsc.Chance < autonomyEsc.Chance, "non-war demand escalates less often than autonomy");
Check(peaceEsc.Chance <= 0.30f, "non-war escalation ~x0.3");

// ---- outcomes: every war goal has candidates; score shifts the favourite
foreach (CivilWarWarGoal goal in new[] { CivilWarWarGoal.Coerce, CivilWarWarGoal.Usurp, CivilWarWarGoal.Secede })
{
	CivilWarOutcomeDef any = CivilWarDecisions.PickOutcome(goal, new Dictionary<string, float>(), tuning, Fixed(0.5f), out _);
	Check(any != null, "outcome exists for " + goal);
}
var rebelsWinning = new Dictionary<string, float> { [CivilWarFeature.WarScore] = 1f, [CivilWarFeature.WarScoreAbs] = 1f };
var crownWinning = new Dictionary<string, float> { [CivilWarFeature.WarScore] = -1f, [CivilWarFeature.WarScoreAbs] = 1f };
Check(CivilWarDecisions.PickOutcome(CivilWarWarGoal.Usurp, rebelsWinning, expectedValue, Fixed(0.5f), out _).Id == "rebels_usurp", "winning rebels favour usurp");
Check(CivilWarDecisions.PickOutcome(CivilWarWarGoal.Usurp, crownWinning, expectedValue, Fixed(0.5f), out _).Id == "crown_victory", "winning crown favours suppression");
var rebelIds = new HashSet<string>();
for (int i = 0; i < 20; i++) rebelIds.Add(CivilWarDecisions.PickOutcome(CivilWarWarGoal.Secede, rebelsWinning, tuning, Fixed(i / 20f), out _).Id);
Check(rebelIds.Count >= 2, "outcome still random when one side is winning");

// ---- demand pick follows grievance source affinity
var executions = new Dictionary<string, float> { ["policy_imposed"] = 100f };
CivilWarDemandDef picked = CivilWarDecisions.PickDemand(CivilWarCatalog.ValidDemands, executions, expectedValue, Fixed(0.5f));
Check(picked?.Id == "revoke_policy", "policy grievance favours reform faction");
Check(CivilWarDecisions.PickDemand(new List<CivilWarDemandDef>(), executions, tuning, Fixed(0.5f)) == null, "no eligible demand -> null");

Console.WriteLine(failures == 0 ? "CivilWarRules smoke tests passed." : failures + " check(s) failed.");
return failures == 0 ? 0 : 1;
