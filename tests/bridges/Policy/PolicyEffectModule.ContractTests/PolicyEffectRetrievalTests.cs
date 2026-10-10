using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using AnimusForge.PolicyEffects;

namespace PolicyEffectModule.ContractTests;

internal static partial class Program
{
	private static void TestProductionPolicyRetrievalContracts()
	{
		IPolicyEffectModule[] modules = ExpectedPromptVisibleModuleIds.Select(RequirePolicyEffectModule).ToArray();
		const string money = "每日给本国领主发放三百第纳尔";
		const string training = "设立常设教官训练其亲自带领的部队";
		IReadOnlyList<string> queries = PolicyEffectModuleRouter.BuildPostAssessmentQueries(
			"综合改革", money + "；" + training + "；" + money,
			"领主收入提高并接受训练", "每位领主每日增加300第纳尔", modules);
		Check(queries.Skip(1).Contains(money) && queries.Skip(1).Contains(training),
			"Production queries must actually emit independent original clauses, not only retain them inside the primary query.");
		Check(queries.Distinct(StringComparer.Ordinal).Count() == queries.Count && queries.Count <= 12,
			"Production clause collection and output deduplication must not suppress clauses or exceed the embedding budget.");
		string longText = new string('甲', 700) + "每日粮食储备增加5点";
		queries = PolicyEffectModuleRouter.BuildPostAssessmentQueries("长政策", longText, "", "", modules);
		Check(queries[0].Contains(longText) && queries.Skip(1).Any(q => q.Contains("每日粮食储备增加5点")),
			"Long unsplit clauses need bounded tail windows as well as an unmodified authoritative primary query.");
		Check(queries.Skip(1).All(q => q.Length <= PolicyEffectModuleRouter.QueryIntentCharacterLimit),
			"Secondary queries must stay within the tokenizer-safe character window.");
		queries = PolicyEffectModuleRouter.BuildPostAssessmentQueries("无数值", "仅更名纪念节日", "举行庆典", "无直接数值意图", modules);
		Check(!queries.Any(q => q == "数值意图：无直接数值意图"),
			"The literal no-numeric-intent sentinel must not consume an embedding or distort numeric retrieval.");
		TestNpcRetrievalRequestSnapshot();
	}

	private static void TestNpcRetrievalRequestSnapshot()
	{
		string temp = Path.Combine(Path.GetTempPath(), "policy-npc-retrieval-" + Guid.NewGuid().ToString("N"));
		Directory.CreateDirectory(temp);
		PolicyEffectModuleRetrievalSettings.SetStorageDirectoryOverrideForContractTests(temp);
		try
		{
			MethodInfo capture = typeof(PolicyEffectModuleRetrievalSettings).GetMethod("CaptureRequestSnapshot", All);
			Check(capture != null, "NPC retrieval must have a request snapshot capture owner.");
			object frozen = capture.Invoke(null, new object[] { PolicyEffectRetrievalContext.NpcRulerKingdom, 6 });
			Task.Run(async () =>
			{
				await Task.Yield();
				Dictionary<string, PolicyEffectModuleRetrievalState> states = PolicyEffectModuleRetrievalSettings.CreateEditableStateSnapshot();
				foreach (PolicyEffectModuleRetrievalState state in states.Values) state.RulerPolicyEnabled = false;
				Check(PolicyEffectModuleRetrievalSettings.TrySave(states, out string error), "MCM test mutation failed: " + error);
			}).GetAwaiter().GetResult();
			string[] ids = ((IEnumerable<string>)frozen.GetType().GetProperty("EnabledModuleIds", All).GetValue(frozen)).ToArray();
			Check(ids.Length == 18 && PolicyEffectModuleRetrievalSettings.GetEnabledModules(PolicyEffectRetrievalContext.NpcRulerKingdom).Count == 0,
				"A genuinely yielded MCM edit must not mutate the in-flight retrieval snapshot.");
			Check((int)frozen.GetType().GetProperty("RequestedDetailCount", All).GetValue(frozen) == 6,
				"In-flight detail budgets must retain the captured value.");
			object later = capture.Invoke(null, new object[] { PolicyEffectRetrievalContext.NpcRulerKingdom, 1 });
			Check(!((IEnumerable<string>)later.GetType().GetProperty("EnabledModuleIds", All).GetValue(later)).Any(),
				"Later requests must observe newly disabled modules, without an active-effect kill switch.");
			Type npc = SutType("AnimusForge.NpcRulerPolicyBehavior");
			Type batch = npc.GetNestedType("NpcRulerPolicyBatchContext", All);
			object context = Activator.CreateInstance(batch, true);
			batch.GetField("CandidateModuleIds", All).SetValue(context, ids.ToList());
			batch.GetField("DetailedModuleIds", All).SetValue(context, ids.Take(6).ToList());
			npc.GetMethod("EnsureNpcPolicyModuleAllowlists", All).Invoke(null, new[] { context, (object)string.Empty });
			Check(batch.GetField("EffectRetrievalSnapshot", All) != null,
				"NPC batch context must retain the frozen request through the draft await.");
			string source = File.ReadAllText(Path.Combine(FindRepositoryRoot(AppDomain.CurrentDomain.BaseDirectory), "PolicySystem", "Npc", "NpcRulerPolicyBehavior.Generation.cs"));
			Check(source.Split(new[] { "context.EffectRetrievalSnapshot = PolicyEffectModuleRetrievalSettings.CaptureRequestSnapshot(" }, StringSplitOptions.None).Length == 3,
				"Both suggested and autonomous NPC submission paths must capture retrieval before queueing.");
			string routing = source.Substring(source.IndexOf("private void PrepareNpcPolicyEffectRouting(", StringComparison.Ordinal));
			routing = routing.Substring(0, routing.IndexOf("private static PolicyTargetHandleDirectory", StringComparison.Ordinal));
			Check(routing.Contains("retrieval.EnabledModuleIds") && routing.Contains("retrieval.RequestedDetailCount")
				&& !routing.Contains("GetEnabledModules(") && !routing.Contains("GetPlayerPolicyEffectModuleDetailCount"),
				"The actual post-await NPC routing consumer must use only the submission snapshot.");
		}
		finally
		{
			PolicyEffectModuleRetrievalSettings.SetStorageDirectoryOverrideForContractTests(null);
		}
	}

	private static void TestProductionPolicyOnnxMatrix()
	{
		string root = FindRepositoryRoot(AppDomain.CurrentDomain.BaseDirectory);
		string fixture = Path.Combine(root, "tests", "bridges", "Policy", "PolicyEffectModule.ContractTests", "TestData", "policy_effect_production_retrieval.jsonl");
		JObject[] cases = File.ReadAllLines(fixture).Where(line => !string.IsNullOrWhiteSpace(line)).Select(JObject.Parse).ToArray();
		foreach (string id in ExpectedPromptVisibleModuleIds)
		{
			Check(new[] { "direct", "paraphrase", "adjacent", "negation", "scope" }.All(family =>
				cases.Any(c => (string)c["moduleId"] == id && (string)c["family"] == family)),
				"The production matrix must cover all five families for module " + id);
		}
		List<string> failures = new List<string>();
		foreach (JObject c in cases)
		{
			PolicyEffectRetrievalContext context = (PolicyEffectRetrievalContext)Enum.Parse(typeof(PolicyEffectRetrievalContext), (string)c["context"]);
			HashSet<string> disabled = new HashSet<string>(c["disabled"]?.Values<string>() ?? Enumerable.Empty<string>(), StringComparer.Ordinal);
			string[] enabled = ExpectedPromptVisibleModuleIds.Where(id => !disabled.Contains(id)).ToArray();
			Stopwatch elapsed = Stopwatch.StartNew();
			PolicyEffectModuleRoutingResult result = PolicyEffectModuleRouter.RouteAfterAssessment(
				(string)c["policyName"], (string)c["policyContent"], (string)c["impactSummary"], (string)c["numericIntent"],
				context, enabled, (int?)c["detailLimit"] ?? 6, new AnimusForge.PolicyTextEmbeddingSession());
			string[] candidates = result.Candidates.Select(s => s.Module.Id).ToArray();
			string[] details = result.Details.Select(s => s.Module.Id).ToArray();
			string[] missingCandidates = (c["expectedCandidates"]?.Values<string>() ?? Enumerable.Empty<string>()).Except(candidates).ToArray();
			string[] missingDetails = (c["expectedDetails"]?.Values<string>() ?? Enumerable.Empty<string>()).Except(details).ToArray();
			string[] forbidden = (c["forbidden"]?.Values<string>() ?? Enumerable.Empty<string>()).Intersect(candidates).ToArray();
			bool passed = missingCandidates.Length == 0 && missingDetails.Length == 0 && forbidden.Length == 0
				&& details.Length <= Math.Min(8, (int?)c["detailLimit"] ?? 6)
				&& result.IntentCount <= 12 && details.All(candidates.Contains)
				&& result.Candidates.All(s => !disabled.Contains(s.Module.Id) && PolicyEffectModuleRetrievalSettings.IsContextSupported(s.Module, context));
			Console.WriteLine("PRODUCTION_ONNX " + new JObject
			{
				["id"] = c["id"], ["family"] = c["family"], ["passed"] = passed,
				["queries"] = result.IntentCount, ["enabled"] = result.EnabledModuleCount,
				["candidates"] = new JArray(candidates), ["details"] = new JArray(details),
				["missingCandidates"] = new JArray(missingCandidates), ["missingDetails"] = new JArray(missingDetails),
				["forbidden"] = new JArray(forbidden), ["elapsedMs"] = elapsed.ElapsedMilliseconds,
				["intentTopIds"] = new JArray(result.IntentTopModuleIds)
			}.ToString(Formatting.None));
			if (!passed) failures.Add((string)c["id"]);
		}
		Check(failures.Count == 0, "Production ONNX matrix failures: " + string.Join(",", failures));
	}
}
