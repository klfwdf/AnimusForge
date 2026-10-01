using System;
using System.Linq;
using System.Reflection;
using System.Runtime.Serialization;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace PolicyEffectModule.ContractTests;

internal static partial class Program
{
	private static JObject PersonalityJson(float mercy = 0f, float honor = 0f,
		float generosity = 0f, float valor = 0f, float calculating = 0f) => new JObject
	{
		["mercy"] = mercy, ["honor"] = honor, ["generosity"] = generosity,
		["valor"] = valor, ["calculating"] = calculating
	};

	private static void TestPolicyVotePersonalityContracts()
	{
		Type rules = SutType("AnimusForge.PolicyVotePersonality");
		Type profileType = SutType("AnimusForge.PolicyVotePersonalityProfile");
		Type behavior = SutType("AnimusForge.CustomPolicyBehavior");
		object Read(JToken json) => InvokeStatic(rules, "ReadRequired", new object[] { json }, 1);
		float Score(object profile, params int[] traits) => (float)InvokeStatic(rules, "Compute",
			new object[] { profile, traits[0], traits[1], traits[2], traits[3], traits[4] }, 6);
		float Delta(bool inverted, float interest, float relation, float personality) =>
			(float)InvokeStatic(behavior, "ComputeVoteScoreDelta",
				new object[] { inverted, interest, relation, personality }, 4);
		void Equal(float actual, float expected, string message) => Check(Math.Abs(actual - expected) < 0.0001f, message);

		object cruelty = Read(PersonalityJson(mercy: -1));
		Equal(Score(cruelty, -2, 0, 0, 0, 0), 1f, "Cruel leaders have positive affinity for explicitly cruel methods.");
		Equal(Score(cruelty, 2, 0, 0, 0, 0), -1f, "Merciful leaders oppose the same methods.");
		Equal(Score(cruelty, 0, 0, 0, 0, 0), 0f, "Neutral personality does not invent an opinion.");
		Equal(Score(cruelty, -1, 0, 0, 0, 0), 0.5f, "Trait strength changes affinity proportionally.");
		Equal(Score(Read(PersonalityJson(mercy: 1)), -2, 0, 0, 0, 0), -1f,
			"Protective methods reverse cruelty affinity; the numeric loss is not the classifier.");
		string[] axes = { "mercy", "honor", "generosity", "valor", "calculating" };
		for (int i = 0; i < axes.Length; i++)
		{
			JObject oneAxis = PersonalityJson(); oneAxis[axes[i]] = 1f;
			object profile = Read(oneAxis);
			int[] traits = new int[5]; traits[i] = 2;
			Equal(Score(profile, traits), 1f, axes[i] + " positive pole is connected.");
			traits[i] = -2;
			Equal(Score(profile, traits), -1f, axes[i] + " negative pole is connected.");
			traits[i] = 0; traits[(i + 1) % 5] = -2;
			Equal(Score(profile, traits), 0f, axes[i] + " does not affect unrelated personality axes.");
		}
		Equal(Score(Read(PersonalityJson()), -2, -2, -2, -2, -2), 0f,
			"All bad traits do not endorse a policy with no personality-related methods.");
		Equal(Score(Read(PersonalityJson(1, 1, 1, 1, 1)), 2, 2, 2, 2, 2), 2f, "Combined affinity has an upper cap.");
		Equal(Score(Read(PersonalityJson(1, 1, 1, 1, 1)), -2, -2, -2, -2, -2), -2f, "Combined affinity has a lower cap.");
		Equal(Score(cruelty, int.MinValue, 0, 0, 0, 0), 1f, "Out-of-range trait levels are bounded without overflow.");
		Equal(Score(null, -2, -2, -2, -2, -2), 0f, "Old policy with no profile retains zero personality adjustment.");
		Equal(Delta(false, -0.55f, 0f, 1f), 0.45f, "Affinity can outweigh moderate loss for a cruel voter.");
		Equal(Delta(false, -0.55f, 0f, -1f), -1.55f, "The same losses plus mercy produce opposition.");
		Equal(Delta(false, -2f, 0f, 1f), -1f, "Severe self-interest loss still makes a cruel voter oppose.");
		Equal(Delta(true, -0.55f, 0.2f, 1f), -0.25f, "Abolition reverses U and P, not the agenda relation term.");
		Equal(Delta(false, -0.55f, 0.2f, 0f), -0.35f, "Disabling personality retains the existing interest and relation score.");

		JObject[] invalid =
		{
			new JObject(), PersonalityJson(), PersonalityJson(), PersonalityJson(),
			PersonalityJson(), PersonalityJson(), PersonalityJson(), PersonalityJson()
		};
		invalid[1]["mercy"] = "-1";
		invalid[2]["honor"] = 1.01;
		invalid[3]["valor"] = double.NaN;
		invalid[4]["calculating"] = double.PositiveInfinity;
		invalid[5]["extra"] = 0;
		invalid[6]["generosity"] = JValue.CreateNull();
		invalid[7].Remove("mercy"); invalid[7]["Mercy"] = 0;
		foreach (JToken malformed in invalid.Cast<JToken>().Concat(new JToken[] { null, new JArray(), JValue.CreateNull() }))
		{
			bool rejected = false;
			try { Read(malformed); } catch (Exception ex) { rejected = Unwrap(ex) is JsonException; }
			Check(rejected, "Malformed new personality profile fails the generation contract.");
		}
		object brokenSavedProfile = JsonConvert.DeserializeObject("{\"mercy\":-1}", profileType);
		Equal(Score(brokenSavedProfile, -2, 0, 0, 0, 0), 0f, "Partial legacy profile does not invent missing axes.");
		object clone = InvokeStatic(rules, "CloneValidated", new[] { cruelty }, 1);
		Check(!ReferenceEquals(clone, cruelty), "Pending assessment clone detaches personality data.");
		profileType.GetProperty("Mercy", All).SetValue(cruelty, 1f);
		Equal(Score(clone, -2, 0, 0, 0, 0), 1f, "Later assessment mutation cannot rewrite the stored snapshot.");

		Type dynamicType = behavior.GetNestedType("DynamicPolicySaveData", All);
		foreach (Type recordType in new[] { dynamicType, SutType("AnimusForge.NpcRulerPolicyRecord") })
		{
			object oldRecord = JsonConvert.DeserializeObject("{}", recordType);
			Check(recordType.GetProperty("VotePersonality", All).GetValue(oldRecord) == null, "Old records load with no personality profile.");
			object record = JsonConvert.DeserializeObject(new JObject { ["votePersonality"] = PersonalityJson(mercy: -1) }.ToString(), recordType);
			object restored = JsonConvert.DeserializeObject(JsonConvert.SerializeObject(record), recordType);
			Equal(Score(recordType.GetProperty("VotePersonality", All).GetValue(restored), -2, 0, 0, 0, 0), 1f,
				"Personality profile survives " + recordType.Name + " serialization.");
		}
		// A policy without any mechanical effects must still carry its personality assessment.
		object host = FormatterServices.GetUninitializedObject(behavior);
		string saved = new JObject { ["votePersonality"] = PersonalityJson(mercy: -1) }.ToString();
		object entry = behavior.GetMethod("BuildVoteInterestEntry", All).Invoke(host, new object[] { saved, 1 });
		Equal(Score(entry.GetType().GetField("Personality", All).GetValue(entry), -2, 0, 0, 0, 0), 1f,
			"Actual vote cache retains personality even when the effect list is empty.");
		object[] skipped = { null, null, null, 987f, false };
		InvokeStatic(behavior, "Patch_KingdomPolicyDecision_DetermineSupport_Postfix", skipped, 5);
		Equal((float)skipped[3], 987f, "A promised vote that skips original scoring is not adjusted.");

		JObject main = new JObject
		{
			["publicFeedback"] = "测试反馈", ["impactSummary"] = "测试影响", ["numericIntent"] = "无直接数值意图",
			["policyContentDigest"] = "测试措施", ["feedbackDigest"] = "测试反应",
			["authoritarianWeight"] = 0.1, ["oligarchicWeight"] = 0, ["egalitarianWeight"] = 0,
			["effectDurationMode"] = "finite", ["durationDays"] = 30, ["votePersonality"] = PersonalityJson(mercy: -1)
		};
		object assessment = InvokeStatic(behavior, "DeserializeMainAssessmentResult", new object[] { main.ToString(), null }, 2);
		Equal(Score(assessment.GetType().GetProperty("VotePersonality", All).GetValue(assessment), -2, 0, 0, 0, 0), 1f,
			"Actual player assessment parser preserves personality.");
		object pendingAssessment = InvokeStatic(behavior, "ClonePlayerPolicyAgendaAssessmentWithoutEffects", new[] { assessment }, 1);
		Equal(Score(pendingAssessment.GetType().GetProperty("VotePersonality", All).GetValue(pendingAssessment), -2, 0, 0, 0, 0), 1f,
			"The real pending player snapshot preserves personality independently of effects.");
		foreach (string field in new[] { "votePersonality", "mercy" })
		{
			JObject missing = (JObject)main.DeepClone();
			if (field == "mercy") ((JObject)missing["votePersonality"]).Remove(field); else missing.Remove(field);
			bool rejected = false;
			try { InvokeStatic(behavior, "DeserializeMainAssessmentResult", new object[] { missing.ToString(), null }, 2); }
			catch (Exception ex) { rejected = Unwrap(ex) is JsonException; }
			Check(rejected, "Fresh player generation cannot silently lose " + field + ".");
		}
	}

	private static void TestNpcVotePersonalityDraft(object context, string draftJson)
	{
		Type npc = SutType("AnimusForge.NpcRulerPolicyBehavior");
		JObject root = JObject.Parse(draftJson);
		root["policy"]["votePersonality"] = PersonalityJson(mercy: -1);
		object[] parsed = { root.ToString(Formatting.None), context, null, null };
		Check((bool)InvokeStatic(npc, "TryParseNpcPolicyDraftResponse", parsed, 4), "NPC parser accepts a non-neutral personality profile.");
		object raw = InvokeStatic(npc, "BuildNpcPolicyRawRecordFromDraft", new[] { context, parsed[2], null }, 3);
		JObject saved = JObject.Parse(JsonConvert.SerializeObject(raw));
		Check(saved["votePersonality"]?["mercy"]?.Value<float>() == -1f,
			"NPC draft to raw record preserves personality, rather than recomputing from the ruler.");
		foreach (bool missing in new[] { true, false })
		{
			JObject bad = (JObject)root.DeepClone();
			if (missing) ((JObject)bad["policy"]).Remove("votePersonality");
			else bad["policy"]["votePersonality"]["mercy"] = "-1";
			object[] rejected = { bad.ToString(Formatting.None), context, null, null };
			Check(!(bool)InvokeStatic(npc, "TryParseNpcPolicyDraftResponse", rejected, 4), "New NPC assessments reject missing/coerced personality data.");
		}
	}
}
