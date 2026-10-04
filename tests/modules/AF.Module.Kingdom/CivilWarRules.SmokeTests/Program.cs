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

// ---- multi-faction (v3)
var cap3 = new CivilWarTuning { MaxFactions = 3 };
Check(CivilWarFactionRules.CanFormFaction(0, 10, 0, cap3), "first faction may form");
Check(CivilWarFactionRules.CanFormFaction(2, 10, 0, cap3), "third faction may form under cap 3");
Check(!CivilWarFactionRules.CanFormFaction(3, 10, 0, cap3), "cap reached blocks a fourth faction");
Check(!CivilWarFactionRules.CanFormFaction(1, 10, 12, cap3), "cooldown blocks new factions");
Check(!CivilWarFactionRules.CanFormFaction(4, 10, 0, new CivilWarTuning { MaxFactions = 99 }), "cap is clamped to 4");
Check(CivilWarFactionRules.CanFormFaction(0, 10, 0, new CivilWarTuning { MaxFactions = 0 }), "cap is clamped to at least 1");

var singleWar = new CivilWarTuning { AllowConcurrentWars = false };
var multiWar = new CivilWarTuning { AllowConcurrentWars = true };
Check(CivilWarFactionRules.CanOpenWar(false, singleWar), "mode A: war allowed when no other faction fights");
Check(!CivilWarFactionRules.CanOpenWar(true, singleWar), "mode A: second war waits");
Check(CivilWarFactionRules.CanOpenWar(true, multiWar), "mode B: concurrent wars allowed");

Check(CivilWarFactionRules.IsDemandTaken(new[] { "make_peace", "redress" }, "redress"), "held demand is taken");
Check(!CivilWarFactionRules.IsDemandTaken(new[] { "make_peace" }, "redress"), "other demand is free");
Check(!CivilWarFactionRules.IsDemandTaken(null, "redress"), "no factions -> free");

// ---- escalation needs enough refusals; loyal clans rarely join the opposition
Check(new CivilWarTuning().UltimatumDelayWeeks == 4, "default ultimatum interval is 4 weeks");
Check(!CivilWarFactionRules.HasEnoughRefusals(1, new CivilWarTuning()), "default: one refusal is not enough for war");
Check(CivilWarFactionRules.HasEnoughRefusals(2, new CivilWarTuning()), "default: two refusals allow war");
Check(CivilWarFactionRules.HasEnoughRefusals(1, new CivilWarTuning { MinRefusalsBeforeWar = 1 }), "minimum 1 restores first-refusal war");
Check(!CivilWarFactionRules.HasEnoughRefusals(3, new CivilWarTuning { MinRefusalsBeforeWar = 9 }), "minimum above MaxRefusals is clamped (3 < 4)");
Check(CivilWarFactionRules.HasEnoughRefusals(4, new CivilWarTuning { MinRefusalsBeforeWar = 9 }), "clamped minimum is reachable at MaxRefusals");
Check(CivilWarFactionRules.JoinRelationFactor(-30) == 1f && CivilWarFactionRules.JoinRelationFactor(0) == 1f, "hostile or neutral clans join at full chance");
Check(Math.Abs(CivilWarFactionRules.JoinRelationFactor(30) - 0.5f) < 1e-5f, "relation 30 halves the join chance");
Check(CivilWarFactionRules.JoinRelationFactor(60) == CivilWarFactionRules.JoinRelationMinFactor && CivilWarFactionRules.JoinRelationFactor(100) == CivilWarFactionRules.JoinRelationMinFactor, "clans at relation 60+ keep a small non-zero join chance");
Check(CivilWarFactionRules.JoinRelationFactor(100) > 0f && CivilWarFactionRules.PassesJoinRelationGate(CivilWarFactionRules.JoinRelationFactor(100), tuning, Fixed(0.05f)), "best-relation clan can still join on a low roll");
Check(!CivilWarFactionRules.PassesJoinRelationGate(CivilWarFactionRules.JoinRelationFactor(100), tuning, Fixed(0.5f)), "best-relation clan usually does not join");
Check(CivilWarFactionRules.PassesJoinRelationGate(1f, tuning, Fixed(0.99f)), "factor 1 always passes");
Check(!CivilWarFactionRules.PassesJoinRelationGate(0f, tuning, Fixed(0f)), "factor 0 never passes");
Check(CivilWarFactionRules.PassesJoinRelationGate(0.5f, tuning, Fixed(0.4f)) && !CivilWarFactionRules.PassesJoinRelationGate(0.5f, tuning, Fixed(0.6f)), "gate compares the roll with the factor");
Check(CivilWarFactionRules.PassesJoinRelationGate(0.5f, expectedValue, Fixed(0.99f)) && !CivilWarFactionRules.PassesJoinRelationGate(0.49f, expectedValue, Fixed(0f)), "randomness 0 passes only at factor >= 0.5");

Check(CivilWarFactionRules.FactionGrievance(new float[0]) == 0f, "empty faction grievance is 0");
Check(Math.Abs(CivilWarFactionRules.FactionGrievance(new[] { 80f, 40f }) - 60f) < 1e-4f, "faction grievance is the member mean");
Check(CivilWarFactionRules.FactionGrievance(new[] { 250f }) == 100f, "faction grievance clamped to 100");

Check(CivilWarFactionRules.PickFactionForClan(new float[0], tuning, Fixed(0.5f)) == -1, "no faction -> -1");
Check(CivilWarFactionRules.PickFactionForClan(new[] { 0.1f, 5f }, expectedValue, Fixed(0.1f)) == 1, "clan joins the faction its grievance fits best");
var joinPicks = new HashSet<int>();
for (int i = 0; i < 20; i++) joinPicks.Add(CivilWarFactionRules.PickFactionForClan(new[] { 1f, 1f }, tuning, Fixed(i / 20f)));
Check(joinPicks.Count == 2, "equal fit splits clans between factions");

// ---- aftermath: a finished civil war calms or emboldens the remaining factions
var cooldown8 = new CivilWarTuning { CooldownWeeks = 8 };
Check(CivilWarAftermathRules.TruceWeeks(cooldown8) == 4, "truce is half the cooldown");
Check(CivilWarAftermathRules.TruceWeeks(new CivilWarTuning { CooldownWeeks = 0 }) == 2, "truce never below 2 weeks");
Check(CivilWarAftermathRules.MoodWeeks(cooldown8) > CivilWarAftermathRules.TruceWeeks(cooldown8), "mood outlasts the truce");
Check(CivilWarAftermathRules.GrievanceFactor(CivilWarAftermath.Suppressed) < 1f, "crown victory lowers other factions' grievance");
Check(CivilWarAftermathRules.GrievanceFactor(CivilWarAftermath.Emboldened) > 1f, "rebel victory raises other factions' grievance");
Check(CivilWarAftermathRules.GrievanceFactor(CivilWarAftermath.Settled) == 1f, "negotiated end leaves grievance");
Check(CivilWarAftermathRules.LeaveFactor(CivilWarAftermath.Suppressed) > 1f && CivilWarAftermathRules.LeaveFactor(CivilWarAftermath.Emboldened) < 1f, "members leave after suppression, stay after rebel win");
Check(CivilWarAftermathRules.EscalationFactor(CivilWarAftermath.Suppressed) < 1f && CivilWarAftermathRules.EscalationFactor(CivilWarAftermath.Emboldened) > 1f, "escalation scaled by aftermath");
Check(CivilWarAftermathRules.Current(CivilWarAftermath.Suppressed, 10, 12) == CivilWarAftermath.Suppressed, "mood active before expiry");
Check(CivilWarAftermathRules.Current(CivilWarAftermath.Suppressed, 13, 12) == CivilWarAftermath.None, "mood expires");
CivilWarDemandDef autonomyDemand = CivilWarCatalog.FindDemand("autonomy");
float calm = CivilWarDecisions.RollRefusal(autonomyDemand, strong, 0, tuning, Fixed(0.99f), CivilWarAftermathRules.EscalationFactor(CivilWarAftermath.Suppressed)).EscalateRoll.Chance;
float bold = CivilWarDecisions.RollRefusal(autonomyDemand, strong, 0, tuning, Fixed(0.99f), CivilWarAftermathRules.EscalationFactor(CivilWarAftermath.Emboldened)).EscalateRoll.Chance;
Check(calm < autonomyEsc.Chance && bold >= autonomyEsc.Chance, "suppressed factions escalate less, emboldened ones more");

// ---- v2 save shape still deserializes into v3 (single "Faction" -> LegacyFaction, Factions empty)
string v2Json = "{\"Version\":2,\"Kingdoms\":{\"empire_w\":{\"KingdomId\":\"empire_w\",\"Stage\":3,\"Faction\":{\"Id\":\"civilwar-faction-empire_w-1\",\"DemandId\":\"make_peace\",\"LeaderClanId\":\"clan_a\",\"Refusals\":1},\"Clans\":{\"clan_a\":{\"ClanId\":\"clan_a\",\"Side\":1}}}}}";
var v2 = Newtonsoft.Json.JsonConvert.DeserializeObject<KingdomCivilWarStorage>(v2Json);
KingdomCivilWarKingdomState v2Kingdom = v2.Kingdoms["empire_w"];
Check(v2Kingdom.LegacyFaction != null && v2Kingdom.LegacyFaction.Refusals == 1, "v2 single faction is read into LegacyFaction");
Check(v2Kingdom.Factions != null && v2Kingdom.Factions.Count == 0, "v2 save starts with no v3 factions (owner migrates)");
Check(v2Kingdom.Clans["clan_a"].FactionId == "", "v2 clan has empty faction id (owner assigns legacy id)");
string v3Json = Newtonsoft.Json.JsonConvert.SerializeObject(new KingdomCivilWarStorage());
Check(!v3Json.Contains("\"Faction\""), "v3 save never writes the legacy Faction field");

Console.WriteLine(failures == 0 ? "CivilWarRules smoke tests passed." : failures + " check(s) failed.");
return failures == 0 ? 0 : 1;
